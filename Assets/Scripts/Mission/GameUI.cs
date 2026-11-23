using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Semua UI gameplay: daftar misi (kiri atas), prompt proximity, toast, catatan papan tulis,
/// keypad laci, dan fade antar scene. Dibangun lewat kode dan hidup lintas scene.
/// </summary>
public partial class GameUI : MonoBehaviour
{
    public static GameUI Instance { get; private set; }

    // ---- Palet warna ----
    private static readonly Color PanelFill = new Color32(9, 13, 30, 240);
    private static readonly Color PanelBorder = new Color32(38, 50, 84, 255);
    private static readonly Color TextMain = new Color32(232, 236, 246, 255);
    private static readonly Color TextDim = new Color32(150, 160, 185, 255);
    private static readonly Color Gold = new Color32(255, 196, 61, 255);
    private static readonly Color Green = new Color32(46, 213, 115, 255);
    private static readonly Color Red = new Color32(255, 82, 96, 255);

    private RectTransform canvasRt;

    // Skala panel misi (ukuran genap supaya teks jatuh tepat di pixel -> tidak blur)
    private const float MS = 0.8f;
    private static float M(float v) => Mathf.Round(v * MS * 0.5f) * 2f;
    private static int MI(int v) => Mathf.RoundToInt(v * MS);

    // Misi
    private sealed class StepRow
    {
        public RectTransform Root;
        public Image BoxBorder;
        public Image BoxFill;
        public GameObject Check;
        public Text Label;
        public Image Strike;
    }
    private UIFactory.BorderedBox missionBox;
    private Text missionTitle;
    private readonly List<StepRow> rows = new List<StepRow>();

    // Prompt
    private UIFactory.BorderedBox promptBox;
    private Text promptLabel;
    private CanvasGroup promptGroup;
    private ProximityInteractable shownPrompt;

    // Toast
    private UIFactory.BorderedBox toastBox;
    private Text toastLabel;
    private CanvasGroup toastGroup;
    private Coroutine toastRoutine;
    private const float ToastX = 200f; // geser kanan agar tidak menimpa panel misi di kiri atas

    // Catatan papan tulis
    private RectTransform noteRoot;
    private UIFactory.BorderedBox noteFrame;
    private Text noteCode;
    private bool noteOpen;

    // Keypad
    private KeypadPanel keypad;

    // Fade
    private Image fade;
    private bool transitioning;

    public bool ModalOpen => noteOpen || announceOpen || (keypad != null && keypad.IsOpen) ||
                             (wordPanel != null && wordPanel.IsOpen);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => Instance = null;

    // ------------------------------------------------------------------ lifecycle

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        BuildCanvas();
        EnsureEventSystem();
        BuildMissionPanel();
        BuildPrompt();
        BuildToast();
        BuildNote();
        BuildAnnouncement();
        BuildKeypad();
        BuildWordPanel();
        BuildFade();

        var m = MissionManager.Instance;
        if (m != null)
        {
            m.MissionChanged += RebuildMissionPanel;
            m.StepCompleted += OnStepCompleted;
            m.KeyStateChanged += OnKeyStateChanged;
        }
        RebuildMissionPanel();
    }

    private void OnDestroy()
    {
        var m = MissionManager.Instance;
        if (m != null)
        {
            m.MissionChanged -= RebuildMissionPanel;
            m.StepCompleted -= OnStepCompleted;
            m.KeyStateChanged -= OnKeyStateChanged;
        }
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        bool modal = ModalOpen || transitioning;
        HandleNoteInput();
        HandleAnnouncementInput();                 // dievaluasi dulu supaya 'E' penutup tidak langsung membuka lagi
        ProximityInteractable.Tick(modal);
        UpdatePrompt(modal);
    }

    // ------------------------------------------------------------------ canvas

    private void BuildCanvas()
    {
        var go = new GameObject("GameCanvas");
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(transform, false);

        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        go.AddComponent<GraphicRaycaster>();
        canvasRt = (RectTransform)go.transform;
    }

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;

        var go = new GameObject("EventSystem");
        go.transform.SetParent(transform, false);
        go.AddComponent<EventSystem>();
        var module = go.AddComponent<InputSystemUIInputModule>();
        module.AssignDefaultActions();
    }

    // ------------------------------------------------------------------ misi (kiri atas)

    private void BuildMissionPanel()
    {
        missionBox = UIFactory.Bordered("MissionPanel", canvasRt, PanelFill, PanelBorder, 2, 18);
        UIFactory.Place(missionBox.Root, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(28f, -28f), new Vector2(M(640f), M(200f)));

        missionTitle = UIFactory.Label("Title", missionBox.Inner, "", MI(30), Gold);
        UIFactory.PlaceTL(missionTitle.rectTransform, M(24), -M(14), M(590), M(46));

        var divider = UIFactory.Box("Divider", missionBox.Inner, PanelBorder);
        UIFactory.PlaceTL(divider.rectTransform, M(24), -M(64), M(586), 2);
    }

    private void RebuildMissionPanel()
    {
        foreach (var row in rows)
            if (row.Root != null) Destroy(row.Root.gameObject);
        rows.Clear();

        var m = MissionManager.Instance;
        var def = m != null ? m.Current : null;
        if (def == null)
        {
            missionBox.Root.gameObject.SetActive(false);
            return;
        }

        missionBox.Root.gameObject.SetActive(true);
        missionTitle.text = def.title;

        for (int i = 0; i < def.steps.Length; i++)
        {
            var row = CreateRow(i, def.steps[i]);
            rows.Add(row);
            if (m.IsStepDone(i)) SetRowDone(row, false);
        }

        float height = M(80f) + def.steps.Length * M(52f) + M(16f);
        missionBox.Root.sizeDelta = new Vector2(M(640f), height);
    }

    private StepRow CreateRow(int index, string text)
    {
        var rt = UIFactory.NewRect("Step" + index, missionBox.Inner);
        UIFactory.PlaceTL(rt, M(24), -M(80) - index * M(52), M(590), M(44));

        var box = UIFactory.Bordered("Checkbox", rt, new Color32(18, 24, 48, 255),
            new Color32(120, 130, 170, 255), 2, 6);
        UIFactory.Place(box.Root, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(M(32f), M(32f)));

        // Tanda centang digambar dari dua batang yang diputar (tidak bergantung pada glyph font).
        var check = UIFactory.NewRect("Check", box.Inner);
        UIFactory.Stretch(check);
        var shortArm = UIFactory.Box("ArmShort", check, Color.white, 2);
        UIFactory.PlaceCenter(shortArm.rectTransform, -3.5f, -2.5f, 4f, 9f);
        shortArm.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 42f);
        var longArm = UIFactory.Box("ArmLong", check, Color.white, 2);
        UIFactory.PlaceCenter(longArm.rectTransform, 2.5f, 0f, 4f, 15f);
        longArm.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -40f);
        check.gameObject.SetActive(false);

        var label = UIFactory.Label("Label", rt, text, MI(25), TextMain, TextAnchor.MiddleLeft, FontStyle.Normal);
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        UIFactory.Place(label.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(M(46f), 0f), new Vector2(M(540f), M(44f)));

        var strike = UIFactory.Box("Strike", rt, Green);
        UIFactory.Place(strike.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(M(46f), 0f), new Vector2(0f, 2f));
        strike.gameObject.SetActive(false);

        return new StepRow
        {
            Root = rt,
            BoxBorder = box.Border,
            BoxFill = box.Fill,
            Check = check.gameObject,
            Label = label,
            Strike = strike
        };
    }

    private void OnStepCompleted(int index)
    {
        if (index >= 0 && index < rows.Count) SetRowDone(rows[index], true);
    }

    private void SetRowDone(StepRow row, bool animate)
    {
        row.BoxFill.color = Green;
        row.BoxBorder.color = Green;
        row.Check.SetActive(true);
        row.Label.color = new Color(Green.r, Green.g, Green.b, 0.9f);
        row.Strike.gameObject.SetActive(true);

        float fullWidth = row.Label.preferredWidth + 4f;
        if (animate) StartCoroutine(AnimateRowDone(row, fullWidth));
        else row.Strike.rectTransform.sizeDelta = new Vector2(fullWidth, 2f);
    }

    private IEnumerator AnimateRowDone(StepRow row, float fullWidth)
    {
        const float duration = 0.3f;
        float t = 0f;
        while (t < duration)
        {
            if (row.Root == null) yield break;
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            row.Strike.rectTransform.sizeDelta = new Vector2(fullWidth * k, 2f);
            float punch = 1f + 0.06f * Mathf.Sin(k * Mathf.PI);
            row.Root.localScale = new Vector3(punch, punch, 1f);
            yield return null;
        }
        if (row.Root == null) yield break;
        row.Strike.rectTransform.sizeDelta = new Vector2(fullWidth, 2f);
        row.Root.localScale = Vector3.one;
    }

    private void OnKeyStateChanged()
    {
        var m = MissionManager.Instance;
        KeyIndicator.SetVisible(m != null && m.HasDoorKey);
    }

    // ------------------------------------------------------------------ prompt proximity

    private void BuildPrompt()
    {
        promptBox = UIFactory.Bordered("Prompt", canvasRt, PanelFill, PanelBorder, 3, 22);
        UIFactory.Place(promptBox.Root, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 110f), new Vector2(640f, 96f));

        var cap = UIFactory.Bordered("KeyCap", promptBox.Inner, new Color32(30, 36, 66, 255), Gold, 3, 12);
        UIFactory.Place(cap.Root, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(22f, 0f), new Vector2(60f, 60f));
        var capText = UIFactory.Label("E", cap.Inner, "E", 38, Gold, TextAnchor.MiddleCenter);
        UIFactory.Stretch(capText.rectTransform);

        promptLabel = UIFactory.Label("Label", promptBox.Inner, "", 32, TextMain);
        promptLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
        var lrt = promptLabel.rectTransform;
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = new Vector2(100f, 0f);
        lrt.offsetMax = new Vector2(-16f, 0f);

        promptGroup = promptBox.Root.gameObject.AddComponent<CanvasGroup>();
        promptGroup.alpha = 0f;
        promptGroup.blocksRaycasts = false;
        promptGroup.interactable = false;
    }

    private void UpdatePrompt(bool hide)
    {
        var active = hide ? null : ProximityInteractable.Active;
        if (active != shownPrompt)
        {
            shownPrompt = active;
            if (active != null)
                promptLabel.text = active.PromptText + "  <size=22><color=#96A0B9>/ klik</color></size>";
        }
        float target = active != null ? 1f : 0f;
        promptGroup.alpha = Mathf.MoveTowards(promptGroup.alpha, target, Time.deltaTime * 8f);
    }

    // ------------------------------------------------------------------ toast

    private void BuildToast()
    {
        toastBox = UIFactory.Bordered("Toast", canvasRt, PanelFill, Gold, 3, 22);
        UIFactory.Place(toastBox.Root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(ToastX, -48f), new Vector2(900f, 84f));

        toastLabel = UIFactory.Label("Label", toastBox.Inner, "", 30, TextMain, TextAnchor.MiddleCenter);
        UIFactory.Stretch(toastLabel.rectTransform, 10f);

        toastGroup = toastBox.Root.gameObject.AddComponent<CanvasGroup>();
        toastGroup.alpha = 0f;
        toastGroup.blocksRaycasts = false;
        toastGroup.interactable = false;
    }

    public void ShowToast(string message, bool isError = false, float seconds = 2.4f)
    {
        toastLabel.text = message;
        toastBox.Border.color = isError ? Red : Gold;
        if (toastRoutine != null) StopCoroutine(toastRoutine);
        toastRoutine = StartCoroutine(ToastRoutine(seconds));
    }

    private IEnumerator ToastRoutine(float seconds)
    {
        var rt = toastBox.Root;
        float t = 0f;
        while (t < 0.15f)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / 0.15f);
            toastGroup.alpha = k;
            rt.anchoredPosition = new Vector2(ToastX, Mathf.Lerp(-30f, -48f, k));
            yield return null;
        }
        toastGroup.alpha = 1f;
        rt.anchoredPosition = new Vector2(ToastX, -48f);

        yield return new WaitForSeconds(seconds);

        t = 0f;
        while (t < 0.4f)
        {
            t += Time.deltaTime;
            toastGroup.alpha = 1f - Mathf.Clamp01(t / 0.4f);
            yield return null;
        }
        toastGroup.alpha = 0f;
        toastRoutine = null;
    }

    // ------------------------------------------------------------------ catatan papan tulis

    private void BuildNote()
    {
        noteRoot = UIFactory.NewRect("BoardNote", canvasRt);
        UIFactory.Stretch(noteRoot);
        var dim = noteRoot.gameObject.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.65f);
        dim.raycastTarget = true;

        // Papan tulis hijau dengan bingkai kayu
        noteFrame = UIFactory.Bordered("Frame", noteRoot, new Color32(27, 56, 45, 255),
            new Color32(104, 72, 42, 255), 16, 26, true);
        UIFactory.PlaceCenter(noteFrame.Root, 0, 0, 1040, 520);
        var inner = noteFrame.Inner;
        var chalk = new Color32(235, 240, 235, 255);
        var chalkDim = new Color32(170, 195, 182, 255);

        var title = UIFactory.Label("Title", inner, "CATATAN DI PAPAN TULIS", 28, chalkDim, TextAnchor.MiddleCenter);
        noteTitle = title;
        UIFactory.PlaceCenter(title.rectTransform, 0, 190, 900, 40);

        var body = UIFactory.Label("Body", inner, "Di sudut papan tulis terdapat catatan kecil:", 40, chalk,
            TextAnchor.MiddleCenter, FontStyle.Normal);
        noteBody = body;
        UIFactory.PlaceCenter(body.rectTransform, 0, 70, 900, 90);

        noteCode = UIFactory.Label("Code", inner, "", 64, chalk, TextAnchor.MiddleCenter);
        UIFactory.PlaceCenter(noteCode.rectTransform, 0, -40, 940, 100);

        var underline = UIFactory.Box("Underline", inner, new Color32(255, 196, 61, 200), 2);
        UIFactory.PlaceCenter(underline.rectTransform, 0, -98, 700, 4);

        var close = UIFactory.MakeButton("Close", inner, "TUTUP", 30, new Color32(46, 84, 68, 255),
            new Color32(170, 195, 182, 255), Color.white, CloseNote, 16, 4);
        UIFactory.PlaceCenter((RectTransform)close.transform, 0, -165, 260, 70);

        var hint = UIFactory.Label("Hint", inner, "[E] atau [Esc] untuk menutup", 22, chalkDim,
            TextAnchor.MiddleCenter, FontStyle.Normal);
        UIFactory.PlaceCenter(hint.rectTransform, 0, -222, 900, 30);

        noteRoot.gameObject.SetActive(false);
    }

    public void ShowBoardNote(string code)
    {
        noteTitle.text = "CATATAN DI PAPAN TULIS";
        noteBody.text = "Di sudut papan tulis terdapat catatan kecil:";
        noteCode.text = "Kode Laci Meja Guru: <color=#FFC43D>" + code + "</color>";
        noteRoot.gameObject.SetActive(true);
        noteOpen = true;
        PlayerFreeze.Set(true);
        StartCoroutine(UIFactory.Pop(noteFrame.Root));
    }

    private void CloseNote()
    {
        if (!noteOpen) return;
        noteOpen = false;
        noteRoot.gameObject.SetActive(false);
        PlayerFreeze.Set(false);
    }

    private void HandleNoteInput()
    {
        if (!noteOpen) return;
        var kb = Keyboard.current;
        if (kb == null) return;
        if (kb.eKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame ||
            kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)
            CloseNote();
    }

    // ------------------------------------------------------------------ keypad

    private void BuildKeypad()
    {
        var rt = UIFactory.NewRect("Keypad", canvasRt);
        keypad = rt.gameObject.AddComponent<KeypadPanel>();
        keypad.Build();
        rt.gameObject.SetActive(false);
    }

    public void OpenKeypad(string header, string note, Func<string, bool> validate, Action onSolved,
        int codeLength = KeypadPanel.DefaultLength)
    {
        if (keypad == null || ModalOpen) return;
        keypad.Open(header, note, validate, onSolved, codeLength);
    }

    // ------------------------------------------------------------------ transisi scene

    private void BuildFade()
    {
        fade = UIFactory.Box("Fade", canvasRt, new Color(0f, 0f, 0f, 0f), 0, false);
        UIFactory.Stretch(fade.rectTransform);
    }

    public void TransitionToScene(string sceneName, float delay = 1.2f)
    {
        if (transitioning) return;
        StartCoroutine(TransitionRoutine(sceneName, delay));
    }

    private IEnumerator TransitionRoutine(string sceneName, float delay)
    {
        transitioning = true;
        PlayerFreeze.Set(true);

        yield return new WaitForSeconds(delay);
        yield return Fade(1f, 0.5f);

        var op = SceneManager.LoadSceneAsync(sceneName);
        while (op != null && !op.isDone) yield return null;
        yield return null;

        PlayerFreeze.Set(false);
        yield return Fade(0f, 0.6f);
        transitioning = false;
    }

    private IEnumerator Fade(float target, float duration)
    {
        float start = fade.color.a;
        float t = 0f;
        fade.raycastTarget = true;
        while (t < duration)
        {
            t += Time.deltaTime;
            SetFadeAlpha(Mathf.Lerp(start, target, Mathf.Clamp01(t / duration)));
            yield return null;
        }
        SetFadeAlpha(target);
        fade.raycastTarget = target > 0.01f;
    }

    private void SetFadeAlpha(float a)
    {
        var c = fade.color;
        c.a = a;
        fade.color = c;
    }
}

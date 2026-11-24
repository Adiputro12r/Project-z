using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Mengatur scene Intro: menampilkan urutan gambar Prolog (Adegan 1-7) dilanjutkan Bagian 2.1-2.6.
/// Tombol "Lanjutkan >" di kanan bawah untuk langsung skip ke gambar berikutnya;
/// jika tidak ditekan, otomatis lanjut setelah 7 detik.
/// Setelah gambar terakhir selesai, otomatis masuk ke SampleScene.
/// </summary>
public class IntroController : MonoBehaviour
{
    [Header("Daftar Gambar Intro (Urutan Tampil)")]
    [Tooltip("Urutan: Adegan 1..7 lalu bagian 2.1..2.6")]
    public Sprite[] slides;

    [Header("Pengaturan")]
    [Tooltip("Durasi otomatis lanjut ke gambar berikutnya (detik).")]
    public float autoAdvanceSeconds = 7f;
    [Tooltip("Durasi fade transisi antar gambar (detik).")]
    public float fadeDuration = 0.35f;
    [Tooltip("Nama scene tujuan setelah semua gambar intro selesai.")]
    public string nextSceneName = "SampleScene";

    private static readonly Color BgNavy = new Color32(18, 28, 47, 255);
    private static readonly Color BtnRed = new Color32(235, 38, 42, 255);
    private static readonly Color BtnRedHover = new Color32(252, 62, 66, 255);
    private static readonly Color BtnRedPress = new Color32(190, 26, 30, 255);
    private static readonly Color TimerBarColor = new Color32(255, 220, 120, 230);

    private Image slideImage;
    private AspectRatioFitter aspectFitter;
    private Image fadeOverlay;
    private Button nextButton;
    private Text nextButtonLabel;
    private Image timerFillBar;
    private Text countdownLabel;

    private int currentIndex;
    private float elapsed;
    private bool isTransitioning;

    private static Sprite roundedSprite;
    private static Sprite whiteSprite;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        roundedSprite = null;
        whiteSprite = null;
    }

    private void Start()
    {
        PlayerFreeze.Set(false);
        CorridorState.Reset();

        EnsureEventSystem();
        BuildUI();

        if (slides == null || slides.Length == 0)
        {
            Debug.LogWarning("[IntroController] Daftar slides kosong, langsung masuk ke " + nextSceneName);
            LoadNextSceneImmediate();
            return;
        }

        currentIndex = 0;
        ShowSlide(currentIndex);
        StartCoroutine(FadeOverlay(1f, 0f, fadeDuration));
    }

    private void Update()
    {
        if (isTransitioning || slides == null || slides.Length == 0) return;

        // Shortcut keyboard: Space / Enter / Panah Kanan untuk lanjut
        var kb = Keyboard.current;
        if (kb != null && (kb.spaceKey.wasPressedThisFrame ||
                           kb.enterKey.wasPressedThisFrame ||
                           kb.numpadEnterKey.wasPressedThisFrame ||
                           kb.rightArrowKey.wasPressedThisFrame))
        {
            AdvanceSlide();
            return;
        }

        elapsed += Time.deltaTime;
        float remaining = Mathf.Max(0f, autoAdvanceSeconds - elapsed);

        if (timerFillBar != null)
        {
            timerFillBar.fillAmount = autoAdvanceSeconds > 0f
                ? Mathf.Clamp01(remaining / autoAdvanceSeconds)
                : 0f;
        }

        if (countdownLabel != null)
        {
            int sec = Mathf.CeilToInt(remaining);
            countdownLabel.text = "Otomatis lanjut dalam " + sec + " detik";
        }

        if (elapsed >= autoAdvanceSeconds)
        {
            AdvanceSlide();
        }
    }

    public void AdvanceSlide()
    {
        if (isTransitioning) return;
        StartCoroutine(AdvanceRoutine());
    }

    private IEnumerator AdvanceRoutine()
    {
        isTransitioning = true;
        if (nextButton != null) nextButton.interactable = false;

        int nextIdx = currentIndex + 1;
        if (nextIdx >= slides.Length)
        {
            // Selesai semua slide -> fade ke hitam lalu masuk SampleScene
            yield return FadeOverlay(0f, 1f, 0.5f);
            LoadNextSceneImmediate();
            yield break;
        }

        // Fade out singkat -> ganti gambar -> fade in
        yield return FadeOverlay(0f, 1f, fadeDuration * 0.6f);
        currentIndex = nextIdx;
        ShowSlide(currentIndex);
        yield return FadeOverlay(1f, 0f, fadeDuration * 0.7f);

        if (nextButton != null) nextButton.interactable = true;
        isTransitioning = false;
    }

    private void ShowSlide(int index)
    {
        elapsed = 0f;
        Sprite sp = slides[index];
        slideImage.sprite = sp;

        if (sp != null && aspectFitter != null && sp.rect.height > 0f)
        {
            aspectFitter.aspectRatio = sp.rect.width / sp.rect.height;
        }

        if (nextButtonLabel != null)
        {
            bool isLast = index >= slides.Length - 1;
            bool isEndOfProlog = index == 6; // Adegan 7 -> transisi ke Bagian 2
            if (isLast)
                nextButtonLabel.text = "Mulai Bertahan Hidup  >";
            else if (isEndOfProlog)
                nextButtonLabel.text = "Masuk ke Sekolah  >";
            else
                nextButtonLabel.text = "Lanjutkan  >";
        }

        if (timerFillBar != null) timerFillBar.fillAmount = 1f;
        if (countdownLabel != null)
            countdownLabel.text = "Otomatis lanjut dalam " + Mathf.CeilToInt(autoAdvanceSeconds) + " detik";
    }

    private void LoadNextSceneImmediate()
    {
        if (string.IsNullOrEmpty(nextSceneName)) return;
        SceneManager.LoadScene(nextSceneName);
    }

    private IEnumerator FadeOverlay(float from, float to, float duration)
    {
        if (fadeOverlay == null) yield break;
        fadeOverlay.raycastTarget = true;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(from, to, Mathf.Clamp01(t / Mathf.Max(0.001f, duration)));
            SetAlpha(fadeOverlay, a);
            yield return null;
        }
        SetAlpha(fadeOverlay, to);
        fadeOverlay.raycastTarget = to > 0.01f;
    }

    private static void SetAlpha(Image img, float a)
    {
        var c = img.color;
        c.a = a;
        img.color = c;
    }

    // ------------------------------------------------------------------ UI Builder

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;
        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        var module = go.AddComponent<InputSystemUIInputModule>();
        module.AssignDefaultActions();
    }

    private void BuildUI()
    {
        var canvasGo = new GameObject("IntroCanvas");
        canvasGo.layer = LayerMask.NameToLayer("UI");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 300;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();
        var root = (RectTransform)canvasGo.transform;

        // Latar gelap navy agar menyatu mulus dengan tepi gambar
        var bg = MakeImage("Background", root, BgNavy);
        Stretch(bg.rectTransform);
        bg.raycastTarget = false;

        // Wadah gambar intro (proporsi gambar terjaga penuh di tengah layar)
        var slideGo = new GameObject("SlideImage", typeof(RectTransform));
        slideGo.transform.SetParent(root, false);
        slideImage = slideGo.AddComponent<Image>();
        slideImage.color = Color.white;
        slideImage.preserveAspect = false;
        slideImage.raycastTarget = false;

        var slideRt = slideImage.rectTransform;
        Stretch(slideRt);

        aspectFitter = slideGo.AddComponent<AspectRatioFitter>();
        aspectFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        aspectFitter.aspectRatio = 1491f / 1055f;

        // Penutup halus area tombol statis bawaan di gambar agar tidak dobel
        var maskPatch = MakeImage("ButtonAreaCover", slideRt, BgNavy);
        maskPatch.rectTransform.anchorMin = new Vector2(0.70f, 0.025f);
        maskPatch.rectTransform.anchorMax = new Vector2(0.975f, 0.145f);
        maskPatch.rectTransform.offsetMin = Vector2.zero;
        maskPatch.rectTransform.offsetMax = Vector2.zero;
        maskPatch.raycastTarget = false;

        Font font = ResolveFont();

        // Teks info auto-skip di kiri bawah slide
        var countGo = new GameObject("CountdownText", typeof(RectTransform));
        countGo.transform.SetParent(slideRt, false);
        countdownLabel = countGo.AddComponent<Text>();
        countdownLabel.font = font;
        countdownLabel.fontSize = 22;
        countdownLabel.fontStyle = FontStyle.Normal;
        countdownLabel.color = new Color32(155, 170, 195, 220);
        countdownLabel.alignment = TextAnchor.MiddleLeft;
        countdownLabel.raycastTarget = false;
        var crt = countdownLabel.rectTransform;
        crt.anchorMin = new Vector2(0.045f, 0.04f);
        crt.anchorMax = new Vector2(0.65f, 0.12f);
        crt.offsetMin = Vector2.zero;
        crt.offsetMax = Vector2.zero;

        // Tombol "Lanjutkan >" interaktif di kanan bawah
        var btnGo = new GameObject("NextButton", typeof(RectTransform));
        btnGo.transform.SetParent(slideRt, false);
        var btnImg = btnGo.AddComponent<Image>();
        btnImg.sprite = GetRoundedSprite();
        btnImg.type = Image.Type.Sliced;
        btnImg.color = Color.white;

        var btnRt = btnImg.rectTransform;
        btnRt.anchorMin = new Vector2(0.715f, 0.04f);
        btnRt.anchorMax = new Vector2(0.96f, 0.125f);
        btnRt.offsetMin = Vector2.zero;
        btnRt.offsetMax = Vector2.zero;

        nextButton = btnGo.AddComponent<Button>();
        nextButton.targetGraphic = btnImg;
        var colors = nextButton.colors;
        colors.normalColor = BtnRed;
        colors.highlightedColor = BtnRedHover;
        colors.pressedColor = BtnRedPress;
        colors.selectedColor = BtnRedHover;
        colors.disabledColor = new Color32(140, 40, 44, 180);
        colors.fadeDuration = 0.08f;
        nextButton.colors = colors;
        nextButton.navigation = new Navigation { mode = Navigation.Mode.None };
        nextButton.onClick.AddListener(AdvanceSlide);

        // Label tombol
        var labelGo = new GameObject("Label", typeof(RectTransform));
        labelGo.transform.SetParent(btnRt, false);
        nextButtonLabel = labelGo.AddComponent<Text>();
        nextButtonLabel.font = font;
        nextButtonLabel.fontSize = 28;
        nextButtonLabel.fontStyle = FontStyle.Bold;
        nextButtonLabel.color = Color.white;
        nextButtonLabel.alignment = TextAnchor.MiddleCenter;
        nextButtonLabel.text = "Lanjutkan  >";
        nextButtonLabel.raycastTarget = false;
        var lrt = nextButtonLabel.rectTransform;
        lrt.anchorMin = new Vector2(0f, 0.12f);
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = new Vector2(12f, 0f);
        lrt.offsetMax = new Vector2(-12f, 0f);

        // Bar timer 7 detik di bagian bawah tombol
        var barBg = MakeImage("TimerBarBg", btnRt, new Color(0f, 0f, 0f, 0.28f));
        barBg.rectTransform.anchorMin = new Vector2(0.06f, 0.08f);
        barBg.rectTransform.anchorMax = new Vector2(0.94f, 0.16f);
        barBg.rectTransform.offsetMin = Vector2.zero;
        barBg.rectTransform.offsetMax = Vector2.zero;
        barBg.raycastTarget = false;

        timerFillBar = MakeImage("TimerFill", barBg.rectTransform, TimerBarColor);
        timerFillBar.sprite = GetWhiteSprite();
        timerFillBar.type = Image.Type.Filled;
        timerFillBar.fillMethod = Image.FillMethod.Horizontal;
        timerFillBar.fillOrigin = 0;
        timerFillBar.fillAmount = 1f;
        timerFillBar.raycastTarget = false;
        Stretch(timerFillBar.rectTransform);

        // Overlay fade hitam untuk transisi halus
        fadeOverlay = MakeImage("FadeOverlay", root, Color.black);
        Stretch(fadeOverlay.rectTransform);
        fadeOverlay.raycastTarget = false;
    }

    private static Font ResolveFont()
    {
        var f = Font.CreateDynamicFontFromOSFont(
            new[] { "Segoe UI", "Arial", "Helvetica", "Roboto" }, 28);
        if (f != null) return f;
        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    private static Image MakeImage(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        return img;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static Sprite GetWhiteSprite()
    {
        if (whiteSprite != null) return whiteSprite;
        var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
        var px = new Color32[16];
        for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, 255);
        tex.SetPixels32(px);
        tex.Apply();
        whiteSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100f);
        whiteSprite.hideFlags = HideFlags.HideAndDontSave;
        return whiteSprite;
    }

    private static Sprite GetRoundedSprite()
    {
        if (roundedSprite != null) return roundedSprite;
        const int size = 64;
        const float r = 16f;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };
        var px = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(0f, Mathf.Max(r - x, x - (size - 1 - r)));
                float dy = Mathf.Max(0f, Mathf.Max(r - y, y - (size - 1 - r)));
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01(r - dist + 0.5f);
                px[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
            }
        }
        tex.SetPixels32(px);
        tex.Apply();
        roundedSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
            SpriteMeshType.FullRect, new Vector4(r, r, r, r));
        roundedSprite.hideFlags = HideFlags.HideAndDontSave;
        return roundedSprite;
    }
}

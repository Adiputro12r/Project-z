using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Panel keypad kode laci (gaya "KODE DARURAT": header merah, kertas petunjuk, kotak digit, keypad).
/// Dibuat lewat kode oleh GameUI. Bisa dipakai mouse atau keyboard (0-9, Backspace, Enter, Esc).
/// </summary>
public class KeypadPanel : MonoBehaviour
{
    public const int DefaultLength = 3;
    public const int MaxLength = 4;

    /// <summary>Panjang kode keypad yang sedang dibuka (kembali ke 3 saat ditutup).</summary>
    public static int CodeLength { get; private set; } = DefaultLength;

    public bool IsOpen { get; private set; }

    private static readonly Key[] DigitKeys =
    {
        Key.Digit0, Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4,
        Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9
    };
    private static readonly Key[] PadKeys =
    {
        Key.Numpad0, Key.Numpad1, Key.Numpad2, Key.Numpad3, Key.Numpad4,
        Key.Numpad5, Key.Numpad6, Key.Numpad7, Key.Numpad8, Key.Numpad9
    };

    private static readonly Color BoxBorder = new Color32(220, 200, 240, 255);
    private static readonly Color BoxFill = new Color32(22, 12, 30, 255);
    private static readonly Color Gold = new Color32(255, 214, 102, 255);
    private static readonly Color Green = new Color32(46, 213, 115, 255);
    private static readonly Color Red = new Color32(255, 82, 96, 255);
    private static readonly Color Dim = new Color32(170, 160, 195, 255);

    private UIFactory.BorderedBox panelBox;
    private Text headerText;
    private Text subtitleText;
    private Text noteBody;
    private Text feedback;
    private UIFactory.BorderedBox[] digitBoxes;
    private Text[] digitTexts;

    private string buffer = "";
    private bool busy;
    private Func<string, bool> validate;
    private Action onSolved;
    private Coroutine shakeRoutine;

    // ------------------------------------------------------------------ build

    public void Build()
    {
        var rt = (RectTransform)transform;
        UIFactory.Stretch(rt);

        var dim = UIFactory.Box("Dim", rt, new Color(0f, 0f, 0f, 0.7f), 0, true);
        UIFactory.Stretch(dim.rectTransform);

        panelBox = UIFactory.Bordered("Panel", rt, new Color32(33, 24, 50, 255),
            new Color32(107, 74, 120, 255), 10, 30, true);
        UIFactory.PlaceCenter(panelBox.Root, 0, 0, 1000, 800);
        var inner = panelBox.Inner; // 980 x 780

        // Header merah
        var banner = UIFactory.Bordered("Banner", inner, new Color32(165, 18, 31, 255),
            new Color32(70, 8, 16, 255), 6, 16);
        UIFactory.PlaceTL(banner.Root, 140, -24, 700, 96);
        headerText = UIFactory.Label("Title", banner.Inner, "LACI MEJA GURU", 54, Color.white, TextAnchor.MiddleCenter);
        UIFactory.Stretch(headerText.rectTransform);
        headerText.rectTransform.offsetMin = new Vector2(56, 0);

        // Lencana peringatan
        var badge = UIFactory.Bordered("Badge", inner, new Color32(120, 10, 20, 255),
            new Color32(255, 90, 100, 255), 5, 26);
        UIFactory.PlaceTL(badge.Root, 76, -24, 96, 96);
        var bang = UIFactory.Label("Bang", badge.Inner, "!", 72, Color.white, TextAnchor.MiddleCenter);
        UIFactory.Stretch(bang.rectTransform);

        // Tombol tutup
        var close = UIFactory.MakeButton("Close", inner, "X", 46, new Color32(70, 10, 22, 255),
            new Color32(255, 90, 100, 255), Color.white, Close, 14, 5);
        UIFactory.PlaceTL((RectTransform)close.transform, 888, -22, 74, 74);

        // Subjudul + garis merah
        subtitleText = UIFactory.Label("Subtitle", inner, "MASUKKAN " + CodeLength + " DIGIT KODE", 44,
            Color.white, TextAnchor.MiddleCenter);
        UIFactory.PlaceTL(subtitleText.rectTransform, 190, -140, 600, 60);
        var lineL = UIFactory.Box("LineL", inner, new Color32(255, 30, 40, 255));
        UIFactory.PlaceTL(lineL.rectTransform, 24, -166, 164, 6);
        var lineR = UIFactory.Box("LineR", inner, new Color32(255, 30, 40, 255));
        UIFactory.PlaceTL(lineR.rectTransform, 792, -166, 164, 6);

        BuildNote(inner);
        BuildDigitBoxes(inner);

        feedback = UIFactory.Label("Feedback", inner, "", 28, Dim, TextAnchor.MiddleCenter);
        UIFactory.PlaceTL(feedback.rectTransform, 413, -336, 534, 38);

        BuildKeypad(inner);
    }

    private void BuildNote(RectTransform inner)
    {
        var paper = UIFactory.Bordered("Note", inner, new Color32(243, 217, 211, 255),
            new Color32(205, 165, 160, 255), 3, 8);
        UIFactory.PlaceTL(paper.Root, 44, -236, 340, 360);
        paper.Root.localRotation = Quaternion.Euler(0f, 0f, 4f);

        var title = UIFactory.Label("Title", paper.Inner, "PETUNJUK", 46, new Color32(110, 10, 20, 255),
            TextAnchor.MiddleCenter);
        UIFactory.PlaceTL(title.rectTransform, 10, -22, 314, 64);
        var underline = UIFactory.Box("Underline", paper.Inner, new Color32(110, 10, 20, 255));
        UIFactory.PlaceTL(underline.rectTransform, 60, -88, 214, 5);

        noteBody = UIFactory.Label("Body", paper.Inner, "", 28, new Color32(80, 38, 38, 255),
            TextAnchor.UpperLeft, FontStyle.Normal);
        UIFactory.PlaceTL(noteBody.rectTransform, 18, -110, 298, 230);

        // Selotip
        for (int i = 0; i < 2; i++)
        {
            var tape = UIFactory.Box("Tape" + i, paper.Root, new Color32(205, 205, 210, 150));
            var pos = i == 0 ? new Vector2(22, -14) : new Vector2(318, -346);
            UIFactory.Place(tape.rectTransform, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), pos, new Vector2(120, 38));
            tape.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 40f);
        }
    }

    private void BuildDigitBoxes(RectTransform inner)
    {
        digitBoxes = new UIFactory.BorderedBox[MaxLength];
        digitTexts = new Text[MaxLength];

        const float w = 150f, h = 110f, gap = 20f;
        for (int i = 0; i < MaxLength; i++)
        {
            var box = UIFactory.Bordered("Digit" + i, inner, BoxFill, BoxBorder, 4, 10);
            UIFactory.PlaceTL(box.Root, 380f + i * (w + gap), -215, w, h);
            var t = UIFactory.Label("Text", box.Inner, "", 82, Color.white, TextAnchor.MiddleCenter);
            UIFactory.Stretch(t.rectTransform);
            digitBoxes[i] = box;
            digitTexts[i] = t;
        }
    }

    /// <summary>Atur jumlah & posisi kotak digit sesuai panjang kode (3 atau 4).</summary>
    private void LayoutBoxes(int len)
    {
        float w = len >= 4 ? 130f : 150f;
        float gap = len >= 4 ? 14f : 20f;
        float total = len * w + (len - 1) * gap;
        float startX = 680f - total * 0.5f;
        for (int i = 0; i < MaxLength; i++)
        {
            bool on = i < len;
            digitBoxes[i].Root.gameObject.SetActive(on);
            if (on) UIFactory.PlaceTL(digitBoxes[i].Root, startX + i * (w + gap), -215, w, 110f);
        }
    }

    private void BuildKeypad(RectTransform inner)
    {
        const float w = 170f, h = 84f, gap = 12f;
        const float x0 = 413f, y0 = -380f;

        var keyFill = new Color32(74, 58, 106, 255);
        var keyBorder = new Color32(20, 12, 28, 255);

        for (int n = 1; n <= 9; n++)
        {
            int digit = n;
            int row = (n - 1) / 3, col = (n - 1) % 3;
            var b = UIFactory.MakeButton("Key" + n, inner, n.ToString(), 58, keyFill, keyBorder, Color.white,
                () => AddDigit((char)('0' + digit)));
            UIFactory.PlaceTL((RectTransform)b.transform, x0 + col * (w + gap), y0 - row * (h + gap), w, h);
        }

        var back = UIFactory.MakeButton("KeyBack", inner, "\u2190", 58, keyFill, keyBorder, Color.white, Backspace);
        UIFactory.PlaceTL((RectTransform)back.transform, x0, y0 - 3 * (h + gap), w, h);

        var zero = UIFactory.MakeButton("Key0", inner, "0", 58, keyFill, keyBorder, Color.white,
            () => AddDigit('0'));
        UIFactory.PlaceTL((RectTransform)zero.transform, x0 + (w + gap), y0 - 3 * (h + gap), w, h);

        var ok = UIFactory.MakeButton("KeyConfirm", inner, "KONFIRMASI", 24, new Color32(165, 18, 31, 255),
            new Color32(50, 8, 14, 255), Color.white, Submit);
        UIFactory.PlaceTL((RectTransform)ok.transform, x0 + 2 * (w + gap), y0 - 3 * (h + gap), w, h);
    }

    // ------------------------------------------------------------------ open / close

    public void Open(string header, string note, Func<string, bool> validator, Action solved, int codeLength = DefaultLength)
    {
        CodeLength = Mathf.Clamp(codeLength, 1, MaxLength);
        subtitleText.text = "MASUKKAN " + CodeLength + " DIGIT KODE";
        LayoutBoxes(CodeLength);
        headerText.text = header;
        noteBody.text = note;
        validate = validator;
        onSolved = solved;

        buffer = "";
        busy = false;
        SetFeedback("Gembok " + CodeLength + " digit. Masukkan kodenya.", Dim);
        Refresh();
        ResetBoxColors();

        gameObject.SetActive(true);
        IsOpen = true;
        PlayerFreeze.Set(true);
        StartCoroutine(UIFactory.Pop(panelBox.Root));
    }

    public void Close()
    {
        if (!IsOpen) return;
        IsOpen = false;
        CodeLength = DefaultLength;
        panelBox.Root.anchoredPosition = Vector2.zero;
        panelBox.Root.localScale = Vector3.one;
        gameObject.SetActive(false); // sekaligus menghentikan coroutine panel ini
        PlayerFreeze.Set(false);
    }

    // ------------------------------------------------------------------ input

    private void Update()
    {
        if (!IsOpen || busy) return;
        var kb = Keyboard.current;
        if (kb == null) return;

        for (int d = 0; d < 10; d++)
        {
            if (kb[DigitKeys[d]].wasPressedThisFrame || kb[PadKeys[d]].wasPressedThisFrame)
                AddDigit((char)('0' + d));
        }
        if (kb.backspaceKey.wasPressedThisFrame) Backspace();
        if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame) Submit();
        if (kb.escapeKey.wasPressedThisFrame) Close();
    }

    private void AddDigit(char c)
    {
        if (!IsOpen || busy || buffer.Length >= CodeLength) return;
        buffer += c;
        Refresh();
    }

    private void Backspace()
    {
        if (!IsOpen || busy || buffer.Length == 0) return;
        buffer = buffer.Substring(0, buffer.Length - 1);
        Refresh();
    }

    private void Submit()
    {
        if (!IsOpen || busy) return;

        if (buffer.Length < CodeLength)
        {
            SetFeedback("Masukkan " + CodeLength + " digit kode.", Gold);
            Shake();
            return;
        }
        StartCoroutine(SubmitRoutine());
    }

    private IEnumerator SubmitRoutine()
    {
        busy = true;
        bool ok = validate != null && validate(buffer);

        if (ok)
        {
            TintBoxes(new Color32(16, 70, 40, 255), Green);
            SetFeedback("Gembok terbuka!", Green);
            yield return new WaitForSeconds(0.7f);

            var callback = onSolved;
            if (callback != null) callback();
            Close();
            yield break;
        }

        TintBoxes(new Color32(80, 12, 20, 255), Red);
        SetFeedback("Kode salah!", Red);
        Shake();
        yield return new WaitForSeconds(0.6f);

        buffer = "";
        Refresh();
        ResetBoxColors();
        SetFeedback("Coba lagi.", Dim);
        busy = false;
    }

    // ------------------------------------------------------------------ visuals

    private void Refresh()
    {
        for (int i = 0; i < CodeLength; i++)
        {
            digitTexts[i].text = i < buffer.Length ? buffer[i].ToString() : "";
            digitBoxes[i].Border.color = i == buffer.Length ? Gold : BoxBorder; // kotak aktif disorot
        }
    }

    private void ResetBoxColors()
    {
        foreach (var box in digitBoxes) box.Fill.color = BoxFill;
        Refresh();
    }

    private void TintBoxes(Color fill, Color border)
    {
        foreach (var box in digitBoxes)
        {
            box.Fill.color = fill;
            box.Border.color = border;
        }
    }

    private void SetFeedback(string message, Color color)
    {
        feedback.text = message;
        feedback.color = color;
    }

    private void Shake()
    {
        if (shakeRoutine != null) StopCoroutine(shakeRoutine);
        shakeRoutine = StartCoroutine(ShakeRoutine());
    }

    private IEnumerator ShakeRoutine()
    {
        const float duration = 0.35f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = 1f - t / duration;
            panelBox.Root.anchoredPosition = new Vector2(Mathf.Sin(t * 70f) * 18f * k, 0f);
            yield return null;
        }
        panelBox.Root.anchoredPosition = Vector2.zero;
        shakeRoutine = null;
    }
}

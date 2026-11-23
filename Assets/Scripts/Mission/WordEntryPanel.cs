using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Panel kata sandi: player mengetik kata (huruf) dengan keyboard, Enter untuk konfirmasi, Esc untuk menutup.
/// Dipakai pintu darurat (jawaban = bahasa Inggris dari tulisan di meja). Dibuat lewat kode oleh GameUI.
/// </summary>
public class WordEntryPanel : MonoBehaviour
{
    public const int MaxLength = 12;

    public bool IsOpen { get; private set; }

    private static readonly Color BoxBorder = new Color32(220, 200, 240, 255);
    private static readonly Color BoxFill = new Color32(22, 12, 30, 255);
    private static readonly Color Gold = new Color32(255, 214, 102, 255);
    private static readonly Color Green = new Color32(46, 213, 115, 255);
    private static readonly Color Red = new Color32(255, 82, 96, 255);
    private static readonly Color Dim = new Color32(170, 160, 195, 255);

    private UIFactory.BorderedBox panelBox;
    private UIFactory.BorderedBox inputBox;
    private Text headerText;
    private Text noteText;
    private Text inputText;
    private Text feedback;

    private string buffer = "";
    private bool busy;
    private bool caretOn = true;
    private float caretTimer;
    private Func<string, bool> validate;
    private Action onSolved;
    private Coroutine shakeRoutine;
    private Keyboard subscribedKeyboard;

    // ------------------------------------------------------------------ build

    public void Build()
    {
        var rt = (RectTransform)transform;
        UIFactory.Stretch(rt);

        var dim = UIFactory.Box("Dim", rt, new Color(0f, 0f, 0f, 0.7f), 0, true);
        UIFactory.Stretch(dim.rectTransform);

        panelBox = UIFactory.Bordered("Panel", rt, new Color32(33, 24, 50, 255),
            new Color32(107, 74, 120, 255), 10, 30, true);
        UIFactory.PlaceCenter(panelBox.Root, 0, 0, 1000, 720);
        var inner = panelBox.Inner; // 980 x 700

        // Header merah
        var banner = UIFactory.Bordered("Banner", inner, new Color32(165, 18, 31, 255),
            new Color32(70, 8, 16, 255), 6, 16);
        UIFactory.PlaceTL(banner.Root, 140, -24, 700, 96);
        headerText = UIFactory.Label("Title", banner.Inner, "PINTU DARURAT", 54, Color.white, TextAnchor.MiddleCenter);
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

        // Subjudul
        var subtitle = UIFactory.Label("Subtitle", inner, "KETIK KATA SANDI (BAHASA INGGRIS)", 40,
            Color.white, TextAnchor.MiddleCenter);
        UIFactory.PlaceTL(subtitle.rectTransform, 60, -140, 860, 60);
        var line = UIFactory.Box("Line", inner, new Color32(255, 30, 40, 255));
        UIFactory.PlaceTL(line.rectTransform, 190, -204, 600, 6);

        // Petunjuk
        noteText = UIFactory.Label("Note", inner, "", 30, Dim, TextAnchor.MiddleCenter, FontStyle.Normal);
        UIFactory.PlaceTL(noteText.rectTransform, 60, -222, 860, 130);

        // Kotak input
        inputBox = UIFactory.Bordered("Input", inner, BoxFill, BoxBorder, 4, 12);
        UIFactory.PlaceTL(inputBox.Root, 190, -370, 600, 120);
        inputText = UIFactory.Label("Text", inputBox.Inner, "", 80, Color.white, TextAnchor.MiddleCenter);
        UIFactory.Stretch(inputText.rectTransform);

        feedback = UIFactory.Label("Feedback", inner, "", 30, Dim, TextAnchor.MiddleCenter);
        UIFactory.PlaceTL(feedback.rectTransform, 60, -508, 860, 44);

        var ok = UIFactory.MakeButton("Confirm", inner, "KONFIRMASI", 36, new Color32(165, 18, 31, 255),
            new Color32(50, 8, 14, 255), Color.white, Submit);
        UIFactory.PlaceTL((RectTransform)ok.transform, 340, -572, 300, 84);

        var hint = UIFactory.Label("Hint", inner, "[Enter] konfirmasi     [Esc] tutup", 24, Dim,
            TextAnchor.MiddleCenter, FontStyle.Normal);
        UIFactory.PlaceTL(hint.rectTransform, 60, -664, 860, 30);
    }

    // ------------------------------------------------------------------ open / close

    public void Open(string header, string note, Func<string, bool> validator, Action solved)
    {
        headerText.text = header;
        noteText.text = note;
        validate = validator;
        onSolved = solved;

        buffer = "";
        busy = false;
        caretOn = true;
        caretTimer = 0f;
        SetFeedback("Ketik jawabanmu lalu tekan Enter.", Dim);
        ResetBox();

        gameObject.SetActive(true);
        IsOpen = true;
        Subscribe();
        Refresh();
        PlayerFreeze.Set(true);
        StartCoroutine(UIFactory.Pop(panelBox.Root));
    }

    public void Close()
    {
        if (!IsOpen) return;
        IsOpen = false;
        Unsubscribe();
        panelBox.Root.anchoredPosition = Vector2.zero;
        panelBox.Root.localScale = Vector3.one;
        gameObject.SetActive(false);
        PlayerFreeze.Set(false);
    }

    private void OnDisable() => Unsubscribe();

    private void Subscribe()
    {
        if (subscribedKeyboard != null) return;
        var kb = Keyboard.current;
        if (kb == null) return;
        kb.onTextInput += OnTextInput;
        subscribedKeyboard = kb;
    }

    private void Unsubscribe()
    {
        if (subscribedKeyboard == null) return;
        subscribedKeyboard.onTextInput -= OnTextInput;
        subscribedKeyboard = null;
    }

    // ------------------------------------------------------------------ input

    private void OnTextInput(char c)
    {
        if (!IsOpen || busy) return;
        bool letter = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
        if (!letter || buffer.Length >= MaxLength) return;
        buffer += char.ToUpperInvariant(c);
        Refresh();
    }

    private void Update()
    {
        if (!IsOpen) return;

        caretTimer += Time.unscaledDeltaTime;
        if (caretTimer >= 0.5f)
        {
            caretTimer = 0f;
            caretOn = !caretOn;
            Refresh();
        }

        if (busy) return;
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.backspaceKey.wasPressedThisFrame && buffer.Length > 0)
        {
            buffer = buffer.Substring(0, buffer.Length - 1);
            Refresh();
        }
        if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame) Submit();
        if (kb.escapeKey.wasPressedThisFrame) Close();
    }

    private void Submit()
    {
        if (!IsOpen || busy) return;

        if (buffer.Length == 0)
        {
            SetFeedback("Ketik katanya dulu.", Gold);
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
            TintBox(new Color32(16, 70, 40, 255), Green);
            SetFeedback("Panel terbuka!", Green);
            Refresh();
            yield return new WaitForSeconds(0.7f);

            var callback = onSolved;
            if (callback != null) callback();
            Close();
            yield break;
        }

        TintBox(new Color32(80, 12, 20, 255), Red);
        SetFeedback("Salah! Coba terjemahkan lagi.", Red);
        Shake();
        Refresh();
        yield return new WaitForSeconds(0.7f);

        buffer = "";
        ResetBox();
        SetFeedback("Coba lagi.", Dim);
        busy = false;
        Refresh();
    }

    // ------------------------------------------------------------------ visuals

    private void Refresh()
    {
        string caret = buffer.Length >= MaxLength
            ? ""
            : (caretOn && !busy ? "<color=#FFD666>_</color>" : "<color=#FFFFFF00>_</color>");
        inputText.text = buffer + caret;
    }

    private void ResetBox()
    {
        inputBox.Fill.color = BoxFill;
        inputBox.Border.color = Gold;
    }

    private void TintBox(Color fill, Color border)
    {
        inputBox.Fill.color = fill;
        inputBox.Border.color = border;
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

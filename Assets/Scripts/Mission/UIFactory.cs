using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Helper kecil untuk membangun uGUI lewat kode (panel rounded, label, tombol).</summary>
public static class UIFactory
{
    public sealed class BorderedBox
    {
        public RectTransform Root;   // elemen luar (warna border)
        public RectTransform Inner;  // area isi (sudah diinset sebesar border)
        public Image Border;
        public Image Fill;
    }

    private static Font font;
    private static readonly Dictionary<int, Sprite> roundedCache = new Dictionary<int, Sprite>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        font = null;
        roundedCache.Clear();
    }

    public static Font DefaultFont
    {
        get
        {
            if (font == null)
            {
                font = Font.CreateDynamicFontFromOSFont(new[] { "Segoe UI", "Arial", "Helvetica" }, 32);
                if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
            return font;
        }
    }

    /// <summary>Sprite 9-slice dengan sudut membulat.</summary>
    public static Sprite Rounded(int radius)
    {
        Sprite cached;
        if (roundedCache.TryGetValue(radius, out cached) && cached != null) return cached;

        int size = radius * 2 + 2;
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
                float cx = Mathf.Clamp(x + 0.5f, radius, size - radius);
                float cy = Mathf.Clamp(y + 0.5f, radius, size - radius);
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                float a = Mathf.Clamp01(radius - d + 0.5f);
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        }
        tex.SetPixels32(px);
        tex.Apply();

        var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
            SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        sprite.hideFlags = HideFlags.HideAndDontSave;
        roundedCache[radius] = sprite;
        return sprite;
    }

    public static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        return rt;
    }

    public static void Stretch(RectTransform rt, float inset = 0f)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(inset, inset);
        rt.offsetMax = new Vector2(-inset, -inset);
    }

    public static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    /// <summary>Posisi relatif sudut kiri-atas parent (y negatif = turun).</summary>
    public static void PlaceTL(RectTransform rt, float x, float y, float w, float h) =>
        Place(rt, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(x, y), new Vector2(w, h));

    /// <summary>Posisi relatif tengah parent.</summary>
    public static void PlaceCenter(RectTransform rt, float x, float y, float w, float h) =>
        Place(rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x, y), new Vector2(w, h));

    private static void ApplyRounded(Image img, int radius)
    {
        if (radius <= 0) return;
        img.sprite = Rounded(radius);
        img.type = Image.Type.Sliced;
    }

    public static Image Box(string name, Transform parent, Color color, int radius = 0, bool raycast = false)
    {
        var rt = NewRect(name, parent);
        var img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = raycast;
        ApplyRounded(img, radius);
        return img;
    }

    public static BorderedBox Bordered(string name, Transform parent, Color fill, Color border,
        int borderPx, int radius, bool raycast = false)
    {
        var root = NewRect(name, parent);
        var b = root.gameObject.AddComponent<Image>();
        b.color = border;
        b.raycastTarget = raycast;
        ApplyRounded(b, radius);

        var inner = NewRect("Fill", root);
        Stretch(inner, borderPx);
        var f = inner.gameObject.AddComponent<Image>();
        f.color = fill;
        f.raycastTarget = false;
        ApplyRounded(f, Mathf.Max(0, radius - borderPx));

        return new BorderedBox { Root = root, Inner = inner, Border = b, Fill = f };
    }

    public static Text Label(string name, Transform parent, string text, int size, Color color,
        TextAnchor anchor = TextAnchor.MiddleLeft, FontStyle style = FontStyle.Bold)
    {
        var rt = NewRect(name, parent);
        var t = rt.gameObject.AddComponent<Text>();
        t.font = DefaultFont;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = anchor;
        t.fontStyle = style;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        t.supportRichText = true;
        return t;
    }

    public static Button MakeButton(string name, Transform parent, string label, int fontSize,
        Color fill, Color border, Color textColor, Action onClick, int radius = 14, int borderPx = 4)
    {
        var box = Bordered(name, parent, fill, border, borderPx, radius, true);
        var btn = box.Root.gameObject.AddComponent<Button>();
        btn.targetGraphic = box.Fill;

        var colors = btn.colors;
        colors.normalColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        colors.highlightedColor = Color.white;
        colors.pressedColor = new Color(0.65f, 0.65f, 0.65f, 1f);
        colors.selectedColor = colors.normalColor;
        colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);
        colors.fadeDuration = 0.05f;
        btn.colors = colors;

        // Cegah tombol "ter-select" lalu ikut ter-trigger oleh Enter/Space.
        btn.navigation = new Navigation { mode = Navigation.Mode.None };

        var text = Label("Label", box.Inner, label, fontSize, textColor, TextAnchor.MiddleCenter);
        Stretch(text.rectTransform);

        btn.onClick.AddListener(() =>
        {
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            if (onClick != null) onClick();
        });
        return btn;
    }

    /// <summary>Animasi "pop" singkat saat panel muncul.</summary>
    public static IEnumerator Pop(RectTransform rt, float duration = 0.16f, float from = 0.9f)
    {
        float t = 0f;
        while (t < duration && rt != null)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            k = 1f - (1f - k) * (1f - k);
            rt.localScale = Vector3.one * Mathf.Lerp(from, 1f, k);
            yield return null;
        }
        if (rt != null) rt.localScale = Vector3.one;
    }
}

/// <summary>Membekukan / melepas kontrol player (dipakai saat UI modal terbuka).</summary>
public static class PlayerFreeze
{
    public static void Set(bool frozen)
    {
        var pc = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
        if (pc == null) return;

        pc.enabled = !frozen;
        if (!frozen) return;

        var rb = pc.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        var anim = pc.GetComponent<Animator>();
        if (anim != null)
        {
            anim.SetBool("IsMoving", false);
            anim.SetFloat("Speed", 0f);
            anim.SetFloat("MoveX", 0f);
            anim.SetFloat("MoveY", 0f);
        }
    }
}

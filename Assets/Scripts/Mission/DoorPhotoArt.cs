using UnityEngine;

/// <summary>
/// "Foto" pintu darurat untuk papan pengumuman Misi 2. Pixel art digambar lewat kode (tanpa file gambar).
/// </summary>
public static class DoorPhotoArt
{
    public const int W = 80;
    public const int H = 104;

    private static Sprite sprite;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => sprite = null;

    public static Sprite GetSprite()
    {
        if (sprite != null) return sprite;

        var px = new Color32[W * H];

        // Tembok malam + garis bata
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                bool seam = y % 8 == 0 || ((x + (y / 8 % 2) * 6) % 12 == 0);
                px[y * W + x] = seam ? new Color32(34, 40, 68, 255) : new Color32(42, 49, 82, 255);
            }

        // Lantai
        Fill(px, 0, 0, W, 14, new Color32(24, 26, 44, 255));
        Fill(px, 0, 14, W, 1, new Color32(70, 76, 112, 255));

        // Kusen + daun pintu merah
        Fill(px, 15, 14, 50, 74, new Color32(20, 20, 30, 255));
        Fill(px, 18, 14, 44, 70, new Color32(170, 34, 44, 255));
        Fill(px, 18, 14, 2, 70, new Color32(208, 76, 82, 255));
        Fill(px, 60, 14, 2, 70, new Color32(120, 22, 30, 255));

        // Panel cekung
        Fill(px, 24, 20, 32, 22, new Color32(142, 26, 36, 255));
        Fill(px, 24, 20, 32, 1, new Color32(110, 18, 28, 255));
        Fill(px, 24, 50, 32, 4, new Color32(142, 26, 36, 255));

        // Jendela kecil
        Fill(px, 34, 62, 12, 16, new Color32(18, 24, 44, 255));
        Fill(px, 35, 63, 10, 14, new Color32(88, 122, 170, 255));
        Fill(px, 35, 63, 10, 3, new Color32(120, 156, 204, 255));

        // Palang dorong
        Fill(px, 20, 46, 40, 4, new Color32(176, 182, 194, 255));
        Fill(px, 20, 46, 40, 1, new Color32(108, 114, 130, 255));

        // Papan EXIT hijau
        Fill(px, 24, 90, 32, 11, new Color32(10, 50, 30, 255));
        Fill(px, 25, 91, 30, 9, new Color32(22, 120, 66, 255));
        DrawGlyph(px, 28, 93, new[] { "111", "100", "111", "100", "111" });   // E
        DrawGlyph(px, 33, 93, new[] { "101", "101", "010", "101", "101" });   // X
        DrawGlyph(px, 38, 93, new[] { "111", "010", "010", "010", "111" });   // I
        DrawGlyph(px, 43, 93, new[] { "111", "010", "010", "010", "010" });   // T
        Fill(px, 49, 95, 4, 1, new Color32(235, 245, 235, 255));              // panah kecil
        Fill(px, 51, 94, 1, 3, new Color32(235, 245, 235, 255));

        // Lampu alarm merah
        Fill(px, 67, 72, 7, 7, new Color32(120, 36, 40, 255));
        Fill(px, 68, 73, 5, 5, new Color32(255, 78, 66, 255));
        Fill(px, 69, 74, 2, 2, new Color32(255, 190, 170, 255));

        // Vinyet supaya terasa seperti foto
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float dx = (x - W * 0.5f) / (W * 0.5f);
                float dy = (y - H * 0.5f) / (H * 0.5f);
                float k = 1f - Mathf.Clamp01((dx * dx + dy * dy) - 0.55f) * 0.55f;
                var c = px[y * W + x];
                px[y * W + x] = new Color32((byte)(c.r * k), (byte)(c.g * k), (byte)(c.b * k), 255);
            }

        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };
        tex.SetPixels32(px);
        tex.Apply();

        sprite = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), 100f);
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }

    private static void Fill(Color32[] px, int x, int y, int w, int h, Color32 c)
    {
        for (int yy = y; yy < y + h; yy++)
            for (int xx = x; xx < x + w; xx++)
                if (xx >= 0 && yy >= 0 && xx < W && yy < H) px[yy * W + xx] = c;
    }

    /// <summary>Huruf bitmap 3x5. Baris pertama = baris paling atas; (x, y) = sudut kiri-bawah huruf.</summary>
    private static void DrawGlyph(Color32[] px, int x, int y, string[] rows)
    {
        var ink = new Color32(240, 248, 240, 255);
        for (int r = 0; r < rows.Length; r++)
            for (int c = 0; c < rows[r].Length; c++)
                if (rows[r][c] == '1') Fill(px, x + c, y + (rows.Length - 1 - r), 1, 1, ink);
    }
}

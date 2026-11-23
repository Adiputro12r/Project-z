using System.Collections;
using UnityEngine;

/// <summary>
/// Bel sekolah + panelnya di lorong. Sprite digambar lewat kode (tanpa file gambar).
/// Saat CorridorState.RingBell() dipanggil: lonceng berayun, bunyi, dan gelombang suara muncul.
/// Taruh di GameObject yang punya SpriteRenderer dan tepat satu Collider2D trigger.
/// Titik pivot objek = lantai di bawah panel (zombie berkumpul di sekitar titik ini).
/// </summary>
public class SchoolBell : MonoBehaviour
{
    private const float PixelsPerUnit = 64f;
    private const float BellHangY = 96f / PixelsPerUnit;   // tinggi tempat lonceng digantung

    private static Sprite panelSprite;
    private static Sprite bellSprite;
    private static Sprite ringSprite;
    private static AudioClip ringClip;

    private Transform bellBody;
    private AudioSource audioSrc;
    private Coroutine ringRoutine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        panelSprite = null;
        bellSprite = null;
        ringSprite = null;
        ringClip = null;
    }

    private void Awake()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = GetPanelSprite();
        sr.sortingOrder = 2;

        var bodyGo = new GameObject("BellBody");
        bodyGo.transform.SetParent(transform, false);
        bodyGo.transform.localPosition = new Vector3(0f, BellHangY, 0f);
        var bsr = bodyGo.AddComponent<SpriteRenderer>();
        bsr.sprite = GetBellSprite();
        bsr.sortingOrder = 3;
        bellBody = bodyGo.transform;

        audioSrc = gameObject.AddComponent<AudioSource>();
        audioSrc.playOnAwake = false;
        audioSrc.spatialBlend = 0f;
        audioSrc.volume = 0.8f;

        CorridorState.BellPosition = transform.position;
        CorridorState.HasBellPosition = true;
    }

    private void OnEnable() => CorridorState.BellRang += Ring;
    private void OnDisable() => CorridorState.BellRang -= Ring;

    private void Start()
    {
        // Balik ke lorong setelah bel dibunyikan: bel tetap berbunyi terus.
        if (CorridorState.BellActive) Ring();
    }

    private void Ring()
    {
        if (ringRoutine != null) StopCoroutine(ringRoutine);
        ringRoutine = StartCoroutine(RingRoutine());
        if (audioSrc != null)
        {
            audioSrc.clip = GetRingClip();
            audioSrc.loop = true;   // berbunyi terus
            audioSrc.Play();
        }
    }

    private IEnumerator RingRoutine()
    {
        float t = 0f;
        float nextWave = 0f;

        while (CorridorState.BellActive)
        {
            t += Time.deltaTime;
            bellBody.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 28f) * 22f);

            if (t >= nextWave)
            {
                nextWave = t + 0.42f;
                StartCoroutine(Wave());
            }
            yield return null;
        }
        bellBody.localRotation = Quaternion.identity;
        if (audioSrc != null) audioSrc.Stop();
        ringRoutine = null;
    }

    private IEnumerator Wave()
    {
        var go = new GameObject("BellWave");
        go.transform.position = transform.position + new Vector3(0f, BellHangY - 0.3f, 0f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetRingSprite();
        sr.sortingOrder = 4;

        const float life = 1.3f;
        float t = 0f;
        while (t < life)
        {
            t += Time.deltaTime;
            float k = t / life;
            go.transform.localScale = Vector3.one * Mathf.Lerp(0.6f, 5f, k);
            sr.color = new Color(1f, 0.85f, 0.35f, (1f - k) * 0.7f);
            yield return null;
        }
        Destroy(go);
    }

    // ------------------------------------------------------------------ gambar sprite lewat kode

    private static readonly Color32 Clear = new Color32(0, 0, 0, 0);

    private static void Fill(Color32[] px, int w, int x0, int y0, int x1, int y1, Color32 c)
    {
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
                px[y * w + x] = c;
    }

    private static Sprite MakeSprite(Color32[] px, int w, int h, Vector2 pivot)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };
        tex.SetPixels32(px);
        tex.Apply();
        var s = Sprite.Create(tex, new Rect(0, 0, w, h), pivot, PixelsPerUnit);
        s.hideFlags = HideFlags.HideAndDontSave;
        return s;
    }

    private static Sprite GetPanelSprite()
    {
        if (panelSprite != null) return panelSprite;

        const int W = 64, H = 100;
        var px = new Color32[W * H];
        var line = new Color32(20, 22, 36, 255);
        var body = new Color32(88, 98, 122, 255);
        var light = new Color32(124, 136, 164, 255);
        var dark = new Color32(60, 68, 90, 255);
        var metal = new Color32(150, 155, 170, 255);

        // badan panel
        Fill(px, W, 6, 0, 57, 58, line);
        Fill(px, W, 8, 2, 55, 56, body);
        Fill(px, W, 8, 54, 55, 56, light);
        Fill(px, W, 8, 2, 55, 5, dark);

        // layar kode
        Fill(px, W, 13, 35, 50, 52, line);
        Fill(px, W, 14, 36, 49, 51, new Color32(12, 38, 32, 255));
        for (int i = 0; i < 4; i++)
            Fill(px, W, 17 + i * 8, 40, 22 + i * 8, 47, new Color32(64, 232, 132, 255));

        // keypad 3x3
        for (int row = 0; row < 3; row++)
            for (int col = 0; col < 3; col++)
            {
                int x = 14 + col * 11, y = 8 + row * 8;
                Fill(px, W, x, y, x + 8, y + 5, line);
                Fill(px, W, x + 1, y + 1, x + 7, y + 5, new Color32(190, 198, 218, 255));
                Fill(px, W, x + 1, y + 1, x + 7, y + 1, new Color32(130, 138, 160, 255));
            }

        // lampu merah
        Fill(px, W, 47, 27, 52, 32, line);
        Fill(px, W, 48, 28, 51, 31, new Color32(235, 60, 70, 255));

        // tiang + lengan gantungan
        Fill(px, W, 28, 58, 35, 96, line);
        Fill(px, W, 29, 58, 34, 96, metal);
        Fill(px, W, 20, 92, 43, 98, line);
        Fill(px, W, 21, 93, 42, 97, metal);

        panelSprite = MakeSprite(px, W, H, new Vector2(0.5f, 0f));
        return panelSprite;
    }

    private static Sprite GetBellSprite()
    {
        if (bellSprite != null) return bellSprite;

        const int W = 40, H = 44;
        var mask = new bool[W, H];
        float cx = 19.5f;

        // kubah lonceng
        for (int y = 4; y <= 36; y++)
        {
            float half;
            if (y >= 10)
            {
                float k = (36f - y) / 26f;
                half = Mathf.Lerp(7f, 13f, Mathf.Pow(k, 1.6f));
            }
            else
            {
                half = 13f + (10 - y) * 0.85f;
            }
            for (int x = 0; x < W; x++)
                if (Mathf.Abs(x - cx) <= half) mask[x, y] = true;
        }
        // cincin gantungan
        for (int y = 37; y <= 42; y++)
            for (int x = 16; x <= 23; x++)
            {
                bool edge = x <= 17 || x >= 22 || y >= 41 || y <= 37;
                if (edge) mask[x, y] = true;
            }
        // pemukul di bawah bibir lonceng
        for (int y = 0; y <= 3; y++)
            for (int x = 0; x < W; x++)
            {
                float dx = x - cx, dy = y - 2.5f;
                if (dx * dx + dy * dy <= 7.5f) mask[x, y] = true;
            }

        var px = new Color32[W * H];
        var gold = new Color32(232, 184, 52, 255);
        var hi = new Color32(255, 230, 128, 255);
        var shade = new Color32(176, 124, 28, 255);
        var rim = new Color32(150, 100, 20, 255);
        var outline = new Color32(70, 42, 8, 255);

        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                Color32 c = Clear;
                if (mask[x, y])
                {
                    if (y <= 3) c = shade;
                    else if (y <= 6) c = rim;
                    else if (x < cx - 6) c = hi;
                    else if (x > cx + 6) c = shade;
                    else c = gold;
                }
                else if (HasNeighbor(mask, x, y, W, H))
                {
                    c = outline;
                }
                px[y * W + x] = c;
            }

        bellSprite = MakeSprite(px, W, H, new Vector2(0.5f, 1f));
        return bellSprite;
    }

    private static bool HasNeighbor(bool[,] m, int x, int y, int w, int h)
    {
        for (int dy = -1; dy <= 1; dy++)
            for (int dx = -1; dx <= 1; dx++)
            {
                if (dx == 0 && dy == 0) continue;
                int nx = x + dx, ny = y + dy;
                if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                if (m[nx, ny]) return true;
            }
        return false;
    }

    private static Sprite GetRingSprite()
    {
        if (ringSprite != null) return ringSprite;

        const int S = 64;
        var px = new Color32[S * S];
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float d = Mathf.Sqrt((x - 31.5f) * (x - 31.5f) + (y - 31.5f) * (y - 31.5f));
                bool on = d <= 31f && d >= 28f;
                px[y * S + x] = on ? new Color32(255, 255, 255, 255) : Clear;
            }
        ringSprite = MakeSprite(px, S, S, new Vector2(0.5f, 0.5f));
        return ringSprite;
    }

    private static AudioClip GetRingClip()
    {
        if (ringClip != null) return ringClip;

        const int rate = 44100;
        const float length = 3.2f;
        int n = (int)(rate * length);
        var data = new float[n];

        for (int i = 0; i < n; i++)
        {
            float t = i / (float)rate;
            float tone =
                Mathf.Sin(2f * Mathf.PI * 1180f * t) * 0.45f +
                Mathf.Sin(2f * Mathf.PI * 1867f * t) * 0.28f +
                Mathf.Sin(2f * Mathf.PI * 2610f * t) * 0.17f +
                Mathf.Sin(2f * Mathf.PI * 590f * t) * 0.25f;
            float hammer = 0.5f + 0.5f * Mathf.Abs(Mathf.Sin(t * Mathf.PI * 13f));   // "kring-kring-kring"
            float decay = Mathf.Exp(-t * 0.8f);
            float tail = Mathf.Clamp01((length - t) / 0.3f);
            data[i] = tone * hammer * decay * tail * 0.6f;
        }

        ringClip = AudioClip.Create("SchoolBellRing", n, 1, rate, false);
        ringClip.SetData(data, 0);
        ringClip.hideFlags = HideFlags.HideAndDontSave;
        return ringClip;
    }
}

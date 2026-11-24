using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// MISI 6: lari ke gerbang luar sekolah sambil dikejar 5 zombie (posisi spawn acak tiap main).
/// Begitu player menyentuh gerbang -> ending TANPA player, latar bg6, teks fade in / fade out.
/// Semua dibangun lewat kode, cukup pasang script ini di satu GameObject kosong.
/// </summary>
public class Mission6Manager : MonoBehaviour
{
    [Header("Gerbang luar sekolah (posisi dunia)")]
    public Vector2 gateCenter = new Vector2(0.2f, -3.6f);
    public Vector2 gateSize = new Vector2(2.2f, 1.0f);
    [Tooltip("Tinggi pagar (y). Pagar dibuat bercelah di sekitar gerbang.")]
    public float fenceY = -3.3f;
    public float fenceThickness = 0.5f;
    [Tooltip("Batas atas halaman (depan gedung).")]
    public float topWallY = 3.35f;

    [Header("Zombie")]
    public int zombieCount = 5;
    public float zombieSpeed = 2.4f;
    [Tooltip("Sprite & Animator controller zombie dari scene Lorong. Kosong = pakai punya player.")]
    public Sprite zombieSprite;
    public RuntimeAnimatorController zombieController;
    public Vector2 spawnMin = new Vector2(-7f, -2.4f);
    public Vector2 spawnMax = new Vector2(7f, 3.0f);
    public float minDistFromPlayer = 3f;
    public float minDistBetweenZombies = 1.6f;
    public float minDistFromGate = 2.2f;
    public float spawnGrace = 1f;

    [Header("Ending")]
    [Tooltip("Isi dengan font estetik (TTF/OTF) dari Assets. Kosong = pakai font sistem bergaya serif.")]
    public Font customFont;
    public int fontSize = 54;
    [Tooltip("Nama scene setelah ending (default kembali ke Intro).")]
    public string nextScene = "Intro";
    [TextArea(2, 4)] public string line1 = "Rasa ingin tahu tanpa tanggung jawab dapat membawa akibat yang besar.";
    [TextArea(2, 4)] public string line2 = "Sebuah eksperimen yang seharusnya membawa harapan justru berubah menjadi sebuah bencana.";
    public Color textColor = new Color(0.96f, 0.93f, 0.85f, 1f);
    [Range(0f, 1f)] public float dimAmount = 0.55f;

    [Header("Durasi (detik)")]
    public float blackFadeIn = 0.9f;
    public float bgReveal = 1.8f;
    public float dimFade = 1.2f;
    public float lineFade = 2.4f;
    public float lineGap = 1.2f;
    public float holdTime = 5f;
    public float textFadeOut = 2.2f;
    public float endFadeOut = 2f;

    private Transform player;
    private Collider2D playerCol;
    private readonly List<EscapeZombie> zombies = new List<EscapeZombie>();
    private bool finished;

    private Image blackImg, dimImg;
    private CanvasGroup group1, group2, dividerGroup, objectiveGroup;
    private Text text1, text2, objectiveText;

    private void Start()
    {
        var pc = FindFirstObjectByType<PlayerController>();
        if (pc == null)
        {
            Debug.LogError("[Mission6] PlayerController tidak ditemukan.");
            enabled = false;
            return;
        }
        player = pc.transform;
        playerCol = pc.GetComponent<Collider2D>();

        var old = GameObject.Find("collison");   // sisa collider ruangan lama
        if (old != null) old.SetActive(false);

        BuildWalls();
        SpawnZombies();
        BuildUI();
        StartCoroutine(ShowObjective());
    }

    private void Update()
    {
        if (finished || playerCol == null) return;
        var hp = PlayerHealth.Instance;
        if (hp != null && hp.IsDead) return;

        var gate = new Bounds(gateCenter, new Vector3(gateSize.x, gateSize.y, 10f));
        if (gate.Contains(new Vector3(playerCol.bounds.center.x, playerCol.bounds.center.y, 0f)))
        {
            finished = true;
            StartCoroutine(EndingRoutine());
        }
    }

    // ------------------------------------------------------------------ dunia

    private void BuildWalls()
    {
        var root = new GameObject("Mission6Walls");
        Bounds b = new Bounds(Vector3.zero, new Vector3(18f, 10f, 1f));
        var bg = GameObject.Find("Bg");
        if (bg != null)
        {
            var sr = bg.GetComponent<SpriteRenderer>();
            if (sr != null) b = sr.bounds;
        }

        float t = 2f;
        // batas luar: kiri, kanan, bawah (jalan raya), atas (depan gedung)
        AddWall(root.transform, new Vector2(b.min.x - t / 2f, b.center.y), new Vector2(t, b.size.y + 2f * t));
        AddWall(root.transform, new Vector2(b.max.x + t / 2f, b.center.y), new Vector2(t, b.size.y + 2f * t));
        AddWall(root.transform, new Vector2(b.center.x, b.min.y - t / 2f), new Vector2(b.size.x + 2f * t, t));
        AddWall(root.transform, new Vector2(b.center.x, topWallY + t / 2f), new Vector2(b.size.x + 2f * t, t));

        // pagar kiri dan kanan gerbang (celah = lebar gerbang)
        float gapL = gateCenter.x - gateSize.x / 2f;
        float gapR = gateCenter.x + gateSize.x / 2f;
        float leftW = gapL - b.min.x;
        float rightW = b.max.x - gapR;
        if (leftW > 0f) AddWall(root.transform, new Vector2(b.min.x + leftW / 2f, fenceY), new Vector2(leftW, fenceThickness));
        if (rightW > 0f) AddWall(root.transform, new Vector2(gapR + rightW / 2f, fenceY), new Vector2(rightW, fenceThickness));
    }

    private static void AddWall(Transform parent, Vector2 center, Vector2 size)
    {
        var go = new GameObject("Wall");
        go.transform.SetParent(parent, false);
        go.transform.position = center;
        var c = go.AddComponent<BoxCollider2D>();
        c.size = size;
    }

    private void SpawnZombies()
    {
        var psr = player.GetComponent<SpriteRenderer>();
        var pan = player.GetComponent<Animator>();
        var pcol = player.GetComponent<BoxCollider2D>();

        var placed = new List<Vector2>();
        for (int i = 0; i < zombieCount; i++)
        {
            Vector2 pos = PickSpawn(placed);
            placed.Add(pos);

            var z = new GameObject("Zombie_" + (i + 1));
            z.transform.position = new Vector3(pos.x, pos.y, player.position.z);
            z.transform.localScale = player.localScale;

            var sr = z.AddComponent<SpriteRenderer>();
            if (psr != null)
            {
                sr.sprite = psr.sprite;
                sr.sharedMaterial = psr.sharedMaterial;
                sr.sortingLayerID = psr.sortingLayerID;
                sr.sortingOrder = psr.sortingOrder;
            }

            var an = z.AddComponent<Animator>();
            if (pan != null)
            {
                an.runtimeAnimatorController = pan.runtimeAnimatorController;
                an.avatar = pan.avatar;
            }

            // sprite & animasi zombie dari scene Lorong (kalau diisi di Inspector)
            if (zombieSprite != null) sr.sprite = zombieSprite;
            if (zombieController != null) { an.runtimeAnimatorController = zombieController; an.avatar = null; }

            
var rb = z.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;

            var bc = z.AddComponent<BoxCollider2D>();
            if (pcol != null) { bc.size = pcol.size; bc.offset = pcol.offset; }

            var ez = z.AddComponent<EscapeZombie>();
            ez.speed = zombieSpeed;
            ez.graceTime = spawnGrace;
            ez.Init(player);
            zombies.Add(ez);
        }
    }

    private Vector2 PickSpawn(List<Vector2> placed)
    {
        Vector2 best = Vector2.zero;
        float bestScore = float.MinValue;

        for (int attempt = 0; attempt < 60; attempt++)
        {
            var p = new Vector2(UnityEngine.Random.Range(spawnMin.x, spawnMax.x),
                                UnityEngine.Random.Range(spawnMin.y, spawnMax.y));

            float dPlayer = Vector2.Distance(p, player.position);
            float dGate = Vector2.Distance(p, gateCenter);
            float dZ = float.MaxValue;
            foreach (var q in placed) dZ = Mathf.Min(dZ, Vector2.Distance(p, q));

            if (dPlayer >= minDistFromPlayer && dGate >= minDistFromGate && dZ >= minDistBetweenZombies)
                return p;

            // cadangan: simpan kandidat terbaik kalau semua percobaan gagal
            float score = Mathf.Min(dPlayer - minDistFromPlayer, dGate - minDistFromGate, dZ - minDistBetweenZombies);
            if (score > bestScore) { bestScore = score; best = p; }
        }
        return best;
    }

    // ------------------------------------------------------------------ UI

    private Font ResolveFont()
    {
        if (customFont != null) return customFont;
        return Font.CreateDynamicFontFromOSFont(
            new[] { "Palatino Linotype", "Georgia", "Garamond", "Book Antiqua", "Times New Roman" }, fontSize);
    }

    private void BuildUI()
    {
        var go = new GameObject("Mission6Canvas");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        Transform root = go.transform;

        Font font = ResolveFont();

        // gelap tipis di belakang teks ending
        dimImg = MakeImage("Dim", root, new Color(0f, 0f, 0f, 0f));
        Stretch(dimImg.rectTransform);

        // teks ending
        text1 = MakeText("Line1", root, font, fontSize, FontStyle.Italic, line1, textColor);
        PlaceCenter(text1.rectTransform, new Vector2(0f, 90f), new Vector2(1450f, 220f));
        group1 = text1.gameObject.AddComponent<CanvasGroup>();
        group1.alpha = 0f;

        var line = MakeImage("Divider", root, new Color(0.85f, 0.7f, 0.4f, 0.9f));
        PlaceCenter(line.rectTransform, new Vector2(0f, -20f), new Vector2(220f, 2f));
        dividerGroup = line.gameObject.AddComponent<CanvasGroup>();
        dividerGroup.alpha = 0f;

        text2 = MakeText("Line2", root, font, fontSize, FontStyle.Italic, line2, textColor);
        PlaceCenter(text2.rectTransform, new Vector2(0f, -140f), new Vector2(1450f, 260f));
        group2 = text2.gameObject.AddComponent<CanvasGroup>();
        group2.alpha = 0f;

        // petunjuk misi
        objectiveText = MakeText("Objective", root, font, 40, FontStyle.Bold,
            "LARI! Keluar lewat gerbang sekolah!", new Color(1f, 0.85f, 0.4f, 1f));
        var ort = objectiveText.rectTransform;
        ort.anchorMin = new Vector2(0.5f, 1f);
        ort.anchorMax = new Vector2(0.5f, 1f);
        ort.pivot = new Vector2(0.5f, 1f);
        ort.anchoredPosition = new Vector2(0f, -40f);
        ort.sizeDelta = new Vector2(1200f, 70f);
        objectiveGroup = objectiveText.gameObject.AddComponent<CanvasGroup>();
        objectiveGroup.alpha = 0f;

        // layar hitam paling atas (untuk transisi)
        blackImg = MakeImage("Black", root, new Color(0f, 0f, 0f, 0f));
        Stretch(blackImg.rectTransform);
        blackImg.raycastTarget = false;
    }

    private static Image MakeImage(string name, Transform parent, Color c)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = c;
        img.raycastTarget = false;
        return img;
    }

    private static Text MakeText(string name, Transform parent, Font font, int size, FontStyle style, string content, Color c)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.font = font;
        t.fontSize = size;
        t.fontStyle = style;
        t.text = content;
        t.color = c;
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.lineSpacing = 1.15f;
        t.supportRichText = true;
        t.raycastTarget = false;

        var sh = go.AddComponent<Shadow>();
        sh.effectColor = new Color(0f, 0f, 0f, 0.75f);
        sh.effectDistance = new Vector2(3f, -3f);
        var ol = go.AddComponent<Outline>();
        ol.effectColor = new Color(0f, 0f, 0f, 0.45f);
        ol.effectDistance = new Vector2(1.5f, -1.5f);
        return t;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void PlaceCenter(RectTransform rt, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    // ------------------------------------------------------------------ alur

    private IEnumerator ShowObjective()
    {
        yield return new WaitForSeconds(0.4f);
        yield return Fade(0.8f, a => objectiveGroup.alpha = a, 0f, 1f);
        yield return new WaitForSeconds(4f);
        if (!finished) yield return Fade(1.2f, a => objectiveGroup.alpha = a, 1f, 0f);
    }

    private IEnumerator EndingRoutine()
    {
        // player berhenti, zombie berhenti menyerang
        var pc = player.GetComponent<PlayerController>();
        if (pc != null) pc.enabled = false;
        var prb = player.GetComponent<Rigidbody2D>();
        if (prb != null) prb.linearVelocity = Vector2.zero;
        foreach (var z in zombies) if (z != null) z.frozen = true;
        objectiveGroup.alpha = 0f;

        // 1) layar memudar ke hitam
        yield return Fade(blackFadeIn, a => SetAlpha(blackImg, a), 0f, 1f);

        // 2) hilangkan player, zombie, bar nyawa -> tinggal latar bg6
        player.gameObject.SetActive(false);
        foreach (var z in zombies) if (z != null) Destroy(z.gameObject);
        var hpCanvas = GameObject.Find("HealthCanvas");
        if (hpCanvas != null) hpCanvas.SetActive(false);
        yield return new WaitForSeconds(0.5f);

        // 3) bg6 muncul perlahan (fade in)
        yield return Fade(bgReveal, a => SetAlpha(blackImg, a), 1f, 0f);
        yield return new WaitForSeconds(0.8f);

        // 4) latar sedikit digelapkan supaya teks terbaca
        yield return Fade(dimFade, a => SetAlpha(dimImg, a * dimAmount), 0f, 1f);

        // 5) kalimat 1 -> garis -> kalimat 2 (masing-masing fade in)
        yield return Fade(lineFade, a => group1.alpha = a, 0f, 1f);
        yield return Fade(0.8f, a => dividerGroup.alpha = a, 0f, 1f);
        yield return new WaitForSeconds(lineGap * 0.4f);
        yield return Fade(lineFade, a => group2.alpha = a, 0f, 1f);
        yield return new WaitForSeconds(holdTime);

        // 6) teks fade out
        yield return Fade(textFadeOut, a =>
        {
            group1.alpha = a; group2.alpha = a; dividerGroup.alpha = a;
        }, 1f, 0f);
        yield return new WaitForSeconds(0.4f);

        // 7) bg6 fade out ke hitam
        yield return Fade(endFadeOut, a => SetAlpha(blackImg, a), 0f, 1f);

        MissionManager.Instance?.CompleteMission6();
        CorridorState.Reset();
        PlayerFreeze.Set(false);

        string targetScene = string.IsNullOrEmpty(nextScene) ? "Intro" : nextScene;
        SceneManager.LoadScene(targetScene);
    }

    private static void SetAlpha(Image img, float a)
    {
        var c = img.color;
        c.a = a;
        img.color = c;
    }

    private static IEnumerator Fade(float duration, Action<float> apply, float from, float to)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            apply(Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration))));
            yield return null;
        }
        apply(to);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.8f);
        Gizmos.DrawWireCube(gateCenter, gateSize);
        Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.6f);
        Vector2 c = (spawnMin + spawnMax) / 2f;
        Gizmos.DrawWireCube(c, spawnMax - spawnMin);
    }
}

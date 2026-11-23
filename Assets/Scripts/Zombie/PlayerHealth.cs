using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Nyawa player dengan bar (fill) di kiri bawah layar.
/// Selama bersentuhan dengan zombie, damage per detik naik makin lama (kena terus = makin sakit).
/// Kalau nyawa habis: scene diulang dari awal (misi ikut ter-reset oleh MissionManager).
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    public static PlayerHealth Instance { get; private set; }

    [Header("Nyawa")]
    [SerializeField] private float maxHealth = 100f;

    [Header("Damage dari zombie (per detik)")]
    [Tooltip("Damage per detik saat baru tersentuh.")]
    [SerializeField] private float baseDamagePerSecond = 8f;
    [Tooltip("Tambahan damage per detik untuk setiap detik sentuhan berlanjut.")]
    [SerializeField] private float rampPerSecond = 6f;
    [SerializeField] private float maxDamagePerSecond = 60f;
    [Tooltip("Seberapa cepat 'kenaikan damage' turun saat player sudah lepas dari zombie.")]
    [SerializeField] private float contactCooldownRate = 1.5f;

    [Header("Game over")]
    [SerializeField] private float restartDelay = 1.3f;

    private static readonly Color PanelFill = new Color32(9, 13, 30, 240);
    private static readonly Color PanelBorder = new Color32(38, 50, 84, 255);
    private static readonly Color Gold = new Color32(255, 196, 61, 255);
    private static readonly Color Green = new Color32(46, 213, 115, 255);
    private static readonly Color Red = new Color32(255, 82, 96, 255);

    /// <summary>Nyawa yang dibawa antar ruangan lorong (Lorong, RuangKepsek, RuangCCTV). -1 = belum ada.</summary>
    private static float savedHealth = -1f;

    private static bool IsCorridorScene(string n) =>
        n == MissionManager.LorongSceneName || n == MissionManager.KepsekSceneName || n == MissionManager.CctvSceneName;

    private float health;
    private float contactTime;
    private int lastContactFrame = -10;
    private bool dead;
    private float flash;
    private float shownFill = 1f;

    private GameObject canvasGo;
    private Image fillImage;
    private Image flashImage;
    private Text valueText;
    private static Sprite whiteSprite;

    public bool IsDead => dead;
    public float Current => health;
    public float Max => maxHealth;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
        whiteSprite = null;
        savedHealth = -1f;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
        health = maxHealth;
        if (IsCorridorScene(SceneManager.GetActiveScene().name))
        {
            if (savedHealth >= 0f) health = Mathf.Clamp(savedHealth, 1f, maxHealth);   // darah ikut terbawa antar ruangan
        }
        else savedHealth = -1f;
        BuildUI();
        Refresh(true);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            if (!dead && IsCorridorScene(gameObject.scene.name)) savedHealth = health;
        }
        if (canvasGo != null) Destroy(canvasGo);
    }

    // ------------------------------------------------------------------ damage

    /// <summary>Dipanggil zombie setiap frame selama menyentuh player.</summary>
    public void ApplyContactDamage(float dt)
    {
        if (dead) return;

        if (lastContactFrame != Time.frameCount)
        {
            contactTime += dt;                 // lama sentuhan berlanjut (sekali per frame)
            lastContactFrame = Time.frameCount;
        }

        float dps = Mathf.Min(baseDamagePerSecond + rampPerSecond * contactTime, maxDamagePerSecond);
        health -= dps * dt;
        flash = Mathf.Max(flash, 0.3f);

        if (health <= 0f)
        {
            health = 0f;
            Die();
        }
    }

    private void Update()
    {
        // Kalau tidak ada sentuhan dalam frame terakhir, kenaikan damage turun pelan-pelan.
        if (Time.frameCount - lastContactFrame > 1)
            contactTime = Mathf.Max(0f, contactTime - contactCooldownRate * Time.deltaTime);

        Refresh(false);
    }

    private void Die()
    {
        if (dead) return;
        dead = true;
        flash = 0.6f;

        PlayerFreeze.Set(true);
        savedHealth = -1f;
        CorridorState.Reset();   // bel, posisi zombie & misi lorong mulai dari awal lagi

        string scene = SceneManager.GetActiveScene().name;
        var ui = GameUI.Instance;
        if (ui != null)
        {
            ui.ShowToast("Kamu tertangkap zombie! Mengulang dari awal...", true, 2.2f);
            ui.TransitionToScene(scene, restartDelay);
        }
        else
        {
            SceneManager.LoadScene(scene);
        }
    }

    // ------------------------------------------------------------------ UI

    private static Sprite WhiteSprite()
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

    private void BuildUI()
    {
        canvasGo = new GameObject("HealthCanvas");
        canvasGo.layer = LayerMask.NameToLayer("UI");

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;               // di bawah GameUI (100) supaya fade scene menutupinya

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        var root = (RectTransform)canvasGo.transform;

        // Kilatan merah saat terluka / nyawa tipis
        flashImage = UIFactory.Box("HurtFlash", root, new Color(0.85f, 0.05f, 0.1f, 0f));
        UIFactory.Stretch(flashImage.rectTransform);

        // Panel bar
        var panel = UIFactory.Bordered("HealthPanel", root, PanelFill, PanelBorder, 2, 16);
        UIFactory.Place(panel.Root, new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(28f, 28f), new Vector2(400f, 64f));

        var hp = UIFactory.Label("HP", panel.Inner, "HP", 26, Gold, TextAnchor.MiddleCenter);
        UIFactory.Place(hp.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(12f, 0f), new Vector2(56f, 48f));

        var barBg = UIFactory.Box("BarBg", panel.Inner, new Color32(28, 34, 58, 255));
        UIFactory.Place(barBg.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(74f, 0f), new Vector2(308f, 28f));

        fillImage = UIFactory.Box("Fill", barBg.rectTransform, Green);
        fillImage.sprite = WhiteSprite();
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = 0;
        fillImage.fillAmount = 1f;
        UIFactory.Stretch(fillImage.rectTransform, 3f);

        valueText = UIFactory.Label("Value", barBg.rectTransform, "100", 20, Color.white, TextAnchor.MiddleCenter);
        UIFactory.Stretch(valueText.rectTransform);
    }

    private void Refresh(bool instant)
    {
        if (fillImage == null) return;

        float target = maxHealth > 0f ? Mathf.Clamp01(health / maxHealth) : 0f;
        shownFill = instant ? target : Mathf.Lerp(shownFill, target, 1f - Mathf.Exp(-14f * Time.deltaTime));
        fillImage.fillAmount = shownFill;
        fillImage.color = target > 0.5f
            ? Color.Lerp(Gold, Green, (target - 0.5f) * 2f)
            : Color.Lerp(Red, Gold, target * 2f);
        valueText.text = Mathf.CeilToInt(health).ToString();

        flash = Mathf.MoveTowards(flash, 0f, Time.deltaTime * 1.2f);
        float lowPulse = (!dead && target < 0.3f) ? 0.1f + 0.07f * Mathf.Sin(Time.time * 6f) : 0f;
        var c = flashImage.color;
        c.a = Mathf.Max(flash, lowPulse);
        flashImage.color = c;
    }
}

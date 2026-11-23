using System.Collections;
using UnityEngine;

/// <summary>
/// Simbol kunci yang melayang di atas kepala player selama player membawa kunci.
/// Dibuat dan dihapus lewat KeyIndicator.SetVisible(...). Sprite digambar lewat kode.
/// </summary>
public class KeyIndicator : MonoBehaviour
{
    private const float HeightAboveHead = 0.45f;
    private const float BobAmount = 0.06f;
    private const float BobSpeed = 3f;

    private static KeyIndicator instance;
    private static Sprite sprite;

    private Transform target;
    private SpriteRenderer targetRenderer;
    private float visibleScale;       // 0..1 untuk animasi muncul/hilang
    private bool hiding;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
        sprite = null;
    }

    public static void SetVisible(bool visible)
    {
        if (visible)
        {
            if (instance == null)
            {
                var go = new GameObject("KeyIcon");
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = GetSprite();
                sr.sortingOrder = 50;
                instance = go.AddComponent<KeyIndicator>();
            }
            instance.hiding = false;
            instance.FindTarget();
        }
        else if (instance != null)
        {
            instance.hiding = true;
        }
    }

    private void FindTarget()
    {
        var pc = FindFirstObjectByType<PlayerController>();
        target = pc != null ? pc.transform : null;
        targetRenderer = pc != null ? pc.GetComponent<SpriteRenderer>() : null;
    }

    private void LateUpdate()
    {
        if (target == null) FindTarget();

        // animasi muncul (pop) / hilang
        visibleScale = Mathf.MoveTowards(visibleScale, hiding ? 0f : 1f, Time.deltaTime * 5f);
        if (hiding && visibleScale <= 0f)
        {
            Destroy(gameObject);
            instance = null;
            return;
        }
        float overshoot = hiding ? visibleScale : 1f + 0.25f * Mathf.Sin(visibleScale * Mathf.PI);
        transform.localScale = Vector3.one * (visibleScale * overshoot);

        if (target == null) return;
        float headY = targetRenderer != null ? targetRenderer.bounds.max.y : target.position.y + 1.5f;
        transform.position = new Vector3(
            target.position.x,
            headY + HeightAboveHead + Mathf.Sin(Time.time * BobSpeed) * BobAmount,
            0f);
    }

    private static Sprite GetSprite()
    {
        if (sprite != null) return sprite;

        const int S = 32;
        var body = new bool[S, S];

        // cincin kunci
        const float cx = 9f, cy = 16f;
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                if (d <= 8f && d >= 3.5f) body[x, y] = true;
            }
        // batang
        for (int x = 15; x <= 29; x++)
            for (int y = 14; y <= 17; y++) body[x, y] = true;
        // gigi kunci
        for (int x = 23; x <= 25; x++)
            for (int y = 9; y <= 13; y++) body[x, y] = true;
        for (int x = 28; x <= 29; x++)
            for (int y = 10; y <= 13; y++) body[x, y] = true;

        var tex = new Texture2D(S, S, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };
        var px = new Color32[S * S];
        var gold = new Color32(255, 205, 60, 255);
        var light = new Color32(255, 237, 140, 255);
        var shade = new Color32(214, 156, 28, 255);
        var outline = new Color32(70, 42, 8, 255);

        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                Color32 c = new Color32(0, 0, 0, 0);
                if (body[x, y])
                {
                    c = y >= 18 ? light : (y <= 13 ? shade : gold);
                }
                else if (HasNeighbor(body, x, y, S))
                {
                    c = outline;
                }
                px[y * S + x] = c;
            }

        tex.SetPixels32(px);
        tex.Apply();
        sprite = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 48f);
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }

    private static bool HasNeighbor(bool[,] body, int x, int y, int size)
    {
        for (int dy = -1; dy <= 1; dy++)
            for (int dx = -1; dx <= 1; dx++)
            {
                if (dx == 0 && dy == 0) continue;
                int nx = x + dx, ny = y + dy;
                if (nx < 0 || ny < 0 || nx >= size || ny >= size) continue;
                if (body[nx, ny]) return true;
            }
        return false;
    }
}

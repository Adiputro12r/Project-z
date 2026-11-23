using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Basis semua objek interaktif. Pasang di GameObject yang punya Collider2D ber-"Is Trigger".
/// Saat player masuk trigger, muncul prompt "[E] ...". Bisa ditekan E atau diklik mouse.
/// Kalau beberapa trigger tumpang tindih, yang terdekat dengan player yang dipilih.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public abstract class ProximityInteractable : MonoBehaviour
{
    [Tooltip("Kosongkan untuk memakai teks bawaan.")]
    [SerializeField] private string promptOverride = "";
    [Tooltip("Boleh juga diklik dengan mouse selama player berada di dalam jangkauan.")]
    [SerializeField] private bool allowMouseClick = true;

    private static readonly List<ProximityInteractable> InRange = new List<ProximityInteractable>();

    /// <summary>Objek yang sedang disorot prompt (null kalau tidak ada).</summary>
    public static ProximityInteractable Active { get; private set; }

    private Collider2D col;
    private Transform playerTf;

    protected abstract string DefaultPrompt { get; }
    protected virtual bool CanInteract => true;
    protected abstract void Interact();

    public string PromptText => string.IsNullOrEmpty(promptOverride) ? DefaultPrompt : promptOverride;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        InRange.Clear();
        Active = null;
    }

    protected virtual void Awake()
    {
        col = GetComponent<Collider2D>();
        if (!col.isTrigger)
            Debug.LogWarning("[" + name + "] Collider2D belum 'Is Trigger' - prompt proximity tidak akan muncul.", this);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var pc = other.GetComponentInParent<PlayerController>();
        if (pc == null) return;
        playerTf = pc.transform;
        if (!InRange.Contains(this)) InRange.Add(this);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerController>() == null) return;
        InRange.Remove(this);
    }

    private void OnDisable() => InRange.Remove(this);

    private float SqrDistanceToPlayer()
    {
        if (playerTf == null || col == null) return float.MaxValue;
        Vector2 p = playerTf.position;
        return ((Vector2)col.ClosestPoint(p) - p).sqrMagnitude;
    }

    /// <summary>Dipanggil GameUI setiap frame: pilih objek aktif lalu baca input E / klik.</summary>
    public static void Tick(bool modalOpen)
    {
        Active = null;
        if (modalOpen || InRange.Count == 0) return;

        float best = float.MaxValue;
        for (int i = InRange.Count - 1; i >= 0; i--)
        {
            var it = InRange[i];
            if (it == null)
            {
                InRange.RemoveAt(i);
                continue;
            }
            if (!it.CanInteract) continue;

            float d = it.SqrDistanceToPlayer();
            if (d < best)
            {
                best = d;
                Active = it;
            }
        }
        if (Active == null) return;

        var clicked = FindClicked();
        if (clicked != null)
        {
            clicked.Interact();
            return;
        }

        var kb = Keyboard.current;
        if (kb != null && kb.eKey.wasPressedThisFrame) Active.Interact();
    }

    private static ProximityInteractable FindClicked()
    {
        var mouse = Mouse.current;
        var cam = Camera.main;
        if (mouse == null || cam == null || !mouse.leftButton.wasPressedThisFrame) return null;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return null;

        Vector3 screen = mouse.position.ReadValue();
        screen.z = Mathf.Abs(cam.transform.position.z);
        Vector2 world = cam.ScreenToWorldPoint(screen);

        for (int i = 0; i < InRange.Count; i++)
        {
            var it = InRange[i];
            if (it != null && it.allowMouseClick && it.CanInteract && it.col.OverlapPoint(world))
                return it;
        }
        return null;
    }
}

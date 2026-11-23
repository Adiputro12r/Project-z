using UnityEngine;

/// <summary>
/// Pintu yang aktif begitu player menyentuh (collision/trigger) tanpa perlu tekan E.
/// Dipakai untuk: pintu ruang kepala sekolah, pintu keluar ke ruang CCTV, dan pintu kembali ke lorong.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class SceneDoorTrigger : MonoBehaviour
{
    public enum Kind { Plain, Office, Cctv }

    [SerializeField] private string targetScene = "";
    [SerializeField] private Kind kind = Kind.Plain;
    [Tooltip("Posisi player di scene tujuan.")]
    [SerializeField] private Vector2 spawnInTarget = Vector2.zero;
    [SerializeField] private bool useSpawn = true;
    [Tooltip("Pintu dikunci permanen / tidak bisa kembali.")]
    [SerializeField] private bool isLocked = false;
    [Tooltip("Pintu baru bisa dipakai setelah bel sekolah dibunyikan.")]
    [SerializeField] private bool requiresBell = false;
    [SerializeField] private string lockedMessage = "Pintu terkunci.";

    private bool used;
    private float nextMessageTime;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other) => TryUse(other);
    private void OnTriggerStay2D(Collider2D other) => TryUse(other);

    private void TryUse(Collider2D other)
    {
        if (used || other.GetComponentInParent<PlayerController>() == null) return;

        var ui = GameUI.Instance;
        var m = MissionManager.Instance;
        if (ui == null || m == null || ui.ModalOpen || ui.IsTransitioning) return;

        if (isLocked)
        {
            if (Time.time >= nextMessageTime)
            {
                nextMessageTime = Time.time + 3f;
                ui.ShowToast(lockedMessage, true, 2.6f);
            }
            return;
        }

        if (requiresBell && !CorridorState.BellActive)
        {
            if (Time.time >= nextMessageTime)
            {
                nextMessageTime = Time.time + 3f;
                ui.ShowToast(lockedMessage, true, 2.6f);
            }
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(targetScene))
        {
            if (Time.time >= nextMessageTime)
            {
                nextMessageTime = Time.time + 3f;
                ui.ShowToast("Scene \"" + targetScene + "\" belum ada di Build Settings.", true, 3.5f);
            }
            return;
        }

        used = true;
        if (kind == Kind.Office) m.EnteredOffice();
        else if (kind == Kind.Cctv) m.EnteredCctv();

        if (useSpawn) CorridorState.SetNextSpawn(spawnInTarget);
        ui.TransitionToScene(targetScene, 0.15f);
    }

    private void OnDrawGizmosSelected()
    {
        var col = GetComponent<Collider2D>();
        if (col == null) return;
        Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.8f);
        Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(spawnInTarget, 0.1f);
    }
}

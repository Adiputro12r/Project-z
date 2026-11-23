using UnityEngine;

/// <summary>
/// Trigger zona mobil patroli polisi di Halaman Sekolah (Misi 6).
/// Saat player mendekati mobil patroli di area parkir, muncul peringatan darurat
/// bahwa mobil mulai terbakar/berasap dan akan meledak, memicu langkah klimaks pelarian ke gerbang.
/// </summary>
public class PatrolCarClimaxTrigger : MonoBehaviour
{
    [SerializeField] private float triggerRadius = 2.5f;
    [SerializeField] private GameObject fireEffect;
    [SerializeField] private Light warningLight;

    private bool triggered;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered || !other.CompareTag("Player")) return;
        TriggerClimax();
    }

    public void TriggerClimax()
    {
        if (triggered) return;
        triggered = true;

        var m = MissionManager.Instance;
        if (m != null)
        {
            m.ApproachedPatrolCar();
        }

        var ui = GameUI.Instance;
        if (ui != null)
        {
            ui.ShowToast("PERINGATAN: Mobil patroli berasap tebal dan hampir meledak! Cepat lari ke Gerbang Utama!", true, 4.0f);
        }

        if (fireEffect != null) fireEffect.SetActive(true);
        if (warningLight != null) warningLight.enabled = true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, triggerRadius);
    }
}

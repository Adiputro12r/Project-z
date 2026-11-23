using UnityEngine;

/// <summary>
/// Trigger zona awal halaman sekolah (Misi 6 - Langkah 0).
/// Saat player melangkah keluar pintu gedung dan memasuki halaman terbuka,
/// menandai langkah 'Menghindari kerumunan zombie di halaman' telah dimulai/dilewati.
/// </summary>
public class CourtyardZombieTrigger : MonoBehaviour
{
    private bool triggered;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered || !other.CompareTag("Player")) return;
        triggered = true;

        var m = MissionManager.Instance;
        if (m != null)
        {
            m.EvadedZombies();
        }

        var ui = GameUI.Instance;
        if (ui != null)
        {
            ui.ShowToast("Zombie berkeliaran di lapangan & parkiran! Menghindarlah dan dekati mobil patroli!", false, 3.5f);
        }
    }
}

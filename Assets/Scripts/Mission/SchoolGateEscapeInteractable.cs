using UnityEngine;

/// <summary>
/// Gerbang utama sekolah di bagian bawah Halaman Sekolah (Misi 6 - Klimaks).
/// Player berinteraksi atau melangkah melewati gerbang untuk memicu ledakan mobil patroli polisi,
/// melumpuhkan seluruh zombie di halaman, dan menyelesaikan game dengan Victory Ending Modal.
/// </summary>
public class SchoolGateEscapeInteractable : ProximityInteractable
{
    private bool escaped;

    protected override string DefaultPrompt => "Kabur Melewati Gerbang Utama";

    protected override bool CanInteract => !escaped;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (escaped || !other.CompareTag("Player")) return;
        Interact();
    }

    protected override void Interact()
    {
        if (escaped) return;
        escaped = true;

        var m = MissionManager.Instance;
        var ui = GameUI.Instance;

        if (m != null)
        {
            m.EscapedSchool();
        }

        // Lumpuhkan semua zombie di halaman sekolah akibat gelombang ledakan
        var zombies = FindObjectsByType<ZombieController>(FindObjectsSortMode.None);
        foreach (var z in zombies)
        {
            if (z != null)
            {
                z.gameObject.SetActive(false);
            }
        }

        if (ui != null)
        {
            ui.ShowVictoryModal(
                "BERHASIL KELUAR DARI KAWASAN SEKOLAH",
                "MISI SELESAI — SELAMAT!",
                "BOOOM!! Mobil patroli kepolisian meledak dahsyat di halaman sekolah!\n\n" +
                "Gelombang ledakan seketika melumpuhkan seluruh kerumunan zombie di halaman, memberimu celah emas untuk menerobos gerbang utama.\n\n" +
                "Dengan napas terengah, kamu melompat melewati pagar ke jalan raya dengan selamat sambil mendekap erat map dokumen rahasia <color=#2ED573><b>PROJECT Z (EXPERIMENT X)</b></color>.\n\n" +
                "Kebenaran kelam di balik bencana wabah ini berhasil kamu selamatkan dan siap diungkap ke publik untuk menghentikan penyebaran virus selamanya!");
        }
    }
}

using UnityEngine;

/// <summary>
/// Tangga darurat di Ruang Operator (CCTV).
/// Terkunci sampai dokumen rahasia PROJECT Z selesai dibaca.
/// Setelah dibaca, player bisa berinteraksi untuk keluar gedung dan menyelesaikan misi serta cerita.
/// </summary>
public class EmergencyExitInteractable : ProximityInteractable
{
    private bool used;

    protected override string DefaultPrompt
    {
        get
        {
            var m = MissionManager.Instance;
            return (m != null && m.DocumentRead)
                ? "Tangga Darurat (Keluar Gedung)"
                : "Tangga Darurat [Terkunci]";
        }
    }

    protected override bool CanInteract => !used;

    protected override void Interact()
    {
        var m = MissionManager.Instance;
        var ui = GameUI.Instance;
        if (m == null || ui == null || used) return;

        if (!m.DocumentRead)
        {
            ui.ShowToast("Pintu tangga darurat terkunci otomatis. Cari dan baca dokumen Project Z terlebih dahulu!", true, 3.2f);
            return;
        }

        used = true;
        m.LeftRoom();

        ui.ShowDocModal(
            "BERHASIL KELUAR DARI RUANG OPERATOR",
            "MISI 5 SELESAI",
            "Ledakan mobil patroli kepolisian melumpuhkan kerumunan zombie di halaman sekolah.\n\n" +
            "Kamu berhasil membuka pintu tangga darurat dan menerobos keluar dari ruang operator!\n\n" +
            "Dengan membawa dokumen bukti rahasia <color=#2ED573><b>PROJECT Z (EXPERIMENT X)</b></color>, kebenaran di balik bencana wabah ini siap diungkap ke publik!",
            new Color32(46, 213, 115, 255),
            () =>
            {
                m.StartMission6();
                ui.ShowToast("Misi 6 dimulai!", false, 1.5f);
                ui.TransitionToScene("Misi6", 0.5f);
            });
    }
}

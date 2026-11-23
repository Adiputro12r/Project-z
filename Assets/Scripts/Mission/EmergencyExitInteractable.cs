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

        ui.ShowToast("Berlari menuruni tangga darurat menuju Halaman Sekolah...", false, 2.5f);
        ui.TransitionToScene("HalamanSekolah", 0.6f);
    }
}

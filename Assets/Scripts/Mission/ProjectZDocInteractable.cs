using System.Collections;
using UnityEngine;

/// <summary>
/// Lemari / brankas berkas rahasia PROJECT Z di Ruang Operator.
/// Hanya bisa diakses setelah memeriksa rekaman CCTV.
/// Menyelesaikan langkah "Mencari dokumen rahasia" dan "Membaca dokumen PROJECT Z".
/// </summary>
public class ProjectZDocInteractable : ProximityInteractable
{
    protected override string DefaultPrompt => "Periksa Berkas Dokumen";

    // Brankas harus dibuka dulu dengan kode sebelum dokumennya bisa dibaca.
    private bool unlocked;

    protected override void Interact()
    {
        var m = MissionManager.Instance;
        var ui = GameUI.Instance;
        if (m == null || ui == null) return;

        if (!m.CctvChecked)
        {
            ui.ShowToast("Periksa rekaman CCTV terlebih dahulu untuk petunjuk dokumen!", true, 3.0f);
            return;
        }

        if (!unlocked)
        {
            ui.OpenKeypad(
                "KODE DARURAT",
                "Berapa angka terakhir alamat IP 192.168.1.10?\n\nKode 4 digit, awali dengan 0 bila kurang.",
                entered => entered == MissionManager.SafeCode,
                () =>
                {
                    unlocked = true;
                    StartCoroutine(ReopenAfterUnlock());
                },
                MissionManager.SafeCode.Length);
            return;
        }

        m.ReadDocument();

        ui.ShowDocModal(
            "DOKUMEN RAHASIA: PROJECT Z",
            "[KLASIFIKASI: SANGAT RAHASIA / TOP SECRET]",
            "<color=#FF5260><b>DOKUMEN OTORITAS RISET: EXPERIMENT X (PROJECT Z)</b></color>\n\n" +
            "<b>1. Sasaran Riset:</b>\n" +
            "Mengembangkan formula serum regenerasi sel organ dan jaringan hidup super (Experiment X).\n\n" +
            "<b>2. Catatan Kegagalan Uji Coba:</b>\n" +
            "Uji coba awal pada primata menunjukkan regenerasi fisik, namun saat diujikan kepada subjek manusia tanpa protokol standar, virus bermutasi seketika: menyerang sistem saraf pusat dan memicu dorongan kanibalistik ekstrem yang sangat agresif.\n\n" +
            "<color=#FFC43D><b>3. Kesimpulan & Penanganan Darurat:</b>\n" +
            "Wabah sudah di luar kendali! Protokol karantina total diaktifkan. Seluruh akses dikunci dan evakuasi hanya melalui Tangga Darurat!</color>",
            new Color32(255, 82, 96, 255),
            () =>
            {
                ui.ShowToast("Rahasia Project Z terungkap! Segera lari lewat Tangga Darurat!", false, 3.5f);
            });
    }


    private IEnumerator ReopenAfterUnlock()
    {
        yield return new WaitForSeconds(0.45f);   // tunggu keypad tertutup dulu
        Interact();
    }
}

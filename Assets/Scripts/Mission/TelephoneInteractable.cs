using UnityEngine;

/// <summary>
/// Meja tengah dengan telepon kabel darurat dan buku kontak kepolisian (Misi 4).
/// </summary>
public class TelephoneInteractable : ProximityInteractable
{
    protected override string DefaultPrompt => "Periksa Meja & Telepon Kabel";

    protected override void Interact()
    {
        var ui = GameUI.Instance;
        if (ui == null) return;

        ui.ShowDocModal(
            "TELEPON KABEL & BUKU KONTAK DARURAT",
            "[STATUS: SAMBUNGAN DARURAT KEPOLISIAN]",
            "Di atas meja operator terdapat telepon kabel merah yang masih berfungsi dan buku kontak darurat sekolah.\n\n" +
            "<color=#FFC43D><b>Catatan Saluran Darurat:</b></color>\n" +
            "Kode Akses Cepat Kepolisian: <color=#2ED573><b>9110</b></color>\n\n" +
            "Terdengar nada panggil tersambung ke stasiun polisi: <i>'Unit patroli kepolisian telah dikirimkan ke sekolah!'</i>\n\n" +
            "<i>Petunjuk: Periksa rekaman Kamera 04 di monitor CCTV untuk mencari tahu penyebab wabah!</i>",
            new Color32(255, 196, 61, 255),
            () =>
            {
                ui.ShowToast("Telepon kabel aktif. Kode akses kepolisian 9110 tersambung!", false, 3.0f);
            });
    }
}

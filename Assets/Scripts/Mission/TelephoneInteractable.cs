using UnityEngine;

/// <summary>
/// Meja operator dengan telepon kabel darurat dan buku kontak kepolisian (Misi 4).
/// Langkah 1: periksa meja operator (muncul catatan kode kepolisian).
/// Langkah 2: sambungkan telepon dengan memasukkan kode (9110) lewat keypad -> lanjut ke Misi 5.
/// </summary>
public class TelephoneInteractable : ProximityInteractable
{
    protected override string DefaultPrompt
    {
        get
        {
            var m = MissionManager.Instance;
            return (m != null && m.OperatorDeskChecked)
                ? "Sambungkan Telepon ke Kepolisian"
                : "Periksa Meja Operator";
        }
    }

    protected override bool CanInteract
    {
        get
        {
            var m = MissionManager.Instance;
            return m != null && !m.PoliceConnected;
        }
    }

    protected override void Interact()
    {
        var m = MissionManager.Instance;
        var ui = GameUI.Instance;
        if (m == null || ui == null) return;

        if (!m.OperatorDeskChecked)
        {
            ui.ShowDocModal(
                "MEJA OPERATOR & TELEPON KABEL",
                "[STATUS: SAMBUNGAN DARURAT KEPOLISIAN]",
                "Di atas meja operator terdapat telepon kabel merah yang masih berfungsi dan buku kontak darurat sekolah.\n\n" +
                "<color=#FFC43D><b>Catatan Saluran Darurat:</b></color>\n" +
                "Kode Akses Cepat Kepolisian: <color=#2ED573><b>" + MissionManager.PoliceCode + "</b></color>\n\n" +
                "<i>Petunjuk: Periksa meja lagi untuk menyambungkan telepon ke kepolisian dengan memasukkan kode di atas.</i>",
                new Color32(255, 196, 61, 255),
                () => m.CheckedOperatorDesk());
            return;
        }

        ui.OpenKeypad(
            "TELEPON KEPOLISIAN",
            "Telepon kabel menunggu kode akses cepat kepolisian (" + MissionManager.PoliceCode.Length + " digit).\n\nKodenya tercatat di buku kontak darurat di meja operator.",
            ValidateCode,
            OnSolved,
            MissionManager.PoliceCode.Length);
    }

    private static bool ValidateCode(string entered)
    {
        var m = MissionManager.Instance;
        return m != null && m.OperatorDeskChecked && entered == MissionManager.PoliceCode;
    }

    private static void OnSolved()
    {
        var m = MissionManager.Instance;
        var ui = GameUI.Instance;
        if (m == null || ui == null) return;

        m.ConnectedPolice();   // Misi 4 selesai -> Misi 5 mulai otomatis
        ui.ShowToast("Tersambung ke kepolisian! 'Unit patroli telah dikirimkan ke sekolah.' Periksa rekaman CCTV Kamera 04.", false, 3.6f);
    }
}

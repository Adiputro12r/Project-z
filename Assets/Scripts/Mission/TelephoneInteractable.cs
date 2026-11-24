using UnityEngine;

/// <summary>
/// Meja operator dengan telepon kabel darurat dan buku kontak kepolisian (Misi 4).
/// Langkah 1: periksa meja operator (muncul catatan kode kepolisian).
/// Langkah 2: sambungkan telepon dengan menjawab soal (0072) lewat keypad -> lanjut ke Misi 5.
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
                "Nomor telepon kepolisian adalah jawaban soal ini:\n<color=#2ED573><b>Berapa jumlah jam dalam 3 hari? (3 x 24)</b></color>\n\n" +
                "<i>Petunjuk: Tulis jawabannya 4 digit (awali dengan 0 jika kurang). Periksa meja lagi untuk menyambungkan telepon ke kepolisian.</i>",
                new Color32(255, 196, 61, 255),
                () => m.CheckedOperatorDesk());
            return;
        }

        ui.OpenKeypad(
            "TELEPON KEPOLISIAN",
            "Telepon kabel menunggu nomor kepolisian (" + MissionManager.PoliceCode.Length + " digit).\n\nSoalnya ada di meja operator. Awali 0 bila kurang.",
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

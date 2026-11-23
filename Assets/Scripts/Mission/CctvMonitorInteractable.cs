using UnityEngine;

/// <summary>
/// Monitor rekaman CCTV di Ruang Operator (CCTV).
/// Memeriksa rekaman kamera 04 (laboratorium bawah tanah).
/// Menyelesaikan langkah pertama Misi 5: "Memeriksa rekaman CCTV mencurigakan".
/// </summary>
public class CctvMonitorInteractable : ProximityInteractable
{
    protected override string DefaultPrompt => "Periksa Rekaman CCTV";

    protected override void Interact()
    {
        var m = MissionManager.Instance;
        var ui = GameUI.Instance;
        if (m == null || ui == null) return;

        m.CheckedCctv();

        ui.ShowDocModal(
            "REKAMAN CCTV LABORATORIUM BAWAH TANAH",
            "[REKAMAN KAMERA 04 — PUKUL 08:15 WIB]",
            "<color=#FF5260><b>[STATUS: PEMUTARAN ARSIP REKAMAN SISTEM KEAMANAN]</b></color>\n\n" +
            "Di layar terlihat seorang peneliti berjas lab putih menyuntikkan serum hijau berpendar ke seorang subjek manusia di ruang isolasi steril bawah tanah gedung sekolah.\n\n" +
            "Beberapa menit kemudian, subjek mengalami kejang hebat, urat-uratnya menghitam, lalu mendobrak kaca pengaman isolasi dan menyerang peneliti secara agresif!\n\n" +
            "Sebelum kamera terganggu statis, terlihat sebuah map dokumen bertuliskan:\n" +
            "<color=#FFC43D><b>\"PROJECT Z — EXPERIMENT X\"</b></color>\n\n" +
            "<i>Petunjuk: Dokumen rahasia ini tersimpan di brankas arsip ruang operator. Periksa lemari dokumen di sebelah kanan!</i>",
            new Color32(0, 210, 211, 255),
            () =>
            {
                ui.ShowToast("Rekaman selesai. Sekarang periksa brankas dokumen rahasia!", false, 3.2f);
            });
    }
}

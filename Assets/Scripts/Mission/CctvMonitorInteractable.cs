using UnityEngine;

/// <summary>
/// Monitor CCTV di dinding atas Ruang Operator.
/// Mendukung 5 kamera berbeda (Kamera 01 - 05).
/// Kamera 04 memutar rekaman laboratorium bawah tanah untuk menyelesaikan langkah Misi 5.
/// </summary>
public class CctvMonitorInteractable : ProximityInteractable
{
    public int cameraNumber = 4;
    public string cameraName = "Lab Bawah Tanah";

    public void Setup(int number, string name)
    {
        cameraNumber = number;
        cameraName = name;
    }

    protected override string DefaultPrompt => $"Periksa Monitor {cameraNumber:00} ({cameraName})";

    protected override void Interact()
    {
        var m = MissionManager.Instance;
        var ui = GameUI.Instance;
        if (m == null || ui == null) return;

        if (cameraNumber == 4)
        {
            if (!m.Mission4Complete)
            {
                ui.ShowToast("Layar monitor 04 belum menyala. Hubungi kepolisian lewat telepon di meja operator dulu.", true, 3.2f);
                return;
            }

            m.CheckedCctv();

            ui.ShowDocModal(
                "REKAMAN CCTV LABORATORIUM BAWAH TANAH",
                "[REKAMAN KAMERA 04 — PUKUL 08:15 WIB]",
                "<color=#FF5260><b>[STATUS: PEMUTARAN ARSIP REKAMAN SISTEM KEAMANAN]</b></color>\n\n" +
                "Di layar monitor 04 terlihat seorang peneliti berjas lab putih menyuntikkan serum hijau berpendar ke seorang subjek manusia di ruang isolasi steril bawah tanah gedung sekolah.\n\n" +
                "Beberapa menit kemudian, subjek mengalami kejang hebat, urat-uratnya menghitam, lalu mendobrak kaca pengaman isolasi dan menyerang peneliti secara agresif!\n\n" +
                "Sebelum kamera terputus statis, terlihat sebuah map dokumen di meja lab bertuliskan:\n" +
                "<color=#FFC43D><b>\"PROJECT Z — EXPERIMENT X\"</b></color>\n\n" +
                "<i>Petunjuk: Salinan dokumen rahasia ini tersimpan di brankas arsip ruangan ini. Periksa brankas di sudut kanan ruangan!</i>",
                new Color32(0, 210, 211, 255),
                () =>
                {
                    ui.ShowToast("Rekaman Kamera 04 selesai. Sekarang periksa brankas dokumen rahasia di meja!", false, 3.2f);
                });
        }
        else if (cameraNumber == 1)
        {
            ui.ShowToast("[KAMERA 01 — GERBANG UTAMA] Gerbang terkunci. Terlihat mobil patroli polisi terbakar dan kerumunan zombie di kejauhan.", false, 3.5f);
        }
        else if (cameraNumber == 2)
        {
            ui.ShowToast("[KAMERA 02 — KELAS AWAL] Ruang kelas awal tampak kosong dan hancur. Jendela kaca jebol.", false, 3.5f);
        }
        else if (cameraNumber == 3)
        {
            ui.ShowToast("[KAMERA 03 — LORONG UTAMA] Kerumunan zombie berkumpul di dekat toilet sekolah mengikuti sumber suara bel.", false, 3.5f);
        }
        else if (cameraNumber == 5)
        {
            ui.ShowToast("[KAMERA 05 — TANGGA DARURAT] Pintu tangga darurat di sebelah kanan tampak aman dan bersih dari zombie. Jalur evakuasi terbaik!", false, 3.5f);
        }
    }
}

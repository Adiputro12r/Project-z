using UnityEngine;

/// <summary>
/// Meja kepala sekolah. Diperiksa -> muncul soal kode panel bel sekolah (512 + 512 = 1024)
/// dan langkah "cari kode" selesai.
/// </summary>
public class PrincipalDeskInteractable : ProximityInteractable
{
    protected override string DefaultPrompt => "Periksa Meja Kepala Sekolah";

protected override void Interact()
    {
        var m = MissionManager.Instance;
        var ui = GameUI.Instance;
        if (m == null || ui == null) return;

        m.FoundBellCode();
        ui.ShowCustomNote(
            "MEJA KEPALA SEKOLAH",
            "Di bawah tumpukan berkas terselip soal kode panel bel sekolah:",
            "Berapa hasil <color=#FFC43D>512 + 512</color>?");
    }
}

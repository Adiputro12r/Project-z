using UnityEngine;

/// <summary>
/// Meja kepala sekolah. Diperiksa -> muncul catatan kode panel bel sekolah (0730)
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
            "Di bawah tumpukan berkas terselip jadwal bel sekolah:",
            "Kode Panel Bel: <color=#FFC43D>" + CorridorState.BellCode + "</color>");
    }
}

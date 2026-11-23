using UnityEngine;

/// <summary>
/// Panel bel sekolah di lorong. Dibuka lewat keypad 4 digit (kode ada di meja kepala sekolah).
/// Kode benar -> bel berbunyi dan semua zombie berkumpul di sekitar bel.
/// Kalau kodenya belum ditemukan, angka apa pun akan salah (sama seperti laci di Misi 1).
/// </summary>
[RequireComponent(typeof(SchoolBell))]
public class BellPanelInteractable : ProximityInteractable
{
    private const int CodeDigits = 4;

    protected override string DefaultPrompt => "Buka Panel Bel Sekolah";

    protected override bool CanInteract => !CorridorState.BellActive;

    protected override void Interact()
    {
        var ui = GameUI.Instance;
        if (ui == null || MissionManager.Instance == null) return;

        ui.OpenKeypad(
            "PANEL BEL SEKOLAH",
            "Panel bel dikunci kode " + CodeDigits + " digit.\n\nJadwal bel biasanya disimpan kepala sekolah di mejanya.",
            ValidateCode,
            OnSolved,
            CodeDigits);
    }

    private static bool ValidateCode(string entered)
    {
        var m = MissionManager.Instance;
        return m != null && m.HasBellCode && entered == CorridorState.BellCode;
    }

    private static void OnSolved()
    {
        var m = MissionManager.Instance;
        if (m != null) m.RangBell();

        CorridorState.RingBell();

        var ui = GameUI.Instance;
        if (ui != null) ui.ShowToast("Bel berbunyi! Zombie bergerak menuju bel.", false, 3.2f);
    }
}

using UnityEngine;

/// <summary>Papan pengumuman: dibaca -> muncul foto pintu darurat + petunjuk, langkah 1 selesai.</summary>
public class AnnouncementBoardInteractable : ProximityInteractable
{
    protected override string DefaultPrompt => "Baca Papan Pengumuman";

    protected override void Interact()
    {
        var m = MissionManager.Instance;
        var ui = GameUI.Instance;
        if (m == null || ui == null) return;

        m.ReadNotice();
        ui.ShowAnnouncement();
    }
}

using UnityEngine;

/// <summary>Papan tulis: dibaca -> muncul soal tahun kemerdekaan (jawabannya = kode laci) dan langkah 1 selesai.</summary>
public class ClueBoardInteractable : ProximityInteractable
{
    protected override string DefaultPrompt => "Periksa Papan Tulis";

protected override void Interact()
    {
        var m = MissionManager.Instance;
        var ui = GameUI.Instance;
        if (m == null || ui == null) return;

        m.FoundClue();
        ui.ShowBoardNote("Berapakah tahun <color=#FFC43D>Indonesia merdeka</color>?");
    }
}

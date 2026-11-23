using UnityEngine;

/// <summary>
/// Meja siswa tempat kode disimpan. Baru memunculkan kode kalau papan pengumuman sudah dibaca
/// (seperti papan tulis di Misi 1). Langkah 2 selesai.
/// </summary>
public class DeskCodeInteractable : ProximityInteractable
{
    protected override string DefaultPrompt => "Periksa Meja";

    protected override void Interact()
    {
        var m = MissionManager.Instance;
        var ui = GameUI.Instance;
        if (m == null || ui == null) return;

        if (!m.NoticeRead)
        {
            ui.ShowToast("Hanya meja biasa. Mungkin ada petunjuk di papan pengumuman.", false, 2.8f);
            return;
        }

        m.FoundDeskCode();
        ui.ShowDeskNote(MissionManager.EmergencyWordHint);
    }
}

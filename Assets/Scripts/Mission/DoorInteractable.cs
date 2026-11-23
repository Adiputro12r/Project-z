using UnityEngine;

/// <summary>Pintu penghubung: butuh kunci. Kalau ada -> langkah 4 dicoret lalu pindah ke scene berikutnya.</summary>
public class DoorInteractable : ProximityInteractable
{
    private bool used;

    protected override string DefaultPrompt => "Buka Pintu Penghubung";
    protected override bool CanInteract => !used;

    protected override void Interact()
    {
        var m = MissionManager.Instance;
        var ui = GameUI.Instance;
        if (m == null || ui == null || used) return;

        if (!m.HasDoorKey)
        {
            ui.ShowToast("Pintu terkunci. Butuh Kunci Pintu Penghubung.", true);
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(MissionManager.NextSceneName))
        {
            ui.ShowToast("Scene \"" + MissionManager.NextSceneName + "\" belum ada di Build Settings.", true, 3.5f);
            return;
        }

        used = true;
        m.UseDoorKey();                            // langkah 4 dicoret + simbol kunci hilang
        ui.ShowToast("Pintu terbuka...", false, 1.2f);
        ui.TransitionToScene(MissionManager.NextSceneName, 1.2f);
    }
}

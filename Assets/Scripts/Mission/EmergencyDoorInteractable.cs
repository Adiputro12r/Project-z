using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Pintu darurat. Urutannya sama seperti laci + pintu di Misi 1:
/// 1) Panel kata sandi: ketik terjemahan bahasa Inggris dari tulisan di meja -> panel terbuka, lalu kunci didapat.
/// 2) Dengan kunci: pintu terbuka dan pindah ke scene lorong.
/// </summary>
public class EmergencyDoorInteractable : ProximityInteractable
{
    private bool used;

    protected override string DefaultPrompt
    {
        get
        {
            var m = MissionManager.Instance;
            return m != null && m.PanelUnlocked ? "Buka Pintu Darurat" : "Buka Panel Pintu Darurat";
        }
    }

    protected override bool CanInteract => !used;

    protected override void Interact()
    {
        var m = MissionManager.Instance;
        var ui = GameUI.Instance;
        if (m == null || ui == null || used) return;

        // Tahap 1: panel kode
        if (!m.PanelUnlocked)
        {
            ui.OpenWordEntry(
                "PINTU DARURAT",
                "Panel dikunci kata sandi.\n\nKetik BAHASA INGGRIS dari kata yang tertulis di salah satu meja kelas.",
                ValidateWord,
                OnSolved);
            return;
        }

        // Tahap 2: butuh kunci
        if (!m.HasDoorKey)
        {
            ui.ShowToast("Pintu terkunci. Butuh Kunci Pintu Darurat.", true);
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(MissionManager.LorongSceneName))
        {
            ui.ShowToast("Scene \"" + MissionManager.LorongSceneName + "\" belum ada di Build Settings.", true, 3.5f);
            return;
        }

        used = true;
        m.UseEmergencyKey();                          // langkah 5 dicoret + simbol kunci hilang
        ui.ShowToast("Pintu darurat terbuka...", false, 1.2f);
        ui.TransitionToScene(MissionManager.LorongSceneName, 1.2f);
    }

    private static bool ValidateWord(string entered)
    {
        var m = MissionManager.Instance;
        return m != null && m.HasDeskCode &&
               string.Equals(entered.Trim(), MissionManager.EmergencyWordAnswer, StringComparison.OrdinalIgnoreCase);
    }

    private void OnSolved()
    {
        MissionManager.Instance.UnlockPanel();        // langkah 3 dicoret
        StartCoroutine(GiveKey());
    }

    private IEnumerator GiveKey()
    {
        yield return new WaitForSeconds(0.45f);
        MissionManager.Instance.TakeEmergencyKey();   // langkah 4 dicoret + simbol kunci muncul di atas player
        if (GameUI.Instance != null)
            GameUI.Instance.ShowToast("Kamu mendapatkan Kunci Pintu Darurat", false, 2.8f);
    }
}

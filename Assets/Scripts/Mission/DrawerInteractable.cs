using System.Collections;
using UnityEngine;

/// <summary>
/// Laci guru: buka keypad. Kode benar (dan petunjuk sudah ditemukan) -> laci terbuka lalu kunci didapat.
/// Kalau petunjuk belum ditemukan, angka apa pun yang dimasukkan akan salah.
/// </summary>
public class DrawerInteractable : ProximityInteractable
{
    protected override string DefaultPrompt => "Buka Laci Meja Guru";

    protected override bool CanInteract =>
        MissionManager.Instance != null && !MissionManager.Instance.DrawerUnlocked;

    protected override void Interact()
    {
        var ui = GameUI.Instance;
        if (ui == null || MissionManager.Instance == null) return;

        ui.OpenKeypad(
            "LACI MEJA GURU",
            "Laci ini dikunci gembok " + KeypadPanel.CodeLength + " digit.\n\nGuru biasanya menulis catatan penting di papan tulis kelas.",
            ValidateCode,
            OnSolved);
    }

    private static bool ValidateCode(string entered)
    {
        var m = MissionManager.Instance;
        return m != null && m.HasClue && entered == MissionManager.DrawerCode;
    }

    private void OnSolved()
    {
        MissionManager.Instance.UnlockDrawer();   // langkah 2 dicoret
        StartCoroutine(GiveKey());
    }

    private IEnumerator GiveKey()
    {
        yield return new WaitForSeconds(0.45f);
        MissionManager.Instance.TakeDoorKey();     // langkah 3 dicoret + simbol kunci muncul di atas player
        if (GameUI.Instance != null)
            GameUI.Instance.ShowToast("Kamu mendapatkan Kunci Pintu Penghubung", false, 2.8f);
    }
}

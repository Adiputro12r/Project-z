using System;
using UnityEngine;

/// <summary>Bagian GameUI untuk panel kata sandi (ketik teks) pintu darurat.</summary>
public partial class GameUI
{
    private WordEntryPanel wordPanel;

    private void BuildWordPanel()
    {
        var rt = UIFactory.NewRect("WordEntry", canvasRt);
        wordPanel = rt.gameObject.AddComponent<WordEntryPanel>();
        wordPanel.Build();
        rt.gameObject.SetActive(false);
    }

    public void OpenWordEntry(string header, string note, Func<string, bool> validate, Action onSolved)
    {
        if (wordPanel == null || ModalOpen) return;
        wordPanel.Open(header, note, validate, onSolved);
    }
}

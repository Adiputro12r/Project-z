using UnityEngine;

/// <summary>Bagian GameUI untuk lorong: catatan serbaguna dan status transisi scene.</summary>
public partial class GameUI
{
    public bool IsTransitioning => transitioning;

    /// <summary>Catatan bergaya papan tulis dengan judul, isi, dan satu baris kode (boleh pakai rich text).</summary>
    public void ShowCustomNote(string title, string body, string codeLine)
    {
        noteTitle.text = title;
        noteBody.text = body;
        noteCode.text = codeLine;
        noteRoot.gameObject.SetActive(true);
        noteOpen = true;
        PlayerFreeze.Set(true);
        StartCoroutine(UIFactory.Pop(noteFrame.Root));
    }
}

using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Bagian GameUI untuk Misi 5: Modal pembaca dokumen / rekaman CCTV beresolusi tinggi
/// dan modal kemenangan / epilog pelarian keluar sekolah.
/// </summary>
public partial class GameUI
{
    // Modal Reader (CCTV & Dokumen Rahasia)
    private RectTransform docModalRoot;
    private UIFactory.BorderedBox docModalFrame;
    private Text docModalTitle;
    private Text docModalBadge;
    private Text docModalBody;
    private bool docModalOpen;
    private Action docModalOnClose;

    // Modal Victory (Klimaks Pelarian Keluar Sekolah)
    private RectTransform victoryModalRoot;
    private UIFactory.BorderedBox victoryModalFrame;
    private Text victoryModalTitle;
    private Text victoryModalBadge;
    private Text victoryModalBody;
    private bool victoryModalOpen;

    private void BuildDocModal()
    {
        docModalRoot = UIFactory.NewRect("DocModal", canvasRt);
        UIFactory.Stretch(docModalRoot);
        var dim = docModalRoot.gameObject.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.78f);
        dim.raycastTarget = true;

        docModalFrame = UIFactory.Bordered("Frame", docModalRoot, new Color32(10, 14, 26, 255),
            new Color32(38, 50, 84, 255), 4, 18, true);
        UIFactory.PlaceCenter(docModalFrame.Root, 0, 0, 1140, 640);
        var inner = docModalFrame.Inner;

        // Badge
        var badgeBox = UIFactory.Bordered("BadgeBox", inner, new Color32(20, 28, 48, 255),
            new Color32(0, 210, 211, 255), 2, 8);
        UIFactory.PlaceCenter(badgeBox.Root, 0, 245, 620, 38);
        docModalBadge = UIFactory.Label("Badge", badgeBox.Inner, "", 20, new Color32(0, 210, 211, 255), TextAnchor.MiddleCenter);
        UIFactory.Stretch(docModalBadge.rectTransform);

        // Title
        docModalTitle = UIFactory.Label("Title", inner, "", 32, Gold, TextAnchor.MiddleCenter);
        UIFactory.PlaceCenter(docModalTitle.rectTransform, 0, 195, 1020, 48);

        // Divider
        var div = UIFactory.Box("Divider", inner, PanelBorder);
        UIFactory.PlaceCenter(div.rectTransform, 0, 160, 1040, 2);

        // Body content
        docModalBody = UIFactory.Label("Body", inner, "", 24, TextMain, TextAnchor.UpperLeft, FontStyle.Normal);
        UIFactory.PlaceCenter(docModalBody.rectTransform, 0, -10, 1040, 310);

        // Close Button
        var btn = UIFactory.MakeButton("CloseBtn", inner, "TUTUP DOKUMEN", 26, new Color32(28, 40, 72, 255),
            Gold, Color.white, CloseDocModal, 12, 3);
        UIFactory.PlaceCenter((RectTransform)btn.transform, 0, -220, 280, 56);

        // Hint
        var hint = UIFactory.Label("Hint", inner, "[E] atau [Esc] untuk menutup", 20, TextDim, TextAnchor.MiddleCenter, FontStyle.Normal);
        UIFactory.PlaceCenter(hint.rectTransform, 0, -268, 600, 26);

        docModalRoot.gameObject.SetActive(false);
    }

    public void ShowDocModal(string title, string badge, string body, Color accentColor, Action onClose = null)
    {
        docModalTitle.text = title;
        docModalBadge.text = badge;
        docModalBadge.color = accentColor;
        docModalBody.text = body;
        docModalFrame.Border.color = accentColor;
        docModalOnClose = onClose;

        docModalRoot.gameObject.SetActive(true);
        docModalOpen = true;
        PlayerFreeze.Set(true);
        StartCoroutine(UIFactory.Pop(docModalFrame.Root));
    }

    public void CloseDocModal()
    {
        if (!docModalOpen) return;
        docModalOpen = false;
        docModalRoot.gameObject.SetActive(false);
        PlayerFreeze.Set(false);
        var cb = docModalOnClose;
        docModalOnClose = null;
        cb?.Invoke();
    }

    private void HandleDocModalInput()
    {
        if (!docModalOpen) return;
        var kb = Keyboard.current;
        if (kb == null) return;
        if (kb.eKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)
        {
            CloseDocModal();
        }
    }

    private void BuildVictoryModal()
    {
        victoryModalRoot = UIFactory.NewRect("VictoryModal", canvasRt);
        UIFactory.Stretch(victoryModalRoot);
        var dim = victoryModalRoot.gameObject.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.88f);
        dim.raycastTarget = true;

        victoryModalFrame = UIFactory.Bordered("Frame", victoryModalRoot, new Color32(8, 14, 28, 255),
            Green, 4, 20, true);
        UIFactory.PlaceCenter(victoryModalFrame.Root, 0, 0, 1160, 660);
        var inner = victoryModalFrame.Inner;

        // Badge
        var badgeBox = UIFactory.Bordered("BadgeBox", inner, new Color32(16, 36, 26, 255),
            Green, 2, 8);
        UIFactory.PlaceCenter(badgeBox.Root, 0, 255, 600, 40);
        victoryModalBadge = UIFactory.Label("Badge", badgeBox.Inner, "MISI SELESAI — SELAMAT!", 22, Green, TextAnchor.MiddleCenter);
        UIFactory.Stretch(victoryModalBadge.rectTransform);

        // Title
        victoryModalTitle = UIFactory.Label("Title", inner, "BERHASIL KELUAR DARI KAWASAN SEKOLAH", 34, Gold, TextAnchor.MiddleCenter);
        UIFactory.PlaceCenter(victoryModalTitle.rectTransform, 0, 200, 1060, 50);

        var div = UIFactory.Box("Divider", inner, Green);
        UIFactory.PlaceCenter(div.rectTransform, 0, 162, 1060, 2);

        // Body
        victoryModalBody = UIFactory.Label("Body", inner, "", 24, TextMain, TextAnchor.UpperLeft, FontStyle.Normal);
        UIFactory.PlaceCenter(victoryModalBody.rectTransform, 0, -10, 1040, 300);

        // Restart button
        var restartBtn = UIFactory.MakeButton("RestartBtn", inner, "MAIN LAGI (RESTART)", 26,
            new Color32(24, 78, 48, 255), Green, Color.white, () =>
            {
                CorridorState.Reset();
                CloseVictoryModal();
                TransitionToScene("SampleScene", 0.5f);
            }, 14, 3);
        UIFactory.PlaceCenter((RectTransform)restartBtn.transform, 0, -220, 360, 64);

        victoryModalRoot.gameObject.SetActive(false);
    }

    public void ShowVictoryModal(string title, string badge, string body)
    {
        victoryModalTitle.text = title;
        victoryModalBadge.text = badge;
        victoryModalBody.text = body;

        victoryModalRoot.gameObject.SetActive(true);
        victoryModalOpen = true;
        PlayerFreeze.Set(true);
        StartCoroutine(UIFactory.Pop(victoryModalFrame.Root));
    }

    public void CloseVictoryModal()
    {
        if (!victoryModalOpen) return;
        victoryModalOpen = false;
        victoryModalRoot.gameObject.SetActive(false);
        PlayerFreeze.Set(false);
    }

    private void HandleVictoryModalInput()
    {
        if (!victoryModalOpen) return;
    }
}

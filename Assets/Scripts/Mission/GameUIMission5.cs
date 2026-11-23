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
        dim.color = new Color(0.02f, 0.04f, 0.08f, 0.94f);
        dim.raycastTarget = true;

        docModalFrame = UIFactory.Bordered("Frame", docModalRoot, new Color32(10, 14, 26, 255),
            new Color32(38, 50, 84, 255), 4, 20, true);
        UIFactory.PlaceCenter(docModalFrame.Root, 0, 0, 1220, 820);
        var inner = docModalFrame.Inner;

        // Badge (Kategori / Klasifikasi)
        var badgeBox = UIFactory.Bordered("BadgeBox", inner, new Color32(18, 26, 46, 255),
            new Color32(0, 210, 211, 255), 2, 8);
        UIFactory.PlaceCenter(badgeBox.Root, 0, 350, 660, 38);
        docModalBadge = UIFactory.Label("Badge", badgeBox.Inner, "", 19, new Color32(0, 210, 211, 255), TextAnchor.MiddleCenter);
        UIFactory.Stretch(docModalBadge.rectTransform);

        // Title
        docModalTitle = UIFactory.Label("Title", inner, "", 30, Gold, TextAnchor.MiddleCenter);
        UIFactory.PlaceCenter(docModalTitle.rectTransform, 0, 300, 1120, 44);

        // Divider Atas
        var divTop = UIFactory.Box("DividerTop", inner, PanelBorder);
        UIFactory.PlaceCenter(divTop.rectTransform, 0, 265, 1140, 2);

        // Body content (Area membaca dokumen yang luas dan terpisah jauh dari tombol)
        docModalBody = UIFactory.Label("Body", inner, "", 19, TextMain, TextAnchor.UpperLeft, FontStyle.Normal);
        docModalBody.lineSpacing = 1.22f;
        UIFactory.PlaceCenter(docModalBody.rectTransform, 0, 45, 1100, 410);

        // Divider Bawah
        var divBottom = UIFactory.Box("DividerBottom", inner, PanelBorder);
        UIFactory.PlaceCenter(divBottom.rectTransform, 0, -185, 1140, 2);

        // Close Button (Tersendiri di footer, tidak akan menabrak teks)
        var btn = UIFactory.MakeButton("CloseBtn", inner, "TUTUP DOKUMEN", 24, new Color32(24, 34, 62, 255),
            Gold, Color.white, CloseDocModal, 12, 3);
        UIFactory.PlaceCenter((RectTransform)btn.transform, 0, -245, 320, 56);

        // Hint
        var hint = UIFactory.Label("Hint", inner, "[E] atau [Esc] untuk menutup", 18, TextDim, TextAnchor.MiddleCenter, FontStyle.Normal);
        UIFactory.PlaceCenter(hint.rectTransform, 0, -300, 600, 26);

        docModalRoot.gameObject.SetActive(false);
    }

    public void ShowDocModal(string title, string badge, string body, Color accentColor, Action onClose = null)
    {
        if (canvasRt == null) BuildCanvas();
        if (docModalFrame == null || docModalFrame.Border == null || docModalRoot == null)
            BuildDocModal();

        docModalTitle.text = title;
        docModalBadge.text = badge;
        docModalBadge.color = accentColor;
        docModalBody.text = body;
        docModalFrame.Border.color = accentColor;
        docModalOnClose = onClose;

        docModalRoot.SetAsLastSibling();
        docModalRoot.gameObject.SetActive(true);
        docModalOpen = true;
        PlayerFreeze.Set(true);

        if (missionBox != null && missionBox.Root != null)
            missionBox.Root.gameObject.SetActive(false);

        StartCoroutine(UIFactory.Pop(docModalFrame.Root));
    }

    public void CloseDocModal()
    {
        if (!docModalOpen) return;
        docModalOpen = false;
        docModalRoot.gameObject.SetActive(false);
        PlayerFreeze.Set(false);

        if (missionBox != null && missionBox.Root != null)
            missionBox.Root.gameObject.SetActive(true);

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
        dim.color = new Color(0.02f, 0.04f, 0.08f, 0.94f);
        dim.raycastTarget = true;

        victoryModalFrame = UIFactory.Bordered("Frame", victoryModalRoot, new Color32(8, 14, 28, 255),
            Green, 4, 20, true);
        UIFactory.PlaceCenter(victoryModalFrame.Root, 0, 0, 1220, 820);
        var inner = victoryModalFrame.Inner;

        // Badge
        var badgeBox = UIFactory.Bordered("BadgeBox", inner, new Color32(16, 36, 26, 255),
            Green, 2, 8);
        UIFactory.PlaceCenter(badgeBox.Root, 0, 350, 660, 38);
        victoryModalBadge = UIFactory.Label("Badge", badgeBox.Inner, "MISI SELESAI — SELAMAT!", 19, Green, TextAnchor.MiddleCenter);
        UIFactory.Stretch(victoryModalBadge.rectTransform);

        // Title
        victoryModalTitle = UIFactory.Label("Title", inner, "BERHASIL KELUAR DARI KAWASAN SEKOLAH", 30, Gold, TextAnchor.MiddleCenter);
        UIFactory.PlaceCenter(victoryModalTitle.rectTransform, 0, 300, 1120, 44);

        // Divider Atas
        var divTop = UIFactory.Box("DividerTop", inner, Green);
        UIFactory.PlaceCenter(divTop.rectTransform, 0, 265, 1140, 2);

        // Body
        victoryModalBody = UIFactory.Label("Body", inner, "", 19, TextMain, TextAnchor.UpperLeft, FontStyle.Normal);
        victoryModalBody.lineSpacing = 1.22f;
        UIFactory.PlaceCenter(victoryModalBody.rectTransform, 0, 45, 1100, 410);

        // Divider Bawah
        var divBottom = UIFactory.Box("DividerBottom", inner, Green);
        UIFactory.PlaceCenter(divBottom.rectTransform, 0, -185, 1140, 2);

        // Restart button
        var restartBtn = UIFactory.MakeButton("RestartBtn", inner, "MAIN LAGI (RESTART)", 24,
            new Color32(24, 78, 48, 255), Green, Color.white, () =>
            {
                CorridorState.Reset();
                CloseVictoryModal();
                TransitionToScene("SampleScene", 0.5f);
            }, 14, 3);
        UIFactory.PlaceCenter((RectTransform)restartBtn.transform, 0, -245, 380, 58);

        // Hint
        var hint = UIFactory.Label("Hint", inner, "[Enter] atau klik tombol untuk mengulang", 18, TextDim, TextAnchor.MiddleCenter, FontStyle.Normal);
        UIFactory.PlaceCenter(hint.rectTransform, 0, -300, 600, 26);

        victoryModalRoot.gameObject.SetActive(false);
    }

    public void ShowVictoryModal(string title, string badge, string body)
    {
        if (canvasRt == null) BuildCanvas();
        if (victoryModalFrame == null || victoryModalFrame.Border == null || victoryModalRoot == null)
            BuildVictoryModal();

        victoryModalTitle.text = title;
        victoryModalBadge.text = badge;
        victoryModalBody.text = body;

        victoryModalRoot.SetAsLastSibling();
        victoryModalRoot.gameObject.SetActive(true);
        victoryModalOpen = true;
        PlayerFreeze.Set(true);

        if (missionBox != null && missionBox.Root != null)
            missionBox.Root.gameObject.SetActive(false);

        StartCoroutine(UIFactory.Pop(victoryModalFrame.Root));
    }

    public void CloseVictoryModal()
    {
        if (!victoryModalOpen) return;
        victoryModalOpen = false;
        victoryModalRoot.gameObject.SetActive(false);
        PlayerFreeze.Set(false);

        if (missionBox != null && missionBox.Root != null)
            missionBox.Root.gameObject.SetActive(true);
    }

    private void HandleVictoryModalInput()
    {
        if (!victoryModalOpen) return;
    }
}

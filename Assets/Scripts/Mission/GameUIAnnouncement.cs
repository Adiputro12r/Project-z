using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Bagian GameUI untuk Misi 2: panel papan pengumuman (foto pintu darurat + petunjuk)
/// dan catatan kode di meja kelas.
/// </summary>
public partial class GameUI
{
    // Judul + isi catatan (dipakai bersama oleh catatan papan tulis dan catatan meja)
    private Text noteTitle;
    private Text noteBody;

    // Papan pengumuman
    private RectTransform announceRoot;
    private UIFactory.BorderedBox announceFrame;
    private bool announceOpen;

    // ------------------------------------------------------------------ catatan meja

    public void ShowDeskNote(string code)
    {
        noteTitle.text = "CATATAN DI MEJA SISWA";
        noteBody.text = "Kertas di bawah buku piket bertuliskan:";
        noteCode.text = "<color=#FFC43D>" + code + "</color> = ?";
        noteRoot.gameObject.SetActive(true);
        noteOpen = true;
        PlayerFreeze.Set(true);
        StartCoroutine(UIFactory.Pop(noteFrame.Root));
    }

    // ------------------------------------------------------------------ papan pengumuman

    private void BuildAnnouncement()
    {
        announceRoot = UIFactory.NewRect("Announcement", canvasRt);
        UIFactory.Stretch(announceRoot);
        var dim = announceRoot.gameObject.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.7f);
        dim.raycastTarget = true;

        // Papan gabus dengan bingkai kayu
        announceFrame = UIFactory.Bordered("Frame", announceRoot, new Color32(176, 128, 82, 255),
            new Color32(94, 62, 36, 255), 16, 26, true);
        UIFactory.PlaceCenter(announceFrame.Root, 0, 0, 1240, 740);
        var inner = announceFrame.Inner;

        var ink = new Color32(48, 34, 28, 255);
        var red = new Color32(150, 20, 30, 255);
        var paper = new Color32(244, 236, 214, 255);
        var paperEdge = new Color32(205, 190, 160, 255);
        var tapeColor = new Color32(215, 215, 220, 170);

        // Judul
        var head = UIFactory.Bordered("Head", inner, paper, paperEdge, 3, 8);
        UIFactory.PlaceCenter(head.Root, 0, 292, 620, 76);
        var title = UIFactory.Label("Title", head.Inner, "PAPAN PENGUMUMAN", 44, ink, TextAnchor.MiddleCenter);
        UIFactory.Stretch(title.rectTransform);

        // Foto pintu darurat (kiri)
        var photo = UIFactory.Bordered("Photo", inner, new Color32(250, 248, 240, 255),
            new Color32(200, 195, 180, 255), 4, 6);
        UIFactory.PlaceCenter(photo.Root, -310, -12, 372, 500);
        photo.Root.localRotation = Quaternion.Euler(0f, 0f, -3f);

        var pic = UIFactory.Box("Pic", photo.Inner, Color.white);
        pic.sprite = DoorPhotoArt.GetSprite();
        pic.preserveAspect = true;
        UIFactory.PlaceCenter(pic.rectTransform, 0, 36, 320, 416);

        var caption = UIFactory.Label("Caption", photo.Inner, "PINTU DARURAT\n(bagian belakang kelas)", 26, ink,
            TextAnchor.MiddleCenter);
        UIFactory.PlaceCenter(caption.rectTransform, 0, -208, 340, 60);

        var tapePhoto = UIFactory.Box("Tape", photo.Root, tapeColor);
        UIFactory.Place(tapePhoto.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -2f), new Vector2(120f, 36f));
        tapePhoto.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 4f);

        // Kertas pengumuman (kanan)
        var sheet = UIFactory.Bordered("Sheet", inner, paper, paperEdge, 3, 8);
        UIFactory.PlaceCenter(sheet.Root, 250, -12, 600, 500);
        sheet.Root.localRotation = Quaternion.Euler(0f, 0f, 2f);

        var sheetTitle = UIFactory.Label("Title", sheet.Inner, "DENAH EVAKUASI", 40, red, TextAnchor.MiddleCenter);
        UIFactory.PlaceCenter(sheetTitle.rectTransform, 0, 205, 560, 56);
        var line = UIFactory.Box("Line", sheet.Inner, red);
        UIFactory.PlaceCenter(line.rectTransform, 0, 168, 440, 4);

        var body = UIFactory.Label("Body", sheet.Inner,
            "Jika terjadi keadaan darurat, semua siswa wajib keluar lewat pintu darurat di bagian belakang kelas (lihat foto)." +
            "\n\nPintu itu dikunci panel kata sandi. Jawabannya harus diketik dalam bahasa Inggris." +
            "\n\nPetunjuk katanya dititipkan pada salah satu meja siswa di kelas ini.",
            28, ink, TextAnchor.UpperLeft, FontStyle.Normal);
        UIFactory.PlaceCenter(body.rectTransform, 0, -30, 540, 330);

        var tapeSheet = UIFactory.Box("Tape", sheet.Root, tapeColor);
        UIFactory.Place(tapeSheet.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -2f), new Vector2(120f, 36f));
        tapeSheet.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -3f);

        // Tombol tutup
        var close = UIFactory.MakeButton("Close", inner, "TUTUP  [E]", 30, new Color32(94, 62, 36, 255),
            new Color32(60, 38, 20, 255), Color.white, CloseAnnouncement, 16, 4);
        UIFactory.PlaceCenter((RectTransform)close.transform, 0, -316, 280, 64);

        announceRoot.gameObject.SetActive(false);
    }

    public void ShowAnnouncement()
    {
        announceRoot.gameObject.SetActive(true);
        announceOpen = true;
        PlayerFreeze.Set(true);
        StartCoroutine(UIFactory.Pop(announceFrame.Root));
    }

    private void CloseAnnouncement()
    {
        if (!announceOpen) return;
        announceOpen = false;
        announceRoot.gameObject.SetActive(false);
        PlayerFreeze.Set(false);
    }

    private void HandleAnnouncementInput()
    {
        if (!announceOpen) return;
        var kb = Keyboard.current;
        if (kb == null) return;
        if (kb.eKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame ||
            kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)
            CloseAnnouncement();
    }
}

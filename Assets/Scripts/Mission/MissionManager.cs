using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Satu misi = judul + daftar langkah (ditampilkan di kiri atas layar).</summary>
[Serializable]
public class MissionDefinition
{
    public string title;
    public string[] steps;

    public MissionDefinition(string title, params string[] steps)
    {
        this.title = title;
        this.steps = steps;
    }
}

/// <summary>Index langkah Misi 1 (urutannya harus sama dengan daftar di MissionManager).</summary>
public static class Mission1Step
{
    public const int FindClue = 0;
    public const int OpenDrawer = 1;
    public const int TakeKey = 2;
    public const int OpenDoor = 3;
}

/// <summary>Index langkah Misi 2 (urutannya harus sama dengan daftar di MissionManager).</summary>
public static class Mission2Step
{
    public const int ReadNotice = 0;
    public const int FindCode = 1;
    public const int OpenPanel = 2;
    public const int TakeKey = 3;
    public const int OpenDoor = 4;
}

/// <summary>Index langkah Misi 5 (urutannya harus sama dengan Mission5 di MissionManager).</summary>
public static class Mission5Step
{
    public const int CheckCctv = 0;
    public const int FindDocument = 1;
    public const int ReadDocument = 2;
    public const int LeaveRoom = 3;
}

/// <summary>Index langkah Misi 6 (urutannya harus sama dengan Mission6 di MissionManager).</summary>
public static class Mission6Step
{
    public const int AvoidZombies = 0;
    public const int ApproachPatrolCar = 1;
    public const int EscapeSchoolGate = 2;
}

/// <summary>
/// Menyimpan state misi (petunjuk, laci, kunci) dan daftar langkah yang sudah selesai.
/// Hidup lintas scene (dibuat otomatis oleh GameBootstrap).
/// </summary>
public class MissionManager : MonoBehaviour
{
    // ====== EDIT DI SINI ======
    public const string DrawerCode = "194";
    public const string NextSceneName = "Scene2";
    public const string EmergencyWordHint = "KELUAR";      // tulisan di meja (bahasa Indonesia)
    public const string EmergencyWordAnswer = "EXIT";      // jawaban yang diketik di panel pintu (bahasa Inggris)
    public const string Scene2Name = "Scene2";
    public const string LorongSceneName = "Lorong";
    public const string KepsekSceneName = "RuangKepsek";
    public const string CctvSceneName = "RuangCCTV";
    public const string HalamanSceneName = "HalamanSekolah";

    /// <summary>Misi 3 dipakai bersama oleh Lorong dan RuangKepsek (progres tidak ke-reset waktu pindah).</summary>
    private static readonly MissionDefinition CorridorMission = new MissionDefinition(
        "MISI 3: Lorong",
        "Masuk ke ruang kepala sekolah",
        "Cari kode panel bel di meja kepala sekolah",
        "Aktifkan panel bel sekolah di lorong",
        "Masuk ke ruang CCTV");

    public static readonly MissionDefinition Mission5 = new MissionDefinition(
        "MISI 5: Rahasia Project Z",
        "Memeriksa rekaman CCTV mencurigakan",
        "Mencari dokumen rahasia",
        "Membaca dokumen PROJECT Z",
        "Tinggalkan ruang operator lewat tangga darurat");

    public static readonly MissionDefinition Mission6 = new MissionDefinition(
        "MISI 6: Pelarian Terakhir",
        "Menghindari kerumunan zombie di halaman",
        "Mendekati area mobil patroli",
        "Keluar melewati gerbang utama sekolah");

    /// <summary>Nama scene -> misi yang aktif di scene itu.</summary>
    private static readonly Dictionary<string, MissionDefinition> MissionByScene =
        new Dictionary<string, MissionDefinition>
        {
            {
                "SampleScene",
                new MissionDefinition(
                    "MISI 1: Kelas Awal",
                    "Mencari petunjuk kode",
                    "Membuka laci meja guru",
                    "Mengambil kunci pintu penghubung",
                    "Membuka pintu penghubung & pindah kelas")
            },
            {
                "Scene2",
                new MissionDefinition(
                    "MISI 2: Kelas Sebelah",
                    "Melihat papan pengumuman",
                    "Mencari kata sandi di meja kelas",
                    "Membuka panel pintu darurat",
                    "Mengambil kunci pintu darurat",
                    "Membuka pintu darurat & masuk lorong")
            },
            { "Lorong", CorridorMission },
            { "RuangKepsek", CorridorMission },
            { "RuangCCTV", Mission5 },
            { "HalamanSekolah", Mission6 },
        };
    private const string FirstSceneName = "SampleScene";
    // ==========================

    public static MissionManager Instance { get; private set; }

    public MissionDefinition Current { get; private set; }
    public bool HasClue { get; private set; }
    public bool DrawerUnlocked { get; private set; }
    public bool HasDoorKey { get; private set; }
    public bool NoticeRead { get; private set; }
    public bool HasDeskCode { get; private set; }
    public bool PanelUnlocked { get; private set; }
    public bool HasBellCode { get; private set; }
    public bool CctvChecked { get; private set; }
    public bool DocumentFound { get; private set; }
    public bool DocumentRead { get; private set; }
    public bool Mission5Complete { get; private set; }

    private bool corridorMissionActive;

    public event Action MissionChanged;
    public event Action<int> StepCompleted;
    public event Action KeyStateChanged;

    private bool[] done = new bool[0];

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => Instance = null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        SceneManager.sceneLoaded += OnSceneLoaded;
        BeginMissionFor(SceneManager.GetActiveScene().name);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this) Instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => BeginMissionFor(scene.name);

    private static bool IsCorridorScene(string sceneName) =>
        sceneName == LorongSceneName || sceneName == KepsekSceneName;

    /// <summary>Dipanggil CorridorState.Reset() (player mati / game diulang): misi 3 mulai dari awal lagi.</summary>
    public void ResetCorridorMission()
    {
        corridorMissionActive = false;
        HasBellCode = false;
    }

    private void BeginMissionFor(string sceneName)
    {
        if (IsCorridorScene(sceneName))
        {
            MissionDefinition cdef;
            MissionByScene.TryGetValue(sceneName, out cdef);
            Current = cdef;
            // Pindah Lorong <-> RuangKepsek: progres misi dipertahankan.
            if (!corridorMissionActive || cdef == null || done.Length != cdef.steps.Length)
            {
                corridorMissionActive = true;
                done = cdef != null ? new bool[cdef.steps.Length] : new bool[0];
                HasBellCode = false;
                HasDoorKey = false;
            }
            MissionChanged?.Invoke();
            KeyStateChanged?.Invoke();
            return;
        }

        corridorMissionActive = false;
        if (sceneName == FirstSceneName || sceneName == Scene2Name) CorridorState.Reset();

        if (sceneName == FirstSceneName)
        {
            HasClue = false;
            DrawerUnlocked = false;
            HasDoorKey = false;
        }
        else if (sceneName == Scene2Name)
        {
            NoticeRead = false;
            HasDeskCode = false;
            PanelUnlocked = false;
            HasDoorKey = false;
        }
        else if (sceneName == CctvSceneName)
        {
            CctvChecked = false;
            DocumentFound = false;
            DocumentRead = false;
            Mission5Complete = false;
        }

        MissionDefinition def;
        MissionByScene.TryGetValue(sceneName, out def);
        Current = def;
        done = def != null ? new bool[def.steps.Length] : new bool[0];
        MissionChanged?.Invoke();
        KeyStateChanged?.Invoke();
    }

    public bool IsStepDone(int index) => index >= 0 && index < done.Length && done[index];

    public void CompleteStep(int index)
    {
        if (index < 0 || index >= done.Length || done[index]) return;
        done[index] = true;
        StepCompleted?.Invoke(index);
    }

    // ---- Aksi gameplay Misi 3 (lorong) ----

    public void EnteredOffice() => CompleteStep(Mission3Step.EnterOffice);

    public void FoundBellCode()
    {
        HasBellCode = true;
        CompleteStep(Mission3Step.FindCode);
    }

    public void RangBell() => CompleteStep(Mission3Step.RingBell);

    public void EnteredCctv() => CompleteStep(Mission3Step.EnterCctv);

    // ---- Aksi gameplay Misi 1 ----

    public void FoundClue()
    {
        HasClue = true;
        CompleteStep(Mission1Step.FindClue);
    }

    public void UnlockDrawer()
    {
        DrawerUnlocked = true;
        CompleteStep(Mission1Step.OpenDrawer);
    }

    public void TakeDoorKey()
    {
        HasDoorKey = true;
        CompleteStep(Mission1Step.TakeKey);
        KeyStateChanged?.Invoke();
    }

    public void UseDoorKey()
    {
        HasDoorKey = false;
        CompleteStep(Mission1Step.OpenDoor);
        KeyStateChanged?.Invoke();
    }

    // ---- Aksi gameplay Misi 2 ----

    public void ReadNotice()
    {
        NoticeRead = true;
        CompleteStep(Mission2Step.ReadNotice);
    }

    public void FoundDeskCode()
    {
        HasDeskCode = true;
        CompleteStep(Mission2Step.FindCode);
    }

    public void UnlockPanel()
    {
        PanelUnlocked = true;
        CompleteStep(Mission2Step.OpenPanel);
    }

    public void TakeEmergencyKey()
    {
        HasDoorKey = true;
        CompleteStep(Mission2Step.TakeKey);
        KeyStateChanged?.Invoke();
    }

    public void UseEmergencyKey()
    {
        HasDoorKey = false;
        CompleteStep(Mission2Step.OpenDoor);
        KeyStateChanged?.Invoke();
    }

    // ---- Aksi gameplay Misi 5 (CCTV & Rahasia Project Z) ----

    public void CheckedCctv()
    {
        CctvChecked = true;
        CompleteStep(Mission5Step.CheckCctv);
    }

    public void FoundDocument()
    {
        DocumentFound = true;
        CompleteStep(Mission5Step.FindDocument);
    }

    public void ReadDocument()
    {
        DocumentFound = true;
        DocumentRead = true;
        CompleteStep(Mission5Step.FindDocument);
        CompleteStep(Mission5Step.ReadDocument);
    }

    public void LeftRoom()
    {
        Mission5Complete = true;
        CompleteStep(Mission5Step.LeaveRoom);
    }

    // ---- Aksi gameplay Misi 6 (Halaman Sekolah / The Final Escape) ----

    public bool PatrolCarApproached { get; private set; }
    public bool EscapedGate { get; private set; }

    public void EvadedZombies() => CompleteStep(Mission6Step.AvoidZombies);

    public void ApproachedPatrolCar()
    {
        PatrolCarApproached = true;
        CompleteStep(Mission6Step.AvoidZombies);
        CompleteStep(Mission6Step.ApproachPatrolCar);
    }

    public void EscapedSchool()
    {
        EscapedGate = true;
        CompleteStep(Mission6Step.AvoidZombies);
        CompleteStep(Mission6Step.ApproachPatrolCar);
        CompleteStep(Mission6Step.EscapeSchoolGate);
    }
}

/// <summary>Membuat MissionManager + GameUI otomatis saat game mulai (tanpa setup manual di scene).</summary>
public static class GameBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        if (MissionManager.Instance != null) return;

        var go = new GameObject("GameSystems");
        UnityEngine.Object.DontDestroyOnLoad(go);
        go.AddComponent<MissionManager>();
        go.AddComponent<GameUI>();
    }
}

/// <summary>Index langkah Misi 3 (urutannya harus sama dengan CorridorMission di MissionManager).</summary>
public static class Mission3Step
{
    public const int EnterOffice = 0;
    public const int FindCode = 1;
    public const int RingBell = 2;
    public const int EnterCctv = 3;
}

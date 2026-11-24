using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// State lorong yang hidup lintas scene (Lorong &lt;-&gt; RuangKepsek):
/// - bel sudah dibunyikan atau belum,
/// - posisi terakhir tiap zombie (supaya tidak ke-reset waktu player keluar-masuk ruangan),
/// - titik muncul player di scene berikutnya.
/// Semuanya di-reset HANYA kalau player mati (PlayerHealth.Die) atau game mulai dari awal.
/// </summary>
public static class CorridorState
{
    /// <summary>Kode panel bel sekolah = jawaban soal 512 + 512 yang ditaruh di meja kepala sekolah.</summary>
    public const string BellCode = "1024";

    public static bool BellActive { get; private set; }
    public static Vector2 BellPosition { get; set; }
    public static bool HasBellPosition { get; set; }

    /// <summary>Naik setiap Reset(). Zombie dari "generasi" lama tidak boleh menyimpan posisi.</summary>
    public static int Epoch { get; private set; }

    public static event Action BellRang;

    private static readonly Dictionary<string, Vector2> ZombiePos = new Dictionary<string, Vector2>();
    private static Vector2? nextSpawn;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        BellActive = false;
        HasBellPosition = false;
        Epoch = 0;
        BellRang = null;
        ZombiePos.Clear();
        nextSpawn = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Hook()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    /// <summary>Kembalikan semuanya ke awal (dipanggil saat player mati / game diulang).</summary>
    public static void Reset()
    {
        BellActive = false;
        ZombiePos.Clear();
        nextSpawn = null;
        Epoch++;
        if (MissionManager.Instance != null) MissionManager.Instance.ResetCorridorMission();
    }

    public static void RingBell()
    {
        if (BellActive) return;
        BellActive = true;
        BellRang?.Invoke();
    }

    // ---- posisi zombie ----

    public static void SavePosition(string id, Vector2 pos, int epoch)
    {
        if (epoch != Epoch) return;
        ZombiePos[id] = pos;
    }

    public static bool TryGetPosition(string id, out Vector2 pos) => ZombiePos.TryGetValue(id, out pos);

    // ---- titik muncul player ----

    public static void SetNextSpawn(Vector2 pos) => nextSpawn = pos;

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!nextSpawn.HasValue) return;
        var pc = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
        if (pc == null) return;

        Vector2 p = nextSpawn.Value;
        nextSpawn = null;

        pc.transform.position = new Vector3(p.x, p.y, pc.transform.position.z);
        var rb = pc.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.position = p;
            rb.linearVelocity = Vector2.zero;
        }
    }
}

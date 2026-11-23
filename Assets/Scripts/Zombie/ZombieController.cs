using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Zombie: patroli antar titik, mengejar player kalau berada dalam jangkauan, dan melukai
/// player selama bersentuhan. Kecepatan kejar = setengah kecepatan player (4 -> 2).
/// Memakai Animator controller yang sama dengan player (parameter MoveX/MoveY/Speed/IsMoving/FacingLeft).
/// Jalur dihitung lewat ZombieNav (A*) sehingga zombie tidak nyangkut di meja.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(BoxCollider2D))]
public class ZombieController : MonoBehaviour
{
    private enum State { Patrol, Chase, Gather }

    [Header("Patrol")]
    [Tooltip("Titik patroli (posisi dunia). Kosong = berkeliling ke titik acak.")]
    [SerializeField] private Vector2[] patrolPoints = new Vector2[0];
    [SerializeField] private float patrolSpeed = 1.2f;
    [SerializeField] private float waitAtPoint = 1f;

    [Header("Chase")]
    [Tooltip("Kecepatan player 4, jadi zombie 2x lebih lambat = 2.")]
    [SerializeField] private float chaseSpeed = 2f;
    [SerializeField] private float detectRadius = 3.5f;
    [Tooltip("Kalau player lebih jauh dari ini, zombie berhenti mengejar dan kembali patroli.")]
    [SerializeField] private float loseRadius = 6f;
    [SerializeField] private float repathInterval = 0.3f;
    [SerializeField] private float stopDistance = 0.35f;

    [Header("Attack")]
    [SerializeField] private float touchMargin = 0.12f;

    [Header("Lorong (bel sekolah)")]
    [Tooltip("Centang untuk zombie di lorong: posisinya diingat saat player pindah ruangan dan zombie ikut berkumpul ke bel.")]
    [SerializeField] private bool persistAcrossScenes = false;
    [SerializeField] private float gatherRadius = 1.9f;
    [SerializeField] private float gatherSpeed = 2f;
    [Tooltip("Setelah player masuk scene lagi, zombie diam sebentar supaya player sempat bereaksi.")]
    [SerializeField] private float spawnGrace = 1.2f;

    [Header("Animation")]
    [SerializeField] private float patrolAnimSpeed = 0.45f;
    [SerializeField] private float chaseAnimSpeed = 0.6f;
    [SerializeField] private Color patrolTint = new Color(0.62f, 0.9f, 0.62f, 1f);
    [SerializeField] private Color chaseTint = new Color(1f, 0.6f, 0.6f, 1f);

    private static readonly int MoveXHash = Animator.StringToHash("MoveX");
    private static readonly int MoveYHash = Animator.StringToHash("MoveY");
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
    private static readonly int FacingLeftHash = Animator.StringToHash("FacingLeft");

    private static ZombieNav sharedNav;
    private static int navSceneHandle = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        sharedNav = null;
        navSceneHandle = -1;
    }

    private Rigidbody2D rb;
    private Animator animator;
    private BoxCollider2D col;
    private SpriteRenderer sr;
    private Transform playerTf;
    private Collider2D playerCol;
    private ZombieNav nav;

    private State state = State.Patrol;
    private readonly List<Vector2> path = new List<Vector2>();
    private int pathIdx;
    private int patrolIndex;
    private bool waiting = true;
    private float waitTimer = 0.5f;
    private float repathTimer;
    private Vector2 moveDir;
    private bool facingLeft;
    private float graceTimer;
    private int epoch;

    private bool BellRung => persistAcrossScenes && CorridorState.BellActive;

    private bool Paused
    {
        get
        {
            var hp = PlayerHealth.Instance;
            if (hp != null && hp.IsDead) return true;
            var ui = GameUI.Instance;
            return ui != null && ui.ModalOpen;   // jangan menyerang saat player membaca catatan / keypad
        }
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        col = GetComponent<BoxCollider2D>();
        sr = GetComponent<SpriteRenderer>();

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        col.isTrigger = true;
    }

    private void Start()
    {
        var pc = FindFirstObjectByType<PlayerController>();
        if (pc == null)
        {
            Debug.LogWarning("[" + name + "] PlayerController tidak ditemukan, zombie dimatikan.", this);
            enabled = false;
            return;
        }
        playerTf = pc.transform;
        playerCol = pc.GetComponent<Collider2D>();

        if (PlayerHealth.Instance == null) pc.gameObject.AddComponent<PlayerHealth>();

        nav = GetNav();
        if (patrolPoints != null && patrolPoints.Length > 0) patrolIndex = 0;

        if (persistAcrossScenes)
        {
            epoch = CorridorState.Epoch;
            Vector2 saved;
            if (CorridorState.TryGetPosition(name, out saved))
            {
                transform.position = new Vector3(saved.x, saved.y, transform.position.z);
                rb.position = saved;
                graceTimer = spawnGrace;
            }
            if (BellRung) EnterGather();
        }
    }

    private void OnEnable() => CorridorState.BellRang += OnBellRang;
    private void OnDisable() => CorridorState.BellRang -= OnBellRang;

    private void OnDestroy()
    {
        if (persistAcrossScenes) CorridorState.SavePosition(name, transform.position, epoch);
    }

    private void OnBellRang()
    {
        if (!persistAcrossScenes || nav == null) return;
        EnterGather();   // bel berbunyi: zombie yang lagi mengejar pun langsung berpaling ke bel
    }

    private ZombieNav GetNav()
    {
        int handle = gameObject.scene.handle;
        if (sharedNav == null || navSceneHandle != handle)
        {
            Bounds area;
            var bg = GameObject.Find("Bg");
            SpriteRenderer bgSr = bg != null ? bg.GetComponent<SpriteRenderer>() : null;
            if (bgSr != null) area = bgSr.bounds;
            else
            {
                var cam = Camera.main;
                float h = cam != null ? cam.orthographicSize : 5f;
                float asp = cam != null ? cam.aspect : 1.78f;
                Vector3 c = cam != null ? cam.transform.position : Vector3.zero;
                area = new Bounds(c, new Vector3(h * 2f * asp, h * 2f, 1f));
            }

            Vector2 offset = (Vector2)col.bounds.center - (Vector2)transform.position;
            sharedNav = new ZombieNav(area, offset, col.bounds.size);
            navSceneHandle = handle;
        }
        return sharedNav;
    }

    // ------------------------------------------------------------------ keputusan (Update)

    private void Update()
    {
        if (nav == null || playerTf == null) return;

        bool paused = Paused;
        if (!paused)
        {
            if (graceTimer > 0f) graceTimer -= Time.deltaTime;
            else
            {
                UpdateState();
                TryAttack();
            }
        }
        UpdateAnimator(paused);
    }

    private void UpdateState()
    {
        float dist = Vector2.Distance(transform.position, playerTf.position);

        // Setelah bel berbunyi, zombie lorong fokus ke bel: tidak lagi mendeteksi / mengejar player.
        if (!BellRung && state != State.Chase && dist <= detectRadius) EnterChase();
        else if (state == State.Chase && (BellRung || dist >= loseRadius))
        {
            if (BellRung) EnterGather();
            else EnterPatrol();
        }

        if (state == State.Chase)
        {
            repathTimer -= Time.deltaTime;
            if (repathTimer <= 0f)
            {
                repathTimer = repathInterval;
                RepathToPlayer();
            }
            return;
        }

        // Berkumpul di sekitar bel sekolah
        if (state == State.Gather)
        {
            if (waiting)
            {
                waitTimer -= Time.deltaTime;
                if (waitTimer <= 0f)
                {
                    waiting = false;
                    GoToGatherPoint();
                }
            }
            else if (pathIdx >= path.Count)
            {
                waiting = true;
                waitTimer = Random.Range(0.8f, 2.2f);
            }
            return;
        }

        // Patroli
        if (waiting)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f)
            {
                waiting = false;
                GoToNextPatrolPoint();
            }
        }
        else if (pathIdx >= path.Count)
        {
            waiting = true;
            waitTimer = waitAtPoint;
        }
    }

    private void EnterChase()
    {
        state = State.Chase;
        waiting = false;
        repathTimer = repathInterval;
        RepathToPlayer();
    }

    private void EnterGather()
    {
        if (!CorridorState.HasBellPosition) return;
        state = State.Gather;
        path.Clear();
        pathIdx = 0;
        waiting = false;
        GoToGatherPoint();
    }

    private void GoToGatherPoint()
    {
        Vector2 dir = Random.insideUnitCircle;
        if (dir.sqrMagnitude < 0.01f) dir = Vector2.right;
        Vector2 target = CorridorState.BellPosition + dir.normalized * Random.Range(0.9f, gatherRadius);
        if (!nav.FindPath(rb.position, target, path)) path.Clear();
        pathIdx = 0;
    }

    private void EnterPatrol()
    {
        state = State.Patrol;
        path.Clear();
        pathIdx = 0;
        waiting = true;
        waitTimer = 0.6f;
    }

    private void RepathToPlayer()
    {
        if (!nav.FindPath(rb.position, playerTf.position, path)) path.Clear();
        pathIdx = 0;
    }

    private void GoToNextPatrolPoint()
    {
        Vector2 target;
        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
            target = patrolPoints[patrolIndex];
        }
        else
        {
            target = nav.RandomWalkable();
        }

        if (!nav.FindPath(rb.position, target, path)) path.Clear();
        pathIdx = 0;
    }

    private void TryAttack()
    {
        var hp = PlayerHealth.Instance;
        if (hp == null || hp.IsDead || playerCol == null) return;

        ColliderDistance2D d = col.Distance(playerCol);
        if (d.isOverlapped || d.distance <= touchMargin)
            hp.ApplyContactDamage(Time.deltaTime);
    }

    // ------------------------------------------------------------------ gerak (FixedUpdate)

    private void FixedUpdate()
    {
        moveDir = Vector2.zero;
        if (nav == null || playerTf == null || Paused || graceTimer > 0f) return;

        if (state == State.Chase &&
            Vector2.Distance(rb.position, playerTf.position) < stopDistance)
            return;

        if (pathIdx >= path.Count) return;

        float speed = state == State.Chase ? chaseSpeed : (state == State.Gather ? gatherSpeed : patrolSpeed);
        Vector2 pos = rb.position;
        Vector2 delta = path[pathIdx] - pos;
        float step = speed * Time.fixedDeltaTime;

        if (delta.magnitude <= step)
        {
            rb.MovePosition(path[pathIdx]);
            pathIdx++;
            if (delta.sqrMagnitude > 0.000001f) moveDir = delta.normalized;
        }
        else
        {
            moveDir = delta.normalized;
            rb.MovePosition(pos + moveDir * step);
        }
    }

    // ------------------------------------------------------------------ animasi (sama dengan player)

    private void UpdateAnimator(bool paused)
    {
        Vector2 dir = paused ? Vector2.zero : moveDir;
        bool moving = dir.sqrMagnitude > 0.0001f;

        if (dir.x < -0.01f) facingLeft = true;
        else if (dir.x > 0.01f) facingLeft = false;

        animator.SetFloat(MoveXHash, dir.x);
        animator.SetFloat(MoveYHash, dir.y);
        animator.SetFloat(SpeedHash, moving ? 1f : 0f);
        animator.SetBool(IsMovingHash, moving);
        animator.SetBool(FacingLeftHash, facingLeft);

        animator.speed = moving ? (state == State.Chase ? chaseAnimSpeed : patrolAnimSpeed) : 1f;
        if (sr != null) sr.color = state == State.Chase ? chaseTint : patrolTint;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, detectRadius);
        Gizmos.color = new Color(1f, 1f, 0.2f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, loseRadius);

        if (patrolPoints == null) return;
        Gizmos.color = Color.green;
        for (int i = 0; i < patrolPoints.Length; i++)
        {
            Gizmos.DrawSphere(patrolPoints[i], 0.12f);
            Gizmos.DrawLine(patrolPoints[i], patrolPoints[(i + 1) % patrolPoints.Length]);
        }
    }
}

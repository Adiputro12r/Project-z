using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Zombie untuk misi 6 (lari ke gerbang): langsung mengejar player tanpa patroli.
/// Kecepatan di bawah kecepatan player (4) supaya misi bisa diselesaikan.
/// Memakai Animator controller yang sama dengan player (MoveX/MoveY/Speed/IsMoving/FacingLeft).
/// Dibuat otomatis oleh Mission6Manager.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(BoxCollider2D))]
public class EscapeZombie : MonoBehaviour
{
    public float speed = 2.4f;
    public float graceTime = 1f;
    public float stopDistance = 0.3f;
    public float touchMargin = 0.12f;
    public float separationRadius = 0.8f;
    public float animSpeed = 0.6f;
    public Color tint = new Color(1f, 0.6f, 0.6f, 1f);

    /// <summary>Diset true saat ending supaya zombie berhenti menyerang.</summary>
    public bool frozen;

    private static readonly List<EscapeZombie> All = new List<EscapeZombie>();
    private static readonly int MoveXHash = Animator.StringToHash("MoveX");
    private static readonly int MoveYHash = Animator.StringToHash("MoveY");
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
    private static readonly int FacingLeftHash = Animator.StringToHash("FacingLeft");

    private Rigidbody2D rb;
    private Animator animator;
    private BoxCollider2D col;
    private SpriteRenderer sr;
    private Transform playerTf;
    private Collider2D playerCol;
    private Vector2 moveDir;
    private bool facingLeft;
    private float grace;

    public void Init(Transform player)
    {
        playerTf = player;
        playerCol = player.GetComponent<Collider2D>();
        grace = graceTime;
    }

    private void OnEnable() => All.Add(this);
    private void OnDisable() => All.Remove(this);

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
        if (sr != null) sr.color = tint;
    }

    private void Update()
    {
        if (playerTf == null) return;

        if (!frozen)
        {
            if (grace > 0f) grace -= Time.deltaTime;
            else TryAttack();
        }

        bool moving = moveDir.sqrMagnitude > 0.0001f;
        if (moveDir.x < -0.01f) facingLeft = true;
        else if (moveDir.x > 0.01f) facingLeft = false;

        animator.SetFloat(MoveXHash, moveDir.x);
        animator.SetFloat(MoveYHash, moveDir.y);
        animator.SetFloat(SpeedHash, moving ? 1f : 0f);
        animator.SetBool(IsMovingHash, moving);
        animator.SetBool(FacingLeftHash, facingLeft);
        animator.speed = moving ? animSpeed : 1f;
    }

    private void FixedUpdate()
    {
        moveDir = Vector2.zero;
        if (playerTf == null || frozen || grace > 0f) return;

        var hp = PlayerHealth.Instance;
        if (hp != null && hp.IsDead) return;

        Vector2 pos = rb.position;
        Vector2 toPlayer = (Vector2)playerTf.position - pos;
        if (toPlayer.magnitude < stopDistance) return;

        Vector2 dir = toPlayer.normalized;

        // saling menjauh sedikit supaya 5 zombie tidak menumpuk jadi satu
        Vector2 push = Vector2.zero;
        for (int i = 0; i < All.Count; i++)
        {
            var o = All[i];
            if (o == this) continue;
            Vector2 away = pos - o.rb.position;
            float d = away.magnitude;
            if (d > 0.001f && d < separationRadius) push += away / d * (1f - d / separationRadius);
        }

        Vector2 final = (dir + push * 0.8f).normalized;
        moveDir = final;
        rb.MovePosition(pos + final * speed * Time.fixedDeltaTime);
    }

    private void TryAttack()
    {
        var hp = PlayerHealth.Instance;
        if (hp == null || hp.IsDead || playerCol == null) return;

        ColliderDistance2D d = col.Distance(playerCol);
        if (d.isOverlapped || d.distance <= touchMargin)
            hp.ApplyContactDamage(Time.deltaTime);
    }
}

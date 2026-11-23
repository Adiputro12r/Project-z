using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Gerak top-down 4 arah + penggerak Animator.
/// - Kiri/kanan: karakter menghadap arah tersebut.
/// - Atas/bawah: karakter tetap memakai arah hadap terakhir (kiri atau kanan).
/// - Diam: kembali ke Idle sesuai arah hadap terakhir.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4f;

    private static readonly int MoveXHash = Animator.StringToHash("MoveX");
    private static readonly int MoveYHash = Animator.StringToHash("MoveY");
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
    private static readonly int FacingLeftHash = Animator.StringToHash("FacingLeft");

    private Rigidbody2D rb;
    private Animator animator;
    private Vector2 moveInput;
    private bool facingLeft;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        rb.gravityScale = 0f;
        rb.freezeRotation = true;
    }

    private void Update()
    {
        moveInput = ReadInput();

        // Hanya input horizontal yang mengubah arah hadap.
        // Naik/turun mempertahankan arah hadap sebelumnya.
        if (moveInput.x < -0.01f) facingLeft = true;
        else if (moveInput.x > 0.01f) facingLeft = false;

        bool isMoving = moveInput.sqrMagnitude > 0.0001f;

        animator.SetFloat(MoveXHash, moveInput.x);
        animator.SetFloat(MoveYHash, moveInput.y);
        animator.SetFloat(SpeedHash, moveInput.magnitude);
        animator.SetBool(IsMovingHash, isMoving);
        animator.SetBool(FacingLeftHash, facingLeft);
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = moveInput * moveSpeed;
    }

    private static Vector2 ReadInput()
    {
        Vector2 v = Vector2.zero;

        Keyboard kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) v.x -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) v.x += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) v.y -= 1f;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) v.y += 1f;
        }

        Gamepad gp = Gamepad.current;
        if (gp != null && v == Vector2.zero)
        {
            v = gp.leftStick.ReadValue();
            if (v.sqrMagnitude < 0.04f) v = Vector2.zero; // deadzone
        }

        return Vector2.ClampMagnitude(v, 1f);
    }
}

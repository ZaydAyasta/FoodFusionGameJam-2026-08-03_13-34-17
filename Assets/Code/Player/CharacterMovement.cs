using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CharacterInput))]
public class CharacterMovement : MonoBehaviour
{
    [SerializeField] private float speed = 5f;
    [SerializeField] private Animator animator;

    private CharacterInput input;
    private Rigidbody2D rb;
    private PlayerDash dash;

    private int lastDirection = 1; // Down por defecto

    public void AddSpeedBonus(float amount)
    {
        if (amount <= 0f)
            return;

        speed += amount;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        input = GetComponent<CharacterInput>();
        dash = GetComponent<PlayerDash>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        rb.gravityScale = 0f;
    }

    private void Update()
    {
        Vector2 move = input.MoveInput;

        bool isMoving = move.sqrMagnitude > 0.01f;

        if (isMoving)
        {
            lastDirection = GetDirection(move);
            if (dash == null || !dash.IsDashing)
                GameAudio.PlayFootstep();
        }

        if (animator != null)
        {
            animator.SetBool("IsMoving", isMoving);
            animator.SetInteger("Direction", lastDirection);
        }
    }

    private void FixedUpdate()
    {
        if (dash != null && dash.IsDashing)
            return;

        Vector2 move = input.MoveInput;

        rb.linearVelocity =
            move.sqrMagnitude > 1f
                ? move.normalized * speed
                : move * speed;
    }

    private int GetDirection(Vector2 move)
    {
        bool right = move.x > 0.1f;
        bool left = move.x < -0.1f;
        bool up = move.y > 0.1f;
        bool down = move.y < -0.1f;

        if (up && !left && !right) return 5; // Up
        if (up && right) return 4; // UpRight
        if (right && !up && !down) return 3; // Right
        if (down && right) return 2; // DownRight
        if (down && !left && !right) return 1; // Down
        if (down && left) return 8; // DownLeft
        if (left && !up && !down) return 7; // Left
        if (up && left) return 6; // UpLeft

        return lastDirection;
    }
}

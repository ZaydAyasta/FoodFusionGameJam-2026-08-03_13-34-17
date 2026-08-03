using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CharacterInput))]
public class CharacterMovement : MonoBehaviour
{
    [SerializeField] private float speed = 5f;

    private CharacterInput input;
    private Rigidbody2D rb;
    private PlayerDash dash;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        input = GetComponent<CharacterInput>();
        dash = GetComponent<PlayerDash>();
        rb.gravityScale = 0f;
    }

    void FixedUpdate()
    {
        if (dash != null && dash.IsDashing)
            return;

        Vector2 move = input.MoveInput;
        rb.linearVelocity = move.sqrMagnitude > 1f ? move.normalized * speed : move * speed;
    }
}

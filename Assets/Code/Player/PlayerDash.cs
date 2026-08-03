using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CharacterInput))]
public class PlayerDash : MonoBehaviour
{
    [SerializeField] private float dashSpeed = 14f;
    [SerializeField] private float dashDuration = 0.12f;
    [SerializeField] private float dashCooldown = 0.75f;
    [SerializeField] private float invulnerabilityDuration = 0.18f;

    private CharacterInput input;
    private Rigidbody2D rb;
    private Health health;
    private Vector2 lastMoveDirection = Vector2.right;
    private float nextDashTime;

    public bool IsDashing { get; private set; }

    private void Awake()
    {
        input = GetComponent<CharacterInput>();
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<Health>();
    }

    private void Update()
    {
        Vector2 move = input.MoveInput;
        if (move.sqrMagnitude > 0.001f)
            lastMoveDirection = move.normalized;

        if (input.DashPressed && Time.time >= nextDashTime && !IsDashing)
            StartCoroutine(DashRoutine());
    }

    private IEnumerator DashRoutine()
    {
        IsDashing = true;
        nextDashTime = Time.time + dashCooldown;
        health?.MakeInvulnerable(invulnerabilityDuration);
        rb.linearVelocity = lastMoveDirection * dashSpeed;

        yield return new WaitForSeconds(dashDuration);

        IsDashing = false;
    }
}

using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class ChaserEnemy : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private float stopDistance = 0.35f;

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
    }

    private void FixedUpdate()
    {
        if (target == null)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 toTarget = target.position - transform.position;
        if (toTarget.magnitude <= stopDistance)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        rb.linearVelocity = toTarget.normalized * moveSpeed;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}

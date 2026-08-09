using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class CheeseMinionEnemy : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer visualRenderer;
    [SerializeField] private bool spriteFacesRight = false;

    [Header("Birth")]
    [SerializeField] private float birthDuration = 0.85f;

    [Header("Search / Wander")]
    [SerializeField] private float wanderSpeed = 1.35f;
    [SerializeField] private float minWanderDirectionDuration = 0.8f;
    [SerializeField] private float maxWanderDirectionDuration = 1.8f;
    [SerializeField] private float detectionDistance = 6f;

    [Header("Chase")]
    [SerializeField] private float chaseSpeed = 4.25f;
    [SerializeField] private float loseTargetDistance = 8f;

    [Header("Contact")]
    [SerializeField] private float contactDamage = 8f;
    [SerializeField, Min(0.05f)] private float contactRadius = 0.45f;
    [SerializeField, Min(0f)] private float postHitRecoveryDuration = 0.3f;

    private Rigidbody2D rb;
    private Transform target;
    private Vector2 wanderDirection;
    private float nextWanderDirectionAt;
    private State state;
    private float birthEndsAt;
    private float recoveryEndsAt;
    private Health health;
    private DamageDealer contactDamageDealer;

    private static readonly int IsMovingParameter = Animator.StringToHash("IsMoving");
    private static readonly int IsChasingParameter = Animator.StringToHash("IsChasing");
    private static readonly int BornParameter = Animator.StringToHash("Born");

    private enum State { Born, Searching, Chasing, Recovering }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);
        if (visualRenderer == null && animator != null)
            visualRenderer = animator.GetComponentInChildren<SpriteRenderer>(true);
        if (visualRenderer == null)
            visualRenderer = GetComponentInChildren<SpriteRenderer>(true);

        health = GetComponent<Health>();
        DamageDealer oldRootDealer = GetComponent<DamageDealer>();
        if (oldRootDealer != null)
            oldRootDealer.enabled = false;

        GameObject contactZone = new("ContactDamageTrigger");
        contactZone.transform.SetParent(transform, false);
        CircleCollider2D contactCollider = contactZone.AddComponent<CircleCollider2D>();
        contactCollider.isTrigger = true;
        contactCollider.radius = contactRadius;
        contactDamageDealer = contactZone.AddComponent<DamageDealer>();
        contactDamageDealer.Configure(CombatFaction.Enemy, contactDamage, false);
        contactDamageDealer.DamageApplied += HandleContactDamageApplied;
    }

    private void OnEnable()
    {
        state = State.Searching;
        ChooseWanderDirection();
        if (contactDamageDealer != null)
            contactDamageDealer.enabled = true;
        SetAnimatorBool(IsMovingParameter, false);
        SetAnimatorBool(IsChasingParameter, false);
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    private void FixedUpdate()
    {
        if (target != null)
            UpdateFacing(target.position.x - transform.position.x);

        if (state == State.Born)
        {
            rb.linearVelocity = Vector2.zero;
            SetAnimatorBool(IsMovingParameter, false);
            if (Time.time >= birthEndsAt)
                FinishBirth();
            return;
        }

        if (state == State.Recovering)
        {
            rb.linearVelocity = Vector2.zero;
            SetAnimatorBool(IsMovingParameter, false);
            if (Time.time >= recoveryEndsAt)
                SetState(State.Searching);
            return;
        }

        if (target == null)
        {
            Wander();
            UpdateFacing(rb.linearVelocity.x);
            return;
        }

        float distance = Vector2.Distance(rb.position, target.position);
        if (state == State.Searching && distance <= detectionDistance)
            SetState(State.Chasing);
        else if (state == State.Chasing && distance > loseTargetDistance)
            SetState(State.Searching);

        if (state == State.Chasing)
        {
            Vector2 direction = (Vector2)target.position - rb.position;
            rb.linearVelocity = direction.sqrMagnitude > 0.001f
                ? direction.normalized * chaseSpeed
                : Vector2.zero;
        }
        else
        {
            Wander();
        }

        SetAnimatorBool(IsMovingParameter, rb.linearVelocity.sqrMagnitude > 0.01f);
    }

    public void BeginDroppedBirth()
    {
        state = State.Born;
        birthEndsAt = Time.time + Mathf.Max(0f, birthDuration);
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        if (health == null)
            health = GetComponent<Health>();
        health?.MakeInvulnerable(Mathf.Max(0f, birthDuration));

        if (contactDamageDealer != null)
            contactDamageDealer.enabled = false;
        SetAnimatorBool(IsMovingParameter, false);
        SetAnimatorBool(IsChasingParameter, false);
        SetAnimatorTrigger(BornParameter);
    }

    private void FinishBirth()
    {
        if (contactDamageDealer != null)
            contactDamageDealer.enabled = true;
        SetState(State.Searching);
    }

    private void HandleContactDamageApplied(GameObject hitObject)
    {
        FactionMember hitFaction = hitObject != null
            ? hitObject.GetComponentInParent<FactionMember>()
            : null;
        if (hitFaction == null || hitFaction.Faction != CombatFaction.Player)
            return;

        recoveryEndsAt = Time.time + Mathf.Max(0f, postHitRecoveryDuration);
        if (rb != null)
            rb.linearVelocity = Vector2.zero;
        SetState(State.Recovering);
        SetAnimatorBool(IsMovingParameter, false);
    }

    private void Wander()
    {
        if (Time.time >= nextWanderDirectionAt)
            ChooseWanderDirection();
        rb.linearVelocity = wanderDirection * wanderSpeed;
    }

    private void ChooseWanderDirection()
    {
        wanderDirection = Random.insideUnitCircle.normalized;
        if (wanderDirection.sqrMagnitude <= 0.001f)
            wanderDirection = Vector2.right;
        float minimum = Mathf.Max(0.1f, minWanderDirectionDuration);
        float maximum = Mathf.Max(minimum, maxWanderDirectionDuration);
        nextWanderDirectionAt = Time.time + Random.Range(minimum, maximum);
    }

    private void SetState(State newState)
    {
        state = newState;
        SetAnimatorBool(IsChasingParameter, state == State.Chasing);
        if (state == State.Searching)
            ChooseWanderDirection();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (state == State.Searching)
            ChooseWanderDirection();
    }

    private void SetAnimatorBool(int hash, bool value)
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);
        if (animator == null)
            return;

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.nameHash == hash && parameter.type == AnimatorControllerParameterType.Bool)
            {
                animator.SetBool(hash, value);
                return;
            }
        }
    }

    private void UpdateFacing(float horizontalDirection)
    {
        if (Mathf.Abs(horizontalDirection) <= 0.01f || visualRenderer == null)
            return;

        visualRenderer.flipX = spriteFacesRight
            ? horizontalDirection < 0f
            : horizontalDirection > 0f;
    }

    private void SetAnimatorTrigger(int hash)
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);
        if (animator == null)
            return;

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.nameHash == hash && parameter.type == AnimatorControllerParameterType.Trigger)
            {
                animator.SetTrigger(hash);
                return;
            }
        }
    }
}

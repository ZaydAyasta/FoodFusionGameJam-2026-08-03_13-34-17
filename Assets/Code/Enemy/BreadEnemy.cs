using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BreadEnemy : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private Animator animator;

    [Header("Hop Movement")]
    [SerializeField] private float preJumpDuration = 0.2f;
    [SerializeField] private float movementBurstDuration = 0.65f;
    [SerializeField] private float postJumpDuration = 0.2f;
    [SerializeField] private float restBetweenJumps = 0.35f;
    [SerializeField] private float moveSpeed = 1.45f;
    [SerializeField] private float approachDistance = 4.2f;
    [SerializeField] private float retreatDistance = 2.2f;
    [SerializeField] private float strafeSpeed = 1.25f;
    [SerializeField] private float strafeSwitchInterval = 0.7f;

    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Spaghetti Ring Attack")]
    [SerializeField] private BouncingSpaghettiProjectile ringPrefab;
    [SerializeField] private Transform projectileOrigin;
    [SerializeField] private float attackRange = 7.5f;
    [SerializeField] private float stationaryChargeDuration = 1.25f;
    [SerializeField] private int minRingsPerAttack = 1;
    [SerializeField] private int maxRingsPerAttack = 2;
    [SerializeField] private float delayBetweenRings = 0.65f;
    [SerializeField] private float attackCooldown = 3.2f;
    [SerializeField] private float ringSpeed = 6.5f;
    [SerializeField] private float ringDamage = 7f;
    [SerializeField] private float ringLifetime = 8f;
    [SerializeField] private int ringWallBounces = 3;

    private Rigidbody2D rb;
    private Collider2D ownHitbox;
    private float nextAttackAt;
    private float nextStrafeSwitchAt;
    private int strafeDirection = 1;
    private bool attacking;
    private HopPhase hopPhase;
    private float hopPhaseEndsAt;
    private float nextHopAt;

    private static readonly int IsMovingParameter = Animator.StringToHash("IsMoving");
    private static readonly int IsAttackingParameter = Animator.StringToHash("IsAttacking");
    private static readonly int JumpParameter = Animator.StringToHash("Jump");
    private static readonly int ChargeParameter = Animator.StringToHash("Charge");
    private static readonly int AttackParameter = Animator.StringToHash("Attack");

    private enum HopPhase { Resting, Preparing, Moving, Landing }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        ownHitbox = GetComponent<Collider2D>();
        ResolveAnimator();
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }
    }

    private void OnEnable()
    {
        nextAttackAt = Time.time + Random.Range(0.7f, Mathf.Max(0.8f, attackCooldown));
        nextStrafeSwitchAt = Time.time + strafeSwitchInterval;
        strafeDirection = Random.value < 0.5f ? -1 : 1;
        attacking = false;
        ResetHop();
        SetAnimatorBool(IsAttackingParameter, false);
    }

    private void FixedUpdate()
    {
        if (rb == null || target == null || attacking || hopPhase != HopPhase.Moving)
        {
            StopMovement();
            return;
        }

        Vector2 toTarget = GetTargetCenter() - GetEnemyCenter();
        float distance = toTarget.magnitude;
        if (distance <= 0.001f)
        {
            StopMovement();
            return;
        }

        Vector2 forward = toTarget / distance;
        if (distance > approachDistance)
        {
            rb.linearVelocity = forward * moveSpeed;
            return;
        }

        if (Time.time >= nextStrafeSwitchAt)
        {
            strafeDirection *= -1;
            nextStrafeSwitchAt = Time.time + Mathf.Max(0.1f, strafeSwitchInterval);
        }

        Vector2 strafe = new(-forward.y, forward.x);
        Vector2 velocity = strafe * (strafeSpeed * strafeDirection);
        if (distance < retreatDistance)
            velocity -= forward * (moveSpeed * 0.65f);
        rb.linearVelocity = velocity;
    }

    private void Update()
    {
        UpdateHopCycle();
        if (target == null || attacking || Time.time < nextAttackAt)
            return;

        if (Vector2.Distance(GetEnemyCenter(), GetTargetCenter()) <= attackRange)
            StartCoroutine(AttackRoutine());
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        ResetHop();
    }

    private void UpdateHopCycle()
    {
        if (target == null || attacking)
            return;

        switch (hopPhase)
        {
            case HopPhase.Resting when Time.time >= nextHopAt:
                hopPhase = HopPhase.Preparing;
                hopPhaseEndsAt = Time.time + Mathf.Max(0f, preJumpDuration);
                SetAnimatorTrigger(JumpParameter);
                break;
            case HopPhase.Preparing when Time.time >= hopPhaseEndsAt:
                hopPhase = HopPhase.Moving;
                hopPhaseEndsAt = Time.time + Mathf.Max(0.05f, movementBurstDuration);
                SetAnimatorBool(IsMovingParameter, true);
                break;
            case HopPhase.Moving when Time.time >= hopPhaseEndsAt:
                StopMovement();
                hopPhase = HopPhase.Landing;
                hopPhaseEndsAt = Time.time + Mathf.Max(0f, postJumpDuration);
                SetAnimatorBool(IsMovingParameter, false);
                break;
            case HopPhase.Landing when Time.time >= hopPhaseEndsAt:
                ResetHop();
                break;
        }
    }

    private IEnumerator AttackRoutine()
    {
        attacking = true;
        StopMovement();
        hopPhase = HopPhase.Resting;
        SetAnimatorBool(IsMovingParameter, false);
        SetAnimatorBool(IsAttackingParameter, true);
        SetAnimatorTrigger(ChargeParameter);

        yield return new WaitForSeconds(Mathf.Max(0f, stationaryChargeDuration));

        int minimum = Mathf.Max(1, minRingsPerAttack);
        int maximum = Mathf.Max(minimum, maxRingsPerAttack);
        int ringCount = Random.Range(minimum, maximum + 1);
        for (int i = 0; i < ringCount; i++)
        {
            if (target == null)
                break;

            SetAnimatorTrigger(AttackParameter);
            FireRing();
            if (i < ringCount - 1)
                yield return new WaitForSeconds(Mathf.Max(0.05f, delayBetweenRings));
        }

        nextAttackAt = Time.time + Mathf.Max(0.1f, attackCooldown);
        attacking = false;
        SetAnimatorBool(IsAttackingParameter, false);
        ResetHop();
    }

    private void FireRing()
    {
        Vector2 direction = GetTargetCenter() - GetEnemyCenter();
        if (direction.sqrMagnitude <= 0.001f)
            direction = Vector2.down;

        BouncingSpaghettiProjectile ring;
        Vector3 origin = projectileOrigin != null ? projectileOrigin.position : (Vector3)GetEnemyCenter();
        if (ringPrefab != null)
            ring = Instantiate(ringPrefab, origin, ringPrefab.transform.rotation);
        else
        {
            GameObject fallback = new("Spaghetti Ring");
            fallback.transform.position = origin;
            fallback.AddComponent<CircleCollider2D>().radius = 0.22f;
            fallback.AddComponent<Rigidbody2D>();
            SpriteRenderer renderer = fallback.AddComponent<SpriteRenderer>();
            renderer.sprite = CreateFallbackRingSprite();
            renderer.color = new Color(1f, 0.78f, 0.18f);
            ring = fallback.AddComponent<BouncingSpaghettiProjectile>();
        }

        ring.Launch(direction, ringSpeed, ringDamage, ringLifetime, ringWallBounces,
            CombatFaction.Enemy, gameObject);
    }

    private Vector2 GetEnemyCenter()
    {
        if (ownHitbox == null)
            ownHitbox = GetComponent<Collider2D>();
        return ownHitbox != null ? ownHitbox.bounds.center : transform.position;
    }

    private Vector2 GetTargetCenter()
    {
        if (target == null)
            return Vector2.zero;
        Collider2D targetCollider = target.GetComponentInChildren<Collider2D>();
        return targetCollider != null ? targetCollider.bounds.center : target.position;
    }

    private void ResetHop()
    {
        hopPhase = HopPhase.Resting;
        nextHopAt = Time.time + Mathf.Max(0f, restBetweenJumps);
        SetAnimatorBool(IsMovingParameter, false);
    }

    private void StopMovement()
    {
        if (rb != null)
            rb.linearVelocity = Vector2.zero;
    }

    private void ResolveAnimator()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);
    }

    private void SetAnimatorBool(int hash, bool value)
    {
        ResolveAnimator();
        if (HasAnimatorParameter(hash, AnimatorControllerParameterType.Bool))
            animator.SetBool(hash, value);
    }

    private void SetAnimatorTrigger(int hash)
    {
        ResolveAnimator();
        if (HasAnimatorParameter(hash, AnimatorControllerParameterType.Trigger))
            animator.SetTrigger(hash);
    }

    private bool HasAnimatorParameter(int hash, AnimatorControllerParameterType type)
    {
        if (animator == null)
            return false;
        foreach (AnimatorControllerParameter parameter in animator.parameters)
            if (parameter.nameHash == hash && parameter.type == type)
                return true;
        return false;
    }

    private static Sprite CreateFallbackRingSprite()
    {
        const int size = 32;
        Texture2D texture = new(size, size);
        texture.filterMode = FilterMode.Point;
        Vector2 center = new((size - 1) * 0.5f, (size - 1) * 0.5f);
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float distance = Vector2.Distance(new Vector2(x, y), center);
            bool ring = distance >= 10f && distance <= 14f;
            texture.SetPixel(x, y, ring ? Color.white : Color.clear);
        }
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
    }
}

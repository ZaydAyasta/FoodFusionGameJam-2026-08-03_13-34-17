using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class WaterBucketEnemy : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer visualRenderer;
    [SerializeField] private bool spriteFacesRight = false;

    [Header("Hop Movement")]
    [SerializeField] private float preJumpDuration = 0.15f;
    [SerializeField] private float movementBurstDuration = 0.5f;
    [SerializeField] private float postJumpDuration = 0.15f;
    [SerializeField] private float restBetweenJumps = 0.25f;
    [SerializeField] private float moveSpeed = 1.8f;
    [SerializeField] private float preferredDistance = 5f;
    [SerializeField] private float retreatDistance = 3.2f;
    [SerializeField] private float strafeSpeed = 1.4f;
    [SerializeField] private float strafeSwitchInterval = 0.9f;

    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Water Stream")]
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private Sprite waterProjectileSprite;
    [SerializeField] private Transform projectileOrigin;
    [SerializeField] private float attackRange = 8f;
    [SerializeField] private float chargeDuration = 0.55f;
    [SerializeField, Min(1)] private int streamProjectileCount = 12;
    [SerializeField] private float delayBetweenProjectiles = 0.085f;
    [SerializeField] private float attackRecoveryDuration = 0.35f;
    [SerializeField] private float minAttackCycle = 4.5f;
    [SerializeField] private float maxAttackCycle = 5.3f;
    [SerializeField] private float projectileSpeed = 7.5f;
    [SerializeField] private float projectileDamage = 2.5f;
    [SerializeField] private float projectileLifetime = 2.4f;
    [SerializeField] private float projectileVisualWorldSize = 0.38f;

    [Header("Wet Trail")]
    [SerializeField] private float wetPatchSpacing = 0.42f;
    [SerializeField] private float wetPatchWorldSize = 0.7f;
    [SerializeField] private float wetPatchLifetime = 2.5f;
    [SerializeField, Range(0f, 1f)] private float wetPatchAlpha = 0.38f;
    [SerializeField, Min(1)] private int maxWetPatches = 18;

    private static Projectile fallbackProjectilePrefab;
    private Rigidbody2D rb;
    private Collider2D ownHitbox;
    private Vector2 lockedAttackDirection = Vector2.down;
    private bool attacking;
    private int strafeDirection = 1;
    private float nextStrafeSwitchAt;
    private float nextAttackAt;
    private float nextHopAt;
    private float hopPhaseEndsAt;
    private HopPhase hopPhase;

    private static readonly int IsMovingParameter = Animator.StringToHash("IsMoving");
    private static readonly int PreJumpParameter = Animator.StringToHash("PreJump");
    private static readonly int JumpParameter = Animator.StringToHash("Jump");
    private static readonly int PostJumpParameter = Animator.StringToHash("PostJump");
    private static readonly int ChargeParameter = Animator.StringToHash("Charge");
    private static readonly int AttackParameter = Animator.StringToHash("Attack");

    private enum HopPhase { Resting, Preparing, Moving, Landing }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        ownHitbox = GetComponent<Collider2D>();
        ResolveVisuals();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    private void OnEnable()
    {
        attacking = false;
        strafeDirection = Random.value < 0.5f ? -1 : 1;
        nextStrafeSwitchAt = Time.time + Mathf.Max(0.1f, strafeSwitchInterval);
        nextAttackAt = Time.time + Random.Range(1.2f, 2f);
        ResetHop();
    }

    private void Update()
    {
        if (!attacking)
            UpdateFacing(GetTargetDirection().x);
        UpdateHopCycle();

        if (target == null || attacking || Time.time < nextAttackAt)
            return;

        if (Vector2.Distance(GetEnemyCenter(), GetTargetCenter()) <= attackRange)
            StartCoroutine(StreamAttackRoutine());
    }

    private void FixedUpdate()
    {
        if (target == null || attacking || hopPhase != HopPhase.Moving)
        {
            StopMovement();
            return;
        }

        Vector2 toTarget = GetTargetDirection();
        float distance = toTarget.magnitude;
        if (distance <= 0.001f)
        {
            StopMovement();
            return;
        }

        Vector2 forward = toTarget / distance;
        if (distance > preferredDistance)
        {
            rb.linearVelocity = forward * moveSpeed;
            return;
        }

        if (distance < retreatDistance)
        {
            rb.linearVelocity = -forward * (moveSpeed * 0.8f);
            return;
        }

        if (Time.time >= nextStrafeSwitchAt)
        {
            strafeDirection *= -1;
            nextStrafeSwitchAt = Time.time + Mathf.Max(0.1f, strafeSwitchInterval);
        }

        Vector2 strafe = new(-forward.y, forward.x);
        rb.linearVelocity = strafe * (strafeSpeed * strafeDirection);
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
                SetAnimatorTrigger(PreJumpParameter);
                break;
            case HopPhase.Preparing when Time.time >= hopPhaseEndsAt:
                hopPhase = HopPhase.Moving;
                hopPhaseEndsAt = Time.time + Mathf.Max(0.05f, movementBurstDuration);
                SetAnimatorBool(IsMovingParameter, true);
                SetAnimatorTrigger(JumpParameter);
                break;
            case HopPhase.Moving when Time.time >= hopPhaseEndsAt:
                StopMovement();
                hopPhase = HopPhase.Landing;
                hopPhaseEndsAt = Time.time + Mathf.Max(0f, postJumpDuration);
                SetAnimatorBool(IsMovingParameter, false);
                SetAnimatorTrigger(PostJumpParameter);
                break;
            case HopPhase.Landing when Time.time >= hopPhaseEndsAt:
                ResetHop();
                break;
        }
    }

    private IEnumerator StreamAttackRoutine()
    {
        attacking = true;
        StopMovement();
        hopPhase = HopPhase.Resting;
        SetAnimatorBool(IsMovingParameter, false);

        lockedAttackDirection = GetTargetDirection();
        if (lockedAttackDirection.sqrMagnitude <= 0.001f)
            lockedAttackDirection = Vector2.down;
        lockedAttackDirection.Normalize();
        UpdateFacing(lockedAttackDirection.x);
        SetAnimatorTrigger(ChargeParameter);

        float minimumCycle = Mathf.Max(0.5f, minAttackCycle);
        float maximumCycle = Mathf.Max(minimumCycle, maxAttackCycle);
        nextAttackAt = Time.time + Random.Range(minimumCycle, maximumCycle);

        yield return new WaitForSeconds(Mathf.Max(0f, chargeDuration));
        SetAnimatorTrigger(AttackParameter);

        int count = Mathf.Max(1, streamProjectileCount);
        for (int i = 0; i < count; i++)
        {
            FireWaterProjectile(i == 0);
            if (i < count - 1)
                yield return new WaitForSeconds(Mathf.Max(0.01f, delayBetweenProjectiles));
        }

        yield return new WaitForSeconds(Mathf.Max(0f, attackRecoveryDuration));
        attacking = false;
        strafeDirection *= -1;
        ResetHop();
    }

    private void FireWaterProjectile(bool emitsWetTrail)
    {
        Projectile prefab = projectilePrefab != null ? projectilePrefab : CreateFallbackProjectilePrefab();
        Vector3 origin = projectileOrigin != null ? projectileOrigin.position : (Vector3)GetEnemyCenter();
        Projectile projectile = Instantiate(prefab, origin, prefab.transform.rotation);
        projectile.gameObject.SetActive(true);
        ApplyWaterSprite(projectile);

        if (emitsWetTrail)
        {
            WaterTrailEmitter emitter = projectile.gameObject.AddComponent<WaterTrailEmitter>();
            emitter.Configure(waterProjectileSprite, wetPatchSpacing, wetPatchWorldSize,
                wetPatchLifetime, wetPatchAlpha, maxWetPatches);
        }

        projectile.Launch(lockedAttackDirection, projectileSpeed, projectileDamage,
            CombatFaction.Enemy, projectileLifetime);
    }

    private void ApplyWaterSprite(Projectile projectile)
    {
        if (projectile == null || waterProjectileSprite == null)
            return;

        SpriteRenderer renderer = projectile.GetComponentInChildren<SpriteRenderer>(true);
        if (renderer == null)
            return;

        renderer.sprite = waterProjectileSprite;
        renderer.color = Color.white;
        float currentSize = Mathf.Max(renderer.bounds.size.x, renderer.bounds.size.y);
        if (currentSize > 0.001f)
            renderer.transform.localScale *= Mathf.Max(0.05f, projectileVisualWorldSize) / currentSize;
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

    private Vector2 GetTargetDirection()
    {
        return target != null ? GetTargetCenter() - GetEnemyCenter() : Vector2.zero;
    }

    private void UpdateFacing(float horizontalDirection)
    {
        if (Mathf.Abs(horizontalDirection) <= 0.01f)
            return;
        ResolveVisuals();
        if (visualRenderer != null)
            visualRenderer.flipX = spriteFacesRight
                ? horizontalDirection < 0f
                : horizontalDirection > 0f;
    }

    private void ResolveVisuals()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);
        if (visualRenderer == null && animator != null)
            visualRenderer = animator.GetComponentInChildren<SpriteRenderer>(true);
        if (visualRenderer == null)
            visualRenderer = GetComponentInChildren<SpriteRenderer>(true);
    }

    private void SetAnimatorBool(int hash, bool value)
    {
        ResolveVisuals();
        if (HasAnimatorParameter(hash, AnimatorControllerParameterType.Bool))
            animator.SetBool(hash, value);
    }

    private void SetAnimatorTrigger(int hash)
    {
        ResolveVisuals();
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

    private static Projectile CreateFallbackProjectilePrefab()
    {
        if (fallbackProjectilePrefab != null)
            return fallbackProjectilePrefab;

        GameObject projectileObject = new("Water Stream Projectile");
        projectileObject.SetActive(false);
        projectileObject.AddComponent<CircleCollider2D>().radius = 0.14f;
        projectileObject.AddComponent<Rigidbody2D>();
        Projectile projectile = projectileObject.AddComponent<Projectile>();
        SpriteRenderer renderer = projectileObject.AddComponent<SpriteRenderer>();
        renderer.sprite = CreateFallbackSprite();
        renderer.color = new Color(0.4f, 0.75f, 1f);
        fallbackProjectilePrefab = projectile;
        return fallbackProjectilePrefab;
    }

    private static Sprite CreateFallbackSprite()
    {
        Texture2D texture = new(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 8f);
    }
}

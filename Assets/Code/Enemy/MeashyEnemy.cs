using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class MeashyEnemy : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer visualRenderer;
    [SerializeField] private bool spriteFacesRight = false;

    [Header("Hop Movement")]
    [SerializeField] private float preJumpDuration = 0.2f;
    [SerializeField] private float movementBurstDuration = 0.65f;
    [SerializeField] private float postJumpDuration = 0.2f;
    [SerializeField] private float restBetweenJumps = 0.35f;
    [SerializeField] private float moveSpeed = 1.65f;
    [SerializeField] private float preferredDistance = 4f;
    [SerializeField] private float retreatDistance = 2.2f;

    [Header("Audio")]
    [SerializeField] private float splashStepInterval = 0.38f;
    [SerializeField] private float splashMovementThreshold = 0.08f;

    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Spike Shotgun")]
    [SerializeField] private Projectile spikeProjectilePrefab;
    [SerializeField] private Transform projectileOrigin;
    [SerializeField] private int spikeCount = 3;
    [SerializeField] private float spikeSpreadDegrees = 18f;
    [SerializeField] private float spikeRange = 7f;
    [SerializeField] private float spikeSpeed = 8f;
    [SerializeField] private float spikeDamage = 6f;
    [SerializeField] private float spikeLifetime = 2f;
    [SerializeField] private float spikeCooldown = 2.1f;
    [SerializeField] private float spikeWindup = 0.35f;

    [Header("Meatball")]
    [SerializeField] private MeatballProjectile meatballPrefab;
    [SerializeField] private float meatballMinDistance = 7f;
    [SerializeField] private float meatballCooldown = 8f;
    [SerializeField] private float meatballFlightDuration = 1.25f;
    [SerializeField] private float meatballArcHeight = 2.3f;
    [SerializeField] private float meatballDamage = 14f;
    [SerializeField] private float meatballExplosionRadius = 1.35f;
    [SerializeField] private float meatballWindup = 0.55f;
    [SerializeField, Range(0f, 1.5f)] private float meatballPredictionStrength = 0.8f;
    [SerializeField, Min(0f)] private float meatballMaximumLeadDistance = 3.2f;
    [SerializeField, Min(0f)] private float meatballImprecisionRadius = 1.15f;
    [SerializeField, Range(0f, 0.5f)] private float meatballCooldownVariation = 0.18f;

    private static Projectile fallbackSpikePrefab;
    private Rigidbody2D rb;
    private Collider2D ownHitbox;
    private float nextSpikeAt;
    private float nextMeatballAt;
    private bool attacking;
    private HopPhase hopPhase;
    private float hopPhaseEndsAt;
    private float nextHopAt;
    private float nextSplashAt;

    private static readonly int IsMovingParameter = Animator.StringToHash("IsMoving");
    private static readonly int IsAttackingParameter = Animator.StringToHash("IsAttacking");
    private static readonly int JumpParameter = Animator.StringToHash("Jump");
    private static readonly int SpikeAttackParameter = Animator.StringToHash("SpikeAttack");
    private static readonly int MeatballAttackParameter = Animator.StringToHash("MeatballAttack");

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
        nextSpikeAt = Time.time + Random.Range(0.5f, spikeCooldown);
        nextMeatballAt = Time.time + meatballCooldown * Random.Range(0.82f, 1.18f);
        attacking = false;
        hopPhase = HopPhase.Resting;
        nextHopAt = Time.time + restBetweenJumps;
        SetAnimatorBool(IsMovingParameter, false);
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

        Vector2 direction = toTarget / distance;
        if (distance > preferredDistance)
            rb.linearVelocity = direction * moveSpeed;
        else if (distance < retreatDistance)
            rb.linearVelocity = -direction * (moveSpeed * 0.7f);
        else
            rb.linearVelocity = Vector2.zero;

        PlaySplashStepIfMoving();
    }

    private void Update()
    {
        UpdateHopCycle();
        if (target != null)
            UpdateFacing(GetTargetCenter().x - GetEnemyCenter().x);
        if (target == null || attacking)
            return;

        float distance = Vector2.Distance(GetEnemyCenter(), GetTargetCenter());
        if (distance >= meatballMinDistance && Time.time >= nextMeatballAt)
            StartCoroutine(MeatballAttackRoutine());
        else if (distance <= spikeRange && Time.time >= nextSpikeAt)
            StartCoroutine(SpikeAttackRoutine());
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

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        ResetHop();
    }

    public void ApplyLateGameAttackScaling(int tier)
    {
        if (tier <= 0)
            return;

        spikeCount = Mathf.Clamp(spikeCount + tier / 3, 1, 7);
        spikeSpreadDegrees = Mathf.Min(34f, spikeSpreadDegrees + tier * 1.4f);
        spikeCooldown = Mathf.Max(0.95f, spikeCooldown * (1f - Mathf.Min(0.48f, tier * 0.07f)));
        spikeSpeed *= 1f + Mathf.Min(0.35f, tier * 0.035f);
        spikeDamage *= 1f + Mathf.Min(0.5f, tier * 0.05f);

        meatballCooldown = Mathf.Max(3.2f, meatballCooldown * (1f - Mathf.Min(0.5f, tier * 0.065f)));
        meatballDamage *= 1f + Mathf.Min(0.55f, tier * 0.055f);
        meatballExplosionRadius *= 1f + Mathf.Min(0.3f, tier * 0.03f);
        meatballPredictionStrength = Mathf.Min(1.25f, meatballPredictionStrength + tier * 0.035f);
        meatballImprecisionRadius = Mathf.Max(0.35f, meatballImprecisionRadius - tier * 0.06f);
    }

    private IEnumerator SpikeAttackRoutine()
    {
        BeginAttack(SpikeAttackParameter);
        yield return new WaitForSeconds(Mathf.Max(0f, spikeWindup));
        if (target != null)
            FireSpikeSpread();
        nextSpikeAt = Time.time + Mathf.Max(0.1f, spikeCooldown);
        EndAttack();
    }

    private IEnumerator MeatballAttackRoutine()
    {
        BeginAttack(MeatballAttackParameter);
        yield return new WaitForSeconds(Mathf.Max(0f, meatballWindup));
        if (target != null)
            ThrowMeatball();
        float cooldownVariation = meatballCooldown * meatballCooldownVariation;
        nextMeatballAt = Time.time + Mathf.Max(1f,
            meatballCooldown + Random.Range(-cooldownVariation, cooldownVariation));
        nextSpikeAt = Mathf.Max(nextSpikeAt, Time.time + 0.75f);
        EndAttack();
    }

    private void BeginAttack(int trigger)
    {
        attacking = true;
        StopMovement();
        hopPhase = HopPhase.Resting;
        SetAnimatorBool(IsMovingParameter, false);
        SetAnimatorBool(IsAttackingParameter, true);
        SetAnimatorTrigger(trigger);
    }

    private void EndAttack()
    {
        attacking = false;
        SetAnimatorBool(IsAttackingParameter, false);
        ResetHop();
    }

    private void ResetHop()
    {
        hopPhase = HopPhase.Resting;
        nextHopAt = Time.time + Mathf.Max(0f, restBetweenJumps);
        SetAnimatorBool(IsMovingParameter, false);
    }

    private void FireSpikeSpread()
    {
        Vector2 centerDirection = GetTargetCenter() - GetEnemyCenter();
        if (centerDirection.sqrMagnitude <= 0.001f)
            centerDirection = Vector2.down;

        int count = Mathf.Max(1, spikeCount);
        Projectile prefab = spikeProjectilePrefab != null ? spikeProjectilePrefab : CreateFallbackSpikePrefab();
        for (int i = 0; i < count; i++)
        {
            float t = count == 1 ? 0.5f : i / (float)(count - 1);
            float angle = Mathf.Lerp(-spikeSpreadDegrees * 0.5f, spikeSpreadDegrees * 0.5f, t);
            Vector2 direction = Quaternion.Euler(0f, 0f, angle) * centerDirection.normalized;
            Projectile spike = Instantiate(prefab, GetProjectileOrigin(), prefab.transform.rotation);
            spike.gameObject.SetActive(true);
            spike.Launch(direction, spikeSpeed, spikeDamage, CombatFaction.Enemy, spikeLifetime);
        }
    }

    private void ThrowMeatball()
    {
        Vector3 start = GetProjectileOrigin();
        MeatballProjectile meatball;
        if (meatballPrefab != null)
            meatball = Instantiate(meatballPrefab, start, meatballPrefab.transform.rotation);
        else
        {
            GameObject fallback = new("Meatball Projectile");
            fallback.transform.position = start;
            SpriteRenderer renderer = fallback.AddComponent<SpriteRenderer>();
            renderer.sprite = CreateFallbackSprite();
            renderer.color = new Color(0.55f, 0.12f, 0.12f);
            meatball = fallback.AddComponent<MeatballProjectile>();
        }

        meatball.Launch(GetPredictedMeatballDestination(), meatballFlightDuration, meatballArcHeight, meatballDamage,
            meatballExplosionRadius, CombatFaction.Enemy);
    }

    private Vector2 GetPredictedMeatballDestination()
    {
        Vector2 currentTargetPosition = GetTargetCenter();
        Rigidbody2D targetBody = target != null ? target.GetComponentInParent<Rigidbody2D>() : null;
        Vector2 targetVelocity = targetBody != null ? targetBody.linearVelocity : Vector2.zero;

        Vector2 predictedOffset = targetVelocity * (meatballFlightDuration * meatballPredictionStrength);
        predictedOffset = Vector2.ClampMagnitude(predictedOffset, meatballMaximumLeadDistance);

        // Each Meashy rolls its own miss direction, producing a useful spread
        // instead of stacking every meatball on exactly the same destination.
        Vector2 imprecision = Random.insideUnitCircle * meatballImprecisionRadius;
        return currentTargetPosition + predictedOffset + imprecision;
    }

    private Vector3 GetProjectileOrigin() => projectileOrigin != null ? projectileOrigin.position : (Vector3)GetEnemyCenter();

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

    private void StopMovement()
    {
        if (rb != null)
            rb.linearVelocity = Vector2.zero;
    }

    private void PlaySplashStepIfMoving()
    {
        if (rb == null || Time.time < nextSplashAt ||
            rb.linearVelocity.sqrMagnitude < splashMovementThreshold * splashMovementThreshold)
            return;

        GameAudio.PlayTinySplash();
        nextSplashAt = Time.time + Mathf.Max(0.08f, splashStepInterval);
    }

    private void ResolveAnimator()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);
        if (visualRenderer == null && animator != null)
            visualRenderer = animator.GetComponentInChildren<SpriteRenderer>(true);
        if (visualRenderer == null)
            visualRenderer = GetComponentInChildren<SpriteRenderer>(true);
    }

    private void UpdateFacing(float horizontalDirection)
    {
        if (Mathf.Abs(horizontalDirection) <= 0.01f)
            return;

        ResolveAnimator();
        if (visualRenderer != null)
            visualRenderer.flipX = spriteFacesRight
                ? horizontalDirection < 0f
                : horizontalDirection > 0f;
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

    private static Projectile CreateFallbackSpikePrefab()
    {
        if (fallbackSpikePrefab != null)
            return fallbackSpikePrefab;
        GameObject projectileObject = new("Meashy Spike Projectile");
        projectileObject.SetActive(false);
        projectileObject.AddComponent<CircleCollider2D>().radius = 0.12f;
        projectileObject.AddComponent<Rigidbody2D>();
        Projectile projectile = projectileObject.AddComponent<Projectile>();
        SpriteRenderer renderer = projectileObject.AddComponent<SpriteRenderer>();
        renderer.sprite = CreateFallbackSprite();
        renderer.color = new Color(0.74f, 0.29f, 0.27f);
        fallbackSpikePrefab = projectile;
        return fallbackSpikePrefab;
    }

    private static Sprite CreateFallbackSprite()
    {
        Texture2D texture = new(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 8f);
    }
}

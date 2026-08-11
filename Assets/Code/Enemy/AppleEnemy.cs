using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class AppleEnemy : MonoBehaviour
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
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float preferredDistance = 4.5f;
    [SerializeField] private float retreatDistance = 2.8f;
    [SerializeField] private float strafeSpeed = 1.6f;
    [SerializeField] private float strafeSwitchInterval = 0.85f;

    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Eight-Way Fruit Burst")]
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private Sprite[] projectileSprites = new Sprite[3];
    [SerializeField] private Transform projectileOrigin;
    [SerializeField] private float attackRange = 7f;
    [SerializeField] private float chargeDuration = 0.55f;
    [SerializeField] private float attackRecoveryDuration = 0.3f;
    [SerializeField] private float minAttackCooldown = 2.8f;
    [SerializeField] private float maxAttackCooldown = 3.5f;
    [SerializeField] private float projectileSpeed = 6.5f;
    [SerializeField] private float projectileDamage = 6f;
    [SerializeField] private float projectileLifetime = 2.8f;
    [SerializeField] private float projectileVisualWorldSize = 0.45f;
    [SerializeField] private float patternAngleOffset;
    [SerializeField, Min(1)] private int burstPatternRepeats = 1;

    private static Projectile fallbackProjectilePrefab;
    private Rigidbody2D rb;
    private Collider2D ownHitbox;
    private Health health;
    private bool attacking;
    private int strafeDirection = 1;
    private float nextStrafeSwitchAt;
    private float nextAttackAt;
    private float nextHopAt;
    private float hopPhaseEndsAt;
    private HopPhase hopPhase;
    private bool deathSoundPlayed;

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
        health = GetComponent<Health>();
        ResolveVisuals();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    private void OnEnable()
    {
        attacking = false;
        deathSoundPlayed = false;
        if (health != null)
        {
            health.Died -= HandleDied;
            health.Died += HandleDied;
        }
        strafeDirection = Random.value < 0.5f ? -1 : 1;
        nextStrafeSwitchAt = Time.time + Mathf.Max(0.1f, strafeSwitchInterval);
        nextAttackAt = Time.time + Random.Range(1f, 1.8f);
        ResetHop();
    }

    private void OnDisable()
    {
        if (health != null)
            health.Died -= HandleDied;
    }

    private void Update()
    {
        UpdateFacingPlayer();
        UpdateHopCycle();

        if (target == null || attacking || Time.time < nextAttackAt)
            return;

        if (Vector2.Distance(GetEnemyCenter(), GetTargetCenter()) <= attackRange)
            StartCoroutine(AttackRoutine());
    }

    private void FixedUpdate()
    {
        if (target == null || attacking || hopPhase != HopPhase.Moving)
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

    public void ApplyLateGameAttackScaling(int tier)
    {
        if (tier <= 0)
            return;

        float cooldownMultiplier = 1f - Mathf.Min(0.45f, tier * 0.06f);
        minAttackCooldown = Mathf.Max(1.25f, minAttackCooldown * cooldownMultiplier);
        maxAttackCooldown = Mathf.Max(minAttackCooldown + 0.2f, maxAttackCooldown * cooldownMultiplier);
        projectileSpeed *= 1f + Mathf.Min(0.35f, tier * 0.035f);
        projectileDamage *= 1f + Mathf.Min(0.45f, tier * 0.045f);
        burstPatternRepeats = Mathf.Clamp(1 + tier / 4, 1, 3);
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

    private IEnumerator AttackRoutine()
    {
        attacking = true;
        StopMovement();
        hopPhase = HopPhase.Resting;
        SetAnimatorBool(IsMovingParameter, false);
        SetAnimatorTrigger(ChargeParameter);

        yield return new WaitForSeconds(Mathf.Max(0f, chargeDuration));

        SetAnimatorTrigger(AttackParameter);
        FireEightWayBurst();
        yield return new WaitForSeconds(Mathf.Max(0f, attackRecoveryDuration));

        float minimum = Mathf.Max(0.1f, minAttackCooldown);
        float maximum = Mathf.Max(minimum, maxAttackCooldown);
        nextAttackAt = Time.time + Random.Range(minimum, maximum);
        attacking = false;
        strafeDirection *= -1;
        ResetHop();
    }

    private void FireEightWayBurst()
    {
        Projectile prefab = projectilePrefab != null ? projectilePrefab : CreateFallbackProjectilePrefab();
        Vector3 origin = projectileOrigin != null ? projectileOrigin.position : (Vector3)GetEnemyCenter();

        int patternCount = Mathf.Max(1, burstPatternRepeats);
        for (int pattern = 0; pattern < patternCount; pattern++)
        {
            float repeatOffset = pattern * (45f / patternCount);
            for (int i = 0; i < 8; i++)
            {
                float angle = patternAngleOffset + repeatOffset + i * 45f;
                Vector2 direction = Quaternion.Euler(0f, 0f, angle) * Vector2.right;
                Projectile projectile = Instantiate(prefab, origin, prefab.transform.rotation);
                projectile.gameObject.SetActive(true);
                ApplyRandomProjectileSprite(projectile);
                projectile.Launch(direction, projectileSpeed, projectileDamage,
                    CombatFaction.Enemy, projectileLifetime);
            }
        }
    }

    private void ApplyRandomProjectileSprite(Projectile projectile)
    {
        if (projectile == null || projectileSprites == null || projectileSprites.Length == 0)
            return;

        Sprite chosenSprite = null;
        int start = Random.Range(0, projectileSprites.Length);
        for (int i = 0; i < projectileSprites.Length; i++)
        {
            Sprite candidate = projectileSprites[(start + i) % projectileSprites.Length];
            if (candidate != null)
            {
                chosenSprite = candidate;
                break;
            }
        }

        if (chosenSprite == null)
            return;

        SpriteRenderer renderer = projectile.GetComponentInChildren<SpriteRenderer>(true);
        if (renderer == null)
            return;

        renderer.sprite = chosenSprite;
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

    private void HandleDied()
    {
        if (deathSoundPlayed)
            return;

        deathSoundPlayed = true;
        GameAudio.PlayAppleDead();
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

    private void UpdateFacingPlayer()
    {
        if (target == null)
            return;

        float horizontalDirection = GetTargetCenter().x - GetEnemyCenter().x;
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

        GameObject projectileObject = new("Apple Fruit Projectile");
        projectileObject.SetActive(false);
        projectileObject.AddComponent<CircleCollider2D>().radius = 0.14f;
        projectileObject.AddComponent<Rigidbody2D>();
        Projectile projectile = projectileObject.AddComponent<Projectile>();
        SpriteRenderer renderer = projectileObject.AddComponent<SpriteRenderer>();
        renderer.sprite = CreateFallbackSprite();
        renderer.color = new Color(0.85f, 0.25f, 0.2f);
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

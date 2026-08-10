using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PotatoEnemy : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer visualRenderer;
    [SerializeField] private bool spriteFacesRight = false;

    [Header("Hop Movement")]
    [SerializeField] private float preJumpDuration = 0.08f;
    [SerializeField] private float movementBurstDuration = 0.48f;
    [SerializeField] private float postJumpDuration = 0.08f;
    [SerializeField] private float restBetweenJumps = 0.08f;
    [SerializeField] private float moveSpeed = 3.8f;

    [Header("Attack Positioning")]
    [Tooltip("Distance it tries to keep on one of the four cardinal sides of the player.")]
    [SerializeField] private float preferredAttackDistance = 3.6f;
    [SerializeField] private float positionTolerance = 0.45f;

    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Four-Way Burst")]
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private Transform projectileOrigin;
    [SerializeField] private float attackRange = 6.5f;
    [SerializeField] private float preAttackDuration = 0.35f;
    [SerializeField] private float attackRecoveryDuration = 0.2f;
    [SerializeField] private float minAttackCooldown = 1.5f;
    [SerializeField] private float maxAttackCooldown = 2.2f;
    [SerializeField] private float projectileSpeed = 7f;
    [SerializeField] private float projectileDamage = 8f;
    [SerializeField] private float projectileLifetime = 2.2f;
    [Tooltip("Rotates the whole four-way pattern without aiming it at the player.")]
    [SerializeField] private float burstAngleOffset;

    private static Projectile fallbackProjectilePrefab;
    private Rigidbody2D rb;
    private Collider2D ownHitbox;
    private bool attacking;
    private Vector2 attackLane = Vector2.right;
    private float nextAttackAt;
    private float hopPhaseEndsAt;
    private float nextHopAt;
    private HopPhase hopPhase;

    private static readonly int IsMovingParameter = Animator.StringToHash("IsMoving");
    private static readonly int PreJumpParameter = Animator.StringToHash("PreJump");
    private static readonly int JumpParameter = Animator.StringToHash("Jump");
    private static readonly int PostJumpParameter = Animator.StringToHash("PostJump");
    private static readonly int PreAttackParameter = Animator.StringToHash("PreAttack");
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
        nextAttackAt = Time.time + Random.Range(0.25f, 0.75f);
        ResetHop();
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

        Vector2 desiredPosition = GetDesiredAttackPosition();
        Vector2 toDesiredPosition = desiredPosition - GetEnemyCenter();
        if (toDesiredPosition.magnitude <= Mathf.Max(0.05f, positionTolerance))
        {
            StopMovement();
            return;
        }

        rb.linearVelocity = toDesiredPosition.normalized * moveSpeed;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        ChooseClosestAttackLane();
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

    private IEnumerator AttackRoutine()
    {
        attacking = true;
        StopMovement();
        hopPhase = HopPhase.Resting;
        SetAnimatorBool(IsMovingParameter, false);
        SetAnimatorTrigger(PreAttackParameter);

        yield return new WaitForSeconds(Mathf.Max(0f, preAttackDuration));

        SetAnimatorTrigger(AttackParameter);
        FireFourWayBurst();
        yield return new WaitForSeconds(Mathf.Max(0f, attackRecoveryDuration));

        float minimum = Mathf.Max(0.1f, minAttackCooldown);
        float maximum = Mathf.Max(minimum, maxAttackCooldown);
        nextAttackAt = Time.time + Random.Range(minimum, maximum);
        ChooseDifferentAttackLane();
        attacking = false;
        ResetHop();
    }

    private Vector2 GetDesiredAttackPosition()
    {
        return GetTargetCenter() + attackLane * Mathf.Max(0.5f, preferredAttackDistance);
    }

    private void ChooseClosestAttackLane()
    {
        if (target == null)
            return;

        Vector2 relative = GetEnemyCenter() - GetTargetCenter();
        if (Mathf.Abs(relative.x) >= Mathf.Abs(relative.y))
            attackLane = relative.x >= 0f ? Vector2.right : Vector2.left;
        else
            attackLane = relative.y >= 0f ? Vector2.up : Vector2.down;
    }

    private void ChooseDifferentAttackLane()
    {
        Vector2[] lanes = { Vector2.up, Vector2.right, Vector2.down, Vector2.left };
        int start = Random.Range(0, lanes.Length);
        for (int i = 0; i < lanes.Length; i++)
        {
            Vector2 candidate = lanes[(start + i) % lanes.Length];
            if (candidate != attackLane)
            {
                attackLane = candidate;
                return;
            }
        }
    }

    private void FireFourWayBurst()
    {
        Vector2[] directions = { Vector2.up, Vector2.right, Vector2.down, Vector2.left };
        Quaternion patternRotation = Quaternion.Euler(0f, 0f, burstAngleOffset);
        Projectile prefab = projectilePrefab != null ? projectilePrefab : CreateFallbackProjectilePrefab();
        Vector3 origin = projectileOrigin != null ? projectileOrigin.position : (Vector3)GetEnemyCenter();

        foreach (Vector2 cardinalDirection in directions)
        {
            Vector2 direction = patternRotation * cardinalDirection;
            Projectile projectile = Instantiate(prefab, origin, prefab.transform.rotation);
            projectile.gameObject.SetActive(true);
            projectile.Launch(direction, projectileSpeed, projectileDamage,
                CombatFaction.Enemy, projectileLifetime);
        }
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

        GameObject projectileObject = new("Potato Projectile");
        projectileObject.SetActive(false);
        projectileObject.AddComponent<CircleCollider2D>().radius = 0.14f;
        projectileObject.AddComponent<Rigidbody2D>();
        Projectile projectile = projectileObject.AddComponent<Projectile>();
        SpriteRenderer renderer = projectileObject.AddComponent<SpriteRenderer>();
        renderer.sprite = CreateFallbackSprite();
        renderer.color = new Color(0.72f, 0.5f, 0.28f);
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

using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class CheeseMinionEnemy : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer visualRenderer;
    [SerializeField] private bool spriteFacesRight = false;
    [SerializeField] private Transform crumbOrigin;

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
    private ParticleSystem runningCrumbParticles;
    private float crumbEmissionAccumulator;
    private Vector3 crumbOriginBaseLocalPosition;

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
        if (crumbOrigin != null)
            crumbOriginBaseLocalPosition = transform.InverseTransformPoint(crumbOrigin.position);

        health = GetComponent<Health>();
        DamageDealer oldRootDealer = GetComponent<DamageDealer>();
        if (oldRootDealer != null)
            oldRootDealer.enabled = false;

        GameObject contactZone = new("ContactDamageTrigger");
        contactZone.transform.SetParent(transform, false);
        ConfigureContactCollider(contactZone);
        contactDamageDealer = contactZone.AddComponent<DamageDealer>();
        contactDamageDealer.Configure(CombatFaction.Enemy, contactDamage, false);
        contactDamageDealer.DamageApplied += HandleContactDamageApplied;

        CreateRunningCrumbParticles();
    }

    private void LateUpdate()
    {
        if (runningCrumbParticles == null || rb == null || Time.timeScale <= 0f)
            return;

        Vector2 velocity = rb.linearVelocity;
        float speed = velocity.magnitude;
        if (speed < 0.25f || state == State.Born)
            return;

        crumbEmissionAccumulator += Time.deltaTime * Mathf.Lerp(9f, 24f, Mathf.Clamp01(speed / chaseSpeed));
        int emitCount = Mathf.FloorToInt(crumbEmissionAccumulator);
        if (emitCount <= 0)
            return;

        crumbEmissionAccumulator -= emitCount;
        Vector2 direction = velocity.normalized;
        float edgeDistance = visualRenderer != null
            ? Mathf.Max(0.18f, visualRenderer.bounds.extents.x * 0.78f)
            : 0.35f;
        Vector3 trailingTip;
        if (crumbOrigin != null)
        {
            Vector3 mirroredOrigin = crumbOriginBaseLocalPosition;
            if (visualRenderer != null && visualRenderer.flipX)
                mirroredOrigin.x = -mirroredOrigin.x;
            trailingTip = transform.TransformPoint(mirroredOrigin);
        }
        else
        {
            trailingTip = transform.position - (Vector3)(direction * edgeDistance);
        }

        for (int i = 0; i < emitCount; i++)
        {
            Vector2 sideways = new(-direction.y, direction.x);
            ParticleSystem.EmitParams particle = new();
            particle.position = trailingTip + (Vector3)(sideways * Random.Range(-0.1f, 0.1f));
            particle.velocity = (-direction * Random.Range(0.65f, 1.35f)) +
                (sideways * Random.Range(-0.42f, 0.42f));
            particle.startLifetime = Random.Range(0.28f, 0.52f);
            particle.startSize = Random.Range(0.11f, 0.22f);
            particle.rotation3D = new Vector3(0f, 0f, Random.Range(0f, 360f));
            particle.startColor = new Color(1f, Random.Range(0.7f, 0.9f), 0.03f, 0.92f);
            runningCrumbParticles.Emit(particle, 1);
        }
    }

    private void CreateRunningCrumbParticles()
    {
        GameObject particlesObject = new("RunningCheeseCrumbs");
        particlesObject.transform.SetParent(transform, false);
        runningCrumbParticles = particlesObject.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = runningCrumbParticles.main;
        main.playOnAwake = false;
        main.loop = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 90;
        main.startSpeed = 0f;
        main.startLifetime = 0.4f;
        main.startSize = 0.16f;
        main.startColor = new Color(1f, 0.82f, 0.04f, 0.92f);

        ParticleSystem.EmissionModule emission = runningCrumbParticles.emission;
        emission.enabled = false;

        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = runningCrumbParticles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f,
            new AnimationCurve(
                new Keyframe(0f, 0.45f),
                new Keyframe(0.18f, 1f),
                new Keyframe(1f, 0f)));

        ParticleSystemRenderer particleRenderer = particlesObject.GetComponent<ParticleSystemRenderer>();
        particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        if (visualRenderer != null)
        {
            particleRenderer.sortingLayerID = visualRenderer.sortingLayerID;
            particleRenderer.sortingOrder = visualRenderer.sortingOrder + 1;
        }

        Shader particleShader = Shader.Find("Sprites/Default");
        if (particleShader != null)
            particleRenderer.material = new Material(particleShader);
    }

    private void ConfigureContactCollider(GameObject contactZone)
    {
        BoxCollider2D bodyBox = GetComponent<BoxCollider2D>();
        if (bodyBox != null && bodyBox.enabled)
        {
            BoxCollider2D contactBox = contactZone.AddComponent<BoxCollider2D>();
            contactBox.isTrigger = true;
            contactBox.offset = bodyBox.offset;
            contactBox.size = new Vector2(bodyBox.size.x * 0.34f, bodyBox.size.y * 0.56f);
            return;
        }

        CircleCollider2D bodyCircle = GetComponent<CircleCollider2D>();
        CircleCollider2D contactCircle = contactZone.AddComponent<CircleCollider2D>();
        contactCircle.isTrigger = true;
        contactCircle.offset = bodyCircle != null ? bodyCircle.offset : Vector2.zero;
        contactCircle.radius = bodyCircle != null && bodyCircle.enabled
            ? bodyCircle.radius * 0.52f
            : Mathf.Max(0.2f, contactRadius * 0.55f);
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
            Vector2 awayFromTarget = target != null
                ? rb.position - (Vector2)target.position
                : Vector2.zero;
            rb.linearVelocity = awayFromTarget.sqrMagnitude > 0.001f
                ? awayFromTarget.normalized * (chaseSpeed * 0.7f)
                : Vector2.right * (chaseSpeed * 0.7f);
            SetAnimatorBool(IsMovingParameter, true);
            if (Time.time >= recoveryEndsAt)
            {
                if (contactDamageDealer != null)
                    contactDamageDealer.enabled = true;
                SetState(target != null ? State.Chasing : State.Searching);
            }
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
        if (contactDamageDealer != null)
            contactDamageDealer.enabled = false;
        SetState(State.Recovering);
        SetAnimatorBool(IsMovingParameter, true);
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

using System.Collections;
using UnityEngine;

public class MilkDropperEnemy : MonoBehaviour
{
    [Header("Damage Hitbox")]
    [Tooltip("Trigger used to receive damage without physically blocking or being pushed by actors.")]
    [SerializeField] private Collider2D damageHitbox;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    [Header("Facing")]
    [SerializeField] private SpriteRenderer visualRenderer;

    [Header("Initial Encounter Composition")]
    [SerializeField] private GameObject cheeseMinionPrefab;
    [SerializeField, Min(1)] private int minInitialDroppers = 1;
    [SerializeField, Min(1)] private int maxInitialDroppers = 2;

    [Header("Cheese Production")]
    [SerializeField] private Transform dropOrigin;
    [SerializeField] private float minDropInterval = 8f;
    [SerializeField] private float maxDropInterval = 12f;
    [SerializeField, Min(1)] private int maxLivingSummons = 3;
    [SerializeField] private float spawnRadius = 0.45f;

    private Transform target;
    private RoomController room;
    private Coroutine dropRoutine;
    private int livingSummons;

    private static readonly int DropParameter = Animator.StringToHash("Drop");

    public GameObject CheeseMinionPrefab => cheeseMinionPrefab;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);
        if (visualRenderer == null && animator != null)
            visualRenderer = animator.GetComponentInChildren<SpriteRenderer>(true);
        if (visualRenderer == null)
            visualRenderer = GetComponentInChildren<SpriteRenderer>(true);

        ConfigureDamageHitbox();
    }

    private void OnEnable()
    {
        if (visualRenderer != null)
            visualRenderer.flipX = Random.value < 0.5f;
    }

    public void ConfigureDamageHitbox()
    {
        if (damageHitbox == null)
            damageHitbox = GetComponent<Collider2D>();

        if (damageHitbox == null)
            damageHitbox = gameObject.AddComponent<CircleCollider2D>();

        damageHitbox.isTrigger = true;

        // A trigger still receives projectile callbacks, but it cannot block or
        // be pushed by the player, cheese minions, or other enemies.
        Collider2D[] rootColliders = GetComponents<Collider2D>();
        foreach (Collider2D rootCollider in rootColliders)
        {
            if (rootCollider != null)
                rootCollider.isTrigger = true;
        }
    }

    private void OnDisable()
    {
        if (dropRoutine != null)
        {
            StopCoroutine(dropRoutine);
            dropRoutine = null;
        }
    }

    public int RollInitialDropperCount(int availableSpawnCount)
    {
        int minimum = Mathf.Clamp(minInitialDroppers, 1, Mathf.Max(1, availableSpawnCount));
        int maximum = Mathf.Clamp(maxInitialDroppers, minimum, Mathf.Max(1, availableSpawnCount));
        return Random.Range(minimum, maximum + 1);
    }

    public void SetCombatContext(Transform playerTarget, RoomController ownerRoom)
    {
        target = playerTarget;
        room = ownerRoom;
        if (dropRoutine != null)
            StopCoroutine(dropRoutine);
        dropRoutine = StartCoroutine(DropLoop());
    }

    private IEnumerator DropLoop()
    {
        while (target != null && room != null && room.State == RoomState.Combat)
        {
            float minimum = Mathf.Max(0.5f, minDropInterval);
            float maximum = Mathf.Max(minimum, maxDropInterval);
            yield return new WaitForSeconds(Random.Range(minimum, maximum));

            if (target == null || room == null || room.State != RoomState.Combat)
                break;
            if (livingSummons >= maxLivingSummons)
                continue;

            SpawnCheeseMinion();
        }

        dropRoutine = null;
    }

    private void SpawnCheeseMinion()
    {
        if (cheeseMinionPrefab == null)
        {
            Debug.LogWarning($"{name} cannot drop cheese because Cheese Minion Prefab is not assigned.", this);
            return;
        }

        SetAnimatorTrigger(DropParameter);
        Vector2 offset = Random.insideUnitCircle * Mathf.Max(0f, spawnRadius);
        Vector3 origin = dropOrigin != null ? dropOrigin.position : transform.position;
        origin += (Vector3)offset;

        GameObject instance = Instantiate(cheeseMinionPrefab, origin, cheeseMinionPrefab.transform.rotation, room.transform);
        Health health = instance.GetComponent<Health>();
        if (health == null)
            health = instance.AddComponent<Health>();
        health.Configure(health.MaxHealth, true, true);

        FactionMember faction = instance.GetComponent<FactionMember>();
        if (faction == null)
            faction = instance.AddComponent<FactionMember>();
        faction.SetFaction(CombatFaction.Enemy);

        EnemyDeathNotifier notifier = instance.GetComponent<EnemyDeathNotifier>();
        if (notifier == null)
            notifier = instance.AddComponent<EnemyDeathNotifier>();

        CheeseMinionEnemy cheese = instance.GetComponent<CheeseMinionEnemy>();
        if (cheese == null)
            cheese = instance.AddComponent<CheeseMinionEnemy>();
        cheese.SetTarget(target);
        cheese.BeginDroppedBirth();

        if (!room.RegisterSpawnedEnemy(notifier))
        {
            Destroy(instance);
            return;
        }

        livingSummons++;
        notifier.Died -= HandleSummonDied;
        notifier.Died += HandleSummonDied;
    }

    private void HandleSummonDied(EnemyDeathNotifier summon)
    {
        if (summon != null)
            summon.Died -= HandleSummonDied;
        livingSummons = Mathf.Max(0, livingSummons - 1);
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

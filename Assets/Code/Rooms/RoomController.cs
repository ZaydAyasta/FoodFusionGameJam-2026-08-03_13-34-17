using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class RoomController : MonoBehaviour
{
    [SerializeField] private DoorController[] doors;
    [SerializeField] private EnemyDeathNotifier[] enemies;
    [SerializeField] private bool activateEnemiesOnEntry = true;
    [SerializeField] private float enemyWakeUpDelay = 1.3f;
    [Header("Rewards")]
    [SerializeField] private Transform rewardSpawnPoint;
    [SerializeField] private RewardChoiceController rewardChoicePrefab;
    [SerializeField] private IngredientData promisedReward;

    private readonly HashSet<EnemyDeathNotifier> aliveEnemies = new();
    private bool entered;
    private bool rewardSpawned;
    private bool rewardClaimed;
    private bool hadEnemiesInCombat;
    private RewardChoiceController activeRewardChoice;
    private Coroutine enemyWakeUpRoutine;

    public RoomState State { get; private set; } = RoomState.Inactive;

    private void Awake()
    {
        Collider2D trigger = GetComponent<Collider2D>();
        trigger.isTrigger = true;

        if (doors == null || doors.Length == 0)
            doors = GetComponentsInChildren<DoorController>(true);

        if (enemies == null || enemies.Length == 0)
            enemies = GetComponentsInChildren<EnemyDeathNotifier>(true);

        SetDoorsClosed(false);

        if (activateEnemiesOnEntry)
        {
            foreach (EnemyDeathNotifier enemy in enemies)
            {
                if (enemy != null)
                    enemy.gameObject.SetActive(false);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryBeginCombat(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryBeginCombat(other);
    }

    private void TryBeginCombat(Collider2D other)
    {
        if (entered || State != RoomState.Inactive)
            return;

        CharacterInput playerInput = other.GetComponentInParent<CharacterInput>();
        if (playerInput == null)
            return;

        FactionMember faction = playerInput.GetComponent<FactionMember>();
        if (faction == null || faction.Faction != CombatFaction.Player)
            return;

        BeginCombat(playerInput.transform);
    }

    private void BeginCombat(Transform player)
    {
        entered = true;
        State = RoomState.Combat;
        aliveEnemies.Clear();
        SetDoorsClosed(true);

        foreach (EnemyDeathNotifier enemy in enemies)
        {
            if (enemy == null)
                continue;

            enemy.gameObject.SetActive(true);
            enemy.Arm();
            enemy.Died -= HandleEnemyDied;
            enemy.Died += HandleEnemyDied;
            aliveEnemies.Add(enemy);
            StopEnemyMotion(enemy);
        }

        hadEnemiesInCombat = aliveEnemies.Count > 0;

        if (aliveEnemies.Count == 0)
        {
            CompleteCombat();
            return;
        }

        if (enemyWakeUpRoutine != null)
            StopCoroutine(enemyWakeUpRoutine);

        enemyWakeUpRoutine = StartCoroutine(WakeEnemiesAfterDelay(player));
    }

    public void ConfigureProcedural(
        DoorController[] roomDoors,
        EnemyDeathNotifier[] roomEnemies,
        Transform roomRewardSpawnPoint,
        IngredientData roomPromisedReward)
    {
        doors = roomDoors ?? System.Array.Empty<DoorController>();
        enemies = roomEnemies ?? System.Array.Empty<EnemyDeathNotifier>();
        rewardSpawnPoint = roomRewardSpawnPoint;
        promisedReward = roomPromisedReward;
        entered = false;
        rewardSpawned = false;
        rewardClaimed = false;
        hadEnemiesInCombat = false;
        activeRewardChoice = null;
        if (enemyWakeUpRoutine != null)
            StopCoroutine(enemyWakeUpRoutine);

        enemyWakeUpRoutine = null;
        aliveEnemies.Clear();
        State = RoomState.Inactive;

        SetDoorsClosed(false);

        if (!activateEnemiesOnEntry)
            return;

        foreach (EnemyDeathNotifier enemy in enemies)
        {
            if (enemy != null)
                enemy.gameObject.SetActive(false);
        }
    }

    private void HandleEnemyDied(EnemyDeathNotifier enemy)
    {
        enemy.Died -= HandleEnemyDied;
        aliveEnemies.Remove(enemy);

        if (State == RoomState.Combat && aliveEnemies.Count == 0)
            CompleteCombat();
    }

    private IEnumerator WakeEnemiesAfterDelay(Transform player)
    {
        float wakeAt = Time.time + Mathf.Max(0f, enemyWakeUpDelay);
        while (Time.time < wakeAt)
        {
            EnemyDeathNotifier[] snapshot = new EnemyDeathNotifier[aliveEnemies.Count];
            aliveEnemies.CopyTo(snapshot);
            foreach (EnemyDeathNotifier enemy in snapshot)
                StopEnemyMotion(enemy);

            yield return null;
        }

        EnemyDeathNotifier[] enemiesToWake = new EnemyDeathNotifier[aliveEnemies.Count];
        aliveEnemies.CopyTo(enemiesToWake);
        foreach (EnemyDeathNotifier enemy in enemiesToWake)
        {
            if (enemy == null)
                continue;

            ChaserEnemy chaser = enemy.GetComponent<ChaserEnemy>();
            if (chaser != null)
                chaser.SetTarget(player);

            RangedEnemy ranged = enemy.GetComponent<RangedEnemy>();
            if (ranged != null)
                ranged.SetTarget(player);

            RiceEnemy rice = enemy.GetComponent<RiceEnemy>();
            if (rice != null)
                rice.SetTarget(player);

            MeashyEnemy meashy = enemy.GetComponent<MeashyEnemy>();
            if (meashy != null)
                meashy.SetTarget(player);
        }

        enemyWakeUpRoutine = null;
    }

    private static void StopEnemyMotion(EnemyDeathNotifier enemy)
    {
        if (enemy == null)
            return;

        Rigidbody2D[] bodies = enemy.GetComponentsInChildren<Rigidbody2D>(true);
        foreach (Rigidbody2D body in bodies)
        {
            if (body != null)
                body.linearVelocity = Vector2.zero;
        }
    }

    private void CompleteCombat()
    {
        if (enemyWakeUpRoutine != null)
        {
            StopCoroutine(enemyWakeUpRoutine);
            enemyWakeUpRoutine = null;
        }

        SetDoorsClosed(false);

        if (ShouldSpawnReward())
        {
            State = RoomState.RewardAvailable;
            SpawnRewardChoice();
            return;
        }

        State = RoomState.Completed;
    }

    public void NotifyRewardClaimed()
    {
        if (rewardClaimed)
            return;

        rewardClaimed = true;
        activeRewardChoice = null;
        State = RoomState.RewardClaimed;
    }

    private void SetDoorsClosed(bool closed)
    {
        foreach (DoorController door in doors)
        {
            if (door != null)
                door.SetClosed(closed);
        }
    }

    private bool ShouldSpawnReward()
    {
        return hadEnemiesInCombat && !rewardSpawned && !rewardClaimed && promisedReward != null;
    }

    private void SpawnRewardChoice()
    {
        if (rewardSpawned)
            return;

        rewardSpawned = true;
        Transform spawnPoint = rewardSpawnPoint != null ? rewardSpawnPoint : transform;
        activeRewardChoice = rewardChoicePrefab != null
            ? Instantiate(rewardChoicePrefab, spawnPoint.position, Quaternion.identity, transform)
            : CreateFallbackRewardChoice(spawnPoint.position);

        activeRewardChoice.Initialize(this, promisedReward, null);
    }

    private RewardChoiceController CreateFallbackRewardChoice(Vector3 position)
    {
        GameObject choiceObject = new("RewardChoice");
        choiceObject.transform.SetParent(transform, false);
        choiceObject.transform.position = position;
        return choiceObject.AddComponent<RewardChoiceController>();
    }
}

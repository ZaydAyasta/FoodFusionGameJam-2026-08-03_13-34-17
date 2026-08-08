using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class RoomController : MonoBehaviour
{
    [SerializeField] private DoorController[] doors;
    [SerializeField] private EnemyDeathNotifier[] enemies;
    [SerializeField] private bool activateEnemiesOnEntry = true;
    [Header("Rewards")]
    [SerializeField] private Transform rewardSpawnPoint;
    [SerializeField] private RewardChoiceController rewardChoicePrefab;
    [SerializeField] private IngredientData[] possibleRewards;

    private readonly HashSet<EnemyDeathNotifier> aliveEnemies = new();
    private bool entered;
    private bool rewardSpawned;
    private bool rewardClaimed;
    private bool hadEnemiesInCombat;
    private RewardChoiceController activeRewardChoice;

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

            ChaserEnemy chaser = enemy.GetComponent<ChaserEnemy>();
            if (chaser != null)
                chaser.SetTarget(player);

            RangedEnemy ranged = enemy.GetComponent<RangedEnemy>();
            if (ranged != null)
                ranged.SetTarget(player);

            RiceEnemy rice = enemy.GetComponent<RiceEnemy>();
            if (rice != null)
                rice.SetTarget(player);
        }

        hadEnemiesInCombat = aliveEnemies.Count > 0;

        if (aliveEnemies.Count == 0)
            CompleteCombat();
    }

    public void ConfigureProcedural(
        DoorController[] roomDoors,
        EnemyDeathNotifier[] roomEnemies,
        Transform roomRewardSpawnPoint,
        IngredientData[] rewardPool)
    {
        doors = roomDoors ?? System.Array.Empty<DoorController>();
        enemies = roomEnemies ?? System.Array.Empty<EnemyDeathNotifier>();
        rewardSpawnPoint = roomRewardSpawnPoint;
        possibleRewards = rewardPool ?? System.Array.Empty<IngredientData>();
        entered = false;
        rewardSpawned = false;
        rewardClaimed = false;
        hadEnemiesInCombat = false;
        activeRewardChoice = null;
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

    private void CompleteCombat()
    {
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
        return hadEnemiesInCombat && !rewardSpawned && !rewardClaimed && possibleRewards != null && possibleRewards.Length >= 2;
    }

    private void SpawnRewardChoice()
    {
        if (rewardSpawned)
            return;

        rewardSpawned = true;
        Transform spawnPoint = rewardSpawnPoint != null ? rewardSpawnPoint : transform;
        IngredientData first = possibleRewards[0];
        IngredientData second = possibleRewards[1];

        if (possibleRewards.Length > 2)
            PickTwoRewards(out first, out second);

        activeRewardChoice = rewardChoicePrefab != null
            ? Instantiate(rewardChoicePrefab, spawnPoint.position, Quaternion.identity, transform)
            : CreateFallbackRewardChoice(spawnPoint.position);

        activeRewardChoice.Initialize(this, first, second);
    }

    private void PickTwoRewards(out IngredientData first, out IngredientData second)
    {
        int firstIndex = Random.Range(0, possibleRewards.Length);
        int secondIndex = Random.Range(0, possibleRewards.Length - 1);
        if (secondIndex >= firstIndex)
            secondIndex++;

        first = possibleRewards[firstIndex];
        second = possibleRewards[secondIndex];
    }

    private RewardChoiceController CreateFallbackRewardChoice(Vector3 position)
    {
        GameObject choiceObject = new("RewardChoice");
        choiceObject.transform.SetParent(transform, false);
        choiceObject.transform.position = position;
        return choiceObject.AddComponent<RewardChoiceController>();
    }
}

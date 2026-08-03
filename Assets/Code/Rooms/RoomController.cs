using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class RoomController : MonoBehaviour
{
    [SerializeField] private DoorController[] doors;
    [SerializeField] private EnemyDeathNotifier[] enemies;
    [SerializeField] private bool activateEnemiesOnEntry = true;

    private readonly HashSet<EnemyDeathNotifier> aliveEnemies = new();
    private bool entered;

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

        FactionMember faction = other.GetComponentInParent<FactionMember>();
        if (faction == null || faction.Faction != CombatFaction.Player)
            return;

        BeginCombat(other.transform.root);
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
            enemy.Died += HandleEnemyDied;
            aliveEnemies.Add(enemy);

            ChaserEnemy chaser = enemy.GetComponent<ChaserEnemy>();
            if (chaser != null)
                chaser.SetTarget(player);
        }

        if (aliveEnemies.Count == 0)
            CompleteCombat();
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
        State = RoomState.Completed;
        SetDoorsClosed(false);
    }

    private void SetDoorsClosed(bool closed)
    {
        foreach (DoorController door in doors)
        {
            if (door != null)
                door.SetClosed(closed);
        }
    }
}

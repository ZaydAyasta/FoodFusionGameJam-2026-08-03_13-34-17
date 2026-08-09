using System;
using System.Collections.Generic;
using UnityEngine;

public class ProceduralRoomLayout : MonoBehaviour
{
    [Header("Door anchors")]
    [SerializeField] private Transform upDoor;
    [SerializeField] private Transform rightDoor;
    [SerializeField] private Transform downDoor;
    [SerializeField] private Transform leftDoor;

    [Header("Camera")]
    [SerializeField] private PolygonCollider2D cameraLimit;
    [SerializeField] private float cameraOrthographicSize = 5.5f;

    [Header("Spawns")]
    [SerializeField] private Transform[] enemySpawnPoints;
    [SerializeField] private Transform rewardSpawnPoint;
    [SerializeField] private Transform kitchenSpawnPoint;
    [SerializeField] private Transform shopSpawnPoint;
    [SerializeField] private Transform[] decorationSpawnPoints;

    public PolygonCollider2D CameraLimit => cameraLimit;
    public float CameraOrthographicSize => cameraOrthographicSize;
    public Transform RewardSpawnPoint => rewardSpawnPoint;
    public Transform KitchenSpawnPoint => kitchenSpawnPoint;
    public Transform ShopSpawnPoint => shopSpawnPoint;
    public Transform[] EnemySpawnPoints => enemySpawnPoints;
    public Transform[] DecorationSpawnPoints => decorationSpawnPoints;

    private void Reset()
    {
        AutoWire();
    }

    private void Awake()
    {
        AutoWireMissingReferences();
    }

    public Transform GetDoor(RoomDirection direction)
    {
        return direction switch
        {
            RoomDirection.Up => upDoor,
            RoomDirection.Right => rightDoor,
            RoomDirection.Down => downDoor,
            RoomDirection.Left => leftDoor,
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
        };
    }

    public void ShowOnlyDoors(IEnumerable<RoomDirection> visibleDirections)
    {
        HashSet<RoomDirection> visible = new(visibleDirections);
        SetDoorVisible(RoomDirection.Up, visible.Contains(RoomDirection.Up));
        SetDoorVisible(RoomDirection.Right, visible.Contains(RoomDirection.Right));
        SetDoorVisible(RoomDirection.Down, visible.Contains(RoomDirection.Down));
        SetDoorVisible(RoomDirection.Left, visible.Contains(RoomDirection.Left));
    }

    public void HideAllDoors()
    {
        SetDoorVisible(RoomDirection.Up, false);
        SetDoorVisible(RoomDirection.Right, false);
        SetDoorVisible(RoomDirection.Down, false);
        SetDoorVisible(RoomDirection.Left, false);
    }

    public void SetKitchenVisible(bool visible)
    {
        if (kitchenSpawnPoint == null)
            return;

        Renderer[] renderers = kitchenSpawnPoint.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer kitchenRenderer in renderers)
            kitchenRenderer.enabled = visible;
    }

    public void AutoWire()
    {
        upDoor = FindDoor("Arr", "Up");
        rightDoor = FindDoor("Der", "Right");
        downDoor = FindDoor("Abj", "Down");
        leftDoor = FindDoor("Izq", "Left");
        cameraLimit = FindCameraLimit();
        enemySpawnPoints = FindSpawnChildren("Enemy_Spawns");
        rewardSpawnPoint = FindByNameContains("Reward_Spawn");
        kitchenSpawnPoint = FindKitchenSpawnPoint();
        shopSpawnPoint = FindByNameContains("ShopSpawn");
        decorationSpawnPoints = FindSpawnChildren("Decorations_Spawn");
    }

    private void AutoWireMissingReferences()
    {
        if (upDoor == null)
            upDoor = FindDoor("Arr", "Up");
        if (rightDoor == null)
            rightDoor = FindDoor("Der", "Right");
        if (downDoor == null)
            downDoor = FindDoor("Abj", "Down");
        if (leftDoor == null)
            leftDoor = FindDoor("Izq", "Left");
        if (cameraLimit == null)
            cameraLimit = FindCameraLimit();
        if (kitchenSpawnPoint == null)
            kitchenSpawnPoint = FindKitchenSpawnPoint();
        if (shopSpawnPoint == null)
            shopSpawnPoint = FindByNameContains("ShopSpawn");
    }

    private Transform FindDoor(params string[] directionTokens)
    {
        Transform[] children = GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
        {
            string childName = child.name;
            if (childName.Contains("ProceduralDoorSprite", StringComparison.OrdinalIgnoreCase)
                || childName.StartsWith("door_", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!childName.Contains("Puerta", StringComparison.OrdinalIgnoreCase)
                && !childName.Contains("Door", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            foreach (string token in directionTokens)
            {
                if (childName.Contains(token, StringComparison.OrdinalIgnoreCase))
                    return child;
            }
        }

        return null;
    }

    private PolygonCollider2D FindCameraLimit()
    {
        PolygonCollider2D[] colliders = GetComponentsInChildren<PolygonCollider2D>(true);
        foreach (PolygonCollider2D candidate in colliders)
        {
            if (candidate != null && candidate.name.Contains("CameraLimit", StringComparison.OrdinalIgnoreCase))
                return candidate;
        }

        return colliders.Length > 0 ? colliders[0] : null;
    }

    private Transform FindByNameContains(string token)
    {
        Transform[] children = GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
        {
            if (child != transform && child.name.Contains(token, StringComparison.OrdinalIgnoreCase))
                return child;
        }

        return null;
    }

    private Transform FindKitchenSpawnPoint()
    {
        Transform kitchenSpawn = FindByNameContains("Kitchen_Spawn");
        if (kitchenSpawn != null)
            return kitchenSpawn;

        Transform[] children = GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
        {
            if (child == transform)
                continue;

            if (IsKitchenMarkerName(child.name))
                return child;
        }

        return null;
    }

    private static bool IsKitchenMarkerName(string objectName)
    {
        return objectName.Equals("co_up", StringComparison.OrdinalIgnoreCase)
            || objectName.Equals("co_down", StringComparison.OrdinalIgnoreCase)
            || objectName.Equals("co_left", StringComparison.OrdinalIgnoreCase)
            || objectName.Equals("co_right", StringComparison.OrdinalIgnoreCase);
    }

    private Transform[] FindSpawnChildren(string parentNameToken)
    {
        Transform parent = FindByNameContains(parentNameToken);
        if (parent == null)
            return Array.Empty<Transform>();

        Transform[] spawnPoints = new Transform[parent.childCount];
        for (int i = 0; i < parent.childCount; i++)
            spawnPoints[i] = parent.GetChild(i);

        return spawnPoints;
    }

    private void SetDoorVisible(RoomDirection direction, bool visible)
    {
        Transform door = GetDoor(direction);
        if (door != null)
        {
            Renderer[] renderers = door.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer doorRenderer in renderers)
                doorRenderer.enabled = visible;
        }

        SetProceduralDoorVisualVisible(direction, visible);
    }

    private void SetProceduralDoorVisualVisible(RoomDirection direction, bool visible)
    {
        string proceduralName = $"ProceduralDoorSprite_{direction}";
        string prototypeName = direction switch
        {
            RoomDirection.Up => "door_0",
            RoomDirection.Right => "door_0 (1)",
            RoomDirection.Down => "door_0 (2)",
            RoomDirection.Left => "door_0 (3)",
            _ => string.Empty
        };

        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
        bool hasProceduralVisual = false;
        foreach (SpriteRenderer spriteRenderer in renderers)
        {
            if (spriteRenderer.name.Equals(proceduralName, StringComparison.OrdinalIgnoreCase))
            {
                hasProceduralVisual = true;
                break;
            }
        }

        foreach (SpriteRenderer spriteRenderer in renderers)
        {
            if (spriteRenderer.name.Equals(proceduralName, StringComparison.OrdinalIgnoreCase))
            {
                spriteRenderer.enabled = visible;
            }
            else if (spriteRenderer.name.Equals(prototypeName, StringComparison.OrdinalIgnoreCase))
            {
                spriteRenderer.enabled = visible && !hasProceduralVisual;
            }
        }
    }
}

using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class RoomGenerationTestBootstrap : MonoBehaviour
{
    [Header("Scene templates")]
    [SerializeField] private ProceduralRoomLayout currentRoom;
    [SerializeField] private ProceduralRoomLayout[] roomTemplates;

    [Header("Scene names")]
    [SerializeField] private string currentRoomName = "Pro_RoomDefault";
    [SerializeField] private string[] templateRoomNames =
    {
        "Pro_Room_Tpye1",
        "Pro_Room_Tpye2",
        "Pro_Room_Tpye3",
        "Pro_Room_Tpye4",
        "Pro_Room_Tpye5",
        "Pro_Room_Tpye6",
        "Pro_Room_Tpye7",
        "Pro_Room_Tpye8",
        "Pro_Room_Tpye9",
        "Pro_Room_Tpye10"
    };

    [Header("Generation")]
    [SerializeField] private int minCandidateCount = 1;
    [SerializeField] private int maxCandidateCount = 3;
    [SerializeField] private int oneDoorWeight = 1;
    [SerializeField] private int twoDoorWeight = 6;
    [SerializeField] private int threeDoorWeight = 3;
    [SerializeField] private float connectionGap;
    [SerializeField] private float commitOffsetFromDoor = 0.75f;
    [SerializeField] private Vector2 commitTriggerSize = new(1.25f, 1.25f);
    [SerializeField] private float playerEntryPushDistance = 2.2f;
    [SerializeField] private float previousRoomDestroyDelay = 0.5f;
    [SerializeField] private float noRoomControllerNextBatchDelay = 1f;
    [SerializeField] private bool hideUnusedTemplates = true;
    [SerializeField] private bool addFallbackPlayerCollider = true;
    [SerializeField] private int firstKitchenRoomNumber = 4;
    [SerializeField] private int kitchenIntervalRooms = 4;

    [Header("Combat")]
    [SerializeField] private bool generateCombatInFirstRoom;
    [SerializeField] private float playerMaxHealth = 100f;
    [SerializeField] private float rangedEnemyShotDamage = 8f;
    [SerializeField] private Vector2 fallbackRiceEnemyHitboxSize = new(1.15f, 1.15f);
    [SerializeField] private EnemyDeathNotifier riceEnemyPrefab;
    [SerializeField] private float riceEnemyHitsToKill = 4f;
    [SerializeField] private Sprite riceEnemySprite;
    [SerializeField] private Vector3 riceEnemyScale = new(0.8f, 0.8f, 0.8f);
    [SerializeField] private Vector2 horizontalDoorBlockerSize = new(2f, 0.55f);
    [SerializeField] private Vector2 verticalDoorBlockerSize = new(0.55f, 2f);
    [SerializeField] private IngredientData[] rewardPool;

    [Header("Door Reward Preview")]
    [SerializeField, Range(0.05f, 1f)] private float rewardPreviewAlpha = 0.55f;
    [SerializeField] private Vector3 rewardPreviewOffset = new(0f, 0.9f, -0.25f);
    [SerializeField] private Vector3 rewardPreviewEulerAngles = new(-40f, 0f, 0f);
    [SerializeField, Min(0.05f)] private float rewardPreviewWorldSize = 0.75f;

    [Header("Hazards")]
    [SerializeField] private float trapDamage = 10f;
    [SerializeField] private float trapDamageInterval = 0.5f;

    [Header("Collision Perspective")]
    [Range(-2f, 2f)]
    [SerializeField] private float collisionVisualZOffset = -0.5f;

    [Header("Door Visuals")]
    [SerializeField] private Sprite doorSprite;
    [SerializeField] private Vector3 upDoorVisualOffset = new(0.08f, -0.92f, -1.76f);
    [SerializeField] private Vector3 rightDoorVisualOffset = new(-1.53f, 0.1f, -1.76f);
    [SerializeField] private Vector3 downDoorVisualOffset = new(0.23f, 0.33f, -1.5f);
    [SerializeField] private Vector3 leftDoorVisualOffset = new(0.5f, -0.06f, -1.21f);
    [SerializeField] private Vector3 doorVisualWorldScale = new(0.73f, 0.72f, 0.73f);

    [Header("Kitchen Visuals")]
    [SerializeField] private Sprite kitchenSprite;
    [SerializeField] private Vector3 kitchenVisualWorldScale = new(0.1f, 0.1f, 0.1f);
    [SerializeField] private Vector3 kitchenVisualOffset = new(0f, 0f, -0.25f);

    [Header("Shop Visuals")]
    [SerializeField] private ShopVisual shopPrefab;
    [SerializeField, Range(0f, 1f)] private float kitchenRoomShopChance = 0.65f;
    [SerializeField] private Vector3 shopVisualOffset = new(0f, 0f, -0.25f);
    [SerializeField] private Vector3 shopVisualEulerAngles = new(-45f, 0f, 0f);

    [Header("Camera")]
    [SerializeField] private CinemachineCamera cinemachineCamera;
    [SerializeField] private CinemachineConfiner2D confiner;
    [SerializeField] private Transform cameraTarget;
    [SerializeField] private Vector3 cameraFollowOffset = new(0f, 0f, -10f);
    [SerializeField] private bool autoFitOrthographicSizeToCameraLimit = true;
    [SerializeField] private float cameraLimitPadding = 0.35f;
    [SerializeField] private float minimumOrthographicSize = 3.5f;
    [SerializeField] private float confinerDamping = 0f;
    [SerializeField] private float confinerSlowingDistance = 0f;
    [SerializeField] private string[] rectangularConfinerRoomNameTokens = { "Pro_Room_Tpye3", "Pro_Room_Type3" };

    private readonly List<ProceduralRoomCandidate> candidates = new();
    private readonly List<ProceduralRoomLayout> hiddenTemplates = new();
    private readonly Dictionary<ProceduralRoomLayout, Collider2D> rectangularCameraLimits = new();
    private readonly Dictionary<RoomDirection, Quaternion> doorVisualRotations = new();
    private readonly string[] placeholderThemes = { "Cheese", "Bread", "Fish", "Butter", "Corn", "Rice" };

    private Sprite fallbackSprite;
    private Material doorVisualMaterial;
    private string doorVisualSortingLayer = "Default";
    private int doorVisualSortingOrder;
    private Material kitchenVisualMaterial;
    private string kitchenVisualSortingLayer = "Default";
    private int kitchenVisualSortingOrder;
    private RoomDirection? blockedExitDirection;
    private RoomController activeRoomController;
    private float nextBatchAt = -1f;
    private bool waitingForRoomCompletion;
    private int currentRoomNumber = 1;
    private int lastGeneratedExitCount;

    private void Start()
    {
        ResolveSceneReferences();
        ResolveDoorVisualReferences();
        ResolveKitchenVisualReferences();
        ResolveShopReferences();

        if (currentRoom == null || roomTemplates == null || roomTemplates.Length == 0)
        {
            Debug.LogError("RoomGenerationTestBootstrap needs a current room and at least one room template.");
            enabled = false;
            return;
        }

        currentRoom.AutoWire();
        EnsureDoorVisuals(currentRoom);
        foreach (ProceduralRoomLayout template in roomTemplates)
        {
            template.AutoWire();
            EnsureDoorVisuals(template);
            SetKitchenVisible(template, false);
            SetShopVisible(template, false, false, false);
        }

        EnsurePlayerColliderForTestScene();
        EnsurePlayerCombatSetup();
        EnsureCameraFollowsPlayer();
        ClearGeneratedCandidates();

        if (hideUnusedTemplates)
            HideTemplateRooms();

        if (generateCombatInFirstRoom)
            PrepareProceduralRoomCombat(currentRoom);

        SetCurrentRoom(currentRoom);
        if (generateCombatInFirstRoom)
        {
            waitingForRoomCompletion = true;
            nextBatchAt = Time.time + noRoomControllerNextBatchDelay;
        }
        else
        {
            GenerateCandidateBatch();
        }
    }

    private void Update()
    {
        if (!waitingForRoomCompletion)
            return;

        if (activeRoomController != null)
        {
            RoomState state = activeRoomController.State;
            if (state != RoomState.Completed && state != RoomState.RewardClaimed)
                return;
        }
        else if (Time.time < nextBatchAt)
        {
            return;
        }

        waitingForRoomCompletion = false;
        GenerateCandidateBatch();
    }

    private void ResolveSceneReferences()
    {
        if (currentRoom == null)
            currentRoom = FindRoomByName(currentRoomName);

        if (roomTemplates == null || roomTemplates.Length == 0)
        {
            List<ProceduralRoomLayout> templates = new();
            foreach (string templateRoomName in templateRoomNames)
            {
                ProceduralRoomLayout template = FindRoomByName(templateRoomName);
                if (template != null)
                    templates.Add(template);
            }

            roomTemplates = templates.ToArray();
        }

        if (cinemachineCamera == null)
            cinemachineCamera = FindFirstObjectByType<CinemachineCamera>();

        if (confiner == null)
            confiner = FindFirstObjectByType<CinemachineConfiner2D>();

        if (cameraTarget == null)
        {
            CharacterInput player = FindFirstObjectByType<CharacterInput>();
            if (player != null)
                cameraTarget = player.transform;
        }
    }

    private ProceduralRoomLayout FindRoomByName(string roomName)
    {
        GameObject roomObject = GameObject.Find(roomName);
        if (roomObject == null)
            return null;

        ProceduralRoomLayout layout = roomObject.GetComponent<ProceduralRoomLayout>();
        if (layout == null)
            layout = roomObject.AddComponent<ProceduralRoomLayout>();

        return layout;
    }

    private void ResolveDoorVisualReferences()
    {
        doorVisualRotations.Clear();
        doorVisualRotations[RoomDirection.Up] = Quaternion.Euler(270f, 0f, 0f);
        doorVisualRotations[RoomDirection.Right] = Quaternion.Euler(359.57f, 91.99f, 269.99f);
        doorVisualRotations[RoomDirection.Down] = Quaternion.Euler(89.22f, 90.9f, 268.91f);
        doorVisualRotations[RoomDirection.Left] = Quaternion.Euler(359.11f, 271.99f, 90.01f);

        if (doorSprite == null)
            doorSprite = ResolveDoorSpriteFromScene();

#if UNITY_EDITOR
        if (doorSprite == null)
        {
            Object[] doorAssets = AssetDatabase.LoadAllAssetsAtPath("Assets/Images/Backgrounds/door.png");
            foreach (Object asset in doorAssets)
            {
                if (asset is Sprite sprite)
                {
                    doorSprite = sprite;
                    break;
                }
            }
        }
#endif

        if (currentRoom == null)
            return;

        CaptureDoorVisualPrototype(RoomDirection.Up, "door_0", ref upDoorVisualOffset);
        CaptureDoorVisualPrototype(RoomDirection.Right, "door_0 (1)", ref rightDoorVisualOffset);
        CaptureDoorVisualPrototype(RoomDirection.Down, "door_0 (2)", ref downDoorVisualOffset);
        CaptureDoorVisualPrototype(RoomDirection.Left, "door_0 (3)", ref leftDoorVisualOffset);
        ConvertDoorRotationsToAnchorOffsets();
    }

    private void ConvertDoorRotationsToAnchorOffsets()
    {
        RoomDirection[] directions =
        {
            RoomDirection.Up,
            RoomDirection.Right,
            RoomDirection.Down,
            RoomDirection.Left
        };

        foreach (RoomDirection direction in directions)
        {
            Transform anchor = currentRoom.GetDoor(direction);
            if (anchor == null || !doorVisualRotations.TryGetValue(direction, out Quaternion roomRotation))
                continue;

            Quaternion prototypeWorldRotation = currentRoom.transform.rotation * roomRotation;
            doorVisualRotations[direction] = Quaternion.Inverse(anchor.rotation) * prototypeWorldRotation;
        }
    }

    private Sprite ResolveDoorSpriteFromScene()
    {
        SpriteRenderer prototype = FindDoorVisualPrototype("door_0");
        return prototype != null ? prototype.sprite : null;
    }

    private void CaptureDoorVisualPrototype(RoomDirection direction, string prototypeName, ref Vector3 offset)
    {
        SpriteRenderer prototype = FindDoorVisualPrototype(prototypeName);
        Transform doorAnchor = currentRoom.GetDoor(direction);
        if (prototype == null || doorAnchor == null)
            return;

        if (prototype.sprite != null)
            doorSprite = prototype.sprite;

        doorVisualMaterial = prototype.sharedMaterial;
        doorVisualSortingLayer = prototype.sortingLayerName;
        doorVisualSortingOrder = prototype.sortingOrder;
        offset = currentRoom.transform.InverseTransformVector(prototype.transform.position - doorAnchor.position);
        doorVisualRotations[direction] = Quaternion.Inverse(currentRoom.transform.rotation) * prototype.transform.rotation;
        doorVisualWorldScale = prototype.transform.lossyScale;
    }

    private SpriteRenderer FindDoorVisualPrototype(string objectName)
    {
        if (currentRoom == null)
            return null;

        SpriteRenderer[] renderers =
            currentRoom.GetComponentsInChildren<SpriteRenderer>(true);

        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer != null &&
                renderer.name.Equals(
                    objectName,
                    System.StringComparison.OrdinalIgnoreCase))
            {
                return renderer;
            }
        }

        return null;
    }

    private void ResolveKitchenVisualReferences()
    {
#if UNITY_EDITOR
        if (kitchenSprite == null)
        {
            UnityEngine.Object[] kitchenAssets = AssetDatabase.LoadAllAssetsAtPath("Assets/Images/Backgrounds/COCINA.png");
            foreach (UnityEngine.Object asset in kitchenAssets)
            {
                if (asset is Sprite sprite)
                {
                    kitchenSprite = sprite;
                    break;
                }
            }
        }
#endif

        if (currentRoom == null)
            return;

        Renderer marker = FindKitchenMarkerRenderer(currentRoom);
        if (marker is SpriteRenderer spriteRenderer)
        {
            kitchenVisualMaterial = spriteRenderer.sharedMaterial;
            kitchenVisualSortingLayer = spriteRenderer.sortingLayerName;
            kitchenVisualSortingOrder = spriteRenderer.sortingOrder;
        }
    }

    private void ResolveShopReferences()
    {
#if UNITY_EDITOR
        if (shopPrefab == null)
            shopPrefab = AssetDatabase.LoadAssetAtPath<ShopVisual>("Assets/Prefabs/Shops/Shop.prefab");
#endif
    }

    private void GenerateCandidateBatch()
    {
        ClearGeneratedCandidates();

        ProceduralRoomLayout selectedTemplate = roomTemplates[Random.Range(0, roomTemplates.Length)];
        selectedTemplate.AutoWire();
        EnsureDoorVisuals(selectedTemplate);

        List<RoomDirection> directions = GetAllowedExitDirections();
        Shuffle(directions);

        int count = SelectCandidateCount(directions.Count);
        List<IngredientData> rewards = PickDistinctRewards(count);
        List<RoomDirection> activeExitDirections = new();
        for (int i = 0; i < count; i++)
        {
            RoomDirection exitDirection = directions[i];
            IngredientData reward = i < rewards.Count ? rewards[i] : null;
            string theme = reward != null ? reward.DisplayName : placeholderThemes[i % placeholderThemes.Length];
            if (CreateCandidate(selectedTemplate, exitDirection, theme, reward, $"Candidate_{exitDirection}_{selectedTemplate.name}") != null)
                activeExitDirections.Add(exitDirection);
        }

        EnsureDoorVisuals(currentRoom);
        currentRoom.ShowOnlyDoors(activeExitDirections);
        SetKitchenVisible(currentRoom, IsKitchenRoom(currentRoomNumber));
        SetShopVisible(currentRoom, IsKitchenRoom(currentRoomNumber), false, true);
        lastGeneratedExitCount = activeExitDirections.Count;
    }

    private int SelectCandidateCount(int availableDirectionCount)
    {
        int minCount = Mathf.Clamp(minCandidateCount, 1, availableDirectionCount);
        int maxCount = Mathf.Clamp(maxCandidateCount, minCount, availableDirectionCount);
        int totalWeight = 0;

        for (int count = minCount; count <= maxCount; count++)
            totalWeight += GetCandidateCountWeight(count);

        if (totalWeight <= 0)
            return Mathf.Clamp(2, minCount, maxCount);

        int roll = Random.Range(0, totalWeight);
        for (int count = minCount; count <= maxCount; count++)
        {
            roll -= GetCandidateCountWeight(count);
            if (roll < 0)
                return count;
        }

        return maxCount;
    }

    private int GetCandidateCountWeight(int count)
    {
        return count switch
        {
            1 => lastGeneratedExitCount == 1 ? 0 : Mathf.Max(0, oneDoorWeight),
            2 => Mathf.Max(0, twoDoorWeight),
            3 => Mathf.Max(0, threeDoorWeight),
            _ => 0
        };
    }

    private List<IngredientData> PickDistinctRewards(int count)
    {
        IngredientData[] pool = GetRewardPool();
        List<IngredientData> available = new();
        foreach (IngredientData ingredient in pool)
        {
            if (ingredient != null && ingredient.Icon != null && !available.Contains(ingredient))
                available.Add(ingredient);
        }

        Shuffle(available);
        if (available.Count > count)
            available.RemoveRange(count, available.Count - count);

        return available;
    }

    private void CreateRewardPreview(Transform door, RoomDirection direction, IngredientData reward)
    {
        if (door == null || reward == null || reward.Icon == null || currentRoom == null)
            return;

        GameObject previewObject = new($"DoorRewardPreview_{direction}_{reward.Id}");
        previewObject.transform.SetParent(currentRoom.transform, true);
        previewObject.transform.position = door.position + rewardPreviewOffset;
        previewObject.transform.rotation = Quaternion.Euler(rewardPreviewEulerAngles);

        SpriteRenderer preview = previewObject.AddComponent<SpriteRenderer>();
        preview.sprite = reward.Icon;
        preview.color = new Color(1f, 1f, 1f, rewardPreviewAlpha);
        preview.sortingLayerName = doorVisualSortingLayer;
        preview.sortingOrder = doorVisualSortingOrder + 1;
        NormalizeRewardPreviewSize(preview);
    }

    private void NormalizeRewardPreviewSize(SpriteRenderer preview)
    {
        if (preview == null || preview.sprite == null)
            return;

        Vector2 spriteSize = preview.sprite.bounds.size;
        float largestSide = Mathf.Max(spriteSize.x, spriteSize.y);
        if (largestSide <= Mathf.Epsilon)
            return;

        float uniformScale = rewardPreviewWorldSize / largestSide;
        SetWorldScale(preview.transform, new Vector3(uniformScale, uniformScale, uniformScale));
    }

    private ProceduralRoomLayout CreateCandidate(
        ProceduralRoomLayout candidateTemplate,
        RoomDirection exitDirection,
        string theme,
        IngredientData promisedReward,
        string instanceName)
    {
        Transform sourceDoor = currentRoom.GetDoor(exitDirection);
        RoomDirection entryDirection = GetOpposite(exitDirection);
        Transform candidateEntryDoor = candidateTemplate.GetDoor(entryDirection);

        if (sourceDoor == null || candidateEntryDoor == null)
        {
            Debug.LogError($"Cannot align {instanceName}. Missing source {exitDirection} or candidate {entryDirection} door.");
            return null;
        }

        ProceduralRoomLayout candidate = Instantiate(candidateTemplate, candidateTemplate.transform.position, candidateTemplate.transform.rotation);
        candidate.name = instanceName;
        candidate.gameObject.SetActive(true);
        candidate.AutoWire();
        EnsureDoorVisuals(candidate);
        // Candidate rooms stay visually open until the player commits to them,
        // just like their front wall. The anchors remain active for alignment.
        candidate.HideAllDoors();
        SetKitchenVisible(candidate, IsKitchenRoom(currentRoomNumber + 1));
        SetShopVisible(candidate, IsKitchenRoom(currentRoomNumber + 1), true, false);
        SetFrontWallVisible(candidate, false);

        Vector3 offset = sourceDoor.position - candidate.GetDoor(entryDirection).position;
        candidate.transform.position += offset + GetGapOffset(exitDirection);

        ProceduralRoomCommitTrigger trigger = CreateCommitTrigger(sourceDoor, exitDirection);
        ProceduralRoomCandidate metadata = candidate.gameObject.AddComponent<ProceduralRoomCandidate>();
        metadata.Initialize(theme, exitDirection, entryDirection, candidate, trigger, promisedReward);
        trigger.Initialize(this, metadata);
        candidates.Add(metadata);
        CreateRewardPreview(sourceDoor, exitDirection, promisedReward);

        return candidate;
    }

    public void CommitCandidate(ProceduralRoomCandidate selectedCandidate, Transform player)
    {
        if (selectedCandidate == null || !candidates.Contains(selectedCandidate))
            return;

        ProceduralRoomLayout previousRoom = currentRoom;
        currentRoom = selectedCandidate.Layout;
        SetFrontWallVisible(previousRoom, false);
        blockedExitDirection = selectedCandidate.EntryDirection;
        currentRoomNumber++;

        DestroyUnselectedCandidates(selectedCandidate);
        DestroyCommitTriggers();
        EnsureDoorVisuals(currentRoom);
        currentRoom.ShowOnlyDoors(new[] { selectedCandidate.EntryDirection });
        MovePlayerInsideCommittedRoom(selectedCandidate, player);
        PrepareProceduralRoomCombat(currentRoom, selectedCandidate.PromisedReward);
        SetCurrentRoom(currentRoom);

        if (previousRoom != null && previousRoom != currentRoom)
            Destroy(previousRoom.gameObject, previousRoomDestroyDelay);

        Destroy(selectedCandidate);
        candidates.Clear();

        activeRoomController = currentRoom.GetComponent<RoomController>();
        waitingForRoomCompletion = true;
        nextBatchAt = Time.time + noRoomControllerNextBatchDelay;
    }

    private void MovePlayerInsideCommittedRoom(ProceduralRoomCandidate selectedCandidate, Transform player)
    {
        if (selectedCandidate == null || player == null || currentRoom == null)
            return;

        Transform entryDoor = currentRoom.GetDoor(selectedCandidate.EntryDirection);
        if (entryDoor == null)
            return;

        Vector3 inwardDirection = GetDirectionVector(selectedCandidate.ExitDirectionFromPreviousRoom);
        Vector3 targetPosition = entryDoor.position + inwardDirection * playerEntryPushDistance;
        targetPosition.z = player.position.z;

        Rigidbody2D playerBody = player.GetComponent<Rigidbody2D>();
        if (playerBody != null)
        {
            playerBody.linearVelocity = Vector2.zero;
            playerBody.position = targetPosition;
        }

        CharacterInput characterInput = player.GetComponent<CharacterInput>();
        if (characterInput != null)
            characterInput.BeginRoomEntryInputGate();

        player.position = targetPosition;
        Physics2D.SyncTransforms();

        // El teleport ya termino; recien ahora se reactivan las colisiones
        // proyectadas del cuarto al que acaba de entrar el jugador.
        ConfigureCollisionBlocks(currentRoom);
    }

    private void SetCurrentRoom(ProceduralRoomLayout room)
    {
        if (room == null)
            return;

        SetFrontWallVisible(room, true);
        activeRoomController = room.GetComponent<RoomController>();
        EnsureDoorVisuals(room);
        EnsureCameraFollowsPlayer();
        SetKitchenVisible(room, IsKitchenRoom(currentRoomNumber));
        SetShopVisible(room, IsKitchenRoom(currentRoomNumber), false, true);
        ConfigureCollisionBlocks(room);

        Collider2D boundingShape = GetConfinerShapeForRoom(room);
        if (confiner != null && boundingShape != null)
        {
            ApplyOrthographicSize(room);
            confiner.BoundingShape2D = boundingShape;
            confiner.Damping = confinerDamping;
            confiner.SlowingDistance = confinerSlowingDistance;
            confiner.InvalidateBoundingShapeCache();
            confiner.InvalidateLensCache();
        }
    }

    private void PrepareProceduralRoomCombat(ProceduralRoomLayout room, IngredientData promisedReward = null)
    {
        if (room == null)
            return;

        room.AutoWire();
        Collider2D trigger = EnsureRoomTrigger(room);
        if (trigger == null)
            return;

        DoorController[] roomDoors = CreateDoorBlockers(room);
        EnemyDeathNotifier[] roomEnemies = CreateRoomEnemies(room, promisedReward);
        ConfigureTrapZones(room);
        RoomController controller = room.GetComponent<RoomController>();
        if (controller == null)
            controller = room.gameObject.AddComponent<RoomController>();

        controller.ConfigureProcedural(roomDoors, roomEnemies, room.RewardSpawnPoint, promisedReward);
        activeRoomController = controller;
    }

    private Collider2D EnsureRoomTrigger(ProceduralRoomLayout room)
    {
        Collider2D trigger = room.GetComponent<Collider2D>();
        if (trigger == null)
            trigger = room.gameObject.AddComponent<BoxCollider2D>();

        trigger.isTrigger = true;

        if (trigger is BoxCollider2D box && room.CameraLimit != null)
        {
            Bounds bounds = room.CameraLimit.bounds;
            Vector3 localCenter = room.transform.InverseTransformPoint(bounds.center);
            Vector3 scale = room.transform.lossyScale;
            float scaleX = Mathf.Approximately(scale.x, 0f) ? 1f : Mathf.Abs(scale.x);
            float scaleY = Mathf.Approximately(scale.y, 0f) ? 1f : Mathf.Abs(scale.y);
            box.offset = localCenter;
            box.size = new Vector2(bounds.size.x / scaleX, bounds.size.y / scaleY);
        }

        return trigger;
    }

    private DoorController[] CreateDoorBlockers(ProceduralRoomLayout room)
    {
        List<DoorController> roomDoors = new();
        RoomDirection[] directions =
        {
            RoomDirection.Up,
            RoomDirection.Right,
            RoomDirection.Down,
            RoomDirection.Left
        };

        foreach (RoomDirection direction in directions)
        {
            Transform doorAnchor = room.GetDoor(direction);
            if (doorAnchor == null)
                continue;

            Transform existing = doorAnchor.Find("ProceduralCombatDoorBlocker");
            GameObject blockerObject = existing != null
                ? existing.gameObject
                : new GameObject("ProceduralCombatDoorBlocker");

            blockerObject.transform.SetParent(doorAnchor, false);
            blockerObject.transform.localPosition = Vector3.zero;
            blockerObject.transform.localRotation = Quaternion.identity;
            blockerObject.transform.localScale = Vector3.one;

            BoxCollider2D blocker = blockerObject.GetComponent<BoxCollider2D>();
            if (blocker == null)
                blocker = blockerObject.AddComponent<BoxCollider2D>();

            blocker.isTrigger = false;
            blocker.size = direction == RoomDirection.Up || direction == RoomDirection.Down
                ? horizontalDoorBlockerSize
                : verticalDoorBlockerSize;

            DoorController doorController = blockerObject.GetComponent<DoorController>();
            if (doorController == null)
                doorController = blockerObject.AddComponent<DoorController>();

            roomDoors.Add(doorController);
        }

        return roomDoors.ToArray();
    }

    private void EnsureDoorVisuals(ProceduralRoomLayout room)
    {
        if (room == null || doorSprite == null)
            return;

        EnsureDoorVisual(room, RoomDirection.Up, "door_0", upDoorVisualOffset);
        EnsureDoorVisual(room, RoomDirection.Right, "door_0 (1)", rightDoorVisualOffset);
        EnsureDoorVisual(room, RoomDirection.Down, "door_0 (2)", downDoorVisualOffset);
        EnsureDoorVisual(room, RoomDirection.Left, "door_0 (3)", leftDoorVisualOffset);
    }

    private void SetKitchenVisible(ProceduralRoomLayout room, bool visible)
    {
        if (room == null)
            return;

        EnsureKitchenVisual(room, visible);
        room.SetKitchenVisible(visible);
    }

    private void SetShopVisible(ProceduralRoomLayout room, bool canSpawnShop, bool rollChance, bool revealShop)
    {
        if (room == null)
            return;

        HideShopSpawnMarker(room);

        ShopVisual existingShop = FindRoomShop(room);
        if (!canSpawnShop)
        {
            if (existingShop != null)
                existingShop.gameObject.SetActive(false);

            return;
        }

        if (existingShop != null)
        {
            existingShop.gameObject.SetActive(revealShop);
            PositionShop(room, existingShop.transform);
            return;
        }

        if (!rollChance)
            return;

        if (Random.value > kitchenRoomShopChance)
            return;

        if (shopPrefab == null || room.ShopSpawnPoint == null)
            return;

        ShopVisual shop = Instantiate(shopPrefab, room.transform);
        shop.name = "ProceduralShop";
        PositionShop(room, shop.transform);
        shop.gameObject.SetActive(revealShop);
    }

    private void PositionShop(ProceduralRoomLayout room, Transform shopTransform)
    {
        if (room == null || shopTransform == null || room.ShopSpawnPoint == null)
            return;

        shopTransform.position = room.ShopSpawnPoint.position + room.transform.TransformVector(shopVisualOffset);
        shopTransform.rotation = Quaternion.Euler(shopVisualEulerAngles);
    }

    private static ShopVisual FindRoomShop(ProceduralRoomLayout room)
    {
        if (room == null)
            return null;

        ShopVisual[] shops = room.GetComponentsInChildren<ShopVisual>(true);
        foreach (ShopVisual shop in shops)
        {
            if (shop != null && shop.name.Equals("ProceduralShop", System.StringComparison.OrdinalIgnoreCase))
                return shop;
        }

        return null;
    }

    private static void HideShopSpawnMarker(ProceduralRoomLayout room)
    {
        if (room == null || room.ShopSpawnPoint == null)
            return;

        Renderer[] renderers = room.ShopSpawnPoint.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            if (renderer != null)
                renderer.enabled = false;
        }
    }

    private void EnsureKitchenVisual(ProceduralRoomLayout room, bool visible)
    {
        Renderer marker = FindKitchenMarkerRenderer(room);
        RoomDirection? markerDirection = GetKitchenMarkerDirection(marker);
        SpriteRenderer visual = FindRoomDoorVisual(room, "ProceduralKitchenSprite");
        if (visual == null)
        {
            GameObject visualObject = new("ProceduralKitchenSprite");
            visual = visualObject.AddComponent<SpriteRenderer>();
        }

        visual.enabled = visible && kitchenSprite != null && marker != null;
        if (!visual.enabled)
            return;

        visual.sprite = kitchenSprite;
        if (kitchenVisualMaterial != null)
            visual.sharedMaterial = kitchenVisualMaterial;

        visual.sortingLayerName = kitchenVisualSortingLayer;
        visual.sortingOrder = kitchenVisualSortingOrder + 1;

        Transform visualTransform = visual.transform;
        visualTransform.SetParent(room.transform, true);
        visualTransform.position = marker.bounds.center + room.transform.TransformVector(kitchenVisualOffset);
        SetKitchenVisualRotation(visualTransform, markerDirection);
        SetWorldScale(visualTransform, kitchenVisualWorldScale);
        AlignRendererCenterToPosition(visual, marker.bounds.center + room.transform.TransformVector(kitchenVisualOffset));
        SetKitchenVisualRotation(visualTransform, markerDirection);
    }

    private Renderer FindKitchenMarkerRenderer(ProceduralRoomLayout room)
    {
        if (room == null)
            return null;

        Renderer namedMarker = FindNamedKitchenMarkerRenderer(room);
        if (namedMarker != null)
            return namedMarker;

        if (room.KitchenSpawnPoint == null)
            return null;

        Renderer[] renderers = room.KitchenSpawnPoint.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            if (renderer is SpriteRenderer spriteRenderer && spriteRenderer.color.r > 0.7f && spriteRenderer.color.g < 0.25f)
                return renderer;
        }

        foreach (Renderer renderer in renderers)
        {
            if (renderer != null)
                return renderer;
        }

        return null;
    }

    private Renderer FindNamedKitchenMarkerRenderer(ProceduralRoomLayout room)
    {
        Renderer[] renderers = room.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            if (renderer != null && GetKitchenMarkerDirection(renderer).HasValue)
                return renderer;
        }

        return null;
    }

    private static RoomDirection? GetKitchenMarkerDirection(Component marker)
    {
        if (marker == null)
            return null;

        string markerName = marker.name;
        if (markerName.Equals("co_up", System.StringComparison.OrdinalIgnoreCase))
            return RoomDirection.Up;
        if (markerName.Equals("co_right", System.StringComparison.OrdinalIgnoreCase))
            return RoomDirection.Right;
        if (markerName.Equals("co_down", System.StringComparison.OrdinalIgnoreCase))
            return RoomDirection.Down;
        if (markerName.Equals("co_left", System.StringComparison.OrdinalIgnoreCase))
            return RoomDirection.Left;

        return null;
    }

    private static void SetKitchenVisualRotation(Transform visualTransform, RoomDirection? direction)
    {
        Vector3 angles = visualTransform.eulerAngles;
        angles.x = 0f;
        angles.y = 0f;
        angles.z = direction switch
        {
            RoomDirection.Up => 0f,
            RoomDirection.Right => -90f,
            RoomDirection.Down => -180f,
            RoomDirection.Left => 90f,
            _ => 0f
        };

        visualTransform.eulerAngles = angles;
    }

    private static void AlignRendererCenterToPosition(Renderer renderer, Vector3 targetCenter)
    {
        if (renderer == null)
            return;

        renderer.transform.position += targetCenter - renderer.bounds.center;
    }

    private void EnsureDoorVisual(ProceduralRoomLayout room, RoomDirection direction, string prototypeName, Vector3 localOffset)
    {
        Transform doorAnchor = room.GetDoor(direction);
        if (doorAnchor == null)
            return;

        string proceduralName = $"ProceduralDoorSprite_{direction}";
        SpriteRenderer visual = FindRoomDoorVisual(room, proceduralName);
        if (visual == null)
            visual = FindRoomDoorVisual(room, prototypeName);

        if (visual == null)
        {
            GameObject visualObject = new(proceduralName);
            visual = visualObject.AddComponent<SpriteRenderer>();
        }

        Transform visualTransform = visual.transform;
        visual.name = proceduralName;

        visualTransform.SetParent(room.transform, true);
        visualTransform.position = doorAnchor.position + room.transform.TransformVector(localOffset);
        visualTransform.rotation = doorAnchor.rotation * GetDoorVisualRotation(direction);
        SetWorldScale(visualTransform, doorVisualWorldScale);

        visual.sprite = doorSprite;
        if (doorVisualMaterial != null)
            visual.sharedMaterial = doorVisualMaterial;

        visual.sortingLayerName = doorVisualSortingLayer;
        visual.sortingOrder = doorVisualSortingOrder;
        DisableDuplicateDoorVisuals(room, direction, visual, prototypeName);
    }

    private void DisableDuplicateDoorVisuals(ProceduralRoomLayout room, RoomDirection direction, SpriteRenderer activeVisual, string prototypeName)
    {
        string proceduralName = $"ProceduralDoorSprite_{direction}";
        SpriteRenderer[] renderers = room.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer == null || renderer == activeVisual)
                continue;

            bool isSameDoorVisual =
                renderer.name.Equals(proceduralName, System.StringComparison.OrdinalIgnoreCase)
                || renderer.name.Equals(prototypeName, System.StringComparison.OrdinalIgnoreCase);

            if (isSameDoorVisual)
                renderer.enabled = false;
        }
    }

    private SpriteRenderer FindRoomDoorVisual(ProceduralRoomLayout room, string objectName)
    {
        SpriteRenderer[] renderers = room.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer != null && renderer.name.Equals(objectName, System.StringComparison.OrdinalIgnoreCase))
                return renderer;
        }

        return null;
    }

    private Quaternion GetDoorVisualRotation(RoomDirection direction)
    {
        return doorVisualRotations.TryGetValue(direction, out Quaternion rotation)
            ? rotation
            : Quaternion.identity;
    }

    private static void SetWorldScale(Transform target, Vector3 worldScale)
    {
        Transform parent = target.parent;
        if (parent == null)
        {
            target.localScale = worldScale;
            return;
        }

        Vector3 parentScale = parent.lossyScale;
        target.localScale = new Vector3(
            Mathf.Approximately(parentScale.x, 0f) ? worldScale.x : worldScale.x / parentScale.x,
            Mathf.Approximately(parentScale.y, 0f) ? worldScale.y : worldScale.y / parentScale.y,
            Mathf.Approximately(parentScale.z, 0f) ? worldScale.z : worldScale.z / parentScale.z);
    }

    private void ConfigureTrapZones(ProceduralRoomLayout room)
    {
        Transform trapsRoot = FindChildByNameContains(room.transform, "Traps");
        if (trapsRoot == null)
            return;

        Renderer[] trapRenderers = trapsRoot.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer trapRenderer in trapRenderers)
        {
            if (trapRenderer == null)
                continue;

            GameObject trapObject = trapRenderer.gameObject;
            BoxCollider2D trapCollider = trapObject.GetComponent<BoxCollider2D>();
            if (trapCollider == null)
                trapCollider = trapObject.AddComponent<BoxCollider2D>();

            ConfigureBoxColliderFromRenderer(trapCollider, trapRenderer);
            trapCollider.isTrigger = true;

            TrapDamageZone trap = trapObject.GetComponent<TrapDamageZone>();
            if (trap == null)
                trap = trapObject.AddComponent<TrapDamageZone>();

            trap.Configure(trapDamage, trapDamageInterval, CombatFaction.Player);
        }
    }

    private void ConfigureCollisionBlocks(ProceduralRoomLayout room)
    {
        Transform collisionsRoot =
            FindChildByNameContains(room.transform, "Collisions");

        if (collisionsRoot == null)
            return;

        Renderer[] collisionRenderers =
            collisionsRoot.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer collisionRenderer in collisionRenderers)
        {
            if (collisionRenderer == null)
                continue;

            BoxCollider2D collision =
                collisionRenderer.GetComponent<BoxCollider2D>();

            if (collision == null)
                collision =
                    collisionRenderer.gameObject.AddComponent<BoxCollider2D>();

            ConfigureBoxColliderFromRenderer(
                collision,
                collisionRenderer
            );

            collision.isTrigger = false;
            collision.enabled = true;

            PolygonCollider2D polygon =
                collisionRenderer.GetComponent<PolygonCollider2D>();

            if (polygon != null)
                polygon.enabled = false;
        }

        CameraProjectedColliderGroup projected =
            collisionsRoot.GetComponent<CameraProjectedColliderGroup>();

        if (projected == null)
        {
            projected =
                collisionsRoot.gameObject
                    .AddComponent<CameraProjectedColliderGroup>();
        }

        projected.Initialize(
            cameraTarget,
            collisionVisualZOffset
        );
    }

    private static void ConfigureBoxColliderFromRenderer(BoxCollider2D collider, Renderer sourceRenderer)
    {
        ConfigureBoxColliderFromRenderer(collider, sourceRenderer, Vector2.zero);
    }

    private static void ConfigureBoxColliderFromRenderer(BoxCollider2D collider, Renderer sourceRenderer, Vector2 localOffset)
    {
        if (collider == null || sourceRenderer == null)
            return;

        Vector3 localCenter = sourceRenderer.transform.InverseTransformPoint(sourceRenderer.bounds.center);
        Vector3 scale = sourceRenderer.transform.lossyScale;
        float scaleX = Mathf.Approximately(scale.x, 0f) ? 1f : Mathf.Abs(scale.x);
        float scaleY = Mathf.Approximately(scale.y, 0f) ? 1f : Mathf.Abs(scale.y);
        collider.offset = (Vector2)localCenter + localOffset;
        collider.size = new Vector2(sourceRenderer.bounds.size.x / scaleX, sourceRenderer.bounds.size.y / scaleY);
    }

    private static Transform FindChildByNameContains(Transform root, string token)
    {
        if (root == null || string.IsNullOrWhiteSpace(token))
            return null;

        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
        {
            if (child != root && child.name.Contains(token, System.StringComparison.OrdinalIgnoreCase))
                return child;
        }

        return null;
    }

    private EnemyDeathNotifier[] CreateRoomEnemies(ProceduralRoomLayout room, IngredientData promisedReward)
    {
        Transform[] spawnPoints = room.EnemySpawnPoints;
        if (spawnPoints == null || spawnPoints.Length == 0)
            return System.Array.Empty<EnemyDeathNotifier>();

        HideSpawnMarkers(spawnPoints);

        int enemyCount = GetActiveEnemySpawnerCount(spawnPoints.Length, promisedReward);
        List<EnemyDeathNotifier> enemies = new();
        for (int i = 0; i < enemyCount; i++)
        {
            EnemyDeathNotifier enemy = CreateLinkedEnemy(room.transform, spawnPoints[i].position, i, promisedReward);
            enemy.gameObject.SetActive(false);
            enemies.Add(enemy);
        }

        return enemies.ToArray();
    }

    private int GetActiveEnemySpawnerCount(int totalSpawnerCount, IngredientData promisedReward)
    {
        if (totalSpawnerCount <= 0)
            return 0;

        int normalPopulation = Mathf.Max(1, currentRoomNumber + 1);
        int populationWeight = GetEnemyPopulationWeight(promisedReward);
        int weightedPopulation = normalPopulation + populationWeight;
        return Mathf.Clamp(weightedPopulation, 1, totalSpawnerCount);
    }

    private int GetEnemyPopulationWeight(IngredientData promisedReward)
    {
        GameObject enemyPrefab = promisedReward != null ? promisedReward.EnemyPrefab : null;
        if (enemyPrefab != null)
        {
            EnemyDeathNotifier notifier = enemyPrefab.GetComponent<EnemyDeathNotifier>();
            if (notifier == null)
                notifier = enemyPrefab.GetComponentInChildren<EnemyDeathNotifier>(true);

            return notifier != null ? notifier.PopulationWeight : 0;
        }

        EnemyDeathNotifier fallback = GetRiceEnemyPrefab();
        return fallback != null ? fallback.PopulationWeight : 0;
    }

    private EnemyDeathNotifier CreateLinkedEnemy(Transform parent, Vector3 position, int index, IngredientData promisedReward)
    {
        GameObject linkedPrefab = promisedReward != null ? promisedReward.EnemyPrefab : null;
        GameObject enemyObject;
        if (linkedPrefab != null)
        {
            GameObject instance = Instantiate(linkedPrefab, position, linkedPrefab.transform.rotation, parent);
            instance.name = $"{promisedReward.Id}_Enemy_{index}";
            enemyObject = instance;
        }
        else
        {
            enemyObject = CreateRiceEnemyObject(parent, position, index);
        }
        Health health = enemyObject.GetComponent<Health>();
        if (health == null)
            health = enemyObject.AddComponent<Health>();

        health.Configure(riceEnemyHitsToKill, true, true);

        FactionMember faction = enemyObject.GetComponent<FactionMember>();
        if (faction == null)
            faction = enemyObject.AddComponent<FactionMember>();

        faction.SetFaction(CombatFaction.Enemy);
        EnsureEnemyHitbox(enemyObject);

        RiceEnemy riceEnemy = enemyObject.GetComponent<RiceEnemy>();
        if (riceEnemy != null)
        {
            riceEnemy.FitHitboxToSpriteSquare();
            ConfigureRiceEnemyForCurrentRoom(riceEnemy, index);
        }

        EnemyDeathNotifier notifier = enemyObject.GetComponent<EnemyDeathNotifier>();
        if (notifier == null)
            notifier = enemyObject.AddComponent<EnemyDeathNotifier>();

        return notifier;
    }

    private GameObject CreateRiceEnemyObject(Transform parent, Vector3 position, int index)
    {
        EnemyDeathNotifier prefab = GetRiceEnemyPrefab();
        if (prefab != null)
        {
            EnemyDeathNotifier instance = Instantiate(prefab, position, prefab.transform.rotation, parent);
            instance.name = $"RiceEnemy_{index}";
            return instance.gameObject;
        }

        return CreateEnemyBase($"RiceEnemy_{index}", parent, position, Color.white);
    }

    private EnemyDeathNotifier GetRiceEnemyPrefab()
    {
        if (riceEnemyPrefab != null)
            return riceEnemyPrefab;

#if UNITY_EDITOR
        riceEnemyPrefab = AssetDatabase.LoadAssetAtPath<EnemyDeathNotifier>("Assets/Prefabs/Enemies/RiceEnemy.prefab");
#endif
        return riceEnemyPrefab;
    }

    private void EnsureEnemyHitbox(GameObject enemyObject)
    {
        BoxCollider2D hitbox = enemyObject.GetComponent<BoxCollider2D>();
        if (hitbox == null)
            hitbox = enemyObject.AddComponent<BoxCollider2D>();

        hitbox.isTrigger = false;
        hitbox.offset = Vector2.zero;
        RiceEnemy riceEnemy = enemyObject.GetComponent<RiceEnemy>();
        if (riceEnemy != null)
        {
            riceEnemy.FitHitboxToSpriteSquare();
        }
        else
        {
            hitbox.offset = Vector2.zero;
            hitbox.size = fallbackRiceEnemyHitboxSize;
        }

        CircleCollider2D circle = enemyObject.GetComponent<CircleCollider2D>();
        if (circle != null)
            circle.enabled = false;
    }

    private void ConfigureRiceEnemyForCurrentRoom(RiceEnemy riceEnemy, int index)
    {
        if (riceEnemy == null)
            return;

        int upgradeTier = Mathf.Max(0, (currentRoomNumber - 1) / 4);
        float burstChance = Mathf.Clamp01(upgradeTier <= 0 ? 0f : 0.5f + (upgradeTier - 1) * 0.2f);
        int burstLimit = Mathf.Clamp(1 + upgradeTier, 1, 4);
        float speed = 1.45f + upgradeTier * 0.12f;
        float strafeSpeed = 1.55f + upgradeTier * 0.12f;
        float minCooldown = Mathf.Max(0.65f, 1f - upgradeTier * 0.06f);
        float maxCooldown = Mathf.Max(minCooldown, 1.9f - upgradeTier * 0.08f);
        bool usesPrediction = Random.value < Mathf.Clamp01(0.3f + upgradeTier * 0.05f);

        riceEnemy.Configure(
            3.2f,
            2.2f,
            speed,
            strafeSpeed,
            minCooldown,
            maxCooldown,
            7f,
            rangedEnemyShotDamage,
            burstChance,
            burstLimit,
            usesPrediction);
    }

    private GameObject CreateEnemyBase(string enemyName, Transform parent, Vector3 position, Color color)
    {
        GameObject enemyObject = new(enemyName);
        enemyObject.transform.SetParent(parent, true);
        enemyObject.transform.position = position;
        enemyObject.transform.localScale = riceEnemyScale;

        SpriteRenderer renderer = enemyObject.AddComponent<SpriteRenderer>();
        renderer.sprite = GetRiceEnemySprite();
        renderer.color = color;
        renderer.sortingOrder = 8;

        BoxCollider2D collider = enemyObject.AddComponent<BoxCollider2D>();
        collider.size = fallbackRiceEnemyHitboxSize;

        Rigidbody2D rb = enemyObject.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        enemyObject.AddComponent<FactionMember>().SetFaction(CombatFaction.Enemy);
        enemyObject.AddComponent<Health>();
        enemyObject.AddComponent<EnemyDeathNotifier>();

        return enemyObject;
    }

    private Vector3 GetPlayerVisualScale()
    {
        CharacterInput player = FindFirstObjectByType<CharacterInput>();
        if (player == null)
            return Vector3.one;

        SpriteRenderer playerRenderer = player.GetComponentInChildren<SpriteRenderer>();
        if (playerRenderer == null)
            return player.transform.lossyScale;

        Bounds bounds = playerRenderer.bounds;
        return new Vector3(
            Mathf.Max(0.1f, bounds.size.x) * 0.65f,
            Mathf.Max(0.1f, bounds.size.y) * 0.65f,
            1f
        );
    }

    private void HideSpawnMarkers(Transform[] spawnPoints)
    {
        foreach (Transform spawnPoint in spawnPoints)
        {
            if (spawnPoint == null)
                continue;

            Renderer[] renderers = spawnPoint.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer spawnRenderer in renderers)
                spawnRenderer.enabled = false;
        }
    }

    private IngredientData[] GetRewardPool()
    {
#if UNITY_EDITOR
        string[] guids = AssetDatabase.FindAssets("t:IngredientData", new[] { "Assets/GameData/Ingredients" });
        List<IngredientData> ingredients = new();
        List<IngredientData> ingredientsWithIcons = new();
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            IngredientData ingredient = AssetDatabase.LoadAssetAtPath<IngredientData>(path);
            if (ingredient == null)
                continue;

            ingredients.Add(ingredient);
            if (ingredient.Icon != null)
                ingredientsWithIcons.Add(ingredient);
        }

        if (ingredientsWithIcons.Count > 0)
        {
            rewardPool = ingredientsWithIcons.ToArray();
            return rewardPool;
        }

        if (ingredients.Count > 0)
            return ingredients.ToArray();
#endif

        return rewardPool ?? System.Array.Empty<IngredientData>();
    }

    private Collider2D GetConfinerShapeForRoom(ProceduralRoomLayout room)
    {
        if (room == null || room.CameraLimit == null)
            return null;

        if (!UsesRectangularConfiner(room))
            return room.CameraLimit;

        if (rectangularCameraLimits.TryGetValue(room, out Collider2D existingLimit) && existingLimit != null)
            return existingLimit;

        Bounds bounds = room.CameraLimit.bounds;
        GameObject limitObject = new("Runtime_RectangularCameraLimit");
        limitObject.transform.SetParent(room.transform, true);
        limitObject.transform.position = bounds.center;

        PolygonCollider2D rectangularLimit = limitObject.AddComponent<PolygonCollider2D>();
        rectangularLimit.isTrigger = true;
        rectangularLimit.pathCount = 1;
        rectangularLimit.SetPath(0, new[]
        {
            new Vector2(-bounds.extents.x, -bounds.extents.y),
            new Vector2(-bounds.extents.x, bounds.extents.y),
            new Vector2(bounds.extents.x, bounds.extents.y),
            new Vector2(bounds.extents.x, -bounds.extents.y)
        });

        rectangularCameraLimits[room] = rectangularLimit;
        return rectangularLimit;
    }

    private bool UsesRectangularConfiner(ProceduralRoomLayout room)
    {
        if (room == null || rectangularConfinerRoomNameTokens == null)
            return false;

        string roomName = room.name.Replace("(Clone)", string.Empty).Trim();
        foreach (string token in rectangularConfinerRoomNameTokens)
        {
            if (!string.IsNullOrWhiteSpace(token) && roomName.Contains(token, System.StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private float GetOrthographicSizeForRoom(ProceduralRoomLayout room)
    {
        float requestedSize = room.CameraOrthographicSize;
        if (!autoFitOrthographicSizeToCameraLimit || room.CameraLimit == null)
            return requestedSize;

        float aspect = Camera.main != null ? Camera.main.aspect : 16f / 10f;
        Bounds bounds = room.CameraLimit.bounds;
        float maxByHeight = bounds.extents.y - cameraLimitPadding;
        float maxByWidth = bounds.extents.x / aspect - cameraLimitPadding;
        float fittedSize = Mathf.Min(requestedSize, maxByHeight, maxByWidth);

        return Mathf.Max(0.5f, Mathf.Min(requestedSize, Mathf.Max(fittedSize, minimumOrthographicSize)));
    }

    private void ApplyOrthographicSize(ProceduralRoomLayout room)
    {
        if (cinemachineCamera == null || room == null)
            return;

        LensSettings lens = cinemachineCamera.Lens;
        lens.OrthographicSize = GetOrthographicSizeForRoom(room);
        cinemachineCamera.Lens = lens;
    }

    private void EnsureCameraFollowsPlayer()
    {
        if (cinemachineCamera == null)
            cinemachineCamera = FindFirstObjectByType<CinemachineCamera>();

        if (cinemachineCamera == null)
            return;

        if (cameraTarget == null)
        {
            CharacterInput player = FindFirstObjectByType<CharacterInput>();
            if (player != null)
                cameraTarget = player.transform;
        }

        if (cameraTarget != null)
            cinemachineCamera.Follow = cameraTarget;

        CinemachineFollow follow = cinemachineCamera.GetComponent<CinemachineFollow>();
        if (follow == null)
            follow = cinemachineCamera.gameObject.AddComponent<CinemachineFollow>();

        follow.FollowOffset = cameraFollowOffset;
    }

    private bool IsKitchenRoom(int roomNumber)
    {
        if (kitchenIntervalRooms <= 0 || roomNumber < firstKitchenRoomNumber)
            return false;

        return (roomNumber - firstKitchenRoomNumber) % kitchenIntervalRooms == 0;
    }

    private ProceduralRoomCommitTrigger CreateCommitTrigger(Transform sourceDoor, RoomDirection exitDirection)
    {
        GameObject triggerObject = new($"CommitTrigger_{exitDirection}");
        triggerObject.transform.SetParent(transform, false);
        triggerObject.transform.position = sourceDoor.position + GetDirectionVector(exitDirection) * commitOffsetFromDoor;

        BoxCollider2D triggerCollider = triggerObject.AddComponent<BoxCollider2D>();
        triggerCollider.isTrigger = true;
        triggerCollider.size = commitTriggerSize;

        return triggerObject.AddComponent<ProceduralRoomCommitTrigger>();
    }

    private Vector3 GetGapOffset(RoomDirection direction)
    {
        return GetDirectionVector(direction) * connectionGap;
    }

    private static Vector3 GetDirectionVector(RoomDirection direction)
    {
        return direction switch
        {
            RoomDirection.Up => Vector3.up,
            RoomDirection.Right => Vector3.right,
            RoomDirection.Down => Vector3.down,
            RoomDirection.Left => Vector3.left,
            _ => Vector3.zero
        };
    }

    private static RoomDirection GetOpposite(RoomDirection direction)
    {
        return direction switch
        {
            RoomDirection.Up => RoomDirection.Down,
            RoomDirection.Right => RoomDirection.Left,
            RoomDirection.Down => RoomDirection.Up,
            RoomDirection.Left => RoomDirection.Right,
            _ => direction
        };
    }

    private List<RoomDirection> GetAllowedExitDirections()
    {
        List<RoomDirection> directions = new()
        {
            RoomDirection.Up,
            RoomDirection.Right,
            RoomDirection.Down,
            RoomDirection.Left
        };

        if (blockedExitDirection.HasValue)
            directions.Remove(blockedExitDirection.Value);

        for (int i = directions.Count - 1; i >= 0; i--)
        {
            if (currentRoom.GetDoor(directions[i]) == null)
                directions.RemoveAt(i);
        }

        return directions;
    }

    private void HideTemplateRooms()
    {
        hiddenTemplates.Clear();
        foreach (ProceduralRoomLayout template in roomTemplates)
        {
            if (template != null && template.gameObject != currentRoom.gameObject)
            {
                template.HideAllDoors();
                SetKitchenVisible(template, false);
                SetShopVisible(template, false, false, false);
                hiddenTemplates.Add(template);
                template.gameObject.SetActive(false);
            }
        }
    }

    private void DestroyUnselectedCandidates(ProceduralRoomCandidate selectedCandidate)
    {
        foreach (ProceduralRoomCandidate candidate in candidates)
        {
            if (candidate != null && candidate != selectedCandidate)
                Destroy(candidate.gameObject);
        }
    }

    private void DestroyCommitTriggers()
    {
        ProceduralRoomCommitTrigger[] triggers = GetComponentsInChildren<ProceduralRoomCommitTrigger>(true);
        foreach (ProceduralRoomCommitTrigger trigger in triggers)
            Destroy(trigger.gameObject);
    }

    private void ClearGeneratedCandidates()
    {
        if (currentRoom != null)
        {
            Transform[] roomChildren = currentRoom.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in roomChildren)
            {
                if (child != null && child.name.StartsWith("DoorRewardPreview_", System.StringComparison.Ordinal))
                    Destroy(child.gameObject);
            }
        }

        candidates.Clear();
        ProceduralRoomCandidate[] oldCandidates = FindObjectsByType<ProceduralRoomCandidate>(FindObjectsSortMode.None);
        foreach (ProceduralRoomCandidate oldCandidate in oldCandidates)
        {
            if (oldCandidate != null && oldCandidate.Layout != currentRoom)
                Destroy(oldCandidate.gameObject);
        }

        DestroyCommitTriggers();
    }

    private void EnsurePlayerColliderForTestScene()
    {
        if (!addFallbackPlayerCollider)
            return;

        CharacterInput player = FindFirstObjectByType<CharacterInput>();
        if (player == null || player.GetComponentInChildren<Collider2D>() != null)
            return;

        CircleCollider2D collider = player.gameObject.AddComponent<CircleCollider2D>();
        collider.radius = 0.35f;
        collider.isTrigger = false;
    }

    private void EnsurePlayerCombatSetup()
    {
        CharacterInput player = FindFirstObjectByType<CharacterInput>();
        if (player == null)
            return;

        Health health = player.GetComponent<Health>();
        if (health == null)
            health = player.gameObject.AddComponent<Health>();

        health.Configure(playerMaxHealth, true, false);

        FactionMember faction = player.GetComponent<FactionMember>();
        if (faction == null)
            faction = player.gameObject.AddComponent<FactionMember>();

        faction.SetFaction(CombatFaction.Player);

        IngredientInventory inventory = player.GetComponent<IngredientInventory>();
        if (inventory == null)
            inventory = player.gameObject.AddComponent<IngredientInventory>();

        PlayerHealthHud hud = FindFirstObjectByType<PlayerHealthHud>();
        if (hud == null)
            hud = new GameObject("PlayerHealthHUD").AddComponent<PlayerHealthHud>();

        hud.Initialize(health);

        IngredientInventoryHud inventoryHud = FindFirstObjectByType<IngredientInventoryHud>();
        if (inventoryHud == null)
            inventoryHud = new GameObject("IngredientInventoryHUD").AddComponent<IngredientInventoryHud>();

        inventoryHud.Initialize(inventory);
    }

    private Sprite GetFallbackSprite()
    {
        if (fallbackSprite != null)
            return fallbackSprite;

        Texture2D texture = new(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        fallbackSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        return fallbackSprite;
    }

    private Sprite GetRiceEnemySprite()
    {
        if (riceEnemySprite != null)
            return riceEnemySprite;

#if UNITY_EDITOR
        Object[] riceAssets = AssetDatabase.LoadAllAssetsAtPath("Assets/Images/ricemonster.png");
        foreach (Object asset in riceAssets)
        {
            if (asset is Sprite sprite && sprite.name == "ricemonster_0")
            {
                riceEnemySprite = sprite;
                return riceEnemySprite;
            }
        }

        foreach (Object asset in riceAssets)
        {
            if (asset is Sprite sprite)
            {
                riceEnemySprite = sprite;
                return riceEnemySprite;
            }
        }
#endif

        return GetFallbackSprite();
    }

    private static void Shuffle<T>(IList<T> list)
    {
        for (int i = 0; i < list.Count - 1; i++)
        {
            int randomIndex = Random.Range(i, list.Count);
            (list[i], list[randomIndex]) = (list[randomIndex], list[i]);
        }
    }

    private void SetFrontWallVisible(ProceduralRoomLayout room, bool visible)
    {
        if (room == null)
            return;

        Transform frontWall = FindChildExact(room.transform, "pared_frente");

        if (frontWall == null)
            return;

        Renderer[] renderers = frontWall.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer renderer in renderers)
        {
            if (renderer != null)
                renderer.enabled = visible;
        }
    }

    private static Transform FindChildExact(Transform root, string childName)
    {
        if (root == null)
            return null;

        Transform[] children = root.GetComponentsInChildren<Transform>(true);

        foreach (Transform child in children)
        {
            if (child.name.Equals(
                childName,
                System.StringComparison.OrdinalIgnoreCase))
            {
                return child;
            }
        }

        return null;
    }
}

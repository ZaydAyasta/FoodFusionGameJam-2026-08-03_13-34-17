using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

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
        "Pro_Room_Tpye6"
    };

    [Header("Generation")]
    [SerializeField] private int minCandidateCount = 1;
    [SerializeField] private int maxCandidateCount = 3;
    [SerializeField] private float connectionGap;
    [SerializeField] private float commitOffsetFromDoor = 0.75f;
    [SerializeField] private Vector2 commitTriggerSize = new(1.25f, 1.25f);
    [SerializeField] private float previousRoomDestroyDelay = 0.5f;
    [SerializeField] private float noRoomControllerNextBatchDelay = 1f;
    [SerializeField] private bool hideUnusedTemplates = true;
    [SerializeField] private bool addFallbackPlayerCollider = true;
    [SerializeField] private int firstKitchenRoomNumber = 4;
    [SerializeField] private int kitchenIntervalRooms = 4;

    [Header("Camera")]
    [SerializeField] private CinemachineCamera cinemachineCamera;
    [SerializeField] private CinemachineConfiner2D confiner;
    [SerializeField] private Transform cameraTarget;
    [SerializeField] private Vector3 cameraFollowOffset = new(0f, 0f, -10f);
    [SerializeField] private bool autoFitOrthographicSizeToCameraLimit = true;
    [SerializeField] private float cameraLimitPadding = 0.35f;
    [SerializeField] private float minimumOrthographicSize = 3.5f;
    [SerializeField] private string[] rectangularConfinerRoomNameTokens = { "Pro_Room_Tpye3", "Pro_Room_Type3" };

    private readonly List<ProceduralRoomCandidate> candidates = new();
    private readonly List<ProceduralRoomLayout> hiddenTemplates = new();
    private readonly Dictionary<ProceduralRoomLayout, Collider2D> rectangularCameraLimits = new();
    private readonly string[] placeholderThemes = { "Cheese", "Bread", "Fish", "Butter", "Corn", "Rice" };

    private RoomDirection? blockedExitDirection;
    private RoomController activeRoomController;
    private float nextBatchAt = -1f;
    private bool waitingForRoomCompletion;
    private int currentRoomNumber = 1;

    private void Start()
    {
        ResolveSceneReferences();

        if (currentRoom == null || roomTemplates == null || roomTemplates.Length == 0)
        {
            Debug.LogError("RoomGenerationTestBootstrap needs a current room and at least one room template.");
            enabled = false;
            return;
        }

        currentRoom.AutoWire();
        foreach (ProceduralRoomLayout template in roomTemplates)
        {
            template.AutoWire();
            template.SetKitchenVisible(false);
        }

        EnsurePlayerColliderForTestScene();
        EnsureCameraFollowsPlayer();
        ClearGeneratedCandidates();

        if (hideUnusedTemplates)
            HideTemplateRooms();

        SetCurrentRoom(currentRoom);
        GenerateCandidateBatch();
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

    private void GenerateCandidateBatch()
    {
        ClearGeneratedCandidates();

        ProceduralRoomLayout selectedTemplate = roomTemplates[Random.Range(0, roomTemplates.Length)];
        selectedTemplate.AutoWire();

        List<RoomDirection> directions = GetAllowedExitDirections();
        Shuffle(directions);

        int count = Mathf.Clamp(Random.Range(minCandidateCount, maxCandidateCount + 1), 1, directions.Count);
        List<RoomDirection> activeExitDirections = new();
        for (int i = 0; i < count; i++)
        {
            RoomDirection exitDirection = directions[i];
            string theme = placeholderThemes[i % placeholderThemes.Length];
            if (CreateCandidate(selectedTemplate, exitDirection, theme, $"Candidate_{exitDirection}_{selectedTemplate.name}") != null)
                activeExitDirections.Add(exitDirection);
        }

        currentRoom.ShowOnlyDoors(activeExitDirections);
        currentRoom.SetKitchenVisible(IsKitchenRoom(currentRoomNumber));
    }

    private ProceduralRoomLayout CreateCandidate(
        ProceduralRoomLayout candidateTemplate,
        RoomDirection exitDirection,
        string theme,
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
        candidate.ShowOnlyDoors(new[] { entryDirection });
        candidate.SetKitchenVisible(IsKitchenRoom(currentRoomNumber + 1));

        Vector3 offset = sourceDoor.position - candidate.GetDoor(entryDirection).position;
        candidate.transform.position += offset + GetGapOffset(exitDirection);

        ProceduralRoomCommitTrigger trigger = CreateCommitTrigger(sourceDoor, exitDirection);
        ProceduralRoomCandidate metadata = candidate.gameObject.AddComponent<ProceduralRoomCandidate>();
        metadata.Initialize(theme, exitDirection, entryDirection, candidate, trigger);
        trigger.Initialize(this, metadata);
        candidates.Add(metadata);

        return candidate;
    }

    public void CommitCandidate(ProceduralRoomCandidate selectedCandidate, Transform player)
    {
        if (selectedCandidate == null || !candidates.Contains(selectedCandidate))
            return;

        ProceduralRoomLayout previousRoom = currentRoom;
        currentRoom = selectedCandidate.Layout;
        blockedExitDirection = selectedCandidate.EntryDirection;
        currentRoomNumber++;

        DestroyUnselectedCandidates(selectedCandidate);
        DestroyCommitTriggers();
        currentRoom.HideAllDoors();
        SetCurrentRoom(currentRoom);

        if (previousRoom != null && previousRoom != currentRoom)
            Destroy(previousRoom.gameObject, previousRoomDestroyDelay);

        Destroy(selectedCandidate);
        candidates.Clear();

        activeRoomController = currentRoom.GetComponent<RoomController>();
        waitingForRoomCompletion = true;
        nextBatchAt = Time.time + noRoomControllerNextBatchDelay;
    }

    private void SetCurrentRoom(ProceduralRoomLayout room)
    {
        if (room == null)
            return;

        activeRoomController = room.GetComponent<RoomController>();
        EnsureCameraFollowsPlayer();
        room.SetKitchenVisible(IsKitchenRoom(currentRoomNumber));

        if (cinemachineCamera != null && room.CameraOrthographicSize > 0f)
        {
            LensSettings lens = cinemachineCamera.Lens;
            lens.ModeOverride = LensSettings.OverrideModes.Orthographic;
            lens.OrthographicSize = GetOrthographicSizeForRoom(room);
            cinemachineCamera.Lens = lens;
        }

        Collider2D boundingShape = GetConfinerShapeForRoom(room);
        if (confiner != null && boundingShape != null)
        {
            confiner.BoundingShape2D = boundingShape;
            confiner.InvalidateBoundingShapeCache();
            confiner.InvalidateLensCache();
        }
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
                template.SetKitchenVisible(false);
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

    private static void Shuffle<T>(IList<T> list)
    {
        for (int i = 0; i < list.Count - 1; i++)
        {
            int randomIndex = Random.Range(i, list.Count);
            (list[i], list[randomIndex]) = (list[randomIndex], list[i]);
        }
    }
}

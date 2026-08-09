using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

public class CameraProjectedColliderGroup : MonoBehaviour
{
    private class Entry
    {
        public BoxCollider2D box;
        public Vector2 originalOffset;
        public Vector2 originalSize;
    }

    private readonly List<Entry> entries = new();

    private Camera targetCamera;
    private Transform gameplayTarget;
    private float visualZOffset;
    private bool initialized;

    public void Initialize(Transform target, float zOffset)
    {
        gameplayTarget = target;
        visualZOffset = zOffset;

        if (targetCamera == null)
            targetCamera = Camera.main;

        if (!initialized)
            Build();

        UpdateAll();
    }

    private void OnEnable()
    {
        CinemachineCore.CameraUpdatedEvent.AddListener(OnCameraUpdated);
    }

    private void OnDisable()
    {
        CinemachineCore.CameraUpdatedEvent.RemoveListener(OnCameraUpdated);
    }

    private void Build()
    {
        entries.Clear();

        BoxCollider2D[] boxes =
            GetComponentsInChildren<BoxCollider2D>(true);

        foreach (BoxCollider2D box in boxes)
        {
            if (box == null || box.isTrigger)
                continue;

            PolygonCollider2D polygon =
                box.GetComponent<PolygonCollider2D>();

            if (polygon != null)
                polygon.enabled = false;

            box.enabled = true;

            entries.Add(new Entry
            {
                box = box,
                originalOffset = box.offset,
                originalSize = box.size
            });
        }

        initialized = true;
    }

    private void OnCameraUpdated(CinemachineBrain brain)
    {
        if (!initialized)
            return;

        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera == null ||
            brain == null ||
            brain.OutputCamera != targetCamera)
            return;

        UpdateAll();
    }

    private void UpdateAll()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera == null)
            return;

        float gameplayZ =
            gameplayTarget != null
                ? gameplayTarget.position.z
                : 0f;

        Plane gameplayPlane =
            new Plane(
                Vector3.forward,
                new Vector3(0f, 0f, gameplayZ)
            );

        for (int i = entries.Count - 1; i >= 0; i--)
        {
            Entry entry = entries[i];

            if (entry.box == null)
            {
                entries.RemoveAt(i);
                continue;
            }

            UpdateCollider(entry, gameplayPlane, gameplayZ);
        }
    }

    private void UpdateCollider(
        Entry entry,
        Plane gameplayPlane,
        float gameplayZ)
    {
        BoxCollider2D box = entry.box;

        Vector3 groundCenter =
            box.transform.TransformPoint(entry.originalOffset);

        Vector3 perspectivePoint = groundCenter;
        perspectivePoint.z = gameplayZ + visualZOffset;

        Vector3 screenPoint =
            targetCamera.WorldToScreenPoint(perspectivePoint);

        Ray ray =
            targetCamera.ScreenPointToRay(screenPoint);

        if (!gameplayPlane.Raycast(ray, out float distance))
            return;

        Vector3 desiredWorldCenter =
            ray.GetPoint(distance);

        Vector3 desiredLocalCenter =
            box.transform.InverseTransformPoint(desiredWorldCenter);

        box.offset = new Vector2(
            desiredLocalCenter.x,
            desiredLocalCenter.y
        );

        box.size = entry.originalSize;
    }
}

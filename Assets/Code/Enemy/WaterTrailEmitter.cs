using UnityEngine;

public class WaterTrailEmitter : MonoBehaviour
{
    private Sprite wetSprite;
    private float spacing;
    private float patchSize;
    private float patchLifetime;
    private float patchAlpha;
    private Vector3 lastPatchPosition;
    private string sortingLayer;
    private int sortingOrder;
    private int patchesCreated;
    private int maxPatches;
    private bool configured;

    public void Configure(
        Sprite sprite,
        float trailSpacing,
        float wetPatchSize,
        float wetPatchLifetime,
        float alpha,
        int patchLimit)
    {
        wetSprite = sprite;
        spacing = Mathf.Max(0.1f, trailSpacing);
        patchSize = Mathf.Max(0.05f, wetPatchSize);
        patchLifetime = Mathf.Max(0.1f, wetPatchLifetime);
        patchAlpha = Mathf.Clamp01(alpha);
        maxPatches = Mathf.Max(1, patchLimit);
        lastPatchPosition = transform.position;

        SpriteRenderer sourceRenderer = GetComponentInChildren<SpriteRenderer>(true);
        sortingLayer = sourceRenderer != null ? sourceRenderer.sortingLayerName : "Default";
        sortingOrder = sourceRenderer != null ? sourceRenderer.sortingOrder - 1 : 0;
        configured = true;
        CreatePatch(lastPatchPosition);
    }

    private void Update()
    {
        if (!configured || patchesCreated >= maxPatches)
            return;

        Vector3 current = transform.position;
        Vector3 delta = current - lastPatchPosition;
        float distance = delta.magnitude;
        if (distance < spacing)
            return;

        Vector3 direction = delta / distance;
        while (distance >= spacing && patchesCreated < maxPatches)
        {
            lastPatchPosition += direction * spacing;
            CreatePatch(lastPatchPosition);
            delta = current - lastPatchPosition;
            distance = delta.magnitude;
        }
    }

    private void CreatePatch(Vector3 position)
    {
        GameObject patchObject = new("Wet Patch");
        position.z += 0.08f;
        patchObject.transform.position = position;
        WaterWetPatch patch = patchObject.AddComponent<WaterWetPatch>();
        patch.Initialize(wetSprite, patchSize, patchLifetime, patchAlpha,
            sortingLayer, sortingOrder);
        patchesCreated++;
    }
}

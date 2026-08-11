using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public sealed class FakeProjectedLight : MonoBehaviour
{
    private enum LightBlendMode
    {
        Additive,
        SoftAdditive,
        Alpha,
        Multiply,
        Screen
    }

    [SerializeField] private Color color = new(1f, 0.72f, 0.42f, 1f);
    [SerializeField, Range(0f, 2f)] private float intensity = 0.2f;
    [SerializeField, Min(0.1f)] private float diameter = 8f;
    [SerializeField, Range(0.01f, 1f)] private float softness = 0.7f;
    [SerializeField] private LightBlendMode blendingMode = LightBlendMode.SoftAdditive;
    [SerializeField] private int orderInLayer = 20;

    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;
    private Material material;
    private Mesh mesh;
    private bool isBuilding;

    private void OnEnable() => Build();
    private void OnValidate() => Build();

    private void Build()
    {
        // Adding a component in OnValidate can invoke OnValidate again immediately.
        // Guard the setup so a partially-created renderer is never accessed.
        if (isBuilding)
            return;

        isBuilding = true;

        try
        {
            if (!TryGetComponent(out meshFilter))
                meshFilter = gameObject.AddComponent<MeshFilter>();

            if (!TryGetComponent(out meshRenderer))
                meshRenderer = gameObject.AddComponent<MeshRenderer>();

            if (meshFilter == null || meshRenderer == null)
                return;

            if (mesh == null)
            {
                mesh = new Mesh { name = "Fake Projected Light Quad", hideFlags = HideFlags.DontSave };
                mesh.vertices = new[]
                {
                    new Vector3(-0.5f, -0.5f), new Vector3(0.5f, -0.5f),
                    new Vector3(0.5f, 0.5f), new Vector3(-0.5f, 0.5f)
                };
                mesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
                mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
                mesh.RecalculateBounds();
            }

            Shader shader = Shader.Find("FoodFusion/Fake Projected Light");
            if (shader == null)
                return;

            if (material == null)
                material = new Material(shader) { hideFlags = HideFlags.DontSave };

            Color output = color;
            output.a = intensity;
            material.SetColor("_Color", output);
            material.SetFloat("_Softness", softness);
            ApplyBlendingMode();
            meshFilter.sharedMesh = mesh;
            meshRenderer.sharedMaterial = material;
            meshRenderer.sortingLayerName = "Default";
            meshRenderer.sortingOrder = orderInLayer;
            transform.localScale = new Vector3(diameter, diameter, 1f);
        }
        finally
        {
            isBuilding = false;
        }
    }

    private void ApplyBlendingMode()
    {
        BlendMode source;
        BlendMode destination;

        switch (blendingMode)
        {
            case LightBlendMode.Alpha:
                source = BlendMode.SrcAlpha;
                destination = BlendMode.OneMinusSrcAlpha;
                break;
            case LightBlendMode.Multiply:
                source = BlendMode.DstColor;
                destination = BlendMode.Zero;
                break;
            case LightBlendMode.SoftAdditive:
                source = BlendMode.OneMinusDstColor;
                destination = BlendMode.One;
                break;
            case LightBlendMode.Screen:
                source = BlendMode.One;
                destination = BlendMode.OneMinusSrcColor;
                break;
            default:
                source = BlendMode.SrcAlpha;
                destination = BlendMode.One;
                break;
        }

        material.SetInt("_SrcBlend", (int)source);
        material.SetInt("_DstBlend", (int)destination);
        material.SetFloat("_BlendMode", (float)blendingMode);
    }
}

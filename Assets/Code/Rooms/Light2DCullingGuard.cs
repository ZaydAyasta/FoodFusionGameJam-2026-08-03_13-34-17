using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(Light2D))]
[DefaultExecutionOrder(10000)]
public sealed class Light2DCullingGuard : MonoBehaviour
{
    [SerializeField, Min(1f)] private float cullingRadius = 1000f;

    private static readonly FieldInfo BoundingSphereField = typeof(Light2D).GetField(
        "<boundingSphere>k__BackingField",
        BindingFlags.Instance | BindingFlags.NonPublic);

    private Light2D light2D;

    private void OnEnable()
    {
        light2D = GetComponent<Light2D>();
        ExpandCullingSphere();
    }

    private void LateUpdate() => ExpandCullingSphere();

    private void ExpandCullingSphere()
    {
        if (light2D == null)
            light2D = GetComponent<Light2D>();

        if (light2D == null || BoundingSphereField == null)
            return;

        BoundingSphereField.SetValue(
            light2D,
            new BoundingSphere(light2D.transform.position, cullingRadius));
    }
}

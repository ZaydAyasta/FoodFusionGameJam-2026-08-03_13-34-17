using UnityEngine;

[RequireComponent(typeof(CharacterInput))]
public class PlayerAim : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;

    private CharacterInput input;

    public Vector2 AimDirection { get; private set; } = Vector2.right;

    private void Awake()
    {
        input = GetComponent<CharacterInput>();
        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    private void Update()
    {
        if (input.LookInput.sqrMagnitude > 0.25f)
            AimDirection = input.LookInput.normalized;

        if (targetCamera == null || input.LookScreenPosition == Vector2.zero)
            return;

        Vector3 world = targetCamera.ScreenToWorldPoint(input.LookScreenPosition);
        Vector2 direction = (Vector2)(world - transform.position);
        if (direction.sqrMagnitude > 0.001f)
            AimDirection = direction.normalized;
    }
}

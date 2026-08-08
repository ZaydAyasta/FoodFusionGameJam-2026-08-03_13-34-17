using UnityEngine;

[RequireComponent(typeof(CharacterInput))]
public class PlayerAim : MonoBehaviour
{
    [SerializeField] private Vector2 defaultDirection = Vector2.down;
    [SerializeField] private float directionDeadZone = 0.1f;

    private CharacterInput input;

    public Vector2 AimDirection { get; private set; } = Vector2.down;

    private void Awake()
    {
        input = GetComponent<CharacterInput>();
        AimDirection = QuantizeToEightDirections(defaultDirection);
    }

    private void Update()
    {
        Vector2 move = input.MoveInput;
        if (move.sqrMagnitude <= directionDeadZone * directionDeadZone)
            return;

        AimDirection = QuantizeToEightDirections(move);
    }

    private static Vector2 QuantizeToEightDirections(Vector2 direction)
    {
        if (direction.sqrMagnitude <= 0.001f)
            return Vector2.down;

        float x = Mathf.Abs(direction.x) > 0.1f ? Mathf.Sign(direction.x) : 0f;
        float y = Mathf.Abs(direction.y) > 0.1f ? Mathf.Sign(direction.y) : 0f;
        Vector2 quantized = new(x, y);
        return quantized.sqrMagnitude > 0.001f ? quantized.normalized : Vector2.down;
    }
}

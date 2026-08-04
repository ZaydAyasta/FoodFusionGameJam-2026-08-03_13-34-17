using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterInput : MonoBehaviour
{
    private InputSystem_Actions input;

    public Vector2 MoveInput { get; private set; }
    public Vector2 LookScreenPosition { get; private set; }
    public Vector2 LookInput { get; private set; }
    public bool AttackHeld { get; private set; }
    public bool DashPressed { get; private set; }

    private void Awake()
    {
        EnsureInput();
    }

    private void OnEnable()
    {
        EnsureInput();
        input.Enable();
    }

    private void Update()
    {
        EnsureInput();

        MoveInput = input.Player.Move.ReadValue<Vector2>();
        LookInput = input.Player.Look.ReadValue<Vector2>();
        if (Mouse.current != null)
            LookScreenPosition = Mouse.current.position.ReadValue();
        AttackHeld = input.Player.Attack.IsPressed();
        DashPressed = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
    }

    private void OnDisable()
    {
        input?.Disable();
    }

    private void OnDestroy()
    {
        input?.Dispose();
        input = null;
    }

    private void EnsureInput()
    {
        input ??= new InputSystem_Actions();
    }
}

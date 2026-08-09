using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterInput : MonoBehaviour
{
    private InputSystem_Actions input;
    private Vector2 previousRawMoveInput;
    private bool roomEntryInputGated;

    public Vector2 MoveInput { get; private set; }
    public Vector2 LookScreenPosition { get; private set; }
    public Vector2 LookInput { get; private set; }
    public bool AttackHeld { get; private set; }
    public bool DashPressed { get; private set; }
    public bool HasGameplayInput => MoveInput.sqrMagnitude > 0.01f || AttackHeld || DashPressed;
    public bool GameplayInputPressedThisFrame { get; private set; }

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

        Vector2 rawMoveInput = input.Player.Move.ReadValue<Vector2>();
        bool rawAttackHeld = input.Player.Attack.IsPressed();
        bool rawDashPressed = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;

        LookInput = input.Player.Look.ReadValue<Vector2>();
        if (Mouse.current != null)
            LookScreenPosition = Mouse.current.position.ReadValue();

        bool horizontalPressed = Mathf.Abs(rawMoveInput.x) > 0.1f && Mathf.Abs(previousRawMoveInput.x) <= 0.1f;
        bool verticalPressed = Mathf.Abs(rawMoveInput.y) > 0.1f && Mathf.Abs(previousRawMoveInput.y) <= 0.1f;
        bool movementKeyPressed = Keyboard.current != null &&
            (Keyboard.current.wKey.wasPressedThisFrame
             || Keyboard.current.aKey.wasPressedThisFrame
             || Keyboard.current.sKey.wasPressedThisFrame
             || Keyboard.current.dKey.wasPressedThisFrame
             || Keyboard.current.upArrowKey.wasPressedThisFrame
             || Keyboard.current.downArrowKey.wasPressedThisFrame
             || Keyboard.current.leftArrowKey.wasPressedThisFrame
             || Keyboard.current.rightArrowKey.wasPressedThisFrame);
        bool attackPressed = input.Player.Attack.WasPressedThisFrame();
        bool newGameplayInput = movementKeyPressed || horizontalPressed || verticalPressed || attackPressed || rawDashPressed;
        previousRawMoveInput = rawMoveInput;

        if (roomEntryInputGated && !newGameplayInput)
        {
            MoveInput = Vector2.zero;
            AttackHeld = false;
            DashPressed = false;
            GameplayInputPressedThisFrame = false;
            return;
        }

        if (roomEntryInputGated)
            roomEntryInputGated = false;

        MoveInput = rawMoveInput;
        AttackHeld = rawAttackHeld;
        DashPressed = rawDashPressed;
        GameplayInputPressedThisFrame = newGameplayInput;
    }

    public void BeginRoomEntryInputGate()
    {
        EnsureInput();
        roomEntryInputGated = true;
        previousRawMoveInput = input.Player.Move.ReadValue<Vector2>();
        MoveInput = Vector2.zero;
        AttackHeld = false;
        DashPressed = false;
        GameplayInputPressedThisFrame = false;
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

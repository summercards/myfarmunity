// Assets/Scripts/Character/TPSInput.cs
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// ��̬���� InputActions������ .inputactions ��Դ��
/// ���� Move/Look/Sprint/Jump ����״̬
/// </summary>
[DefaultExecutionOrder(-100)]
public class TPSInput : MonoBehaviour
{
    public Vector2 Move { get; private set; }
    public Vector2 Look { get; private set; }
    public bool SprintHeld { get; private set; }
    public bool JumpPressed { get; private set; }   // ÿ֡����
    public bool JumpHeld { get; private set; }

    InputAction move;
    InputAction look;
    InputAction sprint;
    InputAction jump;

    void OnEnable()
    {
        // Move: WASD / Arrows / Gamepad LeftStick
        move = new InputAction("Move", InputActionType.Value);
        move.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
        move.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
        move.AddBinding("<Gamepad>/leftStick");
        move.Enable();

        // Look: Mouse delta / Gamepad RightStick
        look = new InputAction("Look", InputActionType.Value);
        look.AddBinding("<Mouse>/delta");
        look.AddBinding("<Gamepad>/rightStick");
        look.Enable();

        // Sprint: Shift / L3
        sprint = new InputAction("Sprint", InputActionType.Button);
        sprint.AddBinding("<Keyboard>/leftShift");
        sprint.AddBinding("<Gamepad>/leftStickButton");
        sprint.Enable();

        // Jump: Space / South
        jump = new InputAction("Jump", InputActionType.Button);
        jump.AddBinding("<Keyboard>/space");
        jump.AddBinding("<Gamepad>/buttonSouth");
        jump.Enable();
    }

    void OnDisable()
    {
        move?.Disable(); look?.Disable(); sprint?.Disable(); jump?.Disable();
    }

    void Update()
    {
        Move = move.ReadValue<Vector2>();
        Look = look.ReadValue<Vector2>();
        SprintHeld = sprint.IsPressed();
        // ���ؼ��
        JumpPressed = jump.WasPressedThisFrame();
        JumpHeld = jump.IsPressed();
    }

    /// <summary> �����á���������������һ�ΰ��� </summary>
    public void ConsumeJump() => JumpPressed = false;
}

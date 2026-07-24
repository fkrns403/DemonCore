using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 플레이어의 입력을 읽어 PlayerController가 사용할 수 있는 값으로 보관합니다.
/// 실제 이동, 공격, 회피 실행은 담당하지 않습니다.
/// </summary>
public class PlayerInputReader : MonoBehaviour
{
    [Header("Input Timing")]
    [SerializeField, Tooltip("좌우 입력을 회피 판정까지 버퍼에 담는 시간입니다.")]
    private float sideInputBufferTime = 0.15f;

    public Vector2 MoveInput { get; private set; }
    public bool JumpPressed { get; private set; }
    public bool SprintHeld { get; private set; }
    public bool DodgePressed { get; private set; }

    public bool LightAttackPressed { get; private set; }
    public bool HeavyAttackPressed { get; private set; }

    /// <summary>패링 시작 입력입니다. E를 처음 누른 프레임에 true입니다.</summary>
    public bool GuardPressed { get; private set; }

    /// <summary>가드 유지 입력입니다. E를 누르고 있는 동안 true입니다.</summary>
    public bool GuardHeld { get; private set; }

    /// <summary>와이어 액션 입력입니다. Q를 처음 누른 프레임에 true입니다.</summary>
    public bool WirePressed { get; private set; }
    public bool WireFastDescendHeld { get; private set; }

    public bool InteractPressed { get; private set; }
    public bool InteractHeld { get; private set; }
    public bool ScanPressed { get; private set; }
    public bool LockOnPressed { get; private set; }

    private float lastSideInput;
    private float sideInputBufferTimer;

    public float BufferedSideInput
    {
        get
        {
            if (sideInputBufferTimer > 0f)
            {
                return lastSideInput;
            }

            return 0f;
        }
    }

    /// <summary>
    /// 현재 프레임의 입력을 갱신합니다.
    /// PlayerController.Update에서 가장 먼저 호출하는 것을 권장합니다.
    /// </summary>
    public void ReadInput()
    {
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;

        ResetFrameInput();
        UpdateInputBuffers();

        if (keyboard == null)
        {
            return;
        }

        ReadMoveInput(keyboard);
        ReadMovementActionInput(keyboard);
        ReadCombatInput(keyboard, mouse);
        ReadInteractionInput(keyboard);
        ReadUtilityInput(keyboard);
    }

    private void ResetFrameInput()
    {
        MoveInput = Vector2.zero;

        JumpPressed = false;
        SprintHeld = false;
        DodgePressed = false;

        LightAttackPressed = false;
        HeavyAttackPressed = false;

        GuardPressed = false;
        GuardHeld = false;

        WirePressed = false;
        WireFastDescendHeld = false;

        InteractPressed = false;
        InteractHeld = false;
        ScanPressed = false;
        LockOnPressed = false;
    }

    private void UpdateInputBuffers()
    {
        if (sideInputBufferTimer > 0f)
        {
            sideInputBufferTimer -= Time.deltaTime;
        }
    }

    private void ReadMoveInput(Keyboard keyboard)
    {
        float x = 0f;
        float y = 0f;

        if (keyboard.aKey.isPressed) x -= 1f;
        if (keyboard.dKey.isPressed) x += 1f;
        if (keyboard.sKey.isPressed) y -= 1f;
        if (keyboard.wKey.isPressed) y += 1f;

        Vector2 rawMoveInput = new Vector2(x, y);

        if (rawMoveInput.sqrMagnitude > 1f)
        {
            rawMoveInput.Normalize();
        }

        MoveInput = rawMoveInput;

        if (Mathf.Abs(x) > 0.01f)
        {
            lastSideInput = Mathf.Sign(x);
            sideInputBufferTimer = sideInputBufferTime;
        }
    }

    private void ReadMovementActionInput(Keyboard keyboard)
    {
        JumpPressed = keyboard.spaceKey.wasPressedThisFrame;

        // Shift = 회피, Ctrl = 달리기
        DodgePressed = keyboard.leftShiftKey.wasPressedThisFrame ||
                       keyboard.rightShiftKey.wasPressedThisFrame;

        SprintHeld = keyboard.leftCtrlKey.isPressed ||
                     keyboard.rightCtrlKey.isPressed;
    }

    private void ReadCombatInput(Keyboard keyboard, Mouse mouse)
    {
        // E = 가드 유지 / 패링 시작
        GuardPressed = keyboard.eKey.wasPressedThisFrame;
        GuardHeld = keyboard.eKey.isPressed;

        // Q = 와이어
        WirePressed = keyboard.qKey.wasPressedThisFrame;
        WireFastDescendHeld = keyboard.leftShiftKey.isPressed ||
                              keyboard.rightShiftKey.isPressed;

        if (mouse == null)
        {
            return;
        }

        LightAttackPressed = mouse.leftButton.wasPressedThisFrame;
        HeavyAttackPressed = mouse.rightButton.wasPressedThisFrame;
    }

    private void ReadInteractionInput(Keyboard keyboard)
    {
        InteractPressed = keyboard.fKey.wasPressedThisFrame;
        InteractHeld = keyboard.fKey.isPressed;
    }

    private void ReadUtilityInput(Keyboard keyboard)
    {
        ScanPressed = keyboard.rKey.wasPressedThisFrame;
        LockOnPressed = keyboard.tabKey.wasPressedThisFrame;
    }
}

using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 플레이어의 입력을 읽어 PlayerController가 사용할 수 있는 값을 보관하는 클레스 
/// 실제 이동, 공격, 회피 실행은 담당하지 않는다
/// </summary>

public class PlayerInputReader : MonoBehaviour
{
    public Vector2 MoveInput {  get; private set; }
    // w,a,s,d 를 x,y값으로
    public bool JumpPressed {  get; private set; }
    // 점프 키
    public bool SprintHeld {  get; private set; }
    // 대쉬
    public bool DodgePressed {  get; private set; }
    // 회피키
    public bool LightAttackPressed { get; private set; }
    // 마우스 좌클릭 공격
    public bool HeavyAttackPressed { get; private set; }
    // 강공 우클릭
    public bool GuardPressed { get; private set; }
    public bool GuardHeld { get; private set; }
    // 방어
    public bool InteractPressed { get; private set; }
    // 상호 작용키
    public bool ScanPressed { get; private set; }
    // 스켄 기능
    public bool LockOnPressed { get; private set; }
    // 록온




    public void ReadInput()
    {
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;
        ResetInput();
        if (keyboard == null)
        {
            return;
        }
        ReadMoveInput(keyboard);
        ReadMovementActionInput(keyboard);
        ReadCombatInput(keyboard,mouse);
        ReadInteractionInput(keyboard);
        ReadUtilityInput(keyboard);
    }


    private void ResetInput()
    {
        MoveInput = Vector2.zero;
        JumpPressed = false;
        SprintHeld = false;
        DodgePressed = false;

        LightAttackPressed = false;
        HeavyAttackPressed = false;

        GuardPressed = false;
        GuardHeld = false;

        InteractPressed = false;
        ScanPressed = false;
        LockOnPressed = false;
    }

    private void ReadMoveInput(Keyboard keyboard)
    {
        float x = 0f;
        float y = 0f;
        if (keyboard.aKey.isPressed)
        {
            x -= 1f;
        }
        if (keyboard.dKey.isPressed)
        {
            x += 1f;
        }
        if (keyboard.sKey.isPressed)
        {
            y -= 1f;
        }
        if (keyboard.wKey.isPressed)
        {
            y += 1f;
        }
        Vector2 rawMoveInput = new Vector2(x, y);

        if (rawMoveInput.sqrMagnitude > 1f)
        {
            rawMoveInput.Normalize();
        }
        MoveInput = rawMoveInput;

    }

    private void ReadMovementActionInput(Keyboard keyboard)
    {
        JumpPressed = keyboard.spaceKey.wasPressedThisFrame;
        SprintHeld = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
        DodgePressed = keyboard.leftShiftKey.wasPressedThisFrame || keyboard.rightShiftKey.wasPressedThisFrame;
    }


    private void ReadCombatInput(Keyboard keyboard, Mouse mouse)
    {
        GuardPressed = keyboard.qKey.wasPressedThisFrame;
        GuardHeld = keyboard.qKey.isPressed;
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
    }

    private void ReadUtilityInput(Keyboard keyboard)
    {
        ScanPressed = keyboard.rKey.wasPressedThisFrame;
        LockOnPressed = keyboard.tabKey.wasPressedThisFrame;
    }
    
}

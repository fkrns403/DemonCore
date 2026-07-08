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
    public bool AttackPressed {  get; private set; }
    // 공격키
    public bool GuardHeld {  get; private set; }
    // 가드
    public bool ParryPressed { get; private set; }
    // 패링
    public bool InteractPressed {  get; private set; }
    // 상호작용
    public bool ScanPressed {  get; private set; }


    public void ReadInput()
    {
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;
        //임시 입력처리
        ReadInput();
        // 임시 입력 초기화

        if (keyboard == null)
        {
            return;
        }

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
        if (keyboard.wKey.isPressed)
        {
            y += 1f;
        }
        if (keyboard.sKey.isPressed)
        {
            y -= 1f;
        }
        //임시 입력처리

        Vector2 rawMoveInput = new Vector2(x, y);

        if (rawMoveInput.sqrMagnitude > 1f)
        {
            rawMoveInput.Normalize();
        }

        MoveInput = rawMoveInput;

        

    }

    private void ResetInput()
    {
        MoveInput = Vector2.zero;

        JumpPressed = false;
        SprintHeld = false;
        DodgePressed = false;

        AttackPressed = false;
        GuardHeld = false;
        ParryPressed = false;

        InteractPressed = false;
        ScanPressed = false;
    }



    
}

using State;
using UnityEngine;


/// <summary>
/// 플레이어 중심제어
/// playerInputReader 의 입력을 읽고 PlayerState를 전환
/// 
/// </summary>
[RequireComponent(typeof(PlayerInputReader))]
[RequireComponent(typeof(PlayerMovement))]
public class PlayerController : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField,Tooltip("true면 상태 변경시 로그 생성")]
    private bool showDebugLog = true;

    private PlayerInputReader inputReader;
    private PlayerMovement playerMovement;
    private PlayerState currentState;
    
    public PlayerState CurrentState => currentState;
    // 외부에서 읽기 전용

    private void Awake()
    {
        inputReader = GetComponent<PlayerInputReader>();
        playerMovement = GetComponent<PlayerMovement>();

        ChangeState(PlayerState.Idle);
    }

    private void Update()
    {
        inputReader.ReadInput();

        UpdateStateForTest();
        UpdateMovement();
        UpdateUtilityInputForTest();
    }

    private void UpdateStateForTest()
    {
        if (currentState == PlayerState.Dead)
        {
            return;
        }

        if (inputReader.LightAttackPressed || inputReader.HeavyAttackPressed)
        {
            ChangeState(PlayerState.Attack);
            return;
        }

        if (inputReader.DodgePressed)
        {
            ChangeState(PlayerState.Dodge);
            return;
        }

        if (inputReader.JumpPressed)
        {
            ChangeState(PlayerState.Jump);
            return;
        }

        if (inputReader.GuardHeld)
        {
            ChangeState(PlayerState.Guard);
            return;
        }

        if (inputReader.ScanPressed)
        {
            ChangeState(PlayerState.Scan);
            return;
        }

        if (inputReader.InteractPressed)
        {
            ChangeState(PlayerState.Interact);
            return;
        }

        if (inputReader.MoveInput.sqrMagnitude > 0.01f)
        {
            ChangeState(PlayerState.Move);
            return;
        }

        ChangeState(PlayerState.Idle);
    }

    private void UpdateMovement()
    {
        if (!CanUseInputMovement())
        {
            return;
        }
        playerMovement.Move(inputReader.MoveInput, inputReader.SprintHeld);
    }

    private bool CanUseInputMovement()
    {
        if (currentState == PlayerState.Dead)
        {
            return false;
        }
        if (currentState == PlayerState.Knockdown)
        {
            return false;
        }
        if (currentState == PlayerState.GetUp)
        {
            return false;
        }
        return true;
    }

    private void UpdateUtilityInputForTest()
    {
        if (inputReader.LockOnPressed && showDebugLog)
        {
            Debug.Log("LockOn Input Pressed");
        }
    }

    private void ChangeState(PlayerState nextState)
    {
        if (currentState == nextState)
        {
            return;
        }
        PlayerState previousState = currentState;
        currentState = nextState;
        if (showDebugLog)
        {
            Debug.Log($"player State changed : {previousState} -> {currentState}");
        }
    }

}

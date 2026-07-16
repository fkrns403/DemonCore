using State;
using Unity.Android.Gradle.Manifest;
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

    [Header("Action Lock")]
    [SerializeField, Tooltip("공격 입력후 이동입력 락")]
    private float lightAttackLockDuration = 0.35f;
    [SerializeField, Tooltip("강공격 입력 후 일반 이동 방지 시간")]
    private float heavyAttackLockDuration = 0.55f;

    [Header("Attack Combo")]
    [SerializeField, Tooltip("약공격 최대 콤보 수")]
    private int maxLightComboCount = 3;
    [SerializeField, Tooltip("공격 파라미터를 유지하는 시간")]
    private float attackParameterResetDelay = 1.1f;
    [SerializeField, Tooltip("공중 공격 입력후 일반 이동 방지 시간")]
    private float airAttackLockDuration = 0.75f;

    private PlayerInputReader inputReader;
    private PlayerMovement playerMovement;
    private PlayerState currentState;
    private AttackType currentAttackType = AttackType.None;
    private int currentComboIndex;
    private float attackParameterResetTimer;

    private float attackLockTimer;
    /// <summary>
    /// 공격중 이동 방지
    /// </summary>
    public PlayerState CurrentState => currentState;
    // 외부에서 읽기 전용
    public AttackType CurrentAttackType => currentAttackType;
    public int CurrentComboIndex => currentComboIndex;
    public bool IsAttackLocked => attackLockTimer > 0f;

    public bool AttackStartedThisFrame {  get; private set; }
    public bool HeavyAttackStartedThisFrame { get; private set; }

    public bool DodgeStartedThisFrame { get; private set; }
    public bool DodgeCounterStartedThisFrame { get; private set; }
    public DodgeType StartedDodgeTypeThisFrame { get; private set; }


    private void Awake()
    {
        inputReader = GetComponent<PlayerInputReader>();
        playerMovement = GetComponent<PlayerMovement>();

        ChangeState(PlayerState.Idle);
    }

    private void Update()
    {
        inputReader.ReadInput();

        ResetFrameActionRequests();
        UpdateActionTimers();

        TryStartDodgeCounter();
        TryStartAttack();
        TryStartDodge();

        UpdateMovement();
        UpdateStateForTest();
        UpdateUtilityInputForTest();
    }


    private void ResetFrameActionRequests()
    {
        AttackStartedThisFrame = false;
        HeavyAttackStartedThisFrame = false;

        DodgeStartedThisFrame = false;
        DodgeCounterStartedThisFrame = false;
        StartedDodgeTypeThisFrame = DodgeType.None;
    }
    private void TryStartAttack()
    {
        bool lightAttackPressed = inputReader.LightAttackPressed;
        bool heavyAttackPressed = inputReader.HeavyAttackPressed;
        bool attackPressed = lightAttackPressed || heavyAttackPressed;

        if (!attackPressed)
        {
            return;
        }

        if (playerMovement.IsDodging)
        {
            return;
        }

        if (IsAttackLocked)
        {
            TryQueueLightCombo(lightAttackPressed);
            return;
        }

        if (CanQueueLightComboAfterLock(lightAttackPressed))
        {
            TryQueueLightCombo(lightAttackPressed);
            return;
        }

        AttackType nextAttackType = DecideAttackType(lightAttackPressed, heavyAttackPressed);

        StartAttack(nextAttackType, 1, heavyAttackPressed);
    }

    private bool CanQueueLightComboAfterLock(bool lightAttackPressed)
    {
        if (!lightAttackPressed)
        {
            return false;
        }

        if (currentAttackType != AttackType.Light)
        {
            return false;
        }

        if (currentComboIndex <= 0)
        {
            return false;
        }

        if (currentComboIndex >= maxLightComboCount)
        {
            return false;
        }

        if (attackParameterResetTimer <= 0f)
        {
            return false;
        }

        return true;
    }

    private void StartAttack(AttackType attackType, int comboIndex, bool isHeavyAttack)
    {
        if (attackType == AttackType.Air)
        {
            if (!playerMovement.TryStartAirAttack())
            {
                return;
            }
        }

        currentAttackType = attackType;
        currentComboIndex = comboIndex;

        attackParameterResetTimer = attackParameterResetDelay;

        attackLockTimer = GetAttackLockDuration(attackType, isHeavyAttack);

        AttackStartedThisFrame = true;
        HeavyAttackStartedThisFrame = attackType == AttackType.Heavy;

        ChangeState(PlayerState.Attack);
    }


    private void TryStartDodge()
    {
        if (IsAttackLocked)
        {
            return;
        }
        if (!inputReader.DodgePressed)
        {
            return;
        }

        if (playerMovement.TryStartDodge(inputReader.MoveInput, inputReader.BufferedSideInput))
        {
            DodgeStartedThisFrame = true;
            StartedDodgeTypeThisFrame = playerMovement.CurrentDodgeType;

            ChangeState(PlayerState.Dodge);
        }
    }

    private void TryStartDodgeCounter()
    {
        bool attackPressed = inputReader.LightAttackPressed || inputReader.HeavyAttackPressed;

        if (!attackPressed)
        {
            return;
        }

        if (!playerMovement.TryStartDodgeFollowUp(DodgeType.ForwardCounterThrust))
        {
            return;
        }

        DodgeCounterStartedThisFrame = true;
        StartedDodgeTypeThisFrame = DodgeType.ForwardCounterThrust;

        ChangeState(PlayerState.Dodge);
    }

    private void UpdateActionTimers()
    {
        UpdateAttackLockTimer();
        UpdateAttackParameterResetTimer();
    }

    private void UpdateAttackLockTimer()
    {
        if (attackLockTimer <= 0f)
        {
            attackLockTimer = 0f;
            return;
        }

        attackLockTimer -= Time.deltaTime;
    }

    private void UpdateAttackParameterResetTimer()
    {
        if (attackParameterResetTimer <= 0f)
        {
            ClearAttackParameters();
            return;
        }

        attackParameterResetTimer -= Time.deltaTime;
    }

    private void ClearAttackParameters()
    {
        currentAttackType = AttackType.None;
        currentComboIndex = 0;
        attackParameterResetTimer = 0f;
    }

    private void UpdateMovement()
    {
        if (!CanUseInputMovement())
        {
            playerMovement.Move(Vector2.zero, false, false);
            return;
        }
        playerMovement.Move(inputReader.MoveInput, inputReader.SprintHeld, inputReader.JumpPressed);
    }

    private void UpdateStateForTest()
    {
        if (currentState == PlayerState.Dead)
        {
            return;
        }

        if (playerMovement.IsDodging)
        {
            ChangeState(PlayerState.Dodge);
            return;
        }

        if (IsAttackLocked)
        {
            ChangeState(PlayerState.Attack);
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

        if (playerMovement.IsRising)
        {
            ChangeState(PlayerState.Jump);
            return;
        }

        if (playerMovement.IsFalling)
        {
             ChangeState(PlayerState.Fall);
            return;
        }

        if (inputReader.MoveInput.sqrMagnitude > 0.01f)
        {
            ChangeState(PlayerState.Move);
            return;
        }

        ChangeState(PlayerState.Idle);
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
        if (IsAttackLocked)
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

    private AttackType DecideAttackType(bool lightAttackPressed, bool heavyAttackPressed)
    {
        if (!playerMovement.IsGrounded)
        {
            return AttackType.Air;
        }

        if (heavyAttackPressed)
        {
            return AttackType.Heavy;
        }

        return AttackType.Light;
    }

    private float GetAttackLockDuration(AttackType attackType, bool isHeavyAttack)
    {
        switch (attackType)
        {
            case AttackType.Air:
                return airAttackLockDuration;

            case AttackType.Heavy:
                return heavyAttackLockDuration;

            case AttackType.Light:
            default:
                return lightAttackLockDuration;
        }
    }

    private void TryQueueLightCombo(bool lightAttackPressed)
    {
        if (!lightAttackPressed)
        {
            return;
        }

        if (currentAttackType != AttackType.Light)
        {
            return;
        }

        if (currentComboIndex >= maxLightComboCount)
        {
            return;
        }

        currentComboIndex++;
        attackParameterResetTimer = attackParameterResetDelay;

        attackLockTimer = Mathf.Max(attackLockTimer, lightAttackLockDuration);

        if (showDebugLog)
        {
            Debug.Log($"Light Combo Queued : {currentComboIndex}");
        }
    }

}

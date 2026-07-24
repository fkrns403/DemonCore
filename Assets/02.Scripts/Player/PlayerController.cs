using State;
using UnityEngine;

/// <summary>
/// 플레이어 중심 제어 클래스입니다.
/// PlayerInputReader의 입력을 읽고, 공격/회피/가드/패링/와이어/락온 상태를 연결합니다.
/// 실제 이동은 PlayerMovement가 담당하고,
/// 실제 애니메이션 파라미터 전달은 PlayerAnimatorController가 담당합니다.
/// </summary>
[DefaultExecutionOrder(-100)]
[RequireComponent(typeof(PlayerInputReader))]
[RequireComponent(typeof(PlayerMovement))]
public class PlayerController : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField, Tooltip("true면 상태 변경 시 로그를 출력합니다.")]
    private bool showDebugLog = true;

    [Header("References")]
    [SerializeField] private PlayerDefense playerDefense;
    [SerializeField] private PlayerWireController playerWireController;
    [SerializeField] private PlayerLockOnController lockOnController;
    [SerializeField] private PlayerAnchorInstaller anchorInstaller;

    [Header("Action Lock")]
    [SerializeField, Tooltip("약공격 입력 후 공격 상태를 유지하는 시간입니다.")]
    private float lightAttackLockDuration = 0.65f;

    [SerializeField, Tooltip("강공격 입력 후 공격 상태를 유지하는 시간입니다.")]
    private float heavyAttackLockDuration = 0.55f;

    [SerializeField, Tooltip("공중 공격 입력 후 공격 상태를 유지하는 시간입니다.")]
    private float airAttackLockDuration = 0.75f;

    [SerializeField, Tooltip("패링 성공 후 카운터 모션의 락 시간입니다.")]
    private float parryCounterLockDuration = 0.65f;

    [Header("Attack Combo")]
    [SerializeField, Tooltip("약공격 최대 콤보 수입니다.")]
    private int maxLightComboCount = 3;

    [SerializeField, Tooltip("공격 파라미터를 유지하는 시간입니다.")]
    private float attackParameterResetDelay = 1.1f;

    [Header("Attack Movement")]
    [SerializeField, Tooltip("공격 중에도 약한 이동을 허용할지 여부입니다.")]
    private bool allowMoveDuringAttack = true;

    [SerializeField, Range(0f, 1f), Tooltip("공격 중 이동 입력 배율입니다.")]
    private float attackMoveInputMultiplier = 0.45f;

    private PlayerInputReader inputReader;
    private PlayerMovement playerMovement;

    private PlayerState currentState;
    private AttackType currentAttackType = AttackType.None;
    private int currentComboIndex;

    private float attackParameterResetTimer;
    private float attackLockTimer;

    public PlayerState CurrentState => currentState;
    public AttackType CurrentAttackType => currentAttackType;
    public int CurrentComboIndex => currentComboIndex;
    public bool IsAttackLocked => attackLockTimer > 0f;

    public bool AttackStartedThisFrame { get; private set; }
    public bool HeavyAttackStartedThisFrame { get; private set; }

    public bool DodgeStartedThisFrame { get; private set; }
    public bool DodgeCounterStartedThisFrame { get; private set; }
    public DodgeType StartedDodgeTypeThisFrame { get; private set; }

    public bool ParryCounterStartedThisFrame { get; private set; }

    private void Awake()
    {
        inputReader = GetComponent<PlayerInputReader>();
        playerMovement = GetComponent<PlayerMovement>();

        if (playerDefense == null)
        {
            playerDefense = GetComponent<PlayerDefense>();
        }

        if (playerWireController == null)
        {
            playerWireController = GetComponent<PlayerWireController>();
        }

        if (lockOnController == null)
        {
            lockOnController = GetComponent<PlayerLockOnController>();
        }

        if (anchorInstaller == null)
        {
            anchorInstaller = GetComponent<PlayerAnchorInstaller>();
        }

        ChangeState(PlayerState.Idle);
    }

    private void Update()
    {
        inputReader.ReadInput();

        ResetFrameActionRequests();
        UpdateActionTimers();

        UpdateLockOn();
        UpdateDefense();
        UpdateWire();
        UpdateAnchorInstall();

        TryStartParryCounter();
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

        ParryCounterStartedThisFrame = false;
    }

    private void UpdateActionTimers()
    {
        UpdateAttackLockTimer();
        UpdateAttackParameterResetTimer();
    }

    private void UpdateLockOn()
    {
        if (lockOnController == null)
        {
            playerMovement.SetLockOnTarget(null);
            return;
        }

        lockOnController.TickLockOn();
        playerMovement.SetLockOnTarget(lockOnController.CurrentTargetTransform);
    }

    private void UpdateDefense()
    {
        if (playerDefense == null)
        {
            return;
        }

        playerDefense.TickDefense(
            inputReader.GuardPressed,
            inputReader.GuardHeld,
            inputReader.LightAttackPressed && !playerMovement.IsDodging
        );
    }

    private void UpdateWire()
    {
        if (playerWireController == null)
        {
            return;
        }

        playerWireController.TickWire(CanStartWireAction());
    }

    private bool CanStartWireAction()
    {
        if (currentState == PlayerState.Dead ||
            currentState == PlayerState.Knockdown ||
            currentState == PlayerState.GetUp)
        {
            return false;
        }

        if (IsAttackLocked ||
            playerMovement.IsDodging ||
            playerMovement.IsAirAttacking ||
            playerMovement.IsWireMoving ||
            (playerWireController != null && playerWireController.HasActiveSuppressionLink))
        {
            return false;
        }

        if (anchorInstaller != null && anchorInstaller.IsInstalling)
        {
            return false;
        }

        if (playerDefense != null &&
            (playerDefense.IsGuarding || playerDefense.IsParryActive))
        {
            return false;
        }

        return true;
    }

    private void UpdateAnchorInstall()
    {
        anchorInstaller?.TickInstall();
    }

    private void TryStartParryCounter()
    {
        if (playerMovement.IsDodging)
        {
            return;
        }

        if (playerDefense == null)
        {
            return;
        }

        if (!playerDefense.CounterStartedThisFrame)
        {
            return;
        }

        ParryCounterStartedThisFrame = true;
        attackLockTimer = parryCounterLockDuration;
        attackParameterResetTimer = attackParameterResetDelay;

        currentAttackType = AttackType.None;
        currentComboIndex = 0;

        ChangeState(PlayerState.Attack);
    }

    private void TryStartAttack()
    {
        if (playerDefense != null && playerDefense.CounterStartedThisFrame)
        {
            return;
        }

        bool lightAttackPressed = inputReader.LightAttackPressed;
        bool heavyAttackPressed = inputReader.HeavyAttackPressed;
        bool attackPressed = lightAttackPressed || heavyAttackPressed;

        if (!attackPressed)
        {
            return;
        }

        if (playerMovement.IsDodging || playerMovement.IsWireMoving)
        {
            return;
        }

        if (anchorInstaller != null && anchorInstaller.IsInstalling)
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

    private void TryStartDodge()
    {
        if (anchorInstaller != null && anchorInstaller.IsInstalling)
        {
            return;
        }

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

        Vector2 moveInput = inputReader.MoveInput;
        bool sprintHeld = inputReader.SprintHeld;
        bool jumpPressed = inputReader.JumpPressed;

        if (IsAttackLocked)
        {
            moveInput *= attackMoveInputMultiplier;
            sprintHeld = false;
            jumpPressed = false;
        }

        playerMovement.Move(
            moveInput,
            sprintHeld,
            jumpPressed,
            inputReader.WireFastDescendHeld
        );
    }

    private void UpdateStateForTest()
    {
        if (currentState == PlayerState.Dead)
        {
            return;
        }

        if (playerMovement.IsWireMoving)
        {
            ChangeState(PlayerState.WireMove);
            return;
        }

        if (playerMovement.IsDodging)
        {
            ChangeState(PlayerState.Dodge);
            return;
        }

        if (playerDefense != null && playerDefense.IsParryActive)
        {
            ChangeState(PlayerState.Parry);
            return;
        }

        if (playerDefense != null && playerDefense.IsGuarding)
        {
            ChangeState(PlayerState.Guard);
            return;
        }

        if (playerMovement.IsAirAttacking)
        {
            ChangeState(PlayerState.Attack);
            return;
        }

        if (IsAttackLocked)
        {
            ChangeState(PlayerState.Attack);
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

        if (playerMovement.IsDodging)
        {
            return false;
        }

        if (playerMovement.IsAirAttacking)
        {
            return false;
        }

        if (anchorInstaller != null && anchorInstaller.IsInstalling)
        {
            return false;
        }

        if (IsAttackLocked && !allowMoveDuringAttack)
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
            Debug.Log($"Player State changed : {previousState} -> {currentState}");
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

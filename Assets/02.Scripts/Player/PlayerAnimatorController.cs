using State;
using UnityEngine;

/// <summary>
/// PlayerController, PlayerMovement, PlayerDefense에서 결정한 상태를 Animator 파라미터로 전달합니다.
/// 공격 판단은 하지 않고, 애니메이션 파라미터 전달과 무기 표시 상태만 담당합니다.
/// </summary>
[DefaultExecutionOrder(-50)]
[RequireComponent(typeof(PlayerInputReader))]
[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(PlayerMovement))]
public class PlayerAnimatorController : MonoBehaviour
{
    [Header("References")]
    [SerializeField, Tooltip("캐릭터 모델 오브젝트에 붙어 있는 Animator입니다.")]
    private Animator animator;

    [SerializeField] private PlayerDefense playerDefense;
    [SerializeField] private PlayerWireController playerWireController;
    [SerializeField] private PlayerWeaponVisibilityController weaponVisibilityController;

    [Header("Animation Settings")]
    [SerializeField, Tooltip("Speed 파라미터가 부드럽게 변하는 시간입니다.")]
    private float speedDampTime = 0.08f;

    [SerializeField, Tooltip("공격/가드/회피 후 무기 준비 자세를 유지하는 시간입니다.")]
    private float weaponReadyDuration = 4.0f;

    [SerializeField, Tooltip("회피 후에도 무기 준비 자세를 유지할지 여부입니다.")]
    private bool keepWeaponReadyAfterDodge = true;

    [SerializeField, Tooltip("와이어 이동 중에도 무기를 보이게 할지 여부입니다.")]
    private bool showWeaponDuringWireMove = false;

    private PlayerInputReader inputReader;
    private PlayerController playerController;
    private PlayerMovement playerMovement;

    private float weaponReadyTimer;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int MoveXHash = Animator.StringToHash("MoveX");
    private static readonly int MoveYHash = Animator.StringToHash("MoveY");
    private static readonly int IsLockedOnHash = Animator.StringToHash("IsLockedOn");

    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
    private static readonly int IsFallingHash = Animator.StringToHash("IsFalling");
    private static readonly int VerticalSpeedHash = Animator.StringToHash("VerticalSpeed");
    private static readonly int JumpTriggerHash = Animator.StringToHash("JumpTrigger");
    private static readonly int LandTriggerHash = Animator.StringToHash("LandTrigger");

    private static readonly int IsWeaponReadyHash = Animator.StringToHash("IsWeaponReady");
    private static readonly int WeaponReadyTriggerHash = Animator.StringToHash("WeaponReadyTrigger");

    private static readonly int AttackTriggerHash = Animator.StringToHash("AttackTrigger");
    private static readonly int AttackTypeHash = Animator.StringToHash("AttackType");
    private static readonly int ComboIndexHash = Animator.StringToHash("ComboIndex");

    private static readonly int DodgeTriggerHash = Animator.StringToHash("DodgeTrigger");
    private static readonly int IsDodgingHash = Animator.StringToHash("IsDodging");
    private static readonly int DodgeTypeHash = Animator.StringToHash("DodgeType");
    private static readonly int DodgeFollowUpTriggerHash = Animator.StringToHash("DodgeFollowUpTrigger");

    private static readonly int IsGuardingHash = Animator.StringToHash("IsGuarding");
    private static readonly int ParryTriggerHash = Animator.StringToHash("ParryTrigger");
    private static readonly int ParrySuccessTriggerHash = Animator.StringToHash("ParrySuccessTrigger");
    private static readonly int CounterTriggerHash = Animator.StringToHash("CounterTrigger");

    private static readonly int IsWireMovingHash = Animator.StringToHash("IsWireMoving");
    private static readonly int WireTriggerHash = Animator.StringToHash("WireTrigger");

    public bool IsWeaponReady => weaponReadyTimer > 0f;

    private void Awake()
    {
        inputReader = GetComponent<PlayerInputReader>();
        playerController = GetComponent<PlayerController>();
        playerMovement = GetComponent<PlayerMovement>();

        if (playerDefense == null)
        {
            playerDefense = GetComponent<PlayerDefense>();
        }

        if (playerWireController == null)
        {
            playerWireController = GetComponent<PlayerWireController>();
        }

        if (weaponVisibilityController == null)
        {
            weaponVisibilityController = GetComponentInChildren<PlayerWeaponVisibilityController>();
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    private void Start()
    {
        if (weaponVisibilityController != null)
        {
            weaponVisibilityController.SetWeaponVisible(false, true);
        }
    }

    private void Update()
    {
        if (animator == null)
        {
            return;
        }

        UpdateMovementAnimation();
        UpdateJumpFallAnimation();
        UpdateAttackAnimatorParameter();
        UpdateWeaponReadyTimer();

        UpdateAttackAnimation();
        UpdateDodgeAnimation();
        UpdateDodgeCounterAnimation();
        UpdateDefenseAnimation();
        UpdateWireAnimation();

        UpdateWeaponReadyParameter();
        UpdateWeaponVisibility();
    }

    /// <summary>
    /// 실제 행동 우선순위에 맞춰 이동 Blend Tree 파라미터를 갱신합니다.
    /// 회피, 와이어, 공격처럼 전신 행동이 실행 중이면 이동 파라미터를 0으로 고정합니다.
    /// </summary>
    private void UpdateMovementAnimation()
    {
        bool suppressLocomotion =
            playerMovement.IsDodging ||
            playerMovement.IsWireMoving ||
            playerController.IsAttackLocked;

        if (suppressLocomotion)
        {
            animator.SetFloat(
                SpeedHash,
                0f,
                speedDampTime,
                Time.deltaTime
            );

            animator.SetFloat(
                MoveXHash,
                0f,
                speedDampTime,
                Time.deltaTime
            );

            animator.SetFloat(
                MoveYHash,
                0f,
                speedDampTime,
                Time.deltaTime
            );

            // 이동은 막아도 락온 자체는 유지합니다.
            animator.SetBool(
                IsLockedOnHash,
                playerMovement.IsLockedOn
            );

            return;
        }

        float speedValue = 0f;

        if (inputReader.MoveInput.sqrMagnitude > 0.01f)
        {
            speedValue = inputReader.SprintHeld ? 1f : 0.5f;
        }

        float moveX;
        float moveY;

        if (playerMovement.IsLockedOn)
        {
            moveX = inputReader.MoveInput.x;
            moveY = inputReader.MoveInput.y;
        }
        else
        {
            moveX = 0f;
            moveY = speedValue > 0.01f ? 1f : 0f;
        }

        animator.SetFloat(
            SpeedHash,
            speedValue,
            speedDampTime,
            Time.deltaTime
        );

        animator.SetFloat(
            MoveXHash,
            moveX,
            speedDampTime,
            Time.deltaTime
        );

        animator.SetFloat(
            MoveYHash,
            moveY,
            speedDampTime,
            Time.deltaTime
        );

        animator.SetBool(
            IsLockedOnHash,
            playerMovement.IsLockedOn
        );
    }

    private void UpdateJumpFallAnimation()
    {
        bool isDodging = playerMovement.IsDodging;
        bool suppressAirTransition = isDodging || playerMovement.IsWireMoving;

        // 회피 Root Motion 중 CharacterController.isGrounded가 한 프레임 흔들려도
        // Air/Fall 상태가 회피를 중간에 끊지 않도록 행동 우선순위를 Animator에도 전달합니다.
        animator.SetBool(IsDodgingHash, isDodging);
        animator.SetBool(IsGroundedHash, suppressAirTransition || playerMovement.IsGrounded);
        animator.SetBool(IsFallingHash, !suppressAirTransition && playerMovement.IsFalling);
        animator.SetFloat(VerticalSpeedHash, playerMovement.VerticalVelocity);

        if (playerMovement.JumpStartedThisFrame)
        {
            animator.SetTrigger(JumpTriggerHash);
        }

        if (playerMovement.LandedThisFrame && !suppressAirTransition)
        {
            animator.SetTrigger(LandTriggerHash);
        }
    }

    private void UpdateWeaponReadyTimer()
    {
        if (weaponReadyTimer <= 0f)
        {
            weaponReadyTimer = 0f;
            return;
        }

        weaponReadyTimer -= Time.deltaTime;
    }

    private void KeepWeaponReady()
    {
        weaponReadyTimer = weaponReadyDuration;
    }

    /// <summary>
    /// 비전투 상태에서 처음 공격/가드/패링을 했을 때 Base Layer를 전투 준비 서브트리로 보내기 위한 Trigger입니다.
    /// 이미 준비 상태여도 Trigger를 다시 쏴도 큰 문제는 없지만, 불필요한 재진입을 줄이기 위해 처음 진입 시점에만 호출합니다.
    /// </summary>
    private void RequestWeaponReadyEnterIfNeeded(bool wasWeaponReady)
    {
        if (!wasWeaponReady)
        {
            animator.SetTrigger(WeaponReadyTriggerHash);
        }
    }

    private void UpdateAttackAnimatorParameter()
    {
        animator.SetInteger(AttackTypeHash, (int)playerController.CurrentAttackType);
        animator.SetInteger(ComboIndexHash, playerController.CurrentComboIndex);
    }

    private void UpdateAttackAnimation()
    {
        if (!playerController.AttackStartedThisFrame)
        {
            return;
        }

        bool wasWeaponReady = IsWeaponReady;
        KeepWeaponReady();
        RequestWeaponReadyEnterIfNeeded(wasWeaponReady);

        // 기존 코드는 첫 공격 입력 때 WeaponReadyTrigger만 보내서 AttackTrigger가 실행되지 않는 문제가 있었습니다.
        // 이제 첫 공격도 무기 표시/발도 준비와 동시에 UpperBodyCombat 공격 레이어를 정상 재생합니다.
        animator.SetTrigger(AttackTriggerHash);

        Debug.Log(
            $"Attack Animation Request : " +
            $"{playerController.CurrentAttackType} / Combo {playerController.CurrentComboIndex}"
        );
    }

    private void UpdateDodgeAnimation()
    {
        if (!playerController.DodgeStartedThisFrame)
        {
            return;
        }

        if (keepWeaponReadyAfterDodge && IsWeaponReady)
        {
            KeepWeaponReady();
        }

        DodgeType dodgeType = playerController.StartedDodgeTypeThisFrame;

        Debug.Log($"Dodge Animation Request : {dodgeType} / {(int)dodgeType}");

        animator.SetInteger(DodgeTypeHash, (int)dodgeType);
        animator.SetTrigger(DodgeTriggerHash);
    }

    private void UpdateDefenseAnimation()
    {
        if (playerDefense == null)
        {
            return;
        }

        animator.SetBool(IsGuardingHash, playerDefense.IsGuarding);

        bool defenseRequested =
            playerDefense.IsGuarding ||
            playerDefense.IsParryActive ||
            playerDefense.ParryStartedThisFrame ||
            playerDefense.ParrySucceededThisFrame;

        if (defenseRequested)
        {
            bool wasWeaponReady = IsWeaponReady;
            KeepWeaponReady();
            RequestWeaponReadyEnterIfNeeded(wasWeaponReady);
        }

        if (playerDefense.ParryStartedThisFrame)
        {
            animator.SetTrigger(ParryTriggerHash);
        }

        if (playerDefense.ParrySucceededThisFrame)
        {
            animator.SetTrigger(ParrySuccessTriggerHash);
        }

        if (playerDefense.CounterStartedThisFrame || playerController.ParryCounterStartedThisFrame)
        {
            KeepWeaponReady();
            animator.SetTrigger(CounterTriggerHash);
        }
    }

    private void UpdateWireAnimation()
    {
        animator.SetBool(IsWireMovingHash, playerMovement.IsWireMoving);

        if (playerWireController != null && playerWireController.WireStartedThisFrame)
        {
            animator.SetTrigger(WireTriggerHash);

            if (showWeaponDuringWireMove)
            {
                KeepWeaponReady();
            }
        }
    }

    private void UpdateDodgeCounterAnimation()
    {
        if (!playerController.DodgeCounterStartedThisFrame)
        {
            return;
        }

        KeepWeaponReady();

        DodgeType dodgeType = playerController.StartedDodgeTypeThisFrame;

        Debug.Log($"Dodge Counter Request : {dodgeType} / {(int)dodgeType}");

        animator.SetInteger(DodgeTypeHash, (int)dodgeType);
        animator.SetTrigger(DodgeFollowUpTriggerHash);
    }

    private void UpdateWeaponReadyParameter()
    {
        animator.SetBool(IsWeaponReadyHash, IsWeaponReady);
    }

    private void UpdateWeaponVisibility()
    {
        if (weaponVisibilityController == null)
        {
            return;
        }

        bool shouldShowWeapon =
            IsWeaponReady ||
            playerController.IsAttackLocked ||
            (playerMovement.IsDodging && IsWeaponReady) ||
            (playerDefense != null && (playerDefense.IsGuarding || playerDefense.IsParryActive));

        weaponVisibilityController.SetWeaponVisible(shouldShowWeapon);
    }
}

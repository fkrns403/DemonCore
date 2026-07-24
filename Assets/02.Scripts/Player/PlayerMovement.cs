using System;
using State;
using UnityEngine;

/// <summary>
/// 플레이어의 실제 위치와 회전을 변경하는 Motor입니다.
/// 입력 해석은 PlayerController, 애니메이션 파라미터는 PlayerAnimatorController,
/// 와이어 목표 탐색은 PlayerWireController가 담당하고 이 클래스는 최종 이동만 실행합니다.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 4f;
    [SerializeField] private float sprintSpeed = 6.5f;
    [SerializeField] private float rotationSpeed = 720f;

    [Header("Jump / Gravity")]
    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float groundedGravity = -2f;

    [Header("Dodge Rules")]
    [SerializeField, Tooltip("회피 1회 스태미나 비용입니다.")]
    private float dodgeStaminaCost = 15f;

    [SerializeField, Tooltip("다음 회피를 허용하기 전 최소 대기시간입니다.")]
    private float dodgeCooldown = 0.35f;

    [SerializeField, Tooltip("애니메이션 이벤트를 아직 배치하지 않았을 때 사용할 기본 무적 시간입니다.")]
    private float defaultDodgeInvulnerabilityDuration = 0.18f;

    [SerializeField, Tooltip("ON이면 클립 마지막 Animation Event가 회피를 종료하고, 타이머는 비정상 상태 방지용으로만 사용합니다.")]
    private bool useAnimationEventDodgeEnd = true;

    [SerializeField, Min(0.1f), Tooltip("Animation Event가 누락되었을 때 회피 상태를 강제로 해제하는 최대 안전 시간입니다.")]
    private float dodgeSafetyTimeout = 2f;

    [SerializeField, Tooltip("ON이면 백스텝 클립에 배치한 Open/Close 이벤트 사이에서만 전진 카운터를 허용합니다.")]
    private bool requireAnimationEventFollowUpWindow;

    [SerializeField, Tooltip("이벤트를 사용하지 않을 때 백스텝 시작 후 전진 카운터를 허용하는 최소 시간입니다.")]
    private float fallbackFollowUpWindowStart = 0.08f;

    [SerializeField, Tooltip("이벤트를 사용하지 않을 때 백스텝 시작 후 전진 카운터를 허용하는 최대 시간입니다.")]
    private float fallbackFollowUpWindowEnd = 0.42f;

    [Header("Dodge - Code Fallback")]
    [SerializeField] private float backstepSpeed = 10f;
    [SerializeField] private float backstepFallbackDuration = 0.65f;
    [SerializeField] private float sideBackstepSpeed = 8f;
    [SerializeField] private float sideBackstepFallbackDuration = 0.65f;
    [SerializeField] private float forwardCounterFallbackDuration = 1.1f;
    [SerializeField] private float disengageSpeed = 11f;
    [SerializeField] private float disengageFallbackDuration = 0.65f;

    [Header("Dodge Root Motion")]
    [SerializeField, Tooltip("회피 클립의 Root Motion 이동값을 CharacterController에 적용합니다.")]
    private bool useDodgeRootMotion = true;

    [SerializeField, Tooltip("회피 클립의 Y축 Root Rotation을 적용합니다.")]
    private bool useDodgeRootRotation = true;

    [SerializeField, Tooltip("전진 카운터 클립의 이동량은 유지하되 방향을 현재 플레이어 정면으로 정렬합니다.")]
    private bool alignForwardCounterRootMotionToFacing = true;

    [SerializeField] private float backstepRootMotionMultiplier = 1f;
    [SerializeField] private float sideBackstepRootMotionMultiplier = 1.25f;
    [SerializeField] private float forwardCounterRootMotionMultiplier = 1f;
    [SerializeField] private float disengageRootMotionMultiplier = 1f;

    [SerializeField, Tooltip("전진 카운터 시작 후 이 시간 동안 수평 Root Motion이 0이면 경고를 출력합니다.")]
    private float rootMotionDiagnosticDelay = 0.2f;

    [Header("References")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private PlayerStamina playerStamina;
    [SerializeField] private PlayerInvulnerability playerInvulnerability;

    [Header("Air Attack")]
    [SerializeField] private float airAttackDuration = 0.75f;
    [SerializeField] private float airAttackGravityMultiplier = 0.2f;
    [SerializeField] private float airAttackStartVerticalVelocity = -0.5f;
    [SerializeField] private bool allowAirAttackHorizontalControl;

    [Header("Wire - Legacy Fallback")]
    [SerializeField] private float legacyWireMoveSpeed = 18f;
    [SerializeField] private float legacyWireStopDistance = 1.2f;
    [SerializeField] private float wireRotationSpeed = 900f;

    [Header("Lock On")]
    [SerializeField] private float lockOnRotationSpeed = 900f;

    private CharacterController characterController;
    private float verticalVelocity;

    private bool isDodging;
    private float dodgeFallbackTimer;
    private float dodgeElapsedTime;
    private float dodgeCooldownTimer;
    private float currentDodgeSpeed;
    private Vector3 dodgeDirection;
    private DodgeType currentDodgeType;
    private bool dodgeFollowUpWindowOpen;
    private float horizontalRootMotionAccumulation;
    private bool rootMotionWarningPrinted;

    private bool isAirAttacking;
    private float airAttackTimer;

    private bool isWireMoving;
    private bool useLegacyWireRequest;
    private Vector3 legacyWireDestination;
    private WireMoveRequest currentWireRequest;
    private float currentWireSpeed;
    private float wireStartDelayTimer;
    private float wireTravelTimer;

    private Transform lockOnTarget;

    public event Action<WireActionType> WireMoveStarted;
    public event Action<WireActionType, WireMoveEndReason> WireMoveEnded;

    public bool IsGrounded { get; private set; }
    public bool IsAirAttacking => isAirAttacking;
    public bool IsDodging => isDodging;
    public bool IsWireMoving => isWireMoving;
    public bool IsLockedOn => lockOnTarget != null;
    public bool IsRising => !IsGrounded && verticalVelocity > 0f;
    public bool IsFalling => !IsGrounded && verticalVelocity <= 0f;
    public float VerticalVelocity => verticalVelocity;
    public DodgeType CurrentDodgeType => currentDodgeType;
    public bool CanAcceptDodgeFollowUp => CanUseDodgeFollowUpWindow();

    public bool JumpStartedThisFrame { get; private set; }
    public bool LandedThisFrame { get; private set; }

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (playerStamina == null)
        {
            playerStamina = GetComponent<PlayerStamina>();
        }

        if (playerInvulnerability == null)
        {
            playerInvulnerability = GetComponent<PlayerInvulnerability>();
        }
    }

    private void Update()
    {
        if (dodgeCooldownTimer > 0f)
        {
            dodgeCooldownTimer -= Time.deltaTime;
        }
    }

    /// <summary>
    /// PlayerController가 매 프레임 호출하는 이동 진입점입니다.
    /// </summary>
    public void Move(Vector2 moveInput, bool isSprinting, bool jumpPressed)
    {
        Move(moveInput, isSprinting, jumpPressed, false);
    }

    /// <summary>
    /// 로프 이동의 빠른 하강 입력까지 포함한 이동 진입점입니다.
    /// </summary>
    public void Move(
        Vector2 moveInput,
        bool isSprinting,
        bool jumpPressed,
        bool wireFastDescendHeld)
    {
        JumpStartedThisFrame = false;
        LandedThisFrame = false;

        if (cameraTransform == null)
        {
            Debug.LogWarning("PlayerMovement : Camera Transform이 지정되지 않았습니다.");
            return;
        }

        bool wasGrounded = IsGrounded;
        UpdateGroundedState();

        if (!wasGrounded && IsGrounded)
        {
            LandedThisFrame = true;
        }

        if (isWireMoving)
        {
            UpdateWireMove(moveInput, jumpPressed, wireFastDescendHeld);
            return;
        }

        if (isAirAttacking)
        {
            UpdateAirAttack(moveInput);
            return;
        }

        ApplyGravity();

        if (isDodging)
        {
            UpdateDodge();
            return;
        }

        ApplyJump(jumpPressed);

        Vector3 horizontalDirection = CalculateCameraRelativeDirection(moveInput);
        float currentSpeed = isSprinting ? sprintSpeed : walkSpeed;

        Vector3 velocity = horizontalDirection * currentSpeed;
        velocity.y = verticalVelocity;

        characterController.Move(velocity * Time.deltaTime);

        if (lockOnTarget != null)
        {
            RotateToLockOnTarget();
        }
        else if (horizontalDirection.sqrMagnitude > 0.01f)
        {
            RotateToMoveDirection(horizontalDirection);
        }
    }

    public void SetLockOnTarget(Transform target)
    {
        lockOnTarget = target;
    }

    public bool TryStartDodge(Vector2 moveInput, float bufferedSideInput)
    {
        UpdateGroundedState();

        if (isDodging || isWireMoving || !IsGrounded || dodgeCooldownTimer > 0f)
        {
            return false;
        }

        if (playerStamina != null && !playerStamina.TrySpend(dodgeStaminaCost))
        {
            return false;
        }

        currentDodgeType = DecideDodgeType(moveInput, bufferedSideInput);
        dodgeDirection = CalculateDodgeDirection(currentDodgeType);
        ApplyDodgeSetting(currentDodgeType);

        isDodging = true;
        dodgeElapsedTime = 0f;
        dodgeFollowUpWindowOpen = false;
        horizontalRootMotionAccumulation = 0f;
        rootMotionWarningPrinted = false;
        dodgeCooldownTimer = dodgeCooldown;

        BeginDodgeInvulnerability(defaultDodgeInvulnerabilityDuration);
        return true;
    }

    public bool TryStartDodgeFollowUp(DodgeType followUpType)
    {
        if (!isDodging || currentDodgeType != DodgeType.Backstep)
        {
            return false;
        }

        if (!CanUseDodgeFollowUpWindow())
        {
            return false;
        }

        if (followUpType != DodgeType.ForwardCounterThrust)
        {
            return false;
        }

        currentDodgeType = followUpType;
        dodgeDirection = Vector3.zero;
        currentDodgeSpeed = 0f;
        dodgeFallbackTimer = GetDodgeEndTimeout(forwardCounterFallbackDuration);
        dodgeElapsedTime = 0f;
        dodgeFollowUpWindowOpen = false;
        horizontalRootMotionAccumulation = 0f;
        rootMotionWarningPrinted = false;
        return true;
    }

    public void OpenDodgeFollowUpWindow()
    {
        if (isDodging && currentDodgeType == DodgeType.Backstep)
        {
            dodgeFollowUpWindowOpen = true;
        }
    }

    public void CloseDodgeFollowUpWindow()
    {
        dodgeFollowUpWindowOpen = false;
    }

    public void BeginDodgeInvulnerability(float duration)
    {
        playerInvulnerability?.Begin(duration);
    }

    public void EndDodgeInvulnerability()
    {
        playerInvulnerability?.End();
    }

    /// <summary>
    /// 회피 클립 마지막 프레임의 Animation Event에서 호출합니다.
    /// 고정 타이머가 루트모션을 중간에 잘라버리는 것을 막습니다.
    /// </summary>
    public void NotifyDodgeAnimationFinished()
    {
        if (isDodging)
        {
            EndDodge();
        }
    }

    /// <summary>
    /// 전환 중 이전 클립의 종료 이벤트가 뒤늦게 들어와 새 회피를 끊지 않도록
    /// 현재 회피 타입이 예상 타입과 같을 때만 종료합니다.
    /// </summary>
    public void NotifyDodgeAnimationFinished(DodgeType expectedType)
    {
        if (isDodging && currentDodgeType == expectedType)
        {
            EndDodge();
        }
    }

    /// <summary>
    /// 타입/프로필/도착점이 포함된 와이어 이동을 시작합니다.
    /// </summary>
    public bool TryStartWireMove(WireMoveRequest request)
    {
        if (isDodging || isAirAttacking || isWireMoving || request.Profile == null)
        {
            return false;
        }

        if (request.ActionType == WireActionType.Suppression)
        {
            return false;
        }

        useLegacyWireRequest = false;
        currentWireRequest = request;
        currentWireSpeed = 0f;
        wireStartDelayTimer = request.Profile.StartDelay;
        wireTravelTimer = request.Profile.MaximumTravelTime;
        verticalVelocity = 0f;
        isWireMoving = true;

        WireMoveStarted?.Invoke(request.ActionType);
        return true;
    }

    /// <summary>
    /// 기존 코드/프리팹과의 호환을 위한 단순 와이어 이동 API입니다.
    /// 신규 구현에서는 WireMoveRequest 사용을 권장합니다.
    /// </summary>
    public bool TryStartWireMove(Vector3 destination)
    {
        if (isDodging || isAirAttacking || isWireMoving)
        {
            return false;
        }

        useLegacyWireRequest = true;
        legacyWireDestination = destination;
        currentWireSpeed = legacyWireMoveSpeed;
        wireStartDelayTimer = 0f;
        wireTravelTimer = 3f;
        verticalVelocity = 0f;
        isWireMoving = true;

        WireMoveStarted?.Invoke(WireActionType.Traverse);
        return true;
    }

    public void CancelWireMove()
    {
        EndWireMove(WireMoveEndReason.Interrupted);
    }

    public bool TryStartAirAttack()
    {
        UpdateGroundedState();

        if (IsGrounded || isAirAttacking || isDodging || isWireMoving)
        {
            return false;
        }

        isAirAttacking = true;
        airAttackTimer = airAttackDuration;

        if (verticalVelocity < airAttackStartVerticalVelocity)
        {
            verticalVelocity = airAttackStartVerticalVelocity;
        }

        return true;
    }

    public void EndAirAttack()
    {
        isAirAttacking = false;
        airAttackTimer = 0f;
    }

    /// <summary>
    /// Animator 자식 오브젝트의 OnAnimatorMove에서 전달된 월드 공간 delta를 적용합니다.
    /// CharacterController.Move는 속도가 아니라 이번 프레임의 이동량을 받으므로 deltaTime을 다시 곱하지 않습니다.
    /// </summary>
    public void ApplyAnimationRootMotion(
        Vector3 animationDeltaPosition,
        Quaternion animationDeltaRotation)
    {
        if (!useDodgeRootMotion || !isDodging)
        {
            return;
        }

        ApplyRootMotionPosition(animationDeltaPosition);
        ApplyRootMotionRotation(animationDeltaRotation);
    }

    private DodgeType DecideDodgeType(Vector2 moveInput, float bufferedSideInput)
    {
        float sideInput = Mathf.Abs(moveInput.x) > 0.01f
            ? moveInput.x
            : bufferedSideInput;

        if (sideInput < -0.01f)
        {
            return DodgeType.SideBackstepLeft;
        }

        if (sideInput > 0.01f)
        {
            return DodgeType.SideBackstepRight;
        }

        if (moveInput.y < -0.1f)
        {
            return DodgeType.Disengage;
        }

        // 기획상 W+Shift를 전진 회피로 직접 사용할 경우 별도 DodgeType을 추가할 수 있습니다.
        // 현재 프로젝트의 ForwardCounterThrust는 백스텝 중 공격 입력으로 발동하는 후속기입니다.
        return DodgeType.Backstep;
    }

    private Vector3 CalculateDodgeDirection(DodgeType dodgeType)
    {
        Vector3 backDirection = -transform.forward;
        Vector3 rightDirection = transform.right;

        backDirection.y = 0f;
        rightDirection.y = 0f;
        backDirection.Normalize();
        rightDirection.Normalize();

        switch (dodgeType)
        {
            case DodgeType.SideBackstepLeft:
                return -rightDirection;
            case DodgeType.SideBackstepRight:
                return rightDirection;
            case DodgeType.Disengage:
            case DodgeType.Backstep:
            default:
                return backDirection;
        }
    }

    private void ApplyDodgeSetting(DodgeType dodgeType)
    {
        switch (dodgeType)
        {
            case DodgeType.SideBackstepLeft:
            case DodgeType.SideBackstepRight:
                currentDodgeSpeed = sideBackstepSpeed;
                dodgeFallbackTimer = GetDodgeEndTimeout(sideBackstepFallbackDuration);
                break;
            case DodgeType.Disengage:
                currentDodgeSpeed = disengageSpeed;
                dodgeFallbackTimer = GetDodgeEndTimeout(disengageFallbackDuration);
                break;
            case DodgeType.Backstep:
            default:
                currentDodgeSpeed = backstepSpeed;
                dodgeFallbackTimer = GetDodgeEndTimeout(backstepFallbackDuration);
                break;
        }
    }

    private float GetDodgeEndTimeout(float clipFallbackDuration)
    {
        return useAnimationEventDodgeEnd
            ? Mathf.Max(dodgeSafetyTimeout, clipFallbackDuration)
            : clipFallbackDuration;
    }

    private bool CanUseDodgeFollowUpWindow()
    {
        if (!isDodging || currentDodgeType != DodgeType.Backstep)
        {
            return false;
        }

        if (requireAnimationEventFollowUpWindow)
        {
            return dodgeFollowUpWindowOpen;
        }

        return dodgeElapsedTime >= fallbackFollowUpWindowStart &&
               dodgeElapsedTime <= fallbackFollowUpWindowEnd;
    }

    private void UpdateDodge()
    {
        dodgeElapsedTime += Time.deltaTime;
        dodgeFallbackTimer -= Time.deltaTime;

        if (!useDodgeRootMotion)
        {
            Vector3 velocity = dodgeDirection * currentDodgeSpeed;
            velocity.y = verticalVelocity;
            characterController.Move(velocity * Time.deltaTime);
        }
        // Root Motion 회피는 OnAnimatorMove에서 수평 이동과 수직 접지 이동을
        // 한 번의 CharacterController.Move 호출로 함께 적용합니다.
        // 여기서 별도로 수직 Move를 호출하면 뒤이어 실행되는 수평 Move가
        // isGrounded를 false로 덮어써 Fall 전환이 발생할 수 있습니다.

        if (lockOnTarget != null && currentDodgeType != DodgeType.ForwardCounterThrust)
        {
            RotateToLockOnTarget();
        }

        DiagnoseForwardCounterRootMotion();

        // Animation Event가 누락되거나 전환이 실패한 경우 상태가 영구 고정되지 않도록 하는 안전장치입니다.
        if (dodgeFallbackTimer <= 0f)
        {
            EndDodge();
        }
    }

    private void DiagnoseForwardCounterRootMotion()
    {
        if (rootMotionWarningPrinted || currentDodgeType != DodgeType.ForwardCounterThrust)
        {
            return;
        }

        if (dodgeElapsedTime < rootMotionDiagnosticDelay)
        {
            return;
        }

        if (horizontalRootMotionAccumulation <= 0.001f)
        {
            rootMotionWarningPrinted = true;
            Debug.LogWarning(
                "ForwardCounterThrust의 수평 Root Motion이 0입니다. " +
                "클립에 실제 Root Motion이 있는지, Root Transform Position(XZ) Bake Into Pose OFF, " +
                "Base Layer 전신 상태, DodgeFollowUpTrigger 전환 조건을 확인하세요."
            );
        }
    }

    private void EndDodge()
    {
        isDodging = false;
        currentDodgeType = DodgeType.None;
        dodgeFollowUpWindowOpen = false;
        dodgeFallbackTimer = 0f;
        dodgeElapsedTime = 0f;
        EndDodgeInvulnerability();
    }

    private void UpdateWireMove(
        Vector2 moveInput,
        bool releasePressed,
        bool fastDescendHeld)
    {
        if (releasePressed)
        {
            EndWireMove(WireMoveEndReason.Released);
            return;
        }

        wireTravelTimer -= Time.deltaTime;

        if (wireTravelTimer <= 0f)
        {
            EndWireMove(WireMoveEndReason.Timeout);
            return;
        }

        if (useLegacyWireRequest)
        {
            UpdateLegacyWireMove();
            return;
        }

        WireActionProfile profile = currentWireRequest.Profile;

        if (profile == null)
        {
            EndWireMove(WireMoveEndReason.InvalidTarget);
            return;
        }

        if (currentWireRequest.ActionType == WireActionType.Rope)
        {
            UpdateRopeMove(moveInput.y, fastDescendHeld, profile);
            return;
        }

        if (wireStartDelayTimer > 0f)
        {
            wireStartDelayTimer -= Time.deltaTime;
            RotateToWireDirection(currentWireRequest.AnchorPosition - transform.position);
            return;
        }

        Vector3 destination = currentWireRequest.Destination;
        Vector3 toDestination = destination - transform.position;
        float distance = toDestination.magnitude;

        if (distance <= profile.StopDistance)
        {
            EndWireMove(WireMoveEndReason.Completed);
            return;
        }

        Vector3 moveDirection = toDestination.normalized;
        float desiredSpeed = CalculateWireDesiredSpeed(distance, profile);
        currentWireSpeed = Mathf.MoveTowards(
            currentWireSpeed,
            desiredSpeed,
            profile.Acceleration * Time.deltaTime
        );

        CollisionFlags collisionFlags = characterController.Move(
            moveDirection * currentWireSpeed * Time.deltaTime
        );

        if ((collisionFlags & CollisionFlags.Sides) != 0 && distance > profile.StopDistance * 1.5f)
        {
            EndWireMove(WireMoveEndReason.Obstructed);
            return;
        }

        RotateToWireDirection(moveDirection);
    }

    private void UpdateLegacyWireMove()
    {
        Vector3 toDestination = legacyWireDestination - transform.position;
        float distance = toDestination.magnitude;

        if (distance <= legacyWireStopDistance)
        {
            EndWireMove(WireMoveEndReason.Completed);
            return;
        }

        Vector3 moveDirection = toDestination.normalized;
        CollisionFlags collisionFlags = characterController.Move(
            moveDirection * legacyWireMoveSpeed * Time.deltaTime
        );

        if ((collisionFlags & CollisionFlags.Sides) != 0)
        {
            EndWireMove(WireMoveEndReason.Obstructed);
            return;
        }

        RotateToWireDirection(moveDirection);
    }

    private void UpdateRopeMove(
        float verticalInput,
        bool fastDescendHeld,
        WireActionProfile profile)
    {
        Vector3 anchorPosition = currentWireRequest.AnchorPosition;
        Vector3 playerPosition = transform.position;

        Vector3 flatCorrection = anchorPosition - playerPosition;
        flatCorrection.y = 0f;

        Vector3 correctionVelocity = Vector3.zero;
        float clearance = profile.RopeWallClearance;

        if (flatCorrection.magnitude > clearance)
        {
            correctionVelocity = flatCorrection.normalized * profile.MoveSpeed;
        }

        float verticalSpeed = 0f;

        if (verticalInput > 0.1f)
        {
            verticalSpeed = profile.RopeAscendSpeed;
        }
        else if (verticalInput < -0.1f)
        {
            verticalSpeed = fastDescendHeld
                ? -profile.RopeFastDescendSpeed
                : -profile.RopeDescendSpeed;
        }

        Vector3 velocity = correctionVelocity + Vector3.up * verticalSpeed;
        characterController.Move(velocity * Time.deltaTime);

        if (verticalSpeed > 0f && transform.position.y >= currentWireRequest.RopeTopPosition.y)
        {
            EndWireMove(WireMoveEndReason.Completed);
        }
        else if (verticalSpeed < 0f && transform.position.y <= currentWireRequest.RopeBottomPosition.y)
        {
            EndWireMove(WireMoveEndReason.Completed);
        }

        RotateToWireDirection(anchorPosition - transform.position);
    }

    private float CalculateWireDesiredSpeed(float distance, WireActionProfile profile)
    {
        if (profile.DecelerationDistance <= 0f || distance >= profile.DecelerationDistance)
        {
            return profile.MoveSpeed;
        }

        float ratio = Mathf.Clamp01(distance / profile.DecelerationDistance);
        ratio = Mathf.Max(profile.MinimumDecelerationRatio, ratio);
        return profile.MoveSpeed * ratio;
    }

    private void EndWireMove(WireMoveEndReason reason)
    {
        if (!isWireMoving)
        {
            return;
        }

        WireActionType endedType = useLegacyWireRequest
            ? WireActionType.Traverse
            : currentWireRequest.ActionType;

        isWireMoving = false;
        useLegacyWireRequest = false;
        currentWireSpeed = 0f;
        wireStartDelayTimer = 0f;
        wireTravelTimer = 0f;
        verticalVelocity = groundedGravity;

        WireMoveEnded?.Invoke(endedType, reason);
    }

    private void UpdateAirAttack(Vector2 moveInput)
    {
        airAttackTimer -= Time.deltaTime;
        verticalVelocity += gravity * airAttackGravityMultiplier * Time.deltaTime;

        Vector3 horizontalDirection = allowAirAttackHorizontalControl
            ? CalculateCameraRelativeDirection(moveInput)
            : Vector3.zero;

        Vector3 velocity = horizontalDirection * walkSpeed;
        velocity.y = verticalVelocity;
        characterController.Move(velocity * Time.deltaTime);

        if (lockOnTarget != null)
        {
            RotateToLockOnTarget();
        }

        if ((characterController.isGrounded && verticalVelocity <= 0f) || airAttackTimer <= 0f)
        {
            EndAirAttack();
        }
    }

    private void UpdateGroundedState()
    {
        IsGrounded = characterController.isGrounded;

        if (IsGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = groundedGravity;
        }
    }

    private void ApplyJump(bool jumpPressed)
    {
        if (!jumpPressed || !IsGrounded)
        {
            return;
        }

        verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        JumpStartedThisFrame = true;
    }

    private void ApplyGravity()
    {
        verticalVelocity += gravity * Time.deltaTime;
    }

    private Vector3 CalculateCameraRelativeDirection(Vector2 moveInput)
    {
        Vector3 cameraForward = cameraTransform.forward;
        Vector3 cameraRight = cameraTransform.right;

        cameraForward.y = 0f;
        cameraRight.y = 0f;
        cameraForward.Normalize();
        cameraRight.Normalize();

        Vector3 moveDirection = cameraForward * moveInput.y + cameraRight * moveInput.x;

        if (moveDirection.sqrMagnitude > 1f)
        {
            moveDirection.Normalize();
        }

        return moveDirection;
    }

    private void RotateToMoveDirection(Vector3 moveDirection)
    {
        Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    private void RotateToLockOnTarget()
    {
        if (lockOnTarget == null)
        {
            return;
        }

        Vector3 direction = lockOnTarget.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            lockOnRotationSpeed * Time.deltaTime
        );
    }

    private void RotateToWireDirection(Vector3 moveDirection)
    {
        Vector3 flatDirection = moveDirection;
        flatDirection.y = 0f;

        if (flatDirection.sqrMagnitude <= 0.001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(flatDirection.normalized);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            wireRotationSpeed * Time.deltaTime
        );
    }

    private void ApplyRootMotionPosition(Vector3 animationDeltaPosition)
    {
        Vector3 horizontalDelta = animationDeltaPosition;
        horizontalDelta.y = 0f;

        if (currentDodgeType == DodgeType.ForwardCounterThrust &&
            alignForwardCounterRootMotionToFacing &&
            horizontalDelta.sqrMagnitude > 0.000001f)
        {
            float magnitude = horizontalDelta.magnitude;
            horizontalDelta = transform.forward * magnitude;
        }

        horizontalDelta *= GetCurrentDodgeRootMotionMultiplier();
        horizontalRootMotionAccumulation += horizontalDelta.magnitude;

        // CharacterController.isGrounded는 마지막 Move 결과에 영향을 받습니다.
        // 수평 Root Motion만 별도로 적용하면 평지에서도 Below 플래그가 사라질 수 있으므로,
        // 중력/접지용 Y 이동을 같은 Move에 포함합니다.
        Vector3 combinedDelta = horizontalDelta;
        combinedDelta.y = verticalVelocity * Time.deltaTime;

        CollisionFlags flags = characterController.Move(combinedDelta);
        bool groundedByMove = (flags & CollisionFlags.Below) != 0;
        IsGrounded = groundedByMove || characterController.isGrounded;

        if (IsGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = groundedGravity;
        }
    }

    private float GetCurrentDodgeRootMotionMultiplier()
    {
        switch (currentDodgeType)
        {
            case DodgeType.SideBackstepLeft:
            case DodgeType.SideBackstepRight:
                return sideBackstepRootMotionMultiplier;
            case DodgeType.ForwardCounterThrust:
                return forwardCounterRootMotionMultiplier;
            case DodgeType.Disengage:
                return disengageRootMotionMultiplier;
            case DodgeType.Backstep:
            default:
                return backstepRootMotionMultiplier;
        }
    }

    private void ApplyRootMotionRotation(Quaternion animationDeltaRotation)
    {
        if (!useDodgeRootRotation || !ShouldApplyRootRotation())
        {
            return;
        }

        float yaw = animationDeltaRotation.eulerAngles.y;

        if (yaw > 180f)
        {
            yaw -= 360f;
        }

        transform.rotation *= Quaternion.Euler(0f, yaw, 0f);
    }

    private bool ShouldApplyRootRotation()
    {
        return currentDodgeType == DodgeType.Disengage ||
               currentDodgeType == DodgeType.ForwardCounterThrust;
    }
}

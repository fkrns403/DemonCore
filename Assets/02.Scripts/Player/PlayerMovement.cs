using State;
using UnityEngine;

/// <summary>
/// 플레이어 이동처리 담당
/// playerinputReader가 읽은 입력값을 카메라 기준 이동 방향으로 변환
/// ChaacherController.move를 통해 플레이어 이동처리
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField, Tooltip("기본 걷기 이동 속도")]
    private float walkSpeed = 4f;
    [SerializeField, Tooltip("데쉬 입력시 이동속도")]
    private float sprintSpeed = 6.5f;
    [SerializeField, Tooltip("플레이어 회전 속도")]
    private float rotationSpeed = 720f;

    [Header("Jump/Gravity")]
    [SerializeField,Tooltip("점프 최대 높이")]
    private float jumpHeight = 1.2f;
    [SerializeField, Tooltip("플레이어에게 적용할 중력 값")]
    private float gravity = -20f;
    [SerializeField, Tooltip("지면 접촉상태시 하강속도")]
    private float groundedGravity = -2f;

    [Header("Dodge/Backstep")]
    [SerializeField, Tooltip("회피중 이동 속도")]
    private float backstepSpeed = 10f;
    [SerializeField, Tooltip("기본 백스탭 지속시간")]
    private float backstepDuration = 0.22f;

    [Header("Root Motion")]
    [SerializeField, Tooltip("회피 중 애니메이션의 이동값을 사용할지")]
    private bool useDodgeRootMotion = true;

    [SerializeField, Tooltip("회피 중 애니메이션의 회전값을 사용할지")]
    private bool useDodgeRootRotation = true;

    [Header("Dodge/side Backstep")]
    [SerializeField, Tooltip("백스텝 좌우이동 속도")]
    private float sideBackstepSpeed = 10.5f;
    [SerializeField, Tooltip("백스텝 좌우이동 지속시간")]
    private float sideBackstepDuration = 0.24f;

    [Header("Dodge - Forward Counter")]
    [SerializeField, Tooltip("백스텝 후 전진 카운터 동작 유지 시간")]
    private float forwardCounterDuration = 0.45f;

    [Header("Dodge - Disengage")]
    [SerializeField, Tooltip("S + 회피로 발동하는 전투 이탈기 속도")]
    private float disengageSpeed = 11f;
    [SerializeField, Tooltip("S + 회피로 발동하는 전투 이탈기 지속 시간")]
    private float disengageDuration = 0.32f;

    [Header("Reference")]
    [SerializeField, Tooltip("이동 방향 기준이 되는 카메라 위치")]
    private Transform cameraTransform;

    private CharacterController characterController;

    private float verticalVelocity;

    private bool isDodging;
    private float dodgeTimer;
    private float currrentDodgeSpeed;
    private Vector3 dodgeDirection;
    private DodgeType currentDodgeType;

    public bool IsGrounded { get; private set; }
    // 지면 접촉 여부
    public bool IsDodging => isDodging;
    public bool IsRising => !IsGrounded && verticalVelocity > 0f;
    // 플레이어 상승 상태인지
    public bool IsFalling => !IsGrounded && verticalVelocity <= 0f;
    // 플레이어가 낙하 상태인지
    public DodgeType CurrentDodgeType => currentDodgeType; 

    

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    public void Move(Vector2 moveInput, bool isSprinting, bool jumpPressed)
    {
        if (cameraTransform == null)
        {
            Debug.LogWarning("playerMovement : cameratransfrom이 지정되지 않았습니다");
            return;
        }

        UpdateGroindedState();
        ApplyGravity();

        if (isDodging)
        {
            UpdateDodge();
            return;
        }

        ApplyJump(jumpPressed);

        Vector3 horizontalDirection = CalculateCamerRelativeDirection(moveInput);
        float currentSpeed = isSprinting ? sprintSpeed : walkSpeed;

        Vector3 velocity = horizontalDirection * currentSpeed;
        velocity.y = verticalVelocity;

        characterController.Move(velocity * Time.deltaTime);

        if (horizontalDirection.sqrMagnitude > 0.01f)
        {
            RotateToMoveDirection(horizontalDirection);
        }

        

    }

    public bool TryStartDodge(Vector2 moveInput, float bufferedSideInput)
    {
        UpdateGroindedState();

        if (isDodging)
        {
            return false;
        }
        if (!IsGrounded)
        {
            return false;
        }


        currentDodgeType = DecideDodgeType(moveInput, bufferedSideInput);
        dodgeDirection = CalculateDodgeDirection(currentDodgeType);

        ApplyDodgeSetting(currentDodgeType);

        isDodging = true;

        return true;
    }

    public bool TryStartDodgeFollowUp(DodgeType followUpType)
    {
        if (!isDodging)
        {
            return false;
        }

        if (currentDodgeType != DodgeType.Backstep)
        {
            return false;
        }

        currentDodgeType = followUpType;

        switch (followUpType)
        {
            case DodgeType.ForwardCounterThrust:
                // 후속 전진 카운터는 애니메이션 이동값을 사용할 예정이므로
                // 코드 이동은 멈추고 Dodge 상태만 유지
                dodgeDirection = Vector3.zero;
                currrentDodgeSpeed = 0f;
                dodgeTimer = forwardCounterDuration;
                return true;

            default:
                return false;
        }
    }
    private DodgeType DecideDodgeType(Vector2 moveInput, float bufferedSideInput)
    {
        float sideInput = 0f;

        if (Mathf.Abs(moveInput.x) > 0.01f)
        {
            sideInput = moveInput.x;
        }
        else if (Mathf.Abs(bufferedSideInput) > 0.01f)
        {
            sideInput = bufferedSideInput;
        }

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
                return backDirection;

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
                currrentDodgeSpeed = sideBackstepSpeed;
                dodgeTimer = sideBackstepDuration;
                break;
            case DodgeType.Disengage:
                currrentDodgeSpeed = disengageSpeed;
                dodgeTimer = disengageDuration;
                break;
            case DodgeType.Backstep:
            default:
                currrentDodgeSpeed = backstepSpeed;
                dodgeTimer = backstepDuration;
                break;
        }
    }

    private void UpdateDodge()
    {
        dodgeTimer -= Time.deltaTime;

        if (useDodgeRootMotion)
        {
            // 수평 이동과 회전은 Animator Root Motion
            // 여기서는 중력만 CharacterController
            Vector3 gravityVelocity = Vector3.up * verticalVelocity;
            characterController.Move(gravityVelocity * Time.deltaTime);
        }
        else
        {
            Vector3 velocity = dodgeDirection * currrentDodgeSpeed;
            velocity.y = verticalVelocity;

            characterController.Move(velocity * Time.deltaTime);
        }

        if (dodgeTimer <= 0f)
        {
            EndDodge();
        }
    }

    private void EndDodge()
    {
        isDodging = false;
        currentDodgeType = DodgeType.None;
    }

    /// <summary>
    /// 지면 접촉 판정
    /// </summary>
    private void UpdateGroindedState()
    {
        IsGrounded = characterController.isGrounded;
        if (IsGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = groundedGravity;
        }
    }

    /// <summary>
    /// 점프 판정과 점프 속도
    /// </summary>
    /// <param name="jumpPressed"></param>
    private void ApplyJump(bool jumpPressed)
    {
        if (!jumpPressed)
        {
            return;
        }
        if (!IsGrounded)
        {
            return;
        }

        verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
    }

    /// <summary>
    /// 중력 가속도
    /// </summary>
    private void ApplyGravity()
    {
        verticalVelocity += gravity * Time.deltaTime;
    }

    public void ApplyAnimationRootMotion(Vector3 animationDeltaPosition, Quaternion animationDeltaRotation)
    {
        if (!useDodgeRootMotion)
        {
            return;
        }

        if (!isDodging)
        {
            return;
        }

        ApplyRootMotionPosition(animationDeltaPosition);
        ApplyRootMotionRotation(animationDeltaRotation);
    }

    private void ApplyRootMotionPosition(Vector3 animationDeltaPosition)
    {
        Vector3 horizontalDelta = animationDeltaPosition;
        horizontalDelta.y = 0f;

        characterController.Move(horizontalDelta);
    }

    private void ApplyRootMotionRotation(Quaternion animationDeltaRotation)
    {
        if (!useDodgeRootRotation)
        {
            return;
        }

        if (!ShouldApplyRootRotation())
        {
            return;
        }

        Vector3 deltaEuler = animationDeltaRotation.eulerAngles;
        Quaternion yawRotation = Quaternion.Euler(0f, deltaEuler.y, 0f);

        transform.rotation = transform.rotation * yawRotation;
    }

    private bool ShouldApplyRootRotation()
    {
        switch (currentDodgeType)
        {
            case DodgeType.Disengage:
                return true;

            case DodgeType.ForwardCounterThrust:
                return true;

            case DodgeType.Backstep:
            case DodgeType.SideBackstepLeft:
            case DodgeType.SideBackstepRight:
            default:
                return false;
        }
    }

    /// <summary>
    /// 입력값을 카메라 기준으로 이동 방향 처리
    /// </summary>
    /// <param name="moveInput">이동 입력값</param>
    /// <returns>카메라 기준 월드 이동 방향</returns>
    private Vector3 CalculateCamerRelativeDirection(Vector2 moveInput)
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

    /// <summary>
    /// 플레이어 이동 방향 보정 
    /// </summary>
    /// <param name="moveDirection">플레이어가 바라보는 방향</param>

    private void RotateToMoveDirection(Vector3 moveDirection)
    {
        Quaternion targetRotation = Quaternion.LookRotation(moveDirection);

        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

}

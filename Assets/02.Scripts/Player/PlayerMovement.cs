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

    [Header("Dodge/side Backstep")]
    [SerializeField, Tooltip("백스텝 좌우이동 속도")]
    private float sideBackstepSpeed = 10.5f;
    [SerializeField, Tooltip("백스텝 좌우이동 지속시간")]
    private float sideBackstepDuration = 0.24f;
    [SerializeField, Tooltip("좌우 회피에서 좌우 방향이 차지하는 비율.")]
    private float sideDirectionWeight = 0.65f;
    [SerializeField, Tooltip("좌우 회피에서 후방 방향이 차지하는 비율")]
    private float sideBackDirectionWeight = 0.35f;

    [Header("Dodge - Disengage")]
    [SerializeField, Tooltip("S + 회피로 발동하는 전투 이탈기 속도입니다.")]
    private float disengageSpeed = 11f;
    [SerializeField, Tooltip("S + 회피로 발동하는 전투 이탈기 지속 시간입니다.")]
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
        dodgeDirection = CalculateDodgeDirection(currentDodgeType, moveInput, bufferedSideInput);

        ApplyDodgeSetting(currentDodgeType);

        isDodging = true;

        return true;
    }

    private DodgeType DecideDodgeType(Vector2 moveInput, float bufferedSideInput)
    {
        if (Mathf.Abs(bufferedSideInput) > 0.01f || Mathf.Abs(moveInput.x) > 0.01f)
        {
            return DodgeType.SideBackstep;
        }

        if (moveInput.y < -0.1f)
        {
            return DodgeType.Disengage;
        }

        return DodgeType.Backstep;
    }

    private Vector3 CalculateDodgeDirection(DodgeType dodgeType, Vector2 moveInput, float bufferedSideInput)
    {
        Vector3 backDirection = -transform.forward;

        switch (dodgeType)
        {
            case DodgeType.SideBackstep:
                return CalculateSideBackstepDirection(moveInput, bufferedSideInput, backDirection);

            case DodgeType.Disengage:
                return backDirection;

            case DodgeType.Backstep:
            default:
                return backDirection;
        }
    }

    private Vector3 CalculateSideBackstepDirection(Vector2 moveInput, float bufferedSideInput, Vector3 backDirection)
    {
        float sideSign = 0f;

        if (Mathf.Abs(moveInput.x) > 0.01f)
        {
            sideSign = Mathf.Sign(moveInput.x);
        }
        else
        {
            sideSign = Mathf.Sign(bufferedSideInput);
        }

        Vector3 cameraRight = cameraTransform.right;
        cameraRight.y = 0f;
        cameraRight.Normalize();

        Vector3 sideDirection = cameraRight * sideSign;

        return (sideDirection * sideDirectionWeight + backDirection * sideBackDirectionWeight).normalized;
    }

    private void ApplyDodgeSetting(DodgeType dodgeType)
    {
        switch (dodgeType)
        {
            case DodgeType.SideBackstep:
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

        Vector3 velocity = dodgeDirection * currrentDodgeSpeed;
        velocity.y = verticalVelocity;

        characterController.Move(velocity * Time.deltaTime);

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

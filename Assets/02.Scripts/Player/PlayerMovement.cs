using UnityEngine;
using UnityEngine.EventSystems;

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

    [Header("Dodge")]
    [SerializeField, Tooltip("회피중 이동 속도")]
    private float dodgeSpeed = 10f;
    [SerializeField, Tooltip("회피 지속시간")]
    private float dodgeDuration = 0.22f;

    [Header("Reference")]
    [SerializeField, Tooltip("이동 방향 기준이 되는 카메라 위치")]
    private Transform camerTransform;

    private CharacterController characterController;
    private float verticalVelocity;

    private bool isDodgeing;
    private float dodgeTimer;
    private Vector3 dodgeDirection;

    public bool IsGrounded { get; private set; }
    // 지면 접촉 여부
    public bool IsDodging => isDodgeing;
    public bool IsRising => !IsGrounded && verticalVelocity > 0f;
    // 플레이어 상승 상태인지
    public bool IsFalling => !IsGrounded && verticalVelocity <= 0f;
    // 플레이어가 낙하 상태인지

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        if (camerTransform == null && Camera.main != null)
        {
            camerTransform = Camera.main.transform;
        }
    }

    public void Move(Vector2 moveInput, bool isSprinting, bool jumpPressed)
    {
        if (camerTransform == null)
        {
            Debug.LogWarning("playerMovement : cameratransfrom이 지정되지 않았습니다");
            return;
        }

        UpdateGroindedState();
        ApplyGravity();

        if (isDodgeing)
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

    public bool TryStartDodge(Vector2 moveInput)
    {
        if (isDodgeing)
        {
            return false;
        }
        if (!IsGrounded)
        {
            return false;
        }

        dodgeDirection = CalcuteDodgeDirection(moveInput);
        dodgeTimer = dodgeDuration;
        isDodgeing = true;

        RotateToMoveDirection(dodgeDirection);
        return true;
    }

    private void UpdateDodge()
    {
        dodgeTimer -= Time.deltaTime;

        Vector3 velocity = dodgeDirection * dodgeSpeed;
        velocity.y = verticalVelocity;

        characterController.Move(velocity * Time.deltaTime);

        if (dodgeTimer <= 0f)
        {
            isDodgeing = false;
        }
    }

    private Vector3 CalcuteDodgeDirection(Vector2 moveInput)
    {
        Vector3 inputDirection = CalculateCamerRelativeDirection(moveInput);
        if (inputDirection.sqrMagnitude > 0.01f)
        {
            return inputDirection;
        }
        return -transform.forward;
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
        Vector3 cameraForward = camerTransform.forward;
        Vector3 cameraRight = camerTransform.right;

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

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
    [Header("Reference")]
    [SerializeField, Tooltip("이동 방향 기준이 되는 카메라 위치")]
    private Transform camerTransform;

    private CharacterController characterController;
    private float verticalVelocity;

    public bool IsGrounded { get; private set; }
    // 지면 접촉 여부
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
        ApplyJump(jumpPressed);
        ApplyGravity();

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

    private void UpdateGroindedState()
    {
        IsGrounded = characterController.isGrounded;
        if (IsGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = groundedGravity;
        }
    }

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

    private void ApplyGravity()
    {
        verticalVelocity += gravity * Time.deltaTime;
    }

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

    private void RotateToMoveDirection(Vector3 moveDirection)
    {
        Quaternion targetRotation = Quaternion.LookRotation(moveDirection);

        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

}

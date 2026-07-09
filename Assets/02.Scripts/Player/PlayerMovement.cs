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
    [Header("Reference")]
    [SerializeField, Tooltip("이동 방향 기준이 되는 카메라 위치")]
    private Transform camerTransform;

    private CharacterController characterController;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        if (camerTransform == null && Camera.main != null)
        {
            camerTransform = Camera.main.transform;
        }
    }

    public void Move(Vector2 moveInput, bool isSprinting)
    {
        if (camerTransform == null)
        {
            Debug.LogWarning("playerMovement : cameratransfrom이 지정되지 않았습니다");
            return;
        }

        Vector3 moveDirection = CalculateCamerRelativeDirection(moveInput);

        if (moveDirection.sqrMagnitude <= 0.01f)
        {
            return;
        }

        float currentSpeed = isSprinting ? sprintSpeed : walkSpeed;
        Vector3 moveAmount = moveDirection * currentSpeed * Time.deltaTime;
        characterController.Move(moveAmount);
        RotateToMoveDirection(moveDirection);
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

        transform.rotation = Quaternion.RotateTowards(transform.rotation,targetRotation,rotationSpeed *Time.deltaTime);
    }

}

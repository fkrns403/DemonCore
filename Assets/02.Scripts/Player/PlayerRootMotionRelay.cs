using UnityEngine;

/// <summary>
/// 모델 자식 오브젝트의 Animator가 계산한 Root Motion을 부모 PlayerMovement로 전달합니다.
/// 실제 위치 변경은 CharacterController가 있는 부모에서만 수행합니다.
/// </summary>
[RequireComponent(typeof(Animator))]
public class PlayerRootMotionRelay : MonoBehaviour
{
    [SerializeField, Tooltip("회피 중 Root Motion delta를 Console에 출력합니다.")]
    private bool showRootMotionLog;

    private Animator animator;
    private PlayerMovement playerMovement;
    private Vector3 initialLocalPosition;
    private Quaternion initialLocalRotation;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        playerMovement = GetComponentInParent<PlayerMovement>();

        initialLocalPosition = transform.localPosition;
        initialLocalRotation = transform.localRotation;
    }

    private void OnAnimatorMove()
    {
        if (animator == null || playerMovement == null)
        {
            ResetModelLocalTransform();
            return;
        }

        if (showRootMotionLog && playerMovement.IsDodging)
        {
            Debug.Log(
                $"RootMotion [{playerMovement.CurrentDodgeType}] " +
                $"Position={animator.deltaPosition}, " +
                $"Rotation={animator.deltaRotation.eulerAngles}"
            );
        }

        playerMovement.ApplyAnimationRootMotion(
            animator.deltaPosition,
            animator.deltaRotation
        );

        // Animator 자식이 이동한 위치를 남겨두면 부모 CharacterController 이동과 이중 적용됩니다.
        ResetModelLocalTransform();
    }

    private void ResetModelLocalTransform()
    {
        transform.localPosition = initialLocalPosition;
        transform.localRotation = initialLocalRotation;
    }
}

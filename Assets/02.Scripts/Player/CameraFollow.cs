using UnityEngine;

/// <summary>
/// 카메라가 플레이어를 일정한 거리와 높이로 따라가도록 합니다.
/// 락온 대상이 있으면 플레이어와 락온 대상 사이를 바라봅니다.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField, Tooltip("카메라 포커스 대상입니다.")]
    private Transform target;

    [SerializeField, Tooltip("락온 상태를 읽을 컨트롤러입니다.")]
    private PlayerLockOnController lockOnController;

    [Header("Follow Settings")]
    [SerializeField, Tooltip("대상 기준 카메라 오프셋입니다.")]
    private Vector3 offset = new Vector3(0f, 3f, -6f);

    [SerializeField, Tooltip("카메라 위치 보간 시간입니다.")]
    private float followSmoothTime = 0.08f;

    [SerializeField, Tooltip("일반 상태에서 바라볼 높이입니다.")]
    private float lookHeight = 1.2f;

    [Header("Lock On Camera")]
    [SerializeField, Tooltip("락온 시 카메라가 플레이어와 적 사이를 바라보는 비율입니다.")]
    private float lockOnTargetBlend = 0.5f;

    private Vector3 followVelocity;

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        FollowTarget();
        LookAtTarget();
    }

    private void FollowTarget()
    {
        Vector3 targetPosition = target.position + offset;
        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref followVelocity,
            followSmoothTime
        );
    }

    private void LookAtTarget()
    {
        Vector3 lookTarget = target.position + Vector3.up * lookHeight;

        if (lockOnController != null && lockOnController.IsLockedOn)
        {
            Vector3 lockOnPosition = lockOnController.CurrentTargetPosition;
            lookTarget = Vector3.Lerp(lookTarget, lockOnPosition, lockOnTargetBlend);
        }

        transform.LookAt(lookTarget);
    }
}

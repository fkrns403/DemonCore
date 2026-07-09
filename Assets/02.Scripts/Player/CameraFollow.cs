using UnityEngine;

/// <summary>
/// 카메라가 플레이어를 일정한 거리와 높이로 따라가도록
/// 현재 단계에서 마우스 회전 없이 고정 오프섹 추적만을 담당
/// </summary>

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField, Tooltip("카메라 포커스 대상")]
    private Transform target;

    [Header("Follow Settings")]
    [SerializeField, Tooltip("대상 기준 카메라 오프셋")]
    private Vector3 offset = new Vector3(0f, 3f, -6f);

    [SerializeField, Tooltip("카메라 흔들림 보정")]
    private float followSmoothTime = 0.08f;

    [SerializeField, Tooltip("카메라 높이")]
    private float lookHeight = 1.2f;

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

        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref followVelocity, followSmoothTime);
    }

    private void LookAtTarget()
    {
        Vector3 lookTarget = target.position + Vector3.up * lookHeight;
        transform.LookAt(lookTarget);
    }

}

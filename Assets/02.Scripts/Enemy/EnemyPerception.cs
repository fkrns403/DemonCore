using UnityEngine;

/// <summary>
/// Enemy Type이 적을 탐지하는 로직을 담당하는 컴포넌트입니다.
/// 시야 탐지, 청각 범위, 피격 반응을 AI 판단 로직과 분리합니다.
/// </summary>
public class EnemyPerception : MonoBehaviour
{
    [Header("Sight Detection")]
    [SerializeField, Tooltip("전방 시야 감지 범위입니다.")]
    private float sightRange = 12.0f;

    [SerializeField, Tooltip("전방 시야각입니다.")]
    private float sightAngle = 120.0f;

    [SerializeField, Tooltip("시야의 기준점입니다. 공란이면 자신의 위치 + eyeHeight를 기준으로 합니다.")]
    private Transform eyePoint;

    [SerializeField, Tooltip("eyePoint 미지정 시 시야의 기준이 되는 눈 높이입니다.")]
    private float eyeHeight = 1.7f;

    [Header("Rear / Close Awareness")]
    [SerializeField, Tooltip("후방 또는 근접 감지 범위입니다.")]
    private float closeAwarenessRange = 2f;

    [Header("Hearing Detection")]
    [SerializeField, Tooltip("소리를 감지하는 기본 거리입니다.")]
    private float hearingRange = 8f;

    [Header("Raycast")]
    [SerializeField, Tooltip("시야를 막는 장애물 레이어입니다.")]
    private LayerMask obstacleMask;

    [SerializeField, Tooltip("타겟의 중심 높이 보정값입니다.")]
    private float targetCenterHeight = 1f;

    private bool isAlerted;
    private Vector3 lastKnownPosition;

    public bool IsAlerted => isAlerted;
    public Vector3 LastKnownPosition => lastKnownPosition;

    public bool CanDetectTarget(Transform target)
    {
        if (target == null)
        {
            return false;
        }

        if (CanSeeTarget(target))
        {
            Alert(target.position);
            return true;
        }

        if (CanSenseCloseTarget(target))
        {
            Alert(target.position);
            return true;
        }

        return isAlerted;
    }

    public void NotifyNoise(Vector3 noisePosition, float noisePower = 1f)
    {
        Vector3 enemyPosition = transform.position;
        enemyPosition.y = 0f;

        Vector3 flatNoisePosition = noisePosition;
        flatNoisePosition.y = 0f;

        float distance = Vector3.Distance(enemyPosition, flatNoisePosition);
        float finalHearingRange = hearingRange * Mathf.Max(0.1f, noisePower);

        if (distance > finalHearingRange)
        {
            return;
        }

        Alert(noisePosition);
    }

    public void NotifyDamaged(Transform attacker)
    {
        if (attacker == null)
        {
            return;
        }

        Alert(attacker.position);
    }

    public void ClearAlert()
    {
        isAlerted = false;
        lastKnownPosition = Vector3.zero;
    }

    private bool CanSeeTarget(Transform target)
    {
        Vector3 origin = GetEyePosition();
        Vector3 targetPosition = GetTargetCenterPosition(target);

        Vector3 toTarget = targetPosition - origin;
        float distance = toTarget.magnitude;

        if (distance > sightRange)
        {
            return false;
        }

        Vector3 flatToTarget = toTarget;
        flatToTarget.y = 0f;

        if (flatToTarget.sqrMagnitude <= 0.001f)
        {
            return true;
        }

        float angle = Vector3.Angle(transform.forward, flatToTarget.normalized);

        if (angle > sightAngle * 0.5f)
        {
            return false;
        }

        if (Physics.Raycast(origin, toTarget.normalized, distance, obstacleMask))
        {
            return false;
        }

        return true;
    }

    private bool CanSenseCloseTarget(Transform target)
    {
        if (closeAwarenessRange <= 0f)
        {
            return false;
        }

        Vector3 enemyPosition = transform.position;
        Vector3 targetPosition = target.position;

        enemyPosition.y = 0f;
        targetPosition.y = 0f;

        float distance = Vector3.Distance(enemyPosition, targetPosition);

        return distance <= closeAwarenessRange;
    }

    private void Alert(Vector3 position)
    {
        isAlerted = true;
        lastKnownPosition = position;
    }

    private Vector3 GetEyePosition()
    {
        if (eyePoint != null)
        {
            return eyePoint.position;
        }

        return transform.position + Vector3.up * eyeHeight;
    }

    private Vector3 GetTargetCenterPosition(Transform target)
    {
        return target.position + Vector3.up * targetCenterHeight;
    }
}

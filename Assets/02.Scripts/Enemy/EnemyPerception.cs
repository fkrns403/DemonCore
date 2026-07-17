using UnityEngine;

/// <summary>
/// Enemy Type이 적을 탐지하는 로직을 담당하는 컴포넌트
/// 시야탐지, 청작범위, 피격 반응 Ai 판단로직과 분리하는 클레스
/// </summary>
public class EnemyPerception : MonoBehaviour
{
    [Header("Sight Detection")]
    [SerializeField, Tooltip("전방 시야 감지 범위")]
    private float sightRange = 12.0f;
    [SerializeField, Tooltip("전방 시야각")]
    private float sightAngle = 120.0f;
    [SerializeField, Tooltip("시야의 기준점 공란이면 자신의 위치 + eyeHeight를 기준으로 한다")]
    private Transform eyePoint;
    [SerializeField, Tooltip("eyepoint 미지정시 시야의 기준이 되는 눈 높이")]
    private float eyeHeight = 1.7f;

    [Header("Rear / Close Awareness")]
    [SerializeField, Tooltip("후방 탐지 범위")]
    private float closeAwarenessRange = 2f;

    [Header("Hearing Detection")]
    [SerializeField, Tooltip("소리를 감지하는 기본 거리입니다.")]
    private float hearingRange = 8f;

    [Header("Raycast")]
    [SerializeField, Tooltip("시야범위를 막는 장애물 레이어")]
    private LayerMask obstacleMask;

    [SerializeField, Tooltip("타겟의 중심 높이 보정값")]
    private float targetCenterHeight = 1f;

    private bool isAlerted;
    private Vector3 lastKnownPosition;

    public bool IsAlerted => isAlerted;
    public Vector3 LastKnownPosition => lastKnownPosition;

    /// <summary>
    /// 전방시야, 근접감지, 이미 경계상태인지 여부 확인하고
    /// 현재 타겟을 감지할 수 있는지를 검사한다
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
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

    /// <summary>
    /// 전방의 시야 범위를 탐색한다
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
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

    /// <summary>
    /// 후방 탐지거리를 탐색한다
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
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

    /// <summary>
    /// 소리 이벤트를 감지
    /// 지금은 함수 틀만 만들어두고, 나중에 NoiseEmitter에서 호출
    /// </summary>
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

    /// <summary>
    /// 피격되었을 때 호출
    /// 공격자를 즉시 감지 상태로 전환
    /// </summary>
    public void NotifyDamaged(Transform attacker)
    {
        if (attacker == null)
        {
            return;
        }

        Alert(attacker.position);
    }

    /// <summary>
    /// 감지 상태를 해제
    /// 안전지대 진입, 활동반경 복귀, 리셋 상태에서 호출할 수 있다.
    /// </summary>
    public void ClearAlert()
    {
        isAlerted = false;
        lastKnownPosition = Vector3.zero;
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

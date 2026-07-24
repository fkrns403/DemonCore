using UnityEngine;

/// <summary>
/// 플레이어의 락온 대상 탐색, 락온 시작/해제, 현재 락온 대상 유효성 검사를 담당합니다.
/// 실제 이동 회전은 PlayerMovement가 현재 락온 대상을 받아 처리합니다.
/// </summary>
public class PlayerLockOnController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInputReader inputReader;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Transform searchOrigin;

    [Header("Search")]
    [SerializeField, Tooltip("락온 가능한 최대 거리입니다.")]
    private float maxLockOnDistance = 18f;

    [SerializeField, Tooltip("카메라 정면 기준 락온 가능한 각도입니다.")]
    private float maxLockOnAngle = 70f;

    [SerializeField, Tooltip("락온 후 이 거리보다 멀어지면 자동 해제됩니다.")]
    private float breakDistance = 24f;

    [SerializeField, Tooltip("락온 후보가 들어있는 레이어입니다. 보통 Enemy 또는 LockOnTarget 레이어를 지정합니다.")]
    private LayerMask targetMask;

    [SerializeField, Tooltip("락온 시야를 막는 벽/장애물 레이어입니다.")]
    private LayerMask obstacleMask;

    public LockOnTarget CurrentLockOnTarget { get; private set; }
    public bool IsLockedOn => CurrentLockOnTarget != null;
    public Transform CurrentTargetTransform => CurrentLockOnTarget != null ? CurrentLockOnTarget.TargetTransform : null;
    public Vector3 CurrentTargetPosition => CurrentLockOnTarget != null
        ? CurrentLockOnTarget.TargetPosition
        : transform.position + transform.forward;

    private void Awake()
    {
        if (inputReader == null)
        {
            inputReader = GetComponent<PlayerInputReader>();
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (searchOrigin == null)
        {
            searchOrigin = transform;
        }
    }

    /// <summary>
    /// PlayerController에서 입력을 읽은 뒤 매 프레임 호출합니다.
    /// </summary>
    public void TickLockOn()
    {
        if (inputReader == null)
        {
            return;
        }

        if (inputReader.LockOnPressed)
        {
            ToggleLockOn();
        }

        ValidateCurrentTarget();
    }

    private void ToggleLockOn()
    {
        if (IsLockedOn)
        {
            ClearLockOn();
            return;
        }

        CurrentLockOnTarget = FindBestTarget();

        if (CurrentLockOnTarget != null)
        {
            Debug.Log($"Lock On : {CurrentLockOnTarget.name}");
        }
        else
        {
            Debug.Log("No Lock On Target");
        }
    }

    public void ClearLockOn()
    {
        if (CurrentLockOnTarget != null)
        {
            Debug.Log("Lock On Cleared");
        }

        CurrentLockOnTarget = null;
    }

    private void ValidateCurrentTarget()
    {
        if (CurrentLockOnTarget == null)
        {
            return;
        }

        float distance = Vector3.Distance(searchOrigin.position, CurrentLockOnTarget.TargetPosition);

        if (distance > breakDistance)
        {
            ClearLockOn();
            return;
        }

        if (IsBlockedByObstacle(CurrentLockOnTarget, distance))
        {
            ClearLockOn();
        }
    }

    private LockOnTarget FindBestTarget()
    {
        if (mainCamera == null)
        {
            return null;
        }

        Collider[] candidates = Physics.OverlapSphere(
            searchOrigin.position,
            maxLockOnDistance,
            targetMask
        );

        LockOnTarget bestTarget = null;
        float bestScore = float.MaxValue;

        foreach (Collider candidate in candidates)
        {
            LockOnTarget lockOnTarget = candidate.GetComponentInParent<LockOnTarget>();

            if (lockOnTarget == null)
            {
                continue;
            }

            Vector3 toTarget = lockOnTarget.TargetPosition - mainCamera.transform.position;
            float distance = toTarget.magnitude;

            if (distance <= 0.001f || distance > maxLockOnDistance)
            {
                continue;
            }

            float angle = Vector3.Angle(mainCamera.transform.forward, toTarget.normalized);

            if (angle > maxLockOnAngle * 0.5f)
            {
                continue;
            }

            if (IsBlockedByObstacle(lockOnTarget, distance))
            {
                continue;
            }

            float score = angle + distance * 0.05f;

            if (score < bestScore)
            {
                bestScore = score;
                bestTarget = lockOnTarget;
            }
        }

        return bestTarget;
    }

    private bool IsBlockedByObstacle(LockOnTarget lockOnTarget, float distance)
    {
        if (mainCamera == null || lockOnTarget == null)
        {
            return true;
        }

        Vector3 origin = mainCamera.transform.position;
        Vector3 direction = lockOnTarget.TargetPosition - origin;

        if (direction.sqrMagnitude <= 0.001f)
        {
            return false;
        }

        return Physics.Raycast(origin, direction.normalized, distance, obstacleMask);
    }
}

using UnityEngine;

/// <summary>
/// 플레이어가 조준할 수 있는 와이어 포인트입니다.
/// 기존 클래스명을 유지해 씬/프리팹 참조를 보존하면서, 액션 타입과 착지/로프/제압 데이터를 확장했습니다.
/// </summary>
public class WireAnchor : MonoBehaviour
{
    [Header("Wire Type")]
    [SerializeField] private WireActionType actionType = WireActionType.Traverse;
    [SerializeField] private bool isAvailable = true;

    [Header("Points")]
    [SerializeField, Tooltip("실제 와이어가 붙는 지점입니다. 비워두면 자기 위치를 사용합니다.")]
    private Transform anchorPoint;

    [SerializeField, Tooltip("횡단/접근 후 도착할 지점입니다. 비워두면 Anchor Point를 도착점으로 사용합니다.")]
    private Transform landingPoint;

    [SerializeField, Tooltip("로프식 이동의 상단 한계점입니다.")]
    private Transform ropeTopPoint;

    [SerializeField, Tooltip("로프식 이동의 하단 한계점입니다.")]
    private Transform ropeBottomPoint;

    [Header("Suppression")]
    [SerializeField, Tooltip("패링 성공 등으로 노출되었을 때만 조준 가능한 보스 제압 부위인지 여부입니다.")]
    private bool requiresExposure;

    [SerializeField, Tooltip("이 제압 부위를 관리하는 보스 제압 컨트롤러입니다.")]
    private BossSuppressionController suppressionOwner;

    private float exposureTimer;

    public WireActionType ActionType => actionType;
    public Transform AnchorTransform => anchorPoint != null ? anchorPoint : transform;
    public Transform LandingTransform => landingPoint;
    public Transform RopeTopTransform => ropeTopPoint;
    public Transform RopeBottomTransform => ropeBottomPoint;
    public BossSuppressionController SuppressionOwner => suppressionOwner;

    public Vector3 AnchorPosition => AnchorTransform.position;
    public Vector3 DestinationPosition => landingPoint != null
        ? landingPoint.position
        : AnchorPosition;

    public bool IsExposed => !requiresExposure || exposureTimer > 0f;
    public bool IsAvailable => isAvailable && IsExposed;
    public bool HasLandingPoint => landingPoint != null;

    private void Update()
    {
        if (exposureTimer > 0f)
        {
            exposureTimer -= Time.deltaTime;

            if (exposureTimer < 0f)
            {
                exposureTimer = 0f;
            }
        }
    }

    /// <summary>
    /// 패링 성공 등으로 보스 와이어 가능 부위를 일정 시간 노출합니다.
    /// </summary>
    public void Expose(float duration)
    {
        exposureTimer = Mathf.Max(exposureTimer, duration);
    }

    public void HideExposure()
    {
        exposureTimer = 0f;
    }

    public void SetAvailable(bool available)
    {
        isAvailable = available;
    }

    /// <summary>
    /// 현재 포인트와 프로필을 기반으로 이동 요청을 생성합니다.
    /// Suppression 타입은 플레이어 이동이 없으므로 요청을 생성하지 않습니다.
    /// </summary>
    public bool TryBuildMoveRequest(WireActionProfile profile, out WireMoveRequest request)
    {
        request = default;

        if (profile == null || profile.ActionType != actionType)
        {
            return false;
        }

        if (actionType == WireActionType.Suppression)
        {
            return false;
        }

        if (actionType == WireActionType.Rope &&
            (ropeTopPoint == null || ropeBottomPoint == null))
        {
            return false;
        }

        if (profile.RequireLandingPoint && landingPoint == null && actionType != WireActionType.Rope)
        {
            return false;
        }

        request = new WireMoveRequest(
            actionType,
            profile,
            AnchorTransform,
            landingPoint,
            ropeTopPoint,
            ropeBottomPoint,
            DestinationPosition
        );

        return true;
    }

    /// <summary>
    /// 보스 제압 와이어를 연결합니다.
    /// 실제 연결 시간과 앵커 수 관리는 BossSuppressionController가 담당합니다.
    /// </summary>
    public bool TryConnectSuppression()
    {
        if (actionType != WireActionType.Suppression || !IsAvailable)
        {
            return false;
        }

        if (suppressionOwner == null)
        {
            Debug.LogWarning($"{name} : BossSuppressionController가 지정되지 않았습니다.");
            return false;
        }

        bool connected = suppressionOwner.TryConnectWeakPoint(this);

        if (connected && requiresExposure)
        {
            HideExposure();
        }

        return connected;
    }
}

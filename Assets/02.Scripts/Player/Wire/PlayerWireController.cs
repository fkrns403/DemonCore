using System;
using UnityEngine;

/// <summary>
/// 와이어 후보 탐색, 발사 요청, 제압 링크, 쿨타임을 조정하는 Orchestrator입니다.
/// 실제 플레이어 이동은 PlayerMovement, 선 표현은 WireLinePresenter가 담당합니다.
/// </summary>
public class PlayerWireController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInputReader inputReader;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private WireLinePresenter linePresenter;

    [Header("Data")]
    [SerializeField, Tooltip("Traverse/Pull/Rope/CombatApproach/Suppression 타입별 프로필입니다.")]
    private WireActionProfile[] actionProfiles;

    [Header("Search")]
    [SerializeField] private LayerMask anchorMask;
    [SerializeField] private LayerMask obstacleMask;

    [SerializeField, Tooltip("동일 점수일 때 각도를 거리보다 얼마나 우선할지 결정합니다.")]
    private float distanceScoreWeight = 0.05f;

    [SerializeField, Range(8, 128), Tooltip("프레임 할당 없이 탐색할 최대 Collider 수입니다.")]
    private int anchorCandidateCapacity = 32;

    private Collider[] anchorCandidateBuffer;
    private WireAnchor currentCandidate;
    private WireAnchor currentActiveAnchor;
    private WireActionProfile currentActiveProfile;
    private BossSuppressionController activeSuppressionOwner;
    private float cooldownTimer;

    public event Action<WireAnchor> CandidateChanged;
    public event Action<WireActionType> WireActionStarted;
    public event Action<WireActionType, WireMoveEndReason> WireActionEnded;
    public event Action<WireAnchor> WireActionFailed;

    public bool WireStartedThisFrame { get; private set; }
    public bool WireFailedThisFrame { get; private set; }
    public WireAnchor CurrentCandidate => currentCandidate;
    public WireAnchor CurrentActiveAnchor => currentActiveAnchor;
    public bool HasActiveSuppressionLink =>
        activeSuppressionOwner != null && activeSuppressionOwner.IsLinkActive;
    public float CooldownRemaining => Mathf.Max(0f, cooldownTimer);

    private void Awake()
    {
        if (inputReader == null)
        {
            inputReader = GetComponent<PlayerInputReader>();
        }

        if (playerMovement == null)
        {
            playerMovement = GetComponent<PlayerMovement>();
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (linePresenter == null)
        {
            linePresenter = GetComponentInChildren<WireLinePresenter>();
        }

        anchorCandidateBuffer = new Collider[Mathf.Max(8, anchorCandidateCapacity)];
    }

    private void OnEnable()
    {
        if (playerMovement != null)
        {
            playerMovement.WireMoveEnded += HandleWireMoveEnded;
        }
    }

    private void OnDisable()
    {
        if (playerMovement != null)
        {
            playerMovement.WireMoveEnded -= HandleWireMoveEnded;
        }
    }

    /// <summary>
    /// PlayerController.Update에서 입력을 읽은 뒤 호출합니다.
    /// </summary>
    public void TickWire()
    {
        TickWire(true);
    }

    /// <summary>
    /// 후보 HUD는 계속 갱신하되, 현재 플레이어 상태가 허용할 때만 발사를 시작합니다.
    /// </summary>
    public void TickWire(bool canStartWireAction)
    {
        WireStartedThisFrame = false;
        WireFailedThisFrame = false;

        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
        }

        ValidateActiveSuppressionLink();
        UpdateCandidate();

        if (inputReader == null || playerMovement == null)
        {
            return;
        }

        if (!inputReader.WirePressed)
        {
            return;
        }

        if (!canStartWireAction)
        {
            FailWire(currentCandidate, 0f);
            return;
        }

        TryStartWire();
    }

    public bool TryInstallSuppressionAnchor(SuppressionAnchorPoint anchorPoint)
    {
        if (!HasActiveSuppressionLink || anchorPoint == null)
        {
            return false;
        }

        return activeSuppressionOwner.TryInstallAnchor(anchorPoint);
    }

    /// <summary>
    /// 플레이어 피격 또는 보스 절단 패턴에서 제압 링크를 끊을 때 사용합니다.
    /// </summary>
    public void BreakSuppressionLink()
    {
        activeSuppressionOwner?.BreakLink();
        ValidateActiveSuppressionLink();
    }

    private void TryStartWire()
    {
        if (cooldownTimer > 0f || currentCandidate == null)
        {
            FailWire(currentCandidate);
            return;
        }

        WireActionProfile profile = GetProfile(currentCandidate.ActionType);

        if (profile == null)
        {
            Debug.LogWarning($"Wire profile missing : {currentCandidate.ActionType}");
            FailWire(currentCandidate);
            return;
        }

        if (currentCandidate.ActionType == WireActionType.Suppression)
        {
            TryStartSuppressionWire(currentCandidate, profile);
            return;
        }

        if (!currentCandidate.TryBuildMoveRequest(profile, out WireMoveRequest request))
        {
            FailWire(currentCandidate, profile.FailureCooldown);
            return;
        }

        if (!playerMovement.TryStartWireMove(request))
        {
            FailWire(currentCandidate, profile.FailureCooldown);
            return;
        }

        currentActiveAnchor = currentCandidate;
        currentActiveProfile = profile;
        WireStartedThisFrame = true;

        if (linePresenter != null)
        {
            linePresenter.Show(currentActiveAnchor.AnchorTransform);
        }

        WireActionStarted?.Invoke(profile.ActionType);
    }

    private void TryStartSuppressionWire(
        WireAnchor anchor,
        WireActionProfile profile)
    {
        if (!anchor.TryConnectSuppression())
        {
            FailWire(anchor, profile.FailureCooldown);
            return;
        }

        currentActiveAnchor = anchor;
        currentActiveProfile = profile;
        activeSuppressionOwner = anchor.SuppressionOwner;
        cooldownTimer = profile.SuccessCooldown;
        WireStartedThisFrame = true;

        if (linePresenter != null)
        {
            linePresenter.Show(anchor.AnchorTransform);
        }

        WireActionStarted?.Invoke(WireActionType.Suppression);
    }

    private void UpdateCandidate()
    {
        WireAnchor nextCandidate = FindBestAnchor();

        if (nextCandidate == currentCandidate)
        {
            return;
        }

        currentCandidate = nextCandidate;
        CandidateChanged?.Invoke(currentCandidate);
    }

    private WireAnchor FindBestAnchor()
    {
        if (mainCamera == null || actionProfiles == null || actionProfiles.Length == 0)
        {
            return null;
        }

        float maximumSearchRange = GetMaximumSearchRange();
        if (anchorCandidateBuffer == null ||
            anchorCandidateBuffer.Length != Mathf.Max(8, anchorCandidateCapacity))
        {
            anchorCandidateBuffer = new Collider[Mathf.Max(8, anchorCandidateCapacity)];
        }

        int hitCount = Physics.OverlapSphereNonAlloc(
            transform.position,
            maximumSearchRange,
            anchorCandidateBuffer,
            anchorMask,
            QueryTriggerInteraction.Collide
        );

        WireAnchor bestAnchor = null;
        float bestScore = float.MaxValue;

        for (int index = 0; index < hitCount; index++)
        {
            Collider hit = anchorCandidateBuffer[index];

            if (hit == null)
            {
                continue;
            }
            WireAnchor anchor = hit.GetComponentInParent<WireAnchor>();

            if (anchor == null || !anchor.IsAvailable)
            {
                continue;
            }

            WireActionProfile profile = GetProfile(anchor.ActionType);

            if (profile == null)
            {
                continue;
            }

            Vector3 playerToAnchor = anchor.AnchorPosition - transform.position;
            float distance = playerToAnchor.magnitude;

            if (distance < profile.MinRange || distance > profile.MaxRange)
            {
                continue;
            }

            if (profile.RequireLandingPoint &&
                anchor.ActionType != WireActionType.Rope &&
                anchor.ActionType != WireActionType.Suppression &&
                !anchor.HasLandingPoint)
            {
                continue;
            }

            Vector3 cameraToAnchor = anchor.AnchorPosition - mainCamera.transform.position;

            if (cameraToAnchor.sqrMagnitude <= 0.001f)
            {
                continue;
            }

            float angle = Vector3.Angle(
                mainCamera.transform.forward,
                cameraToAnchor.normalized
            );

            if (angle > profile.MaxTargetAngle)
            {
                continue;
            }

            if (Physics.Raycast(
                    mainCamera.transform.position,
                    cameraToAnchor.normalized,
                    cameraToAnchor.magnitude,
                    obstacleMask))
            {
                continue;
            }

            float score = angle + distance * distanceScoreWeight;

            if (score < bestScore)
            {
                bestScore = score;
                bestAnchor = anchor;
            }
        }

        return bestAnchor;
    }

    private WireActionProfile GetProfile(WireActionType actionType)
    {
        if (actionProfiles == null)
        {
            return null;
        }

        foreach (WireActionProfile profile in actionProfiles)
        {
            if (profile != null && profile.ActionType == actionType)
            {
                return profile;
            }
        }

        return null;
    }

    private float GetMaximumSearchRange()
    {
        float maximum = 0f;

        foreach (WireActionProfile profile in actionProfiles)
        {
            if (profile != null)
            {
                maximum = Mathf.Max(maximum, profile.MaxRange);
            }
        }

        return Mathf.Max(0.1f, maximum);
    }

    private void HandleWireMoveEnded(
        WireActionType actionType,
        WireMoveEndReason reason)
    {
        float nextCooldown = 0f;

        if (currentActiveProfile != null)
        {
            nextCooldown = reason == WireMoveEndReason.Completed
                ? currentActiveProfile.SuccessCooldown
                : currentActiveProfile.FailureCooldown;
        }

        cooldownTimer = Mathf.Max(cooldownTimer, nextCooldown);
        linePresenter?.Hide();
        currentActiveAnchor = null;
        currentActiveProfile = null;

        WireActionEnded?.Invoke(actionType, reason);
    }

    private void ValidateActiveSuppressionLink()
    {
        if (activeSuppressionOwner == null)
        {
            return;
        }

        if (activeSuppressionOwner.IsLinkActive)
        {
            Transform weakPoint = activeSuppressionOwner.ConnectedWeakPointTransform;

            if (weakPoint != null && linePresenter != null && !linePresenter.IsVisible)
            {
                linePresenter.Show(weakPoint);
            }

            return;
        }

        linePresenter?.Hide();
        currentActiveAnchor = null;
        currentActiveProfile = null;
        activeSuppressionOwner = null;

        WireActionEnded?.Invoke(
            WireActionType.Suppression,
            WireMoveEndReason.Interrupted
        );
    }

    private void FailWire(WireAnchor failedAnchor, float cooldown = 0.7f)
    {
        WireFailedThisFrame = true;
        cooldownTimer = Mathf.Max(cooldownTimer, cooldown);
        WireActionFailed?.Invoke(failedAnchor);
    }
}

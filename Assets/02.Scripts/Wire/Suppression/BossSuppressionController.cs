using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 보스의 제압용 와이어 연결, 앵커 설치 수, 페이즈 전환을 관리합니다.
/// 보스 HP/공격 패턴과 직접 결합하지 않고 이벤트로 결과를 전달합니다.
/// </summary>
public class BossSuppressionController : MonoBehaviour
{
    [Header("Phase Rules")]
    [SerializeField, Min(1)] private int phaseOneRequiredAnchors = 2;
    [SerializeField, Min(1)] private int phaseTwoRequiredAnchors = 4;

    [Header("Connection")]
    [SerializeField, Min(0.1f)] private float baseConnectionDuration = 4f;
    [SerializeField, Min(0f)] private float anchorConnectionBonus = 2f;

    private readonly List<SuppressionAnchorPoint> currentPhaseAnchors =
        new List<SuppressionAnchorPoint>();

    private WireAnchor connectedWeakPoint;
    private float connectionTimer;
    private int currentPhase = 1;

    public event Action<int, int> AnchorCountChanged;
    public event Action<int> PhaseChanged;
    public event Action SuppressionCompleted;
    public event Action<bool> LinkStateChanged;

    public bool IsLinkActive => connectedWeakPoint != null && connectionTimer > 0f;
    public bool IsSuppressed { get; private set; }
    public int CurrentPhase => currentPhase;
    public int InstalledAnchorCount => currentPhaseAnchors.Count;
    public int RequiredAnchorCount => currentPhase == 1
        ? phaseOneRequiredAnchors
        : phaseTwoRequiredAnchors;
    public float ConnectionTimeRemaining => Mathf.Max(0f, connectionTimer);
    public Transform ConnectedWeakPointTransform => connectedWeakPoint != null
        ? connectedWeakPoint.AnchorTransform
        : null;

    private void Update()
    {
        if (!IsLinkActive || IsSuppressed)
        {
            return;
        }

        connectionTimer -= Time.deltaTime;

        if (connectionTimer <= 0f)
        {
            BreakLink();
        }
    }

    public bool TryConnectWeakPoint(WireAnchor weakPoint)
    {
        if (weakPoint == null || IsSuppressed)
        {
            return false;
        }

        connectedWeakPoint = weakPoint;
        connectionTimer = baseConnectionDuration;
        LinkStateChanged?.Invoke(true);
        return true;
    }

    public bool TryInstallAnchor(SuppressionAnchorPoint anchorPoint)
    {
        if (!IsLinkActive || anchorPoint == null || IsSuppressed)
        {
            return false;
        }

        if (!anchorPoint.TryInstall())
        {
            return false;
        }

        currentPhaseAnchors.Add(anchorPoint);
        connectionTimer += anchorConnectionBonus;
        AnchorCountChanged?.Invoke(InstalledAnchorCount, RequiredAnchorCount);

        if (InstalledAnchorCount >= RequiredAnchorCount)
        {
            CompleteCurrentPhase();
        }

        return true;
    }

    /// <summary>
    /// 보스의 와이어 절단 패턴, 플레이어 피격, 유지 시간 종료 시 호출합니다.
    /// 실패로 끊기면 현재 페이즈에 설치한 앵커도 파괴되어 카운트가 초기화됩니다.
    /// </summary>
    public void BreakLink()
    {
        bool wasActive = connectedWeakPoint != null;

        connectedWeakPoint = null;
        connectionTimer = 0f;
        ResetCurrentPhaseAnchors();

        if (wasActive)
        {
            LinkStateChanged?.Invoke(false);
        }
    }

    private void CompleteCurrentPhase()
    {
        ClearLinkOnly();
        currentPhaseAnchors.Clear();

        if (currentPhase == 1)
        {
            currentPhase = 2;
            PhaseChanged?.Invoke(currentPhase);
            AnchorCountChanged?.Invoke(0, RequiredAnchorCount);
            return;
        }

        IsSuppressed = true;
        SuppressionCompleted?.Invoke();
    }

    private void ClearLinkOnly()
    {
        bool wasActive = connectedWeakPoint != null;
        connectedWeakPoint = null;
        connectionTimer = 0f;

        if (wasActive)
        {
            LinkStateChanged?.Invoke(false);
        }
    }

    private void ResetCurrentPhaseAnchors()
    {
        foreach (SuppressionAnchorPoint anchorPoint in currentPhaseAnchors)
        {
            anchorPoint?.ResetPoint();
        }

        currentPhaseAnchors.Clear();
        AnchorCountChanged?.Invoke(0, RequiredAnchorCount);
    }
}

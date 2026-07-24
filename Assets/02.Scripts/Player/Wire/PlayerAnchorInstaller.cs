using System;
using UnityEngine;

/// <summary>
/// 보스 제압 와이어 연결 후, 플레이어 주변의 바닥/벽 앵커 지점에 F 유지 설치를 수행합니다.
/// HUD는 InstallProgressChanged 이벤트로 게이지만 표시합니다.
/// </summary>
public class PlayerAnchorInstaller : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInputReader inputReader;
    [SerializeField] private PlayerWireController wireController;

    [Header("Install")]
    [SerializeField, Min(0.1f)] private float searchRadius = 2.5f;
    [SerializeField, Min(0.05f)] private float installDuration = 0.8f;
    [SerializeField] private LayerMask anchorPointMask;
    [SerializeField, Range(4, 64)] private int candidateCapacity = 16;

    private Collider[] candidateBuffer;
    private SuppressionAnchorPoint currentPoint;
    private float installTimer;

    public event Action<float> InstallProgressChanged;
    public event Action<SuppressionAnchorPoint> AnchorInstalled;

    public bool IsInstalling => currentPoint != null && installTimer > 0f;
    public float ProgressNormalized => installDuration > 0f
        ? Mathf.Clamp01(installTimer / installDuration)
        : 0f;
    public SuppressionAnchorPoint CurrentPoint => currentPoint;

    private void Awake()
    {
        if (inputReader == null)
        {
            inputReader = GetComponent<PlayerInputReader>();
        }

        if (wireController == null)
        {
            wireController = GetComponent<PlayerWireController>();
        }

        candidateBuffer = new Collider[Mathf.Max(4, candidateCapacity)];
    }

    /// <summary>
    /// PlayerController.Update에서 입력을 읽은 뒤 호출합니다.
    /// </summary>
    public void TickInstall()
    {
        if (inputReader == null || wireController == null || !wireController.HasActiveSuppressionLink)
        {
            ResetInstall();
            return;
        }

        SuppressionAnchorPoint nearestPoint = FindNearestAvailablePoint();

        if (nearestPoint != currentPoint)
        {
            currentPoint = nearestPoint;
            installTimer = 0f;
            InstallProgressChanged?.Invoke(0f);
        }

        if (currentPoint == null || !inputReader.InteractHeld)
        {
            if (installTimer > 0f)
            {
                installTimer = 0f;
                InstallProgressChanged?.Invoke(0f);
            }

            return;
        }

        installTimer += Time.deltaTime;
        InstallProgressChanged?.Invoke(ProgressNormalized);

        if (installTimer < installDuration)
        {
            return;
        }

        if (wireController.TryInstallSuppressionAnchor(currentPoint))
        {
            SuppressionAnchorPoint installedPoint = currentPoint;
            ResetInstall();
            AnchorInstalled?.Invoke(installedPoint);
        }
        else
        {
            ResetInstall();
        }
    }

    private SuppressionAnchorPoint FindNearestAvailablePoint()
    {
        if (candidateBuffer == null ||
            candidateBuffer.Length != Mathf.Max(4, candidateCapacity))
        {
            candidateBuffer = new Collider[Mathf.Max(4, candidateCapacity)];
        }

        int hitCount = Physics.OverlapSphereNonAlloc(
            transform.position,
            searchRadius,
            candidateBuffer,
            anchorPointMask,
            QueryTriggerInteraction.Collide
        );

        SuppressionAnchorPoint bestPoint = null;
        float bestDistanceSqr = float.MaxValue;

        for (int index = 0; index < hitCount; index++)
        {
            Collider hit = candidateBuffer[index];

            if (hit == null)
            {
                continue;
            }
            SuppressionAnchorPoint point = hit.GetComponentInParent<SuppressionAnchorPoint>();

            if (point == null || !point.IsAvailable)
            {
                continue;
            }

            float distanceSqr = (point.InstallPosition - transform.position).sqrMagnitude;

            if (distanceSqr < bestDistanceSqr)
            {
                bestDistanceSqr = distanceSqr;
                bestPoint = point;
            }
        }

        return bestPoint;
    }

    private void ResetInstall()
    {
        currentPoint = null;
        installTimer = 0f;
        InstallProgressChanged?.Invoke(0f);
    }
}

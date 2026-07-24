using UnityEngine;

/// <summary>
/// 보스에게 연결된 제압 와이어를 바닥/벽에 고정하는 설치 지점입니다.
/// 설치 지점의 사용 여부만 관리하고, 보스 페이즈/장력 계산은 BossSuppressionController가 담당합니다.
/// </summary>
public class SuppressionAnchorPoint : MonoBehaviour
{
    [SerializeField] private Transform installPoint;
    [SerializeField] private bool isAvailable = true;

    public Vector3 InstallPosition => installPoint != null
        ? installPoint.position
        : transform.position;

    public bool IsInstalled { get; private set; }
    public bool IsAvailable => isAvailable && !IsInstalled;

    public bool TryInstall()
    {
        if (!IsAvailable)
        {
            return false;
        }

        IsInstalled = true;
        return true;
    }

    public void ResetPoint()
    {
        IsInstalled = false;
    }

    public void SetAvailable(bool available)
    {
        isAvailable = available;
    }
}

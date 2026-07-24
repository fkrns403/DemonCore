using UnityEngine;

/// <summary>
/// 플레이어가 락온할 수 있는 대상 지점입니다.
/// 적 루트 또는 적의 가슴/머리 부근 자식 오브젝트에 붙입니다.
/// </summary>
public class LockOnTarget : MonoBehaviour
{
    [Header("Target Point")]
    [SerializeField, Tooltip("락온 카메라와 캐릭터가 바라볼 기준점입니다. 비워두면 자기 위치를 사용합니다.")]
    private Transform targetPoint;

    [SerializeField, Tooltip("targetPoint가 없을 때 사용할 높이 보정값입니다.")]
    private float defaultHeight = 1.2f;

    public Transform TargetTransform => targetPoint != null ? targetPoint : transform;

    public Vector3 TargetPosition
    {
        get
        {
            if (targetPoint != null)
            {
                return targetPoint.position;
            }

            return transform.position + Vector3.up * defaultHeight;
        }
    }
}

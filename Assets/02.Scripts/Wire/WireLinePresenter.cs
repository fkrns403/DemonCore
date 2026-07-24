using UnityEngine;

/// <summary>
/// 오른쪽 손목의 사출 지점과 현재 목표 사이의 와이어 선을 표시합니다.
/// 타겟 탐색/이동 판단과 시각 표현을 분리하기 위한 Presenter입니다.
/// </summary>
public class WireLinePresenter : MonoBehaviour
{
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField, Tooltip("캐릭터 기준 오른쪽 손목 위 사출 장치의 끝점입니다.")]
    private Transform wireStartPoint;

    private Transform targetTransform;
    private Vector3 staticTargetPosition;
    private bool useStaticTarget;

    public bool IsVisible => lineRenderer != null && lineRenderer.enabled;

    private void Awake()
    {
        if (lineRenderer == null)
        {
            lineRenderer = GetComponent<LineRenderer>();
        }

        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 2;
            lineRenderer.enabled = false;
        }
    }

    private void LateUpdate()
    {
        if (!IsVisible)
        {
            return;
        }

        Vector3 start = wireStartPoint != null
            ? wireStartPoint.position
            : transform.position;

        Vector3 end = useStaticTarget || targetTransform == null
            ? staticTargetPosition
            : targetTransform.position;

        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);
    }

    public void Show(Transform target)
    {
        targetTransform = target;
        useStaticTarget = false;

        if (target != null)
        {
            staticTargetPosition = target.position;
        }

        SetVisible(true);
    }

    public void Show(Vector3 targetPosition)
    {
        targetTransform = null;
        staticTargetPosition = targetPosition;
        useStaticTarget = true;
        SetVisible(true);
    }

    public void Hide()
    {
        targetTransform = null;
        useStaticTarget = false;
        SetVisible(false);
    }

    private void SetVisible(bool visible)
    {
        if (lineRenderer != null)
        {
            lineRenderer.enabled = visible;
        }
    }
}

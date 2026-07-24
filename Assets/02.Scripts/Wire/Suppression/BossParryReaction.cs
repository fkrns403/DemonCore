using UnityEngine;

/// <summary>
/// 보스가 패링당했을 때 제압용 와이어 부위를 일정 시간 노출합니다.
/// 일반 적의 경직 반응과 보스 제압 기회를 분리하기 위한 구현입니다.
/// </summary>
public class BossParryReaction : MonoBehaviour, IParryReaction
{
    [SerializeField] private Animator animator;
    [SerializeField] private string parriedTriggerName = "ParriedTrigger";
    [SerializeField, Min(0.1f)] private float weakPointExposureDuration = 1.5f;
    [SerializeField] private WireAnchor[] suppressionWeakPoints;

    private int parriedTriggerHash;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        parriedTriggerHash = Animator.StringToHash(parriedTriggerName);
    }

    public void OnParried(Transform defender)
    {
        if (animator != null)
        {
            animator.SetTrigger(parriedTriggerHash);
        }

        if (suppressionWeakPoints == null)
        {
            return;
        }

        foreach (WireAnchor weakPoint in suppressionWeakPoints)
        {
            weakPoint?.Expose(weakPointExposureDuration);
        }
    }
}

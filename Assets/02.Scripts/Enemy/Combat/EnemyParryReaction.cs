using UnityEngine;

/// <summary>
/// 적이 플레이어에게 패링당했을 때의 반응을 담당합니다.
/// 현재는 Animator Trigger와 로그만 처리하고,
/// 이후 경직, 넉백, 슈퍼아머 판정 등을 확장할 수 있습니다.
/// </summary>
public class EnemyParryReaction : MonoBehaviour, IParryReaction
{
    [SerializeField] private Animator animator;
    [SerializeField, Tooltip("패링 성공 시 적 AI가 멈춰 있는 시간입니다.")]
    private float parryStunDuration = 0.35f;
    [SerializeField] private string parriedTriggerName = "ParriedTrigger";

    private int parriedTriggerHash;
    private BaseEnemyController enemyController;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        enemyController = GetComponent<BaseEnemyController>();
        parriedTriggerHash = Animator.StringToHash(parriedTriggerName);
    }

    public void OnParried(Transform defender)
    {
        Debug.Log($"Enemy Parried by {defender.name}");

        if (enemyController != null)
        {
            enemyController.ApplyParryStun(parryStunDuration);
        }

        if (animator != null)
        {
            animator.SetTrigger(parriedTriggerHash);
        }
    }
}

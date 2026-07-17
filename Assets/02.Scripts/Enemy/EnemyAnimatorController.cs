using UnityEngine;

/// <summary>
/// 적 컨트롤러의 값을 Animator 파라미터로 전달합니다.
/// AI 판단은 하지 않고, 애니메이션 파라미터 전달만 담당합니다.
/// </summary>
public class EnemyAnimatorController : MonoBehaviour
{
    [Header("References")]
    [SerializeField, Tooltip("EnemyModel에 붙어 있는 Animator입니다.")]
    private Animator animator;

    [Header("Animation Settings")]
    [SerializeField, Tooltip("Speed 파라미터 보간 시간입니다.")]
    private float speedDampTime = 0.08f;

    private BaseEnemyController enemyController;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int AttackTriggerHash = Animator.StringToHash("AttackTrigger");

    private void Awake()
    {
        enemyController = GetComponent<BaseEnemyController>();

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    private void LateUpdate()
    {
        if (enemyController == null || animator == null)
        {
            return;
        }

        UpdateMovementAnimation();
        UpdateAttackAnimation();
    }

    private void UpdateMovementAnimation()
    {
        animator.SetFloat(
            SpeedHash,
            enemyController.MoveAnimationSpeed,
            speedDampTime,
            Time.deltaTime
        );
    }

    private void UpdateAttackAnimation()
    {
        if (!enemyController.AttackStartedThisFrame)
        {
            return;
        }

        animator.SetTrigger(AttackTriggerHash);

        Debug.Log("Enemy Attack Animation Request");
    }
}
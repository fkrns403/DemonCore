using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 적 HP와 피격 처리를 담당합니다.
/// 피격 시 EnemyPerception에 공격자를 알려 즉시 경계 상태로 전환할 수 있게 합니다.
/// </summary>
public class EnemyHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private int maxHealth = 100;

    [Header("Animator")]
    [SerializeField] private Animator animator;
    [SerializeField] private string hitTriggerName = "HitTrigger";
    [SerializeField] private string deadTriggerName = "DeadTrigger";

    [Header("Death")]
    [SerializeField, Tooltip("사망 시 AI 컨트롤러를 끌지 여부입니다.")]
    private bool disableEnemyControllerOnDeath = true;

    [SerializeField, Tooltip("사망 시 NavMeshAgent를 끌지 여부입니다.")]
    private bool disableAgentOnDeath = true;

    private int currentHealth;
    private EnemyPerception perception;
    private BaseEnemyController enemyController;
    private NavMeshAgent navMeshAgent;

    private int hitTriggerHash;
    private int deadTriggerHash;

    public int CurrentHealth => currentHealth;
    public bool IsDead => currentHealth <= 0;

    private void Awake()
    {
        currentHealth = maxHealth;
        perception = GetComponent<EnemyPerception>();
        enemyController = GetComponent<BaseEnemyController>();
        navMeshAgent = GetComponent<NavMeshAgent>();

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        hitTriggerHash = Animator.StringToHash(hitTriggerName);
        deadTriggerHash = Animator.StringToHash(deadTriggerName);
    }

    public void TakeDamage(DamageInfo damageInfo)
    {
        if (IsDead)
        {
            return;
        }

        currentHealth -= damageInfo.Damage;
        currentHealth = Mathf.Max(currentHealth, 0);

        if (perception != null)
        {
            perception.NotifyDamaged(damageInfo.Attacker);
        }

        Debug.Log($"Enemy Damaged : {damageInfo.Damage} / HP {currentHealth}");

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        PlayHitAnimation();
    }

    private void PlayHitAnimation()
    {
        if (animator != null)
        {
            animator.SetTrigger(hitTriggerHash);
        }
    }

    private void Die()
    {
        Debug.Log("Enemy Dead");

        if (animator != null)
        {
            animator.SetTrigger(deadTriggerHash);
        }

        if (disableAgentOnDeath && navMeshAgent != null)
        {
            navMeshAgent.isStopped = true;
            navMeshAgent.ResetPath();
            navMeshAgent.enabled = false;
        }

        if (disableEnemyControllerOnDeath && enemyController != null)
        {
            enemyController.enabled = false;
        }
    }
}

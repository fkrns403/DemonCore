using State;
using UnityEngine;

/// <summary>
/// 모든 적이 공통으로 가지는 기본 제어 클래스입니다.
/// 감지 자체는 EnemyPerception이 담당하고,
/// 이 클래스는 타겟, 상태, 공격 쿨타임 같은 공통 흐름을 담당합니다.
/// </summary>
public abstract class BaseEnemyController : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField, Tooltip("상태 변경 로그를 출력할지 여부입니다.")]
    protected bool showDebugLog = true;

    [Header("Target")]
    [SerializeField, Tooltip("추적 대상입니다. 비워두면 Player 태그 또는 이름으로 찾습니다.")]
    protected Transform target;

    [Header("Common Attack")]
    [SerializeField, Tooltip("공격을 시작하는 거리입니다.")]
    protected float attackRange = 1.8f;

    [SerializeField, Tooltip("공격 후 다음 공격까지의 대기 시간입니다.")]
    protected float attackCooldown = 1.5f;

    [SerializeField, Tooltip("공격 애니메이션이 끝날 때까지 공격 상태를 유지하는 시간입니다.")]
    protected float attackLockDuration = 1.0f;

    protected EnemyPerception perception;

    protected EnemyState currentState = EnemyState.Idle;

    protected float attackCooldownTimer;
    protected float attackLockTimer;
    protected float parryStunTimer;

    public EnemyState CurrentState => currentState;
    public float MoveAnimationSpeed { get; protected set; }
    public bool AttackStartedThisFrame { get; protected set; }
    public bool IsAttackLocked => attackLockTimer > 0f;
    public bool IsParryStunned => parryStunTimer > 0f;

    protected virtual void Start()
    {
        perception = GetComponent<EnemyPerception>();

        ResolveTarget();
        ChangeState(EnemyState.Idle);
    }

    protected virtual void Update()
    {
        ResetFrameFlags();
        UpdateTimers();

        if (target == null)
        {
            ResolveTarget();
            SetNoTargetState();
            return;
        }

        UpdateEnemyLogic();
    }

    protected virtual bool HasDetectedTarget()
    {
        if (perception == null)
        {
            return false;
        }

        return perception.CanDetectTarget(target);
    }

    protected virtual void ResetFrameFlags()
    {
        AttackStartedThisFrame = false;
    }

    protected virtual void UpdateTimers()
    {
        if (attackCooldownTimer > 0f)
        {
            attackCooldownTimer -= Time.deltaTime;
        }

        if (attackLockTimer > 0f)
        {
            attackLockTimer -= Time.deltaTime;
        }

        if (parryStunTimer > 0f)
        {
            parryStunTimer -= Time.deltaTime;
        }
    }

    protected virtual void ResolveTarget()
    {
        if (target != null)
        {
            return;
        }

        GameObject playerByTag = GameObject.FindGameObjectWithTag("Player");

        if (playerByTag != null)
        {
            target = playerByTag.transform;
            return;
        }

        GameObject playerByName = GameObject.Find("Player");

        if (playerByName != null)
        {
            target = playerByName.transform;
        }
    }

    protected virtual void SetNoTargetState()
    {
        MoveAnimationSpeed = 0f;
        ChangeState(EnemyState.Idle);
    }

    protected float GetFlatDistanceToTarget()
    {
        if (target == null)
        {
            return float.MaxValue;
        }

        Vector3 enemyPosition = transform.position;
        Vector3 targetPosition = target.position;

        enemyPosition.y = 0f;
        targetPosition.y = 0f;

        return Vector3.Distance(enemyPosition, targetPosition);
    }

    public virtual void ApplyParryStun(float duration)
    {
        parryStunTimer = Mathf.Max(parryStunTimer, duration);
        attackLockTimer = 0f;
        AttackStartedThisFrame = false;
        MoveAnimationSpeed = 0f;
        ChangeState(EnemyState.Hit);
    }

    protected virtual void ChangeState(EnemyState nextState)
    {
        if (currentState == nextState)
        {
            return;
        }

        EnemyState previousState = currentState;
        currentState = nextState;

        if (showDebugLog)
        {
            Debug.Log($"Enemy State Changed : {previousState} -> {currentState}");
        }
    }

    protected abstract void UpdateEnemyLogic();
}

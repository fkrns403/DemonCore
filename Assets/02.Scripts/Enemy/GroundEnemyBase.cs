using State;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// NavMeshAgent를 사용하는 지상형 적의 공통 클래스입니다.
/// 감지된 타겟을 추적하고, 공격 거리 안에 들어오면 이동을 멈춘 뒤 공격 상태로 전환합니다.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(EnemyPerception))]
public abstract class GroundEnemyBase : BaseEnemyController
{
    protected NavMeshAgent agent;

    [Header("Ground Movement")]
    [SerializeField, Tooltip("추적 이동 속도입니다.")]
    protected float chaseSpeed = 2.5f;

    [SerializeField, Tooltip("회전 속도입니다.")]
    protected float rotationSpeed = 540f;

    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    protected override void Start()
    {
        SetupAgent();
        base.Start();
    }

    /// <summary>
    /// NavMeshAgent의 기본 이동 설정을 초기화합니다.
    /// 이동 경로는 Agent가 담당하고, 회전은 코드에서 직접 처리합니다.
    /// </summary>
    protected virtual void SetupAgent()
    {
        if (agent == null)
        {
            return;
        }

        agent.speed = chaseSpeed;
        agent.stoppingDistance = attackRange;

        // 액션게임에서는 공격 직전 방향 제어가 중요하므로 회전은 코드에서 직접 처리합니다.
        agent.updateRotation = false;
    }

    protected override void UpdateEnemyLogic()
    {
        if (currentState == EnemyState.Dead)
        {
            StopMove();
            return;
        }

        if (IsParryStunned)
        {
            UpdateParryStunState();
            return;
        }

        if (IsAttackLocked)
        {
            UpdateAttackLockState();
            return;
        }

        if (!HasDetectedTarget())
        {
            SetIdle();
            return;
        }

        float distanceToTarget = GetFlatDistanceToTarget();

        if (distanceToTarget <= attackRange)
        {
            TryStartAttack();
            return;
        }

        ChaseTarget();
    }

    protected virtual void SetIdle()
    {
        StopMove();
        MoveAnimationSpeed = 0f;
        ChangeState(EnemyState.Idle);
    }

    protected virtual void ChaseTarget()
    {
        if (target == null)
        {
            SetIdle();
            return;
        }

        ChangeState(EnemyState.Chase);

        MoveToTarget(target.position);
        RotateToTarget();
        UpdateMoveAnimationSpeed();
    }

    protected virtual void MoveToTarget(Vector3 destination)
    {
        if (agent == null)
        {
            return;
        }

        if (!agent.isOnNavMesh)
        {
            Debug.LogWarning($"{name} : NavMeshAgent가 NavMesh 위에 있지 않습니다.");
            MoveAnimationSpeed = 0f;
            return;
        }

        agent.isStopped = false;
        agent.SetDestination(destination);
    }

    protected virtual void TryStartAttack()
    {
        StopMove();
        RotateToTarget();

        MoveAnimationSpeed = 0f;

        if (attackCooldownTimer > 0f)
        {
            ChangeState(EnemyState.Idle);
            return;
        }

        attackCooldownTimer = attackCooldown;
        attackLockTimer = attackLockDuration;

        AttackStartedThisFrame = true;

        ChangeState(EnemyState.Attack);
    }

    protected virtual void UpdateAttackLockState()
    {
        StopMove();
        RotateToTarget();

        MoveAnimationSpeed = 0f;

        ChangeState(EnemyState.Attack);
    }

    protected virtual void UpdateParryStunState()
    {
        StopMove();
        RotateToTarget();

        MoveAnimationSpeed = 0f;

        ChangeState(EnemyState.Hit);
    }

    protected virtual void StopMove()
    {
        if (agent == null)
        {
            return;
        }

        if (!agent.isOnNavMesh)
        {
            return;
        }

        agent.isStopped = true;
        agent.ResetPath();
    }

    protected virtual void RotateToTarget()
    {
        if (target == null)
        {
            return;
        }

        Vector3 direction = target.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    protected virtual void UpdateMoveAnimationSpeed()
    {
        if (agent == null || agent.speed <= 0f)
        {
            MoveAnimationSpeed = 0f;
            return;
        }

        MoveAnimationSpeed = Mathf.Clamp01(agent.velocity.magnitude / agent.speed);
    }
}

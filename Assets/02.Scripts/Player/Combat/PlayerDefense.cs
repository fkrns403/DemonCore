using System;
using UnityEngine;

/// <summary>
/// 플레이어의 가드, 패링, 카운터 가능 시간을 관리합니다.
/// 실제 HP 감소는 PlayerHealth가 담당하고,
/// 이 클래스는 데미지를 받기 전에 방어 판정을 먼저 수행합니다.
/// </summary>
public class PlayerDefense : MonoBehaviour
{
    [Header("Parry")]
    [SerializeField, Tooltip("패링 입력 후 패링이 유효한 시간입니다.")]
    private float parryDuration = 0.18f;

    [SerializeField, Tooltip("패링 성공 후 카운터 입력이 가능한 시간입니다.")]
    private float counterWindowDuration = 0.35f;

    [SerializeField, Tooltip("정면 기준 패링 가능한 각도입니다.")]
    private float parryAngle = 120f;

    [Header("Guard")]
    [SerializeField, Tooltip("정면 기준 가드 가능한 각도입니다.")]
    private float guardAngle = 120f;

    [SerializeField, Tooltip("가드 성공 시 실제로 받는 데미지 비율입니다.")]
    private float guardDamageMultiplier = 0.30f;

    [SerializeField, Range(0f, 100f), Tooltip("공격 1회 가드 시 소모되는 스태미나입니다.")]
    private float guardStaminaCost = 15f;

    [SerializeField] private PlayerStamina playerStamina;

    private float parryTimer;
    private float counterTimer;

    public bool IsGuarding { get; private set; }
    public bool IsParryActive => parryTimer > 0f;
    public bool IsCounterAvailable => counterTimer > 0f;

    public bool ParryStartedThisFrame { get; private set; }
    public bool ParrySucceededThisFrame { get; private set; }
    public bool CounterStartedThisFrame { get; private set; }

    public event Action GuardBroken;

    private void Awake()
    {
        if (playerStamina == null)
        {
            playerStamina = GetComponent<PlayerStamina>();
        }
    }

    /// <summary>
    /// PlayerController에서 매 프레임 호출합니다.
    /// PlayerInputReader가 입력을 읽은 뒤 호출해야 입력 순서가 안정적입니다.
    /// </summary>
    public void TickDefense(bool guardPressed, bool guardHeld, bool counterAttackPressed)
    {
        ResetFrameFlags();
        UpdateTimers();

        IsGuarding = guardHeld;

        if (guardPressed)
        {
            StartParry();
        }

        if (counterAttackPressed)
        {
            TryStartCounter();
        }
    }

    /// <summary>
    /// 적 공격이 플레이어에게 닿았을 때 먼저 호출됩니다.
    /// 패링 성공 여부만 판단합니다.
    /// </summary>
    public bool TryParry(DamageInfo damageInfo)
    {
        if (!IsParryActive)
        {
            return false;
        }

        if (!damageInfo.IsParryable)
        {
            return false;
        }

        if (!IsAttackFromFront(damageInfo.Attacker, parryAngle))
        {
            return false;
        }

        parryTimer = 0f;
        counterTimer = counterWindowDuration;

        ParrySucceededThisFrame = true;

        Debug.Log("Parry Success");

        return true;
    }

    /// <summary>
    /// 가드 성공 여부와 감소된 데미지를 계산합니다.
    /// </summary>
    public bool TryGuard(DamageInfo damageInfo, out int reducedDamage)
    {
        reducedDamage = damageInfo.Damage;

        if (!IsGuarding)
        {
            return false;
        }

        if (!damageInfo.IsGuardable)
        {
            return false;
        }

        if (!IsAttackFromFront(damageInfo.Attacker, guardAngle))
        {
            return false;
        }

        if (playerStamina != null && !playerStamina.TrySpend(guardStaminaCost))
        {
            IsGuarding = false;
            GuardBroken?.Invoke();
            Debug.Log("Guard Break : Not Enough Stamina");
            return false;
        }

        reducedDamage = Mathf.CeilToInt(damageInfo.Damage * guardDamageMultiplier);
        Debug.Log($"Guard Success : {damageInfo.Damage} -> {reducedDamage}");

        return true;
    }

    private void ResetFrameFlags()
    {
        ParryStartedThisFrame = false;
        ParrySucceededThisFrame = false;
        CounterStartedThisFrame = false;
    }

    private void UpdateTimers()
    {
        if (parryTimer > 0f)
        {
            parryTimer -= Time.deltaTime;
        }

        if (counterTimer > 0f)
        {
            counterTimer -= Time.deltaTime;
        }
    }

    private void StartParry()
    {
        parryTimer = parryDuration;
        ParryStartedThisFrame = true;
    }

    private void TryStartCounter()
    {
        if (!IsCounterAvailable)
        {
            return;
        }

        counterTimer = 0f;
        CounterStartedThisFrame = true;

        Debug.Log("Parry Counter Start");
    }

    private bool IsAttackFromFront(Transform attacker, float angleLimit)
    {
        if (attacker == null)
        {
            return false;
        }

        Vector3 toAttacker = attacker.position - transform.position;
        toAttacker.y = 0f;

        if (toAttacker.sqrMagnitude <= 0.001f)
        {
            return true;
        }

        float angle = Vector3.Angle(transform.forward, toAttacker.normalized);

        return angle <= angleLimit * 0.5f;
    }
}

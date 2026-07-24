using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어 무기의 공격 판정입니다.
/// 무기 모델이 자체 본과 애니메이션을 가지는 경우,
/// 이 Hitbox를 무기 본 또는 칼날 오브젝트 아래에 붙이면 애니메이션에 따라 히트박스가 같이 움직입니다.
/// </summary>
public class PlayerAttackHitbox : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private int damage = 100;
    [SerializeField] private bool isParryable = false;
    [SerializeField] private bool isGuardable = false;

    [Header("References")]
    [SerializeField, Tooltip("공격자 루트입니다. 비워두면 transform.root를 사용합니다.")]
    private Transform owner;

    [SerializeField, Tooltip("공격 판정용 콜라이더입니다. 비워두면 자기 오브젝트의 Collider를 사용합니다.")]
    private Collider hitboxCollider;

    private bool isActive;
    private readonly HashSet<IDamageable> damagedTargets = new HashSet<IDamageable>();

    private void Awake()
    {
        if (owner == null)
        {
            owner = transform.root;
        }

        if (hitboxCollider == null)
        {
            hitboxCollider = GetComponent<Collider>();
        }

        if (hitboxCollider != null)
        {
            hitboxCollider.isTrigger = true;
            hitboxCollider.enabled = false;
        }
    }

    private void OnDisable()
    {
        DisableHitbox();
    }

    /// <summary>
    /// 애니메이션 이벤트에서 공격별 데미지를 바꿀 때 사용합니다.
    /// 예: 1타 100, 2타 110, 3타 130, 패링 카운터 250.
    /// </summary>
    public void SetDamage(int nextDamage)
    {
        damage = Mathf.Max(0, nextDamage);
    }

    /// <summary>
    /// 공격의 방어/패링 가능 여부를 바꿉니다.
    /// 플레이어 공격은 보통 적에게 맞는 공격이므로 기본값은 false입니다.
    /// </summary>
    public void SetAttackFlags(bool nextIsParryable, bool nextIsGuardable)
    {
        isParryable = nextIsParryable;
        isGuardable = nextIsGuardable;
    }

    public void EnableHitbox()
    {
        isActive = true;
        damagedTargets.Clear();

        if (hitboxCollider != null)
        {
            hitboxCollider.enabled = true;
        }
    }

    public void EnableHitbox(int nextDamage)
    {
        SetDamage(nextDamage);
        EnableHitbox();
    }

    public void DisableHitbox()
    {
        isActive = false;
        damagedTargets.Clear();

        if (hitboxCollider != null)
        {
            hitboxCollider.enabled = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isActive)
        {
            return;
        }

        if (owner != null && other.transform.root == owner.root)
        {
            return;
        }

        IDamageable damageable = other.GetComponentInParent<IDamageable>();

        if (damageable == null)
        {
            return;
        }

        if (damagedTargets.Contains(damageable))
        {
            return;
        }

        damagedTargets.Add(damageable);

        Vector3 hitPoint = other.ClosestPoint(transform.position);
        Vector3 attackDirection = transform.forward;

        DamageInfo damageInfo = new DamageInfo(
            damage,
            owner,
            hitPoint,
            attackDirection,
            isParryable,
            isGuardable
        );

        damageable.TakeDamage(damageInfo);
    }
}

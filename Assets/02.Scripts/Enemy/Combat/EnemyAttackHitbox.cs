using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 적의 공격 판정입니다.
/// 공격 애니메이션의 유효 프레임에서만 활성화되어 플레이어에게 데미지를 전달합니다.
/// </summary>
public class EnemyAttackHitbox : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private int damage = 20;
    [SerializeField] private bool isParryable = true;
    [SerializeField] private bool isGuardable = true;

    [Header("References")]
    [SerializeField] private Transform owner;
    [SerializeField] private Collider hitboxCollider;

    private bool isActive;
    private readonly HashSet<Collider> hitColliders = new HashSet<Collider>();

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

    public void SetDamage(int nextDamage)
    {
        damage = Mathf.Max(0, nextDamage);
    }

    public void SetAttackFlags(bool nextIsParryable, bool nextIsGuardable)
    {
        isParryable = nextIsParryable;
        isGuardable = nextIsGuardable;
    }

    public void EnableHitbox()
    {
        isActive = true;
        hitColliders.Clear();

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
        hitColliders.Clear();

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

        if (hitColliders.Contains(other))
        {
            return;
        }

        hitColliders.Add(other);

        PlayerDefense defense = other.GetComponentInParent<PlayerDefense>();
        IDamageable damageable = other.GetComponentInParent<IDamageable>();

        if (defense == null && damageable == null)
        {
            return;
        }

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

        if (defense != null && defense.TryParry(damageInfo))
        {
            IParryReaction parryReaction = owner != null ? owner.GetComponent<IParryReaction>() : null;

            if (parryReaction != null)
            {
                parryReaction.OnParried(defense.transform);
            }

            return;
        }

        if (defense != null && defense.TryGuard(damageInfo, out int reducedDamage))
        {
            DamageInfo guardedDamageInfo = new DamageInfo(
                reducedDamage,
                damageInfo.Attacker,
                damageInfo.HitPoint,
                damageInfo.AttackDirection,
                damageInfo.IsParryable,
                damageInfo.IsGuardable
            );

            damageable?.TakeDamage(guardedDamageInfo);
            return;
        }

        damageable?.TakeDamage(damageInfo);
    }
}

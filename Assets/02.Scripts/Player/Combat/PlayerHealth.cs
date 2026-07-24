using System;
using UnityEngine;

/// <summary>
/// 플레이어 HP와 최종 피격 처리를 담당합니다.
/// 패링/가드는 EnemyAttackHitbox에서 먼저 판정하고, 회피 무적은 여기에서 최종 차단합니다.
/// </summary>
public class PlayerHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private int maxHealth = 1000;
    [SerializeField] private PlayerInvulnerability invulnerability;
    [SerializeField] private PlayerWireController wireController;

    private int currentHealth;

    public event Action<int, int> HealthChanged;
    public event Action<DamageInfo> Damaged;
    public event Action Died;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDead => currentHealth <= 0;

    private void Awake()
    {
        currentHealth = maxHealth;

        if (invulnerability == null)
        {
            invulnerability = GetComponent<PlayerInvulnerability>();
        }

        if (wireController == null)
        {
            wireController = GetComponent<PlayerWireController>();
        }
    }

    public void TakeDamage(DamageInfo damageInfo)
    {
        if (IsDead)
        {
            return;
        }

        if (invulnerability != null && invulnerability.IsInvulnerable)
        {
            Debug.Log("Dodge Invulnerability : Damage Ignored");
            return;
        }

        int appliedDamage = Mathf.Max(0, damageInfo.Damage);
        currentHealth = Mathf.Max(0, currentHealth - appliedDamage);

        wireController?.BreakSuppressionLink();
        Damaged?.Invoke(damageInfo);
        HealthChanged?.Invoke(currentHealth, maxHealth);

        Debug.Log($"Player Damaged : {appliedDamage} / HP {currentHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        if (IsDead || amount <= 0)
        {
            return;
        }

        int nextHealth = Mathf.Min(maxHealth, currentHealth + amount);

        if (nextHealth == currentHealth)
        {
            return;
        }

        currentHealth = nextHealth;
        HealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void Die()
    {
        wireController?.BreakSuppressionLink();
        Died?.Invoke();
        Debug.Log("Player Dead");
    }
}

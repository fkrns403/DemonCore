using System;
using UnityEngine;

/// <summary>
/// 플레이어 스태미나의 소모, 회복 지연, 자연 회복을 담당합니다.
/// HUD는 StaminaChanged 이벤트만 구독하고 게임 규칙은 이 컴포넌트에 유지합니다.
/// </summary>
public class PlayerStamina : MonoBehaviour
{
    [SerializeField, Min(1f)] private float maxStamina = 120f;
    [SerializeField, Min(0f)] private float regenerationPerSecond = 28f;
    [SerializeField, Min(0f)] private float regenerationDelay = 0.8f;

    private float currentStamina;
    private float regenerationDelayTimer;

    public event Action<float, float> StaminaChanged;

    public float CurrentStamina => currentStamina;
    public float MaxStamina => maxStamina;
    public float Normalized => maxStamina > 0f ? currentStamina / maxStamina : 0f;

    private void Awake()
    {
        currentStamina = maxStamina;
    }

    private void Update()
    {
        if (regenerationDelayTimer > 0f)
        {
            regenerationDelayTimer -= Time.deltaTime;
            return;
        }

        if (currentStamina >= maxStamina)
        {
            return;
        }

        SetCurrent(currentStamina + regenerationPerSecond * Time.deltaTime);
    }

    public bool CanSpend(float amount)
    {
        return amount <= 0f || currentStamina >= amount;
    }

    public bool TrySpend(float amount)
    {
        amount = Mathf.Max(0f, amount);

        if (!CanSpend(amount))
        {
            return false;
        }

        SetCurrent(currentStamina - amount);
        regenerationDelayTimer = regenerationDelay;
        return true;
    }

    public void Restore(float amount)
    {
        SetCurrent(currentStamina + Mathf.Max(0f, amount));
    }

    public void RestoreFull()
    {
        SetCurrent(maxStamina);
    }

    public void DelayRegeneration(float duration)
    {
        regenerationDelayTimer = Mathf.Max(regenerationDelayTimer, duration);
    }

    private void SetCurrent(float value)
    {
        float clamped = Mathf.Clamp(value, 0f, maxStamina);

        if (Mathf.Approximately(clamped, currentStamina))
        {
            return;
        }

        currentStamina = clamped;
        StaminaChanged?.Invoke(currentStamina, maxStamina);
    }
}

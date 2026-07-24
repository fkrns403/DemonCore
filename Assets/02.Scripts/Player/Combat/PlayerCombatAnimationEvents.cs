using UnityEngine;

/// <summary>
/// 플레이어 공격 애니메이션 이벤트를 받는 컴포넌트입니다.
/// Animator가 붙은 모델 오브젝트에 붙이고,
/// 무기 본 아래의 PlayerAttackHitbox를 연결합니다.
/// </summary>
public class PlayerCombatAnimationEvents : MonoBehaviour
{
    [SerializeField] private PlayerAttackHitbox attackHitbox;

    [Header("Default Damage")]
    [SerializeField] private int lightAttack1Damage = 100;
    [SerializeField] private int lightAttack2Damage = 110;
    [SerializeField] private int lightAttack3Damage = 130;
    [SerializeField] private int heavyAttackDamage = 180;
    [SerializeField] private int airAttackDamage = 120;
    [SerializeField] private int parryCounterDamage = 250;

    public void AnimationEvent_EnablePlayerHitbox()
    {
        if (attackHitbox != null)
        {
            attackHitbox.EnableHitbox();
        }
    }

    public void AnimationEvent_EnablePlayerHitboxWithDamage(int damage)
    {
        if (attackHitbox != null)
        {
            attackHitbox.EnableHitbox(damage);
        }
    }

    public void AnimationEvent_DisablePlayerHitbox()
    {
        if (attackHitbox != null)
        {
            attackHitbox.DisableHitbox();
        }
    }

    public void AnimationEvent_SetPlayerHitboxDamage(int damage)
    {
        if (attackHitbox != null)
        {
            attackHitbox.SetDamage(damage);
        }
    }

    public void AnimationEvent_EnableLightAttack1Hitbox()
    {
        AnimationEvent_EnablePlayerHitboxWithDamage(lightAttack1Damage);
    }

    public void AnimationEvent_EnableLightAttack2Hitbox()
    {
        AnimationEvent_EnablePlayerHitboxWithDamage(lightAttack2Damage);
    }

    public void AnimationEvent_EnableLightAttack3Hitbox()
    {
        AnimationEvent_EnablePlayerHitboxWithDamage(lightAttack3Damage);
    }

    public void AnimationEvent_EnableHeavyAttackHitbox()
    {
        AnimationEvent_EnablePlayerHitboxWithDamage(heavyAttackDamage);
    }

    public void AnimationEvent_EnableAirAttackHitbox()
    {
        AnimationEvent_EnablePlayerHitboxWithDamage(airAttackDamage);
    }

    public void AnimationEvent_EnableParryCounterHitbox()
    {
        AnimationEvent_EnablePlayerHitboxWithDamage(parryCounterDamage);
    }
}

using UnityEngine;

/// <summary>
/// 적 공격 애니메이션 이벤트를 받아 공격 히트박스를 켜고 끕니다.
/// </summary>
public class EnemyCombatAnimationEvents : MonoBehaviour
{
    [SerializeField] private EnemyAttackHitbox attackHitbox;

    public void AnimationEvent_EnableEnemyHitbox()
    {
        if (attackHitbox != null)
        {
            attackHitbox.EnableHitbox();
        }
    }

    public void AnimationEvent_EnableEnemyHitboxWithDamage(int damage)
    {
        if (attackHitbox != null)
        {
            attackHitbox.EnableHitbox(damage);
        }
    }

    public void AnimationEvent_SetEnemyHitboxDamage(int damage)
    {
        if (attackHitbox != null)
        {
            attackHitbox.SetDamage(damage);
        }
    }

    public void AnimationEvent_DisableEnemyHitbox()
    {
        if (attackHitbox != null)
        {
            attackHitbox.DisableHitbox();
        }
    }
}

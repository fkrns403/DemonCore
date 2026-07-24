using State;
using UnityEngine;

/// <summary>
/// 회피 애니메이션 클립의 정확한 프레임에서 후속 입력 창, 무적 구간, 회피 종료를 PlayerMovement에 전달합니다.
/// 고정 초 타이머만으로 루트모션 상태를 종료해 이동값이 잘리는 문제를 방지합니다.
/// </summary>
public class PlayerDodgeAnimationEvents : MonoBehaviour
{
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField, Min(0f)] private float defaultInvulnerabilityDuration = 0.18f;

    private void Awake()
    {
        if (playerMovement == null)
        {
            playerMovement = GetComponentInParent<PlayerMovement>();
        }
    }

    public void AnimationEvent_OpenDodgeFollowUpWindow()
    {
        playerMovement?.OpenDodgeFollowUpWindow();
    }

    public void AnimationEvent_CloseDodgeFollowUpWindow()
    {
        playerMovement?.CloseDodgeFollowUpWindow();
    }

    public void AnimationEvent_BeginDodgeInvulnerability()
    {
        playerMovement?.BeginDodgeInvulnerability(defaultInvulnerabilityDuration);
    }

    public void AnimationEvent_EndDodgeInvulnerability()
    {
        playerMovement?.EndDodgeInvulnerability();
    }

    /// <summary>
    /// 기존 클립 호환용 일반 종료 이벤트입니다.
    /// 신규 세팅은 아래 타입별 종료 이벤트를 권장합니다.
    /// </summary>
    public void AnimationEvent_EndDodge()
    {
        playerMovement?.NotifyDodgeAnimationFinished();
    }

    public void AnimationEvent_EndBackstepDodge()
    {
        playerMovement?.NotifyDodgeAnimationFinished(DodgeType.Backstep);
    }

    public void AnimationEvent_EndSideLeftDodge()
    {
        playerMovement?.NotifyDodgeAnimationFinished(DodgeType.SideBackstepLeft);
    }

    public void AnimationEvent_EndSideRightDodge()
    {
        playerMovement?.NotifyDodgeAnimationFinished(DodgeType.SideBackstepRight);
    }

    public void AnimationEvent_EndForwardCounterDodge()
    {
        playerMovement?.NotifyDodgeAnimationFinished(DodgeType.ForwardCounterThrust);
    }

    public void AnimationEvent_EndDisengageDodge()
    {
        playerMovement?.NotifyDodgeAnimationFinished(DodgeType.Disengage);
    }
}

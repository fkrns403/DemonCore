using UnityEngine;

/// <summary>
/// 발도/납도 애니메이션의 특정 프레임에서 무기 표시를 직접 제어하고 싶을 때 사용합니다.
/// Animator가 붙은 모델 오브젝트에 붙이고 Animation Event로 호출합니다.
/// </summary>
public class PlayerWeaponAnimationEvents : MonoBehaviour
{
    [SerializeField] private PlayerWeaponVisibilityController weaponVisibilityController;

    private void Awake()
    {
        if (weaponVisibilityController == null)
        {
            weaponVisibilityController = GetComponentInParent<PlayerWeaponVisibilityController>();
        }
    }

    public void AnimationEvent_ShowWeapon()
    {
        if (weaponVisibilityController != null)
        {
            weaponVisibilityController.SetWeaponVisible(true);
        }
    }

    public void AnimationEvent_HideWeapon()
    {
        if (weaponVisibilityController != null)
        {
            weaponVisibilityController.SetWeaponVisible(false);
        }
    }
}

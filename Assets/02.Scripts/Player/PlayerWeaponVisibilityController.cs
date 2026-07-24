using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어 무기와 검집의 표시 상태를 관리합니다.
/// 무기 본과 오브젝트는 비활성화하지 않고, Renderer만 켜고 끄거나 디졸브 값을 변경합니다.
/// </summary>
public class PlayerWeaponVisibilityController : MonoBehaviour
{
    [Header("Weapon Visual Roots")]
    [SerializeField, Tooltip("전투 상태에서 보이게 할 무기/검집 오브젝트 루트입니다.")]
    private Transform[] weaponVisualRoots;

    [Header("Options")]
    [SerializeField, Tooltip("Awake에서 하위 Renderer와 Collider를 자동 수집합니다.")]
    private bool collectChildrenOnAwake = true;

    [SerializeField, Tooltip("게임 시작 시 무기와 검집을 숨깁니다.")]
    private bool hideOnAwake = true;

    [SerializeField, Tooltip("숨김 상태일 때 하위 Collider도 꺼줍니다.")]
    private bool disableCollidersWhenHidden = true;

    [SerializeField, Tooltip("표시 상태가 되었을 때 Collider도 같이 켭니다. 공격 히트박스는 Animation Event로 켜는 것을 권장하므로 보통 OFF입니다.")]
    private bool enableCollidersWhenVisible = false;

    [Header("Dissolve")]
    [SerializeField, Tooltip("디졸브 머티리얼 값을 사용할지 여부입니다.")]
    private bool useDissolveMaterial = false;

    [SerializeField, Tooltip("디졸브 셰이더 프로퍼티 이름입니다. 예: _DissolveAmount")]
    private string dissolvePropertyName = "_DissolveAmount";

    [SerializeField, Tooltip("보이는 상태의 디졸브 값입니다.")]
    private float visibleDissolveValue = 0f;

    [SerializeField, Tooltip("숨김 상태의 디졸브 값입니다.")]
    private float hiddenDissolveValue = 1f;

    [SerializeField, Tooltip("디졸브와 함께 Renderer.enabled도 같이 제어합니다.")]
    private bool alsoToggleRenderer = true;

    private readonly List<Renderer> weaponRenderers = new List<Renderer>();
    private readonly List<Collider> weaponColliders = new List<Collider>();

    private MaterialPropertyBlock propertyBlock;
    private int dissolvePropertyId;
    private bool isVisible;

    public bool IsVisible => isVisible;

    private void Awake()
    {
        // Unity 엔진 객체는 필드 초기화에서 만들면 안 됩니다.
        // 반드시 Awake 또는 Start에서 생성해야 합니다.
        propertyBlock = new MaterialPropertyBlock();
        dissolvePropertyId = Shader.PropertyToID(dissolvePropertyName);

        if (collectChildrenOnAwake)
        {
            CollectWeaponParts();
        }

        if (hideOnAwake)
        {
            SetWeaponVisible(false, true);
        }
    }

    /// <summary>
    /// 등록된 무기 루트 아래의 Renderer와 Collider를 수집합니다.
    /// </summary>
    private void CollectWeaponParts()
    {
        weaponRenderers.Clear();
        weaponColliders.Clear();

        if (weaponVisualRoots == null)
        {
            return;
        }

        for (int i = 0; i < weaponVisualRoots.Length; i++)
        {
            Transform root = weaponVisualRoots[i];

            if (root == null)
            {
                continue;
            }

            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            Collider[] colliders = root.GetComponentsInChildren<Collider>(true);

            for (int r = 0; r < renderers.Length; r++)
            {
                if (!weaponRenderers.Contains(renderers[r]))
                {
                    weaponRenderers.Add(renderers[r]);
                }
            }

            for (int c = 0; c < colliders.Length; c++)
            {
                if (!weaponColliders.Contains(colliders[c]))
                {
                    weaponColliders.Add(colliders[c]);
                }
            }
        }
    }

    /// <summary>
    /// 무기와 검집을 보이거나 숨깁니다.
    /// GameObject 자체는 끄지 않고 Renderer와 Collider만 제어합니다.
    /// </summary>
    public void SetWeaponVisible(bool visible)
    {
        SetWeaponVisible(visible, false);
    }

    /// <summary>
    /// 무기와 검집을 보이거나 숨깁니다.
    /// </summary>
    public void SetWeaponVisible(bool visible, bool force)
    {
        if (!force && isVisible == visible)
        {
            return;
        }

        isVisible = visible;

        ApplyRendererVisibility(visible);
        ApplyColliderVisibility(visible);
    }

    /// <summary>
    /// Renderer 표시 상태와 디졸브 값을 적용합니다.
    /// </summary>
    private void ApplyRendererVisibility(bool visible)
    {
        float dissolveValue = visible ? visibleDissolveValue : hiddenDissolveValue;

        for (int i = 0; i < weaponRenderers.Count; i++)
        {
            Renderer targetRenderer = weaponRenderers[i];

            if (targetRenderer == null)
            {
                continue;
            }

            if (useDissolveMaterial)
            {
                targetRenderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetFloat(dissolvePropertyId, dissolveValue);
                targetRenderer.SetPropertyBlock(propertyBlock);
            }

            if (alsoToggleRenderer)
            {
                targetRenderer.enabled = visible;
            }
        }
    }

    /// <summary>
    /// 무기 하위 Collider 표시 상태를 제어합니다.
    /// 공격 판정용 Collider는 보통 Animation Event에서 따로 켜는 것이 안전합니다.
    /// </summary>
    private void ApplyColliderVisibility(bool visible)
    {
        if (visible && !enableCollidersWhenVisible)
        {
            return;
        }

        if (!visible && !disableCollidersWhenHidden)
        {
            return;
        }

        for (int i = 0; i < weaponColliders.Count; i++)
        {
            Collider targetCollider = weaponColliders[i];

            if (targetCollider == null)
            {
                continue;
            }

            targetCollider.enabled = visible;
        }
    }

    /// <summary>
    /// Animation Event에서 무기 표시를 켤 때 사용합니다.
    /// </summary>
    public void ShowWeapon()
    {
        SetWeaponVisible(true);
    }

    /// <summary>
    /// Animation Event에서 무기 표시를 끌 때 사용합니다.
    /// </summary>
    public void HideWeapon()
    {
        SetWeaponVisible(false);
    }
}
using UnityEngine;

/// <summary>
/// 공격자가 피격 대상에게 전달하는 데미지 정보입니다.
/// 데미지, 공격자, 피격 위치, 공격 방향, 패링/가드 가능 여부를 포함합니다.
/// </summary>
public struct DamageInfo
{
    public int Damage;
    public Transform Attacker;
    public Vector3 HitPoint;
    public Vector3 AttackDirection;
    public bool IsParryable;
    public bool IsGuardable;

    public DamageInfo(
        int damage,
        Transform attacker,
        Vector3 hitPoint,
        Vector3 attackDirection,
        bool isParryable,
        bool isGuardable)
    {
        Damage = damage;
        Attacker = attacker;
        HitPoint = hitPoint;
        AttackDirection = attackDirection;
        IsParryable = isParryable;
        IsGuardable = isGuardable;
    }
}

/// <summary>
/// 데미지를 받을 수 있는 대상이 구현하는 인터페이스입니다.
/// 플레이어와 적 모두 같은 방식으로 데미지를 받을 수 있게 합니다.
/// </summary>
public interface IDamageable
{
    void TakeDamage(DamageInfo damageInfo);
}

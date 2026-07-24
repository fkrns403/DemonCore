/// <summary>
/// 와이어가 수행하는 역할을 구분합니다.
/// 이동용, 전투 접근용, 보스 제압용을 하나의 bool 조합으로 섞지 않고 명시적인 타입으로 분리합니다.
/// </summary>
public enum WireActionType
{
    Traverse = 0,
    PullToPoint = 1,
    Rope = 2,
    CombatApproach = 3,
    Suppression = 4
}

namespace State
{
    /// <summary>
    /// 적 AI의 기본 상태입니다.
    /// </summary>
    public enum EnemyState
    {
        Idle = 0,
        Dormant = 1,
        Awaken = 2,
        Chase = 3,
        Attack = 4,
        Hit = 5,
        Return = 6,
        SelfDestruct = 7,
        Dead = 8
    }
}

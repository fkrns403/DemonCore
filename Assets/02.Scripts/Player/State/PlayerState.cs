namespace State
{
    /// <summary>
    /// 플레이어의 현재 행동 상태입니다.
    /// 코드에서 상태 우선순위를 판단할 때 사용합니다.
    /// </summary>
    public enum PlayerState
    {
        Idle = 0,
        Move = 1,
        Jump = 2,
        Fall = 3,
        Dodge = 4,
        Attack = 5,
        Guard = 6,
        Parry = 7,
        Hit = 8,
        Knockback = 9,
        Knockdown = 10,
        GetUp = 11,
        Interact = 12,
        WireMove = 13,
        Scan = 14,
        Dead = 15
    }
}

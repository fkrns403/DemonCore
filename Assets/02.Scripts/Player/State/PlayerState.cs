namespace State
{
    public enum PlayerState
    {
        Idle,
        // 대기
        Move,
        // 이동
        Jump,
        // 점프
        Fall,
        // 낙하
        Dodge,
        // 회피
        Attack,
        // 공격
        Guard,
        // 방어
        Parry,
        // 패링
        Hit,
        // 피격
        Knockback,
        // 넉백
        Knockdown,
        // 넉다운
        GetUp,
        // 기상
        Interact,
        // 상호작용
        WireMove,
        // 와이어 이동
        Scan,
        // 스켄
        Dead
        // 사망
    }
}
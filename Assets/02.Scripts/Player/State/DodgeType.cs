namespace State
{
    /// <summary>
    /// 플레이어 회피 종류입니다.
    /// Animator Int 파라미터와 연결되므로 숫자를 명시합니다.
    /// </summary>
    public enum DodgeType
    {
        None = 0,

        /// <summary>기본 후방 회피입니다.</summary>
        Backstep = 1,

        /// <summary>왼쪽 측면 백스텝입니다.</summary>
        SideBackstepLeft = 2,

        /// <summary>오른쪽 측면 백스텝입니다.</summary>
        SideBackstepRight = 3,

        /// <summary>백스텝 후 앞으로 재진입하는 찌르기 카운터입니다.</summary>
        ForwardCounterThrust = 4,

        /// <summary>S + 회피로 발동하는 전투 이탈기입니다.</summary>
        Disengage = 5
    }
}

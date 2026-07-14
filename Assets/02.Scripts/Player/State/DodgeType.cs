namespace State
{
    public enum DodgeType
    {
        None,
        Backstep,
        // 기본 회피
        SideBackstepLeft,
        // 백스텝 사이드 왼쪽
        SideBackstepRight,
        // 백스텝 사이드 오른쪽
        ForwardCounterThrust,
        // 백스텝 후 앞으로 재진입하는 찌르기 카운터
        Disengage
        // 백스텝후 이탈
    }
}
/// <summary>
/// 와이어 이동이 끝난 이유입니다.
/// HUD, 사운드, 재사용 대기시간, 낙하 전환을 동일한 문자열 로그가 아니라 타입으로 처리하기 위해 사용합니다.
/// </summary>
public enum WireMoveEndReason
{
    Completed = 0,
    Released = 1,
    Obstructed = 2,
    Interrupted = 3,
    InvalidTarget = 4,
    Timeout = 5
}

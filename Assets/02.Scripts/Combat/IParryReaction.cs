using UnityEngine;

/// <summary>
/// 패링을 당했을 때 반응할 수 있는 대상이 구현합니다.
/// 적 공격 중단, 경직, 넉백 등을 연결할 수 있습니다.
/// </summary>
public interface IParryReaction
{
    void OnParried(Transform defender);
}

using UnityEngine;

/// <summary>
/// PlayerWireController가 선택한 와이어 목표를 PlayerMovement에 전달하는 요청 값입니다.
/// 이동 코드는 목표를 다시 탐색하지 않고 이 요청만 실행합니다.
/// </summary>
public readonly struct WireMoveRequest
{
    public WireActionType ActionType { get; }
    public WireActionProfile Profile { get; }
    public Transform AnchorTransform { get; }
    public Transform DestinationTransform { get; }
    public Transform RopeTopTransform { get; }
    public Transform RopeBottomTransform { get; }
    public Vector3 StaticDestination { get; }

    public WireMoveRequest(
        WireActionType actionType,
        WireActionProfile profile,
        Transform anchorTransform,
        Transform destinationTransform,
        Transform ropeTopTransform,
        Transform ropeBottomTransform,
        Vector3 staticDestination)
    {
        ActionType = actionType;
        Profile = profile;
        AnchorTransform = anchorTransform;
        DestinationTransform = destinationTransform;
        RopeTopTransform = ropeTopTransform;
        RopeBottomTransform = ropeBottomTransform;
        StaticDestination = staticDestination;
    }

    public Vector3 AnchorPosition => AnchorTransform != null
        ? AnchorTransform.position
        : StaticDestination;

    public Vector3 Destination => DestinationTransform != null
        ? DestinationTransform.position
        : StaticDestination;

    public Vector3 RopeTopPosition => RopeTopTransform != null
        ? RopeTopTransform.position
        : AnchorPosition;

    public Vector3 RopeBottomPosition => RopeBottomTransform != null
        ? RopeBottomTransform.position
        : StaticDestination;
}

using UnityEngine;

/// <summary>
/// 와이어 액션의 탐색 조건과 이동 수치를 보관하는 데이터 에셋입니다.
/// 코드 수정 없이 탐색용/전투용/로프용 밸런스를 따로 조정할 수 있습니다.
/// </summary>
[CreateAssetMenu(
    fileName = "WireActionProfile",
    menuName = "Demon Core/Wire/Wire Action Profile")]
public class WireActionProfile : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private WireActionType actionType = WireActionType.Traverse;

    [Header("Target Search")]
    [SerializeField, Min(0f)] private float minRange = 0f;
    [SerializeField, Min(0.1f)] private float maxRange = 22f;
    [SerializeField, Range(1f, 180f)] private float maxTargetAngle = 35f;
    [SerializeField] private bool requireLandingPoint = true;

    [Header("Travel")]
    [SerializeField, Min(0.1f)] private float moveSpeed = 18f;
    [SerializeField, Min(0.1f)] private float acceleration = 45f;
    [SerializeField, Min(0f)] private float startDelay = 0.12f;
    [SerializeField, Min(0.01f)] private float stopDistance = 1.2f;
    [SerializeField, Min(0f)] private float decelerationDistance = 3.6f;
    [SerializeField, Range(0.05f, 1f)] private float minimumDecelerationRatio = 0.25f;
    [SerializeField, Min(0.1f)] private float maximumTravelTime = 3f;

    [Header("Rope")]
    [SerializeField, Min(0.1f)] private float ropeAscendSpeed = 4.5f;
    [SerializeField, Min(0.1f)] private float ropeDescendSpeed = 5.5f;
    [SerializeField, Min(0.1f)] private float ropeFastDescendSpeed = 8f;
    [SerializeField, Min(0f)] private float ropeWallClearance = 0.5f;

    [Header("Cooldown")]
    [SerializeField, Min(0f)] private float successCooldown = 0.15f;
    [SerializeField, Min(0f)] private float failureCooldown = 0.7f;

    public WireActionType ActionType => actionType;
    public float MinRange => minRange;
    public float MaxRange => maxRange;
    public float MaxTargetAngle => maxTargetAngle;
    public bool RequireLandingPoint => requireLandingPoint;
    public float MoveSpeed => moveSpeed;
    public float Acceleration => acceleration;
    public float StartDelay => startDelay;
    public float StopDistance => stopDistance;
    public float DecelerationDistance => decelerationDistance;
    public float MinimumDecelerationRatio => minimumDecelerationRatio;
    public float MaximumTravelTime => maximumTravelTime;
    public float RopeAscendSpeed => ropeAscendSpeed;
    public float RopeDescendSpeed => ropeDescendSpeed;
    public float RopeFastDescendSpeed => ropeFastDescendSpeed;
    public float RopeWallClearance => ropeWallClearance;
    public float SuccessCooldown => successCooldown;
    public float FailureCooldown => failureCooldown;
}

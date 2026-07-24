# DEMON CORE Scripts Review Patch v7

## 1. 이번 검토에서 확인한 전진 카운터 문제

v6의 전진 카운터 흐름은 다음과 같았습니다.

1. 백스텝 중 공격 입력
2. `TryStartDodgeFollowUp(ForwardCounterThrust)` 성공
3. 코드의 `DodgeType`만 4로 변경
4. `PlayerAnimatorController`는 Int 값만 갱신
5. 고정 `forwardCounterDuration` 타이머가 감소
6. 타이머가 끝나면 `isDodging = false`
7. 이후 전달되는 Animator Root Motion은 `isDodging == false`라서 무시

따라서 아래 두 문제가 동시에 생길 수 있었습니다.

- 백스텝 상태에서 전진 카운터 상태로 즉시 넘어갈 전용 Trigger가 없음
- Animator 전환이 늦으면 전진 찌르기 클립이 시작되기 전에 회피 타이머가 먼저 끝남

v7에서는 다음과 같이 변경했습니다.

- Animator 파라미터 `DodgeFollowUpTrigger` 추가
- 백스텝 → 전진 카운터 전환을 전용 Trigger로 요청
- `PlayerDodgeAnimationEvents` 추가
- 회피 종료를 클립 마지막 Animation Event에서 통지 가능
- 이벤트 사용 시 기존 클립 길이 타이머 대신 2초 안전 타임아웃만 유지
- `PlayerController → PlayerAnimatorController → Animator 평가` 순서가 보장되도록 실행 순서 지정
- 전진 카운터가 시작됐는데 Root Motion XZ가 0이면 Console 경고 출력
- 전진 카운터의 애니메이션 이동량은 유지하면서 현재 플레이어 정면으로 방향 보정 가능

---

## 2. 전진 카운터 Animator 필수 세팅

### Animator Parameter

다음 파라미터를 추가합니다.

| 이름 | 타입 | 용도 |
|---|---|---|
| `DodgeTrigger` | Trigger | 최초 회피 진입 |
| `DodgeType` | Int | 회피 종류 선택 |
| `DodgeFollowUpTrigger` | Trigger | 백스텝 후 전진 카운터 진입 |

`DodgeType` 값은 다음과 같습니다.

| 값 | 타입 |
|---:|---|
| 1 | Backstep |
| 2 | SideBackstepLeft |
| 3 | SideBackstepRight |
| 4 | ForwardCounterThrust |
| 5 | Disengage |

### Backstep → ForwardCounterThrust Transition

- From: 백스텝 상태
- To: 전진 카운터 찌르기 상태
- Has Exit Time: OFF
- Fixed Duration: ON 권장
- Transition Duration: 0.03~0.08
- Conditions:
  - `DodgeFollowUpTrigger`
  - 필요하면 `DodgeType == 4` 추가

`Any State → ForwardCounterThrust`보다 백스텝 상태에서만 넘어가게 만드는 편이 안전합니다.

### 전진 카운터 클립 Import

- Loop Time: OFF
- Root Transform Rotation / Bake Into Pose: ON 권장
- Root Transform Position Y / Bake Into Pose: ON
- Root Transform Position XZ / Bake Into Pose: **OFF**
- Mirror: OFF

XZ를 Bake Into Pose로 켜면 찌르기 애니메이션의 전진 이동량이 `Animator.deltaPosition`에 나오지 않습니다.

### 레이어 위치

전진 카운터는 반드시 Base Layer의 전신 회피 트리에 둡니다.

- UpperBodyCombat Layer에 두지 않기
- UpperBodyMask를 적용한 레이어에 두지 않기
- 하체와 루트가 포함된 전신 상태로 재생

---

## 3. 회피 Animation Event 세팅

캐릭터 모델의 Animator 오브젝트에 다음 컴포넌트를 추가합니다.

- `PlayerRootMotionRelay`
- `PlayerDodgeAnimationEvents`

### Backstep 클립 권장 이벤트

| 정규화 시점 예시 | 이벤트 |
|---:|---|
| 0.00~0.05 | `AnimationEvent_BeginDodgeInvulnerability` |
| 0.18~0.25 | `AnimationEvent_OpenDodgeFollowUpWindow` |
| 0.65~0.80 | `AnimationEvent_CloseDodgeFollowUpWindow` |
| 0.95~0.99 | `AnimationEvent_EndBackstepDodge` |

전환이 발생하면 Backstep 마지막 이벤트까지 재생되지 않고 ForwardCounter로 넘어가므로, ForwardCounter 클립에도 종료 이벤트가 필요합니다.

### ForwardCounterThrust 클립 권장 이벤트

| 정규화 시점 예시 | 이벤트 |
|---:|---|
| 0.95~0.99 | `AnimationEvent_EndForwardCounterDodge` |

좌/우 회피와 전투 이탈 클립도 각각 `AnimationEvent_EndSideLeftDodge`, `AnimationEvent_EndSideRightDodge`, `AnimationEvent_EndDisengageDodge`를 마지막에 배치합니다. 타입별 이벤트는 전환 중 이전 클립의 종료 이벤트가 뒤늦게 들어와 새 동작을 끊는 문제를 막습니다.

공격 판정은 기존 `PlayerCombatAnimationEvents`의 카운터 히트박스 이벤트를 사용합니다.

- 찌르기 유효 프레임: `AnimationEvent_EnableParryCounterHitbox`
- 유효 프레임 종료: `AnimationEvent_DisablePlayerHitbox`

현재 전진 회피 카운터와 패링 반격이 같은 찌르기 클립을 공유하더라도, 상태와 Trigger는 분리하는 편이 좋습니다.

---

## 4. PlayerMovement 권장 Inspector 값

### Dodge Rules

- Dodge Stamina Cost: 15
- Dodge Cooldown: 0.35
- Default Dodge Invulnerability Duration: 0.18
- Use Animation Event Dodge End: ON
- Dodge Safety Timeout: 2.0
- Require Animation Event Follow Up Window:
  - 이벤트 배치 전: OFF
  - 이벤트 배치 완료 후: ON
- Fallback Follow Up Window Start: 0.08
- Fallback Follow Up Window End: 0.42

### Dodge Root Motion

- Use Dodge Root Motion: ON
- Use Dodge Root Rotation: ON
- Align Forward Counter Root Motion To Facing: ON
- Backstep Multiplier: 1.0
- Side Backstep Multiplier: 1.15~1.35
- Forward Counter Multiplier: 1.0부터 테스트
- Disengage Multiplier: 1.0

전진 거리가 부족하면 먼저 클립 자체의 XZ Root Motion이 존재하는지 로그로 확인한 뒤 `Forward Counter Root Motion Multiplier`를 조절합니다.

---

## 5. Root Motion 확인 절차

1. 모델 Animator 오브젝트에 `PlayerRootMotionRelay`가 있는지 확인
2. `OnAnimatorMove`가 직접 Root Motion을 처리하므로 Animator의 Apply Root Motion 체크값을 런타임에서 강제로 변경하지 않기
3. `PlayerRootMotionRelay / Show Root Motion Log` ON
4. 백스텝 후 공격 입력
5. Console에서 `ForwardCounterThrust`의 Position XZ 값 확인

### 정상

예시:

```text
RootMotion [ForwardCounterThrust] Position=(0.00, 0.00, 0.08)
```

프레임마다 XZ 값이 누적되어 부모 CharacterController가 앞으로 이동합니다.

### 비정상: Position이 계속 0

다음 순서로 확인합니다.

1. 클립 XZ Bake Into Pose가 OFF인지
2. 클립 원본에 실제 루트 이동이 있는지
3. ForwardCounter 상태가 Base Layer 전신 상태인지
4. `DodgeFollowUpTrigger` 전환이 실제로 발생했는지
5. Animator 오브젝트에 `PlayerRootMotionRelay`가 있는지
6. Animator가 붙은 모델 자식이 아니라 Player 부모에 Relay를 붙이지 않았는지

---

## 6. 새로 추가된 회피 의존 시스템

### PlayerStamina

기획서의 회피 비용 15, 최대 스태미나 120, 회복 지연 HUD를 지원합니다.

Player 루트에 추가:

- Max Stamina: 120
- Regeneration Per Second: 28부터 테스트
- Regeneration Delay: 0.8

HUD는 `StaminaChanged(float current, float max)` 이벤트를 구독합니다.

### PlayerInvulnerability

회피 무적 시간을 PlayerHealth에서 최종 검사합니다.

Player 루트에 추가하고 다음 연결을 확인합니다.

- PlayerMovement / Player Invulnerability
- PlayerHealth / Invulnerability

Animation Event를 사용하면 실제 몸이 공격 궤적을 통과하는 구간에 맞춰 무적 시작/종료를 조절할 수 있습니다.

### 실행 순서

- `PlayerController`: `DefaultExecutionOrder(-100)`
- `PlayerAnimatorController`: `DefaultExecutionOrder(-50)`

입력과 전진 카운터 요청을 먼저 확정한 뒤 같은 프레임의 Animator 평가 전에 Trigger를 전달하도록 `PlayerAnimatorController`는 `Update`에서 동작합니다. 기존처럼 `LateUpdate`에서 Trigger를 보내면 전환 요청이 다음 애니메이션 평가까지 밀려 백스텝 Root Motion과 전진 카운터 상태가 한 프레임 어긋날 수 있습니다.

### PlayerDefense 가드 스태미나

- Guard Stamina Cost: 15부터 테스트
- 가드 성공 1회마다 `PlayerStamina.TrySpend` 호출
- 스태미나가 부족하면 가드가 성립하지 않고 `GuardBroken` 이벤트 발생
- HUD/가드 브레이크 애니메이션은 `GuardBroken` 이벤트를 구독

### PlayerHealth

- 기본 maxHealth를 기획서 기준 1000으로 변경
- `HealthChanged` 이벤트 추가
- 무적 상태에서는 피해 무시
- 피격 시 보스 제압 와이어 링크 해제

기존 씬에 저장된 `maxHealth=100` 값은 코드 기본값을 바꿔도 자동으로 1000이 되지 않을 수 있으므로 Inspector에서 직접 확인해야 합니다.

---

## 7. 와이어 구조 변경

v6는 모든 와이어를 하나의 `WireAnchor + 직선 이동`으로 처리했습니다.

v7에서는 역할을 분리했습니다.

| 타입 | 역할 |
|---|---|
| Traverse | 끊긴 길 고속 횡단 |
| PullToPoint | 벽면 포인트 짧은 끌어당김 |
| Rope | 로프식 상승/하강 |
| CombatApproach | 적에게 짧게 접근 |
| Suppression | 보스 부위 제압 연결, 플레이어 이동 없음 |

### 책임 분리

- `PlayerWireController`
  - 후보 탐색 (`OverlapSphereNonAlloc` 버퍼 사용)
  - 공격/회피/가드/설치 상태에서는 발사 차단, 후보 HUD는 유지
  - 시야각/거리/장애물 판정
  - 프로필 선택
  - 이동/제압 요청
  - 성공/실패 쿨타임
- `PlayerMovement`
  - 실제 CharacterController 이동
  - 가속/감속
  - 충돌 중단
  - 로프 상승/하강
- `WireLinePresenter`
  - 오른쪽 손목 사출점과 목표 사이 LineRenderer 표시
- `WireAnchor`
  - 월드 와이어 포인트 데이터
  - 액션 타입, 착지점, 로프 상하단, 보스 노출 상태
- `WireActionProfile`
  - 타입별 밸런스 데이터 ScriptableObject

---

## 8. WireActionProfile 생성

Project 창에서:

```text
Create
→ Demon Core
→ Wire
→ Wire Action Profile
```

타입별로 5개를 만듭니다.

### 1) Wire_Traversal

- Action Type: Traverse
- Min Range: 0
- Max Range: 22
- Max Target Angle: 35
- Require Landing Point: ON
- Move Speed: 18
- Acceleration: 45
- Start Delay: 0.15
- Stop Distance: 1.2
- Deceleration Distance: 3.6
- Maximum Travel Time: 3.0
- Success Cooldown: 0.15
- Failure Cooldown: 0.7

### 2) Wire_PullToPoint

- Action Type: PullToPoint
- Max Range: 18
- Max Target Angle: 35
- Require Landing Point: OFF 또는 실제 발판이 있으면 ON
- Move Speed: 14
- Acceleration: 40
- Start Delay: 0.25
- Stop Distance: 1.0
- Deceleration Distance: 2.8
- Maximum Travel Time: 2.5

### 3) Wire_Rope

- Action Type: Rope
- Max Range: 22
- Max Target Angle: 35
- Require Landing Point: OFF
- Move Speed: 8~12 (수평 줄 정렬 보정용)
- Rope Ascend Speed: 4.5
- Rope Descend Speed: 5.5
- Rope Fast Descend Speed: 8.0
- Rope Wall Clearance: 0.5

### 4) Wire_CombatApproach

- Action Type: CombatApproach
- Min Range: 5
- Max Range: 12
- Max Target Angle: 35
- Require Landing Point: OFF
- Move Speed: 16
- Acceleration: 55
- Start Delay: 0.12
- Stop Distance: 1.8~2.2
- Deceleration Distance: 2.5
- Maximum Travel Time: 1.5

### 5) Wire_Suppression

- Action Type: Suppression
- Max Range: 14~16
- Max Target Angle: 35
- Require Landing Point: OFF
- Success Cooldown: 0.15
- Failure Cooldown: 0.7

이동 수치는 Suppression 타입에서는 사용하지 않습니다.

생성한 5개 프로필을 Player 루트의 `PlayerWireController / Action Profiles` 배열에 모두 넣습니다.

---

## 9. WireAnchor 타입별 세팅

### Traverse

오브젝트 구성 예시:

```text
WP_Traversal_01
├─ AnchorPoint
└─ LandingPoint
```

- Action Type: Traverse
- Anchor Point: AnchorPoint
- Landing Point: LandingPoint
- Is Available: ON

### PullToPoint

```text
WP_WallPull_01
├─ AnchorPoint
└─ LandingPoint (선택)
```

발판까지 확실하게 보내려면 LandingPoint를 지정합니다. 목표 근처에서 낙하시키려면 비워둘 수 있습니다.

### Rope

```text
WP_Rope_01
├─ AnchorPoint
├─ RopeTopPoint
└─ RopeBottomPoint
```

Rope 타입은 RopeTopPoint와 RopeBottomPoint가 둘 다 필요합니다.

- W: 상승
- S: 하강
- Shift 유지: 빠른 하강
- Space: 와이어 해제 후 공중 상태

### CombatApproach

적의 가슴/몸통 쪽에 WireAnchor를 둡니다.

- Action Type: CombatApproach
- Anchor Point: 적 몸 중심
- Landing Point: 비워도 됨
- 대상 Collider는 Anchor Mask 레이어에 포함

적이 움직이면 AnchorPoint Transform도 움직이므로 도착점이 실시간 갱신됩니다.

---

## 10. 오른손목 와이어 사출점

계층 예시:

```text
RightHand 또는 RightForeArm
└─ WireDevice_Right
   └─ WireStart_RightWrist
```

`WireStart_RightWrist`를 사출구 끝에 배치하고 `WireLinePresenter / Wire Start Point`에 연결합니다.

LineRenderer는 별도 자식 오브젝트에 두는 것을 권장합니다.

```text
Player
├─ CharacterModel
├─ WireLine
│  ├─ LineRenderer
│  └─ WireLinePresenter
└─ ...
```

LineRenderer 권장:

- Position Count: 코드에서 2로 설정
- Use World Space: ON
- Width: 0.01~0.025부터 테스트
- Cast Shadows: OFF
- Receive Shadows: OFF
- 비활성 시작

---

## 11. 보스 제압 와이어 세팅

### 보스 루트

추가 컴포넌트:

- `BossSuppressionController`
- `BossParryReaction`

BossSuppressionController:

- Phase One Required Anchors: 2
- Phase Two Required Anchors: 4
- Base Connection Duration: 4
- Anchor Connection Bonus: 2

### 보스 약점 부위

```text
Boss
└─ WeakPart_Arm_R
   └─ WirePoint
```

WirePoint의 `WireAnchor`:

- Action Type: Suppression
- Requires Exposure: ON
- Suppression Owner: 보스 루트의 BossSuppressionController

BossParryReaction:

- Weak Point Exposure Duration: 1.5
- Suppression Weak Points: 보스 약점 WireAnchor 배열

패링 성공 시 `IParryReaction.OnParried`가 호출되고 약점이 1.5초 노출됩니다.

### 바닥/벽 앵커 지점

각 설치 지점에:

- Collider
- 전용 AnchorPoint Layer
- `SuppressionAnchorPoint`

Player 루트에:

- `PlayerAnchorInstaller`

PlayerAnchorInstaller:

- Search Radius: 2.5
- Install Duration: 0.8
- Anchor Point Mask: 설치 지점 레이어

보스 제압 링크가 살아 있고 지점 2.5m 안에서 F를 유지하면 설치됩니다.

현재 구현 규칙:

- 1페이즈 2개
- 2페이즈 4개
- 플레이어 피격/시간 종료/보스 절단 호출 시 현재 페이즈 앵커 초기화
- 조건 충족 시 PhaseChanged 또는 SuppressionCompleted 이벤트 발생

실제 보스 Animator, 패턴 변경, 코어 노출은 이 이벤트를 구독해 연결합니다.

---

## 12. Player 루트 최종 컴포넌트

```text
Player
- CharacterController
- PlayerInputReader
- PlayerController
- PlayerMovement
- PlayerAnimatorController
- PlayerLockOnController
- PlayerWireController
- PlayerAnchorInstaller
- PlayerStamina
- PlayerInvulnerability
- PlayerHealth
- PlayerDefense
```

모델 자식:

```text
CharacterModel
- Animator
- PlayerRootMotionRelay
- PlayerDodgeAnimationEvents
- PlayerCombatAnimationEvents
- PlayerWeaponAnimationEvents
```

와이어 표시 자식:

```text
WireLine
- LineRenderer
- WireLinePresenter
```

---

## 13. Layer 권장

- Player
- Enemy
- LockOnTarget
- WirePoint
- WireObstacle
- SuppressionAnchorPoint

PlayerWireController:

- Anchor Candidate Capacity: 32부터 시작
- Anchor Mask: WirePoint
- Obstacle Mask: Default + Environment 등 실제 벽 레이어
- Player/Enemy Trigger는 Obstacle Mask에서 제외 권장

PlayerAnchorInstaller:

- Anchor Point Mask: SuppressionAnchorPoint

---

## 14. 테스트 순서

### 전진 카운터

1. Root Motion 로그 ON
2. Shift 단독으로 백스텝
3. 후속 창에서 좌클릭
4. Animator가 ForwardCounterThrust 상태로 즉시 전환되는지 확인
5. Console XZ delta 확인
6. Player 부모 Transform/CharacterController가 이동하는지 확인
7. 클립 종료 후 이동/공격으로 복귀하는지 확인

### 회피 제한

1. 스태미나 15 미만에서 회피 불가
2. 0.35초 안에 두 번째 회피 불가
3. 무적 0.18초에 공격이 닿으면 HP 감소 없음
4. 무적 종료 후 공격이 닿으면 HP 감소

### 횡단 와이어

1. 22m 이내, 화면 중심 35도 이내 포인트 조준
2. Q 입력
3. 사출 지연 후 18m/s 가속
4. 도착 전 감속
5. LandingPoint에서 종료
6. 중간 벽 충돌 시 Obstructed 종료

### 로프

1. Q로 Rope 타입 연결
2. W 상승 / S 하강
3. Shift+S 빠른 하강
4. Space 해제 후 Fall 상태

### 보스 제압

1. 보스 패링 성공
2. 약점 1.5초 노출
3. Q 연결
4. 설치 지점 2.5m 접근
5. F 0.8초 유지
6. 1페이즈 2개 후 PhaseChanged(2)
7. 2페이즈 4개 후 SuppressionCompleted
8. 연결 중 피격 시 링크와 현재 페이즈 앵커 초기화

---

## 15. 이번 패치에서 의도적으로 구현하지 않은 독립 시스템

다음은 상세 기획서에 필요하지만 회피/와이어 수정과 직접 결합하면 한 번에 오류 범위가 지나치게 커지는 독립 모듈이라 이번 v7에는 포함하지 않았습니다.

- Scan Registry / Scan Marker Pool / 암전 UI
- 일반 상호작용 공통 인터페이스
- 보급 상자/인벤토리/퀵슬롯
- SaveData/Checkpoint/Shelter
- 강화 데이터베이스와 강화 UI
- 보스 패턴 전체 상태 머신

대신 v7은 이후 HUD와 보스 로직이 구독할 수 있도록 다음 이벤트 기반을 추가했습니다.

- PlayerHealth.HealthChanged
- PlayerHealth.Damaged
- PlayerHealth.Died
- PlayerStamina.StaminaChanged
- PlayerDefense.GuardBroken
- PlayerWireController.CandidateChanged
- PlayerWireController.WireActionStarted
- PlayerWireController.WireActionEnded
- PlayerAnchorInstaller.InstallProgressChanged
- BossSuppressionController.AnchorCountChanged
- BossSuppressionController.PhaseChanged
- BossSuppressionController.SuppressionCompleted

이 이벤트를 기준으로 UI를 연결하면 게임 규칙 코드가 Slider, Image, Text 같은 UI 컴포넌트를 직접 참조하지 않게 됩니다.

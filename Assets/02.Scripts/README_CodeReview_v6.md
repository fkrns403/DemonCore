# DEMON CORE Scripts Review Patch v6

## 이번 패치에서 수정한 핵심

### PlayerAnimatorController.cs
- 첫 공격 입력 시 `WeaponReadyTrigger`만 실행되고 `AttackTrigger`가 실행되지 않던 흐름을 수정했습니다.
- 이제 첫 공격도 무기 표시/발도 준비와 동시에 UpperBodyCombat 공격 레이어가 재생될 수 있습니다.
- E 가드/패링 입력 시에도 `WeaponReadyTrigger`를 보내서 Base Layer가 `SM_20_CombatReady`로 들어갈 수 있게 했습니다.
- `IsWeaponReady`를 public getter로 열어 디버깅하기 쉽게 했습니다.

### PlayerController.cs
- 약공격 기본 락 시간을 `0.35`에서 `0.65`로 조정했습니다.
- 콤보/상체 공격 전환이 너무 빨리 끊기지 않게 하기 위한 기본값입니다.

### PlayerAttackHitbox.cs
- 애니메이션 이벤트에서 공격별 데미지를 바꿀 수 있도록 `SetDamage`, `EnableHitbox(int damage)`를 추가했습니다.
- 오브젝트 비활성화 시 히트박스가 남아 있지 않도록 `OnDisable`에서 강제로 끄도록 했습니다.

### PlayerCombatAnimationEvents.cs
- 공격별 데미지 이벤트를 추가했습니다.
- 사용할 수 있는 이벤트 예시:
  - `AnimationEvent_EnableLightAttack1Hitbox`
  - `AnimationEvent_EnableLightAttack2Hitbox`
  - `AnimationEvent_EnableLightAttack3Hitbox`
  - `AnimationEvent_EnableHeavyAttackHitbox`
  - `AnimationEvent_EnableAirAttackHitbox`
  - `AnimationEvent_EnableParryCounterHitbox`
  - `AnimationEvent_DisablePlayerHitbox`

### EnemyAttackHitbox.cs / EnemyCombatAnimationEvents.cs
- 적 공격도 애니메이션 이벤트에서 데미지를 지정할 수 있게 했습니다.
- 히트박스가 켜진 상태로 남는 것을 방지하기 위해 `OnDisable`에서 끕니다.

### EnemyHealth.cs
- 피격 시 `HitTrigger`, 사망 시 `DeadTrigger`를 Animator에 전달하도록 했습니다.
- 사망 시 `NavMeshAgent`와 `BaseEnemyController`를 끌 수 있는 옵션을 추가했습니다.

## Animator에서 필요한 이벤트 예시

### LightAttack_1
- 타격 시작 프레임: `AnimationEvent_EnableLightAttack1Hitbox`
- 타격 종료 프레임: `AnimationEvent_DisablePlayerHitbox`

### LightAttack_2
- 타격 시작 프레임: `AnimationEvent_EnableLightAttack2Hitbox`
- 타격 종료 프레임: `AnimationEvent_DisablePlayerHitbox`

### LightAttack_3
- 타격 시작 프레임: `AnimationEvent_EnableLightAttack3Hitbox`
- 타격 종료 프레임: `AnimationEvent_DisablePlayerHitbox`

### Parry Counter
- 찌르기 시작 프레임: `AnimationEvent_EnableParryCounterHitbox`
- 찌르기 종료 프레임: `AnimationEvent_DisablePlayerHitbox`

## Inspector 체크

- PlayerAnimatorController의 `Weapon Visibility Controller` 연결
- PlayerCombatAnimationEvents의 `Attack Hitbox` 연결
- WeaponHitbox에 `PlayerAttackHitbox` 추가
- WeaponHitbox Rigidbody: Is Kinematic ON, Use Gravity OFF
- EnemyHealth의 Animator 연결 또는 자동 검색 확인
- Enemy Animator에 `HitTrigger`, `DeadTrigger`, `ParriedTrigger` 파라미터 추가

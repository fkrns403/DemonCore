using UnityEngine;
using UnityEngine.Animations;

[RequireComponent (typeof(PlayerInputReader))]
[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(PlayerMovement))]
public class PlayerAnimatorController : MonoBehaviour
{
    [Header("References")]
    [SerializeField,Tooltip("캐릭터 모델 오브젝트 항목  Animator")]
    private Animator animator;

    [Header("Animation Settings")]
    [SerializeField,Tooltip("Speed 파라미터가 부드럽게 변환되는 시간")]
    private float speedDampTime = 0.08f;

    [SerializeField, Tooltip("공격/전투 행동후 무기 준비 자세를 유지하는 시간")]
    private float weaponReadyDuration = 4.0f;


    private PlayerInputReader inputReader;
    private PlayerController playerController;
    private PlayerMovement playerMovement;

    private float weaponReadyTimer;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsWeaponReadyHash = Animator.StringToHash("IsWeaponReady");
    private static readonly int WeaponReadyTriggerHash = Animator.StringToHash("WeaponReadyTrigger");
    private static readonly int AttackTriggerHash = Animator.StringToHash("AttackTrigger");
    private static readonly int DodgeTriggerHash = Animator.StringToHash("DodgeTrigger");
    private static readonly int DodgeTypeHash = Animator.StringToHash("DodgeType");


    private bool IsWeaponReady => weaponReadyTimer > 0;

    private void Awake()
    {
        inputReader = GetComponent<PlayerInputReader>();
        playerController = GetComponent<PlayerController>();
        playerMovement = GetComponent<PlayerMovement>();
    }

    private void LateUpdate()
    {
        if (animator == null)
        {
            return;
        }

        UpdateMovementAnimation();
        UpdateWeaponReadyTimer();
        UpdateAttackAnimation();
        UpdateDodgeAnimation();
        UpdateWeaponReadyParameter();
    }

    /// <summary>
    /// 이동 입력 값을 speed로 전달
    /// </summary>
    private void UpdateMovementAnimation()
    {
        float speedValue = 0f;
        if (inputReader.MoveInput.sqrMagnitude > 0.01f)
        {
            speedValue = inputReader.SprintHeld ? 1f : 0.5f;
        }
        if (playerController.IsAttackLocked || playerMovement.IsDodging)
        {
            speedValue = 0f;
        }


        animator.SetFloat(SpeedHash, speedValue, speedDampTime, Time.deltaTime);
    }
    
    /// <summary>
    /// 무기 준비 자세 유지 시간연산
    /// </summary>
    private void UpdateWeaponReadyTimer()
    {
        
        if (weaponReadyTimer <= 0f)
        {
            weaponReadyTimer = 0f;
            return;
        }

        weaponReadyTimer -= Time.deltaTime;
    }
    /// <summary>
    /// 공격 입력이 들어오면 무기 준비 상태 여부에 따라
    /// 준비 자세 진입 또는 즉시 공격 애니메이션을 실행
    /// </summary>
    private void UpdateAttackAnimation()
    {
        
        if (!playerController.AttackStartedThisFrame)
        {
            return;
        }

        bool wasWeaponReady = IsWeaponReady;

        weaponReadyTimer = weaponReadyDuration;

        if (wasWeaponReady)
        {
            animator.SetTrigger(AttackTriggerHash);
        }
        else
        {
            animator.SetTrigger(WeaponReadyTriggerHash);
        }
    }

    private void UpdateDodgeAnimation()
    {
        if (!playerController.DodgeStartedThisFrame)
        {
            return;
        }

        weaponReadyTimer = weaponReadyDuration;

        animator.SetInteger(DodgeTypeHash, (int)playerController.StartedDodgeTypeThisFrame);
        animator.SetTrigger(DodgeTriggerHash);
    }

    /// <summary>
    /// 현재 무기 준비 상태를 Animator Bool 파라미터로 전달
    /// </summary>
    private void UpdateWeaponReadyParameter()
    {
        animator.SetBool(IsWeaponReadyHash, IsWeaponReady);
    }
}

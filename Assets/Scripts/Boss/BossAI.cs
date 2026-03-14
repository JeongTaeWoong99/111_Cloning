using System.Collections;
using UnityEngine;

/// <summary>
/// 보스 AI — State 패턴 기반 행동 제어.
/// BossManager가 연출 완료 후 Activate()를 호출하면 전투를 시작한다.
/// </summary>
[RequireComponent(typeof(Boss))]
public class BossAI : MonoBehaviour
{
    // ── State ─────────────────────────────────────────────────────
    private enum BossState { Idle, Chase, Attack, Skill, Dying }

    // ── Serialized Fields ─────────────────────────────────────────
    [SerializeField] private float _chargeSpeedMultiplier = 3f;
    [SerializeField] private float _chargeHitRange        = 1.5f;
    [SerializeField] private float _chargePushForce       = 6f;
    [SerializeField] private float _chargePushDuration    = 0.3f;

    // ── Properties ───────────────────────────────────────────────
    /// <summary>보스가 공격/스킬 동작 중인지 여부. 패링 경직 판단에 사용.</summary>
    public bool IsInAction => _currentState == BossState.Attack || _currentState == BossState.Skill;

    // ── Fields ────────────────────────────────────────────────────
    private Boss      _boss;
    private Transform _playerTransform;
    private BossState _currentState = BossState.Idle;

    private float _attackTimer;
    private float _skillTimer;
    private bool  _isActionLocked; // 공격·스킬 클립 재생 중
    private bool  _chargeHit;      // 돌진 히트 중복 방지

    // ── MonoBehaviour ─────────────────────────────────────────────
    private void Awake()
    {
        _boss = GetComponent<Boss>();
    }

    private void Update()
    {
        // 비전투 상태에서는 AI 정지
        if (GameManager.Instance.CurrentState != GameState.Combat)
        {
            return;
        }

        // Activate() 호출 전 — 플레이어 참조 없으면 AI 정지
        if (_playerTransform == null)
        {
            return;
        }

        // Die() 직후 즉시 AI 차단 (OnBossDying 1초 지연보다 선행)
        if (_boss.IsDying)
        {
            return;
        }

        if (_currentState == BossState.Dying)
        {
            return;
        }

        // 돌진 속도·히트·쿨타임은 isActionLocked 무관하게 매 프레임 실행
        ApplyChargeVelocity();
        CheckChargeHit();
        TickTimers(); // 애니메이션 중에도 쿨타임 카운트다운

        // 클립 재생 대기 중이면 스킵
        if (_isActionLocked)
        {
            return;
        }

        DecideNextState();
        ApplyChaseVelocity();
    }

    // ── Public Methods ────────────────────────────────────────────
    /// <summary>
    /// BossManager가 연출 완료 후 호출한다. 이 시점부터 AI 루프가 동작한다.
    /// </summary>
    public void Activate(Transform playerTransform)
    {
        _playerTransform = playerTransform;
        _boss.SetPlayer(playerTransform);
        _attackTimer     = 0f;
        _skillTimer      = 0f;
        _currentState    = BossState.Chase;
        _boss.PlayRun();
    }

    /// <summary>
    /// Boss.OnSkillExecute에서 Charge 스킬 발동 시 호출된다.
    /// </summary>
    public void StartCharge()
    {
        _chargeHit = false;
    }

    /// <summary>
    /// S-패링 성공 시 BossManager를 통해 호출된다. 보스를 일시 경직시킨다.
    /// </summary>
    public void ApplyStagger(float duration)
    {
        Debug.Log($"[BossAI] ApplyStagger 호출 — duration={duration:F2}, state={_currentState}");

        StopAllCoroutines();
        _isActionLocked = false;
        // 쿨타임은 TransitionToAttack/Skill 시작 시점에 이미 세팅·카운트 중 → 건드리지 않음

        _boss.Rigidbody.linearVelocity = Vector2.zero;
        _boss.PlayIdle();
        _boss.PauseAnimation();
        _boss.SetStaggerColor();
        _currentState = BossState.Idle;
        StartCoroutine(StaggerRoutine(duration));
    }

    /// <summary>
    /// 보스가 사망했을 때 BossManager가 호출한다.
    /// </summary>
    public void OnBossDying()
    {
        _currentState   = BossState.Dying;
        _isActionLocked = false;
        StopAllCoroutines();
        _boss.Rigidbody.linearVelocity = Vector2.zero;
    }

    // ── Private Methods ───────────────────────────────────────────
    private IEnumerator StaggerRoutine(float duration)
    {
        _isActionLocked = true;
        yield return new WaitForSeconds(duration);
        _boss.ResumeAnimation();
        _boss.ResetColor();
        _isActionLocked = false;
        Debug.Log("[BossAI] 경직 종료 → AI 재개");
    }

    /// <summary>
    /// Skill(Charge) 상태에서 매 프레임 돌진 속도를 적용한다.
    /// </summary>
    private void ApplyChargeVelocity()
    {
        if (_currentState != BossState.Skill) return;
        if (_boss.BossData.skill?.skillType != BossSkillType.Charge) return;
        if (_boss.IsKnockback) return;

        float chargeSpeed = _boss.MoveSpeed * _chargeSpeedMultiplier;
        _boss.Rigidbody.linearVelocity = new Vector2(-chargeSpeed, _boss.Rigidbody.linearVelocity.y);
    }

    /// <summary>
    /// Skill(Charge) 상태에서 플레이어와 접촉 시 밀침 처리한다 (1회).
    /// </summary>
    private void CheckChargeHit()
    {
        if (_currentState != BossState.Skill) return;
        if (_boss.BossData.skill?.skillType != BossSkillType.Charge) return;
        if (_chargeHit) return;

        float dist = GetDistanceToPlayer();
        if (dist > _chargeHitRange) return;

        _chargeHit = true;

        // 플레이어를 오른쪽 + 위로 밀침 (데미지 없음)
        Vector2 pushDir = (Vector2.right + Vector2.up * 0.5f).normalized;
        PlayerMover.Instance.Launch(pushDir * _chargePushForce, _chargePushDuration);
    }

    private void TickTimers()
    {
        if (_attackTimer > 0f)
        {
            _attackTimer -= Time.deltaTime;
        }

        if (_skillTimer > 0f)
        {
            _skillTimer -= Time.deltaTime;
        }
    }

    private void DecideNextState()
    {
        // 넉백 중에는 AI 판단 중지
        if (_boss.IsKnockback)
        {
            return;
        }

        float dist = GetDistanceToPlayer();

        BossSkillData skill = _boss.BossData.skill;
        bool skillReady = skill != null
                          && _skillTimer <= 0f
                          && (skill.healthThreshold == 0f || _boss.HealthRatio <= skill.healthThreshold);

        // Debug.Log($"[BossAI] DecideNextState — dist={dist:F2} | " +
        //           $"attackTimer={_attackTimer:F2} skillTimer={_skillTimer:F2} | " +
        //           $"skillReady={skillReady} skillRange={skill?.range:F2} attackRange={_boss.BossData.attackRange:F2}");

        // 스킬이 준비됐으면 공격보다 항상 우선
        // — 사거리 안이면 즉시 발동, 밖이면 사거리 안에 들 때까지 추격만 (공격 안 함)
        if (skillReady)
        {
            if (dist <= skill.range)
            {
                Debug.Log("[BossAI] → Skill 발동");
                TransitionToSkill();
            }
            else
            {
                Debug.Log("[BossAI] → Skill 대기 (사거리 밖, 추격)");
                TransitionTo(BossState.Chase);
            }
            return;
        }

        // 스킬이 준비되지 않은 경우에만 공격 판단
        if (_attackTimer <= 0f && dist <= _boss.BossData.attackRange)
        {
            Debug.Log("[BossAI] → Attack 발동");
            TransitionToAttack();
            return;
        }

        // 추격 또는 대기
        if (dist > _boss.BossData.attackRange)
        {
            TransitionTo(BossState.Chase);
        }
        else
        {
            TransitionTo(BossState.Idle);
        }
    }

    private void TransitionTo(BossState state)
    {
        if (_currentState == state)
        {
            return;
        }

        _currentState = state;

        switch (state)
        {
            case BossState.Chase:
                _boss.Rigidbody.linearVelocity = new Vector2(-_boss.MoveSpeed, _boss.Rigidbody.linearVelocity.y);
                _boss.PlayRun();
                break;

            case BossState.Idle:
                _boss.Rigidbody.linearVelocity = Vector2.zero;
                _boss.PlayIdle();
                break;
        }
    }

    private void TransitionToAttack()
    {
        _currentState                  = BossState.Attack;
        _boss.Rigidbody.linearVelocity = Vector2.zero;
        _attackTimer                   = _boss.BossData.attackCooldown; // 시작 즉시 쿨타임 개시
        _boss.PlayAttack();
        StartCoroutine(WaitForClipThenChase("Attack"));
    }

    private void TransitionToSkill()
    {
        _currentState                  = BossState.Skill;
        _boss.Rigidbody.linearVelocity = Vector2.zero;
        _skillTimer                    = _boss.BossData.skill != null ? _boss.BossData.skill.cooldown : 0f; // 시작 즉시 쿨타임 개시
        _boss.PlaySkill();
        StartCoroutine(WaitForClipThenChase("Skill"));
    }

    /// <summary>
    /// 클립 길이만큼 대기 후 Chase로 복귀한다.
    /// 쿨타임은 애니메이션 시작 시점(TransitionToAttack/Skill)에 이미 세팅됨.
    /// </summary>
    private IEnumerator WaitForClipThenChase(string clipName)
    {
        _isActionLocked = true;

        float clipLength = GetClipLength(clipName);
        yield return new WaitForSeconds(clipLength);

        _isActionLocked = false;

        if (_currentState != BossState.Dying && !_boss.IsDying)
        {
            TransitionTo(BossState.Chase);
        }
    }

    // Chase 상태일 때 매 프레임 velocity를 갱신해 물리 간섭을 보정한다.
    // 넉백 중에는 velocity를 덮어쓰지 않아 물리 넉백이 유지된다.
    private void ApplyChaseVelocity()
    {
        if (_currentState != BossState.Chase)
        {
            return;
        }

        if (_boss.IsKnockback)
        {
            return;
        }

        _boss.Rigidbody.linearVelocity = new Vector2(-_boss.MoveSpeed, _boss.Rigidbody.linearVelocity.y);
    }

    private float GetDistanceToPlayer()
    {
        if (_playerTransform == null)
        {
            return float.MaxValue;
        }

        return Mathf.Abs(transform.position.x - _playerTransform.position.x);
    }

    /// <summary>
    /// 현재 AnimatorController에서 클립 길이를 조회한다.
    /// 클립을 찾지 못하면 1초를 반환한다.
    /// </summary>
    private float GetClipLength(string clipName)
    {
        RuntimeAnimatorController controller = _boss.GetComponent<Animator>().runtimeAnimatorController;

        foreach (AnimationClip clip in controller.animationClips)
        {
            if (clip.name == clipName)
            {
                return clip.length;
            }
        }

        return 1f;
    }
}

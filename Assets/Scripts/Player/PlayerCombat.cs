using System.Collections;
using System.Collections.Generic;
using Inventory;
using UnityEngine;

/// <summary>
/// 플레이어 전투 입력 처리 — 공격(A), 대쉬(D), 패리(S).
/// 히트 판정은 애니메이션 이벤트 OnMeleeHit / OnArrowSpawn으로 처리한다.
/// </summary>
public class PlayerCombat : MonoBehaviour
{
    // ── Serialized Fields ─────────────────────────────────────────
    [Header("공격")]
    private float _attackRange = 3f;  // CharacterData.attackRange에서 초기화

    [SerializeField] private LayerMask _enemyLayer;

    [Header("대쉬")]
    [SerializeField, Tooltip("대쉬 쿨다운 (초)")]
    [Range(0.1f, 5f)]
    private float _dashCooldown = 1f;

    [Header("패링")]
    // 경계 밀쳐내기(S키)는 쿨타임 미적용 — 이 값은 전투 패링 구현 시 사용
    [SerializeField, Tooltip("패링 쿨다운 (초)")]
    [Range(0.1f, 5f)]
    private float _parryCooldown = 1f;

    [SerializeField, Tooltip("보스 공격 차단 범위 (m)")]
    private float _parryInterruptRange = 3f;

    [SerializeField, Tooltip("보스 경직 시간 (초)")]
    private float _staggerDuration = 0.8f;

    [SerializeField, Tooltip("S 패링 무적 시간 (초)")]
    private float _parryInvincibleDuration = 0.25f;

    // ── Properties ───────────────────────────────────────────────
    /// <summary>S 패링 쿨타임 잔여 시간. 0이면 즉시 사용 가능.</summary>
    public float ParryCooldownRemaining => Mathf.Max(0f, _parryCooldown - _parryTimer);

    /// <summary>D 대쉬 쿨타임 잔여 시간. 0이면 즉시 사용 가능.</summary>
    public float DashCooldownRemaining => Mathf.Max(0f, _dashCooldown - _dashTimer);

    /// <summary>패링 차단 범위. PlayerBoundaryHandler 넉백 거리 제한에 사용.</summary>
    public float ParryRange => _parryInterruptRange;

    // ── Fields ────────────────────────────────────────────────────
    private bool      _isAttacking;
    private bool      _isDashing;
    private Coroutine _attackCoroutine;
    private float     _dashTimer;
    private float     _parryTimer;

    private PlayerAnimator _playerAnimator;

    // ── Singleton ─────────────────────────────────────────────────
    public static PlayerCombat Instance { get; private set; }

    // ── MonoBehaviour ─────────────────────────────────────────────
    private void Awake()
    {
        Instance        = this;
        _playerAnimator = GetComponent<PlayerAnimator>();

        PlayerInventory.Instance.OnCharacterChanged += ApplyAttackRange;
        ApplyAttackRange();
    }

    private void OnDestroy()
    {
        if (PlayerInventory.Instance != null)
            PlayerInventory.Instance.OnCharacterChanged -= ApplyAttackRange;
    }

    private void Update()
    {
        // 쿨타임은 timeScale > 0이면 항상 진행 (층 이동·등장 중에도 감소)
        _parryTimer += Time.deltaTime;
        _dashTimer  += Time.deltaTime;

        GameState state = GameManager.Instance.CurrentState;

        // 사망 중(Die 애니메이션 ~ GameOver 전환 사이) 또는 전투 입력 중단 상태
        if (PlayerHealth.Instance.IsDead || GameManager.Instance.ShouldInterruptCombat)
        {
            if (_isAttacking) StopAttack();
            return;
        }

        if (state == GameState.Pinned)
        {
            HandleParry();
            return;
        }

        if (state != GameState.Combat)
        {
            return;
        }

        HandleAttack();
        HandleDash();
        HandleCounterattackSkill();
        HandleSkill();
    }

    // ── Private Methods ───────────────────────────────────────────
    private void ApplyAttackRange()
    {
        CharacterData ch = PlayerInventory.Instance?.SelectedCharacter;
        if (ch != null) _attackRange = ch.attackRange;
    }

    private void HandleAttack()
    {
        // 대쉬 중 또는 이미 공격 코루틴 실행 중이면 무시
        if (_isDashing || _isAttacking) return;

        if (Input.GetKey(KeyCode.A))
        {
            StartAttack();
        }
    }

    private void StartAttack()
    {
        if (_attackCoroutine != null)
        {
            StopCoroutine(_attackCoroutine);
        }

        _attackCoroutine = StartCoroutine(AttackLoop());
    }

    private void StopAttack()
    {
        if (_attackCoroutine != null)
        {
            StopCoroutine(_attackCoroutine);
            _attackCoroutine = null;
        }

        _isAttacking = false;
        // PlayIdle 제거 — PlayerAnimator.OnGameStateChanged가 처리
    }

    /// <summary>
    /// A키를 누르고 있는 동안 공격 애니메이션을 1회씩 반복 재생한다.
    /// 클립 길이 / 공속으로 정확한 타이밍을 제어하므로 Animator 폴링이 필요 없다.
    /// </summary>
    private IEnumerator AttackLoop()
    {
        _isAttacking = true;

        do
        {
            float speed      = PlayerStats.Instance?.TotalAttackSpeed ?? 1f;
            float clipLength = _playerAnimator.GetAttackClipLength();

            _playerAnimator.PlayAttack(speed);
            yield return new WaitForSeconds(clipLength / speed);

            // 전투 불가 상태로 바뀌었으면 즉시 종료
            if (GameManager.Instance.CurrentState != GameState.Combat) break;
        }
        while (Input.GetKey(KeyCode.A));

        _isAttacking     = false;
        _attackCoroutine = null;
        // 루프 종료 후 상태에 따라 Idle 복귀 또는 PlayerAnimator.OnGameStateChanged가 처리
        if (GameManager.Instance.CurrentState == GameState.Combat)
            _playerAnimator.PlayIdle();
    }

    private void HandleDash()
    {
        if (_isDashing)                    return;  // 대쉬 애니메이션 중 재대쉬 방지
        if (PlayerMover.Instance.IsMoving) return;
        if (!Input.GetKeyDown(KeyCode.D))  return;
        if (_dashTimer < _dashCooldown)    return;

        _dashTimer = 0f;

        // 공격 코루틴 중단 후 대쉬 시작
        StopAttack();

        Enemy nearest = FindNearestEnemy();
        if (nearest == null) return;

        // 적 왼쪽 1m 앞으로 대쉬
        Vector2 dashTarget = (Vector2)nearest.transform.position + Vector2.left * 1f;
        dashTarget.y = transform.position.y;    // 위로 뜨지 않게 내 위치로
        StartCoroutine(DashSequence(dashTarget));
    }

    /// <summary>
    /// 물리 이동(DashTo)과 Dash 애니메이션이 모두 끝난 뒤 다음 상태로 전이한다.
    /// </summary>
    private IEnumerator DashSequence(Vector2 target)
    {
        _isDashing = true;
        GameManager.Instance.SetState(GameState.Dash);   // 대쉬 중 다른 입력 차단
        float clip = _playerAnimator.GetClipLength("Dash");
        _playerAnimator.PlayDash();

        // 대쉬 애니메이션 동안 무적
        PlayerHealth.Instance.SetInvincible(clip);

        // 물리 독립 실행 (fire-and-forget)
        StartCoroutine(PlayerMover.Instance.DashTo(target));

        // 애니메이션 클립 길이만큼만 대기
        yield return new WaitForSeconds(clip);

        _isDashing = false;

        // Dash 상태일 때만 Combat 복귀 (Pinned 등 외부에서 상태 바뀌면 무시)
        if (GameManager.Instance.CurrentState != GameState.Dash) yield break;
        GameManager.Instance.SetState(GameState.Combat);

        if (Input.GetKey(KeyCode.A))
            StartAttack();
        else
            _playerAnimator.PlayIdle();
    }

    private void HandleParry()
    {
        if (!Input.GetKeyDown(KeyCode.S)) return;

        // Pinned 상태에서도 보스 경직·무적 적용
        TryInterruptBoss();
        TryDestroyNearbySpears();
        PlayerBoundaryHandler.Instance.Counterattack();
    }

    /// <summary>
    /// Combat 중 S키 — 보스 경직 시도 후, 항상 플레이어 Launch + 넉백도 실행.
    /// </summary>
    private void HandleCounterattackSkill()
    {
        if (!Input.GetKeyDown(KeyCode.S))  return;
        if (_parryTimer < _parryCooldown)  return;
        if (PlayerMover.Instance.IsMoving) return;  // 대쉬/런치 중 방지

        _parryTimer = 0f;
        StopAttack();

        TryInterruptBoss(); // 보스 경직 시도 (결과 무관하게 계속 진행)
        TryDestroyNearbySpears();
        PlayerBoundaryHandler.Instance.CounterattackSkill();
    }

    /// <summary>
    /// 보스가 Attack/Skill 중이고 범위 내에 있으면 경직시키고 무적을 부여한다.
    /// </summary>
    private bool TryInterruptBoss()
    {
        // BossManager가 없으면 보스 방이 아님
        if (BossManager.Instance == null) return false;

        // 보스가 Attack/Skill 중이 아니면 패링 대상 없음
        if (!BossManager.Instance.IsBossInAction) return false;

        Boss boss = BossManager.Instance.CurrentBoss;
        // 보스 참조가 없거나 사망 중이면 패링 무시
        if (boss == null || boss.IsDying) return false;

        float dist = Mathf.Abs(transform.position.x - boss.transform.position.x);

        // 범위 초과 시 패링 실패
        if (dist > _parryInterruptRange) return false;

        // 경직 적용 + 무적 부여
        BossManager.Instance.StaggerBoss(_staggerDuration);
        PlayerHealth.Instance.SetInvincible(_parryInvincibleDuration);
        return true;
    }

    /// <summary>
    /// 패링 범위(_parryInterruptRange) 내에 날아오는 창이 있으면 모두 제거한다.
    /// </summary>
    private void TryDestroyNearbySpears()
    {
        BossSpear[] spears = FindObjectsByType<BossSpear>(FindObjectsSortMode.None);
        foreach (BossSpear spear in spears)
        {
            float dist = Mathf.Abs(spear.transform.position.x - transform.position.x);
            if (dist <= _parryInterruptRange)
                spear.DestroyByParry();
        }
    }

    private void HandleSkill()
    {
        if (!Input.GetKeyDown(KeyCode.F))  return;
        if (PlayerMover.Instance.IsMoving) return;  // 대쉬/런치 중 방지
        
        PlayerSkillHandler.Instance.TryExecute();
    }

    private Enemy FindNearestEnemy()
    {
        IReadOnlyList<Enemy> enemies = EnemySpawnManager.Instance.LivingEnemies;

        Enemy nearest = null;
        float minDist = float.MaxValue;

        foreach (Enemy enemy in enemies)
        {
            // 사망 중인 적(Die 애니메이션 재생 중) 제외
            if (enemy.IsDying) continue;

            float dist = Mathf.Abs(enemy.transform.position.x - transform.position.x);

            if (dist < minDist)
            {
                minDist = dist;
                nearest = enemy;
            }
        }

        // 보스 방에서는 BossManager의 보스도 검사
        Boss boss = BossManager.Instance != null ? BossManager.Instance.CurrentBoss : null;

        if (boss != null && !boss.IsDying)
        {
            float dist = Mathf.Abs(boss.transform.position.x - transform.position.x);

            if (dist < minDist)
            {
                nearest = boss;
            }
        }

        return nearest;
    }

    private void OnDrawGizmos()
    {
        // 패링 범위 — 항상 표시되는 원형 (파랑)
        Gizmos.color = new Color(0.3f, 0.5f, 1f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, _parryInterruptRange);
    }

    private void OnDrawGizmosSelected()
    {
        // ── 공격 범위 (빨강) ──────────────────────────────────────
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Vector3 attackCenter = transform.position + Vector3.right * (_attackRange * 0.5f);
        Vector3 attackSize   = new Vector3(_attackRange, 1f, 0f);
        Gizmos.DrawCube(attackCenter, attackSize);
        Gizmos.color = new Color(1f, 0f, 0f, 0.8f);
        Gizmos.DrawWireCube(attackCenter, attackSize);
    }

    // ── 애니메이션 이벤트 타겟 ─────────────────────────────────────

    /// <summary>
    /// Attack 클립의 타격 타이밍에 애니메이션 이벤트로 호출 (근접: Sword, Spear).
    /// </summary>
    public void OnMeleeHit()
    {
        float   damage    = PlayerStats.Instance?.TotalAttack ?? 1f;
        Vector2 boxCenter = (Vector2)transform.position + Vector2.right * (_attackRange * 0.5f);
        Vector2 boxSize   = new Vector2(_attackRange, 1f);

        Collider2D[] hits = Physics2D.OverlapBoxAll(boxCenter, boxSize, 0f, _enemyLayer);

        foreach (Collider2D hit in hits)
        {
            if (hit.TryGetComponent(out Enemy enemy))
            {
                enemy.TakeDamage(damage);
            }
        }
    }

    /// <summary>
    /// Attack 클립의 발사 타이밍에 애니메이션 이벤트로 호출 (원거리: Bow).
    /// </summary>
    public void OnArrowSpawn()
    {
        GameObject obj = ObjectPoolManager.Instance.Get("Arrow");
        
        if (obj != null && obj.TryGetComponent(out Arrow arrow))
            arrow.Initialize(transform.position, PlayerStats.Instance?.TotalAttack ?? 1f, _enemyLayer);
    }
}

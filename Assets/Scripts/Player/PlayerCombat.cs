using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어 전투 입력 처리 — 공격(A), 대쉬(D), 패리(S).
/// 히트 판정은 애니메이션 이벤트 OnMeleeHit / OnArrowSpawn으로 처리한다.
/// </summary>
public class PlayerCombat : MonoBehaviour
{
    // ── Serialized Fields ─────────────────────────────────────────
    [Header("공격")]
    [SerializeField, Tooltip("공격 범위 (m)")]
    [Range(1f, 10f)]
    private float _attackRange = 3f;

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

    // ── Fields ────────────────────────────────────────────────────
    private bool      _isAttacking;
    private bool      _isDashing;
    private Coroutine _attackCoroutine;
    private float     _dashTimer;
    private float     _parryTimer;

    private PlayerAnimator _playerAnimator;

    // ── MonoBehaviour ─────────────────────────────────────────────
    private void Awake()
    {
        _playerAnimator = GetComponent<PlayerAnimator>();
    }

    private void Update()
    {
        GameState state = GameManager.Instance.CurrentState;

        // 사망 중(Die 애니메이션 ~ GameOver 전환 사이) 또는 전투 불가 상태
        if (PlayerHealth.Instance.IsDead        ||
            state == GameState.Cleared          ||
            state == GameState.Transitioning    ||
            state == GameState.Entering         ||
            state == GameState.GameOver)
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

        _parryTimer += Time.deltaTime;

        HandleAttack();
        HandleDash();
    }

    // ── Private Methods ───────────────────────────────────────────
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
        _dashTimer += Time.deltaTime;

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
        StartCoroutine(DashSequence(dashTarget));
    }

    /// <summary>
    /// 물리 이동(DashTo)과 Dash 애니메이션이 모두 끝난 뒤 다음 상태로 전이한다.
    /// </summary>
    private IEnumerator DashSequence(Vector2 target)
    {
        _isDashing = true;
        float clip = _playerAnimator.GetClipLength("Dash");
        _playerAnimator.PlayDash();

        // 물리 독립 실행 (fire-and-forget)
        StartCoroutine(PlayerMover.Instance.DashTo(target));

        // 애니메이션 클립 길이만큼만 대기
        yield return new WaitForSeconds(clip);

        // 대쉬 종료 — 이후 상태가 Combat이면 입력에 따라 공격 또는 Idle 복귀
        _isDashing = false;

        // Combat 상태일 때만 전이 (Pinned 등 다른 상태면 무시)
        if (GameManager.Instance.CurrentState != GameState.Combat) yield break;

        if (Input.GetKey(KeyCode.A))
            StartAttack();
        else
            _playerAnimator.PlayIdle();
    }

    private void HandleParry()
    {
        if (!Input.GetKeyDown(KeyCode.S)) return;

        PlayerBoundaryHandler.Instance.Counterattack();
    }

    private Enemy FindNearestEnemy()
    {
        IReadOnlyList<Enemy> enemies = EnemySpawnManager.Instance.LivingEnemies;

        Enemy nearest = null;
        float minDist = float.MaxValue;

        foreach (Enemy enemy in enemies)
        {
            float dist = Mathf.Abs(enemy.transform.position.x - transform.position.x);

            if (dist < minDist)
            {
                minDist = dist;
                nearest = enemy;
            }
        }

        return nearest;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);

        Vector3 boxCenter = transform.position + Vector3.right * (_attackRange * 0.5f);
        Vector3 boxSize   = new Vector3(_attackRange, 1f, 0f);

        Gizmos.DrawCube(boxCenter, boxSize);

        Gizmos.color = new Color(1f, 0f, 0f, 0.8f);
        Gizmos.DrawWireCube(boxCenter, boxSize);
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
    /// Attack 클립의 발사 타이밍에 애니메이션 이벤트로 호출 (원거리: Bow) — 스텁.
    /// </summary>
    public void OnArrowSpawn() { /* TODO: 화살 프리팹 생성 */ }
}

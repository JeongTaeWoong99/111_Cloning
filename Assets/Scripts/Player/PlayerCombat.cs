using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어 전투 입력 처리 — 공격(A), 대쉬(D), 패리(S).
/// </summary>
public class PlayerCombat : MonoBehaviour
{
    // ── Serialized Fields ─────────────────────────────────────────
    [Header("공격")]
    [SerializeField, Tooltip("공격 범위 (m)")]
    [Range(1f, 10f)]
    private float _attackRange = 3f;

    [SerializeField, Tooltip("공격 데미지")]
    [Range(1f, 100f)]
    private float _attackDamage = 10f;

    [SerializeField, Tooltip("공격 쿨다운 (초)")]
    [Range(0.1f, 2f)]
    private float _attackCooldown = 0.3f;

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
    private float _attackTimer;
    private float _dashTimer;
    private float _parryTimer;

    // ── MonoBehaviour ─────────────────────────────────────────────
    private void Update()
    {
        GameState state = GameManager.Instance.CurrentState;

        if (state == GameState.Pinned)
        {
            HandleParry();
            return;
        }

        if (state != GameState.Combat)
        {
            return;
        }

        // 패링 타이머는 Combat 상태에서 계속 증가 (전투 패링 구현 시 HandleParry에서 사용)
        _parryTimer += Time.deltaTime;

        HandleAttack();
        HandleDash();
    }

    // ── Private Methods ───────────────────────────────────────────
    private void HandleAttack()
    {
        _attackTimer += Time.deltaTime;

        if (!Input.GetKey(KeyCode.A))
        {
            return;
        }

        if (_attackTimer < _attackCooldown)
        {
            return;
        }

        _attackTimer = 0f;

        // 플레이어 오른쪽 방향 범위 안의 적에게 데미지
        Vector2 boxCenter = (Vector2)transform.position + Vector2.right * (_attackRange * 0.5f);
        Vector2 boxSize   = new Vector2(_attackRange, 1f);

        Collider2D[] hits = Physics2D.OverlapBoxAll(boxCenter, boxSize, 0f, _enemyLayer);

        // 장비 스탯이 있으면 공격력 보너스 반영, 없으면 Inspector 기본값 사용
        float damage = PlayerStats.Instance != null
            ? _attackDamage + PlayerStats.Instance.TotalAttack
            : _attackDamage;

        foreach (Collider2D hit in hits)
        {
            if (hit.TryGetComponent(out Enemy enemy))
            {
                enemy.TakeDamage(damage);
            }
        }
    }

    private void HandleDash()
    {
        _dashTimer += Time.deltaTime;

        if (PlayerMover.Instance.IsMoving)
        {
            return;
        }

        if (!Input.GetKeyDown(KeyCode.D))
        {
            return;
        }

        if (_dashTimer < _dashCooldown)
        {
            return;
        }

        _dashTimer = 0f;

        Enemy nearest = FindNearestEnemy();

        if (nearest == null)
        {
            return;
        }

        // 적 왼쪽 1m 앞으로 대쉬
        Vector2 dashTarget = (Vector2)nearest.transform.position + Vector2.left * 1f;
        StartCoroutine(PlayerMover.Instance.DashTo(dashTarget));
    }

    private void HandleParry()
    {
        if (!Input.GetKeyDown(KeyCode.S))
        {
            return;
        }

        PlayerBoundaryHandler.Instance.Counterattack();
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
}

using System.Collections;
using UnityEngine;

/// <summary>
/// 플레이어 전투 입력 처리 — 공격(A), 대쉬(D), 패리(S).
/// </summary>
public class PlayerCombat : MonoBehaviour
{
    // ── Serialized Fields ─────────────────────────────────────────
    [Header("참조")]
    [SerializeField] private PlayerMover _playerMover;

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

    // ── Fields ────────────────────────────────────────────────────
    private float _attackTimer;

    // ── MonoBehaviour ─────────────────────────────────────────────
    private void Update()
    {
        if (GameManager.Instance.CurrentState != GameState.Combat)
        {
            return;
        }

        // 대쉬 중 입력 차단
        if (_playerMover.IsMoving)
        {
            return;
        }

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

        foreach (Collider2D hit in hits)
        {
            if (hit.TryGetComponent(out Enemy enemy))
            {
                enemy.TakeDamage(_attackDamage);
            }
        }
    }

    private void HandleDash()
    {
        if (!Input.GetKeyDown(KeyCode.D))
        {
            return;
        }

        Enemy nearest = FindNearestEnemy();

        if (nearest == null)
        {
            return;
        }

        // 적 왼쪽 1m 앞으로 대쉬
        Vector2 dashTarget = (Vector2)nearest.transform.position + Vector2.left * 1f;
        StartCoroutine(_playerMover.DashTo(dashTarget));
    }

    // S키 패리 — 추후 구현
    private void HandleParry() { }

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
        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);

        Enemy nearest  = null;
        float minDist  = float.MaxValue;

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

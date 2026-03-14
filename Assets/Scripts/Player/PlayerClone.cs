using System.Collections;
using UnityEngine;

/// <summary>
/// Spear 스킬로 소환되는 Clone 오브젝트.
/// 플레이어가 Attack 애니메이션에 진입하면 클론도 1회 공격 후 Idle로 복귀한다.
/// 클론의 공격 속도는 플레이어와 독립적으로 _attackDuration으로 결정된다.
/// _duration 경과 후 스스로 Destroy된다.
/// </summary>
public class PlayerClone : MonoBehaviour
{
    // ── Serialized Fields ─────────────────────────────────────────
    [SerializeField, Tooltip("1회 공격 지속 시간 (초) — 클론 자체 공격 속도"), Range(0.1f, 5f)]
    private float _attackDuration = 0.5f;

    [SerializeField, Tooltip("히트박스 너비 (m)"), Range(0.1f, 5f)]
    private float _attackRange = 3f;

    [SerializeField, Tooltip("지속 시간 (초)"), Range(1f, 30f)]
    private float _duration = 10f;

    [SerializeField, Tooltip("Attack/Idle 상태 전환용 애니메이터 (없으면 무시)")]
    private Animator _animator;

    // ── Fields ────────────────────────────────────────────────────
    private float          _damage;
    private LayerMask      _enemyLayer;
    private float          _lifeTimer;
    private string         _currentState;
    private bool           _isAttacking;
    private PlayerAnimator _playerAnimator;

    // ── Public Methods ────────────────────────────────────────────
    /// <summary>
    /// 소환 직후 PlayerSkillHandler에서 호출하여 데미지·레이어를 설정한다.
    /// 부모(플레이어)의 PlayerAnimator를 자동으로 참조한다.
    /// </summary>
    public void Initialize(float damage, LayerMask enemyLayer)
    {
        _damage         = damage;
        _enemyLayer     = enemyLayer;
        _lifeTimer      = 0f;
        _currentState   = string.Empty;
        _isAttacking    = false;
        _playerAnimator = GetComponentInParent<PlayerAnimator>();

        SetAnimatorState("Idle");
    }

    // ── MonoBehaviour ─────────────────────────────────────────────
    private void Update()
    {
        _lifeTimer += Time.deltaTime;
        if (_lifeTimer >= _duration)
        {
            Destroy(gameObject);
            return;
        }

        // 공격 코루틴 실행 중이면 대기
        if (_isAttacking) return;

        // 플레이어가 Attack 상태일 때만 1회 공격 시작
        if (_playerAnimator != null && _playerAnimator.IsAttacking)
            StartCoroutine(AttackOnce());
    }

    // ── Private Methods ───────────────────────────────────────────
    /// <summary>
    /// Attack 애니메이션을 1회 재생하고 판정 후 Idle로 복귀한다.
    /// </summary>
    private IEnumerator AttackOnce()
    {
        _isAttacking = true;
        SetAnimatorState("Attack");
        Attack();

        yield return new WaitForSeconds(_attackDuration);

        SetAnimatorState("Idle");
        _isAttacking = false;
    }

    private void Attack()
    {
        Vector2      boxCenter = (Vector2)transform.position + Vector2.right * (_attackRange * 0.5f);
        Vector2      boxSize   = new Vector2(_attackRange, 1f);
        Collider2D[] hits      = Physics2D.OverlapBoxAll(boxCenter, boxSize, 0f, _enemyLayer);

        foreach (Collider2D hit in hits)
        {
            if (hit.TryGetComponent(out Enemy enemy))
                enemy.TakeDamage(_damage);
        }
    }

    /// <summary>중복 Play를 막기 위해 상태가 바뀔 때만 Animator에 전달한다.</summary>
    private void SetAnimatorState(string state)
    {
        if (_animator == null || state == _currentState) return;

        _currentState = state;
        _animator.Play(state);
    }

    // ── Gizmos ────────────────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 0.8f, 1f, 0.3f);
        Vector3 center = transform.position + Vector3.right * (_attackRange * 0.5f);
        Gizmos.DrawCube(center, new Vector3(_attackRange, 1f, 0f));
        Gizmos.color = new Color(0f, 0.8f, 1f, 0.8f);
        Gizmos.DrawWireCube(center, new Vector3(_attackRange, 1f, 0f));
    }
}

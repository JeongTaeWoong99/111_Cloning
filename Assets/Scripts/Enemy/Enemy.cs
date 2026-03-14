using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 적 개체. HP + 피격 + 사망 + 넉백 + 애니메이션만 담당한다. 이동은 EnemySpawnManager가 처리한다.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Enemy : MonoBehaviour
{
    // ── Properties ───────────────────────────────────────────────
    public EnemyData   Data        { get; private set; }
    public float       MoveSpeed   { get; protected set; }
    public Rigidbody2D Rigidbody   { get; private set; }
    public bool        IsKnockback => _isKnockback;
    public bool        IsDying     => _isDying;

    // ── Events ────────────────────────────────────────────────────
    public event Action<Enemy> OnDied;

    // ── Fields ────────────────────────────────────────────────────
    private static int s_deadLayer = -1;

    private   Animator _animator;
    protected float   _health;
    protected float   _maxHealth;
    private   int     _originalLayer;
    private bool     _isKnockback;
    private bool     _isDying;

    // ── MonoBehaviour ─────────────────────────────────────────────
    private void Awake()
    {
        Rigidbody      = GetComponent<Rigidbody2D>();
        _animator      = GetComponent<Animator>();
        _originalLayer = gameObject.layer;

        // 첫 인스턴스 생성 시 한 번만 레이어 번호를 캐싱
        if (s_deadLayer == -1)
        {
            s_deadLayer = LayerMask.NameToLayer("EnemyDead");
        }
    }

    private void OnDisable()
    {
        // 풀 반환 시 코루틴·물리·애니메이터 상태 초기화
        StopAllCoroutines();
        _isKnockback     = false;
        _isDying         = false;
        gameObject.layer = _originalLayer;
        _animator.Rebind();
        // _animator.Update(0f) 제거 — 비활성 오브젝트에서 호출 불가
    }

    // ── Public Methods ────────────────────────────────────────────
    /// <summary>
    /// EnemyData를 기반으로 스탯과 애니메이션을 초기화한다.
    /// </summary>
    public void Initialize(EnemyData data)
    {
        Data        = data;
        _maxHealth  = data.maxHealth;
        _health     = _maxHealth;
        MoveSpeed   = data.moveSpeed;

        if (data.overrideController != null)
        {
            _animator.runtimeAnimatorController = data.overrideController;
        }

        PlayIdle();
    }

    /// <summary>
    /// 데미지를 적용하고 HP가 0 이하이면 사망 처리한다.
    /// </summary>
    public virtual void TakeDamage(float amount)
    {
        if (_isDying)
        {
            return;
        }

        _health -= amount;

        if (_health <= 0f)
        {
            Die();
        }
    }

    /// <summary>
    /// AddForce 기반 넉백을 적용하고 duration 후 해제한다.
    /// </summary>
    public void ApplyKnockback(Vector2 force, float duration)
    {
        _isKnockback = true;
        Rigidbody.AddForce(force, ForceMode2D.Impulse);
        StartCoroutine(ResetKnockbackAfter(duration));
    }

    public void PlayIdle() => _animator.Play("Idle");
    public void PlayRun()  => _animator.Play("Run");
    public void PlayDie()  => _animator.Play("Die");

    // ── Private Methods ───────────────────────────────────────────
    private IEnumerator ResetKnockbackAfter(float duration)
    {
        yield return new WaitForSeconds(duration);
        _isKnockback = false;
    }

    private void Die()
    {
        _isDying                 = true;
        gameObject.layer         = s_deadLayer;
        Rigidbody.linearVelocity = Vector2.zero;
        PlayDie();
        StartCoroutine(InvokeDiedAfterDelay());
    }

    private IEnumerator InvokeDiedAfterDelay()
    {
        // Die 애니메이션이 모두 재생될 때까지 대기 (모든 Die 클립이 1초 이하)
        yield return new WaitForSeconds(1f);
        OnDied?.Invoke(this);
    }
}

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

    private   Animator         _animator;
    protected SpriteRenderer   _spriteRenderer;
    protected Color            _normalColor = Color.white;
    protected float            _health;
    protected float            _maxHealth;
    private   int              _originalLayer;
    private   bool             _isKnockback;
    private   bool             _isDying;
    private   Coroutine        _hitFlashCoroutine;

    private static readonly Color HitFlashColor    = new Color(3f, 3f, 3f, 1f); // HDR 흰색 — 3배 밝기로 Bloom 효과 강조
    private const           float HitFlashDuration = 0.1f;

    // ── MonoBehaviour ─────────────────────────────────────────────
    private void Awake()
    {
        Rigidbody       = GetComponent<Rigidbody2D>();
        _animator       = GetComponent<Animator>();
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        _normalColor    = _spriteRenderer != null ? _spriteRenderer.color : Color.white;
        _originalLayer  = gameObject.layer;

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
        _isKnockback        = false;
        _isDying            = false;
        _hitFlashCoroutine  = null;
        if (_spriteRenderer != null) _spriteRenderer.color = _normalColor;
        gameObject.layer    = _originalLayer;
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
        if (_isDying) return;

        _health -= amount;

        if (_health <= 0f)
        {
            TryStartHitFlash(); // 한방 사망 시에도 히트 플래시 표시
            Die();
        }
        else
        {
            TryStartHitFlash();
        }
    }

    /// <summary>진행 중인 히트 플래시를 즉시 중단한다. 스태거 등 색 우선순위가 더 높은 효과가 걸릴 때 호출한다.</summary>
    protected void CancelHitFlash()
    {
        if (_hitFlashCoroutine == null) return;
        StopCoroutine(_hitFlashCoroutine);
        _hitFlashCoroutine = null;
    }

    /// <summary>히트 플래시를 시작한다. Boss에서 경직 여부에 따라 오버라이드한다.</summary>
    protected virtual void TryStartHitFlash()
    {
        if (_hitFlashCoroutine != null) StopCoroutine(_hitFlashCoroutine);
        _hitFlashCoroutine = StartCoroutine(HitFlashRoutine());
    }

    /// <summary>
    /// 흰색(HDR) 플래시에서 원래 색으로 빠르게 감소시킨다.
    /// 재질이 HDR을 지원하면 Bloom 효과로 시각적으로 두드러진다.
    /// </summary>
    private IEnumerator HitFlashRoutine()
    {
        if (_spriteRenderer == null) yield break;

        _spriteRenderer.color = HitFlashColor;

        float elapsed = 0f;
        while (elapsed < HitFlashDuration)
        {
            elapsed += Time.deltaTime;
            _spriteRenderer.color = Color.Lerp(HitFlashColor, _normalColor, elapsed / HitFlashDuration);
            yield return null;
        }

        _spriteRenderer.color = _normalColor;
        _hitFlashCoroutine    = null;
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

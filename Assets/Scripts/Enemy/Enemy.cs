using System;
using UnityEngine;

/// <summary>
/// 적 개체. HP + 피격 + 사망만 담당한다. 이동은 EnemySpawnManager가 처리한다.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Enemy : MonoBehaviour
{
    // ── Properties ───────────────────────────────────────────────
    public EnemyData   Data      { get; private set; }
    public float       MoveSpeed { get; private set; }
    public Rigidbody2D Rigidbody { get; private set; }

    // ── Events ────────────────────────────────────────────────────
    public event Action<Enemy> OnDied;

    // ── Fields ────────────────────────────────────────────────────
    private float _health;

    // ── MonoBehaviour ─────────────────────────────────────────────
    private void Awake()
    {
        Rigidbody = GetComponent<Rigidbody2D>();
    }

    // ── Public Methods ────────────────────────────────────────────
    /// <summary>
    /// EnemyData를 기반으로 초기화한다.
    /// </summary>
    public void Initialize(EnemyData data)
    {
        Data      = data;
        _health   = data.maxHealth;
        MoveSpeed = data.moveSpeed;
    }

    /// <summary>
    /// 데미지를 적용하고 HP가 0 이하이면 사망 처리한다.
    /// </summary>
    public void TakeDamage(float amount)
    {
        _health -= amount;

        if (_health <= 0f)
        {
            Die();
        }
    }

    // ── Private Methods ───────────────────────────────────────────
    private void Die()
    {
        OnDied?.Invoke(this);
        // 풀 반환은 EnemySpawnManager가 처리
    }
}

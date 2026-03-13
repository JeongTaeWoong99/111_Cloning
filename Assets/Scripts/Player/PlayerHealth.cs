using System;
using UnityEngine;

/// <summary>
/// 플레이어 체력 관리 싱글턴.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────
    public static PlayerHealth Instance { get; private set; }

    // ── Serialized Fields ─────────────────────────────────────────
    [SerializeField, Tooltip("최대 체력")]
    [Range(1, 10)]
    private int _maxHealth = 3;

    // ── Properties ───────────────────────────────────────────────
    public int CurrentHealth { get; private set; }
    public int MaxHealth     => _maxHealth;

    // ── Events ────────────────────────────────────────────────────
    public event Action      OnDied;
    public event Action<int> OnHealthChanged;

    // ── MonoBehaviour ─────────────────────────────────────────────
    private void Awake()
    {
        Instance = this;

        // 장비 스탯이 있으면 그 값으로 최대 체력 초기화, 없으면 Inspector 기본값 사용
        int maxFromStats = PlayerStats.Instance != null
            ? PlayerStats.Instance.TotalHealth
            : _maxHealth;

        _maxHealth    = maxFromStats;
        CurrentHealth = maxFromStats;
    }

    // ── Public Methods ────────────────────────────────────────────
    /// <summary>
    /// 데미지를 적용하고 체력이 0 이하이면 사망 처리한다.
    /// </summary>
    public void TakeDamage(int amount)
    {
        if (CurrentHealth <= 0)
        {
            return;
        }

        CurrentHealth -= amount;
        OnHealthChanged?.Invoke(CurrentHealth);

        if (CurrentHealth <= 0)
        {
            OnDied?.Invoke();
            GameManager.Instance.SetState(GameState.GameOver);
        }
    }
}

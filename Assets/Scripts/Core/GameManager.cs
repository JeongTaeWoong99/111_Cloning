using System;
using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
/// <summary>
/// 전투 흐름 단계.
/// </summary>
public enum GameState
{
    Entering,      // 플레이어 등장 이동 중
    Combat,        // 적 이동 + 플레이어 입력 활성
    Cleared,       // 적 전멸, 플레이어 퇴장 이동
    Transitioning, // 층 전환 중 (카메라 스크롤)
}

/// <summary>
/// 게임 상태를 보유하고 상태 변경 이벤트를 발행하는 싱글턴.
/// </summary>
public class GameManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────
    public static GameManager Instance { get; private set; }

    // ── Properties ───────────────────────────────────────────────
    public GameState CurrentState { get; private set; }

    // ── Events ────────────────────────────────────────────────────
    public event Action<GameState> OnStateChanged;

    // ── MonoBehaviour ─────────────────────────────────────────────
    private void Awake()
    {
        Instance = this;
    }

    // ── Public Methods ────────────────────────────────────────────
    /// <summary>
    /// 상태를 변경하고 구독자에게 알린다.
    /// </summary>
    public void SetState(GameState state)
    {
        CurrentState = state;
        OnStateChanged?.Invoke(state);
    }
}

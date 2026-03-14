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
    Reward,        // 보상 방 진행 중 (전투 없음, 플레이어 Idle)
    Cleared,       // 적 전멸, 플레이어 퇴장 이동
    Transitioning, // 층 전환 중 (카메라 스크롤)
    Skill,         // 스킬 연출 중 — timeScale=0으로 전체 freeze
    Dash,          // 대쉬 중 — 다른 입력 차단
    Pinned,        // 플레이어가 왼쪽 경계에 밀림 — timeScale=0
    GameOver,      // 게임 오버 — timeScale=0
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

    // ── Properties (헬퍼) ─────────────────────────────────────────
    /// <summary>공격·대쉬·스킬 등 전투 입력이 강제 중단되어야 하는 상태.</summary>
    public bool ShouldInterruptCombat =>
        CurrentState != GameState.Combat  &&
        CurrentState != GameState.Pinned  &&
        CurrentState != GameState.Reward;

    // ── Public Methods ────────────────────────────────────────────
    /// <summary>
    /// 상태를 변경하고 구독자에게 알린다.
    /// </summary>
    public void SetState(GameState state)
    {
        CurrentState = state;

        // timeScale 중앙 관리 — Pinned/Skill/GameOver는 0, 나머지는 1
        Time.timeScale = state switch
        {
            GameState.Pinned   => 0f,
            GameState.Skill    => 0f,
            GameState.GameOver => 0f,
            _                  => 1f,
        };

        OnStateChanged?.Invoke(state);
    }
}

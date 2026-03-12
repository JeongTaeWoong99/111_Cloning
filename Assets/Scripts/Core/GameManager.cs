using System;
using UnityEngine;

/// <summary>
/// 게임 상태를 보유하고 상태 변경 이벤트를 발행하는 싱글턴.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameState CurrentState { get; private set; }

    public event Action<GameState> OnStateChanged;

    private void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// 상태를 변경하고 구독자에게 알린다.
    /// </summary>
    public void SetState(GameState state)
    {
        CurrentState = state;
        OnStateChanged?.Invoke(state);
    }
}

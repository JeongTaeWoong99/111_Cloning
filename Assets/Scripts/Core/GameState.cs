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
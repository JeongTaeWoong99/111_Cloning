using System;
using UnityEngine;

/// <summary>
/// 한 층에서 사용하는 위치 참조 세트 (A 또는 B).
/// FloorManager가 A/B를 번갈아 Current/Next 역할로 사용한다.
/// </summary>
[Serializable]
public class FloorPositionSet
{
    public Transform playerStartSpawnPos;
    public Transform playerStartMovePos;
    public Transform enemySpawnPos;
    public Transform playerEndMovePos;
}
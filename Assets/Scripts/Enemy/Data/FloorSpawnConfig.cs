using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 한 종류의 적과 스폰 비율을 묶는 항목.
/// </summary>
[Serializable]
public class EnemyWeightEntry
{
    public EnemyData        data;
    [Range(0, 100)]
    public float            percent;
}

/// <summary>
/// 층별 적 스폰 비율과 총 마릿수를 정의하는 ScriptableObject.
/// </summary>
[CreateAssetMenu(menuName = "Game/Floor Spawn Config")]
public class FloorSpawnConfig : ScriptableObject
{
    [Tooltip("이 층에서 스폰할 적 총 마릿수")]
    public int totalEnemyCount;

    [Tooltip("적 종류별 스폰 비율 (합계가 100에 가까울수록 정확)")]
    public List<EnemyWeightEntry> entries;
}

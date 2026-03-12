using System;

/// <summary>
/// 스폰 목록 항목 — 어떤 적을 몇 마리 스폰할지 지정한다.
/// </summary>
[Serializable]
public class EnemySpawnEntry
{
    public EnemyData data;
    public int       count;
}

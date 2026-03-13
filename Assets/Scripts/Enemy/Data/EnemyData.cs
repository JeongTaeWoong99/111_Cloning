using UnityEngine;

/// <summary>
/// 적 한 종류의 스탯 정의. ScriptableObject로 관리한다.
/// </summary>
[CreateAssetMenu(menuName = "Game/Enemy Data")]
public class EnemyData : ScriptableObject
{
    public AnimatorOverrideController overrideController;
    public float                      maxHealth;
    public float                      moveSpeed;
}

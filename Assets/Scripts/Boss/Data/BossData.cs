using UnityEngine;

/// <summary>
/// 보스 한 종류의 스탯 + 공격/스킬 설정 데이터.
/// 공격 애니메이션은 "Attack", 스킬 애니메이션은 "Skill" 클립 이름을 공통 사용한다.
/// </summary>
[CreateAssetMenu(menuName = "Game/Boss Data")]
public class BossData : ScriptableObject
{
    public AnimatorOverrideController overrideController;
    public float                      maxHealth;
    public float                      moveSpeed;

    [Header("공격")]
    [Tooltip("공격 판정 거리 (m)")]
    public float attackRange;

    [Tooltip("공격 쿨다운 (초)")]
    public float attackCooldown;

    [Tooltip("근접 공격 데미지")]
    public float attackDamage;

    [Header("스킬")]
    [Tooltip("보스 고유 스킬 (쿨다운·발동 조건·범위·데미지)")]
    public BossSkillData skill;
}

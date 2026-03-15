using UnityEngine;

/// <summary>
/// 스킬 종류.
/// </summary>
public enum BossSkillType { Charge, SpearThrow }

/// <summary>
/// 보스 스킬 1개의 설정 데이터 — 쿨다운, 발동 조건, 범위, 데미지.
/// </summary>
[CreateAssetMenu(menuName = "Game/Boss Skill Data")]
public class BossSkillData : ScriptableObject
{
    [Tooltip("애니메이션 클립 이름 (예: \"Skill1\", \"Skill2\")")]
    public string skillId;

    [Tooltip("쿨다운 (초)")]
    public float cooldown;

    [Tooltip("이 체력 비율 이하일 때만 사용 가능 (0 = 항상 사용 가능)")]
    [Range(0f, 1f)]
    public float healthThreshold;

    [Tooltip("발동 가능 거리 (m)")]
    public float range;

    [Tooltip("스킬 데미지")]
    public float damage;

    [Tooltip("스킬 종류 (Charge / SpearThrow)")]
    public BossSkillType skillType;

    [Tooltip("SpearThrow 전용: 창 초기 속도 (m/s)")]
    public float spearThrowSpeed = 8f;

    [Tooltip("SpearThrow 전용: 최소 발사 각도 (도) — 낮을수록 멀리 날아감")]
    [Range(5f, 90f)]
    public float spearAngleMin = 15f;

    [Tooltip("SpearThrow 전용: 최대 발사 각도 (도) — 높을수록 바로 앞에 떨어짐")]
    [Range(5f, 90f)]
    public float spearAngleMax = 70f;
}

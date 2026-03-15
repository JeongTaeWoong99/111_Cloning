using System.Collections.Generic;
using Inventory;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Character Data")]
public class CharacterData : ScriptableObject
{
    [Header("기본 정보")]
    public string     characterName;
    public WeaponType weaponType;    // 허용 무기 타입 (Sword/Bow/Spear)

    [Header("기본 스탯")]
    public int   baseAttack;
    public int   baseHealth;
    public float baseAttackSpeed = 1f;  // 1.0 = 기본, 2.0 = 2배속

    [Header("전투")]
    [Tooltip("근접 공격 판정 거리 (m). 창병 > 검병.")]
    public float attackRange = 3f;


    [Header("스킬")]
    public string skillName;
    [TextArea(2, 4)]
    public string skillDescription;
    public Sprite skillSprite;
    [Tooltip("스킬 쿨다운 (초).")]
    public float skillCooldown = 10f;

    [Header("애니메이션")]
    [Tooltip("캐릭터 고유 애니메이터 오버라이드 컨트롤러")]
    public AnimatorOverrideController overrideController;

    /// <summary>
    /// overrideController에서 Attack 클립 길이를 읽어 반환합니다.
    /// 찾지 못하면 0.5f를 반환합니다.
    /// </summary>
    public float GetAttackClipLength()
    {
        if (overrideController == null) return 0.5f;

        var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        overrideController.GetOverrides(overrides);

        foreach (KeyValuePair<AnimationClip, AnimationClip> pair in overrides)
        {
            if (pair.Key == null || pair.Value == null) continue;

            // 키 클립명 앞부분으로 상태 매칭: "Attack_Bow" → "Attack"
            string baseName = pair.Key.name.Split('_')[0];
            if (string.Equals(baseName, "Attack", System.StringComparison.OrdinalIgnoreCase))
                return pair.Value.length;
        }

        return 0.5f;
    }
}

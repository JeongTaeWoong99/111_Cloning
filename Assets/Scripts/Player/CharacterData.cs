using Inventory;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Character Data")]
public class CharacterData : ScriptableObject
{
    [Header("기본 정보")]
    public string     characterName;
    public WeaponType weaponType;    // 허용 무기 타입 (Sword/Bow/Spear)

    [Header("기본 스탯")]
    public int baseAttack;
    public int baseHealth;

    [Header("애니메이션")]
    [Tooltip("캐릭터 고유 애니메이터 오버라이드 컨트롤러")]
    public AnimatorOverrideController overrideController;
}

using UnityEngine;

namespace Inventory
{
    [CreateAssetMenu(menuName = "Game/Item Data")]
    public class ItemData : ScriptableObject
    {
        [Header("아이템 정보")]
        [SerializeField, Tooltip("아이템 표시 이름")]
        public string displayName;

        [SerializeField, Tooltip("아이템 타입 (장착 슬롯 결정)")]
        public ItemType type;

        [SerializeField, Tooltip("인벤토리에 표시될 아이콘")]
        public Sprite icon;

        [Header("스탯")]
        [SerializeField, Tooltip("공격력 보너스")]
        public int attackBonus;

        [SerializeField, Tooltip("체력 보너스")]
        public int healthBonus;

        [SerializeField, Tooltip("공격 속도 보너스 (% 단위, 100 = +1.0배속 추가)")]
        public int attackSpeedBonus;

        [Header("무기 전용")]
        [SerializeField, Tooltip("무기 서브 타입 (비무기 아이템은 None)")]
        public WeaponType weaponType;
    }
}

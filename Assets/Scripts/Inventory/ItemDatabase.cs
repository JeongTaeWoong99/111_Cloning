using System.Collections.Generic;
using UnityEngine;

namespace Inventory
{
    [CreateAssetMenu(menuName = "Game/Item Database")]
    public class ItemDatabase : ScriptableObject
    {
        [SerializeField, Tooltip("게임 내 전체 아이템 목록")]
        public List<ItemData> allItems;

        /// <summary>
        /// SO 에셋명으로 ItemData를 검색합니다. (PlayerPrefs 저장/불러오기용)
        /// </summary>
        public ItemData FindByName(string itemName) => allItems.Find(x => x.name == itemName);
    }
}

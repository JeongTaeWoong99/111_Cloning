using System.Collections.Generic;
using UnityEngine;

namespace Inventory
{
    [CreateAssetMenu(menuName = "Game/Item Database")]
    public class ItemDatabase : ScriptableObject
    {
        [SerializeField, Tooltip("게임 내 전체 아이템 목록")]
        public List<ItemData> allItems;

        // SO 로드·플레이 모드 진입 시 자동 빌드되는 이름→아이템 Dictionary
        private Dictionary<string, ItemData> _nameCache;

        private void OnEnable()
        {
            // OnEnable은 에셋 로드·어셈블리 리로드 시 재호출되므로 항상 재빌드
            _nameCache = new Dictionary<string, ItemData>(allItems.Count);
            foreach (ItemData item in allItems)
            {
                if (item != null)
                    _nameCache[item.name] = item;
            }
        }

        /// <summary>
        /// SO 에셋명으로 ItemData를 O(1)로 검색합니다. (PlayerPrefs 저장/불러오기용)
        /// </summary>
        public ItemData FindByName(string itemName)
            => _nameCache.TryGetValue(itemName, out ItemData item) ? item : null;
    }
}

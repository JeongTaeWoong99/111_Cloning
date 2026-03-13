using System.Collections.Generic;
using Inventory;
using UnityEngine;

namespace UI
{
    /// <summary>
    /// 씬 시작 시 테스트 아이템을 인벤토리에 주입합니다.
    /// Inspector에서 _startItems에 ItemData SO를 연결하세요.
    /// </summary>
    public class InventoryInitializer : MonoBehaviour
    {
        // ──────────────────────────────────────────
        // [SerializeField] Private Fields
        // ──────────────────────────────────────────
        [Header("시작 아이템 (테스트용)")]
        [SerializeField, Tooltip("게임 시작 시 인벤토리에 추가할 아이템 목록")]
        private List<ItemData> _startItems;

        // ──────────────────────────────────────────
        // MonoBehaviour
        // ──────────────────────────────────────────
        private void Start()
        {
            foreach (ItemData item in _startItems)
            {
                PlayerInventory.Instance.AddItem(item);
            }
        }
    }
}

using Inventory;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// 인벤토리 슬롯 UI. 아무 타입의 아이템이나 받을 수 있습니다.
    /// </summary>
    public class InventorySlotUI : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IDropHandler
    {
        // ──────────────────────────────────────────
        // [SerializeField] Private Fields
        // ──────────────────────────────────────────
        [SerializeField, Tooltip("인벤토리 내 슬롯 인덱스")]
        private int _index;

        // ──────────────────────────────────────────
        // Private Fields
        // ──────────────────────────────────────────
        private static readonly Color HighlightColor = new(1f, 1f, 0.5f, 1f);
        private static readonly Color DefaultColor   = Color.white;

        private Image   _background;
        private ItemUI  _cachedItem;  // SetItem/ClearItem에서 갱신

        // ──────────────────────────────────────────
        // Properties
        // ──────────────────────────────────────────
        public int      Index       => _index;
        public ItemUI   CurrentItem => _cachedItem;

        // ──────────────────────────────────────────
        // MonoBehaviour
        // ──────────────────────────────────────────
        private void Awake()
        {
            _background = GetComponent<Image>();
        }

        // ──────────────────────────────────────────
        // Public Methods
        // ──────────────────────────────────────────

        /// <summary>슬롯에 아이템 UI를 배치하고 캐시를 갱신합니다.</summary>
        public void SetItem(ItemUI itemUI)
        {
            _cachedItem = itemUI;
            itemUI.transform.SetParent(transform);
            itemUI.transform.localPosition = Vector3.zero;
        }

        /// <summary>슬롯의 아이템 UI를 제거하고 캐시를 초기화합니다.</summary>
        public void ClearItem()
        {
            if (_cachedItem != null)
            {
                Destroy(_cachedItem.gameObject);
                _cachedItem = null;
            }
        }

        // ──────────────────────────────────────────
        // Pointer Handlers
        // ──────────────────────────────────────────
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_background != null)
            {
                _background.color = HighlightColor;
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_background != null)
            {
                _background.color = DefaultColor;
            }
        }

        // ──────────────────────────────────────────
        // Drop Handler
        // ──────────────────────────────────────────
        public void OnDrop(PointerEventData eventData)
        {
            ItemUI droppedItemUI = eventData.pointerDrag?.GetComponent<ItemUI>();

            if (droppedItemUI == null)
            {
                return;
            }

            PlayerInventory inventory = PlayerInventory.Instance;

            // 장착 슬롯에서 드래그된 경우 → 장착 해제 (Refresh가 UI 재생성)
            EquipmentSlotUI sourceEquipSlot = droppedItemUI.GetComponent<DraggableUI>()
                .PreviousParent?.GetComponent<EquipmentSlotUI>();

            if (sourceEquipSlot != null)
            {
                droppedItemUI.GetComponent<DraggableUI>().NotifyDropped();
                inventory.UnequipItem(sourceEquipSlot.SlotType);
                return;
            }

            // 인벤토리 내 이동: 수동 UI 조작
            ItemUI existing = CurrentItem;

            if (existing != null && existing != droppedItemUI)
            {
                // 드래그 소스 슬롯과 이 슬롯의 아이템을 swap
                Transform sourceParent = droppedItemUI.GetComponent<DraggableUI>().PreviousParent;
                SetItem(droppedItemUI);

                if (sourceParent != null)
                {
                    // 소스 슬롯이 InventorySlotUI면 SetItem으로 캐시도 함께 갱신
                    if (sourceParent.TryGetComponent(out InventorySlotUI sourceSlot))
                        sourceSlot.SetItem(existing);
                    else
                    {
                        existing.transform.SetParent(sourceParent);
                        existing.transform.localPosition = Vector3.zero;
                    }
                }
            }
            else
            {
                SetItem(droppedItemUI);
            }

            inventory.Save();
        }
    }
}

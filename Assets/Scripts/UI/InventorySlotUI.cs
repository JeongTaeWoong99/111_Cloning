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

        private Image _background;

        // ──────────────────────────────────────────
        // Properties
        // ──────────────────────────────────────────
        public int      Index       => _index;
        public ItemUI   CurrentItem => GetComponentInChildren<ItemUI>();

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

        /// <summary>슬롯에 아이템 UI를 배치합니다.</summary>
        public void SetItem(ItemUI itemUI)
        {
            itemUI.transform.SetParent(transform);
            itemUI.transform.localPosition = Vector3.zero;
        }

        /// <summary>슬롯의 아이템 UI를 제거합니다.</summary>
        public void ClearItem()
        {
            ItemUI current = CurrentItem;

            if (current != null)
            {
                Destroy(current.gameObject);
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
            ItemUI          existing  = CurrentItem;

            if (existing != null && existing != droppedItemUI)
            {
                // 드래그 소스 슬롯과 이 슬롯의 아이템을 swap
                Transform sourceParent = droppedItemUI.GetComponent<DraggableUI>().PreviousParent;

                SetItem(droppedItemUI);

                if (sourceParent != null)
                {
                    existing.transform.SetParent(sourceParent);
                    existing.transform.localPosition = Vector3.zero;
                }
            }
            else
            {
                SetItem(droppedItemUI);
            }

            // 장착 슬롯에서 드래그된 경우 → 장착 해제
            EquipmentSlotUI sourceEquipSlot = droppedItemUI.GetComponent<DraggableUI>()
                .PreviousParent?.GetComponent<EquipmentSlotUI>();

            if (sourceEquipSlot != null)
            {
                inventory.UnequipItem(sourceEquipSlot.SlotType);
            }

            inventory.Save();
        }
    }
}

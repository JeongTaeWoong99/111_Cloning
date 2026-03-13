using Inventory;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// 장착 슬롯 UI. 지정된 ItemType만 받을 수 있습니다.
    /// </summary>
    public class EquipmentSlotUI : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IDropHandler
    {
        // ──────────────────────────────────────────
        // [SerializeField] Private Fields
        // ──────────────────────────────────────────
        [SerializeField, Tooltip("이 슬롯이 허용하는 아이템 타입")]
        private ItemType _slotType;

        // ──────────────────────────────────────────
        // Private Fields
        // ──────────────────────────────────────────
        private static readonly Color HighlightColor = new(1f, 1f, 0.5f, 1f);
        private static readonly Color DefaultColor   = Color.white;
        private static readonly Color InvalidColor   = new(1f, 0.4f, 0.4f, 1f);

        private Image _background;

        // ──────────────────────────────────────────
        // Properties
        // ──────────────────────────────────────────
        public ItemType SlotType    => _slotType;
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
            if (_background == null)
            {
                return;
            }

            // 드래그 중인 아이템 타입이 맞지 않으면 빨간색으로 표시
            ItemUI dragging = eventData.pointerDrag?.GetComponent<ItemUI>();

            if (dragging != null && dragging.Data.type != _slotType)
            {
                _background.color = InvalidColor;
            }
            else
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

            // 타입 불일치 → DraggableUI가 OnEndDrag에서 previousParent로 복귀 처리
            if (droppedItemUI.Data.type != _slotType)
            {
                return;
            }

            PlayerInventory inventory = PlayerInventory.Instance;

            // 기존 장착 아이템이 있으면 드래그 소스 슬롯에 배치 (swap)
            ItemUI existingItemUI = CurrentItem;

            if (existingItemUI != null)
            {
                Transform sourceParent = droppedItemUI.GetComponent<DraggableUI>().PreviousParent;

                if (sourceParent != null)
                {
                    existingItemUI.transform.SetParent(sourceParent);
                    existingItemUI.transform.localPosition = Vector3.zero;
                }
            }

            inventory.EquipItem(droppedItemUI.Data, _slotType);
            SetItem(droppedItemUI);

            if (_background != null)
            {
                _background.color = DefaultColor;
            }
        }
    }
}

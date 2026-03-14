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

        private Image   _background;
        private ItemUI  _cachedItem;  // SetItem/ClearItem에서 갱신

        // ──────────────────────────────────────────
        // Properties
        // ──────────────────────────────────────────
        public ItemType SlotType    => _slotType;
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
            if (_background == null)
            {
                return;
            }

            ItemUI dragging = eventData.pointerDrag?.GetComponent<ItemUI>();

            if (dragging == null)
            {
                return;
            }

            // 아이템 타입 불일치
            if (dragging.Data.type != _slotType)
            {
                _background.color = InvalidColor;
                return;
            }

            // 무기 슬롯: 캐릭터 타입 불일치
            if (_slotType == ItemType.Weapon && IsWeaponTypeMismatch(dragging.Data))
            {
                _background.color = InvalidColor;
                return;
            }

            _background.color = HighlightColor;
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

            // 아이템 타입 불일치 → DraggableUI가 OnEndDrag에서 복귀 처리
            if (droppedItemUI.Data.type != _slotType)
            {
                return;
            }

            // 무기 슬롯: 캐릭터 타입 불일치
            if (_slotType == ItemType.Weapon && IsWeaponTypeMismatch(droppedItemUI.Data))
            {
                return;
            }

            // 드롭 성공 마킹 → OnEndDrag에서 원본 ItemUI 제거
            droppedItemUI.GetComponent<DraggableUI>().NotifyDropped();

            PlayerInventory.Instance.EquipItem(droppedItemUI.Data, _slotType);
            // Refresh가 새 ItemUI를 슬롯에 생성하므로 SetItem 불필요

            if (_background != null)
            {
                _background.color = DefaultColor;
            }
        }

        // ──────────────────────────────────────────
        // Private Methods
        // ──────────────────────────────────────────

        private bool IsWeaponTypeMismatch(ItemData item)
        {
            CharacterData ch = PlayerInventory.Instance.SelectedCharacter;
            bool mismatch = ch != null && item.weaponType != WeaponType.None
                                          && item.weaponType != ch.weaponType;

            return mismatch;
        }
    }
}

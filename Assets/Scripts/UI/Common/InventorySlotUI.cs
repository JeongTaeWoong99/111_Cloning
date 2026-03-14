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
        // Private Fields
        // ──────────────────────────────────────────
        // _index는 Awake에서 sibling 순서로 자동 설정 (Inspector 의존 제거)
        private int _index;

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
            // Inventory 컨테이너 내 자식 순서를 인덱스로 사용 (Inspector 설정 불필요)
            _index = transform.GetSiblingIndex();
        }

        // ──────────────────────────────────────────
        // Public Methods
        // ──────────────────────────────────────────

        /// <summary>슬롯에 아이템 UI를 배치하고 캐시를 갱신합니다.</summary>
        public void SetItem(ItemUI itemUI)
        {
            if (_cachedItem != null && _cachedItem != itemUI)
                Destroy(_cachedItem.gameObject);  // 기존 아이템 파괴 (중복 방지)
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

        /// <summary>파괴 없이 _cachedItem 참조만 해제합니다. 아이템이 다른 슬롯으로 이동된 경우 사용합니다.</summary>
        public void DetachItem() => _cachedItem = null;

        // ──────────────────────────────────────────
        // Pointer Handlers
        // ──────────────────────────────────────────
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_background != null)
                _background.color = HighlightColor;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_background != null)
                _background.color = DefaultColor;
        }

        // ──────────────────────────────────────────
        // Drop Handler
        // ──────────────────────────────────────────
        public void OnDrop(PointerEventData eventData)
        {
            ItemUI droppedItemUI = eventData.pointerDrag?.GetComponent<ItemUI>();
            if (droppedItemUI == null) return;

            // 드롭 위치에 EquipmentSlotUI가 겹쳐있으면 양보 → EquipmentSlotUI.OnDrop이 처리
            foreach (GameObject hovered in eventData.hovered)
            {
                if (hovered.GetComponent<EquipmentSlotUI>() != null) return;
            }

            PlayerInventory inventory = PlayerInventory.Instance;
            DraggableUI draggable = droppedItemUI.GetComponent<DraggableUI>();

            // 장착 슬롯에서 드래그된 경우
            EquipmentSlotUI sourceEquipSlot = draggable.PreviousParent?.GetComponent<EquipmentSlotUI>();

            if (sourceEquipSlot != null)
            {
                HandleUnequipDrop(droppedItemUI, sourceEquipSlot, draggable, inventory);
                return;
            }

            // 인벤토리 내 이동
            HandleInventoryDrop(droppedItemUI, draggable, inventory);
        }

        // ──────────────────────────────────────────
        // Private Methods
        // ──────────────────────────────────────────

        /// <summary>장착 슬롯 → 인벤토리 드롭 처리.</summary>
        private void HandleUnequipDrop(ItemUI droppedItemUI, EquipmentSlotUI sourceEquipSlot,
                                       DraggableUI draggable, PlayerInventory inventory)
        {
            ItemUI existing = CurrentItem;

            if (existing == null)
            {
                // 빈 슬롯: 장착 해제 후 이 슬롯에 배치
                draggable.NotifyDropped();
                inventory.UnequipItem(sourceEquipSlot.SlotType, _index);
            }
            else
            {
                // 기존 아이템: 타입 호환 시 교환, 불가 시 취소
                if (CanEquip(existing.Data, sourceEquipSlot.SlotType, inventory))
                {
                    draggable.NotifyDropped();
                    inventory.EquipItem(existing.Data, sourceEquipSlot.SlotType, _index);
                }
                // 불호환: NotifyDropped 호출 안 함 → OnEndDrag에서 ReturnToPreviousParent
            }
        }

        /// <summary>인벤토리 ↔ 인벤토리 드롭 처리.</summary>
        private void HandleInventoryDrop(ItemUI droppedItemUI, DraggableUI draggable,
                                         PlayerInventory inventory)
        {
            Transform sourceParent = draggable.PreviousParent;
            InventorySlotUI sourceSlot = sourceParent != null
                ? sourceParent.GetComponent<InventorySlotUI>()
                : null;

            ItemUI existing = CurrentItem;

            if (existing != null && existing != droppedItemUI)
            {
                // 두 슬롯 모두 아이템 있음: 스왑
                if (sourceSlot == null) return;

                _cachedItem = null;           // 기존 아이템 파괴 방지
                SetItem(droppedItemUI);
                sourceSlot.DetachItem();      // 소스 슬롯 캐시 해제 (droppedItemUI 파괴 방지)
                sourceSlot.SetItem(existing);
                inventory.SwapItems(_index, sourceSlot.Index);
            }
            else
            {
                // 빈 슬롯으로 이동
                if (sourceSlot != null) sourceSlot.DetachItem();  // 소스 슬롯 캐시 해제
                SetItem(droppedItemUI);
                if (sourceSlot != null) inventory.SwapItems(_index, sourceSlot.Index);
            }

            inventory.Save();
        }

        /// <summary>아이템이 장착 슬롯 타입에 호환되는지 확인합니다.</summary>
        private static bool CanEquip(ItemData item, ItemType slotType, PlayerInventory inventory)
        {
            if (item.type != slotType) return false;

            if (slotType == ItemType.Weapon && item.weaponType != WeaponType.None)
            {
                CharacterData ch = inventory.SelectedCharacter;
                if (ch != null && item.weaponType != ch.weaponType) return false;
            }

            return true;
        }
    }
}

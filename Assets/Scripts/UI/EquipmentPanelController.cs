using Inventory;
using UnityEngine;

namespace UI
{
    /// <summary>
    /// 장비 인벤토리 패널을 관리합니다. I키로 토글하며, PlayerInventory 상태를 UI에 반영합니다.
    /// </summary>
    public class EquipmentPanelController : MonoBehaviour
    {
        // ──────────────────────────────────────────
        // [SerializeField] Private Fields
        // ──────────────────────────────────────────
        [Header("패널")]
        [SerializeField, Tooltip("토글할 장비 패널 루트 오브젝트")]
        private GameObject _panel;

        [Header("슬롯")]
        [SerializeField, Tooltip("장착 슬롯 4개 (Weapon/Armor/Helmet/Ring 순서)")]
        private EquipmentSlotUI[] _equipSlots;

        [SerializeField, Tooltip("인벤토리 슬롯 25개")]
        private InventorySlotUI[] _inventorySlots;

        [Header("프리팹")]
        [SerializeField, Tooltip("아이템 UI 프리팹 (ItemUI 컴포넌트 부착)")]
        private ItemUI _itemPrefab;

        // ──────────────────────────────────────────
        // MonoBehaviour
        // ──────────────────────────────────────────
        private void Awake()
        {
            // Start보다 먼저 구독해야 InventoryInitializer.Start()의 AddItem 이벤트를 놓치지 않음
            PlayerInventory.Instance.OnChanged += Refresh;

            if (_panel != null)
            {
                _panel.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (PlayerInventory.Instance != null)
            {
                PlayerInventory.Instance.OnChanged -= Refresh;
            }
        }

        private void Start()
        {
            // Awake에서 이미 구독 완료. Start는 초기 Refresh용
            Refresh();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.I))
            {
                Toggle();
            }
        }

        // ──────────────────────────────────────────
        // Public Methods
        // ──────────────────────────────────────────

        /// <summary>패널 열림/닫힘을 전환합니다.</summary>
        public void Toggle()
        {
            if (_panel == null)
            {
                return;
            }

            if (_panel.activeSelf)
            {
                Close();
            }
            else
            {
                Open();
            }
        }

        /// <summary>패널을 열고 UI를 갱신합니다.</summary>
        public void Open()
        {
            if (_panel == null)
            {
                return;
            }

            _panel.SetActive(true);
            Refresh();
        }

        /// <summary>패널을 닫습니다.</summary>
        public void Close()
        {
            if (_panel == null)
            {
                return;
            }

            _panel.SetActive(false);
        }

        // ──────────────────────────────────────────
        // Private Methods
        // ──────────────────────────────────────────

        private void Refresh()
        {
            if (_panel == null)
            {
                return;
            }

            // 패널이 꺼진 상태에서도 데이터를 갱신해둬야
            // 이후 수동 활성화 또는 Open() 호출 시 즉시 표시됨
            ClearAllSlots();
            PopulateInventorySlots();
            PopulateEquipSlots();
        }

        private void ClearAllSlots()
        {
            foreach (InventorySlotUI slot in _inventorySlots)
            {
                slot.ClearItem();
            }

            foreach (EquipmentSlotUI slot in _equipSlots)
            {
                slot.ClearItem();
            }
        }

        private void PopulateInventorySlots()
        {
            var items = PlayerInventory.Instance.Items;

            for (int i = 0; i < items.Count && i < _inventorySlots.Length; i++)
            {
                ItemUI itemUI = Instantiate(_itemPrefab, _inventorySlots[i].transform);
                itemUI.Initialize(items[i]);
                itemUI.transform.localPosition = Vector3.zero;
            }
        }

        private void PopulateEquipSlots()
        {
            foreach (EquipmentSlotUI slot in _equipSlots)
            {
                ItemData equipped = PlayerInventory.Instance.GetEquipped(slot.SlotType);

                if (equipped == null)
                {
                    continue;
                }

                ItemUI itemUI = Instantiate(_itemPrefab, slot.transform);
                itemUI.Initialize(equipped);
                itemUI.transform.localPosition = Vector3.zero;
            }
        }
    }
}

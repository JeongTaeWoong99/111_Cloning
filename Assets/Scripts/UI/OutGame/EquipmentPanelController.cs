using System.Collections;
using Inventory;
using UnityEngine;

namespace UI.OutGame
{
    /// <summary>
    /// 장비 인벤토리 패널을 전담합니다. 슬롯 Refresh 및 카메라 스크롤을 처리합니다.
    /// 패널 전환 총괄은 OutGameUIManager가 담당합니다.
    /// </summary>
    public class EquipmentPanelController : MonoBehaviour
    {
        // ──────────────────────────────────────────
        // [SerializeField] Private Fields
        // ──────────────────────────────────────────
        [Header("장비 패널")]
        [SerializeField, Tooltip("토글할 장비 패널 루트 오브젝트")]
        private GameObject _equipmentPanel;

        [Header("슬롯")]
        [SerializeField, Tooltip("장착 슬롯 4개 (Weapon/Armor/Helmet/Ring 순서)")]
        private EquipmentSlotUI[] _equipSlots;

        [SerializeField, Tooltip("인벤토리 슬롯 25개")]
        private InventorySlotUI[] _inventorySlots;

        [Header("프리팹")]
        [SerializeField, Tooltip("아이템 UI 프리팹 (ItemUI 컴포넌트 부착)")]
        private ItemUI _itemPrefab;

        [Header("카메라 스크롤")]
        [SerializeField, Tooltip("카메라 스크롤 컨트롤러 (1번 씬)")]
        private CameraScrollController _cameraScroll;

        [SerializeField, Tooltip("패널 열릴 때 카메라 Y 위치")]
        private float _openCameraY;

        [SerializeField, Tooltip("패널 닫힐 때 카메라 Y 위치 (기본 위치)")]
        private float _closeCameraY;

        // ──────────────────────────────────────────
        // Properties
        // ──────────────────────────────────────────
        /// <summary>장비 패널이 현재 활성화 상태인지 반환합니다.</summary>
        public bool IsOpen => _equipmentPanel != null && _equipmentPanel.activeSelf;

        // ──────────────────────────────────────────
        // Private Fields
        // ──────────────────────────────────────────
        // 같은 프레임에 OnChanged가 여러 번 발생해도 LateUpdate에서 한 번만 Refresh
        private bool _isDirty;

        // ──────────────────────────────────────────
        // MonoBehaviour
        // ──────────────────────────────────────────
        private void Awake()
        {
            // Start보다 먼저 구독해야 InventoryInitializer.Start()의 AddItem 이벤트를 놓치지 않음
            PlayerInventory.Instance.OnChanged += RequestRefresh;

            if (_equipmentPanel != null)
            {
                _equipmentPanel.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (PlayerInventory.Instance != null)
            {
                PlayerInventory.Instance.OnChanged -= RequestRefresh;
            }
        }

        private void Start()
        {
            // Awake에서 이미 구독 완료. Start는 초기 Refresh 예약
            RequestRefresh();
        }

        private void LateUpdate()
        {
            if (!_isDirty) return;
            _isDirty = false;
            Refresh();
        }

        // ──────────────────────────────────────────
        // Public Methods
        // ──────────────────────────────────────────

        /// <summary>카메라 이동 완료 후 패널을 엽니다. yield return으로 완료를 대기할 수 있습니다.</summary>
        public Coroutine Open()
        {
            if (_equipmentPanel == null) return null;
            return StartCoroutine(OpenRoutine());
        }

        /// <summary>패널을 즉시 닫고 카메라를 이동합니다. yield return으로 완료를 대기할 수 있습니다.</summary>
        public Coroutine Close()
        {
            if (_equipmentPanel == null) return null;
            return StartCoroutine(CloseRoutine());
        }

        // ──────────────────────────────────────────
        // Private Coroutines
        // ──────────────────────────────────────────

        // 카메라 이동 완료 → 패널 표시
        private IEnumerator OpenRoutine()
        {
            if (_cameraScroll != null)
                yield return StartCoroutine(_cameraScroll.ScrollTo(_openCameraY));

            _equipmentPanel.SetActive(true);
            Refresh();
        }

        // 패널 즉시 숨김 → 카메라 이동
        private IEnumerator CloseRoutine()
        {
            _equipmentPanel.SetActive(false);

            if (_cameraScroll != null)
                yield return StartCoroutine(_cameraScroll.ScrollTo(_closeCameraY));
        }

        // ──────────────────────────────────────────
        // Private Methods
        // ──────────────────────────────────────────

        private void RequestRefresh() => _isDirty = true;

        private void Refresh()
        {
            if (_equipmentPanel == null)
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

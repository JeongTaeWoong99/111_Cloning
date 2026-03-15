using Inventory;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// 인벤토리/장착 슬롯에 표시되는 아이템 비주얼 컴포넌트.
    /// Item.prefab에 부착하며, EquipmentPanelController가 Instantiate하여 사용합니다.
    /// </summary>
    [RequireComponent(typeof(DraggableUI))]
    [RequireComponent(typeof(CanvasGroup))]
    [RequireComponent(typeof(Image))]
    public class ItemUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        // ──────────────────────────────────────────
        // Private Fields
        // ──────────────────────────────────────────
        private Image _icon;

        // ──────────────────────────────────────────
        // Properties
        // ──────────────────────────────────────────
        public ItemData Data { get; private set; }

        // ──────────────────────────────────────────
        // MonoBehaviour
        // ──────────────────────────────────────────
        private void Awake()
        {
            _icon = GetComponent<Image>();
        }

        // ──────────────────────────────────────────
        // Public Methods
        // ──────────────────────────────────────────

        // ── 툴팁 ──────────────────────────────────────────────────────
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (Data != null)
                ItemTooltipUI.Instance.Show(Data, transform.position);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            ItemTooltipUI.Instance.Hide();
        }

        /// <summary>아이템 데이터를 설정하고 아이콘 스프라이트를 갱신합니다.</summary>
        public void Initialize(ItemData data)
        {
            Data = data;

            // Awake보다 먼저 호출되는 경우 대비 (e.g. Instantiate 직후 동기 호출)
            if (_icon == null)
            {
                _icon = GetComponentInChildren<Image>();
            }

            if (_icon != null)
            {
                _icon.sprite = data.icon;
            }
        }
    }
}

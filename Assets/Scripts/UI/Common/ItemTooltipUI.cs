using Inventory;
using TMPro;
using UnityEngine;

namespace UI
{
    /// <summary>
    /// 아이템 호버 시 표시되는 툴팁 패널.
    /// Canvas 최상단에 배치하며, ItemUI의 OnPointerEnter/Exit에서 Show/Hide를 호출한다.
    /// </summary>
    public class ItemTooltipUI : MonoBehaviour
    {
        public static ItemTooltipUI Instance { get; private set; }

        // ── Serialized Fields ─────────────────────────────────────────
        [Header("Name 행 (weaponType != None일 때만 활성)")]
        [SerializeField] private GameObject _nameRow;
        [SerializeField] private TMP_Text   _nameText;

        [Header("공통 행")]
        [SerializeField] private TMP_Text _typeText;
        [SerializeField] private TMP_Text _attackText;
        [SerializeField] private TMP_Text _healthText;
        [SerializeField] private TMP_Text _speedText;

        // ── Fields ────────────────────────────────────────────────────
        private RectTransform _rect;

        // ── MonoBehaviour ─────────────────────────────────────────────
        private void Awake()
        {
            Instance = this;
            _rect    = GetComponent<RectTransform>();

            // 툴팁 패널이 레이캐스트를 차단하면 아이템 아이콘의 OnPointerExit가 즉시 발동해
            // 켜졌다 꺼졌다 무한 반복되므로, 반드시 blocksRaycasts를 끈다.
            CanvasGroup cg = gameObject.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            cg.interactable   = false;

            gameObject.SetActive(false);
        }

        // ── Public Methods ────────────────────────────────────────────
        /// <summary>아이템 위치 바로 위에 툴팁을 표시한다.</summary>
        public void Show(ItemData data, Vector2 itemScreenPos)
        {
            // Name 행: 무기 타입이 지정된 아이템만 표시
            bool hasWeaponType = data.weaponType != WeaponType.None;
            _nameRow.SetActive(hasWeaponType);
                _nameText.text = $"{data.displayName}";
            
            if (hasWeaponType)
                _typeText.text = data.type.ToString() + $" ({GetExclusiveLabel(data.weaponType)})";
            else
                _typeText.text = data.type.ToString();
            _attackText.text = "+" + data.attackBonus.ToString();
            _healthText.text = "+" + data.healthBonus.ToString();
            _speedText.text  = "+" + data.attackSpeedBonus.ToString() + "%";

            // 아이템 아이콘 바로 위에 고정
            _rect.position = itemScreenPos + new Vector2(0f, 275f);

            gameObject.SetActive(true);
        }

        /// <summary>툴팁을 숨긴다.</summary>
        public void Hide() => gameObject.SetActive(false);

        // ── Private Methods ───────────────────────────────────────────
        private static string GetExclusiveLabel(WeaponType weaponType) => weaponType switch
        {
            WeaponType.Bow   => "Archers",
            WeaponType.Spear => "Spearmen",
            WeaponType.Sword => "Swordsmen",
            _                => weaponType.ToString()
        };
    }
}

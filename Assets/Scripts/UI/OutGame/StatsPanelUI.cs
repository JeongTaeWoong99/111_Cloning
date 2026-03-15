using Inventory;
using TMPro;
using UnityEngine;

namespace UI
{
    /// <summary>
    /// 왼쪽 State 패널 UI.
    /// 캐릭터 변경 또는 장비 변경 시 Name / ATK / HP / Speed를 실시간 갱신합니다.
    /// </summary>
    public class StatsPanelUI : MonoBehaviour
    {
        // ──────────────────────────────────────────
        // [SerializeField] Private Fields
        // ──────────────────────────────────────────
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _atkText;
        [SerializeField] private TMP_Text _hpText;
        [SerializeField] private TMP_Text _speedText;

        // ──────────────────────────────────────────
        // MonoBehaviour
        // ──────────────────────────────────────────
        private void Start()
        {
            PlayerInventory.Instance.OnChanged          += Refresh;
            PlayerInventory.Instance.OnCharacterChanged += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (PlayerInventory.Instance == null)
            {
                return;
            }

            PlayerInventory.Instance.OnChanged          -= Refresh;
            PlayerInventory.Instance.OnCharacterChanged -= Refresh;
        }

        // ──────────────────────────────────────────
        // Private Methods
        // ──────────────────────────────────────────
        private void Refresh()
        {
            if (PlayerStats.Instance == null) return;

            CharacterData ch = PlayerInventory.Instance?.SelectedCharacter;
            if (_nameText != null)
                _nameText.text = ch != null ? ch.characterName : "";

            _atkText.text   = $"ATK : {PlayerStats.Instance.TotalAttack}";
            _hpText.text    = $"HP : {PlayerStats.Instance.TotalHealth}";
            _speedText.text = $"SPD : {PlayerStats.Instance.AttackInterval:F2}/s";

            // Debug.Log($"[StatsPanelUI] 갱신 → SPD: {PlayerStats.Instance.AttackInterval:F4}s"
            //         + $" (공속배율: {PlayerStats.Instance.TotalAttackSpeed:F3})");
        }
    }
}

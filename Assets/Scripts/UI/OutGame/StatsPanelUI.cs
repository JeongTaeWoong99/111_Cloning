using Inventory;
using TMPro;
using UnityEngine;

namespace UI
{
    /// <summary>
    /// 장비 패널 내 스텟 표시 UI.
    /// 장비 변경 또는 캐릭터 변경 시 ATK/HP를 실시간 갱신합니다.
    /// </summary>
    public class StatsPanelUI : MonoBehaviour
    {
        // ──────────────────────────────────────────
        // [SerializeField] Private Fields
        // ──────────────────────────────────────────
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
            if (PlayerStats.Instance == null)
            {
                return;
            }

            _atkText.text   = $"ATK: {PlayerStats.Instance.TotalAttack}";
            _hpText.text    = $"HP: {PlayerStats.Instance.TotalHealth}";
            _speedText.text = $"SPD: {PlayerStats.Instance.AttackInterval:F4}";

            // Debug.Log($"[StatsPanelUI] 갱신 → SPD: {PlayerStats.Instance.AttackInterval:F4}s"
            //         + $" (공속배율: {PlayerStats.Instance.TotalAttackSpeed:F3})");
        }
    }
}

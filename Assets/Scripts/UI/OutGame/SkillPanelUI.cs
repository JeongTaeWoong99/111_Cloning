using Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// 오른쪽 Skill 패널 UI.
    /// 캐릭터 변경 시 스킬 이름 / 아이콘 / 설명을 실시간 갱신합니다.
    /// </summary>
    public class SkillPanelUI : MonoBehaviour
    {
        // ──────────────────────────────────────────
        // [SerializeField] Private Fields
        // ──────────────────────────────────────────
        [SerializeField] private TMP_Text _skillNameText;
        [SerializeField] private Image    _skillImage;
        [SerializeField] private TMP_Text _skillDescText;

        // ──────────────────────────────────────────
        // MonoBehaviour
        // ──────────────────────────────────────────
        private void Start()
        {
            PlayerInventory.Instance.OnCharacterChanged += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (PlayerInventory.Instance == null) return;
            PlayerInventory.Instance.OnCharacterChanged -= Refresh;
        }

        // ──────────────────────────────────────────
        // Private Methods
        // ──────────────────────────────────────────
        private void Refresh()
        {
            CharacterData data = PlayerInventory.Instance?.SelectedCharacter;

            if (_skillNameText != null) _skillNameText.text = data != null ? data.skillName        : "";
            if (_skillDescText != null) _skillDescText.text = data != null ? data.skillDescription : "";
            if (_skillImage    != null) _skillImage.sprite  = data?.skillSprite;
        }
    }
}

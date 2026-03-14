using System;
using Inventory;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// OutGame 씬 전용 캐릭터 선택 UI.
    /// 좌/우 버튼으로 캐릭터를 순환하며 미리보기 애니메이션을 갱신합니다.
    /// </summary>
    public class CharacterSelectUI : MonoBehaviour
    {
        // ──────────────────────────────────────────
        // [SerializeField] Private Fields
        // ──────────────────────────────────────────
        [SerializeField, Tooltip("선택 가능한 캐릭터 목록")]
        private CharacterData[] _characters;

        [SerializeField, Tooltip("캐릭터 미리보기 Animator")]
        private Animator _previewAnimator;

        [SerializeField, Tooltip("이전 캐릭터 버튼")]
        private Button _prevButton;

        [SerializeField, Tooltip("다음 캐릭터 버튼")]
        private Button _nextButton;

        // ──────────────────────────────────────────
        // Private Fields
        // ──────────────────────────────────────────
        private int _currentIndex;

        // ──────────────────────────────────────────
        // MonoBehaviour
        // ──────────────────────────────────────────
        private void Start()
        {
            _prevButton.onClick.AddListener(OnPrevClicked);
            _nextButton.onClick.AddListener(OnNextClicked);

            // 저장된 캐릭터로 초기 인덱스 결정
            string savedName = PlayerPrefs.GetString("selected_character", "");
            _currentIndex = Array.FindIndex(_characters, c => c.name == savedName);
            if (_currentIndex < 0) _currentIndex = 0;

            ApplyCharacter();
        }

        // ──────────────────────────────────────────
        // Private Methods
        // ──────────────────────────────────────────
        private void OnPrevClicked()
        {
            _currentIndex = (_currentIndex - 1 + _characters.Length) % _characters.Length;
            ApplyCharacter();
        }

        private void OnNextClicked()
        {
            _currentIndex = (_currentIndex + 1) % _characters.Length;
            ApplyCharacter();
        }

        private void ApplyCharacter()
        {
            if (_characters.Length == 0)
            {
                return;
            }

            CharacterData data = _characters[_currentIndex];

            // 미리보기 애니메이터에 캐릭터 고유 컨트롤러 적용 → Idle 자동 재생
            if (_previewAnimator != null)
            {
                _previewAnimator.runtimeAnimatorController = data.overrideController;
            }

            PlayerInventory.Instance.SelectCharacter(data);
        }
    }
}

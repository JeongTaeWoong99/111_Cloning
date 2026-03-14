using System.Collections;
using UnityEngine;

namespace UI.OutGame
{
    /// <summary>
    /// OutGame 씬의 패널 전환을 총괄합니다.
    /// I키 입력을 감지하고 기본 패널 ↔ 장비 패널 전환을 관리합니다.
    /// </summary>
    public class OutGameUIManager : MonoBehaviour
    {
        // ──────────────────────────────────────────
        // [SerializeField] Private Fields
        // ──────────────────────────────────────────
        [Header("패널")]
        [SerializeField, Tooltip("씬 시작 시 보이는 기본 패널")]
        private GameObject _defaultPanel;

        [SerializeField, Tooltip("장비 인벤토리 패널 컨트롤러")]
        private EquipmentPanelController _equipmentPanel;

        [SerializeField, Tooltip("씬 전환 컨트롤러")]
        private SceneController _sceneController;

        // ──────────────────────────────────────────
        // Private Fields
        // ──────────────────────────────────────────
        // 전환 중 I키 중복 입력 방지
        private bool _isTransitioning;

        // ──────────────────────────────────────────
        // MonoBehaviour
        // ──────────────────────────────────────────
        private void Start()
        {
            _defaultPanel.SetActive(true);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.I)) // 인벤토리
                ToggleEquipmentPanel();

            if (Input.GetKeyDown(KeyCode.T)) // 인게임 씬으로 이동
                _sceneController?.LoadInGame();
        }

        // ──────────────────────────────────────────
        // Private Methods
        // ──────────────────────────────────────────
        public void ToggleEquipmentPanel()
        {
            if (_isTransitioning) return;
            StartCoroutine(DoToggle());
        }

        private IEnumerator DoToggle()
        {
            _isTransitioning = true;

            if (_equipmentPanel.IsOpen)
            {
                // 장비 패널 즉시 닫기 → 카메라 이동 → 기본 패널 표시
                yield return _equipmentPanel.Close();
                _defaultPanel.SetActive(true);
            }
            else
            {
                // 기본 패널 즉시 닫기 → 카메라 이동 → 장비 패널 표시
                _defaultPanel.SetActive(false);
                yield return _equipmentPanel.Open();
            }

            _isTransitioning = false;
        }
    }
}

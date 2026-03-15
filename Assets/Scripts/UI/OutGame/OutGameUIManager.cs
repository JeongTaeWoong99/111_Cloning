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

        [SerializeField, Tooltip("맵 선택 패널 (2_Ingame / 3_Ingame 진입 버튼 포함)")]
        private GameObject _mapPanel;

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
            _mapPanel.SetActive(false);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.I)) // 장비 인벤토리 패널 열기
                ToggleEquipmentPanel();

            if (Input.GetKeyDown(KeyCode.M)) // 맵 패널 열기
                ToggleMapPanel();
        }

        // ──────────────────────────────────────────
        // Private Methods
        // ──────────────────────────────────────────
        public void ToggleEquipmentPanel()
        {
            if (_isTransitioning) return;
            StartCoroutine(DoToggleEquipment());
        }

        public void ToggleMapPanel()
        {
            if (_isTransitioning) return;
            StartCoroutine(DoToggleMap());
        }

        private IEnumerator DoToggleEquipment()
        {
            _isTransitioning = true;

            if (_equipmentPanel.IsOpen)
            {
                // 장비 패널 닫기 → 기본 패널 표시
                yield return _equipmentPanel.Close();
                _defaultPanel.SetActive(true);
            }
            else
            {
                // 기본 패널 닫기 → 장비 패널 표시
                _defaultPanel.SetActive(false);
                yield return _equipmentPanel.Open();
            }

            _isTransitioning = false;
        }

        private IEnumerator DoToggleMap()
        {
            _isTransitioning = true;

            bool mapIsOpen = _mapPanel.activeSelf;

            if (mapIsOpen)
            {
                // 맵 패널 닫기 → 기본 패널 표시
                _mapPanel.SetActive(false);
                _defaultPanel.SetActive(true);
            }
            else
            {
                // 장비 패널이 열려 있으면 먼저 닫기
                if (_equipmentPanel.IsOpen)
                    yield return _equipmentPanel.Close();

                // 기본 패널 닫기 → 맵 패널 표시
                _defaultPanel.SetActive(false);
                _mapPanel.SetActive(true);
            }

            _isTransitioning = false;
        }
    }
}

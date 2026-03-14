using TMPro;
using UnityEngine;

namespace UI.InGame
{
    /// <summary>
    /// InGame HUD 관리자 — 층 텍스트 갱신 등 인게임 UI를 담당한다.
    /// </summary>
    public class InGameUIManager : MonoBehaviour
    {
        // ── Serialized Fields ─────────────────────────────────────────
        [SerializeField] private TMP_Text _floorText;
        [SerializeField] private string   _towerName = "Beginner's Tower";

        // ── MonoBehaviour ─────────────────────────────────────────────
        private void OnEnable()  => FloorManager.OnFloorChanged += UpdateFloorText;
        private void OnDisable() => FloorManager.OnFloorChanged -= UpdateFloorText;

        // ── Private Methods ───────────────────────────────────────────
        private void UpdateFloorText(int floor)
            => _floorText.text = $"{_towerName}\n{floor}F";
    }
}

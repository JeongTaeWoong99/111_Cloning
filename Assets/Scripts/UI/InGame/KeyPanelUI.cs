using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 인게임 HUD — A/S/D/F 키 버튼의 입력·쿨타임 상태를 실시간으로 반영한다.
/// </summary>
public class KeyPanelUI : MonoBehaviour
{
    // ── Serialized Fields ─────────────────────────────────────────
    [Header("버튼")]
    [SerializeField] private Button   _aButton;
    [SerializeField] private Button   _sButton;
    [SerializeField] private Button   _dButton;
    [SerializeField] private Button   _fButton;

    [Header("버튼 레이블 텍스트")]
    [SerializeField] private TMP_Text _aLabel;
    [SerializeField] private TMP_Text _sLabel;
    [SerializeField] private TMP_Text _dLabel;
    [SerializeField] private TMP_Text _fLabel;

    [Header("참조")]
    [SerializeField] private PlayerCombat       _playerCombat;
    [SerializeField] private PlayerSkillHandler _skillHandler;

    // ── MonoBehaviour ─────────────────────────────────────────────
    private void Update()
    {
        // A: 누르는 동안 비활성화
        _aButton.interactable = !Input.GetKey(KeyCode.A);

        // S / D / F: 쿨타임 잔여 시간에 따라 텍스트·활성화 상태 갱신
        RefreshCooldownButton(_sButton, _sLabel, _playerCombat.ParryCooldownRemaining, "S");
        RefreshCooldownButton(_dButton, _dLabel, _playerCombat.DashCooldownRemaining,  "D");
        RefreshCooldownButton(_fButton, _fLabel, _skillHandler.SkillCooldownRemaining, "F");
    }

    // ── Private Methods ───────────────────────────────────────────
    /// <summary>
    /// 쿨타임 잔여 시간에 따라 버튼 활성화 여부와 레이블 텍스트를 갱신한다.
    /// </summary>
    private static void RefreshCooldownButton(Button btn, TMP_Text label, float remaining, string keyName)
    {
        bool onCooldown = remaining > 0f;
        btn.interactable = !onCooldown;
        label.text = onCooldown ? remaining.ToString("F1") : keyName;
    }
}

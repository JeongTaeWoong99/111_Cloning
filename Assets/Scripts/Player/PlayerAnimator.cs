using Inventory;
using UnityEngine;

/// <summary>
/// 플레이어 애니메이션을 관리합니다.
/// 선택된 캐릭터의 AnimatorOverrideController를 런타임에 적용합니다.
/// Player 오브젝트에 부착하세요.
/// </summary>
[RequireComponent(typeof(Animator))]
public class PlayerAnimator : MonoBehaviour
{
    // ──────────────────────────────────────────
    // [SerializeField] Private Fields
    // ──────────────────────────────────────────
    [SerializeField, Tooltip("캐릭터 미선택 시 사용할 기본 Override Controller")]
    private AnimatorOverrideController _defaultController;

    // ──────────────────────────────────────────
    // Private Fields
    // ──────────────────────────────────────────
    private Animator _animator;

    // ──────────────────────────────────────────
    // MonoBehaviour
    // ──────────────────────────────────────────
    private void Awake()
    {
        _animator = GetComponent<Animator>();

        // Awake에서 구독: SceneManager.sceneLoaded 이벤트(Awake 이후, Start 이전 발행)를 놓치지 않기 위함
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.OnChanged          += ApplyAnimation;
            PlayerInventory.Instance.OnCharacterChanged += ApplyAnimation;
        }

        ApplyAnimation();
    }

    private void OnDestroy()
    {
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.OnChanged          -= ApplyAnimation;
            PlayerInventory.Instance.OnCharacterChanged -= ApplyAnimation;
        }
    }

    // ──────────────────────────────────────────
    // Public Methods — 애니메이션 재생
    // ──────────────────────────────────────────
    public void PlayIdle()   => _animator.Play("Idle");
    public void PlayRun()    => _animator.Play("Run");
    public void PlayAttack() => _animator.Play("Attack");
    public void PlayDash()   => _animator.Play("Dash");
    public void PlayDie()    => _animator.Play("Die");
    public void PlaySkill()  => _animator.Play("Skill");  // 추후 에셋 준비 후 사용

    // ──────────────────────────────────────────
    // Private Methods
    // ──────────────────────────────────────────

    private void ApplyAnimation()
    {
        CharacterData ch = PlayerInventory.Instance?.SelectedCharacter;
        AnimatorOverrideController controller = ch?.overrideController ?? _defaultController;

        Debug.Log($"[PlayerAnimator] ApplyAnimation — 캐릭터: {ch?.name ?? "null"} / 컨트롤러: {controller?.name ?? "null"}");

        if (controller == null)
        {
            Debug.LogWarning("[PlayerAnimator] AnimatorOverrideController가 없습니다. CharacterData.overrideController 또는 _defaultController를 Inspector에서 확인하세요.");
            return;
        }

        _animator.runtimeAnimatorController = controller;
        _animator.Play("Idle");  // 컨트롤러 교체 후 Idle 명시 재생
    }
}

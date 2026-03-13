using Inventory;
using UnityEngine;

/// <summary>
/// 플레이어 애니메이션을 관리합니다.
/// 장착된 무기의 AnimatorOverrideController를 런타임에 적용합니다. (Enemy 패턴과 동일)
/// Player 오브젝트에 부착하세요.
/// </summary>
[RequireComponent(typeof(Animator))]
public class PlayerAnimator : MonoBehaviour
{
    // ──────────────────────────────────────────
    // [SerializeField] Private Fields
    // ──────────────────────────────────────────
    [SerializeField, Tooltip("무기 미장착 시 사용할 기본 Override Controller")]
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
    }

    private void Start()
    {
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.OnChanged += ApplyWeaponAnimation;
        }

        ApplyWeaponAnimation();
    }

    private void OnDestroy()
    {
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.OnChanged -= ApplyWeaponAnimation;
        }
    }

    // ──────────────────────────────────────────
    // Public Methods — 애니메이션 재생
    // ──────────────────────────────────────────
    public void PlayIdle()   => _animator.Play("Idle");
    public void PlayRun()    => _animator.Play("Run");
    public void PlayAttack() => _animator.Play("Attack");
    public void PlayDie()    => _animator.Play("Die");

    // ──────────────────────────────────────────
    // Private Methods
    // ──────────────────────────────────────────

    private void ApplyWeaponAnimation()
    {
        if (PlayerInventory.Instance == null)
        {
            return;
        }

        ItemData weapon = PlayerInventory.Instance.GetEquipped(ItemType.Weapon);

        // 장착된 무기의 override controller, 없으면 기본 controller 적용
        AnimatorOverrideController target = weapon?.overrideController ?? _defaultController;
        _animator.runtimeAnimatorController = target;
    }
}

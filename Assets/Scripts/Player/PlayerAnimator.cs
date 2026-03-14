using System.Collections.Generic;
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
        else
        {
            Debug.LogWarning("[PA] PlayerInventory.Instance가 null — 이벤트 구독 불가!");
        }

        ApplyAnimation();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged += OnGameStateChanged;
        }
        else
        {
            Debug.LogWarning("[PA] GameManager null — OnStateChanged 구독 실패!");
        }
    }

    private void OnDestroy()
    {
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.OnChanged          -= ApplyAnimation;
            PlayerInventory.Instance.OnCharacterChanged -= ApplyAnimation;
        }

        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged -= OnGameStateChanged;
    }

    // ──────────────────────────────────────────
    // Public Methods — 애니메이션 재생
    // ──────────────────────────────────────────
    public void PlayIdle()  { ResetSpeed(); _animator.Play("Idle"); }
    public void PlayRun()   { ResetSpeed(); _animator.Play("Run"); }
    public void PlayDash()  { ResetSpeed(); _animator.Play("Dash"); }
    public void PlayDie()   { ResetSpeed(); _animator.Play("Die"); }
    public void PlaySkill() { ResetSpeed(); _animator.Play("Skill"); }  // 추후 에셋 준비 후 사용

    /// <summary>speed 배율로 Attack 애니메이션을 처음부터 재생합니다.</summary>
    public void PlayAttack(float speed = 1f)
    {
        _animator.speed = speed;
        // normalizedTime=0 명시: 동일 상태에서 재호출 시에도 반드시 처음부터 재생
        _animator.Play("Attack", 0, 0f);
    }

    /// <summary>AnimatorOverrideController에서 상태명에 해당하는 클립 길이를 반환합니다.</summary>
    /// <remarks>
    /// AOC의 키 클립명은 "Attack_Bow" 형태이므로, '_' 기준으로 앞부분만 잘라 stateName과 비교합니다.
    /// </remarks>
    public float GetClipLength(string stateName)
    {
        if (_animator.runtimeAnimatorController is AnimatorOverrideController aoc)
        {
            var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            aoc.GetOverrides(overrides);

            foreach (KeyValuePair<AnimationClip, AnimationClip> pair in overrides)
            {
                if (pair.Key == null || pair.Value == null) continue;

                // 키 클립명 앞부분으로 상태 매칭: "Attack_Bow" → "Attack"
                string baseName = pair.Key.name.Split('_')[0];
                if (string.Equals(baseName, stateName, System.StringComparison.OrdinalIgnoreCase))
                {
                    return pair.Value.length;
                }
            }
            Debug.LogWarning($"[PA] GetClipLength({stateName}): 매핑 없음 → 0.5f 폴백!");
        }
        else
        {
            Debug.LogWarning("[PA] GetClipLength: runtimeAnimatorController가 AOC 아님!");
        }

        return 0.5f;
    }

    public float GetAttackClipLength() => GetClipLength("Attack");

    public void ResetSpeed() => _animator.speed = 1f;

    // ──────────────────────────────────────────
    // Private Methods
    // ──────────────────────────────────────────
    private void OnGameStateChanged(GameState state)
    {
        switch (state)
        {
            case GameState.Entering:
            case GameState.Cleared:
                PlayRun();   // 등장/퇴장 이동 중 Run
                break;
            case GameState.Combat:
            case GameState.Reward:
                PlayIdle();  // 전투 진입 / 보상 방 대기 시 Idle
                break;
            // Pinned / Transitioning / GameOver: 다른 시스템이 처리
        }
    }

    private void ApplyAnimation()
    {
        CharacterData ch = PlayerInventory.Instance?.SelectedCharacter;
        AnimatorOverrideController controller = ch?.overrideController ?? _defaultController;

        if (controller == null)
        {
            Debug.LogWarning("[PlayerAnimator] AnimatorOverrideController가 없습니다. CharacterData.overrideController 또는 _defaultController를 Inspector에서 확인하세요.");
            return;
        }

        _animator.runtimeAnimatorController = controller;
        PlayIdle();  // 컨트롤러 교체 후 Idle 명시 재생 (speed 초기화 포함)
    }
}

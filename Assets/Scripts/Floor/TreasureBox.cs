using System;
using Inventory;
using UnityEngine;

/// <summary>
/// 보상 방의 보물 상자. Open 애니메이션 재생 후
/// 애니메이션 이벤트 OnBoxOpened()에서 아이템을 인벤토리에 추가한다.
/// </summary>
[RequireComponent(typeof(Animator))]
public class TreasureBox : MonoBehaviour
{
    // ── Private Fields ────────────────────────────────────────────
    private Animator _animator;
    private ItemData _reward;
    private Action   _onCollected;

    // ── Public Methods ────────────────────────────────────────────
    /// <summary>
    /// 박스 오픈 시퀀스를 시작한다. FloorManager가 호출한다.
    /// </summary>
    /// <param name="reward">획득할 아이템 (null이면 빈 보상)</param>
    /// <param name="onCollected">아이템 획득 완료 시 호출할 콜백</param>
    public void Open(ItemData reward, Action onCollected)
    {
        _reward      = reward;
        _onCollected = onCollected;
        _animator    = GetComponent<Animator>();
        _animator.Play("Open");
    }

    // ── 애니메이션 이벤트 ─────────────────────────────────────────
    /// <summary>
    /// Open 클립의 아이템 획득 타이밍 프레임에 등록할 애니메이션 이벤트.
    /// </summary>
    public void OnBoxOpened()
    {
        if (_reward != null)
        {
            Debug.Log($"[보상] 아이템 획득: {_reward.displayName}");
            PlayerInventory.Instance.AddItem(_reward);
            PlayerInventory.Instance.Save();
        }
        else
        {
            Debug.LogWarning("[보상] reward가 null — 아이템 없이 완료 처리");
        }

        _onCollected?.Invoke();
    }
}

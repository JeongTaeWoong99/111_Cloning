using System;
using System.Collections;
using Inventory;
using UnityEngine;

/// <summary>
/// 보상 방의 보물 상자. Open 애니메이션 재생 후
/// 애니메이션 이벤트 OnBoxOpened()에서 아이템을 인벤토리에 추가하고 팝업 연출을 재생한다.
/// </summary>
[RequireComponent(typeof(Animator))]
public class TreasureBox : MonoBehaviour
{
    // ── Serialized Fields ─────────────────────────────────────────
    [SerializeField, Tooltip("아이템 팝업 프리팹 (SpriteRenderer 루트 + Bright 파티클 자식)")]
    private GameObject _itemPopupPrefab;

    [SerializeField, Tooltip("팝업 스케일 애니메이션 시간 (초)")]
    private float _popupDuration = 0.3f;

    [SerializeField, Tooltip("팝업 자동 소멸 시간 (초)")]
    private float _popupLifetime = 2.5f;

    [SerializeField, Tooltip("팝업 스폰 Y 오프셋 (박스 기준)")]
    private float _popupYOffset = 3f;

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
            PlayerInventory.Instance.AddItem(_reward);
            PlayerInventory.Instance.Save();

            // 아이템 팝업 연출
            if (_itemPopupPrefab != null)
                StartCoroutine(SpawnItemPopup());
        }
        else
        {
            Debug.LogWarning("[보상] reward가 null — 아이템 없이 완료 처리");
        }

        _onCollected?.Invoke();
    }

    // ── Private Methods ───────────────────────────────────────────
    /// <summary>
    /// 아이템 아이콘 팝업을 상자 위에 스폰하고 EaseOutBack으로 스케일 애니메이션을 재생한다.
    /// </summary>
    private IEnumerator SpawnItemPopup()
    {
        // 박스 위치에서 시작 → 목표 위치로 이동하면서 크기도 키움
        Vector3    startPos  = transform.position;
        Vector3    targetPos = transform.position + Vector3.up * _popupYOffset;
        GameObject popup     = Instantiate(_itemPopupPrefab, startPos, Quaternion.identity);
        popup.transform.localScale = Vector3.zero;

        // 루트 SpriteRenderer에 아이템 아이콘 적용
        if (popup.TryGetComponent(out SpriteRenderer sr))
            sr.sprite = _reward.icon;

        // 위치(Y 상승) + 스케일(0→1) 동시 애니메이션
        float elapsed = 0f;
        
        while (elapsed < _popupDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / _popupDuration);

            popup.transform.position   = Vector3.Lerp(startPos, targetPos, t);
            popup.transform.localScale = Vector3.one * EaseOutBack(t);
            yield return null;
        }
        
        popup.transform.position   = targetPos;
        popup.transform.localScale = Vector3.one;

        Destroy(popup, _popupLifetime);
    }

    /// <summary>
    /// 오버슈트 후 1.0으로 수렴하는 이징 함수 (EaseOutBack).
    /// </summary>
    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }
}

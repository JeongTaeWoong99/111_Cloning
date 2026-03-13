using UnityEngine;

/// <summary>
/// X 위치에 따라 SpriteRenderer.sortingOrder를 동적으로 설정합니다.
/// X가 작을수록 (더 왼쪽) sortingOrder가 높아져 앞에 표시됩니다.
/// 몬스터 루트 오브젝트에 부착하세요.
/// SpriteRenderer가 자식에 있다면 GetComponentInChildren으로 교체하세요.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class EnemySortingOrder : MonoBehaviour
{
    // ──────────────────────────────────────────
    // [SerializeField] Private Fields
    // ──────────────────────────────────────────
    [SerializeField, Tooltip("X 위치당 sortingOrder 변화 배율")]
    private float _multiplier = 10f;

    // ──────────────────────────────────────────
    // Private Fields
    // ──────────────────────────────────────────
    private SpriteRenderer _renderer;

    // ──────────────────────────────────────────
    // MonoBehaviour
    // ──────────────────────────────────────────
    private void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
    }

    private void LateUpdate()
    {
        // X가 작을수록(왼쪽) sortingOrder 높음(앞)
        _renderer.sortingOrder = Mathf.RoundToInt(-transform.position.x * _multiplier);
    }
}

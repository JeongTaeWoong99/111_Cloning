using System.Collections;
using UnityEngine;

/// <summary>
/// 오른쪽으로 일직선 비행. 적 충돌 또는 수명 만료 시 풀에 반환.
/// Initialize() 호출로 위치·데미지·수명을 설정한다.
/// </summary>
public class Arrow : MonoBehaviour
{
    // ── Serialized Fields ─────────────────────────────────────────
    [SerializeField, Range(5f, 30f)] private float _speed    = 15f;
    [SerializeField, Range(1f, 10f)] private float _lifetime = 3f;

    // ── Fields ────────────────────────────────────────────────────
    private float     _damage;
    private LayerMask _enemyLayer;
    private Coroutine _lifetimeRoutine;

    // ── Public Methods ────────────────────────────────────────────
    /// <summary>풀에서 꺼낸 직후 호출하여 위치·데미지를 초기화하고 수명 타이머를 시작한다.</summary>
    public void Initialize(Vector2 position, float damage, LayerMask enemyLayer)
    {
        transform.position = position;
        _damage     = damage;
        _enemyLayer = enemyLayer;

        if (_lifetimeRoutine != null) StopCoroutine(_lifetimeRoutine);
        _lifetimeRoutine = StartCoroutine(ReturnAfterLifetime());
    }

    // ── MonoBehaviour ─────────────────────────────────────────────
    private void Update()
        => transform.Translate(Vector2.right * _speed * Time.deltaTime);

    private void OnTriggerEnter2D(Collider2D other)
    {
        if ((_enemyLayer.value & (1 << other.gameObject.layer)) == 0) return;
        if (other.TryGetComponent(out Enemy enemy)) enemy.TakeDamage(_damage);
        ReturnToPool();
    }

    // ── Private Methods ───────────────────────────────────────────
    private IEnumerator ReturnAfterLifetime()
    {
        yield return new WaitForSeconds(_lifetime);
        ReturnToPool();
    }

    private void ReturnToPool()
    {
        if (_lifetimeRoutine != null)
        {
            StopCoroutine(_lifetimeRoutine);
            _lifetimeRoutine = null;
        }

        ObjectPoolManager.Instance.Release("Arrow", gameObject);
    }
}

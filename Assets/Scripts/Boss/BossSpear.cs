using System.Collections;
using UnityEngine;

/// <summary>
/// 보스 창던지기 스킬 투사체 — 포물선 궤도로 날아가 플레이어에게 데미지를 준다.
/// </summary>
public class BossSpear : MonoBehaviour
{
    // ── Fields ────────────────────────────────────────────────────
    private float     _damage;
    private LayerMask _playerLayer;

    // ── Public Methods ────────────────────────────────────────────
    /// <summary>
    /// Boss.SpawnSpear에서 호출된다. 초기 속도·데미지·레이어를 설정하고 자동 소멸을 시작한다.
    /// </summary>
    public void Initialize(Vector2 velocity, float damage, LayerMask playerLayer)
    {
        _damage      = damage;
        _playerLayer = playerLayer;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        rb.linearVelocity = velocity;

        // 진행 방향으로 스프라이트 회전
        float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        StartCoroutine(AutoDestroy(3f));
    }

    // ── MonoBehaviour ─────────────────────────────────────────────
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & _playerLayer) == 0) return;

        PlayerHealth.Instance.TakeDamage((int)_damage);
        Destroy(gameObject);
    }

    // ── Private Methods ───────────────────────────────────────────
    private IEnumerator AutoDestroy(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }
}

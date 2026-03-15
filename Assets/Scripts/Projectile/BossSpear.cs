using System.Collections;
using UnityEngine;

/// <summary>
/// 보스 창던지기 스킬 투사체 — Dynamic Rigidbody2D + Unity 내장 중력으로 포물선 이동한다.
/// 땅(Default 레이어)에 닿으면 박힘 연출로 전환되고, 패링으로 제거 가능하다.
/// </summary>
public class BossSpear : MonoBehaviour
{
    // ── Serialized Fields ─────────────────────────────────────────
    [SerializeField, Tooltip("생성 직후 착지 판정 무시 시간 (초) — 보스 발밑 즉시 충돌 방지")]
    private float _landingDelay = 0.15f;

    // ── Fields ────────────────────────────────────────────────────
    private float          _damage;
    private LayerMask      _playerLayer;
    private bool           _hasLanded;
    private bool           _canLand;     // landingDelay 이후 착지 판정 활성화

    private Rigidbody2D    _rb;
    private SpriteRenderer _sr;
    private Collider2D     _col;

    // ── MonoBehaviour ─────────────────────────────────────────────
    private void Awake()
    {
        _rb  = GetComponent<Rigidbody2D>();
        _sr  = GetComponentInChildren<SpriteRenderer>();
        _col = GetComponent<Collider2D>();
    }

    private void Update()
    {
        if (_hasLanded) return;

        Vector2 vel = _rb.linearVelocity;
        if (vel.sqrMagnitude < 0.01f) return;

        bool  goLeft = vel.x < 0f;

        // 스프라이트가 오른쪽을 바라보므로 왼쪽 비행 시 X 플립 후 좌표계 반전
        _sr.flipX = goLeft;
        float angle = goLeft
            ? Mathf.Atan2(-vel.y, -vel.x) * Mathf.Rad2Deg
            : Mathf.Atan2( vel.y,  vel.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_hasLanded) return;

        // 다른 창과의 충돌 무시
        if (other.TryGetComponent<BossSpear>(out _)) return;

        // 땅(Default 레이어) 충돌 → 착지 딜레이 후에만 박힘 처리
        if (other.gameObject.layer == LayerMask.NameToLayer("Default"))
        {
            if (!_canLand) return;
            StickToGround();
            return;
        }

        // 플레이어 피격
        if (((1 << other.gameObject.layer) & _playerLayer) == 0) return;
        PlayerHealth.Instance.TakeDamage((int)_damage);
        Destroy(gameObject);
    }

    // ── Public Methods ────────────────────────────────────────────
    /// <summary>
    /// Boss.SpawnSpear에서 호출된다. 초기 속도·데미지·레이어를 설정하고 자동 소멸을 시작한다.
    /// </summary>
    public void Initialize(Vector2 velocity, float damage, LayerMask playerLayer)
    {
        _damage      = damage;
        _playerLayer = playerLayer;

        _rb.linearVelocity = velocity;

        StartCoroutine(EnableLanding());
        StartCoroutine(AutoDestroy(5f));
    }

    /// <summary>
    /// 패링 성공 시 PlayerCombat에서 호출된다. 이미 땅에 박힌 창은 무시한다.
    /// </summary>
    public void DestroyByParry()
    {
        if (_hasLanded) return;
        Destroy(gameObject);
    }

    // ── Private Methods ───────────────────────────────────────────
    private IEnumerator EnableLanding()
    {
        yield return new WaitForSeconds(_landingDelay);
        // landingDelay 경과 후 착지 판정 활성화 — 보스 발밑 즉시 충돌 방지
        _canLand = true;
    }

    /// <summary>
    /// 땅에 박힌 것처럼 연출한다 — 이동 정지, 충돌 비활성화, 정렬 순서를 배경으로 전환한다.
    /// </summary>
    private void StickToGround()
    {
        _hasLanded = true;
        StopAllCoroutines();

        _rb.linearVelocity  = Vector2.zero;
        _rb.angularVelocity = 0f;
        _rb.isKinematic     = true;  // 박힌 뒤 물리 완전 정지

        _col.enabled = false;  // 박힌 뒤 플레이어 피격 차단

        _sr.sortingLayerName = "Default";
        _sr.sortingOrder     = 100;

        StartCoroutine(AutoDestroy(5f));
    }

    private IEnumerator AutoDestroy(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }
}

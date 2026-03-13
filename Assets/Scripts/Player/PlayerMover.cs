using System.Collections;
using UnityEngine;

/// <summary>
/// 플레이어 위치 이동을 담당하는 싱글턴.
/// FloorManager가 코루틴으로 제어한다.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMover : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────
    public static PlayerMover Instance { get; private set; }

    // ── Serialized Fields ─────────────────────────────────────────
    [SerializeField, Tooltip("이동 속도 (m/s)")]
    [Range(1f, 20f)]
    private float _moveSpeed = 5f;

    [SerializeField, Tooltip("대쉬 이동 속도 (m/s)")]
    [Range(5f, 50f)]
    private float _dashSpeed = 20f;

    // ── Properties ───────────────────────────────────────────────
    public bool IsMoving { get; private set; }

    // ── Fields ────────────────────────────────────────────────────
    private Rigidbody2D _rigidbody;

    // ── MonoBehaviour ─────────────────────────────────────────────
    private void Awake()
    {
        Instance   = this;
        _rigidbody = GetComponent<Rigidbody2D>();
    }

    // ── Public Methods ────────────────────────────────────────────
    /// <summary>
    /// 목표 위치까지 물리 기반으로 이동한다.
    /// </summary>
    public IEnumerator MoveTo(Vector2 target)
    {
        IsMoving = true;

        while (Vector2.Distance(_rigidbody.position, target) > 0.5f)
        {
            Vector2 next = Vector2.MoveTowards(_rigidbody.position, target, _moveSpeed * Time.fixedDeltaTime);
            _rigidbody.MovePosition(next);

            yield return new WaitForFixedUpdate();
        }

        IsMoving = false;
    }

    /// <summary>
    /// 목표 위치까지 대쉬 속도로 물리 기반으로 이동한다.
    /// </summary>
    public IEnumerator DashTo(Vector2 target)
    {
        IsMoving = true;

        while (Vector2.Distance(_rigidbody.position, target) > 0.5f)
        {
            Vector2 next = Vector2.MoveTowards(_rigidbody.position, target, _dashSpeed * Time.fixedDeltaTime);
            _rigidbody.MovePosition(next);

            yield return new WaitForFixedUpdate();
        }

        IsMoving = false;
    }

    /// <summary>
    /// AddForce 기반으로 플레이어를 발사하고, duration 동안 대쉬를 차단한다.
    /// </summary>
    public void Launch(Vector2 force, float duration)
    {
        StartCoroutine(LaunchAsync(force, duration));
    }

    private IEnumerator LaunchAsync(Vector2 force, float duration)
    {
        IsMoving = true;
        _rigidbody.AddForce(force, ForceMode2D.Impulse);
        yield return new WaitForSeconds(duration);
        IsMoving = false;
    }

    /// <summary>
    /// 실행 중인 이동 코루틴을 즉시 중단하고 velocity를 0으로 만든다.
    /// </summary>
    public void StopImmediate()
    {
        StopAllCoroutines();
        IsMoving                  = false;
        _rigidbody.linearVelocity = Vector2.zero;
    }

    /// <summary>
    /// 지정 위치로 즉시 순간이동한다.
    /// </summary>
    public void TeleportTo(Vector2 position)
    {
        _rigidbody.position = position;
    }
}

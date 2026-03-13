using UnityEngine;

/// <summary>
/// 왼쪽 경계 감지 및 반격(S키) 처리 싱글턴.
/// </summary>
public class PlayerBoundaryHandler : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────
    public static PlayerBoundaryHandler Instance { get; private set; }

    // ── Serialized Fields ─────────────────────────────────────────
    [Header("경계")]
    [SerializeField, Tooltip("카메라 왼쪽 끝 기준 x 오프셋 (m)")]
    [Range(-5f, 5f)]
    private float _boundaryXOffset = 0f;

    [Header("반격")]
    [SerializeField, Tooltip("반격 시 적 넉백 충격량")]
    [Range(1f, 20f)]
    private float _knockbackForce = 8f;

    [SerializeField, Tooltip("넉백 지속 시간 (초)")]
    [Range(0.1f, 2f)]
    private float _knockbackDuration = 0.6f;

    [SerializeField, Tooltip("반격 시 플레이어 점프 충격량")]
    [Range(1f, 15f)]
    private float _playerLaunchForce = 5f;

    [SerializeField, Tooltip("플레이어 발사 지속 시간 (초)")]
    [Range(0.1f, 2f)]
    private float _playerLaunchDuration = 0.4f;

    // ── Fields ────────────────────────────────────────────────────
    private bool _isPinned;

    // ── MonoBehaviour ─────────────────────────────────────────────
    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (GameManager.Instance.CurrentState != GameState.Combat)
        {
            return;
        }

        // 반격(LaunchAsync) 중 경계 재진입 방지
        if (PlayerMover.Instance.IsMoving)
        {
            return;
        }

        CheckBoundary();
    }

    private void OnDrawGizmos()
    {
        if (Camera.main == null)
        {
            return;
        }

        float x = Camera.main.ViewportToWorldPoint(Vector3.zero).x + _boundaryXOffset;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(new Vector3(x, -50f, 0f), new Vector3(x, 50f, 0f));
    }

    // ── Public Methods ────────────────────────────────────────────
    /// <summary>
    /// S키 반격 — 플레이어는 앞으로 점프, 적은 45도 방향으로 넉백.
    /// </summary>
    public void Counterattack()
    {
        _isPinned = false;
        GameManager.Instance.SetState(GameState.Combat);

        // 45도 방향 = (right + up).normalized
        Vector2 knockbackDir = (Vector2.right + Vector2.up).normalized;

        PlayerMover.Instance.Launch(
            knockbackDir * _playerLaunchForce, _playerLaunchDuration);

        EnemySpawnManager.Instance.KnockbackEnemies(
            knockbackDir * _knockbackForce, _knockbackDuration);
    }

    // ── Private Methods ───────────────────────────────────────────
    private void CheckBoundary()
    {
        float leftBound = Camera.main.ViewportToWorldPoint(Vector3.zero).x + _boundaryXOffset;

        if (transform.position.x <= leftBound)
        {
            EnterPinned();
        }
    }

    private void EnterPinned()
    {
        if (_isPinned)
        {
            return;
        }

        _isPinned = true;
        GameManager.Instance.SetState(GameState.Pinned);
        PlayerHealth.Instance.TakeDamage(1);
        PlayerMover.Instance.StopImmediate();
        EnemySpawnManager.Instance.StopMoving();
    }
}

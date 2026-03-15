using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// 적 스폰·사전 배치·일괄 이동·전멸 감지를 담당하는 싱글턴.
/// </summary>
public class EnemySpawnManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────
    public static EnemySpawnManager Instance { get; private set; }

    // ── Serialized Fields ─────────────────────────────────────────
    [Header("프리팹")]
    [SerializeField] private Enemy _enemyPrefab;

    [Header("스폰 설정")]
    [SerializeField] private List<FloorSpawnConfig> _floorConfigs;

    [SerializeField, Tooltip("적 사이 간격 (m)")]
    [Range(0.5f, 5f)]
    private float _spacingX = 1f;

    // ── Properties ───────────────────────────────────────────────
    public IReadOnlyList<Enemy> LivingEnemies => _livingEnemies;
    public int                  FloorCount    => _floorConfigs.Count;

    // ── Events ────────────────────────────────────────────────────
    public event Action OnAllDefeated;

    // ── Fields ────────────────────────────────────────────────────
    private readonly Dictionary<EnemyData, ObjectPool<Enemy>> _pools               = new();
    private readonly Dictionary<int, List<Enemy>>             _pendingEnemies      = new();
    private readonly HashSet<Enemy>                           _currentFloorEnemies = new(); // 현재 전투 층 적만 추적 (넉백 범위 제한용)
    private          List<Enemy>                              _livingEnemies       = new();
    private          bool                                     _isMoving;

    // ── MonoBehaviour ─────────────────────────────────────────────
    private void Awake()
    {
        Instance = this;
    }

    private void FixedUpdate()
    {
        if (!_isMoving)
        {
            return;
        }

        foreach (Enemy enemy in _livingEnemies)
        {
            // 넉백·사망 중인 적은 자동이동 스킵
            if (enemy.IsKnockback || enemy.IsDying)
            {
                continue;
            }

            // x만 설정해 중력(y)은 물리 엔진에 위임
            enemy.Rigidbody.linearVelocity = new Vector2(-enemy.MoveSpeed,
                                                          enemy.Rigidbody.linearVelocity.y);
        }
    }

    // ── Public Methods ────────────────────────────────────────────
    /// <summary>
    /// 지정 층의 적을 미리 풀에서 생성하여 대기 상태로 배치한다.
    /// </summary>
    public void PreloadFloor(int floor, Vector2 origin)
    {
        List<EnemyData> shuffled       = BuildShuffledList(floor);
        List<Enemy>     pendingEnemies = new();

        for (int i = 0; i < shuffled.Count; i++)
        {
            EnemyData data     = shuffled[i];
            Vector2   spawnPos = origin + Vector2.right * (_spacingX * i);
            Enemy     enemy    = GetOrCreatePool(data).Get();

            enemy.transform.position = spawnPos;
            enemy.Initialize(data);
            pendingEnemies.Add(enemy);
        }

        _pendingEnemies[floor] = pendingEnemies;
    }

    /// <summary>
    /// 대기 중인 적을 활성화하여 전투 목록에 편입한다.
    /// </summary>
    public void ActivateFloor(int floor)
    {
        if (!_pendingEnemies.TryGetValue(floor, out List<Enemy> enemies))
        {
            return;
        }

        _currentFloorEnemies.Clear();

        foreach (Enemy enemy in enemies)
        {
            enemy.OnDied += OnEnemyDied;
            _livingEnemies.Add(enemy);
            _currentFloorEnemies.Add(enemy);

            if (_isMoving)
            {
                enemy.PlayRun();
            }
        }

        _pendingEnemies.Remove(floor);
    }

    /// <summary>
    /// 대기 중인 층의 적을 풀에 반환하고 제거한다. 키가 없으면 무시한다.
    /// </summary>
    public void UnloadFloor(int floor)
    {
        if (!_pendingEnemies.TryGetValue(floor, out List<Enemy> enemies))
        {
            return;
        }

        foreach (Enemy enemy in enemies)
        {
            if (enemy != null)
            {
                _pools[enemy.Data].Release(enemy);
            }
        }

        _pendingEnemies.Remove(floor);
    }

    /// <summary>
    /// 해당 층이 사전 배치 대기 중인지 반환한다.
    /// </summary>
    public bool HasPendingFloor(int floor) => _pendingEnemies.ContainsKey(floor);

    /// <summary>
    /// 해당 층이 보상 방인지 반환한다.
    /// </summary>
    public bool IsRewardFloor(int floor) => GetConfigForFloor(floor).isRewardRoom;

    /// <summary>
    /// 해당 층이 보스 방인지 반환한다.
    /// </summary>
    public bool IsBossFloor(int floor) => GetConfigForFloor(floor).isBossRoom;

    /// <summary>
    /// 해당 층의 보스 데이터를 반환한다.
    /// </summary>
    public BossData GetBossData(int floor) => GetConfigForFloor(floor).bossData;

    /// <summary>
    /// 해당 층의 possibleRewards에서 랜덤으로 1개 반환한다.
    /// </summary>
    public Inventory.ItemData GetRewardItem(int floor)
    {
        List<Inventory.ItemData> pool = GetConfigForFloor(floor).possibleRewards;

        if (pool == null || pool.Count == 0)
        {
            return null;
        }
        
        return pool[UnityEngine.Random.Range(0, pool.Count)];
    }

    /// <summary>
    /// 적 일괄 이동을 시작하고 Run 애니메이션을 재생한다.
    /// </summary>
    public void StartMoving()
    {
        _isMoving = true;

        foreach (Enemy enemy in _livingEnemies)
        {
            if (enemy.IsDying) continue;
            enemy.PlayRun();
        }
    }

    /// <summary>
    /// 적 일괄 이동을 멈추고 모든 적의 velocity를 즉시 0으로 만들고 Idle 애니메이션을 재생한다.
    /// </summary>
    public void StopMoving()
    {
        _isMoving = false;

        foreach (Enemy enemy in _livingEnemies)
        {
            if (enemy.IsDying) continue;
            enemy.Rigidbody.linearVelocity = Vector2.zero;
            enemy.PlayIdle();
        }
    }

    /// <summary>
    /// 살아있는 적과 대기 중인 적을 모두 풀에 반환한다.
    /// </summary>
    public void ClearAll()
    {
        foreach (Enemy enemy in _livingEnemies)
        {
            if (enemy != null)
            {
                enemy.OnDied -= OnEnemyDied;
                _pools[enemy.Data].Release(enemy);
            }
        }

        _livingEnemies.Clear();
        _currentFloorEnemies.Clear();

        foreach (List<Enemy> pending in _pendingEnemies.Values)
        {
            foreach (Enemy enemy in pending)
            {
                if (enemy != null)
                {
                    _pools[enemy.Data].Release(enemy);
                }
            }
        }

        _pendingEnemies.Clear();
        _isMoving = false;
    }

    /// <summary>
    /// 현재 층 적 중 origin에서 range 이내에 있는 적에게 넉백을 적용하고, duration 후 이동을 재개한다.
    /// </summary>
    public void KnockbackEnemies(Vector2 force, float duration, Vector2 origin, float range)
    {
        StartCoroutine(KnockbackEnemiesAsync(force, duration, origin, range));
    }

    private IEnumerator KnockbackEnemiesAsync(Vector2 force, float duration, Vector2 origin, float range)
    {
        // 현재 층 적만 대상 — 타 층 적 제외
        // HashSet은 순회 중 변경에 취약하므로 스냅샷 배열 복사
        Enemy[] targets = new Enemy[_currentFloorEnemies.Count];
        _currentFloorEnemies.CopyTo(targets);

        foreach (Enemy enemy in targets)
        {
            // 패링 범위 내 적만 넉백
            if (Mathf.Abs(enemy.transform.position.x - origin.x) <= range)
            {
                enemy.ApplyKnockback(force, duration);
            }
        }

        yield return new WaitForSeconds(duration);

        // Pinned·GameOver 등 상태에서는 이동 재개 안 함
        if (GameManager.Instance.CurrentState == GameState.Combat)
        {
            StartMoving();
        }
    }

    // ── Private Methods ───────────────────────────────────────────
    // EnemyData에 대응하는 풀을 반환. 없으면 생성.
    private ObjectPool<Enemy> GetOrCreatePool(EnemyData data)
    {
        if (!_pools.ContainsKey(data))
        {
            _pools[data] = new ObjectPool<Enemy>(
                createFunc:      ()    => Instantiate(_enemyPrefab),
                actionOnGet:     enemy => enemy.gameObject.SetActive(true),
                actionOnRelease: enemy => enemy.gameObject.SetActive(false),
                actionOnDestroy: enemy => Destroy(enemy.gameObject)
            );
        }

        return _pools[data];
    }

    // floor 번호에 대응하는 config를 반환. 초과 시 마지막 config 사용.
    private FloorSpawnConfig GetConfigForFloor(int floor)
    {
        int index = Mathf.Clamp(floor - 1, 0, _floorConfigs.Count - 1);
        return _floorConfigs[index];
    }

    // percent 기반으로 배분 후 Fisher-Yates 셔플한 리스트 반환
    private List<EnemyData> BuildShuffledList(int floor)
    {
        FloorSpawnConfig config    = GetConfigForFloor(floor);
        List<EnemyData>  list      = new();
        int              remaining = config.totalEnemyCount;

        for (int i = 0; i < config.entries.Count; i++)
        {
            EnemyWeightEntry entry = config.entries[i];

            // 마지막 항목은 나머지 마릿수를 흡수
            int count = (i == config.entries.Count - 1)
                ? remaining
                : Mathf.RoundToInt(config.totalEnemyCount * entry.percent / 100f);

            count = Mathf.Min(count, remaining);

            for (int j = 0; j < count; j++)
            {
                list.Add(entry.data);
            }

            remaining -= count;

            if (remaining <= 0)
            {
                break;
            }
        }

        // Fisher-Yates shuffle
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }

        return list;
    }

    private void OnEnemyDied(Enemy enemy)
    {
        enemy.OnDied -= OnEnemyDied;
        _livingEnemies.Remove(enemy);
        _currentFloorEnemies.Remove(enemy);
        _pools[enemy.Data].Release(enemy);

        if (_livingEnemies.Count == 0)
        {
            OnAllDefeated?.Invoke();
        }
    }
}

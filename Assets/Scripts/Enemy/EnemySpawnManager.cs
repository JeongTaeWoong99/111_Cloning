using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

// ─────────────────────────────────────────────────────────────────────────────
/// <summary>
/// 스폰 목록 항목 — 어떤 적을 몇 마리 스폰할지 지정한다.
/// </summary>
[Serializable]
public class EnemySpawnEntry
{
    public EnemyData data;
    public int       count;
}

/// <summary>
/// 적 스폰 + 일괄 이동 + 전멸 감지를 담당하는 싱글턴.
/// </summary>
public class EnemySpawnManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────
    public static EnemySpawnManager Instance { get; private set; }

    // ── Serialized Fields ─────────────────────────────────────────
    [Header("스폰 설정")]
    [SerializeField] private List<EnemySpawnEntry> _spawnEntries;

    [SerializeField, Tooltip("적 사이 간격 (m)")]
    [Range(0.5f, 5f)]
    private float _spacingX = 1f;

    // ── Events ────────────────────────────────────────────────────
    public event Action OnAllDefeated;

    // ── Fields ────────────────────────────────────────────────────
    private readonly Dictionary<EnemyData, ObjectPool<Enemy>> _pools = new();
    private List<Enemy> _livingEnemies = new();
    private bool        _isMoving;

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

        float deltaTime = Time.fixedDeltaTime;

        foreach (Enemy enemy in _livingEnemies)
        {
            Vector2 next = enemy.Rigidbody.position + Vector2.left * (enemy.MoveSpeed * deltaTime);
            enemy.Rigidbody.MovePosition(next);
        }
    }

    // ── Public Methods ────────────────────────────────────────────
    /// <summary>
    /// positionSet.enemySpawnPos 기준으로 오른쪽에 간격을 두고 적을 스폰한다.
    /// </summary>
    public void SpawnEnemies(FloorPositionSet positionSet)
    {
        List<EnemyData> shuffled = BuildShuffledList();
        Vector2         origin   = positionSet.enemySpawnPos.position;

        for (int i = 0; i < shuffled.Count; i++)
        {
            EnemyData data     = shuffled[i];
            Vector2   spawnPos = origin + Vector2.right * (_spacingX * i);
            Enemy     enemy    = GetOrCreatePool(data).Get();

            enemy.transform.position = spawnPos;
            enemy.Initialize(data);
            enemy.OnDied += OnEnemyDied;
            _livingEnemies.Add(enemy);
        }
    }

    /// <summary>
    /// 적 일괄 이동을 시작한다.
    /// </summary>
    public void StartMoving()
    {
        _isMoving = true;
    }

    /// <summary>
    /// 적 일괄 이동을 멈춘다.
    /// </summary>
    public void StopMoving()
    {
        _isMoving = false;
    }

    /// <summary>
    /// 층 전환 전 남은 적을 모두 풀에 반환한다.
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
        _isMoving = false;
    }

    // ── Private Methods ───────────────────────────────────────────
    // EnemyData에 대응하는 풀을 반환. 없으면 생성.
    private ObjectPool<Enemy> GetOrCreatePool(EnemyData data)
    {
        if (!_pools.ContainsKey(data))
        {
            _pools[data] = new ObjectPool<Enemy>(
                createFunc:      () => Instantiate(data.prefab).GetComponent<Enemy>(),
                actionOnGet:     enemy => enemy.gameObject.SetActive(true),
                actionOnRelease: enemy => enemy.gameObject.SetActive(false),
                actionOnDestroy: enemy => Destroy(enemy.gameObject)
            );
        }

        return _pools[data];
    }

    // spawnEntries의 각 종류별 count를 합쳐 Fisher-Yates 셔플한 리스트를 반환
    private List<EnemyData> BuildShuffledList()
    {
        List<EnemyData> list = new();

        foreach (EnemySpawnEntry entry in _spawnEntries)
        {
            for (int i = 0; i < entry.count; i++)
            {
                list.Add(entry.data);
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
        _pools[enemy.Data].Release(enemy);

        if (_livingEnemies.Count == 0)
        {
            OnAllDefeated?.Invoke();
        }
    }
}

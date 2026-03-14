using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// ID 기반 범용 오브젝트 풀 싱글턴.
/// Inspector의 PoolEntry 배열에 프리팹을 등록한 뒤
/// Get(id) / Release(id, obj) 로 꺼내고 반환한다.
/// </summary>
public class ObjectPoolManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────
    public static ObjectPoolManager Instance { get; private set; }

    // ── Serialized Fields ─────────────────────────────────────────
    [SerializeField] private PoolEntry[] _entries;

    // ── Fields ────────────────────────────────────────────────────
    private readonly Dictionary<string, ObjectPool<GameObject>> _pools = new();

    // ── MonoBehaviour ─────────────────────────────────────────────
    private void Awake()
    {
        Instance = this;

        foreach (PoolEntry entry in _entries)
            RegisterPool(entry);
    }

    // ── Public Methods ────────────────────────────────────────────
    /// <summary>풀에서 오브젝트를 꺼내 활성화한다.</summary>
    public GameObject Get(string id)
    {
        if (!_pools.TryGetValue(id, out ObjectPool<GameObject> pool))
        {
            Debug.LogError($"[ObjectPoolManager] 등록되지 않은 ID: {id}");
            return null;
        }

        return pool.Get();
    }

    /// <summary>오브젝트를 비활성화하고 풀에 반환한다.</summary>
    public void Release(string id, GameObject obj)
    {
        if (!_pools.TryGetValue(id, out ObjectPool<GameObject> pool))
        {
            Destroy(obj);
            return;
        }

        pool.Release(obj);
    }

    // ── Private Methods ───────────────────────────────────────────
    private void RegisterPool(PoolEntry entry)
    {
        GameObject prefab = entry.prefab;

        _pools[entry.id] = new ObjectPool<GameObject>(
            createFunc:      ()  => Instantiate(prefab),
            actionOnGet:     go  => go.SetActive(true),
            actionOnRelease: go  => go.SetActive(false),
            actionOnDestroy: go  => Destroy(go),
            defaultCapacity: entry.defaultCapacity,
            maxSize:         entry.maxSize
        );
    }
}

// ── PoolEntry ──────────────────────────────────────────────────────────────

/// <summary>
/// Inspector에서 풀을 등록하는 단위.
/// id는 Get/Release 호출 시 사용하는 문자열 키다.
/// </summary>
[Serializable]
public struct PoolEntry
{
    public string     id;
    public GameObject prefab;

    [Range(1, 50)]  public int defaultCapacity;
    [Range(1, 200)] public int maxSize;
}

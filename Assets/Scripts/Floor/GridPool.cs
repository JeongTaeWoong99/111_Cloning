using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Loop Grid n개 Queue 풀 + End Grid 단일 인스턴스 관리.
/// </summary>
public class GridPool : MonoBehaviour
{
    // ── Serialized Fields ─────────────────────────────────────────
    [SerializeField] private FloorGrid _loopGridPrefab;
    [SerializeField] private FloorGrid _endGridPrefab;
    [SerializeField] private int       _loopPoolSize = 3;

    // ── Fields ────────────────────────────────────────────────────
    private readonly Queue<FloorGrid> _available = new();
    private FloorGrid                 _endGridInstance;

    // ── Public Methods ────────────────────────────────────────────
    /// <summary>
    /// 풀을 초기화하고 인스턴스를 생성한다.
    /// </summary>
    public void Initialize()
    {
        for (int i = 0; i < _loopPoolSize; i++)
        {
            FloorGrid grid = Instantiate(_loopGridPrefab);
            grid.gameObject.SetActive(false);
            _available.Enqueue(grid);
        }

        _endGridInstance = Instantiate(_endGridPrefab);
        _endGridInstance.gameObject.SetActive(false);
    }

    /// <summary>
    /// Loop Grid를 꺼내 지정 위치에 배치한다.
    /// </summary>
    public FloorGrid GetLoop(Vector3 pos)
    {
        FloorGrid grid = _available.Dequeue();
        grid.transform.position = pos;
        grid.gameObject.SetActive(true);

        return grid;
    }

    /// <summary>
    /// End Grid(단일 인스턴스)를 꺼내 지정 위치에 배치한다.
    /// </summary>
    public FloorGrid GetEnd(Vector3 pos)
    {
        _endGridInstance.transform.position = pos;
        _endGridInstance.gameObject.SetActive(true);

        return _endGridInstance;
    }

    /// <summary>
    /// Loop Grid를 반환한다. Start/End 그리드는 반환하지 않는다.
    /// </summary>
    public void Return(FloorGrid grid)
    {
        grid.gameObject.SetActive(false);
        _available.Enqueue(grid);
    }
}

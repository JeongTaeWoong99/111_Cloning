using System.Collections.Generic;
using UnityEngine;

// Loop Grid 4개 Queue 풀 + End Grid 단일 인스턴스 관리
public class GridPool : MonoBehaviour
{
    [SerializeField] private FloorGrid _loopGridPrefab;
    [SerializeField] private FloorGrid _endGridPrefab;
    [SerializeField] private int       _loopPoolSize = 3;

    private readonly Queue<FloorGrid> _available = new();
    private FloorGrid                 _endGridInstance;

    public void Initialize()
    {
        // Loop Grid 풀 생성
        for (int i = 0; i < _loopPoolSize; i++)
        {
            FloorGrid grid = Instantiate(_loopGridPrefab);
            grid.gameObject.SetActive(false);
            _available.Enqueue(grid);
        }

        // End Grid 단일 인스턴스 생성
        _endGridInstance = Instantiate(_endGridPrefab);
        _endGridInstance.gameObject.SetActive(false);
    }

    // Loop Grid 꺼내기
    public FloorGrid GetLoop(Vector3 pos)
    {
        FloorGrid grid = _available.Dequeue();
        grid.transform.position = pos;
        grid.gameObject.SetActive(true);
        
        return grid;
    }

    // End Grid 꺼내기 (단일 인스턴스)
    public FloorGrid GetEnd(Vector3 pos)
    {
        _endGridInstance.transform.position = pos;
        _endGridInstance.gameObject.SetActive(true);
        
        return _endGridInstance;
    }

    // Loop Grid 반환 (Start/End는 반환하지 않음)
    public void Return(FloorGrid grid)
    {
        grid.gameObject.SetActive(false);
        _available.Enqueue(grid);
    }
}

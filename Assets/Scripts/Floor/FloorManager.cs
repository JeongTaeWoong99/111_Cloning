using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 층 관리 오케스트레이터 — 초기 배치 + F1키로 다음 층 전환
// 활성화 규칙: 현재층 ~ 현재층+2 (최대 3개) + 이전 Start Grid(1층만)
public class FloorManager : MonoBehaviour
{
    [SerializeField] private GridPool               _gridPool;
    [SerializeField] private CameraScrollController _cameraScroll;
    [SerializeField] private FloorGrid              _startGrid;   // 씬에 배치된 Start Grid 인스턴스
    [SerializeField] private float                  _gridHeight = 10f;
    [SerializeField] private int                    _totalFloors = 10;

    // 층 번호 → 그리드 인스턴스 매핑
    private readonly Dictionary<int, FloorGrid> _floorGridMap = new();

    private int  _currentFloor = 1;
    private bool _isTransitioning;

    // 시작 시 현재층 포함 위 2개 미리 배치 (start + loop × 2), 풀에 1개 대기
    private const int PreloadCount = 2;

    // 층 번호 → 월드 위치 (y축 기준)
    private Vector3 FloorToPos(int floor) => Vector3.up * ((floor - 1) * _gridHeight);

    private void Start()
    {
        _gridPool.Initialize();

        // 1층: Start Grid
        _floorGridMap[1] = _startGrid;
        _startGrid.gameObject.SetActive(true);

        // 2 ~ 3층: Loop Grid 미리 배치
        for (int floor = 2; floor <= 1 + PreloadCount && floor <= _totalFloors; floor++)
        {
            FloorGrid grid = (floor == _totalFloors) ? _gridPool.GetEnd(FloorToPos(floor))
                                                     : _gridPool.GetLoop(FloorToPos(floor));

            _floorGridMap[floor] = grid;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1) && !_isTransitioning && _currentFloor < _totalFloors)
            StartCoroutine(NextFloor());
    }

    private IEnumerator NextFloor()
    {
        _isTransitioning = true;
        _currentFloor++;

        // 카메라 스크롤
        yield return StartCoroutine(_cameraScroll.ScrollTo(FloorToPos(_currentFloor).y));

        // 현재층-2 비활성화
        int removeFloor = _currentFloor - 2;
        
        if (_floorGridMap.TryGetValue(removeFloor, out FloorGrid removeGrid))
        {
            // Start Grid(1층)는 풀에 반환하지 않고 단순 비활성화
            if (removeFloor == 1)
                removeGrid.gameObject.SetActive(false);
            else
                _gridPool.Return(removeGrid);

            _floorGridMap.Remove(removeFloor);
        }

        // 현재층+2에 새 그리드 배치
        int addFloor = _currentFloor + 2;
        if (addFloor <= _totalFloors && !_floorGridMap.ContainsKey(addFloor))
        {
            FloorGrid newGrid = (addFloor == _totalFloors) ? _gridPool.GetEnd(FloorToPos(addFloor))
                                                           : _gridPool.GetLoop(FloorToPos(addFloor));
            _floorGridMap[addFloor] = newGrid;
        }

        _isTransitioning = false;
    }

}

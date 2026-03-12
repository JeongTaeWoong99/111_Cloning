using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 층 관리 오케스트레이터 — 상태 전환 + 플레이어 시퀀스 + 전투 흐름
// 활성화 규칙: 현재층 ~ 현재층+2 (최대 3개) + 이전 Start Grid(1층만)
public class FloorManager : MonoBehaviour
{
    [Header("층 구성")]
    [SerializeField] private GridPool               _gridPool;
    [SerializeField] private CameraScrollController _cameraScroll;
    [SerializeField] private FloorGrid              _startGrid;
    [SerializeField] private float                  _gridHeight  = 10f;
    [SerializeField] private int                    _totalFloors = 10;

    [Header("플레이어 위치 세트")]
    [SerializeField] private FloorPositionSet _setA;
    [SerializeField] private FloorPositionSet _setB;
    [SerializeField] private PlayerMover      _playerMover;

    [Header("전투")]
    [SerializeField] private EnemySpawnManager _enemySpawnManager;

    // 층 번호 → 그리드 인스턴스 매핑
    private readonly Dictionary<int, FloorGrid> _floorGridMap = new();

    private int  _currentFloor = 1;
    private bool _useSetA      = true;

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

        StartCoroutine(RunFloor());
    }

    // 한 층의 전체 진행 흐름: 등장 → 전투 → 퇴장 → 층 전환 → 반복
    private IEnumerator RunFloor()
    {
        FloorPositionSet activeSet  = _useSetA ? _setA : _setB;
        FloorPositionSet standbySet = _useSetA ? _setB : _setA;

        // 1. 등장 (Entering)
        GameManager.Instance.SetState(GameState.Entering);
        _playerMover.TeleportTo(activeSet.playerStartSpawnPos.position);
        _enemySpawnManager.SpawnEnemies(activeSet);
        yield return StartCoroutine(_playerMover.MoveTo(activeSet.playerStartMovePos.position));

        // 2. 전투 (Combat) — 적 전멸까지 대기
        GameManager.Instance.SetState(GameState.Combat);

        bool isCleared = false;
        _enemySpawnManager.OnAllDefeated += HandleAllDefeated;
        _enemySpawnManager.StartMoving();

        yield return new WaitUntil(() => isCleared);

        _enemySpawnManager.OnAllDefeated -= HandleAllDefeated;

        void HandleAllDefeated() => isCleared = true;

        // 3. 퇴장 (Cleared)
        GameManager.Instance.SetState(GameState.Cleared);
        yield return StartCoroutine(_playerMover.MoveTo(activeSet.playerEndMovePos.position));

        // 4. 마지막 층 도달 → 시퀀스 종료
        if (_currentFloor >= _totalFloors)
        {
            yield break;
        }

        // 5. 층 전환 (Transitioning)
        GameManager.Instance.SetState(GameState.Transitioning);
        _playerMover.TeleportTo(standbySet.playerStartSpawnPos.position);
        MoveSetUp(activeSet, _gridHeight * 2f);

        // 6. 카메라 스크롤 + 그리드 순환
        yield return StartCoroutine(AdvanceFloor());

        // 7. A/B 스왑 후 다음 층 진행
        _useSetA = !_useSetA;
        StartCoroutine(RunFloor());
    }

    // 세트의 4개 Transform을 y축으로 deltaY만큼 이동
    private void MoveSetUp(FloorPositionSet set, float deltaY)
    {
        MoveTransformUp(set.playerStartSpawnPos, deltaY);
        MoveTransformUp(set.playerStartMovePos,  deltaY);
        MoveTransformUp(set.enemySpawnPos,       deltaY);
        MoveTransformUp(set.playerEndMovePos,    deltaY);
    }

    private void MoveTransformUp(Transform target, float deltaY)
    {
        Vector3 position = target.position;
        position.y      += deltaY;
        target.position  = position;
    }

    // 카메라 스크롤 + 그리드 풀 순환
    private IEnumerator AdvanceFloor()
    {
        _currentFloor++;

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
    }
}

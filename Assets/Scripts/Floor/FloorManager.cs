using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// ─────────────────────────────────────────────────────────────────────────────
/// <summary>
/// 한 층에서 사용하는 위치 참조 세트 (A 또는 B).
/// FloorManager가 A/B를 번갈아 Current/Next 역할로 사용한다.
/// </summary>
[Serializable]
public class FloorPositionSet
{
    public Transform playerStartSpawnPos;
    public Transform playerStartMovePos;
    public Transform enemySpawnPos;
    public Transform playerEndMovePos;
    [Tooltip("보상 방 전용: 박스 스폰 위치")]
    public Transform boxSpawnPos;
}

/// <summary>
/// 층 관리 오케스트레이터 — 상태 전환 + 플레이어 시퀀스 + 전투 흐름.
/// 활성화 규칙: 현재층 ~ 현재층+2 (최대 3개) + 이전 Start Grid(1층만).
/// </summary>
public class FloorManager : MonoBehaviour
{
    // ── Serialized Fields ─────────────────────────────────────────
    [Header("층 구성")]
    [SerializeField] private GridPool               _gridPool;
    [SerializeField] private CameraScrollController _cameraScroll;
    [SerializeField] private FloorGrid              _startGrid;
    [SerializeField] private float                  _gridHeight = 10f;
    [SerializeField] private BoxCollider2D          _rightWall;
    
    // _floorConfigs.Count에서 자동으로 설정됨 (Inspector 미노출)
    private int _totalFloors;

    [Header("보상 방")]
    [SerializeField] private TreasureBox _treasureBoxPrefab;

    [Header("플레이어 위치 세트")]
    [SerializeField] private FloorPositionSet _setA;
    [SerializeField] private FloorPositionSet _setB;

    // ── Events ────────────────────────────────────────────────────
    /// <summary>층이 시작될 때 현재 층 번호를 전달한다.</summary>
    public static event Action<int> OnFloorChanged;

    // ── Fields ────────────────────────────────────────────────────
    // 층 번호 → 그리드 인스턴스 매핑
    private readonly Dictionary<int, FloorGrid>   _floorGridMap = new();
    private readonly Dictionary<int, TreasureBox> _boxMap       = new();

    private int  _currentFloor = 1;
    private bool _useSetA      = true;

    private const int PreloadCount = 2;

    // ── Helpers ───────────────────────────────────────────────────
    // 층 번호 → 월드 위치 (y축 기준)
    private Vector3 FloorToPos(int floor) => Vector3.up * ((floor - 1) * _gridHeight);

    // ── MonoBehaviour ─────────────────────────────────────────────
    private void Start()
    {
        _totalFloors = EnemySpawnManager.Instance.FloorCount;
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

        // 1층·2층 적/박스/보스 사전 배치
        if (EnemySpawnManager.Instance.IsBossFloor(1))
            BossManager.Instance.PreloadBoss(EnemySpawnManager.Instance.GetBossData(1), _setA.enemySpawnPos.position);
        else
            PreloadFloorOrBox(1, _setA);

        if (2 <= _totalFloors)
        {
            if (EnemySpawnManager.Instance.IsBossFloor(2))
                BossManager.Instance.PreloadBoss(EnemySpawnManager.Instance.GetBossData(2), _setB.enemySpawnPos.position);
            else
                PreloadFloorOrBox(2, _setB);
        }

        StartCoroutine(RunFloor());
    }

    // ── Private Methods ───────────────────────────────────────────
    /// <summary>
    /// 한 층의 전체 진행 흐름 : 등장 → 전투 → 퇴장 → 층 전환 → 반복.
    /// </summary>
    private IEnumerator RunFloor()
    {
        OnFloorChanged?.Invoke(_currentFloor);

        FloorPositionSet activeSet  = _useSetA ? _setA : _setB;
        FloorPositionSet standbySet = _useSetA ? _setB : _setA;

        // 1. 등장 (Entering)
        GameManager.Instance.SetState(GameState.Entering);
        PlayerMover.Instance.TeleportTo(activeSet.playerStartSpawnPos.position);

        if (EnemySpawnManager.Instance.IsRewardFloor(_currentFloor))
        {
            yield return StartCoroutine(RunRewardRoom(activeSet));
        }
        else if (EnemySpawnManager.Instance.IsBossFloor(_currentFloor))
        {
            yield return StartCoroutine(RunBossRoom(activeSet));
        }
        else
        {
            yield return StartCoroutine(RunCombatRoom(activeSet));
        }

        // 3. 퇴장 (Cleared)
        GameManager.Instance.SetState(GameState.Cleared);
        _rightWall.enabled = false; // 벽 콜리더 끄기
        
        yield return StartCoroutine(PlayerMover.Instance.MoveTo(activeSet.playerEndMovePos.position));

        // 4. 마지막 층 도달 → 시퀀스 종료
        if (_currentFloor >= _totalFloors)
        {
            yield break;
        }

        // 5. 층 전환 (Transitioning)
        _rightWall.enabled = true; // 벽 콜리더 켜기
        
        GameManager.Instance.SetState(GameState.Transitioning);
        PlayerMover.Instance.TeleportTo(standbySet.playerStartSpawnPos.position);
        MoveSetUp(activeSet, _gridHeight * 2f);

        // 6. 카메라 스크롤 + 그리드 순환 + 다음 층 사전 배치
        yield return StartCoroutine(NextFloor(activeSet));

        // 7. A/B 스왑 후 다음 층 진행
        _useSetA = !_useSetA;
        StartCoroutine(RunFloor());
    }

    /// <summary>
    /// 세트의 Transform을 y축으로 deltaY만큼 이동한다.
    /// </summary>
    private void MoveSetUp(FloorPositionSet set, float deltaY)
    {
        MoveTransformUp(set.playerStartSpawnPos, deltaY);
        MoveTransformUp(set.playerStartMovePos,  deltaY);
        MoveTransformUp(set.enemySpawnPos,       deltaY);
        MoveTransformUp(set.playerEndMovePos,    deltaY);

        if (set.boxSpawnPos != null)
            MoveTransformUp(set.boxSpawnPos, deltaY);
    }

    private void MoveTransformUp(Transform target, float deltaY)
    {
        Vector3 position  = target.position;
        position.y       += deltaY;
        target.position   = position;
    }

    /// <summary>
    /// 카메라 스크롤 후 그리드 풀을 순환하며 다음 층을 준비한다.
    /// MoveSetUp 완료 후 activeSet이 currentFloor+1 위치에 있으므로
    /// enemySpawnPos를 사전 배치 기준으로 직접 사용한다.
    /// </summary>
    private IEnumerator NextFloor(FloorPositionSet activeSet)
    {
        _currentFloor++;

        yield return StartCoroutine(_cameraScroll.ScrollTo(FloorToPos(_currentFloor).y));

        // 현재층-2 그리드 비활성화 + 대기 적 해제 (이미 활성화된 경우 무시)
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

        EnemySpawnManager.Instance.UnloadFloor(removeFloor);

        // 현재층-2 박스 정리 (보상 방이었던 경우)
        if (_boxMap.TryGetValue(removeFloor, out TreasureBox removeBox))
        {
            Destroy(removeBox.gameObject);
            _boxMap.Remove(removeFloor);
        }

        // 현재층-2가 보스 방이었던 경우 보스 오브젝트 제거
        if (EnemySpawnManager.Instance.IsBossFloor(removeFloor))
        {
            BossManager.Instance.ClearBoss();
        }

        // 현재층+2에 새 그리드 배치
        int addFloor = _currentFloor + 2;

        if (addFloor <= _totalFloors && !_floorGridMap.ContainsKey(addFloor))
        {
            FloorGrid newGrid = (addFloor == _totalFloors) ? _gridPool.GetEnd(FloorToPos(addFloor))
                                                           : _gridPool.GetLoop(FloorToPos(addFloor));

            _floorGridMap[addFloor] = newGrid;
        }

        // 현재층+1 적/박스/보스 사전 배치
        int nextFloor = _currentFloor + 1;

        if (nextFloor <= _totalFloors)
        {
            if (EnemySpawnManager.Instance.IsBossFloor(nextFloor))
            {
                // 보스를 미리 스폰 (AI는 비활성, Idle 상태로 대기)
                BossManager.Instance.PreloadBoss(
                    EnemySpawnManager.Instance.GetBossData(nextFloor),
                    activeSet.enemySpawnPos.position);
            }
            else if (!EnemySpawnManager.Instance.HasPendingFloor(nextFloor) && !_boxMap.ContainsKey(nextFloor))
            {
                PreloadFloorOrBox(nextFloor, activeSet);
            }
        }
    }

    /// <summary>
    /// 일반 전투 방 시퀀스: 적 활성화 → 입장 이동 → 전투 → 전멸 대기.
    /// </summary>
    private IEnumerator RunCombatRoom(FloorPositionSet activeSet)
    {
        EnemySpawnManager.Instance.ActivateFloor(_currentFloor);
        yield return StartCoroutine(PlayerMover.Instance.MoveTo(activeSet.playerStartMovePos.position));

        GameManager.Instance.SetState(GameState.Combat);

        bool isCleared = false;
        EnemySpawnManager.Instance.OnAllDefeated += HandleAllDefeated;
        EnemySpawnManager.Instance.StartMoving();

        yield return new WaitUntil(() => isCleared);

        EnemySpawnManager.Instance.OnAllDefeated -= HandleAllDefeated;

        void HandleAllDefeated() => isCleared = true;
    }

    /// <summary>
    /// 보스 방 시퀀스: 보스 스폰 → 입장 이동 → 연출 → 전투 → 사망 대기.
    /// </summary>
    private IEnumerator RunBossRoom(FloorPositionSet activeSet)
    {
        // 보스는 이미 PreloadBoss()에서 스폰되어 있음
        yield return StartCoroutine(PlayerMover.Instance.MoveTo(activeSet.playerStartMovePos.position));

        GameManager.Instance.SetState(GameState.Combat);

        Debug.Log($"[FloorManager] 보스 방 진입 — floor {_currentFloor}");

        bool isCleared = false;
        BossManager.Instance.OnBossDefeated += HandleBossDefeated;

        yield return StartCoroutine(BossManager.Instance.PlayIntroAndActivate(PlayerMover.Instance.transform));

        yield return new WaitUntil(() => isCleared);

        BossManager.Instance.OnBossDefeated -= HandleBossDefeated;

        void HandleBossDefeated() => isCleared = true;
    }

    /// <summary>
    /// 보상 방 진행 시퀀스: 입장 이동 → Reward 상태 → 박스 오픈 → 완료 대기.
    /// </summary>
    private IEnumerator RunRewardRoom(FloorPositionSet activeSet)
    {
        yield return StartCoroutine(PlayerMover.Instance.MoveTo(activeSet.playerStartMovePos.position));

        // 보상 방 상태 전환 → PlayerAnimator가 Idle 재생
        GameManager.Instance.SetState(GameState.Reward);

        Inventory.ItemData reward = EnemySpawnManager.Instance.GetRewardItem(_currentFloor);
        TreasureBox        box    = _boxMap[_currentFloor];

        bool rewardCollected = false;
        box.Open(reward, () => rewardCollected = true);

        yield return new WaitUntil(() => rewardCollected);
    }

    /// <summary>
    /// 층 유형에 따라 적/박스를 사전 배치한다. 보스 방은 스폰을 건너뛴다.
    /// </summary>
    private void PreloadFloorOrBox(int floor, FloorPositionSet set)
    {
        if (EnemySpawnManager.Instance.IsRewardFloor(floor))
        {
            if (!_boxMap.ContainsKey(floor))
            {
                PreloadBox(floor, set.boxSpawnPos.position);
            }
        }
        else
        {
            if (!EnemySpawnManager.Instance.HasPendingFloor(floor))
            {
                EnemySpawnManager.Instance.PreloadFloor(floor, set.enemySpawnPos.position);
            }
        }
    }

    /// <summary>
    /// 보상 방 박스를 스폰하고 층 번호와 매핑한다.
    /// </summary>
    private void PreloadBox(int floor, Vector2 spawnPos)
    {
        TreasureBox box = Instantiate(_treasureBoxPrefab, spawnPos, Quaternion.identity);
        _boxMap[floor] = box;
    }
}

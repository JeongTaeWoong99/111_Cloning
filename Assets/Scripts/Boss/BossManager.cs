using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 보스 생명주기 관리 싱글턴 — 스폰·입장 연출·전투 활성화·제거.
/// FloorManager에서 호출한다.
/// </summary>
public class BossManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────
    public static BossManager Instance { get; private set; }

    // ── Serialized Fields ─────────────────────────────────────────
    [SerializeField] private Boss _bossPrefab;

    // ── Properties ───────────────────────────────────────────────
    /// <summary>현재 활성 보스. 대쉬·넉백 타겟 참조용.</summary>
    public Boss CurrentBoss => _currentBoss;

    /// <summary>보스가 Attack/Skill 동작 중인지 여부. 패링 경직 조건 판단에 사용.</summary>
    public bool IsBossInAction => _currentBossAI != null && _currentBossAI.IsInAction;

    // ── Events ────────────────────────────────────────────────────
    public event Action OnBossDefeated;

    // ── Fields ────────────────────────────────────────────────────
    private Boss    _currentBoss;
    private BossAI  _currentBossAI;

    // ── MonoBehaviour ─────────────────────────────────────────────
    private void Awake()
    {
        Instance = this;
    }

    // ── Public Methods ────────────────────────────────────────────
    /// <summary>
    /// 보스를 미리 스폰하고 초기화한다. AI는 Activate() 호출 전까지 동작하지 않는다.
    /// </summary>
    public void PreloadBoss(BossData data, Vector2 position)
    {
        // 이미 다른 보스가 있으면 먼저 정리 (연속 보스 방 등 예외 방어)
        if (_currentBoss != null) ClearBoss();

        _currentBoss   = Instantiate(_bossPrefab, position, Quaternion.identity);
        _currentBossAI = _currentBoss.GetComponent<BossAI>();

        _currentBoss.Initialize(data);
        _currentBoss.OnDied += HandleBossDied;
    }

    /// <summary>
    /// 입장 연출을 재생하고 완료 후 AI를 활성화한다.
    /// IBossIntroSequence가 없으면 즉시 활성화 (NullObject 패턴).
    /// </summary>
    public IEnumerator PlayIntroAndActivate(Transform playerTransform)
    {
        if (_currentBoss == null)
        {
            yield break;
        }

        // 보스 오브젝트에 연출 컴포넌트가 있으면 재생
        IBossIntroSequence intro = _currentBoss.GetComponent<IBossIntroSequence>();

        if (intro != null)
        {
            yield return _currentBoss.StartCoroutine(intro.PlayIntro());
        }

        _currentBossAI.Activate(playerTransform);
    }

    /// <summary>
    /// 보스를 경직시킨다. S-패링 성공 시 PlayerCombat에서 호출한다.
    /// </summary>
    public void StaggerBoss(float duration)
    {
        _currentBossAI?.ApplyStagger(duration);
    }

    /// <summary>
    /// 보스에게 넉백을 적용한다. PlayerBoundaryHandler의 반격 스킬에서 호출한다.
    /// </summary>
    public void KnockbackBoss(Vector2 force, float duration)
    {
        if (_currentBoss == null || _currentBoss.IsDying)
        {
            return;
        }

        _currentBoss.ApplyKnockback(force, duration);
    }

    /// <summary>
    /// 현재 보스 오브젝트를 제거한다. NextFloor 시 FloorManager에서 호출한다.
    /// </summary>
    public void ClearBoss()
    {
        if (_currentBoss == null)
        {
            return;
        }

        _currentBoss.OnDied -= HandleBossDied;
        Destroy(_currentBoss.gameObject);
        _currentBoss   = null;
        _currentBossAI = null;
    }

    // ── Private Methods ───────────────────────────────────────────
    private void HandleBossDied(Enemy boss)
    {
        if (_currentBossAI != null)
        {
            _currentBossAI.OnBossDying();
        }

        OnBossDefeated?.Invoke();
    }
}

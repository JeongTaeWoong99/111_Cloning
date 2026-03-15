using System.Collections;
using Inventory;
using UnityEngine;

/// <summary>
/// 플레이어 스킬(F키) 실행 핸들러.
/// 쿨다운 관리·WeaponType 분기·각 스킬 실행을 전담한다.
/// PlayerCombat.HandleSkill()이 TryExecute()를 호출한다.
/// </summary>
public class PlayerSkillHandler : MonoBehaviour
{
    public static PlayerSkillHandler Instance { get; private set; }

    // ── Serialized Fields ─────────────────────────────────────────
    [Header("스킬 공통")]
    private float _skillCooldown = 10f;  // CharacterData.skillCooldown에서 초기화

    [SerializeField, Tooltip("스킬 전 연출 대기 (초) — 추후 반짝 이펙트 타이밍"), Range(0f, 2f)]
    private float _skillDelay = 0.5f;

    [Header("Slash (Sword)")]
    [SerializeField, Tooltip("전방 히트박스 너비 (m)"), Range(1f, 10f)]
    private float _slashRange = 4f;

    [Header("Arrow Rain (Bow)")]
    [SerializeField, Tooltip("발사 화살 수"), Range(5, 30)]
    private int   _arrowCount = 20;

    [SerializeField, Tooltip("화살 간 발사 딜레이 (초)"), Range(0f, 1f)]
    private float _arrowInterval = 0.05f;

    [SerializeField, Tooltip("플레이어 Y 기준 스폰 높이 오프셋"), Range(0f, 15f)]
    private float _arrowRainYOffset = 5f;

    [SerializeField, Tooltip("플레이어 X 기준 왼쪽 스프레드 거리"), Range(0f, 15f)]
    private float _arrowRainXLeft = 5f;

    [SerializeField, Tooltip("플레이어 X 기준 오른쪽 스프레드 거리"), Range(0f, 15f)]
    private float _arrowRainXRight = 5f;

    [Header("Clone (Spear)")]
    [SerializeField, Tooltip("소환할 Clone 프리팹")]
    private PlayerClone _clonePrefab;

    [SerializeField, Tooltip("플레이어 기준 Clone 스폰 오프셋")]
    private Vector2 _cloneSpawnOffset = new Vector2(-1.5f, 0f);

    [Header("공통 레이어")]
    [SerializeField]
    private LayerMask _enemyLayer;

    // ── Properties ───────────────────────────────────────────────
    /// <summary>F 스킬 쿨타임 잔여 시간. 0이면 즉시 사용 가능.</summary>
    public float SkillCooldownRemaining => Mathf.Max(0f, _skillCooldown - _skillTimer);

    // ── Fields ────────────────────────────────────────────────────
    // float.MaxValue: 시작 시 쿨다운 없이 즉시 사용 가능
    private float       _skillTimer  = float.MaxValue;
    private PlayerClone _activeClone;

    // ── MonoBehaviour ─────────────────────────────────────────────
    private void Awake()
    {
        Instance = this;

        PlayerInventory.Instance.OnCharacterChanged += ApplySkillCooldown;
        ApplySkillCooldown();
    }

    private void OnDestroy()
    {
        if (PlayerInventory.Instance != null)
            PlayerInventory.Instance.OnCharacterChanged -= ApplySkillCooldown;
    }

    private void Update() => _skillTimer += Time.deltaTime;

    // ── Public Methods ────────────────────────────────────────────
    private void ApplySkillCooldown()
    {
        CharacterData ch = PlayerInventory.Instance?.SelectedCharacter;
        if (ch != null) _skillCooldown = ch.skillCooldown;
    }

    /// <summary>
    /// F키 입력 시 PlayerCombat에서 호출. 쿨다운 미충족 시 무시.
    /// </summary>
    public void TryExecute()
    {
        if (_skillTimer < _skillCooldown) return;
        
        CharacterData data = PlayerInventory.Instance.SelectedCharacter;
        if (data == null) return;

        _skillTimer = 0f;

        switch (data.weaponType)
        {
            case WeaponType.Sword: StartCoroutine(ExecuteSlash());     break;
            case WeaponType.Bow:   StartCoroutine(ExecuteArrowRain()); break;
            case WeaponType.Spear: StartCoroutine(ExecuteClone());      break;
        }
    }

    // ── 스킬 구현 ─────────────────────────────────────────────────

    /// <summary>
    /// Slash — 0.5초 연출 대기 후 전방 범위에 2× ATK 데미지.
    /// </summary>
    private IEnumerator ExecuteSlash()
    {
        // Skill 상태(timeScale=0)로 전체 freeze
        GameManager.Instance.SetState(GameState.Skill);
        yield return new WaitForSecondsRealtime(_skillDelay);
        // 판정 실행 전 Combat 복귀(timeScale=1)
        GameManager.Instance.SetState(GameState.Combat);

        float   damage    = PlayerStats.Instance.TotalAttack * 2f;
        Vector2 boxCenter = (Vector2)transform.position + Vector2.right * (_slashRange * 0.5f);
        Vector2 boxSize   = new Vector2(_slashRange, 1.5f);

        Collider2D[] hits = Physics2D.OverlapBoxAll(boxCenter, boxSize, 0f, _enemyLayer);
        foreach (Collider2D hit in hits)
        {
            if (hit.TryGetComponent(out Enemy enemy))
                enemy.TakeDamage(damage);
        }
    }

    /// <summary>
    /// Arrow Rain — 0.5초 대기 후 X 범위에 분산된 화살 20발을 아래로 낙하.
    /// Arrow는 Z축 -90° 회전으로 로컬 right = 월드 down 방향으로 이동.
    /// </summary>
    private IEnumerator ExecuteArrowRain()
    {
        // Skill 상태(timeScale=0)로 전체 freeze
        GameManager.Instance.SetState(GameState.Skill);
        yield return new WaitForSecondsRealtime(_skillDelay);
        // 판정 실행 전 Combat 복귀(timeScale=1)
        GameManager.Instance.SetState(GameState.Combat);

        float damage  = PlayerStats.Instance.TotalAttack;
        float xStart  = transform.position.x - _arrowRainXLeft;
        float xEnd    = transform.position.x + _arrowRainXRight;
        float spawnY  = transform.position.y + _arrowRainYOffset;

        for (int i = 0; i < _arrowCount; i++)
        {
            GameObject obj = ObjectPoolManager.Instance.Get("RainArrow");
                
            if (obj != null && obj.TryGetComponent(out Arrow arrow))
            {
                Vector2 spawnPos = new Vector2(Random.Range(xStart, xEnd), spawnY);

                // -90° 회전: 로컬 right = 월드 down → Arrow.Update의 Translate가 비스듬하게 이동
                obj.transform.rotation = Quaternion.Euler(0f, 0f, -45f);
                arrow.Initialize(spawnPos, damage, _enemyLayer);
            }

            yield return new WaitForSeconds(_arrowInterval);
        }
    }

    /// <summary>
    /// Clone — 0.5초 연출 대기 후 50% ATK Clone 소환.
    /// </summary>
    private IEnumerator ExecuteClone()
    {
        if (_clonePrefab == null)
        {
            Debug.LogWarning("[SkillHandler] Clone 프리팹이 연결되지 않았습니다.");
            yield break;
        }

        // Skill 상태(timeScale=0)로 전체 freeze
        GameManager.Instance.SetState(GameState.Skill);
        yield return new WaitForSecondsRealtime(_skillDelay);
        // 소환 전 Combat 복귀(timeScale=1)
        GameManager.Instance.SetState(GameState.Combat);

        // 기존 클론이 살아있으면 제거 — 중복 생성 방지
        if (_activeClone != null)
            Destroy(_activeClone.gameObject);

        // 플레이어 자식으로 생성 → 플레이어를 따라다님
        _activeClone = Instantiate(_clonePrefab, transform);
        _activeClone.transform.localPosition = _cloneSpawnOffset;
        _activeClone.Initialize(PlayerStats.Instance.TotalAttack * 0.5f, _enemyLayer);
    }

    // ── Gizmos ────────────────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        // Slash 범위
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Vector3 slashCenter = transform.position + Vector3.right * (_slashRange * 0.5f);
        Gizmos.DrawCube(slashCenter, new Vector3(_slashRange, 1.5f, 0f));
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.8f);
        Gizmos.DrawWireCube(slashCenter, new Vector3(_slashRange, 1.5f, 0f));

        // Arrow Rain 스폰 영역
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.5f);
        float   rainXCenter = transform.position.x + (_arrowRainXRight - _arrowRainXLeft) * 0.5f;
        float   rainWidth   = _arrowRainXLeft + _arrowRainXRight;
        Vector3 rainCenter  = new Vector3(rainXCenter, transform.position.y + _arrowRainYOffset, 0f);
        Gizmos.DrawWireCube(rainCenter, new Vector3(rainWidth, 0.2f, 0f));
    }
}

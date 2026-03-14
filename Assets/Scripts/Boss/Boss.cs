using System;
using UnityEngine;

/// <summary>
/// 보스 개체 — Enemy를 상속하며 체력 이벤트·공격/스킬 애니메이션을 담당한다.
/// 실제 AI 제어는 BossAI가 담당한다.
/// </summary>
public class Boss : Enemy
{
    // ── Serialized Fields ─────────────────────────────────────────
    [SerializeField] private LayerMask _playerLayer;
    [SerializeField] private GameObject _spearPrefab;

    // ── Properties ───────────────────────────────────────────────
    public BossData BossData    { get; private set; }
    public float    HealthRatio => _maxHealth > 0f ? _health / _maxHealth : 0f;

    // ── Events ────────────────────────────────────────────────────
    /// <summary>현재 체력 비율 (0~1)을 전달한다. UI 연동용.</summary>
    public event Action<float> OnHealthChanged;

    // ── Fields ────────────────────────────────────────────────────
    private Animator        _bossAnimator;
    private SpriteRenderer  _spriteRenderer;
    private Transform       _playerTransform;

    // ── Public Methods ────────────────────────────────────────────
    /// <summary>
    /// BossAI.Activate에서 플레이어 참조를 전달한다.
    /// </summary>
    public void SetPlayer(Transform playerTransform)
    {
        _playerTransform = playerTransform;
    }

    /// <summary>
    /// BossData로 보스 스탯·애니메이션을 초기화한다.
    /// </summary>
    public void Initialize(BossData data)
    {
        // Awake 시점에 캐싱하지 않고 최초 Initialize에서 캐싱
        // (Enemy.Awake와 충돌 없이 안전하게 참조 획득)
        if (_bossAnimator == null)
        {
            _bossAnimator = GetComponent<Animator>();
        }

        if (_spriteRenderer == null)
        {
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        BossData   = data;
        _maxHealth = data.maxHealth;
        _health    = _maxHealth;
        MoveSpeed  = data.moveSpeed;

        if (data.overrideController != null)
        {
            _bossAnimator.runtimeAnimatorController = data.overrideController;
        }

        PlayIdle();
    }

    /// <summary>
    /// 데미지를 적용하고 OnHealthChanged 이벤트를 발행한다.
    /// </summary>
    public override void TakeDamage(float amount)
    {
        base.TakeDamage(amount);
        OnHealthChanged?.Invoke(HealthRatio);
    }

    // ── 경직 시각 효과 ────────────────────────────────────────────
    /// <summary>Animator를 일시 정지한다. ApplyStagger에서 호출한다.</summary>
    public void PauseAnimation()
    {
        _bossAnimator.speed = 0f;
    }

    /// <summary>Animator를 재개한다. StaggerRoutine 종료 후 호출한다.</summary>
    public void ResumeAnimation()
    {
        _bossAnimator.speed = 1f;
    }

    /// <summary>SpriteRenderer 색을 경직 색(파랑 계열)으로 변경한다.</summary>
    public void SetStaggerColor()
    {
        if (_spriteRenderer != null)
            _spriteRenderer.color = new Color(0.4f, 0.6f, 1f, 1f);
    }

    /// <summary>SpriteRenderer 색을 원래대로 복원한다.</summary>
    public void ResetColor()
    {
        if (_spriteRenderer != null)
            _spriteRenderer.color = Color.white;
    }

    // ── 애니메이션 래퍼 ───────────────────────────────────────────
    /// <summary>"Attack" 클립을 재생한다. 모든 보스가 공통으로 사용한다.</summary>
    public void PlayAttack() => _bossAnimator.Play("Attack");

    /// <summary>"Skill" 클립을 재생한다. 보스마다 다른 효과를 가진다.</summary>
    public void PlaySkill() => _bossAnimator.Play("Skill");

    // ── 애니메이션 이벤트 수신 ────────────────────────────────────
    /// <summary>Attack 클립의 타격 프레임에서 호출된다.</summary>
    public void OnAttackHit()
    {
        // 보스는 왼쪽을 향하므로 박스를 왼쪽으로 오프셋
        Vector2 center = (Vector2)transform.position + Vector2.left * (BossData.attackRange * 0.5f);
        Vector2 size   = new Vector2(BossData.attackRange, 1.5f);

        Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, 0f, _playerLayer);
        foreach (Collider2D hit in hits)
        {
            if (hit.TryGetComponent(out PlayerHealth player))
                player.TakeDamage((int)BossData.attackDamage);
        }
    }

    /// <summary>Skill 클립의 발동 프레임에서 호출된다.</summary>
    public void OnSkillExecute()
    {
        if (BossData.skill == null) return;

        switch (BossData.skill.skillType)
        {
            case BossSkillType.Charge:
                GetComponent<BossAI>().StartCharge();
                break;
            case BossSkillType.SpearThrow:
                SpawnSpears();
                break;
        }
    }

    // ── Private Methods ───────────────────────────────────────────
    private void SpawnSpears()
    {
        float  speed  = BossData.skill.spearThrowSpeed;
        float  damage = BossData.skill.damage;
        // 45도 각도로 좌우 투척
        Vector2 leftVel  = new Vector2(-speed * 0.707f, speed * 0.707f);
        Vector2 rightVel = new Vector2( speed * 0.707f, speed * 0.707f);

        SpawnSpear(leftVel,  damage);
        SpawnSpear(rightVel, damage);
    }

    private void SpawnSpear(Vector2 velocity, float damage)
    {
        if (_spearPrefab == null) return;

        GameObject obj = Instantiate(_spearPrefab, transform.position, Quaternion.identity);
        if (obj.TryGetComponent(out BossSpear spear))
            spear.Initialize(velocity, damage, _playerLayer);
    }
}

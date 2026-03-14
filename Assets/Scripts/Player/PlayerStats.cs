using Inventory;
using UnityEngine;

/// <summary>
/// 장착된 모든 장비의 스탯을 합산하는 DontDestroyOnLoad 싱글턴.
/// PlayerInventory와 같은 GameObject에 부착하세요.
/// </summary>
[DefaultExecutionOrder(-50)]
public class PlayerStats : MonoBehaviour
{
    // ──────────────────────────────────────────
    // Static
    // ──────────────────────────────────────────
    public static PlayerStats Instance { get; private set; }

    // Enum.GetValues 호출 시 매번 배열이 생성되므로 정적 캐싱
    private static readonly ItemType[] SlotTypes = (ItemType[])System.Enum.GetValues(typeof(ItemType));

    // ──────────────────────────────────────────
    // Properties
    // ──────────────────────────────────────────
    public int   TotalAttack      { get; private set; }
    public int   TotalHealth      { get; private set; }
    public float TotalAttackSpeed { get; private set; }

    // ──────────────────────────────────────────
    // MonoBehaviour
    // ──────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        PlayerInventory.Instance.OnChanged          += Recalculate;
        PlayerInventory.Instance.OnCharacterChanged += Recalculate;
        Recalculate();
    }

    private void OnDestroy()
    {
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.OnChanged          -= Recalculate;
            PlayerInventory.Instance.OnCharacterChanged -= Recalculate;
        }
    }

    // ──────────────────────────────────────────
    // Public Methods
    // ──────────────────────────────────────────

    /// <summary>
    /// 장착 아이템 + 캐릭터 기본 스탯을 재계산합니다.
    /// 변경 시 자동 호출됩니다.
    /// </summary>
    public void Recalculate()
    {
        CharacterData ch = PlayerInventory.Instance?.SelectedCharacter;
        int   attack      = ch?.baseAttack      ?? 1;
        int   health      = ch?.baseHealth      ?? 3;
        float attackSpeed = ch?.baseAttackSpeed ?? 1f;

        foreach (ItemType slot in SlotTypes)
        {
            ItemData item = PlayerInventory.Instance.GetEquipped(slot);

            if (item == null)
            {
                continue;
            }

            attack      += item.attackBonus;
            health      += item.healthBonus;
            attackSpeed += item.attackSpeedBonus / 100f;
        }
 
        TotalAttack      = attack;
        TotalHealth      = health;
        TotalAttackSpeed = attackSpeed;
    }
}

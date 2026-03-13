using System;
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

    // ──────────────────────────────────────────
    // Private Fields
    // ──────────────────────────────────────────
    private const int BaseAttack = 1;
    private const int BaseHealth = 3;

    // ──────────────────────────────────────────
    // Properties
    // ──────────────────────────────────────────
    public int TotalAttack { get; private set; }
    public int TotalHealth { get; private set; }

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
        // PlayerInventory가 DontDestroyOnLoad를 처리하므로 이 컴포넌트는 별도 호출 불필요

        PlayerInventory.Instance.OnChanged += Recalculate;
        Recalculate();
    }

    private void OnDestroy()
    {
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.OnChanged -= Recalculate;
        }
    }

    // ──────────────────────────────────────────
    // Public Methods
    // ──────────────────────────────────────────

    /// <summary>장착 아이템 스탯을 재계산합니다. 장착 변경 시 자동 호출됩니다.</summary>
    public void Recalculate()
    {
        int attack = BaseAttack;
        int health = BaseHealth;

        foreach (ItemType slot in Enum.GetValues(typeof(ItemType)))
        {
            ItemData item = PlayerInventory.Instance.GetEquipped(slot);

            if (item == null)
            {
                continue;
            }

            attack += item.attackBonus;
            health += item.healthBonus;
        }

        TotalAttack = attack;
        TotalHealth = health;
    }
}

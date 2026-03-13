using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Inventory
{
    [DefaultExecutionOrder(-100)]
    public class PlayerInventory : MonoBehaviour
    {
        // ──────────────────────────────────────────
        // Static
        // ──────────────────────────────────────────
        public static PlayerInventory Instance { get; private set; }

        // ──────────────────────────────────────────
        // [SerializeField] Private Fields
        // ──────────────────────────────────────────
        [Header("데이터베이스")]
        [SerializeField, Tooltip("전체 아이템 목록 SO")]
        private ItemDatabase _database;

        [SerializeField, Tooltip("캐릭터 목록 SO")]
        private CharacterDatabase _characterDatabase;

        // ──────────────────────────────────────────
        // Private Fields
        // ──────────────────────────────────────────
        private const int MaxInventorySize = 25;

        private List<ItemData>                  _items    = new();
        private Dictionary<ItemType, ItemData>  _equipped = new();

        // ──────────────────────────────────────────
        // Properties
        // ──────────────────────────────────────────
        public IReadOnlyList<ItemData> Items            => _items;
        public CharacterData           SelectedCharacter { get; private set; }

        // ──────────────────────────────────────────
        // Events
        // ──────────────────────────────────────────
        public event Action OnChanged;
        public event Action OnCharacterChanged;

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
            DontDestroyOnLoad(gameObject);

            SceneManager.sceneLoaded += OnSceneLoaded;
            Load();
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        /// <summary>씬 전환 후 모든 구독자에게 현재 상태를 재발행합니다.</summary>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            OnChanged?.Invoke();
            OnCharacterChanged?.Invoke();
        }

        // ──────────────────────────────────────────
        // Public Methods
        // ──────────────────────────────────────────

        /// <summary>아이템을 인벤토리에 추가합니다. 꽉 차거나 null이면 false를 반환합니다.</summary>
        public bool AddItem(ItemData item)
        {
            if (item == null)
            {
                Debug.LogWarning("[PlayerInventory] null 아이템은 추가할 수 없습니다.");
                return false;
            }

            if (_items.Count >= MaxInventorySize)
            {
                return false;
            }

            _items.Add(item);
            OnChanged?.Invoke();
            return true;
        }

        /// <summary>아이템을 인벤토리에서 제거합니다.</summary>
        public void RemoveItem(ItemData item)
        {
            _items.Remove(item);
            OnChanged?.Invoke();
        }

        /// <summary>
        /// 아이템을 지정 슬롯에 장착합니다.
        /// 인벤토리에서 제거되며, 기존 슬롯 아이템은 인벤토리로 이동합니다.
        /// 무기 슬롯의 경우 캐릭터 무기 타입 불일치 시 false를 반환합니다.
        /// </summary>
        public bool EquipItem(ItemData item, ItemType slot)
        {
            // 무기 슬롯: 캐릭터 타입 불일치 시 거부
            if (slot == ItemType.Weapon
                && item.weaponType != WeaponType.None
                && SelectedCharacter != null
                && item.weaponType != SelectedCharacter.weaponType)
            {
                return false;
            }

            // 기존 장착 아이템 → 인벤토리로 이동
            if (_equipped.TryGetValue(slot, out ItemData previousItem) && previousItem != null)
            {
                _items.Add(previousItem);
            }

            _items.Remove(item);
            _equipped[slot] = item;

            Save();
            OnChanged?.Invoke();
            return true;
        }

        /// <summary>슬롯의 장착 아이템을 해제하고 인벤토리로 이동합니다.</summary>
        public void UnequipItem(ItemType slot)
        {
            if (!_equipped.TryGetValue(slot, out ItemData item) || item == null)
            {
                return;
            }

            _items.Add(item);
            _equipped[slot] = null;

            Save();
            OnChanged?.Invoke();
        }

        /// <summary>슬롯에 장착된 아이템을 반환합니다. 없으면 null.</summary>
        public ItemData GetEquipped(ItemType slot)
        {
            return _equipped.TryGetValue(slot, out ItemData item) ? item : null;
        }

        /// <summary>캐릭터를 선택합니다. 타입이 다른 무기가 장착 중이면 자동 해제합니다.</summary>
        public void SelectCharacter(CharacterData character)
        {
            SelectedCharacter = character;

            // 다른 타입 무기 장착 중이면 자동 해제
            ItemData weapon = GetEquipped(ItemType.Weapon);
            if (weapon != null && weapon.weaponType != WeaponType.None
                && weapon.weaponType != character.weaponType)
            {
                UnequipItem(ItemType.Weapon);
            }

            PlayerPrefs.SetString("selected_character", character.name);
            PlayerPrefs.Save();
            OnCharacterChanged?.Invoke();
        }

        // ──────────────────────────────────────────
        // Save / Load
        // ──────────────────────────────────────────

        /// <summary>인벤토리와 장착 상태를 PlayerPrefs에 저장합니다.</summary>
        public void Save()
        {
            // 인벤토리
            PlayerPrefs.SetInt("inv_count", _items.Count);
            for (int i = 0; i < _items.Count; i++)
            {
                PlayerPrefs.SetString($"inv_{i}", _items[i].name);
            }

            // 장착 슬롯
            foreach (ItemType slotType in Enum.GetValues(typeof(ItemType)))
            {
                string savedName = _equipped.TryGetValue(slotType, out ItemData equippedItem) && equippedItem != null
                    ? equippedItem.name
                    : "";

                PlayerPrefs.SetString($"equip_{slotType}", savedName);
            }

            PlayerPrefs.Save();
        }

        /// <summary>PlayerPrefs에서 인벤토리와 장착 상태를 불러옵니다.</summary>
        public void Load()
        {
            if (_database == null)
            {
                Debug.LogWarning("[PlayerInventory] ItemDatabase가 연결되지 않았습니다.");
                return;
            }

            _items.Clear();
            _equipped.Clear();

            // 인벤토리
            int count = PlayerPrefs.GetInt("inv_count", 0);
            for (int i = 0; i < count; i++)
            {
                string itemName = PlayerPrefs.GetString($"inv_{i}", "");
                ItemData item   = _database.FindByName(itemName);

                if (item != null)
                {
                    _items.Add(item);
                }
            }

            // 장착 슬롯
            foreach (ItemType slotType in Enum.GetValues(typeof(ItemType)))
            {
                string itemName   = PlayerPrefs.GetString($"equip_{slotType}", "");
                ItemData equipped = _database.FindByName(itemName);

                _equipped[slotType] = equipped;
            }

            // 선택된 캐릭터 복원
            if (_characterDatabase != null)
            {
                string savedCharacter = PlayerPrefs.GetString("selected_character", "");
                SelectedCharacter = _characterDatabase.FindByName(savedCharacter)
                    ?? (_characterDatabase.characters?.Count > 0 ? _characterDatabase.characters[0] : null);
            }

            // 복원된 캐릭터와 무기 타입 불일치 시 해제
            if (SelectedCharacter != null)
            {
                ItemData weapon = GetEquipped(ItemType.Weapon);
                if (weapon != null
                    && weapon.weaponType != WeaponType.None
                    && weapon.weaponType != SelectedCharacter.weaponType)
                {
                    _items.Add(weapon);
                    _equipped[ItemType.Weapon] = null;
                }
            }

            OnChanged?.Invoke();
        }
    }
}

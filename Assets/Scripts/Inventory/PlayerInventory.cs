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

        // Enum.GetValues 호출 시 매번 배열이 생성되므로 정적 캐싱
        private static readonly ItemType[] SlotTypes = (ItemType[])Enum.GetValues(typeof(ItemType));

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

        // 슬롯 위치를 유지하는 고정 크기 배열 (null = 빈 슬롯)
        private ItemData[] _slots = new ItemData[MaxInventorySize];
        private Dictionary<ItemType, ItemData> _equipped = new();

        // ──────────────────────────────────────────
        // Properties
        // ──────────────────────────────────────────
        public IReadOnlyList<ItemData> Slots         => _slots;
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

        /// <summary>아이템을 첫 번째 빈 슬롯에 추가합니다. 꽉 차거나 null이면 false를 반환합니다.</summary>
        public bool AddItem(ItemData item)
        {
            if (item == null)
            {
                Debug.LogWarning("[PlayerInventory] null 아이템은 추가할 수 없습니다.");
                return false;
            }

            if (!PlaceInFirstEmpty(item)) return false;

            OnChanged?.Invoke();
            Save();
            return true;
        }

        /// <summary>아이템을 인벤토리에서 제거합니다.</summary>
        public void RemoveItem(ItemData item)
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] == item)
                {
                    _slots[i] = null;
                    OnChanged?.Invoke();
                    return;
                }
            }
        }

        /// <summary>
        /// 아이템을 지정 슬롯에 장착합니다.
        /// sourceSlotIndex가 유효하면 해당 슬롯의 아이템을 제거하고 기존 장착 아이템을 그 자리에 배치합니다.
        /// 무기 슬롯의 경우 캐릭터 무기 타입 불일치 시 false를 반환합니다.
        /// </summary>
        public bool EquipItem(ItemData item, ItemType slot, int sourceSlotIndex = -1)
        {
            // 무기 슬롯: 캐릭터 타입 불일치 시 거부
            if (slot == ItemType.Weapon
                && item.weaponType != WeaponType.None
                && SelectedCharacter != null
                && item.weaponType != SelectedCharacter.weaponType)
            {
                return false;
            }

            ItemData prev = _equipped.TryGetValue(slot, out var p) ? p : null;

            if (sourceSlotIndex >= 0 && sourceSlotIndex < _slots.Length)
            {
                // 소스 슬롯에 기존 장착 아이템 배치 (prev가 null이면 슬롯이 비워짐)
                _slots[sourceSlotIndex] = prev;
            }
            else
            {
                // 소스 슬롯 없이 아이템 검색 제거 후 기존 장착 아이템을 빈 슬롯에 배치
                for (int i = 0; i < _slots.Length; i++)
                    if (_slots[i] == item) { _slots[i] = null; break; }

                if (prev != null) PlaceInFirstEmpty(prev);
            }

            _equipped[slot] = item;
            Save();
            OnChanged?.Invoke();
            return true;
        }

        /// <summary>슬롯의 장착 아이템을 해제합니다. targetSlotIndex가 유효하면 해당 슬롯에 배치합니다.</summary>
        public void UnequipItem(ItemType slot, int targetSlotIndex = -1)
        {
            if (!_equipped.TryGetValue(slot, out ItemData item) || item == null) return;

            if (targetSlotIndex >= 0 && targetSlotIndex < _slots.Length)
                _slots[targetSlotIndex] = item;
            else
                PlaceInFirstEmpty(item);

            _equipped[slot] = null;
            Save();
            OnChanged?.Invoke();
        }

        /// <summary>인벤토리 내 두 슬롯의 아이템을 교환합니다. UI 드래그&드롭 스왑 시 호출됩니다.</summary>
        public void SwapItems(int indexA, int indexB)
        {
            if (indexA < 0 || indexB < 0 || indexA >= _slots.Length || indexB >= _slots.Length) return;
            (_slots[indexA], _slots[indexB]) = (_slots[indexB], _slots[indexA]);
            Save();
        }

        /// <summary>슬롯에 장착된 아이템을 반환합니다. 없으면 null.</summary>
        public ItemData GetEquipped(ItemType slot)
        {
            return _equipped.TryGetValue(slot, out ItemData item) ? item : null;
        }

        /// <summary>비어있지 않은 아이템을 앞으로 정렬합니다. 패널을 열 때만 호출합니다.</summary>
        public void CompactItems()
        {
            int write = 0;
            for (int i = 0; i < _slots.Length; i++)
                if (_slots[i] != null) _slots[write++] = _slots[i];
            for (int i = write; i < _slots.Length; i++)
                _slots[i] = null;
            Save();
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
            // 인벤토리 슬롯 (위치 기반 고정 크기)
            for (int i = 0; i < MaxInventorySize; i++)
                PlayerPrefs.SetString($"inv_{i}", _slots[i]?.name ?? "");

            // 장착 슬롯
            foreach (ItemType slotType in SlotTypes)
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

            Array.Clear(_slots, 0, _slots.Length);
            _equipped.Clear();

            // 인벤토리 슬롯 (위치 기반 복원)
            for (int i = 0; i < MaxInventorySize; i++)
            {
                string itemName = PlayerPrefs.GetString($"inv_{i}", "");
                if (string.IsNullOrEmpty(itemName)) continue;

                ItemData item = _database.FindByName(itemName);
                if (item != null)
                    _slots[i] = item;
                else
                    Debug.LogWarning($"[PlayerInventory] Load: '{itemName}' 아이템을 ItemDatabase에서 찾을 수 없음 — ItemDatabase.allItems에 해당 SO 추가 필요");
            }

            // 장착 슬롯
            foreach (ItemType slotType in SlotTypes)
            {
                string itemName   = PlayerPrefs.GetString($"equip_{slotType}", "");
                _equipped[slotType] = _database.FindByName(itemName);
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
                    PlaceInFirstEmpty(weapon);
                    _equipped[ItemType.Weapon] = null;
                }
            }

            OnChanged?.Invoke();
        }

        // ──────────────────────────────────────────
        // Private Methods
        // ──────────────────────────────────────────

        private bool PlaceInFirstEmpty(ItemData item)
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] == null) { _slots[i] = item; return true; }
            }
            return false;
        }
    }
}

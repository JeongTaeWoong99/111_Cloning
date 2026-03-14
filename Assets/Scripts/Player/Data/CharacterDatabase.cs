using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Character Database")]
public class CharacterDatabase : ScriptableObject
{
    public List<CharacterData> characters;

    // SO 로드·플레이 모드 진입 시 자동 빌드되는 이름→캐릭터 Dictionary
    private Dictionary<string, CharacterData> _nameCache;

    private void OnEnable()
    {
        _nameCache = new Dictionary<string, CharacterData>(characters.Count);
        foreach (CharacterData ch in characters)
        {
            if (ch != null)
                _nameCache[ch.name] = ch;
        }
    }

    /// <summary>에셋명으로 CharacterData를 O(1)로 검색합니다.</summary>
    public CharacterData FindByName(string n)
        => _nameCache.TryGetValue(n, out CharacterData ch) ? ch : null;
}

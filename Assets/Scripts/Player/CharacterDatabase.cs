using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Character Database")]
public class CharacterDatabase : ScriptableObject
{
    public List<CharacterData> characters;

    public CharacterData FindByName(string n) => characters.Find(x => x.name == n);
}

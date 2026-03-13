using System;
using Inventory;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Tools > 게임 데이터 초기화 에디터 윈도우.
/// Play 진입 없이 PlayerPrefs 데이터를 초기화합니다.
/// </summary>
public class GameDataResetWindow : EditorWindow
{
    [MenuItem("Tools/게임 데이터 초기화")]
    private static void Open() => GetWindow<GameDataResetWindow>("게임 데이터 초기화");

    private void OnGUI()
    {
        GUILayout.Label("플레이어 데이터 초기화", EditorStyles.boldLabel);
        GUILayout.Space(8);

        if (GUILayout.Button("보유 아이템 초기화"))
        {
            ResetInventory();
        }
    }

    private void ResetInventory()
    {
        int count = PlayerPrefs.GetInt("inv_count", 0);
        for (int i = 0; i < count; i++)
        {
            PlayerPrefs.DeleteKey($"inv_{i}");
        }
        PlayerPrefs.DeleteKey("inv_count");

        foreach (ItemType slot in Enum.GetValues(typeof(ItemType)))
        {
            PlayerPrefs.DeleteKey($"equip_{slot}");
        }

        PlayerPrefs.Save();
        Debug.Log("[GameDataReset] 보유 아이템 초기화 완료");
    }
}

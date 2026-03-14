using System;
using Inventory;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Tools > 게임 데이터 초기화 에디터 윈도우.
/// Play 진입 없이 PlayerPrefs 데이터 초기화 및 DB 자동 수집을 수행합니다.
/// </summary>
public class GameDataResetWindow : EditorWindow
{
    [MenuItem("Tools/게임 데이터 초기화")]
    private static void Open() => GetWindow<GameDataResetWindow>("게임 데이터 초기화");

    private void OnGUI()
    {
        // ── 플레이어 데이터 초기화 ──────────────────────
        GUILayout.Label("플레이어 데이터 초기화", EditorStyles.boldLabel);
        GUILayout.Space(4);

        if (GUILayout.Button("보유 아이템 초기화"))
        {
            ResetInventory();
        }

        GUILayout.Space(16);

        // ── 데이터베이스 관리 ───────────────────────────
        GUILayout.Label("데이터베이스 관리", EditorStyles.boldLabel);
        GUILayout.Space(4);

        if (GUILayout.Button("모든 ItemData 자동 수집"))
        {
            CollectAllItemData();
        }
    }

    // ──────────────────────────────────────────
    // Private Methods
    // ──────────────────────────────────────────

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

    private void CollectAllItemData()
    {
        // ItemDatabase SO 탐색
        string[] dbGuids = AssetDatabase.FindAssets("t:ItemDatabase");
        if (dbGuids.Length == 0)
        {
            Debug.LogWarning("[GameDataReset] ItemDatabase SO를 찾을 수 없습니다.");
            return;
        }

        string dbPath     = AssetDatabase.GUIDToAssetPath(dbGuids[0]);
        ItemDatabase db   = AssetDatabase.LoadAssetAtPath<ItemDatabase>(dbPath);

        // 프로젝트 내 모든 ItemData 수집
        string[] itemGuids      = AssetDatabase.FindAssets("t:ItemData");
        db.allItems             = new System.Collections.Generic.List<ItemData>();

        foreach (string guid in itemGuids)
        {
            string path  = AssetDatabase.GUIDToAssetPath(guid);
            ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
            if (item != null)
                db.allItems.Add(item);
        }

        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();

        Debug.Log($"[GameDataReset] ItemData 자동 수집 완료 — {db.allItems.Count}개 등록 ({dbPath})");
    }
}

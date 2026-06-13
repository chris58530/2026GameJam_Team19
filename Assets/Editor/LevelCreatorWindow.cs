using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// 一鍵關卡建立工具。
/// 
/// 開啟方式：Unity 頂部選單 → Tools → Level Creator
/// 
/// 用法：
///   1. 把隊友給的場景檔案（.unity）拖入欄位
///   2. 填入關卡名稱（或用預設）
///   3. 按「Create Level from Scene」
///   4. 工具會自動：
///      - 開啟該場景
///      - 把場景內容打包成 Prefab
///      - 建立 LevelDefinition asset
///      - 加入 LevelDatabase
///      - 完成！
/// </summary>
public class LevelCreatorWindow : EditorWindow
{
    private SceneAsset sourceScene;
    private string levelDisplayName = "";
    private string levelId = "";
    private string difficulty = "Normal";
    private int sortOrder = 0;
    private bool autoAddToDatabase = true;

    // 排除的物件名稱（這些不會被包進 Level Prefab）
    private static readonly HashSet<string> ExcludedNames = new HashSet<string>
    {
        "SceneLoadManager", "GameFlowManager", "EventSystem",
        "PauseMenuCanvas", "Canvas" // 只排除系統用的 Canvas
    };

    [MenuItem("Tools/Level Creator")]
    public static void ShowWindow()
    {
        var window = GetWindow<LevelCreatorWindow>("Level Creator");
        window.minSize = new Vector2(400, 350);
    }

    private void OnGUI()
    {
        GUILayout.Space(10);
        EditorGUILayout.LabelField("一鍵建立關卡", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "把隊友做好的場景拖入下方，按一個按鈕即可自動轉換成 Level Prefab 並加入系統。",
            MessageType.Info);

        GUILayout.Space(15);

        // Source Scene
        EditorGUILayout.LabelField("來源場景", EditorStyles.boldLabel);
        sourceScene = (SceneAsset)EditorGUILayout.ObjectField(
            "場景檔案 (.unity)", sourceScene, typeof(SceneAsset), false);

        GUILayout.Space(10);

        // Auto-fill name from scene
        if (sourceScene != null && string.IsNullOrEmpty(levelDisplayName))
        {
            levelDisplayName = sourceScene.name;
            levelId = sourceScene.name.ToLower().Replace(" ", "_");
        }

        // Level info
        EditorGUILayout.LabelField("關卡資訊", EditorStyles.boldLabel);
        levelDisplayName = EditorGUILayout.TextField("顯示名稱", levelDisplayName);
        levelId = EditorGUILayout.TextField("關卡 ID", levelId);
        difficulty = EditorGUILayout.TextField("難度", difficulty);
        sortOrder = EditorGUILayout.IntField("排序順序", sortOrder);
        autoAddToDatabase = EditorGUILayout.Toggle("自動加入 LevelDatabase", autoAddToDatabase);

        GUILayout.Space(20);

        // Create button
        GUI.enabled = sourceScene != null;
        GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
        if (GUILayout.Button("建立關卡 (Create Level from Scene)", GUILayout.Height(40)))
        {
            CreateLevelFromScene();
        }
        GUI.backgroundColor = Color.white;
        GUI.enabled = true;

        GUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "排除的物件（不會包進 Prefab）：\n" +
            "SceneLoadManager, GameFlowManager, EventSystem, PauseMenuCanvas",
            MessageType.None);
    }

    private void CreateLevelFromScene()
    {
        if (sourceScene == null)
        {
            EditorUtility.DisplayDialog("錯誤", "請先拖入場景檔案！", "OK");
            return;
        }

        // Save current scene
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

        string scenePath = AssetDatabase.GetAssetPath(sourceScene);
        string safeName = string.IsNullOrEmpty(levelId) ? sourceScene.name : levelId;
        string prefabName = "Level_" + safeName;

        // Open the source scene
        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        // Collect all root objects (excluding system objects)
        Scene scene = SceneManager.GetActiveScene();
        GameObject[] rootObjects = scene.GetRootGameObjects();

        // Create level root
        GameObject levelRoot = new GameObject(prefabName);

        int copiedCount = 0;
        foreach (var obj in rootObjects)
        {
            // Skip excluded objects
            if (IsExcluded(obj.name))
                continue;

            // Duplicate and parent
            GameObject copy = Instantiate(obj);
            copy.name = obj.name; // Remove (Clone)
            copy.transform.SetParent(levelRoot.transform, true);
            copiedCount++;
        }

        if (copiedCount == 0)
        {
            DestroyImmediate(levelRoot);
            EditorUtility.DisplayDialog("錯誤", "場景中沒有找到可用的遊戲物件！", "OK");
            return;
        }

        // Ensure folders exist
        EnsureFolder("Assets/Prefabs/Levels");
        EnsureFolder("Assets/ScriptableObjects/Levels");

        // Save Prefab
        string prefabPath = $"Assets/Prefabs/Levels/{prefabName}.prefab";
        // If exists, add number
        if (File.Exists(prefabPath))
        {
            if (!EditorUtility.DisplayDialog("覆蓋確認",
                $"Prefab 已存在：{prefabPath}\n要覆蓋嗎？", "覆蓋", "取消"))
            {
                DestroyImmediate(levelRoot);
                return;
            }
        }

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(levelRoot, prefabPath);
        DestroyImmediate(levelRoot);

        // Create LevelDefinition
        string defPath = $"Assets/ScriptableObjects/Levels/{prefabName}.asset";
        LevelDefinition levelDef = AssetDatabase.LoadAssetAtPath<LevelDefinition>(defPath);

        if (levelDef == null)
        {
            levelDef = ScriptableObject.CreateInstance<LevelDefinition>();
            AssetDatabase.CreateAsset(levelDef, defPath);
        }

        levelDef.levelId = safeName;
        levelDef.displayName = string.IsNullOrEmpty(levelDisplayName) ? sourceScene.name : levelDisplayName;
        levelDef.levelPrefab = prefab;
        levelDef.difficulty = difficulty;
        levelDef.sortOrder = sortOrder;
        levelDef.unlockedByDefault = true;
        EditorUtility.SetDirty(levelDef);

        // Add to LevelDatabase
        if (autoAddToDatabase)
        {
            AddToDatabase(levelDef);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Done!
        EditorUtility.DisplayDialog("完成！",
            $"關卡建立成功！\n\n" +
            $"Prefab: {prefabPath}\n" +
            $"Definition: {defPath}\n" +
            $"包含 {copiedCount} 個物件",
            "OK");

        // Reset fields
        sourceScene = null;
        levelDisplayName = "";
        levelId = "";

        // Ping the created asset
        EditorGUIUtility.PingObject(levelDef);
    }

    private bool IsExcluded(string objName)
    {
        foreach (var excluded in ExcludedNames)
        {
            if (objName.Contains(excluded))
                return true;
        }
        return false;
    }

    private void AddToDatabase(LevelDefinition levelDef)
    {
        // Find LevelDatabase asset
        string[] guids = AssetDatabase.FindAssets("t:LevelDatabase");
        if (guids.Length == 0)
        {
            Debug.LogWarning("[LevelCreator] 找不到 LevelDatabase！請手動加入。");
            return;
        }

        string dbPath = AssetDatabase.GUIDToAssetPath(guids[0]);
        LevelDatabase db = AssetDatabase.LoadAssetAtPath<LevelDatabase>(dbPath);

        if (db == null) return;

        // Check if already exists
        for (int i = 0; i < db.levels.Count; i++)
        {
            if (db.levels[i] != null && db.levels[i].levelId == levelDef.levelId)
            {
                db.levels[i] = levelDef; // Update existing
                EditorUtility.SetDirty(db);
                Debug.Log($"[LevelCreator] 更新 LevelDatabase 中的 {levelDef.levelId}");
                return;
            }
        }

        // Add new
        db.levels.Add(levelDef);
        EditorUtility.SetDirty(db);
        Debug.Log($"[LevelCreator] 已將 {levelDef.levelId} 加入 LevelDatabase");
    }

    private void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;

        string[] parts = path.Split('/');
        string current = parts[0]; // "Assets"
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}

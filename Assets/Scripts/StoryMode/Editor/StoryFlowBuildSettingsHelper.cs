#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// 編輯器工具：自動將 Story Mode 所需場景加入 Build Settings。
/// 不會移除已存在的場景，只會在缺少時新增。
/// 
/// 使用方式：
///   Unity 選單 → Tools → Story Mode → Add Scenes to Build Settings
/// </summary>
public static class StoryFlowBuildSettingsHelper
{
    [MenuItem("Tools/Story Mode/Add Scenes to Build Settings")]
    public static void AddStoryScenesToBuildSettings()
    {
        // 需要的場景名稱列表
        string[] requiredScenes = new string[]
        {
            "TitleMenu",
            "OpeningAnimation",
            "Level01",
            "Level02",
            "Level03",
            "Ending"
        };

        // 取得現有的 Build Settings 場景
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        HashSet<string> existingPaths = new HashSet<string>();

        foreach (var scene in scenes)
        {
            existingPaths.Add(scene.path);
        }

        int addedCount = 0;

        foreach (string sceneName in requiredScenes)
        {
            // 搜尋場景檔案
            string[] guids = AssetDatabase.FindAssets($"t:Scene {sceneName}");
            string scenePath = null;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = Path.GetFileNameWithoutExtension(path);

                if (fileName == sceneName)
                {
                    scenePath = path;
                    break;
                }
            }

            if (string.IsNullOrEmpty(scenePath))
            {
                Debug.LogWarning($"[StoryFlowBuildSettings] 找不到場景: {sceneName}。請先建立此場景（Assets/Scenes/{sceneName}.unity）。");
                continue;
            }

            if (existingPaths.Contains(scenePath))
            {
                Debug.Log($"[StoryFlowBuildSettings] 場景已存在於 Build Settings: {sceneName}");
                continue;
            }

            // 新增到 Build Settings
            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            existingPaths.Add(scenePath);
            addedCount++;
            Debug.Log($"[StoryFlowBuildSettings] 已新增場景到 Build Settings: {sceneName} ({scenePath})");
        }

        // 套用變更
        EditorBuildSettings.scenes = scenes.ToArray();

        if (addedCount > 0)
            Debug.Log($"[StoryFlowBuildSettings] 完成！新增了 {addedCount} 個場景。");
        else
            Debug.Log("[StoryFlowBuildSettings] 所有場景已在 Build Settings 中。");
    }

    [MenuItem("Tools/Story Mode/Check Scene Status")]
    public static void CheckSceneStatus()
    {
        string[] requiredScenes = new string[]
        {
            "TitleMenu",
            "OpeningAnimation",
            "Level01",
            "Level02",
            "Level03",
            "Ending"
        };

        Debug.Log("===== Story Mode 場景狀態檢查 =====");

        foreach (string sceneName in requiredScenes)
        {
            string[] guids = AssetDatabase.FindAssets($"t:Scene {sceneName}");
            bool found = false;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = Path.GetFileNameWithoutExtension(path);
                if (fileName == sceneName)
                {
                    found = true;
                    // 檢查是否在 Build Settings 中
                    bool inBuild = false;
                    foreach (var buildScene in EditorBuildSettings.scenes)
                    {
                        if (buildScene.path == path)
                        {
                            inBuild = true;
                            break;
                        }
                    }
                    Debug.Log($"  {(inBuild ? "✓" : "✗")} {sceneName} → {path} {(inBuild ? "(已在 Build Settings)" : "(未加入 Build Settings)")}");
                    break;
                }
            }

            if (!found)
            {
                Debug.LogWarning($"  ✗ {sceneName} → 場景檔案不存在！需要建立。");
            }
        }

        Debug.Log("===================================");
    }
}
#endif

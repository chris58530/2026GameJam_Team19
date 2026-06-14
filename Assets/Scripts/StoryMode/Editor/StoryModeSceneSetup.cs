#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// 一鍵建立 Story Mode 所需的所有場景，並正確掛上腳本與元件。
/// 
/// 使用方式：
///   Unity 選單 → Tools → Story Mode → Setup All Scenes
/// 
/// 此工具會：
///   1. 建立 TitleMenu 場景（含 Canvas、Animator、Start 按鈕、StoryFlowManager）
///   2. 建立 OpeningAnimation 場景（含 Canvas、Animator、OpeningAnimationController）
///   3. 建立 Ending 場景（含 Canvas、Animator、EndingUI、按鈕面板）
///   4. 在 Game0/Game1/Game2 中加入 LevelManager（如果還沒有）
///   5. 將所有場景加入 Build Settings
/// 
/// 注意：如果場景已存在，不會覆蓋，只會跳過。
/// </summary>
public static class StoryModeSceneSetup
{
    private const string ScenesFolder = "Assets/Scenes";

    [MenuItem("Tools/Story Mode/Setup All Scenes (一鍵建立全部)", priority = 0)]
    public static void SetupAllScenes()
    {
        // 先儲存當前場景
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

        bool created = false;

        // 1. TitleMenu
        if (!SceneExists("TitleMenu"))
        {
            CreateTitleMenuScene();
            created = true;
        }
        else
        {
            Debug.Log("[StoryModeSetup] TitleMenu 場景已存在，跳過。");
        }

        // 2. OpeningAnimation
        if (!SceneExists("OpeningAnimation"))
        {
            CreateOpeningAnimationScene();
            created = true;
        }
        else
        {
            Debug.Log("[StoryModeSetup] OpeningAnimation 場景已存在，跳過。");
        }

        // 3. Ending
        if (!SceneExists("Ending"))
        {
            CreateEndingScene();
            created = true;
        }
        else
        {
            Debug.Log("[StoryModeSetup] Ending 場景已存在，跳過。");
        }

        // 4. 在 Game0/1/2 加入 LevelManager
        AddLevelManagerToExistingScenes();

        // 5. 更新 Build Settings
        UpdateBuildSettings();

        AssetDatabase.Refresh();

        if (created)
            Debug.Log("[StoryModeSetup] ✓ 場景建立完成！請回到 Unity Editor 確認。");
        else
            Debug.Log("[StoryModeSetup] ✓ 所有場景已存在，已確認 Build Settings 和 LevelManager。");
    }

    [MenuItem("Tools/Story Mode/1. Create TitleMenu Scene")]
    public static void CreateTitleMenuScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // StoryFlowManager（DontDestroyOnLoad）
        var flowMgr = new GameObject("StoryFlowManager");
        var sfm = flowMgr.AddComponent<StoryFlowManager>();
        // 設定預設值 — levelScenes 會使用 Game0, Game1, Game2
        var serializedObj = new SerializedObject(sfm);
        serializedObj.FindProperty("titleMenuScene").stringValue = "TitleMenu";
        serializedObj.FindProperty("openingAnimationScene").stringValue = "OpeningAnimation";

        var levelScenesProperty = serializedObj.FindProperty("levelScenes");
        levelScenesProperty.arraySize = 3;
        levelScenesProperty.GetArrayElementAtIndex(0).stringValue = "Game0";
        levelScenesProperty.GetArrayElementAtIndex(1).stringValue = "Game1";
        levelScenesProperty.GetArrayElementAtIndex(2).stringValue = "Game2";

        serializedObj.FindProperty("endingScene").stringValue = "Ending";
        serializedObj.ApplyModifiedProperties();

        // Canvas
        var canvasObj = CreateCanvas("TitleCanvas");

        // TitleMenuUI
        canvasObj.AddComponent<TitleMenuUI>();

        // Animator（讓你之後可以加標題動畫）
        var animator = canvasObj.AddComponent<Animator>();
        // 提示：建立 Animator Controller 後拖入此處

        // TitleAnimationHolder（放標題動畫用的全螢幕面板）
        var animHolder = CreateFullScreenPanel(canvasObj.transform, "TitleAnimationHolder");
        animHolder.AddComponent<CanvasGroup>(); // 用於 fade 動畫

        // Start 按鈕
        var startBtn = CreateButton(canvasObj.transform, "StartButton", "START", 
            new Vector2(0.5f, 0.3f), new Vector2(0.5f, 0.3f), new Vector2(300, 80));

        // 連接按鈕到 TitleMenuUI
        var titleMenuUI = canvasObj.GetComponent<TitleMenuUI>();
        var serializedUI = new SerializedObject(titleMenuUI);
        serializedUI.FindProperty("startButton").objectReferenceValue = startBtn.GetComponent<Button>();
        serializedUI.ApplyModifiedProperties();

        // EventSystem
        CreateEventSystem();

        // 儲存場景
        string path = $"{ScenesFolder}/TitleMenu.unity";
        EnsureDirectoryExists(path);
        EditorSceneManager.SaveScene(scene, path);
        Debug.Log($"[StoryModeSetup] ✓ 已建立 TitleMenu 場景: {path}");
    }

    [MenuItem("Tools/Story Mode/2. Create OpeningAnimation Scene")]
    public static void CreateOpeningAnimationScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // OpeningAnimationPlayer（掛 Controller + Animator）
        var playerObj = new GameObject("OpeningAnimationPlayer");
        var controller = playerObj.AddComponent<OpeningAnimationController>();
        var animator = playerObj.AddComponent<Animator>(); // Animator for animation clips

        // 設定預設值
        var serializedCtrl = new SerializedObject(controller);
        serializedCtrl.FindProperty("useAutoTimer").boolValue = true;
        serializedCtrl.FindProperty("autoTimerDuration").floatValue = 3f;
        serializedCtrl.FindProperty("allowSkip").boolValue = true;
        serializedCtrl.FindProperty("skipMinWait").floatValue = 0.5f;
        serializedCtrl.ApplyModifiedProperties();

        // Canvas（用於顯示動畫內容如圖片序列、文字等）
        var canvasObj = CreateCanvas("AnimationCanvas");
        var canvasAnimator = canvasObj.AddComponent<Animator>(); // Canvas 自己也有 Animator，可做 UI 動畫

        // 全螢幕動畫面板（放動畫圖片/影片用）
        var animPanel = CreateFullScreenPanel(canvasObj.transform, "AnimationPanel");
        animPanel.AddComponent<CanvasGroup>(); // 用於 fade in/out
        var panelImage = animPanel.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 1); // 黑色底

        // 提示文字
        var skipText = CreateTextObject(canvasObj.transform, "SkipHintText",
            "Press any key to skip...",
            new Vector2(0.5f, 0.05f), new Vector2(0.5f, 0.05f), new Vector2(400, 40));

        // EventSystem
        CreateEventSystem();

        // 儲存
        string path = $"{ScenesFolder}/OpeningAnimation.unity";
        EnsureDirectoryExists(path);
        EditorSceneManager.SaveScene(scene, path);
        Debug.Log($"[StoryModeSetup] ✓ 已建立 OpeningAnimation 場景: {path}");
    }

    [MenuItem("Tools/Story Mode/3. Create Ending Scene")]
    public static void CreateEndingScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // Canvas
        var canvasObj = CreateCanvas("EndingCanvas");

        // EndingUI
        var endingUI = canvasObj.AddComponent<EndingUI>();

        // Victory 動畫物件
        var victoryObj = CreateFullScreenPanel(canvasObj.transform, "VictoryAnimObject");
        victoryObj.AddComponent<Animator>(); // Animator for victory animation
        victoryObj.AddComponent<CanvasGroup>();
        var victoryImage = victoryObj.AddComponent<Image>();
        victoryImage.color = new Color(0.1f, 0.4f, 0.1f, 1f); // 綠色底
        victoryObj.SetActive(false);

        // Victory 文字
        var victoryText = CreateTextObject(victoryObj.transform, "VictoryText", "VICTORY!",
            new Vector2(0.5f, 0.6f), new Vector2(0.5f, 0.6f), new Vector2(600, 120));

        // Fail 動畫物件
        var failObj = CreateFullScreenPanel(canvasObj.transform, "FailAnimObject");
        failObj.AddComponent<Animator>(); // Animator for fail animation
        failObj.AddComponent<CanvasGroup>();
        var failImage = failObj.AddComponent<Image>();
        failImage.color = new Color(0.4f, 0.1f, 0.1f, 1f); // 紅色底
        failObj.SetActive(false);

        // Fail 文字
        var failText = CreateTextObject(failObj.transform, "FailText", "GAME OVER",
            new Vector2(0.5f, 0.6f), new Vector2(0.5f, 0.6f), new Vector2(600, 120));

        // Buttons Panel
        var buttonsPanel = new GameObject("ButtonsPanel");
        buttonsPanel.transform.SetParent(canvasObj.transform, false);
        var buttonsPanelRect = buttonsPanel.AddComponent<RectTransform>();
        buttonsPanelRect.anchorMin = new Vector2(0.5f, 0.1f);
        buttonsPanelRect.anchorMax = new Vector2(0.5f, 0.4f);
        buttonsPanelRect.anchoredPosition = Vector2.zero;
        buttonsPanelRect.sizeDelta = new Vector2(400, 0);

        // Vertical Layout
        var vlg = buttonsPanel.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 15;
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childControlHeight = false;
        vlg.childControlWidth = false;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = false;

        buttonsPanel.SetActive(false); // 預設隱藏，動畫後顯示

        // 按鈕
        var retryBtn = CreateButton(buttonsPanel.transform, "RetryButton", "RETRY", 
            Vector2.zero, Vector2.zero, new Vector2(280, 60));
        var backBtn = CreateButton(buttonsPanel.transform, "BackToTitleButton", "BACK TO TITLE",
            Vector2.zero, Vector2.zero, new Vector2(280, 60));
        var quitBtn = CreateButton(buttonsPanel.transform, "QuitButton", "QUIT",
            Vector2.zero, Vector2.zero, new Vector2(280, 60));

        // 連接 EndingUI references
        var serializedEndingUI = new SerializedObject(endingUI);
        serializedEndingUI.FindProperty("victoryAnimObject").objectReferenceValue = victoryObj;
        serializedEndingUI.FindProperty("failAnimObject").objectReferenceValue = failObj;
        serializedEndingUI.FindProperty("buttonsPanel").objectReferenceValue = buttonsPanel;
        serializedEndingUI.FindProperty("retryButton").objectReferenceValue = retryBtn.GetComponent<Button>();
        serializedEndingUI.FindProperty("backToTitleButton").objectReferenceValue = backBtn.GetComponent<Button>();
        serializedEndingUI.FindProperty("quitButton").objectReferenceValue = quitBtn.GetComponent<Button>();
        serializedEndingUI.FindProperty("autoShowButtonsDelay").floatValue = 3f;
        serializedEndingUI.FindProperty("useAutoShowButtons").boolValue = true;
        serializedEndingUI.FindProperty("victoryMessage").stringValue = "Victory!";
        serializedEndingUI.FindProperty("failMessage").stringValue = "Game Over";
        serializedEndingUI.ApplyModifiedProperties();

        // EventSystem
        CreateEventSystem();

        // 儲存
        string path = $"{ScenesFolder}/Ending.unity";
        EnsureDirectoryExists(path);
        EditorSceneManager.SaveScene(scene, path);
        Debug.Log($"[StoryModeSetup] ✓ 已建立 Ending 場景: {path}");
    }

    [MenuItem("Tools/Story Mode/4. Add LevelManager to Game0-1-2")]
    public static void AddLevelManagerToExistingScenes()
    {
        string[] levelScenes = { "Game0", "Game1", "Game2" };

        foreach (string sceneName in levelScenes)
        {
            string scenePath = $"{ScenesFolder}/{sceneName}.unity";

            if (!File.Exists(scenePath))
            {
                Debug.LogWarning($"[StoryModeSetup] 找不到場景: {scenePath}，跳過。");
                continue;
            }

            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            // 檢查是否已有 LevelManager
            var existing = Object.FindFirstObjectByType<LevelManager>();
            if (existing != null)
            {
                Debug.Log($"[StoryModeSetup] {sceneName} 已有 LevelManager，跳過。");
                continue;
            }

            // 建立 LevelManager
            var lmObj = new GameObject("LevelManager");
            var lm = lmObj.AddComponent<LevelManager>();

            // 設定延遲
            var serializedLM = new SerializedObject(lm);
            serializedLM.FindProperty("clearDelay").floatValue = 2.5f;
            serializedLM.FindProperty("failDelay").floatValue = 2f;
            serializedLM.ApplyModifiedProperties();

            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[StoryModeSetup] ✓ 已在 {sceneName} 中加入 LevelManager。");
        }
    }

    private static void UpdateBuildSettings()
    {
        string[] requiredScenes = {
            "TitleMenu", "OpeningAnimation", "Game0", "Game1", "Game2", "Ending",
            "LoadingScene" // 保留 Loading 場景支持
        };

        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        HashSet<string> existingPaths = new HashSet<string>();
        foreach (var s in scenes)
            existingPaths.Add(s.path);

        int addedCount = 0;
        foreach (string sceneName in requiredScenes)
        {
            string path = $"{ScenesFolder}/{sceneName}.unity";
            if (!File.Exists(path)) continue;
            if (existingPaths.Contains(path)) continue;

            scenes.Add(new EditorBuildSettingsScene(path, true));
            existingPaths.Add(path);
            addedCount++;
        }

        if (addedCount > 0)
        {
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log($"[StoryModeSetup] ✓ 已新增 {addedCount} 個場景到 Build Settings。");
        }
        else
        {
            Debug.Log("[StoryModeSetup] Build Settings 已包含所有需要的場景。");
        }
    }

    // ========== Helper Methods ==========

    private static bool SceneExists(string sceneName)
    {
        return File.Exists($"{ScenesFolder}/{sceneName}.unity");
    }

    private static void EnsureDirectoryExists(string path)
    {
        string dir = Path.GetDirectoryName(path);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
    }

    private static GameObject CreateCanvas(string name)
    {
        var canvasObj = new GameObject(name);
        canvasObj.layer = LayerMask.NameToLayer("UI");

        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        return canvasObj;
    }

    private static GameObject CreateFullScreenPanel(Transform parent, string name)
    {
        var panel = new GameObject(name);
        panel.layer = LayerMask.NameToLayer("UI");
        panel.transform.SetParent(parent, false);

        var rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;

        return panel;
    }

    private static GameObject CreateButton(Transform parent, string name, string text,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 size)
    {
        var btnObj = new GameObject(name);
        btnObj.layer = LayerMask.NameToLayer("UI");
        btnObj.transform.SetParent(parent, false);

        var rect = btnObj.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;

        var image = btnObj.AddComponent<Image>();
        image.color = new Color(0.25f, 0.25f, 0.35f, 1f);

        var button = btnObj.AddComponent<Button>();
        button.targetGraphic = image;

        // 按鈕文字
        var textObj = new GameObject("Text");
        textObj.layer = LayerMask.NameToLayer("UI");
        textObj.transform.SetParent(btnObj.transform, false);

        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.anchoredPosition = Vector2.zero;
        textRect.sizeDelta = Vector2.zero;

        var textComp = textObj.AddComponent<UnityEngine.UI.Text>();
        textComp.text = text;
        textComp.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        textComp.fontSize = 32;
        textComp.alignment = TextAnchor.MiddleCenter;
        textComp.color = Color.white;

        return btnObj;
    }

    private static GameObject CreateTextObject(Transform parent, string name, string text,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 size)
    {
        var textObj = new GameObject(name);
        textObj.layer = LayerMask.NameToLayer("UI");
        textObj.transform.SetParent(parent, false);

        var rect = textObj.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;

        var textComp = textObj.AddComponent<UnityEngine.UI.Text>();
        textComp.text = text;
        textComp.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        textComp.fontSize = 48;
        textComp.alignment = TextAnchor.MiddleCenter;
        textComp.color = Color.white;

        return textObj;
    }

    private static void CreateEventSystem()
    {
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var esObj = new GameObject("EventSystem");
            esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
    }
}
#endif

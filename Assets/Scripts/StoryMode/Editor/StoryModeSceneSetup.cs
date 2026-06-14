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

        // 背景影片播放器
        var bgVideoObj = new GameObject("BackgroundVideoPlayer");
        var bgVideoPlayer = bgVideoObj.AddComponent<UnityEngine.Video.VideoPlayer>();
        bgVideoPlayer.playOnAwake = false;
        bgVideoPlayer.isLooping = true;
        bgVideoPlayer.renderMode = UnityEngine.Video.VideoRenderMode.CameraFarPlane;
        // Target Camera 需要手動拖入 Main Camera

        // Canvas
        var canvasObj = CreateCanvas("TitleCanvas");

        // TitleMenuUI
        var titleUI = canvasObj.AddComponent<TitleMenuUI>();

        // Start 按鈕
        var startBtn = CreateButton(canvasObj.transform, "StartButton", "START", 
            new Vector2(0.5f, 0.3f), new Vector2(0.5f, 0.3f), new Vector2(300, 80));

        // 連接
        var serializedUI = new SerializedObject(titleUI);
        serializedUI.FindProperty("startButton").objectReferenceValue = startBtn.GetComponent<Button>();
        serializedUI.FindProperty("backgroundVideo").objectReferenceValue = bgVideoPlayer;
        serializedUI.ApplyModifiedProperties();

        // EventSystem
        CreateEventSystem();

        // 儲存場景
        string path = $"{ScenesFolder}/TitleMenu.unity";
        EnsureDirectoryExists(path);
        EditorSceneManager.SaveScene(scene, path);
        Debug.Log($"[StoryModeSetup] ✓ 已建立 TitleMenu 場景: {path}");
        Debug.Log("[StoryModeSetup] → 記得在 BackgroundVideoPlayer 的 Inspector 中：拖入影片 Clip + 設定 Target Camera = Main Camera");
    }

    [MenuItem("Tools/Story Mode/2. Create OpeningAnimation Scene")]
    public static void CreateOpeningAnimationScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // OpeningAnimationPlayer（掛 Controller + VideoPlayer）
        var playerObj = new GameObject("OpeningAnimationPlayer");
        var controller = playerObj.AddComponent<OpeningAnimationController>();
        var videoPlayer = playerObj.GetComponent<UnityEngine.Video.VideoPlayer>();
        // VideoPlayer 由 [RequireComponent] 自動加上
        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.renderMode = UnityEngine.Video.VideoRenderMode.CameraFarPlane;
        // Target Camera 需手動設定 Main Camera

        // 設定預設值
        var serializedCtrl = new SerializedObject(controller);
        serializedCtrl.FindProperty("allowSkip").boolValue = true;
        serializedCtrl.FindProperty("skipMinWait").floatValue = 1f;
        serializedCtrl.FindProperty("fallbackDuration").floatValue = 3f;
        serializedCtrl.ApplyModifiedProperties();

        // Canvas（顯示影片用的 RawImage + 跳過提示）
        var canvasObj = CreateCanvas("UICanvas");

        // 全螢幕 RawImage（用來顯示影片）
        var rawImageObj = CreateFullScreenPanel(canvasObj.transform, "VideoDisplay");
        var rawImage = rawImageObj.AddComponent<RawImage>();
        rawImage.color = Color.white;

        // 連接 RawImage 到 Controller
        var serializedCtrl2 = new SerializedObject(controller);
        serializedCtrl2.FindProperty("displayRawImage").objectReferenceValue = rawImage;
        serializedCtrl2.ApplyModifiedProperties();

        // 跳過提示文字
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
        Debug.Log("[StoryModeSetup] → 記得在 OpeningAnimationPlayer 的 Inspector 中：拖入影片 Clip + 設定 Target Camera = Main Camera");
    }

    [MenuItem("Tools/Story Mode/3. Create Ending Scene")]
    public static void CreateEndingScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // Victory 影片播放器
        var victoryVideoObj = new GameObject("VictoryVideoPlayer");
        var victoryVP = victoryVideoObj.AddComponent<UnityEngine.Video.VideoPlayer>();
        victoryVP.playOnAwake = false;
        victoryVP.isLooping = false;
        victoryVP.renderMode = UnityEngine.Video.VideoRenderMode.CameraFarPlane;
        victoryVideoObj.SetActive(false);

        // Fail 影片播放器
        var failVideoObj = new GameObject("FailVideoPlayer");
        var failVP = failVideoObj.AddComponent<UnityEngine.Video.VideoPlayer>();
        failVP.playOnAwake = false;
        failVP.isLooping = false;
        failVP.renderMode = UnityEngine.Video.VideoRenderMode.CameraFarPlane;
        failVideoObj.SetActive(false);

        // Canvas
        var canvasObj = CreateCanvas("EndingCanvas");

        // EndingUI
        var endingUI = canvasObj.AddComponent<EndingUI>();

        // Buttons Panel
        var buttonsPanel = new GameObject("ButtonsPanel");
        buttonsPanel.transform.SetParent(canvasObj.transform, false);
        buttonsPanel.layer = LayerMask.NameToLayer("UI");
        var buttonsPanelRect = buttonsPanel.AddComponent<RectTransform>();
        buttonsPanelRect.anchorMin = new Vector2(0.5f, 0.1f);
        buttonsPanelRect.anchorMax = new Vector2(0.5f, 0.4f);
        buttonsPanelRect.anchoredPosition = Vector2.zero;
        buttonsPanelRect.sizeDelta = new Vector2(400, 0);

        var vlg = buttonsPanel.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 15;
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childControlHeight = false;
        vlg.childControlWidth = false;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = false;
        buttonsPanel.SetActive(false);

        // 按鈕
        var retryBtn = CreateButton(buttonsPanel.transform, "RetryButton", "RETRY", 
            Vector2.zero, Vector2.zero, new Vector2(280, 60));
        var backBtn = CreateButton(buttonsPanel.transform, "BackToTitleButton", "BACK TO TITLE",
            Vector2.zero, Vector2.zero, new Vector2(280, 60));
        var quitBtn = CreateButton(buttonsPanel.transform, "QuitButton", "QUIT",
            Vector2.zero, Vector2.zero, new Vector2(280, 60));

        // 連接 EndingUI
        var serializedEndingUI = new SerializedObject(endingUI);
        serializedEndingUI.FindProperty("victoryVideoPlayer").objectReferenceValue = victoryVP;
        serializedEndingUI.FindProperty("failVideoPlayer").objectReferenceValue = failVP;
        serializedEndingUI.FindProperty("buttonsPanel").objectReferenceValue = buttonsPanel;
        serializedEndingUI.FindProperty("retryButton").objectReferenceValue = retryBtn.GetComponent<Button>();
        serializedEndingUI.FindProperty("backToTitleButton").objectReferenceValue = backBtn.GetComponent<Button>();
        serializedEndingUI.FindProperty("quitButton").objectReferenceValue = quitBtn.GetComponent<Button>();
        serializedEndingUI.FindProperty("autoShowButtonsDelay").floatValue = 3f;
        serializedEndingUI.ApplyModifiedProperties();

        // EventSystem
        CreateEventSystem();

        // 儲存
        string path = $"{ScenesFolder}/Ending.unity";
        EnsureDirectoryExists(path);
        EditorSceneManager.SaveScene(scene, path);
        Debug.Log($"[StoryModeSetup] ✓ 已建立 Ending 場景: {path}");
        Debug.Log("[StoryModeSetup] → 記得在 VictoryVideoPlayer / FailVideoPlayer 的 Inspector 中：拖入影片 Clip + 設定 Target Camera = Main Camera");
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

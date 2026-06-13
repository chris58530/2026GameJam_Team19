using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

/// <summary>
/// 一鍵建立「按鈕輪迴版」關卡 (Over My Dead Body Loop)。
/// 選單:Tools/屍體墊步/建立按鈕輪迴關卡
/// 佈局由提供的 HTML 原型 (800x600, 1 單位 = 40px) 換算而來。
/// </summary>
public static class OverMyDeadBodyLoopBuilder
{
    const string ScenePath = "Assets/Scenes/OverMyDeadBodyLoop.unity";
    const string SpritePath = "Assets/Sprites/WhiteSquare.png";

    [MenuItem("Tools/屍體墊步/建立按鈕輪迴關卡")]
    public static void Build()
    {
        int groundLayer = EnsureLayer("Ground");
        Sprite square = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
        if (square == null)
        {
            EditorUtility.DisplayDialog("錯誤", "找不到 " + SpritePath, "OK");
            return;
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // ---- 攝影機 (正交,涵蓋換算後的關卡高度 15 單位) ----
        var cam = Camera.main;
        if (cam != null)
        {
            cam.orthographic = true;
            cam.orthographicSize = 7.5f;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.10f, 0.10f, 0.18f, 1f);
        }

        Color platColor = new Color(0.32f, 0.32f, 0.40f);
        Color wallColor = new Color(0.22f, 0.22f, 0.30f);

        // ---- 幾何 (世界座標,y 向上) ----
        MakeBlock(square, "Floor", new Vector2(0f, -6.875f), new Vector2(20f, 1.25f), platColor, groundLayer, true, 0);
        MakeBlock(square, "Wall_Left", new Vector2(-9.875f, 0f), new Vector2(0.25f, 15f), wallColor, groundLayer, true, 0);
        MakeBlock(square, "Wall_Right", new Vector2(9.875f, 0f), new Vector2(0.25f, 15f), wallColor, groundLayer, true, 0);
        MakeBlock(square, "MidPlatform", new Vector2(-3.25f, -4.0f), new Vector2(2.5f, 0.5f), platColor, groundLayer, true, 1);
        MakeBlock(square, "HighPlatform", new Vector2(3.25f, -0.75f), new Vector2(2.5f, 0.5f), platColor, groundLayer, true, 1);

        // ---- 按鈕 A/B/C (無碰撞器,純偵測區 + 視覺) ----
        var btnA = MakeButton(square, "Button_A", "A", new Vector2(-7.5f, -6.0f), new Color(0.9f, 0.3f, 0.25f));
        var btnB = MakeButton(square, "Button_B", "B", new Vector2(-3.25f, -3.5f), new Color(0.25f, 0.55f, 0.9f));
        var btnC = MakeButton(square, "Button_C", "C", new Vector2(3.25f, -0.25f), new Color(0.2f, 0.8f, 0.4f));

        // ---- 大門 (右側地板,trigger) ----
        var door = MakeBlock(square, "Door", new Vector2(8.25f, -5.0f), new Vector2(1.5f, 2.5f),
            new Color(0.75f, 0.6f, 0.25f), -1, true, 2);
        door.GetComponent<BoxCollider2D>().isTrigger = true;
        door.AddComponent<LoopDoorExit>();

        // ---- 閘門機關 (Gate prefab 範例:A/B/C 三鈕全壓才開,擋在門口) ----
        var gatePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Gate.prefab");
        if (gatePrefab != null)
        {
            var gate = (GameObject)PrefabUtility.InstantiatePrefab(gatePrefab);
            gate.name = "DoorGate";
            gate.transform.position = new Vector3(7.3f, -5.0f, 0f);
            gate.transform.localScale = new Vector3(0.6f, 2.6f, 1f);
            var mech = gate.GetComponent<Mechanism>();
            mech.triggers = new[]
            {
                btnA.GetComponent<PressButton>(),
                btnB.GetComponent<PressButton>(),
                btnC.GetComponent<PressButton>()
            };
            mech.requireAll = true;
            mech.direction = OpenDirection.Up;
            mech.distance = 2.7f;
        }

        // ---- 重生點 ----
        var spawn = new GameObject("SpawnPoint");
        spawn.transform.position = new Vector3(-8.75f, -5.75f, 0f);

        // ---- 玩家 ----
        var player = new GameObject("Player");
        player.tag = "Player";
        player.transform.position = new Vector3(-8.75f, -5.75f, 0f);
        var psr = player.AddComponent<SpriteRenderer>();
        psr.sprite = square;
        psr.color = new Color(0.9f, 0.3f, 0.25f);
        psr.sortingOrder = 10;
        var rb = player.AddComponent<Rigidbody2D>();
        rb.gravityScale = 4.5f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        player.AddComponent<BoxCollider2D>();
        var pc = player.AddComponent<PlayerController2D>();
        pc.groundLayer = (1 << groundLayer);
        pc.moveSpeed = 6f;
        pc.jumpForce = 16f;

        // ---- 管理器 ----
        var gm = new GameObject("GameManager");
        var mgr = gm.AddComponent<LoopManager>();
        mgr.spawnPoint = spawn.transform;
        mgr.buttons = new[]
        {
            btnA.GetComponent<PressButton>(),
            btnB.GetComponent<PressButton>(),
            btnC.GetComponent<PressButton>()
        };
        mgr.ghostSprite = square;
        mgr.ghostColor = new Color(0.85f, 0.3f, 0.3f, 0.7f);
        mgr.ghostScale = Vector2.one;
        mgr.ghostLayer = groundLayer;
        mgr.ghostSortingOrder = 5;
        mgr.loopTime = 10f;

        // ---- UI ----
        BuildUI(mgr, square);

        // ---- 存檔 ----
        if (!Directory.Exists("Assets/Scenes")) Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.Refresh();

        var sv = SceneView.lastActiveSceneView;
        if (sv != null) { sv.in2DMode = true; sv.LookAt(new Vector3(0f, -3f, 0f), Quaternion.identity, 10f); }

        Debug.Log("[OverMyDeadBodyLoop] 關卡已建立:" + ScenePath);
        EditorUtility.DisplayDialog("完成",
            "已建立按鈕輪迴關卡:\n" + ScenePath +
            "\n\nA/D 移動、W 跳、K 提早結算、R 重置。\n每輪 10 秒,用殘影壓住 A/B/C 三鈕開門。",
            "OK");
    }

    // ---------- 幾何 ----------
    static GameObject MakeBlock(Sprite sprite, string name, Vector2 pos, Vector2 size, Color color,
        int layer, bool collider, int sortingOrder)
    {
        var go = new GameObject(name);
        go.transform.position = new Vector3(pos.x, pos.y, 0f);
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        if (layer >= 0) go.layer = layer;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = sortingOrder;

        if (collider) go.AddComponent<BoxCollider2D>();
        return go;
    }

    static GameObject MakeButton(Sprite sprite, string name, string id, Vector2 pos, Color color)
    {
        var go = MakeBlock(sprite, name, pos, new Vector2(1f, 0.4f), color, -1, false, 3);
        var pb = go.AddComponent<PressButton>();
        pb.id = id;
        pb.checkOffset = new Vector2(0f, 0.5f);
        pb.checkSize = new Vector2(1.0f, 1.0f);
        return go;
    }

    // ---------- UI ----------
    static void BuildUI(LoopManager mgr, Sprite square)
    {
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var canvasGO = new GameObject("HUDCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        var hud = canvasGO.AddComponent<HUDController>();
        hud.manager = mgr;

        // 左上 TIME
        hud.timeText = MakeText(canvas.transform, font, "TimeText", "TIME: 10 s", 40, Color.white,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -30f), new Vector2(420f, 56f),
            TextAnchor.UpperLeft);

        // 右上 LOOP
        hud.loopText = MakeText(canvas.transform, font, "LoopText", "LOOP: 1", 40, Color.white,
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40f, -30f), new Vector2(420f, 56f),
            TextAnchor.UpperRight);

        // 上中 BUTTONS x/3
        hud.progressText = MakeText(canvas.transform, font, "ProgressText", "BUTTONS: 0 / 3", 40,
            new Color(1f, 0.85f, 0.2f),
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(700f, 56f),
            TextAnchor.UpperCenter);

        // 下方提示
        MakeText(canvas.transform, font, "HintText",
            "A/D = Move    W = Jump    K = End loop (leave ghost)    R = Reset", 28,
            new Color(0.8f, 0.8f, 0.85f),
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(1400f, 44f),
            TextAnchor.LowerCenter);

        // ---- CLEAR 覆蓋層 ----
        var panelGO = new GameObject("ClearPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panelGO.transform.SetParent(canvas.transform, false);
        var prt = panelGO.GetComponent<RectTransform>();
        prt.anchorMin = Vector2.zero; prt.anchorMax = Vector2.one;
        prt.offsetMin = Vector2.zero; prt.offsetMax = Vector2.zero;
        var panelImg = panelGO.GetComponent<Image>();
        panelImg.sprite = square;
        panelImg.type = Image.Type.Simple;
        panelImg.color = new Color(0f, 0f, 0f, 0.85f);

        MakeText(panelGO.transform, font, "ClearTitle", "CLEAR!", 110, new Color(1f, 0.82f, 0.2f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 80f), new Vector2(900f, 160f),
            TextAnchor.MiddleCenter);

        hud.clearLoopsText = MakeText(panelGO.transform, font, "ClearLoops", "Loops Used: 0", 48, Color.white,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -30f), new Vector2(900f, 70f),
            TextAnchor.MiddleCenter);

        MakeText(panelGO.transform, font, "ClearHint", "Press R to try again with fewer loops", 30,
            new Color(0.75f, 0.75f, 0.8f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -110f), new Vector2(1000f, 50f),
            TextAnchor.MiddleCenter);

        hud.clearPanel = panelGO;
        panelGO.SetActive(false);
    }

    static Text MakeText(Transform parent, Font font, string name, string text, int fontSize, Color color,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size, TextAnchor align)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.font = font;
        t.text = text;
        t.fontSize = fontSize;
        t.fontStyle = FontStyle.Bold;
        t.color = color;
        t.alignment = align;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;

        var rt = t.rectTransform;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = anchoredPos;
        return t;
    }

    static int EnsureLayer(string name)
    {
        var tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layers = tagManager.FindProperty("layers");
        for (int i = 0; i < layers.arraySize; i++)
            if (layers.GetArrayElementAtIndex(i).stringValue == name) return i;
        for (int i = 8; i < layers.arraySize; i++)
        {
            var el = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(el.stringValue))
            {
                el.stringValue = name;
                tagManager.ApplyModifiedProperties();
                return i;
            }
        }
        return 0;
    }
}

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

/// <summary>
/// 一鍵建立「屍體墊步」關卡場景 (Over My Dead Body)。
/// 選單:Tools/屍體墊步/建立關卡場景
/// </summary>
public static class OverMyDeadBodyBuilder
{
    const string ScenePath = "Assets/Scenes/OverMyDeadBody.unity";
    const string SpritePath = "Assets/Sprites/WhiteSquare.png";

    [MenuItem("Tools/屍體墊步/建立關卡場景")]
    public static void Build()
    {
        int groundLayer = EnsureLayer("Ground");
        Sprite square = GetSquareSprite();
        if (square == null)
        {
            EditorUtility.DisplayDialog("錯誤", "找不到 " + SpritePath, "OK");
            return;
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // ---- 攝影機 ----
        var cam = Camera.main;
        if (cam != null)
        {
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.10f, 0.10f, 0.14f, 1f);
        }

        Color groundColor = new Color(0.40f, 0.30f, 0.22f);
        Color wallColor = new Color(0.26f, 0.26f, 0.32f);
        Color ledgeColor = new Color(0.34f, 0.34f, 0.42f);

        // ---- 關卡幾何 (正交 size 5 => y[-5,5], 16:9 寬約 [-8.9,8.9]) ----
        MakeBlock(square, "Ground", new Vector2(0f, -4f), new Vector2(18f, 1f), groundColor, groundLayer, true, false, 0);
        MakeBlock(square, "Wall_Left", new Vector2(-8.5f, 0f), new Vector2(1f, 10f), wallColor, groundLayer, true, false, 0);
        MakeBlock(square, "Wall_Right", new Vector2(8.5f, 0f), new Vector2(1f, 10f), wallColor, groundLayer, true, false, 0);
        MakeBlock(square, "Ceiling", new Vector2(0f, 4.7f), new Vector2(18f, 0.6f), wallColor, groundLayer, true, false, 0);

        // 高台 (放鑰匙) — 單跳無法到達,需要疊屍體上來
        MakeBlock(square, "KeyLedge", new Vector2(6f, 0.7f), new Vector2(4f, 0.6f), ledgeColor, groundLayer, true, false, 0);

        // ---- 重生點 ----
        var spawn = new GameObject("SpawnPoint");
        spawn.transform.position = new Vector3(-7f, -3f, 0f);
        // 視覺標記 (綠色細條,無碰撞)
        MakeBlock(square, "StartMarker", new Vector2(-7.5f, -2.7f), new Vector2(0.3f, 1.6f),
            new Color(0.25f, 0.8f, 0.35f), -1, false, false, 3);

        // ---- 鑰匙 ----
        var key = MakeBlock(square, "Key", new Vector2(6f, 1.6f), new Vector2(0.5f, 0.5f),
            new Color(1f, 0.85f, 0.2f), -1, true, true, 8);
        key.AddComponent<KeyPickup>();

        // ---- 大門 (地面右側) ----
        var door = MakeBlock(square, "Door", new Vector2(7.8f, -2.8f), new Vector2(1f, 1.4f),
            new Color(0.85f, 0.65f, 0.25f), -1, true, true, 4);
        door.AddComponent<DoorExit>();

        // ---- 玩家 ----
        var player = new GameObject("Player");
        player.tag = "Player";
        player.transform.position = new Vector3(-7f, -3f, 0f);
        var psr = player.AddComponent<SpriteRenderer>();
        psr.sprite = square;
        psr.color = new Color(0.3f, 0.6f, 0.95f);
        psr.sortingOrder = 10;
        var rb = player.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        player.AddComponent<BoxCollider2D>();
        var pc = player.AddComponent<PlayerController2D>();
        pc.groundLayer = (1 << groundLayer);

        // ---- 關卡管理器 ----
        var gm = new GameObject("GameManager");
        var mgr = gm.AddComponent<DeadBodyManager>();
        mgr.spawnPoint = spawn.transform;
        mgr.keyObject = key;
        mgr.corpseSprite = square;
        mgr.corpseColor = new Color(0.8f, 0.35f, 0.35f);
        mgr.corpseScale = Vector2.one;
        mgr.corpseLayer = groundLayer;
        mgr.corpseSortingOrder = 5;

        // 技能卡系統 (示範場景牌庫留空 = 無技能,維持原本玩法)
        var skills = gm.AddComponent<CorpseSkillSystem>();
        skills.groundLayer = groundLayer;
        mgr.skillSystem = skills;

        // ---- 存檔 ----
        if (!Directory.Exists("Assets/Scenes")) Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.Refresh();

        // 在 Scene 視窗切到 2D 並對準
        var sv = SceneView.lastActiveSceneView;
        if (sv != null) { sv.in2DMode = true; sv.LookAt(new Vector3(0f, -0.5f, 0f), Quaternion.identity, 7f); }

        Debug.Log("[OverMyDeadBody] 關卡場景已建立並存檔:" + ScenePath);
        EditorUtility.DisplayDialog("完成", "已建立關卡場景:\n" + ScenePath + "\n\n按 Play 試玩。\nA/D 移動、W 跳、K 自殺墊步、R 重置。", "OK");
    }

    static GameObject MakeBlock(Sprite sprite, string name, Vector2 pos, Vector2 size, Color color,
        int layer, bool collider, bool trigger, int sortingOrder)
    {
        var go = new GameObject(name);
        go.transform.position = new Vector3(pos.x, pos.y, 0f);
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        if (layer >= 0) go.layer = layer;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = sortingOrder;

        if (collider)
        {
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = trigger;
        }
        return go;
    }

    static Sprite GetSquareSprite()
    {
        var s = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
        return s;
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

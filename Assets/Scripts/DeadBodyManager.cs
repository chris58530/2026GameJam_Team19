using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 「屍體墊步」關卡管理器。
/// - K：自殺。死亡位置留下一具屍體(實體平台),玩家回到重生點。
/// - R：重置關卡(清除所有屍體、回收鑰匙、回到起點)。
/// 玩家踩著累積的屍體疊出階梯,取得高處鑰匙後進入大門通關。
/// </summary>
public class DeadBodyManager : MonoBehaviour
{
    [Header("場景參考")]
    [Tooltip("玩家重生點")]
    public Transform spawnPoint;

    [Tooltip("鑰匙物件 (重置時會重新啟用)")]
    public GameObject keyObject;

    [Header("屍體設定")]
    [Tooltip("屍體使用的 Sprite (建議指定 WhiteSquare)")]
    public Sprite corpseSprite;

    [Tooltip("屍體顏色")]
    public Color corpseColor = new Color(0.8f, 0.35f, 0.35f);

    [Tooltip("屍體大小 (世界單位)")]
    public Vector2 corpseScale = Vector2.one;

    [Tooltip("屍體所在圖層 (應為 Ground,才能被地面偵測當平台)")]
    public int corpseLayer;

    [Tooltip("屍體排序層級 (顯示在背景之上、玩家之下)")]
    public int corpseSortingOrder = 5;

    // 執行時狀態
    private GameObject _player;
    private Rigidbody2D _playerRb;
    private readonly List<GameObject> _corpses = new List<GameObject>();

    public int Deaths { get; private set; }
    public bool HasKey { get; private set; }
    public bool Won { get; private set; }

    private float _messageTimer;
    private string _message = "";

    private void Start()
    {
        _player = GameObject.FindGameObjectWithTag("Player");
        if (_player != null) _playerRb = _player.GetComponent<Rigidbody2D>();
        if (corpseLayer == 0)
        {
            int g = LayerMask.NameToLayer("Ground");
            if (g >= 0) corpseLayer = g;
        }
    }

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        // R 重置:任何時候都可用 (含通關後再玩一次)
        if (kb.rKey.wasPressedThisFrame)
        {
            ResetLevel();
            return;
        }

        if (_messageTimer > 0f) _messageTimer -= Time.deltaTime;

        if (Won) return;

        if (kb.kKey.wasPressedThisFrame)
            Die();
    }

    /// <summary>玩家自殺:留下屍體平台並回到重生點。</summary>
    public void Die()
    {
        if (_player == null || spawnPoint == null) return;

        SpawnCorpse(_player.transform.position);
        Deaths++;

        _player.transform.position = spawnPoint.position;
        if (_playerRb != null) _playerRb.linearVelocity = Vector2.zero;
    }

    private void SpawnCorpse(Vector3 pos)
    {
        var go = new GameObject("Corpse_" + (_corpses.Count + 1));
        go.transform.position = pos;
        go.transform.localScale = new Vector3(corpseScale.x, corpseScale.y, 1f);
        if (corpseLayer >= 0) go.layer = corpseLayer;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = corpseSprite;
        sr.color = corpseColor;
        sr.sortingOrder = corpseSortingOrder;

        go.AddComponent<BoxCollider2D>();
        _corpses.Add(go);
    }

    /// <summary>重置:清除屍體、回收鑰匙、回到起點。</summary>
    public void ResetLevel()
    {
        foreach (var c in _corpses)
            if (c != null) Destroy(c);
        _corpses.Clear();

        Deaths = 0;
        HasKey = false;
        Won = false;

        if (keyObject != null) keyObject.SetActive(true);

        if (_player != null && spawnPoint != null)
        {
            _player.transform.position = spawnPoint.position;
            if (_playerRb != null) _playerRb.linearVelocity = Vector2.zero;
        }
    }

    public void CollectKey()
    {
        HasKey = true;
        ShowMessage("GOT THE KEY!  Head to the door");
    }

    public void TryExit()
    {
        if (HasKey)
            Win();
        else
            ShowMessage("NEED A KEY!");
    }

    private void Win()
    {
        Won = true;
        if (_playerRb != null) _playerRb.linearVelocity = Vector2.zero;
    }

    private void ShowMessage(string msg)
    {
        _message = msg;
        _messageTimer = 2f;
    }

    private void OnGUI()
    {
        var style = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold };
        GUI.Label(new Rect(16, 12, 600, 30),
            "Loops: " + Deaths + (HasKey ? "    Key: YES" : "    Key: NO"), style);

        GUI.Label(new Rect(16, 44, 600, 26), "A/D = Move    W = Jump    K = Die (stack corpse)    R = Reset");

        if (_messageTimer > 0f)
        {
            var m = new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold };
            m.normal.textColor = Color.yellow;
            GUI.Label(new Rect(0, Screen.height * 0.18f, Screen.width, 40), _message,
                Centered(m));
        }

        if (Won)
        {
            var big = new GUIStyle(GUI.skin.label) { fontSize = 80, fontStyle = FontStyle.Bold };
            big.normal.textColor = Color.white;
            GUI.Label(new Rect(0, Screen.height * 0.32f, Screen.width, 120), "CLEAR!", Centered(big));

            var sub = new GUIStyle(GUI.skin.label) { fontSize = 28, fontStyle = FontStyle.Bold };
            sub.normal.textColor = new Color(1f, 0.85f, 0.3f);
            GUI.Label(new Rect(0, Screen.height * 0.52f, Screen.width, 40),
                "Loops Used: " + Deaths + "    (Press R to retry)", Centered(sub));
        }
    }

    private static GUIStyle Centered(GUIStyle s)
    {
        s.alignment = TextAnchor.MiddleCenter;
        return s;
    }
}

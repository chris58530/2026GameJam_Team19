using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 「屍體墊步」關卡管理器 (整合技能卡系統)。
/// - 每條命開始先抽卡選技能 (透過 CorpseSkillSystem);選定後才開始這條命。
/// - K:自殺。死亡位置留下一具屍體 (帶本命選的技能),玩家回到重生點,再抽下一張。
/// - 碰到 Hazard 傷害區域 → 宣告失敗,顯示文字後整關重來。
/// - R:重置關卡 (清除所有屍體、回收鑰匙、回到起點、重新抽卡)。
///
/// 技能系統為可選:若場上沒有 CorpseSkillSystem 或牌庫為空,維持原本無技能玩法
/// (屍體一律普通,不顯示選卡)。
/// </summary>
public class DeadBodyManager : MonoBehaviour, ILevelFailHandler
{
    [Header("場景參考")]
    [Tooltip("玩家重生點")]
    public Transform spawnPoint;

    [Tooltip("鑰匙物件 (重置時會重新啟用)")]
    public GameObject keyObject;

    [Tooltip("技能卡系統 (可選)。留空會自動在自身或場景中尋找。")]
    public CorpseSkillSystem skillSystem;

    [Header("屍體設定")]
    [Tooltip("屍體使用的 Sprite (建議指定 WhiteSquare)")]
    public Sprite corpseSprite;

    [Tooltip("屍體底色 (無技能系統時使用;有技能時會被技能顏色覆蓋)")]
    public Color corpseColor = new Color(0.8f, 0.35f, 0.35f);

    [Tooltip("屍體大小 (世界單位)")]
    public Vector2 corpseScale = Vector2.one;

    [Tooltip("屍體所在圖層 (應為 Ground,才能被地面偵測當平台)")]
    public int corpseLayer;

    [Tooltip("屍體排序層級 (顯示在背景之上、玩家之下)")]
    public int corpseSortingOrder = 5;

    [Header("失敗設定")]
    [Tooltip("顯示失敗文字的秒數,結束後整關重來")]
    public float failDisplayTime = 1.5f;

    // 執行時狀態
    private GameObject _player;
    private Rigidbody2D _playerRb;
    private PlayerController2D _pc;
    private readonly List<GameObject> _corpses = new List<GameObject>();

    private bool _failed;
    private float _failTimer;
    private string _failReason = "";

    public int Deaths { get; private set; }
    public bool HasKey { get; private set; }
    public bool Won { get; private set; }

    private float _messageTimer;
    private string _message = "";

    private CorpseSkillType ArmedSkill =>
        skillSystem != null ? skillSystem.ArmedSkill : CorpseSkillType.Normal;

    private void Start()
    {
        _player = GameObject.FindGameObjectWithTag("Player");
        if (_player != null)
        {
            _playerRb = _player.GetComponent<Rigidbody2D>();
            _pc = _player.GetComponent<PlayerController2D>();
        }
        if (corpseLayer == 0)
        {
            int g = LayerMask.NameToLayer("Ground");
            if (g >= 0) corpseLayer = g;
        }
        if (skillSystem == null)
        {
            skillSystem = GetComponent<CorpseSkillSystem>();
            if (skillSystem == null) skillSystem = FindAnyObjectByType<CorpseSkillSystem>();
        }

        BeginLife();
    }

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.rKey.wasPressedThisFrame)
        {
            ResetLevel();
            return;
        }

        if (_messageTimer > 0f) _messageTimer -= Time.deltaTime;

        if (_failed)
        {
            _failTimer -= Time.unscaledDeltaTime;
            if (_failTimer <= 0f) ResetLevel();
            return;
        }

        // 選卡中:交給技能系統處理
        if (skillSystem != null && skillSystem.IsBusy) return;

        if (Won) return;

        if (kb.kKey.wasPressedThisFrame)
            Die();
    }

    /// <summary>開始一條新命:先抽卡選技能,選定後才開始遊玩。</summary>
    private void BeginLife()
    {
        if (skillSystem == null) return;

        if (_pc != null) _pc.enabled = false;
        skillSystem.BeginSelection(_ =>
        {
            if (_pc != null) _pc.enabled = true;
        });
    }

    /// <summary>玩家自殺:留下帶技能的屍體,回到重生點,進入下一條命。</summary>
    public void Die()
    {
        if (_player == null || spawnPoint == null) return;

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX("Die");

        SpawnCorpse(_player.transform.position, ArmedSkill);
        Deaths++;

        _player.transform.position = spawnPoint.position;
        if (_playerRb != null) _playerRb.linearVelocity = Vector2.zero;

        BeginLife();
    }

    private void SpawnCorpse(Vector3 pos, CorpseSkillType skill)
    {
        var go = new GameObject("Corpse_" + (_corpses.Count + 1) + "_" + skill);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(corpseScale.x, corpseScale.y, 1f);
        if (corpseLayer >= 0) go.layer = corpseLayer;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = corpseSprite;
        sr.color = corpseColor;
        sr.sortingOrder = corpseSortingOrder;

        go.AddComponent<BoxCollider2D>();

        if (skillSystem != null)
            skillSystem.ApplySkill(go, skill);

        _corpses.Add(go);
    }

    /// <summary>ILevelFailHandler:非自身機制死亡 → 失敗後整關重來。</summary>
    public void FailLevel(string reason)
    {
        if (_failed || Won) return;

        _failed = true;
        _failReason = reason;
        _failTimer = failDisplayTime;

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX("Fail");

        Time.timeScale = 1f;
        if (_pc != null) _pc.enabled = false;
        if (_playerRb != null) _playerRb.linearVelocity = Vector2.zero;
    }

    /// <summary>重置:清除屍體、回收鑰匙、回到起點、重新抽卡。</summary>
    public void ResetLevel()
    {
        foreach (var c in _corpses)
            if (c != null) Destroy(c);
        _corpses.Clear();

        Deaths = 0;
        HasKey = false;
        Won = false;
        _failed = false;
        _failReason = "";

        if (keyObject != null) keyObject.SetActive(true);

        Time.timeScale = 1f;
        if (_pc != null) _pc.enabled = true;

        if (_player != null && spawnPoint != null)
        {
            _player.transform.position = spawnPoint.position;
            if (_playerRb != null) _playerRb.linearVelocity = Vector2.zero;
        }

        if (skillSystem != null) skillSystem.ResetMemory();
        BeginLife();
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
        {
            if (SoundManager.Instance != null)
                SoundManager.Instance.PlaySFX("DoorLocked");
            ShowMessage("NEED A KEY!");
        }
    }

    private void Win()
    {
        Won = true;
        Time.timeScale = 1f;
        if (_playerRb != null) _playerRb.linearVelocity = Vector2.zero;

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX("LevelClear");
    }

    private void ShowMessage(string msg)
    {
        _message = msg;
        _messageTimer = 2f;
    }

    private void OnGUI()
    {
        var style = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold };
        string armedText = (skillSystem != null && skillSystem.HasUsableDeck())
            ? "    本命技能: " + CorpseSkillNames.ToDisplay(ArmedSkill) : "";
        GUI.Label(new Rect(16, 12, 800, 30),
            "Loops: " + Deaths + (HasKey ? "    Key: YES" : "    Key: NO") + armedText, style);

        GUI.Label(new Rect(16, 44, 800, 26), "A/D = Move    W = Jump    K = Die (stack corpse)    R = Reset");

        if (_messageTimer > 0f)
        {
            var m = new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold };
            m.normal.textColor = Color.yellow;
            GUI.Label(new Rect(0, Screen.height * 0.18f, Screen.width, 40), _message, Centered(m));
        }

        if (_failed)
        {
            var big = new GUIStyle(GUI.skin.label) { fontSize = 70, fontStyle = FontStyle.Bold };
            big.normal.textColor = new Color(1f, 0.3f, 0.3f);
            GUI.Label(new Rect(0, Screen.height * 0.34f, Screen.width, 100), "FAILED", Centered(big));

            var sub = new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold };
            sub.normal.textColor = Color.white;
            GUI.Label(new Rect(0, Screen.height * 0.50f, Screen.width, 40),
                _failReason + "   重新開始...", Centered(sub));
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

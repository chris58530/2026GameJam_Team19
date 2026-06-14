using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 「Over My Dead Body」按鈕輪迴關卡管理器 (整合技能卡系統)。
/// - 每輪開始先抽卡選技能 (透過 CorpseSkillSystem);選定後才開始這一輪。
/// - 每輪 loopTime 秒;時間到或按 K → 在原地留下殘影 (帶本輪選的技能)、玩家回起點、輪數+1。
/// - 殘影是實體平台 (可踩),也能壓住按鈕;若選到移動/傳送等技能會有對應行為。
/// - A/B/C 按鈕同時被壓下 → 門開;門開時玩家走到門口即通關。
/// - 碰到 Hazard 傷害區域 → 宣告失敗,顯示文字後整關重來。
/// - R 從第 1 輪重來。
///
/// 技能系統為可選:若場上沒有 CorpseSkillSystem 或牌庫為空,維持原本無技能玩法。
/// </summary>
public class LoopManager : MonoBehaviour, ILevelFailHandler
{
    [Header("場景參考")]
    public Transform spawnPoint;
    public PressButton[] buttons;

    [Tooltip("技能卡系統 (可選)。留空會自動在自身或場景中尋找。")]
    public CorpseSkillSystem skillSystem;

    [Header("殘影設定")]
    public Sprite ghostSprite;
    public Color ghostColor = new Color(0.85f, 0.3f, 0.3f, 0.7f);
    public Vector2 ghostScale = Vector2.one;
    [Tooltip("殘影碰撞體大小 (世界單位)。與玩家碰撞體一致,不受殘影圖片大小影響。")]
    public Vector2 ghostColliderSize = Vector2.one;
    [Tooltip("殘影是否上下顛倒 (倒立)。玩家圖片變成殘影時翻轉。")]
    public bool ghostFlipY = true;
    [Tooltip("殘影圖層 (應為 Ground,才能當平台被踩)")]
    public int ghostLayer;
    public int ghostSortingOrder = 5;

    [Header("輪迴設定")]
    public float loopTime = 10f;

    [Header("失敗設定")]
    [Tooltip("顯示失敗文字的秒數,結束後整關重來")]
    public float failDisplayTime = 1.5f;

    public float TimeLeft { get; private set; }
    public int LoopCount { get; private set; } = 1;
    public bool Won { get; private set; }
    public int PressedCount { get; private set; }
    public bool DoorOpen { get; private set; }

    private GameObject _player;
    private Rigidbody2D _rb;
    private PlayerController2D _pc;
    private readonly List<GameObject> _ghosts = new List<GameObject>();

    private bool _failed;
    private float _failTimer;
    private string _failReason = "";

    private void Start()
    {
        _player = GameObject.FindGameObjectWithTag("Player");
        if (_player != null)
        {
            _rb = _player.GetComponent<Rigidbody2D>();
            _pc = _player.GetComponent<PlayerController2D>();
        }
        if (ghostLayer == 0)
        {
            int g = LayerMask.NameToLayer("Ground");
            if (g >= 0) ghostLayer = g;
        }
        if (skillSystem == null)
        {
            skillSystem = GetComponent<CorpseSkillSystem>();
            if (skillSystem == null) skillSystem = FindAnyObjectByType<CorpseSkillSystem>();
        }

        TimeLeft = loopTime;
        BeginLife();
    }

    private void Update()
    {
        var kb = Keyboard.current;

        if (kb != null && kb.rKey.wasPressedThisFrame)
        {
            ResetLevel();
            return;
        }

        // 失敗中:倒數後整關重來
        if (_failed)
        {
            _failTimer -= Time.unscaledDeltaTime;
            if (_failTimer <= 0f) ResetLevel();
            return;
        }

        // 選卡中:暫停本管理器邏輯,交給技能系統處理
        if (skillSystem != null && skillSystem.IsBusy) return;

        // 按鈕狀態 / 門
        PressedCount = 0;
        if (buttons != null)
            foreach (var b in buttons)
                if (b != null && b.IsPressed) PressedCount++;
        DoorOpen = buttons != null && buttons.Length > 0 && PressedCount == buttons.Length;

        if (Won) return;

        TimeLeft -= Time.deltaTime;
        if (TimeLeft <= 0f)
            LeaveGhostAndRespawn();

        if (kb != null && kb.kKey.wasPressedThisFrame)
            LeaveGhostAndRespawn();
    }

    /// <summary>開始新的一輪:先抽卡選技能,選定後才開始計時遊玩。</summary>
    private void BeginLife()
    {
        if (skillSystem == null)
            return; // 無技能系統 → 直接遊玩

        if (_pc != null) _pc.enabled = false;
        skillSystem.BeginSelection(_ =>
        {
            if (_pc != null) _pc.enabled = true;
        });
    }

    private void LeaveGhostAndRespawn()
    {
        if (_player == null || spawnPoint == null) return;

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX("Die");

        var ghost = SpawnGhost(_player.transform.position);
        if (skillSystem != null && ghost != null)
            skillSystem.ApplySkill(ghost, skillSystem.ArmedSkill);

        _player.transform.position = spawnPoint.position;
        if (_rb != null) _rb.linearVelocity = Vector2.zero;

        TimeLeft = loopTime;
        LoopCount++;

        BeginLife(); // 抽下一輪的技能
    }

    private GameObject SpawnGhost(Vector3 pos)
    {
        var go = new GameObject("Ghost_" + (_ghosts.Count + 1));
        go.transform.position = pos;
        go.transform.localScale = new Vector3(ghostScale.x, ghostScale.y, 1f);
        if (ghostLayer >= 0) go.layer = ghostLayer;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = ghostSprite;
        sr.color = ghostColor;
        sr.sortingOrder = ghostSortingOrder;
        sr.flipY = ghostFlipY;   // 殘影上下顛倒 (玩家圖片倒立)

        var col = go.AddComponent<BoxCollider2D>();
        // 維持殘影碰撞體為固定世界大小 (與玩家一致),抵銷縮放與大圖的影響
        float sx = Mathf.Approximately(ghostScale.x, 0f) ? 1f : Mathf.Abs(ghostScale.x);
        float sy = Mathf.Approximately(ghostScale.y, 0f) ? 1f : Mathf.Abs(ghostScale.y);
        col.size = new Vector2(ghostColliderSize.x / sx, ghostColliderSize.y / sy);

        go.AddComponent<Ghost>();
        _ghosts.Add(go);
        return go;
    }

    /// <summary>由門呼叫:門開時玩家進門即通關。</summary>
    public void TryExit()
    {
        if (!Won && DoorOpen)
        {
            Won = true;
            Time.timeScale = 1f;
            if (_rb != null) _rb.linearVelocity = Vector2.zero;
            if (_pc != null) _pc.enabled = false;

            Debug.Log($"[Victory] Level: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name} — Victory!");

            if (SoundManager.Instance != null)
                SoundManager.Instance.PlaySFX("LevelClear");
        }
    }

    /// <summary>ILevelFailHandler:非自身機制死亡 (如踏入 Hazard) → 失敗後整關重來。</summary>
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
        if (_rb != null) _rb.linearVelocity = Vector2.zero;
    }

    public void ResetLevel()
    {
        foreach (var g in _ghosts)
            if (g != null) Destroy(g);
        _ghosts.Clear();

        LoopCount = 1;
        TimeLeft = loopTime;
        Won = false;
        DoorOpen = false;
        PressedCount = 0;
        _failed = false;
        _failReason = "";

        Time.timeScale = 1f;
        if (_pc != null) _pc.enabled = true;
        if (_player != null && spawnPoint != null)
        {
            _player.transform.position = spawnPoint.position;
            if (_rb != null) _rb.linearVelocity = Vector2.zero;
        }

        BeginLife();
    }

    private void OnGUI()
    {
        if (_failed)
        {
            var big = new GUIStyle(GUI.skin.label)
            {
                fontSize = 70,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            big.normal.textColor = new Color(1f, 0.3f, 0.3f);
            GUI.Label(new Rect(0, Screen.height * 0.34f, Screen.width, 100), "FAILED", big);

            var sub = new GUIStyle(GUI.skin.label)
            {
                fontSize = 26,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            sub.normal.textColor = Color.white;
            GUI.Label(new Rect(0, Screen.height * 0.50f, Screen.width, 40),
                _failReason + "   重新開始...", sub);
        }
    }
}

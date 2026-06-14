using System.Collections;
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
    [Tooltip("屍體 prefab。指定後改用此 prefab 生成屍體 (外觀、碰撞體大小由 prefab 決定,自己編輯)。留空則用下方參數程式生成。")]
    public GameObject ghostPrefab;

    [Tooltip("使用 prefab 時,是否把屍體圖片換成 FALL 動畫最後一幀 (關閉則用 prefab 自己的圖片)")]
    public bool overrideGhostSpriteWithDeathFrame = true;

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

    [Tooltip("屍體完全出現後,停留多久 (秒) 讓玩家看清屍體位置,再讓主角從出生點復活")]
    public float corpseViewDelay = 0.5f;

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
    private bool _dying;

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

        // 死亡動畫播放中:暫停計時與輸入,等動畫播完才會變屍體
        if (_dying) return;

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
        if (_dying) return;
        StartCoroutine(DeathSequence());
    }

    /// <summary>
    /// 死亡流程:
    /// 1. 播放死亡 (FALL) 動畫,主角留在死亡點 (相機跟著主角,所以停在死亡點)。
    /// 2. 動畫播完 → 在原地留下屍體 (圖片用 FALL 最後一幀),屍體依技能執行行為。
    /// 3. 隱藏主角,停留 corpseViewDelay 秒讓玩家看清屍體位置。
    /// 4. 主角才回到出生點復活、顯示並恢復控制 → 相機開始跟隨主角。
    /// </summary>
    private IEnumerator DeathSequence()
    {
        _dying = true;

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX("Die");

        // 停止玩家操作與移動,播放死亡動畫並取得動畫長度
        float wait = 0f;
        bool flipX = false;
        if (_pc != null)
        {
            _pc.enabled = false;        // 停止移動控制 (FixedUpdate 不再覆蓋動畫)
            wait = _pc.PlayDeath();     // 播放死亡動畫
            flipX = _pc.SpriteFlipX;    // 記下死亡時的朝向,屍體沿用
        }
        if (_rb != null)
        {
            _rb.linearVelocity = Vector2.zero;
            _rb.gravityScale = 0f;      // 死亡動畫期間定身,不繼續下墜
        }

        // 等待死亡動畫播放完畢
        if (wait > 0f)
            yield return new WaitForSeconds(wait);

        // 動畫播放期間若已宣告失敗 (例如踏入 Hazard),交給失敗流程整關重來,不再生成屍體
        if (_failed)
        {
            _dying = false;
            yield break;
        }

        // 在死亡位置留下屍體 (圖片 = FALL 最後一幀)
        Vector3 deathPos = _player.transform.position;
        Sprite corpseSprite = (_pc != null) ? _pc.GetDeathLastFrameSprite() : null;

        var ghost = SpawnGhost(deathPos, corpseSprite, flipX);
        if (skillSystem != null && ghost != null)
            skillSystem.ApplySkill(ghost, skillSystem.ArmedSkill);

        // 隱藏主角,畫面只留下屍體 (相機仍停在死亡點,因為主角還沒移動)
        if (_pc != null) _pc.SetRendererVisible(false);

        // 屍體完全出現後停留一段時間,讓玩家看清楚屍體在哪
        yield return new WaitForSeconds(corpseViewDelay);

        if (_failed)
        {
            _dying = false;
            yield break;
        }

        // 主角回到出生點復活,顯示並恢復控制 → 相機開始跟隨主角過去
        _player.transform.position = spawnPoint.position;
        if (_rb != null) _rb.linearVelocity = Vector2.zero;
        if (_pc != null)
        {
            _pc.SetRendererVisible(true);
            _pc.enabled = true;         // OnEnable 解除死亡鎖定;FixedUpdate 恢復重力
        }

        TimeLeft = loopTime;
        LoopCount++;

        _dying = false;
        BeginLife(); // 抽下一輪的技能
    }

    private GameObject SpawnGhost(Vector3 pos, Sprite overrideSprite = null, bool flipX = false)
    {
        // 有指定 prefab → 用 prefab 生成 (外觀/碰撞體大小由 prefab 決定),只覆蓋圖片與朝向
        if (ghostPrefab != null)
            return SpawnGhostFromPrefab(pos, overrideSprite, flipX);

        var go = new GameObject("Ghost_" + (_ghosts.Count + 1));
        go.transform.position = pos;
        go.transform.localScale = new Vector3(ghostScale.x, ghostScale.y, 1f);
        if (ghostLayer >= 0) go.layer = ghostLayer;

        bool useFallFrame = overrideSprite != null;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = useFallFrame ? overrideSprite : ghostSprite;
        sr.color = ghostColor;
        sr.sortingOrder = ghostSortingOrder;
        sr.flipX = flipX;
        // 用 FALL 最後一幀時,圖片本身已是死亡姿勢,不再上下顛倒;否則沿用設定
        sr.flipY = useFallFrame ? false : ghostFlipY;

        var col = go.AddComponent<BoxCollider2D>();
        // 維持殘影碰撞體為固定世界大小 (與玩家一致),抵銷縮放與大圖的影響
        float sx = Mathf.Approximately(ghostScale.x, 0f) ? 1f : Mathf.Abs(ghostScale.x);
        float sy = Mathf.Approximately(ghostScale.y, 0f) ? 1f : Mathf.Abs(ghostScale.y);
        col.size = new Vector2(ghostColliderSize.x / sx, ghostColliderSize.y / sy);

        go.AddComponent<Ghost>();
        _ghosts.Add(go);
        return go;
    }

    /// <summary>
    /// 由 prefab 生成屍體。外觀、碰撞體大小、圖層等都沿用 prefab 設定 (供自行編輯),
    /// 只覆蓋圖片 (FALL 最後一幀) 與左右朝向。
    /// </summary>
    private GameObject SpawnGhostFromPrefab(Vector3 pos, Sprite overrideSprite, bool flipX)
    {
        var go = Instantiate(ghostPrefab, pos, Quaternion.identity);
        go.name = "Ghost_" + (_ghosts.Count + 1);

        // 確保有 Ghost 標記 (按鈕辨識用)
        if (go.GetComponent<Ghost>() == null)
            go.AddComponent<Ghost>();

        var sr = go.GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            if (overrideGhostSpriteWithDeathFrame && overrideSprite != null)
                sr.sprite = overrideSprite;
            sr.flipX = flipX;
        }

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

            // Story Mode 連接：通知 LevelManager 進入下一關
            if (LevelManager.Instance != null)
                LevelManager.Instance.OnLevelCleared();
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

        // 失敗死亡的視覺回饋 (純視覺,不影響邏輯/碰撞):
        // 玩家位置爆裂粒子 + 螢幕震動 + 紅色全螢幕閃光,並隱藏主角圖片。
        if (_player != null)
        {
            JuiceFX.DeathBurst(_player.transform.position, new Color(1f, 0.35f, 0.25f, 1f));
            if (_pc != null) _pc.SetRendererVisible(false);
        }
        JuiceFX.Shake(0.42f, 0.35f);
        JuiceFX.ScreenFlash(new Color(0.7f, 0f, 0f, 0.45f), 0.5f);

        Time.timeScale = 1f;
        if (_pc != null) _pc.enabled = false;
        if (_rb != null) _rb.linearVelocity = Vector2.zero;
    }

    public void ResetLevel()
    {
        StopAllCoroutines();
        _dying = false;

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
        if (_pc != null)
        {
            _pc.enabled = true;
            _pc.SetRendererVisible(true); // 失敗死亡曾隱藏主角圖片,重來時確保恢復顯示
        }
        if (_player != null && spawnPoint != null)
        {
            _player.transform.position = spawnPoint.position;
            if (_rb != null) _rb.linearVelocity = Vector2.zero;
        }

        if (skillSystem != null) skillSystem.ResetMemory();
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

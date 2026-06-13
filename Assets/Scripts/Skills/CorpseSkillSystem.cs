using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 屍體技能卡系統 (可重用、與關卡管理器解耦)。
///
/// 對外只提供三個接口:
///   - BeginSelection(onChosen):抽牌、暫停遊戲、跳出選卡 UI,玩家選定後以 callback 回傳技能。
///   - ApplySkill(corpse, skill):把對應技能行為 (彈跳 / 移動 / 傳送) 與顏色掛到屍體上。
///   - IsBusy:是否正在選卡 (管理器可據此暫停自身邏輯)。
///
/// 本元件不認識任何關卡管理器。掛在共用的 GameManager 上,每關只需各自設定牌庫即可。
/// 採「先抽卡 → 再玩」:管理器在每條命/每輪開始時呼叫 BeginSelection。
/// </summary>
public class CorpseSkillSystem : MonoBehaviour
{
    [Header("技能卡牌庫 (每關各自設定)")]
    [Tooltip("本關總卡牌:每筆 = 技能種類 + 數量(抽牌權重)。留空 = 停用技能系統。")]
    public List<CorpseSkillCard> deck = new List<CorpseSkillCard>();

    [Tooltip("每次抽出幾張供玩家選 (例如 3 = 三選一)。種類不足時自動變少。")]
    [Min(1)]
    public int displayCount = 3;

    [Header("技能參數")]
    [Tooltip("加速倍率 (X = 水平移動速度, Y = 跳躍力)")]
    public Vector2 speedMultiplier = new Vector2(2f, 2f);

    [Tooltip("加速離開後每秒遞減的倍率量 (例如 2 = 0.5 秒回到原速)")]
    public float speedDecayPerSecond = 2f;

    [Tooltip("移動屍體的單程距離")]
    public float moverDistance = 3f;

    [Tooltip("移動屍體的速度")]
    public float moverSpeed = 2f;

    [Tooltip("地面 / 平台圖層 (移動折返與向下傳送會用到)。留空(-1)會自動抓 Ground。")]
    public int groundLayer = -1;

    [Header("屍體顏色 (依技能區分,會保留原本透明度)")]
    public Color colorNormal = new Color(0.8f, 0.35f, 0.35f);
    public Color colorSpeed = new Color(0.35f, 0.85f, 0.4f);
    public Color colorHorizontal = new Color(0.4f, 0.6f, 0.95f);
    public Color colorVertical = new Color(0.95f, 0.75f, 0.3f);
    public Color colorTeleport = new Color(0.75f, 0.4f, 0.9f);

    /// <summary>是否正在選卡 (選卡期間管理器應暫停自身死亡/計時邏輯)。</summary>
    public bool IsBusy { get; private set; }

    /// <summary>目前已選定、將套用到下一具屍體的技能。</summary>
    public CorpseSkillType ArmedSkill { get; private set; } = CorpseSkillType.Normal;

    private readonly List<CorpseSkillType> _hand = new List<CorpseSkillType>();
    private Action<CorpseSkillType> _onChosen;

    private void Awake()
    {
        if (groundLayer < 0)
        {
            int g = LayerMask.NameToLayer("Ground");
            groundLayer = g >= 0 ? g : 0;
        }
    }

    private int GroundMask => groundLayer >= 0 ? (1 << groundLayer) : ~0;

    /// <summary>牌庫是否有可用的卡。</summary>
    public bool HasUsableDeck()
    {
        if (deck == null) return false;
        foreach (var c in deck)
            if (c != null && c.count > 0) return true;
        return false;
    }

    /// <summary>
    /// 開始選卡:抽牌、暫停、顯示 UI。玩家選定後呼叫 onChosen(技能)。
    /// 若牌庫為空則直接回傳 Normal,不暫停、不顯示 UI。
    /// </summary>
    public void BeginSelection(Action<CorpseSkillType> onChosen)
    {
        if (!HasUsableDeck())
        {
            ArmedSkill = CorpseSkillType.Normal;
            onChosen?.Invoke(ArmedSkill);
            return;
        }

        DrawHand();
        if (_hand.Count == 0)
        {
            ArmedSkill = CorpseSkillType.Normal;
            onChosen?.Invoke(ArmedSkill);
            return;
        }

        _onChosen = onChosen;
        IsBusy = true;
        Time.timeScale = 0f;
    }

    private void Update()
    {
        if (!IsBusy) return;

        var kb = Keyboard.current;
        if (kb == null) return;

        // 數字鍵 1~9 快速選 (滑鼠點擊在 OnGUI 處理)
        for (int i = 0; i < _hand.Count && i < 9; i++)
        {
            var key = kb[Key.Digit1 + i];
            if (key != null && key.wasPressedThisFrame)
            {
                Choose(i);
                break;
            }
        }
    }

    private void Choose(int index)
    {
        if (index < 0 || index >= _hand.Count) return;

        ArmedSkill = _hand[index];
        IsBusy = false;
        Time.timeScale = 1f;

        var cb = _onChosen;
        _onChosen = null;
        cb?.Invoke(ArmedSkill);
    }

    /// <summary>依數量權重隨機抽出 displayCount 張不重複種類的卡。</summary>
    private void DrawHand()
    {
        _hand.Clear();

        var weights = new Dictionary<CorpseSkillType, int>();
        foreach (var c in deck)
        {
            if (c == null || c.count <= 0) continue;
            weights.TryGetValue(c.type, out int w);
            weights[c.type] = w + c.count;
        }
        if (weights.Count == 0) return;

        var pool = new List<KeyValuePair<CorpseSkillType, int>>(weights);
        int take = Mathf.Min(displayCount, pool.Count);

        for (int n = 0; n < take; n++)
        {
            int total = 0;
            foreach (var kv in pool) total += kv.Value;
            int roll = UnityEngine.Random.Range(0, total);

            int idx = 0, acc = 0;
            for (int i = 0; i < pool.Count; i++)
            {
                acc += pool[i].Value;
                if (roll < acc) { idx = i; break; }
            }

            _hand.Add(pool[idx].Key);
            pool.RemoveAt(idx); // 不重複種類
        }
    }

    /// <summary>把技能行為與顏色掛到屍體 (殘影) 上。可被任何管理器呼叫。</summary>
    public void ApplySkill(GameObject corpse, CorpseSkillType skill)
    {
        if (corpse == null) return;

        // 重新上色 (保留原透明度)
        var sr = corpse.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            Color c = ColorFor(skill);
            c.a = sr.color.a;
            sr.color = c;
        }

        var col = corpse.GetComponent<BoxCollider2D>();

        switch (skill)
        {
            case CorpseSkillType.Speed:
                var spd = corpse.AddComponent<CorpseSkill_Speed>();
                spd.Configure(speedMultiplier, speedDecayPerSecond);
                break;

            case CorpseSkillType.HorizontalSway:
                var mh = corpse.AddComponent<CorpseSkill_Mover>();
                mh.Configure(CorpseSkill_Mover.MoveAxis.Horizontal, moverDistance, moverSpeed, GroundMask);
                break;

            case CorpseSkillType.VerticalSway:
                var mv = corpse.AddComponent<CorpseSkill_Mover>();
                mv.Configure(CorpseSkill_Mover.MoveAxis.Vertical, moverDistance, moverSpeed, GroundMask);
                break;

            case CorpseSkillType.TeleportDown:
                if (col != null) col.isTrigger = true; // 設為傳送門,玩家可穿過
                corpse.layer = 0;                      // 移出 Ground 層,不被當平台踩
                var tp = corpse.AddComponent<CorpseSkill_TeleportDown>();
                tp.Configure(GroundMask);
                break;

            case CorpseSkillType.Normal:
            default:
                break;
        }
    }

    public Color ColorFor(CorpseSkillType skill)
    {
        switch (skill)
        {
            case CorpseSkillType.Speed: return colorSpeed;
            case CorpseSkillType.HorizontalSway: return colorHorizontal;
            case CorpseSkillType.VerticalSway: return colorVertical;
            case CorpseSkillType.TeleportDown: return colorTeleport;
            default: return colorNormal;
        }
    }

    private void OnGUI()
    {
        if (!IsBusy) return;

        // 半透明遮罩
        var prev = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.5f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = prev;

        var title = new GUIStyle(GUI.skin.label) { fontSize = 34, fontStyle = FontStyle.Bold };
        title.normal.textColor = Color.white;
        title.alignment = TextAnchor.MiddleCenter;
        GUI.Label(new Rect(0, Screen.height * 0.18f, Screen.width, 50),
            "選擇本命技能 (這條命的屍體)", title);

        int n = _hand.Count;
        float cardW = 200f, cardH = 120f, gap = 30f;
        float totalW = n * cardW + (n - 1) * gap;
        float startX = (Screen.width - totalW) * 0.5f;
        float y = Screen.height * 0.40f;

        var btnStyle = new GUIStyle(GUI.skin.button) { fontSize = 26, fontStyle = FontStyle.Bold };

        for (int i = 0; i < n; i++)
        {
            float x = startX + i * (cardW + gap);
            string label = (i + 1) + ". " + CorpseSkillNames.ToDisplay(_hand[i]);
            if (GUI.Button(new Rect(x, y, cardW, cardH), label, btnStyle))
                Choose(i);
        }

        var hint = new GUIStyle(GUI.skin.label) { fontSize = 20 };
        hint.normal.textColor = new Color(0.85f, 0.85f, 0.85f);
        hint.alignment = TextAnchor.MiddleCenter;
        GUI.Label(new Rect(0, y + cardH + 20f, Screen.width, 30),
            "點擊卡片或按數字鍵 1~" + n + " 選擇", hint);
    }
}

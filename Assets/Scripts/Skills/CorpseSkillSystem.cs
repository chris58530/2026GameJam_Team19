using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 屍體技能卡系統 (可重用、與關卡管理器解耦)。
///
/// 對外只提供三個接口:
///   - BeginSelection(onChosen):依模式抽卡,玩家確定後以 callback 回傳技能。
///   - ApplySkill(corpse, skill):把對應技能行為與顏色掛到屍體上。
///   - IsBusy:是否正在選卡 (管理器可據此暫停自身邏輯)。
///
/// 兩種選卡模式 (selectionMode):
///   - Classic:抽 displayCount 張,玩家點選 / 按數字鍵挑一張 (OnGUI)。
///   - Slot:拉霸滾輪,單張卡快速循環後減速停在「預先決定的結果」上,
///           DOTween 衝擊回饋。可自動停或玩家按鍵提前停,但結果不變。
///
/// 採「先抽卡 → 再玩」:管理器在每條命/每輪開始時呼叫 BeginSelection。
/// </summary>
public class CorpseSkillSystem : MonoBehaviour
{
    public enum SelectionMode { Classic, Slot }

    [Header("選卡模式")]
    [Tooltip("Classic = 玩家三選一;Slot = 拉霸滾輪隨機抽")]
    public SelectionMode selectionMode = SelectionMode.Classic;

    [Header("技能卡牌庫 (每關各自設定)")]
    [Tooltip("本關總卡牌:每筆 = 技能種類 + 數量(抽牌權重)。留空 = 停用技能系統。")]
    public List<CorpseSkillCard> deck = new List<CorpseSkillCard>();

    [Tooltip("Classic 模式每次抽出幾張供玩家選 (Slot 模式無效)。")]
    [Min(1)]
    public int displayCount = 3;

    [Header("拉霸設定 (Slot 模式)")]
    [Tooltip("是否允許玩家按鍵(空白/K)提前停止;結果不受按下時機影響")]
    public bool slotAllowManualStop = true;

    [Tooltip("快轉階段每格間隔秒數")]
    public float slotFastInterval = 0.2f;

    [Tooltip("快轉最短持續秒數(即使立刻按停也至少轉這麼久)")]
    public float slotMinFastTime = 0.6f;

    [Tooltip("快轉最長持續秒數(沒按停就自動進入減速)")]
    public float slotMaxFastTime = 1.2f;

    [Tooltip("減速階段的格數(逐格變慢直到停在結果)")]
    public int slotDecelTicks = 6;

    [Tooltip("減速到最後一格的最大間隔秒數")]
    public float slotMaxInterval = 0.45f;

    [Tooltip("停在結果後的停留秒數(讓玩家看清楚)")]
    public float slotResultHold = 0.35f;

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

    // Classic 狀態
    private readonly List<CorpseSkillType> _hand = new List<CorpseSkillType>();
    private Action<CorpseSkillType> _onChosen;

    // Slot 狀態
    private CorpseSkillType _slotResult;
    private bool _slotStopRequested;
    private Coroutine _slotRoutine;

    // Slot uGUI
    private GameObject _uiRoot;
    private RectTransform _cardRect;
    private Image _cardImage;
    private Image _overlay;
    private Text _cardLabel;
    private Text _titleLabel;

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
    /// 開始選卡。玩家確定後呼叫 onChosen(技能)。
    /// 牌庫為空則直接回傳 Normal,不暫停、不顯示 UI。
    /// </summary>
    public void BeginSelection(Action<CorpseSkillType> onChosen)
    {
        if (!HasUsableDeck())
        {
            ArmedSkill = CorpseSkillType.Normal;
            onChosen?.Invoke(ArmedSkill);
            return;
        }

        _onChosen = onChosen;

        if (selectionMode == SelectionMode.Slot)
            BeginSlot();
        else
            BeginClassic();
    }

    // ---------------- Classic ----------------

    private void BeginClassic()
    {
        DrawHand();
        if (_hand.Count == 0)
        {
            ArmedSkill = CorpseSkillType.Normal;
            FinishSelection(ArmedSkill);
            return;
        }

        IsBusy = true;
        Time.timeScale = 0f;
    }

    private void Update()
    {
        if (!IsBusy) return;

        var kb = Keyboard.current;
        if (kb == null) return;

        if (selectionMode == SelectionMode.Classic)
        {
            for (int i = 0; i < _hand.Count && i < 9; i++)
            {
                var key = kb[Key.Digit1 + i];
                if (key != null && key.wasPressedThisFrame)
                {
                    ChooseClassic(i);
                    break;
                }
            }
        }
        else // Slot:按鍵提前停
        {
            if (slotAllowManualStop && !_slotStopRequested &&
                (kb.spaceKey.wasPressedThisFrame || kb.kKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame))
            {
                _slotStopRequested = true;
            }
        }
    }

    private void ChooseClassic(int index)
    {
        if (index < 0 || index >= _hand.Count) return;
        FinishSelection(_hand[index]);
    }

    // ---------------- Slot ----------------

    private void BeginSlot()
    {
        _slotResult = WeightedDrawOne();
        _slotStopRequested = false;
        IsBusy = true;
        Time.timeScale = 0f;

        EnsureUI();
        _uiRoot.SetActive(true);
        if (_slotRoutine != null) StopCoroutine(_slotRoutine);
        _slotRoutine = StartCoroutine(SlotRoutine());
    }

    private IEnumerator SlotRoutine()
    {
        var cycle = DistinctDeckTypes();
        if (cycle.Count == 0) { FinishSlot(CorpseSkillType.Normal); yield break; }

        int idx = 0;

        // 1) 快轉:固定間隔,直到最短時間後若按停或達最長時間就進入減速
        float fast = 0f;
        while (true)
        {
            ShowCardType(cycle[idx % cycle.Count]);
            TickPulse();
            idx++;
            yield return new WaitForSecondsRealtime(slotFastInterval);
            fast += slotFastInterval;

            bool canStop = fast >= slotMinFastTime;
            if (canStop && (_slotStopRequested || fast >= slotMaxFastTime))
                break;
        }

        // 2) 減速:逐格拉長間隔,最後一格落在結果
        int ticks = Mathf.Max(1, slotDecelTicks);
        for (int i = 0; i < ticks; i++)
        {
            bool last = (i == ticks - 1);
            ShowCardType(last ? _slotResult : cycle[idx % cycle.Count]);
            TickPulse();
            idx++;

            float t = (i + 1f) / ticks;
            float interval = Mathf.Lerp(slotFastInterval, slotMaxInterval, t);
            yield return new WaitForSecondsRealtime(interval);
        }

        // 3) 確保停在結果 + 衝擊回饋
        ShowCardType(_slotResult);
        yield return StartCoroutine(ImpactFeedback());

        // 4) 停留後完成
        yield return new WaitForSecondsRealtime(slotResultHold);
        FinishSlot(_slotResult);
    }

    /// <summary>轉動時每格的小脈動。</summary>
    private void TickPulse()
    {
        if (_cardRect == null) return;
        _cardRect.DOComplete();
        _cardRect.localScale = Vector3.one;
        _cardRect.DOPunchScale(Vector3.one * 0.06f, 0.08f, 0, 0f).SetUpdate(true);
    }

    /// <summary>停在結果的衝擊回饋:蓄力縮小 → OutBack 彈回放大 + 閃光。</summary>
    private IEnumerator ImpactFeedback()
    {
        if (_cardRect == null) yield break;

        _cardRect.DOComplete();

        // 蓄力縮小
        _cardRect.localScale = Vector3.one;
        _cardRect.DOScale(0.85f, 0.07f).SetUpdate(true).SetEase(Ease.OutQuad);
        yield return new WaitForSecondsRealtime(0.07f);

        // 閃白 → 回到技能色 (DOTween UI 模組)
        if (_cardImage != null)
        {
            Color target = ColorFor(_slotResult); target.a = 1f;
            _cardImage.color = Color.white;
            _cardImage.DOColor(target, 0.35f).SetUpdate(true);
        }

        // 衝擊放大 (overshoot 回彈)
        _cardRect.DOScale(1f, 0.35f).SetUpdate(true).SetEase(Ease.OutBack);
        yield return new WaitForSecondsRealtime(0.35f);
    }

    private void FinishSlot(CorpseSkillType result)
    {
        if (_uiRoot != null) _uiRoot.SetActive(false);
        _slotRoutine = null;
        FinishSelection(result);
    }

    // ---------------- 共用收尾 ----------------

    private void FinishSelection(CorpseSkillType skill)
    {
        ArmedSkill = skill;
        IsBusy = false;
        Time.timeScale = 1f;

        var cb = _onChosen;
        _onChosen = null;
        cb?.Invoke(ArmedSkill);
    }

    // ---------------- 抽牌 ----------------

    private Dictionary<CorpseSkillType, int> BuildWeights()
    {
        var weights = new Dictionary<CorpseSkillType, int>();
        foreach (var c in deck)
        {
            if (c == null || c.count <= 0) continue;
            weights.TryGetValue(c.type, out int w);
            weights[c.type] = w + c.count;
        }
        return weights;
    }

    private List<CorpseSkillType> DistinctDeckTypes()
    {
        var list = new List<CorpseSkillType>();
        foreach (var kv in BuildWeights()) list.Add(kv.Key);
        return list;
    }

    /// <summary>依權重抽一張。</summary>
    private CorpseSkillType WeightedDrawOne()
    {
        var weights = BuildWeights();
        int total = 0;
        foreach (var kv in weights) total += kv.Value;
        if (total <= 0) return CorpseSkillType.Normal;

        int roll = UnityEngine.Random.Range(0, total);
        int acc = 0;
        foreach (var kv in weights)
        {
            acc += kv.Value;
            if (roll < acc) return kv.Key;
        }
        return CorpseSkillType.Normal;
    }

    /// <summary>Classic:依權重抽出 displayCount 張不重複種類的卡。</summary>
    private void DrawHand()
    {
        _hand.Clear();
        var pool = new List<KeyValuePair<CorpseSkillType, int>>(BuildWeights());
        if (pool.Count == 0) return;

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
            pool.RemoveAt(idx);
        }
    }

    // ---------------- 套用技能 ----------------

    /// <summary>把技能行為與顏色掛到屍體 (殘影) 上。可被任何管理器呼叫。</summary>
    public void ApplySkill(GameObject corpse, CorpseSkillType skill)
    {
        if (corpse == null) return;

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
                if (col != null) col.isTrigger = true;
                corpse.layer = 0;
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

    // ---------------- Classic OnGUI ----------------

    private void OnGUI()
    {
        if (!IsBusy || selectionMode != SelectionMode.Classic) return;

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
                ChooseClassic(i);
        }

        var hint = new GUIStyle(GUI.skin.label) { fontSize = 20 };
        hint.normal.textColor = new Color(0.85f, 0.85f, 0.85f);
        hint.alignment = TextAnchor.MiddleCenter;
        GUI.Label(new Rect(0, y + cardH + 20f, Screen.width, 30),
            "點擊卡片或按數字鍵 1~" + n + " 選擇", hint);
    }

    // ---------------- Slot uGUI 建構 ----------------

    private void EnsureUI()
    {
        if (_uiRoot != null) return;

        _uiRoot = new GameObject("SkillSlotCanvas");
        _uiRoot.transform.SetParent(transform, false);

        var canvas = _uiRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        var scaler = _uiRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        _uiRoot.AddComponent<GraphicRaycaster>();

        // 半透明遮罩
        _overlay = CreateImage("Overlay", _uiRoot.transform, new Color(0f, 0f, 0f, 0.6f));
        StretchFull(_overlay.rectTransform);

        // 標題
        _titleLabel = CreateText("Title", _uiRoot.transform, "抽取技能", 54, Color.white);
        var tr = _titleLabel.rectTransform;
        tr.anchorMin = tr.anchorMax = new Vector2(0.5f, 0.5f);
        tr.pivot = new Vector2(0.5f, 0.5f);
        tr.anchoredPosition = new Vector2(0, 230);
        tr.sizeDelta = new Vector2(800, 80);

        // 卡片
        _cardImage = CreateImage("Card", _uiRoot.transform, colorNormal);
        _cardRect = _cardImage.rectTransform;
        _cardRect.anchorMin = _cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        _cardRect.pivot = new Vector2(0.5f, 0.5f);
        _cardRect.anchoredPosition = Vector2.zero;
        _cardRect.sizeDelta = new Vector2(280, 360);

        // 卡片技能名
        _cardLabel = CreateText("CardLabel", _cardRect, "", 46, Color.white);
        StretchFull(_cardLabel.rectTransform);

        _uiRoot.SetActive(false);
    }

    private void ShowCardType(CorpseSkillType type)
    {
        if (_cardImage == null) return;
        Color c = ColorFor(type); c.a = 1f;
        _cardImage.color = c;
        if (_cardLabel != null) _cardLabel.text = CorpseSkillNames.ToDisplay(type);
    }

    private static Image CreateImage(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        return img;
    }

    private static Text CreateText(string name, Transform parent, string text, int fontSize, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.text = text;
        t.fontSize = fontSize;
        t.color = color;
        t.alignment = TextAnchor.MiddleCenter;
        t.fontStyle = FontStyle.Bold;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.font = GetBuiltinFont();
        return t;
    }

    private static Font _builtinFont;
    private static Font GetBuiltinFont()
    {
        if (_builtinFont != null) return _builtinFont;
        _builtinFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (_builtinFont == null) _builtinFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return _builtinFont;
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}

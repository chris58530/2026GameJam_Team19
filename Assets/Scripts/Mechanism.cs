using UnityEngine;

/// <summary>
/// 機關的效果類型。
/// Gate          = 閘門:觸發時滑開 + 關閉碰撞(人可通過)+ 變色;放開則關回擋人。
/// MovingPlatform = 移動平台/升降台:觸發時移動到目標位置,碰撞一直保留(可站上去);放開則移回。
/// </summary>
public enum MechanismMode
{
    Gate,
    MovingPlatform
}

/// <summary>觸發時的移動方向。Custom 時使用 customOffset。</summary>
public enum OpenDirection
{
    Up,
    Down,
    Left,
    Right,
    Custom
}

/// <summary>
/// 通用機關:由一組按鈕觸發,依 mode 產生不同效果。可重複套用。
/// 把要控制它的按鈕拖進 triggers,選好 mode,設定 openOffset(觸發後移動到哪)即可。
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class Mechanism : MonoBehaviour
{
    [Header("觸發條件")]
    [Tooltip("控制這個機關的按鈕 (就是條件陣列)")]
    public PressButton[] triggers;

    [Tooltip("true = 全部壓下才觸發;false = 任一壓下就觸發")]
    public bool requireAll = true;

    [Header("效果類型")]
    [Tooltip("Gate = 閘門 (開啟可通過);MovingPlatform = 移動平台 (始終可踩)")]
    public MechanismMode mode = MechanismMode.Gate;

    [Header("觸發動作")]
    [Tooltip("觸發時的移動方向 (Custom 時用下面的自訂位移)")]
    public OpenDirection direction = OpenDirection.Up;

    [Tooltip("移動距離 (世界單位)")]
    public float distance = 2.7f;

    [Tooltip("方向選 Custom 時使用的自訂位移")]
    public Vector2 customOffset = new Vector2(0f, 3f);

    [Tooltip("移動速度")]
    public float moveSpeed = 14f;

    [Header("Gate 專用視覺")]
    public Color closedColor = new Color(0.7f, 0.25f, 0.25f);
    public Color openColor = new Color(0.3f, 0.75f, 0.4f);

    /// <summary>機關目前是否被觸發 (開啟)。</summary>
    public bool IsActive { get; private set; }

    private Vector3 _closedPos;
    private Vector3 _openPos;
    private Collider2D _col;
    private SpriteRenderer _sr;
    private bool _prevActive;

    private void Awake()
    {
        _closedPos = transform.position;
        _openPos = _closedPos + (Vector3)GetOffset();
        _col = GetComponent<Collider2D>();
        _sr = GetComponent<SpriteRenderer>();
    }

    /// <summary>依方向/距離 (或 Custom) 算出觸發後的位移。</summary>
    public Vector2 GetOffset()
    {
        switch (direction)
        {
            case OpenDirection.Up: return Vector2.up * distance;
            case OpenDirection.Down: return Vector2.down * distance;
            case OpenDirection.Left: return Vector2.left * distance;
            case OpenDirection.Right: return Vector2.right * distance;
            default: return customOffset;
        }
    }

    private void Update()
    {
        IsActive = Evaluate();

        // 狀態改變時播放音效
        if (IsActive && !_prevActive)
        {
            if (SoundManager.Instance != null)
                SoundManager.Instance.PlaySFX(mode == MechanismMode.Gate ? "GateOpen" : "PlatformMove");
        }
        else if (!IsActive && _prevActive)
        {
            if (SoundManager.Instance != null)
                SoundManager.Instance.PlaySFX(mode == MechanismMode.Gate ? "GateClose" : "PlatformMove");
        }
        _prevActive = IsActive;

        Vector3 target = IsActive ? _openPos : _closedPos;
        transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);

        if (mode == MechanismMode.Gate)
        {
            // 閘門:開啟時可通過 + 變色
            if (_col != null) _col.enabled = !IsActive;
            if (_sr != null) _sr.color = IsActive ? openColor : closedColor;
        }
        else // MovingPlatform
        {
            // 移動平台:碰撞一直保留可踩,不變色
            if (_col != null) _col.enabled = true;
        }
    }

    private bool Evaluate()
    {
        if (triggers == null || triggers.Length == 0) return false;

        if (requireAll)
        {
            foreach (var t in triggers)
                if (t == null || !t.IsPressed) return false;
            return true;
        }
        else
        {
            foreach (var t in triggers)
                if (t != null && t.IsPressed) return true;
            return false;
        }
    }
}

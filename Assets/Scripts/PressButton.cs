using UnityEngine;

/// <summary>
/// 按鈕:偵測上方是否有玩家或殘影壓著 (Physics2D.OverlapBox)。
/// 不需要實體碰撞器,純粹當作偵測區 + 視覺。
/// </summary>
public class PressButton : MonoBehaviour
{
    [Tooltip("按鈕代號 (A/B/C)")]
    public string id = "A";

    [Tooltip("辨識玩家用的 Tag")]
    public string playerTag = "Player";

    [Tooltip("偵測區相對按鈕中心的偏移")]
    public Vector2 checkOffset = new Vector2(0f, 0.5f);

    [Tooltip("偵測區大小")]
    public Vector2 checkSize = new Vector2(1.0f, 1.0f);

    public bool IsPressed { get; private set; }

    private SpriteRenderer _sr;
    private Color _baseColor;
    private Vector3 _baseScale;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        if (_sr != null) _baseColor = _sr.color;
        _baseScale = transform.localScale;
    }

    private void FixedUpdate()
    {
        Vector2 center = (Vector2)transform.position + checkOffset;
        var hits = Physics2D.OverlapBoxAll(center, checkSize, 0f);
        bool pressed = false;
        foreach (var h in hits)
        {
            if (h == null) continue;
            if (h.CompareTag(playerTag) || h.GetComponent<Ghost>() != null)
            {
                pressed = true;
                break;
            }
        }

        // 狀態改變時播放音效
        if (pressed && !IsPressed)
        {
            if (SoundManager.Instance != null)
                SoundManager.Instance.PlaySFX("ButtonPress");
        }
        else if (!pressed && IsPressed)
        {
            if (SoundManager.Instance != null)
                SoundManager.Instance.PlaySFX("ButtonRelease");
        }

        IsPressed = pressed;
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (_sr != null)
            _sr.color = IsPressed ? Color.Lerp(_baseColor, Color.white, 0.5f) : _baseColor;

        // 壓下時壓扁
        transform.localScale = IsPressed
            ? new Vector3(_baseScale.x, _baseScale.y * 0.5f, _baseScale.z)
            : _baseScale;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube((Vector2)transform.position + checkOffset, checkSize);
    }
}

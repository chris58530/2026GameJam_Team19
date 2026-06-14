using UnityEngine;

/// <summary>
/// 限次使用屍體 (冰結)。
/// 屍體平時是可踩的實體平台,但「每次踩上/觸碰」算一次使用。
/// 用滿 maxUses 次後屍體消失。剩餘次數以透明度提示:
/// 每用掉一次,Sprite 透明度往下遞減一階,最後一次用完後銷毀。
/// 玩家離開後再次接觸才會再計一次 (連續貼著不會持續扣)。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class CorpseSkill_LimitedUse : MonoBehaviour
{
    [Tooltip("可使用的總次數")]
    [Min(1)]
    public int maxUses = 3;

    [Tooltip("用滿後的銷毀延遲 (秒,0 = 立即)")]
    public float destroyDelay = 0f;

    [Tooltip("玩家 Tag")]
    public string playerTag = "Player";

    private SpriteRenderer _sr;
    private int _usesLeft;
    private bool _playerInContact;
    private bool _consumed;

    public void Configure(int uses)
    {
        maxUses = Mathf.Max(1, uses);
    }

    private void Awake()
    {
        _sr = GetComponentInChildren<SpriteRenderer>();
    }

    private void Start()
    {
        _usesLeft = Mathf.Max(1, maxUses);
        ApplyAlpha();
    }

    private void OnCollisionEnter2D(Collision2D c) => HandleEnter(c.collider);
    private void OnCollisionExit2D(Collision2D c) => HandleExit(c.collider);
    private void OnTriggerEnter2D(Collider2D other) => HandleEnter(other);
    private void OnTriggerExit2D(Collider2D other) => HandleExit(other);

    private void HandleEnter(Collider2D other)
    {
        if (_consumed) return;
        if (other == null || !other.CompareTag(playerTag)) return;
        if (_playerInContact) return; // 同一次接觸只算一次

        _playerInContact = true;
        ConsumeOne();
    }

    private void HandleExit(Collider2D other)
    {
        if (other == null || !other.CompareTag(playerTag)) return;
        _playerInContact = false;
    }

    private void ConsumeOne()
    {
        _usesLeft--;
        if (_usesLeft <= 0)
        {
            _consumed = true;
            if (destroyDelay > 0f) Destroy(gameObject, destroyDelay);
            else Destroy(gameObject);
            return;
        }
        ApplyAlpha();
    }

    /// <summary>依剩餘次數設定透明度:剩越少越透明 (剩 maxUses = 不透明)。</summary>
    private void ApplyAlpha()
    {
        if (_sr == null) return;
        float a = Mathf.Clamp01((float)_usesLeft / Mathf.Max(1, maxUses));
        Color c = _sr.color;
        c.a = a;
        _sr.color = c;
    }
}

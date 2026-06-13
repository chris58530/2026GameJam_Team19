using UnityEngine;

/// <summary>
/// 向下傳送屍體 (傳送門)。
/// - 本屍體的 collider 設為 Trigger,玩家經過 (進入範圍) 時觸發。
/// - 從屍體位置向正下方發射射線,找到第一個「平台」(地面圖層),
///   把玩家傳送到該平台上方。
/// - 內建短冷卻,避免落點仍在範圍內造成連續傳送。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class CorpseSkill_TeleportDown : MonoBehaviour
{
    [Tooltip("往下偵測平台的圖層 (地面)")]
    public LayerMask groundMask = ~0;

    [Tooltip("往下偵測的最大距離")]
    public float maxCastDistance = 50f;

    [Tooltip("傳送後玩家相對平台頂端的高度偏移 (約玩家半身高)")]
    public float landingOffset = 0.6f;

    [Tooltip("傳送後的冷卻秒數")]
    public float cooldown = 0.5f;

    [Tooltip("玩家 Tag")]
    public string playerTag = "Player";

    private float _cooldownTimer;

    public void Configure(LayerMask ground)
    {
        groundMask = ground;
    }

    private void Update()
    {
        if (_cooldownTimer > 0f) _cooldownTimer -= Time.deltaTime;
    }

    private void OnTriggerEnter2D(Collider2D other) => TryTeleport(other);
    private void OnTriggerStay2D(Collider2D other) => TryTeleport(other);

    private void TryTeleport(Collider2D other)
    {
        if (_cooldownTimer > 0f) return;
        if (!other.CompareTag(playerTag)) return;

        Vector2 origin = transform.position;

        // 從屍體稍微往下一點開始射線,避免打到自己 (即使是 Trigger 也保險)
        float skin = 0.05f;
        RaycastHit2D hit = Physics2D.Raycast(
            origin + Vector2.down * skin, Vector2.down, maxCastDistance, groundMask);

        if (hit.collider == null) return;
        if (hit.collider.gameObject == gameObject) return;

        Vector3 dest = new Vector3(hit.point.x, hit.point.y + landingOffset, other.transform.position.z);

        var rb = other.attachedRigidbody;
        if (rb != null)
        {
            rb.position = dest;
            rb.linearVelocity = Vector2.zero;
        }
        else
        {
            other.transform.position = dest;
        }

        _cooldownTimer = cooldown;
    }
}

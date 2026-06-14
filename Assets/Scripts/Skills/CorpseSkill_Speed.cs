using UnityEngine;

/// <summary>
/// 加速屍體 (僅影響水平移動速度)。
/// 玩家碰撞器接觸到這具屍體時,呼叫玩家控制器套用水平速度倍率。
/// 接觸期間維持滿倍率,離開後由玩家端隨時間遞減回原速。
/// 屍體本身仍是可踩的實體平台。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class CorpseSkill_Speed : MonoBehaviour
{
    [Tooltip("水平移動速度倍率")]
    public float speedMultiplier = 2f;

    [Tooltip("離開後每秒遞減的倍率量 (例如 2 = 從 2 倍在 0.5 秒內回到 1 倍)")]
    public float decayPerSecond = 2f;

    [Tooltip("玩家 Tag")]
    public string playerTag = "Player";

    public void Configure(float multiplier, float decay)
    {
        speedMultiplier = multiplier;
        decayPerSecond = decay;
    }

    private void OnCollisionEnter2D(Collision2D c) => TryBoost(c.collider);
    private void OnCollisionStay2D(Collision2D c) => TryBoost(c.collider);

    private void TryBoost(Collider2D other)
    {
        if (other == null || !other.CompareTag(playerTag)) return;

        var pc = other.GetComponent<PlayerController2D>();
        if (pc == null && other.attachedRigidbody != null)
            pc = other.attachedRigidbody.GetComponent<PlayerController2D>();

        if (pc != null) pc.RefreshSpeedBoost(speedMultiplier, decayPerSecond);
    }
}

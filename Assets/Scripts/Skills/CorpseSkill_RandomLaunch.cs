using UnityEngine;

/// <summary>
/// 隨機彈射屍體 (百威)。
/// 玩家碰撞器一接觸到這具屍體,立刻朝「上半圓的隨機角度」彈飛。
/// 彈飛力道沿用玩家本身的原始跳躍力 (jumpForce),所以距離/高度 = 原始設定。
/// 內建短冷卻,避免接觸的同一瞬間連續觸發。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class CorpseSkill_RandomLaunch : MonoBehaviour
{
    [Tooltip("隨機彈飛角度下限 (度,從 +X 軸逆時針量;90 = 正上方)")]
    public float minAngle = 50f;

    [Tooltip("隨機彈飛角度上限 (度)")]
    public float maxAngle = 130f;

    [Tooltip("觸發後的冷卻秒數 (避免同一次接觸連續彈射)")]
    public float cooldown = 0.4f;

    [Tooltip("找不到玩家原始跳躍力時的後備力道")]
    public float fallbackForce = 14f;

    [Tooltip("玩家 Tag")]
    public string playerTag = "Player";

    private float _cooldownTimer;

    public void Configure(float min, float max, float cd)
    {
        minAngle = min;
        maxAngle = max;
        cooldown = cd;
    }

    private void Update()
    {
        if (_cooldownTimer > 0f) _cooldownTimer -= Time.deltaTime;
    }

    private void OnCollisionEnter2D(Collision2D c) => TryLaunch(c.collider);
    private void OnCollisionStay2D(Collision2D c) => TryLaunch(c.collider);

    private void TryLaunch(Collider2D other)
    {
        if (_cooldownTimer > 0f) return;
        if (other == null || !other.CompareTag(playerTag)) return;

        var rb = other.attachedRigidbody;
        if (rb == null) return;

        // 力道 = 玩家原始跳躍力 (距離/高度沿用原始設定)
        float force = fallbackForce;
        var pc = rb.GetComponent<PlayerController2D>();
        if (pc != null) force = pc.jumpForce;

        // 上半圓隨機方向
        float angleDeg = Random.Range(minAngle, maxAngle);
        float rad = angleDeg * Mathf.Deg2Rad;
        Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

        rb.linearVelocity = dir * force;
        _cooldownTimer = cooldown;
    }
}

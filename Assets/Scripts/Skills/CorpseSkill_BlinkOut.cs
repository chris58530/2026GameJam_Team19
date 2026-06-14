using System.Collections;
using UnityEngine;

/// <summary>
/// 消失再現屍體 (Asahi)。
/// 玩家一碰到這具屍體,延遲 disappearDelay 秒後消失 (關閉碰撞與顯示),
/// 再過 reappearDelay 秒後重新出現,可再次觸發。
/// 屍體本身平時仍是可踩的實體平台。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class CorpseSkill_BlinkOut : MonoBehaviour
{
    [Tooltip("碰到後到消失的延遲下限 (秒)")]
    public float disappearDelayMin = 0.8f;

    [Tooltip("碰到後到消失的延遲上限 (秒)")]
    public float disappearDelayMax = 1.5f;

    [Tooltip("消失後到重新出現的秒數")]
    public float reappearDelay = 1f;

    [Tooltip("玩家 Tag")]
    public string playerTag = "Player";

    private Collider2D _col;
    private SpriteRenderer _sr;
    private bool _cycleRunning;

    public void Configure(float delayMin, float delayMax, float reappear)
    {
        disappearDelayMin = delayMin;
        disappearDelayMax = delayMax;
        reappearDelay = reappear;
    }

    private void Awake()
    {
        _col = GetComponent<Collider2D>();
        _sr = GetComponentInChildren<SpriteRenderer>();
    }

    private void OnCollisionEnter2D(Collision2D c) => TryTrigger(c.collider);
    private void OnTriggerEnter2D(Collider2D other) => TryTrigger(other);

    private void TryTrigger(Collider2D other)
    {
        if (_cycleRunning) return;
        if (other == null || !other.CompareTag(playerTag)) return;
        StartCoroutine(BlinkCycle());
    }

    private IEnumerator BlinkCycle()
    {
        _cycleRunning = true;

        float delay = Random.Range(disappearDelayMin, disappearDelayMax);
        yield return new WaitForSeconds(delay);

        SetVisible(false);
        yield return new WaitForSeconds(reappearDelay);
        SetVisible(true);

        _cycleRunning = false;
    }

    private void SetVisible(bool visible)
    {
        if (_col != null) _col.enabled = visible;
        if (_sr != null) _sr.enabled = visible;
    }
}

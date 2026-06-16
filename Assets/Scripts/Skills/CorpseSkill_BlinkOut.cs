using System.Collections;
using UnityEngine;

/// <summary>
/// Disappear-and-reappear corpse (Asahi).
/// As soon as the player touches this corpse, it disappears after a disappearDelay (turning off its collider and renderer),
/// then reappears after reappearDelay and can be triggered again.
/// The corpse is otherwise a solid platform the player can stand on.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class CorpseSkill_BlinkOut : MonoBehaviour
{
    [Tooltip("Minimum delay from being touched to disappearing (seconds)")]
    public float disappearDelayMin = 0.8f;

    [Tooltip("Maximum delay from being touched to disappearing (seconds)")]
    public float disappearDelayMax = 1.5f;

    [Tooltip("Seconds from disappearing to reappearing")]
    public float reappearDelay = 1f;

    [Tooltip("Player Tag")]
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

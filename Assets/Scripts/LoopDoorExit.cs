using UnityEngine;

/// <summary>
/// Main door: clears the level when the door is open (all three buttons pressed) and the player enters.
/// Uses OnTriggerStay2D, because the player may stand at the door first and the buttons only line up afterward.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class LoopDoorExit : MonoBehaviour
{
    public string playerTag = "Player";

    [Tooltip("Name of the sound effect played when the player reaches the door")]
    public string arriveSfxName = "Conv-1";

    private LoopManager _manager;
    private bool _sfxPlayed;

    private void Awake()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
        _manager = FindFirstObjectByType<LoopManager>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;

        if (!_sfxPlayed && SoundManager.Instance != null && !string.IsNullOrEmpty(arriveSfxName))
        {
            SoundManager.Instance.PlaySFX(arriveSfxName);
            _sfxPlayed = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        _sfxPlayed = false;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (_manager != null) _manager.TryExit();
    }
}

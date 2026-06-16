using UnityEngine;

/// <summary>
/// Main door: when the player enters, clears the level if the key has been collected, otherwise prompts that a key is needed.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class DoorExit : MonoBehaviour
{
    public string playerTag = "Player";

    [Tooltip("Name of the sound effect played when the player reaches the door")]
    public string arriveSfxName = "Conv-1";

    private DeadBodyManager _manager;
    private bool _sfxPlayed;

    private void Awake()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
        _manager = FindFirstObjectByType<DeadBodyManager>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;

        if (!_sfxPlayed && SoundManager.Instance != null && !string.IsNullOrEmpty(arriveSfxName))
        {
            SoundManager.Instance.PlaySFX(arriveSfxName);
            _sfxPlayed = true;
        }

        if (_manager != null) _manager.TryExit();
    }
}

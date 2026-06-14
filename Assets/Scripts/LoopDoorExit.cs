using UnityEngine;

/// <summary>
/// 大門:門開啟 (三按鈕全壓) 且玩家進入時通關。
/// 用 OnTriggerStay2D,因為玩家可能先站在門口、按鈕之後才湊齊。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class LoopDoorExit : MonoBehaviour
{
    public string playerTag = "Player";

    [Tooltip("玩家到達門口時播放的音效名稱")]
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

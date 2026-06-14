using UnityEngine;

/// <summary>
/// 大門:玩家進入時,若已取得鑰匙則通關,否則提示需要鑰匙。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class DoorExit : MonoBehaviour
{
    public string playerTag = "Player";

    [Tooltip("玩家到達門口時播放的音效名稱")]
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

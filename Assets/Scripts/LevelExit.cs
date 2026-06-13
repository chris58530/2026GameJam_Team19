using UnityEngine;

/// <summary>
/// 關卡出口:玩家進入觸發區時顯示提示訊息。
/// 之後可在 OnReached 內擴充為過關 / 重新載入 / 切換場景。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class LevelExit : MonoBehaviour
{
    [Tooltip("辨識玩家用的 Tag")]
    public string playerTag = "Player";

    [Tooltip("到達出口時顯示的訊息")]
    public string message = "到達出口!";

    private bool _reached;

    private void Reset()
    {
        // 確保 Collider 為觸發器
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_reached) return;
        if (!other.CompareTag(playerTag)) return;

        _reached = true;
        Debug.Log(message);
    }
}

using UnityEngine;

/// <summary>
/// 結局動畫控制器（備用）。
/// 如果你不用 VideoPlayer 而是用 Unity Animator 做結局動畫，
/// 可以在動畫最後一帧加 Animation Event 呼叫 OnEndingAnimationFinished()。
/// 
/// 目前 EndingUI 已內建 VideoPlayer 支援，影片播完會自動顯示按鈕。
/// 此腳本只在你使用 Animator（非 VideoPlayer）做結局動畫時才需要。
/// </summary>
public class EndingAnimationController : MonoBehaviour
{
    private bool hasFinished = false;

    /// <summary>
    /// 結局動畫播放完畢後呼叫此方法。
    /// 連接方式：Animation Event 在最後一帧呼叫此函式。
    /// </summary>
    public void OnEndingAnimationFinished()
    {
        if (hasFinished) return;
        hasFinished = true;

        Debug.Log("[EndingAnimationController] 結局動畫結束，顯示按鈕。");

        if (EndingUI.Instance != null)
        {
            EndingUI.Instance.ShowButtons();
        }
        else
        {
            Debug.LogWarning("[EndingAnimationController] EndingUI.Instance 不存在！");
        }
    }
}

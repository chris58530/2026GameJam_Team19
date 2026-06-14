using UnityEngine;

/// <summary>
/// 結局動畫控制器。掛在 Ending 場景的動畫物件上。
/// 動畫播放完畢後通知 EndingUI 顯示按鈕。
/// 
/// 設定方式（三種方式擇一）：
/// 
///   方式 A：使用 Unity Animation Event（推薦）
///     1. 在 Victory/Fail 動畫 Clip 的最後一帧加入 Animation Event
///     2. 選擇函式：OnEndingAnimationFinished
///     3. 播放完畢自動呼叫
/// 
///   方式 B：使用 Animator Trigger
///     1. 在 Animator 中設定 Trigger，動畫結束後呼叫此腳本
/// 
///   方式 C：不使用此腳本
///     1. 在 EndingUI 中設定 useAutoShowButtons = true
///     2. 按鈕會在 autoShowButtonsDelay 秒後自動顯示
///     3. 此時不需要 EndingAnimationController
/// 
/// 注意：如果使用此腳本，請在 EndingUI 中設定 useAutoShowButtons = false，
/// 避免按鈕提前出現。
/// </summary>
public class EndingAnimationController : MonoBehaviour
{
    private bool hasFinished = false;

    /// <summary>
    /// 結局動畫播放完畢後呼叫此方法。
    /// 
    /// 連接方式：
    ///   - Animation Event：在動畫 Clip 最後一帧新增事件，選擇此函式
    ///   - 或由其他腳本手動呼叫
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
            Debug.LogWarning("[EndingAnimationController] EndingUI.Instance 不存在！無法顯示按鈕。");
        }
    }
}

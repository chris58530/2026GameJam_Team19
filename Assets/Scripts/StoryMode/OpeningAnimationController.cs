using UnityEngine;

/// <summary>
/// 開場動畫控制器。掛在 OpeningAnimation 場景中的動畫物件上。
/// 
/// 功能：
///   - 播放開場動畫（可以是 Animator、Timeline、或簡單的等待）
///   - 動畫結束後呼叫 StoryFlowManager.Instance.StartGameLoop() 進入第一關
/// 
/// 設定方式（三種方式擇一）：
/// 
///   方式 A：使用 Unity Animation Event（推薦）
///     1. 在 Animator Controller 的動畫 Clip 最後一帧加入 Animation Event
///     2. 選擇函式：OnOpeningAnimationFinished
///     3. 播放完畢自動呼叫
/// 
///   方式 B：使用 Animator StateMachineBehaviour
///     1. 在動畫狀態結束時 SendMessage("OnOpeningAnimationFinished")
/// 
///   方式 C：使用自動計時（Inspector 設定）
///     1. 勾選 useAutoTimer = true
///     2. 設定 autoTimerDuration（秒）
///     3. 不需要 Animation Event，時間到自動切換
/// 
/// 如果沒有動畫，可以用方式 C 作為 placeholder，
/// 未來有動畫後再改成方式 A。
/// </summary>
public class OpeningAnimationController : MonoBehaviour
{
    [Header("自動計時模式（無動畫時使用）")]
    [Tooltip("是否使用自動計時代替 Animation Event")]
    [SerializeField] private bool useAutoTimer = true;

    [Tooltip("自動計時秒數（useAutoTimer 為 true 時生效）")]
    [SerializeField] private float autoTimerDuration = 3f;

    [Header("可選：跳過動畫按鈕")]
    [Tooltip("是否允許按任意鍵跳過開場動畫")]
    [SerializeField] private bool allowSkip = true;

    [Tooltip("跳過前的最短等待時間（防止誤觸）")]
    [SerializeField] private float skipMinWait = 0.5f;

    private float elapsedTime = 0f;
    private bool hasFinished = false;

    private void Update()
    {
        if (hasFinished) return;

        elapsedTime += Time.deltaTime;

        // 自動計時模式
        if (useAutoTimer && elapsedTime >= autoTimerDuration)
        {
            OnOpeningAnimationFinished();
            return;
        }

        // 允許跳過
        if (allowSkip && elapsedTime >= skipMinWait && Input.anyKeyDown)
        {
            OnOpeningAnimationFinished();
            return;
        }
    }

    /// <summary>
    /// 開場動畫播放完畢後呼叫此方法。
    /// 
    /// 連接方式：
    ///   - Animation Event：在動畫 Clip 最後一帧新增事件，選擇此函式
    ///   - 或由 useAutoTimer 自動觸發
    ///   - 或由其他腳本手動呼叫：GetComponent&lt;OpeningAnimationController&gt;().OnOpeningAnimationFinished()
    /// </summary>
    public void OnOpeningAnimationFinished()
    {
        if (hasFinished) return;
        hasFinished = true;

        Debug.Log("[OpeningAnimationController] 開場動畫結束，進入遊戲關卡。");

        if (StoryFlowManager.Instance != null)
        {
            StoryFlowManager.Instance.StartGameLoop();
        }
        else
        {
            Debug.LogError("[OpeningAnimationController] StoryFlowManager 不存在！無法開始遊戲。");
        }
    }
}

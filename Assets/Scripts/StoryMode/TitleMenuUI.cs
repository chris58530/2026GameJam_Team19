using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 標題選單 UI 控制器。掛在 TitleMenu 場景的 Canvas 上。
/// 
/// 設定方式：
///   1. 在 TitleMenu 場景中建立 Canvas
///   2. 建立 "Start" 按鈕
///   3. 掛上此腳本
///   4. 在 Inspector 中將 Start 按鈕拖入 startButton 欄位
///      或直接在按鈕的 OnClick() 事件中連接 TitleMenuUI.OnStartButtonClicked()
/// 
/// 按鈕連接（Inspector 設定）：
///   - Start 按鈕 → OnStartButtonClicked()
///   - Quit 按鈕  → OnQuitButtonClicked()（可選）
/// </summary>
public class TitleMenuUI : MonoBehaviour
{
    [Header("按鈕參考（可選，也可用 Inspector 的 OnClick 連接）")]
    [Tooltip("開始遊戲按鈕")]
    [SerializeField] private Button startButton;

    [Tooltip("退出遊戲按鈕（可選）")]
    [SerializeField] private Button quitButton;

    private void Start()
    {
        // 如果有在 Inspector 拖入按鈕，自動連接事件
        if (startButton != null)
            startButton.onClick.AddListener(OnStartButtonClicked);

        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitButtonClicked);
    }

    /// <summary>
    /// Start 按鈕點擊 → 前往 OpeningAnimation。
    /// 在 Inspector 中連接：Button.OnClick() → TitleMenuUI.OnStartButtonClicked()
    /// </summary>
    public void OnStartButtonClicked()
    {
        if (StoryFlowManager.Instance != null)
        {
            StoryFlowManager.Instance.StartOpeningAnimation();
        }
        else
        {
            Debug.LogError("[TitleMenuUI] StoryFlowManager 不存在！請確認場景中有 StoryFlowManager 物件。");
        }
    }

    /// <summary>
    /// Quit 按鈕點擊 → 退出遊戲。
    /// </summary>
    public void OnQuitButtonClicked()
    {
        if (StoryFlowManager.Instance != null)
        {
            StoryFlowManager.Instance.QuitGame();
        }
        else
        {
            Debug.Log("[TitleMenuUI] 退出遊戲");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}

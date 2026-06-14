using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 結局場景 UI 控制器。掛在 Ending 場景的 Canvas 上。
/// 
/// 功能：
///   - 讀取 StoryFlowManager.Instance.CurrentResult 判斷是 Victory 還是 Fail
///   - 啟用對應的動畫物件（victoryAnimObj 或 failAnimObj）
///   - 動畫播放完畢後顯示結局按鈕（Retry / Back to Title / Quit）
///   - 如果沒有動畫，可設定 autoShowButtonsDelay 自動顯示按鈕
/// 
/// 設定方式：
///   1. 在 Ending 場景中建立 Canvas
///   2. 建立兩個動畫物件（或 placeholder 圖片）：Victory 和 Fail
///   3. 建立按鈕面板（含 Retry, Back to Title, Quit 三個按鈕）
///   4. 掛上此腳本
///   5. 在 Inspector 中連接：
///      - victoryAnimObject: Victory 動畫/圖片 GameObject
///      - failAnimObject: Fail 動畫/圖片 GameObject
///      - buttonsPanel: 按鈕面板 GameObject（預設隱藏）
///      - retryButton → OnRetryClicked()
///      - backToTitleButton → OnBackToTitleClicked()
///      - quitButton → OnQuitClicked()
/// 
/// 動畫結束通知（二擇一）：
///   A. 由 EndingAnimationController 呼叫 EndingUI.Instance.ShowButtons()
///   B. 使用 autoShowButtonsDelay 自動延遲顯示
/// </summary>
public class EndingUI : MonoBehaviour
{
    public static EndingUI Instance { get; private set; }

    [Header("動畫物件")]
    [Tooltip("Victory 動畫/圖片 GameObject（勝利時啟用）")]
    [SerializeField] private GameObject victoryAnimObject;

    [Tooltip("Fail 動畫/圖片 GameObject（失敗時啟用）")]
    [SerializeField] private GameObject failAnimObject;

    [Header("UI 面板")]
    [Tooltip("結局按鈕面板（動畫結束後顯示）")]
    [SerializeField] private GameObject buttonsPanel;

    [Header("按鈕參考（可選，也可用 Inspector 的 OnClick 連接）")]
    [Tooltip("重新開始按鈕")]
    [SerializeField] private Button retryButton;

    [Tooltip("返回標題按鈕")]
    [SerializeField] private Button backToTitleButton;

    [Tooltip("退出遊戲按鈕")]
    [SerializeField] private Button quitButton;

    [Header("結果文字（可選）")]
    [Tooltip("顯示 Victory / Fail 的文字")]
    [SerializeField] private TMP_Text resultText;

    [Tooltip("Victory 時顯示的文字")]
    [SerializeField] private string victoryMessage = "Victory!";

    [Tooltip("Fail 時顯示的文字")]
    [SerializeField] private string failMessage = "Game Over";

    [Header("自動顯示按鈕設定")]
    [Tooltip("如果沒有 EndingAnimationController，延遲幾秒後自動顯示按鈕")]
    [SerializeField] private float autoShowButtonsDelay = 3f;

    [Tooltip("是否使用自動延遲顯示按鈕（false = 等待 EndingAnimationController 呼叫）")]
    [SerializeField] private bool useAutoShowButtons = true;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // 隱藏按鈕面板
        if (buttonsPanel != null)
            buttonsPanel.SetActive(false);

        // 連接按鈕事件
        if (retryButton != null)
            retryButton.onClick.AddListener(OnRetryClicked);
        if (backToTitleButton != null)
            backToTitleButton.onClick.AddListener(OnBackToTitleClicked);
        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitClicked);

        // 根據結果顯示對應內容
        SetupEndingDisplay();

        // 自動顯示按鈕
        if (useAutoShowButtons)
        {
            Invoke(nameof(ShowButtons), autoShowButtonsDelay);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>
    /// 根據 StoryFlowManager 的結果設定顯示內容。
    /// </summary>
    private void SetupEndingDisplay()
    {
        StoryFlowManager.GameResult result = StoryFlowManager.GameResult.None;

        if (StoryFlowManager.Instance != null)
        {
            result = StoryFlowManager.Instance.CurrentResult;
        }

        bool isVictory = (result == StoryFlowManager.GameResult.Victory);

        // 啟用/隱藏對應動畫物件
        if (victoryAnimObject != null)
            victoryAnimObject.SetActive(isVictory);

        if (failAnimObject != null)
            failAnimObject.SetActive(!isVictory);

        // 設定文字
        if (resultText != null)
        {
            resultText.text = isVictory ? victoryMessage : failMessage;
        }

        Debug.Log($"[EndingUI] 結局類型: {(isVictory ? "Victory" : "Fail")}");
    }

    /// <summary>
    /// 顯示結局按鈕面板。
    /// 由 EndingAnimationController.OnEndingAnimationFinished() 呼叫，
    /// 或在 autoShowButtonsDelay 後自動呼叫。
    /// </summary>
    public void ShowButtons()
    {
        if (buttonsPanel != null)
        {
            buttonsPanel.SetActive(true);
            Debug.Log("[EndingUI] 顯示結局按鈕。");
        }
    }

    /// <summary>
    /// Retry 按鈕 → 從第一關重新開始。
    /// Inspector 連接：Button.OnClick() → EndingUI.OnRetryClicked()
    /// </summary>
    public void OnRetryClicked()
    {
        if (StoryFlowManager.Instance != null)
        {
            StoryFlowManager.Instance.RetryGame();
        }
        else
        {
            Debug.LogError("[EndingUI] StoryFlowManager 不存在！");
        }
    }

    /// <summary>
    /// Back to Title 按鈕 → 返回標題選單。
    /// Inspector 連接：Button.OnClick() → EndingUI.OnBackToTitleClicked()
    /// </summary>
    public void OnBackToTitleClicked()
    {
        if (StoryFlowManager.Instance != null)
        {
            StoryFlowManager.Instance.BackToTitle();
        }
        else
        {
            Debug.LogError("[EndingUI] StoryFlowManager 不存在！");
        }
    }

    /// <summary>
    /// Quit 按鈕 → 退出遊戲。
    /// Inspector 連接：Button.OnClick() → EndingUI.OnQuitClicked()
    /// </summary>
    public void OnQuitClicked()
    {
        if (StoryFlowManager.Instance != null)
        {
            StoryFlowManager.Instance.QuitGame();
        }
        else
        {
            Debug.Log("[EndingUI] 退出遊戲");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}

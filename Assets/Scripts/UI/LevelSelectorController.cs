using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 關卡選擇畫面控制器。
/// 讀取 LevelDatabase 中所有關卡定義，動態產生選擇按鈕。
/// 
/// 設定方式：
///   1. 在 LevelSelectorScene 中建立 Canvas
///   2. 掛上此腳本
///   3. 在 Inspector 中設定：
///      - levelDatabase: 拖入 LevelDatabase asset
///      - levelButtonPrefab: 拖入關卡按鈕 Prefab（包含 Button 和 TMP_Text）
///      - levelListParent: 放置按鈕的父物件（建議使用 Vertical/Grid Layout Group）
///      - backButton: 返回主選單按鈕
/// 
/// 按鈕連接：
///   - Back 按鈕 → OnBackButtonClicked()
///   - 關卡按鈕由程式自動產生和連接
/// </summary>
public class LevelSelectorController : MonoBehaviour
{
    [Header("資料來源")]
    [Tooltip("拖入 LevelDatabase ScriptableObject asset")]
    [SerializeField] private LevelDatabase levelDatabase;

    [Header("UI 參考")]
    [Tooltip("關卡按鈕 Prefab（需包含 Button 和子 TMP_Text）")]
    [SerializeField] private GameObject levelButtonPrefab;

    [Tooltip("按鈕產生的父物件（建議加 VerticalLayoutGroup 或 GridLayoutGroup）")]
    [SerializeField] private Transform levelListParent;

    [Tooltip("返回主選單按鈕")]
    [SerializeField] private Button backButton;

    [Header("選擇性 UI")]
    [Tooltip("關卡說明文字（可選）")]
    [SerializeField] private TMP_Text levelDescriptionText;

    [Tooltip("關卡預覽圖（可選）")]
    [SerializeField] private Image levelPreviewImage;

    private void Start()
    {
        // 連接返回按鈕
        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackButtonClicked);
        }

        // 產生關卡按鈕
        PopulateLevelButtons();
    }

    /// <summary>
    /// 動態產生所有關卡按鈕。
    /// </summary>
    private void PopulateLevelButtons()
    {
        if (levelDatabase == null)
        {
            Debug.LogError("[LevelSelectorController] levelDatabase 未設定！請在 Inspector 中拖入 LevelDatabase asset。");
            return;
        }

        if (levelButtonPrefab == null)
        {
            Debug.LogError("[LevelSelectorController] levelButtonPrefab 未設定！");
            return;
        }

        if (levelListParent == null)
        {
            Debug.LogError("[LevelSelectorController] levelListParent 未設定！");
            return;
        }

        // 清除舊按鈕
        foreach (Transform child in levelListParent)
        {
            Destroy(child.gameObject);
        }

        // 取得已排序的關卡列表
        var levels = levelDatabase.GetSortedLevels();

        foreach (var levelDef in levels)
        {
            if (levelDef == null) continue;

            // 實例化按鈕
            GameObject buttonObj = Instantiate(levelButtonPrefab, levelListParent);
            buttonObj.name = $"LevelButton_{levelDef.levelId}";

            // 設定按鈕文字
            TMP_Text buttonText = buttonObj.GetComponentInChildren<TMP_Text>();
            if (buttonText != null)
            {
                buttonText.text = levelDef.displayName;
            }

            // 設定按鈕點擊事件
            Button button = buttonObj.GetComponent<Button>();
            if (button != null)
            {
                // 使用區域變數捕獲，避免閉包問題
                LevelDefinition capturedDef = levelDef;
                button.onClick.AddListener(() => OnLevelSelected(capturedDef));

                // 如果關卡未解鎖，禁用按鈕
                if (!levelDef.unlockedByDefault)
                {
                    button.interactable = false;
                }
            }
        }

        Debug.Log($"[LevelSelectorController] 已產生 {levels.Count} 個關卡按鈕。");
    }

    /// <summary>
    /// 當玩家選擇一個關卡時呼叫。
    /// </summary>
    private void OnLevelSelected(LevelDefinition levelDef)
    {
        if (levelDef == null)
        {
            Debug.LogError("[LevelSelectorController] 選擇的關卡定義為 null！");
            return;
        }

        Debug.Log($"[LevelSelectorController] 選擇關卡: {levelDef.displayName}");

        // 建立 LevelRunContext
        LevelRunContext context = new LevelRunContext
        {
            selectedLevelId = levelDef.levelId,
            selectedLevelName = levelDef.displayName,
            difficulty = levelDef.difficulty,
            replayIndex = 0,
            seed = Random.Range(0, int.MaxValue)
        };

        // 透過 GameFlowManager 開始關卡
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.StartLevel(levelDef, context);
        }
        else
        {
            Debug.LogError("[LevelSelectorController] GameFlowManager 不存在！無法開始關卡。");
        }
    }

    /// <summary>
    /// 返回主選單按鈕點擊。
    /// </summary>
    public void OnBackButtonClicked()
    {
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.GoToMainMenu();
        }
        else if (SceneLoadManager.Instance != null)
        {
            SceneLoadManager.Instance.LoadMainMenu();
        }
        else
        {
            Debug.LogError("[LevelSelectorController] 無法返回主選單！Manager 不存在。");
        }
    }
}

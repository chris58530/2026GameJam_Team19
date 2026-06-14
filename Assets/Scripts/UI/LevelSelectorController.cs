using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Level selection screen controller.
/// Reads all level definitions from LevelDatabase and dynamically generates selection buttons.
/// 
/// Setup:
///   1. Create a Canvas in LevelSelectorScene
///   2. Attach this script
///   3. Configure in the Inspector:
///      - levelDatabase: drag in the LevelDatabase asset
///      - levelButtonPrefab: drag in the level button Prefab (contains a Button and a TMP_Text)
///      - levelListParent: the parent object that holds the buttons (recommend using a Vertical/Grid Layout Group)
///      - backButton: the return-to-main-menu button
/// 
/// Button connections:
///   - Back button -> OnBackButtonClicked()
///   - Level buttons are generated and connected automatically by code
/// </summary>
public class LevelSelectorController : MonoBehaviour
{
    [Header("Data source")]
    [Tooltip("Drag in the LevelDatabase ScriptableObject asset")]
    [SerializeField] private LevelDatabase levelDatabase;

    [Header("UI references")]
    [Tooltip("Level button Prefab (must contain a Button and a child TMP_Text)")]
    [SerializeField] private GameObject levelButtonPrefab;

    [Tooltip("Parent object the buttons are generated under (recommend adding a VerticalLayoutGroup or GridLayoutGroup)")]
    [SerializeField] private Transform levelListParent;

    [Tooltip("Return-to-main-menu button")]
    [SerializeField] private Button backButton;

    [Header("Optional UI")]
    [Tooltip("Level description text (optional)")]
    [SerializeField] private TMP_Text levelDescriptionText;

    [Tooltip("Level preview image (optional)")]
    [SerializeField] private Image levelPreviewImage;

    private void Start()
    {
        // Connect the back button
        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackButtonClicked);
        }

        // Generate the level buttons
        PopulateLevelButtons();
    }

    /// <summary>
    /// Dynamically generate all level buttons.
    /// </summary>
    private void PopulateLevelButtons()
    {
        if (levelDatabase == null)
        {
            Debug.LogError("[LevelSelectorController] levelDatabase is not set! Please drag in the LevelDatabase asset in the Inspector.");
            return;
        }

        if (levelButtonPrefab == null)
        {
            Debug.LogError("[LevelSelectorController] levelButtonPrefab is not set!");
            return;
        }

        if (levelListParent == null)
        {
            Debug.LogError("[LevelSelectorController] levelListParent is not set!");
            return;
        }

        // Clear old buttons
        foreach (Transform child in levelListParent)
        {
            Destroy(child.gameObject);
        }

        // Get the sorted level list
        var levels = levelDatabase.GetSortedLevels();

        foreach (var levelDef in levels)
        {
            if (levelDef == null) continue;

            // Instantiate the button
            GameObject buttonObj = Instantiate(levelButtonPrefab, levelListParent);
            buttonObj.name = $"LevelButton_{levelDef.levelId}";

            // Set the button text
            TMP_Text buttonText = buttonObj.GetComponentInChildren<TMP_Text>();
            if (buttonText != null)
            {
                buttonText.text = levelDef.displayName;
            }

            // Set the button click event
            Button button = buttonObj.GetComponent<Button>();
            if (button != null)
            {
                // Capture into a local variable to avoid closure issues
                LevelDefinition capturedDef = levelDef;
                button.onClick.AddListener(() => OnLevelSelected(capturedDef));

                // If the level is not unlocked, disable the button
                if (!levelDef.unlockedByDefault)
                {
                    button.interactable = false;
                }
            }
        }

        Debug.Log($"[LevelSelectorController] Generated {levels.Count} level buttons.");
    }

    /// <summary>
    /// Called when the player selects a level.
    /// </summary>
    private void OnLevelSelected(LevelDefinition levelDef)
    {
        if (levelDef == null)
        {
            Debug.LogError("[LevelSelectorController] The selected level definition is null!");
            return;
        }

        Debug.Log($"[LevelSelectorController] Selected level: {levelDef.displayName}");

        // Build the LevelRunContext
        LevelRunContext context = new LevelRunContext
        {
            selectedLevelId = levelDef.levelId,
            selectedLevelName = levelDef.displayName,
            difficulty = levelDef.difficulty,
            replayIndex = 0,
            seed = Random.Range(0, int.MaxValue)
        };

        // Start the level through GameFlowManager
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.StartLevel(levelDef, context);
        }
        else
        {
            Debug.LogError("[LevelSelectorController] GameFlowManager does not exist! Cannot start the level.");
        }
    }

    /// <summary>
    /// Back-to-main-menu button click.
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
            Debug.LogError("[LevelSelectorController] Cannot return to main menu! Manager does not exist.");
        }
    }
}

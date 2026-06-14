using UnityEngine;

/// <summary>
/// Level definition ScriptableObject.
/// One asset file per level, describing the level's basic info and Prefab reference.
/// 
/// How to create:
///   Right-click in the Project panel -> Create -> GameJam -> Level Definition
/// 
/// Setup:
///   - levelId: unique ID (e.g. "level_01")
///   - displayName: display name (e.g. "Level 1")
///   - levelPrefab: drag in the level Prefab (e.g. Level_01.prefab)
///   - previewImage: optional preview image
///   - difficulty: difficulty description
///   - description: level description
///   - unlockedByDefault: whether it is unlocked by default
///   - sortOrder: sort order
/// </summary>
[CreateAssetMenu(fileName = "NewLevelDefinition", menuName = "GameJam/Level Definition")]
public class LevelDefinition : ScriptableObject
{
    [Header("Basic Info")]
    [Tooltip("Unique level ID, e.g. level_01")]
    public string levelId = "";

    [Tooltip("Display name, used in the UI")]
    public string displayName = "New Level";

    [Header("Level Prefab")]
    [Tooltip("Level Prefab, instantiated in the GameScene")]
    public GameObject levelPrefab;

    [Header("UI Display")]
    [Tooltip("Level preview image (optional)")]
    public Sprite previewImage;

    [Tooltip("Difficulty description")]
    public string difficulty = "Normal";

    [Tooltip("Level description")]
    [TextArea(2, 4)]
    public string description = "";

    [Header("Unlock and Sorting")]
    [Tooltip("Whether it is unlocked by default")]
    public bool unlockedByDefault = true;

    [Tooltip("Sort order (smaller numbers come first)")]
    public int sortOrder = 0;
}

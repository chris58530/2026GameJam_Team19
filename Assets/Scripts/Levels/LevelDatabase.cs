using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Level database ScriptableObject.
/// Stores all available LevelDefinitions for use by the LevelSelectorController.
/// 
/// How to create:
///   Right-click in the Project panel -> Create -> GameJam -> Level Database
/// 
/// Setup:
///   - Drag all LevelDefinition assets into the levels list
/// </summary>
[CreateAssetMenu(fileName = "LevelDatabase", menuName = "GameJam/Level Database")]
public class LevelDatabase : ScriptableObject
{
    [Header("All Level Definitions")]
    [Tooltip("Drag all LevelDefinition assets into this list")]
    public List<LevelDefinition> levels = new List<LevelDefinition>();

    /// <summary>
    /// Gets all level definitions.
    /// </summary>
    public List<LevelDefinition> GetAllLevels()
    {
        return levels;
    }

    /// <summary>
    /// Finds a level definition by levelId.
    /// </summary>
    public LevelDefinition GetLevelById(string levelId)
    {
        if (string.IsNullOrEmpty(levelId))
        {
            Debug.LogWarning("[LevelDatabase] GetLevelById: levelId is empty!");
            return null;
        }

        for (int i = 0; i < levels.Count; i++)
        {
            if (levels[i] != null && levels[i].levelId == levelId)
                return levels[i];
        }

        Debug.LogWarning($"[LevelDatabase] Could not find levelId: {levelId}");
        return null;
    }

    /// <summary>
    /// Gets the sorted list of levels (by sortOrder).
    /// </summary>
    public List<LevelDefinition> GetSortedLevels()
    {
        var sorted = new List<LevelDefinition>(levels);
        sorted.RemoveAll(l => l == null);
        sorted.Sort((a, b) => a.sortOrder.CompareTo(b.sortOrder));
        return sorted;
    }
}

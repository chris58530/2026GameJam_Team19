using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 關卡資料庫 ScriptableObject。
/// 存放所有可用的 LevelDefinition，供 LevelSelectorController 使用。
/// 
/// 建立方式：
///   Project 面板右鍵 → Create → GameJam → Level Database
/// 
/// 設定方式：
///   - 將所有 LevelDefinition asset 拖入 levels 列表
/// </summary>
[CreateAssetMenu(fileName = "LevelDatabase", menuName = "GameJam/Level Database")]
public class LevelDatabase : ScriptableObject
{
    [Header("所有關卡定義")]
    [Tooltip("將所有 LevelDefinition asset 拖入此列表")]
    public List<LevelDefinition> levels = new List<LevelDefinition>();

    /// <summary>
    /// 取得所有關卡定義。
    /// </summary>
    public List<LevelDefinition> GetAllLevels()
    {
        return levels;
    }

    /// <summary>
    /// 依 levelId 查找關卡定義。
    /// </summary>
    public LevelDefinition GetLevelById(string levelId)
    {
        if (string.IsNullOrEmpty(levelId))
        {
            Debug.LogWarning("[LevelDatabase] GetLevelById: levelId 為空！");
            return null;
        }

        for (int i = 0; i < levels.Count; i++)
        {
            if (levels[i] != null && levels[i].levelId == levelId)
                return levels[i];
        }

        Debug.LogWarning($"[LevelDatabase] 找不到 levelId: {levelId}");
        return null;
    }

    /// <summary>
    /// 取得已排序的關卡列表（依 sortOrder）。
    /// </summary>
    public List<LevelDefinition> GetSortedLevels()
    {
        var sorted = new List<LevelDefinition>(levels);
        sorted.RemoveAll(l => l == null);
        sorted.Sort((a, b) => a.sortOrder.CompareTo(b.sortOrder));
        return sorted;
    }
}

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 關卡執行時上下文資料。
/// 在 LevelSelectorScene 中建立，傳遞給 GameScene 中的關卡 Prefab。
/// 
/// 用法：
///   var context = new LevelRunContext();
///   context.selectedLevelId = "level_01";
///   context.difficulty = "normal";
///   context.SetInt("customVar", 42);
/// </summary>
[System.Serializable]
public class LevelRunContext
{
    [Header("基本關卡資訊")]
    public string selectedLevelId = "";
    public string selectedLevelName = "";
    public string difficulty = "normal";
    public string playerSpawnId = "";
    public int replayIndex = 0;
    public int seed = 0;

    // --- 自訂變數字典 ---
    private Dictionary<string, string> stringVars = new Dictionary<string, string>();
    private Dictionary<string, int> intVars = new Dictionary<string, int>();
    private Dictionary<string, float> floatVars = new Dictionary<string, float>();
    private Dictionary<string, bool> boolVars = new Dictionary<string, bool>();

    // ===== String =====
    public void SetString(string key, string value)
    {
        stringVars[key] = value;
    }

    public string GetString(string key, string fallback = "")
    {
        return stringVars.TryGetValue(key, out string val) ? val : fallback;
    }

    // ===== Int =====
    public void SetInt(string key, int value)
    {
        intVars[key] = value;
    }

    public int GetInt(string key, int fallback = 0)
    {
        return intVars.TryGetValue(key, out int val) ? val : fallback;
    }

    // ===== Float =====
    public void SetFloat(string key, float value)
    {
        floatVars[key] = value;
    }

    public float GetFloat(string key, float fallback = 0f)
    {
        return floatVars.TryGetValue(key, out float val) ? val : fallback;
    }

    // ===== Bool =====
    public void SetBool(string key, bool value)
    {
        boolVars[key] = value;
    }

    public bool GetBool(string key, bool fallback = false)
    {
        return boolVars.TryGetValue(key, out bool val) ? val : fallback;
    }

    /// <summary>
    /// 建立此 Context 的淺拷貝（用於 Retry 時保持原始資料）。
    /// </summary>
    public LevelRunContext Clone()
    {
        var clone = new LevelRunContext
        {
            selectedLevelId = this.selectedLevelId,
            selectedLevelName = this.selectedLevelName,
            difficulty = this.difficulty,
            playerSpawnId = this.playerSpawnId,
            replayIndex = this.replayIndex,
            seed = this.seed,
            stringVars = new Dictionary<string, string>(this.stringVars),
            intVars = new Dictionary<string, int>(this.intVars),
            floatVars = new Dictionary<string, float>(this.floatVars),
            boolVars = new Dictionary<string, bool>(this.boolVars)
        };
        return clone;
    }

    public override string ToString()
    {
        return $"[LevelRunContext] id={selectedLevelId}, name={selectedLevelName}, " +
               $"difficulty={difficulty}, spawn={playerSpawnId}, " +
               $"replay={replayIndex}, seed={seed}";
    }
}

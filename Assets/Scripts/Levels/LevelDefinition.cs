using UnityEngine;

/// <summary>
/// 關卡定義 ScriptableObject。
/// 每個關卡一個 asset 檔案，描述關卡的基本資訊與 Prefab 參考。
/// 
/// 建立方式：
///   Project 面板右鍵 → Create → GameJam → Level Definition
/// 
/// 設定方式：
///   - levelId: 唯一 ID（如 "level_01"）
///   - displayName: 顯示名稱（如 "第一關"）
///   - levelPrefab: 拖入關卡 Prefab（如 Level_01.prefab）
///   - previewImage: 可選的預覽圖
///   - difficulty: 難度描述
///   - description: 關卡說明
///   - unlockedByDefault: 是否預設解鎖
///   - sortOrder: 排序順序
/// </summary>
[CreateAssetMenu(fileName = "NewLevelDefinition", menuName = "GameJam/Level Definition")]
public class LevelDefinition : ScriptableObject
{
    [Header("基本資訊")]
    [Tooltip("關卡唯一 ID，例如 level_01")]
    public string levelId = "";

    [Tooltip("顯示名稱，用於 UI")]
    public string displayName = "New Level";

    [Header("關卡 Prefab")]
    [Tooltip("關卡 Prefab，會在 GameScene 中實例化")]
    public GameObject levelPrefab;

    [Header("UI 顯示")]
    [Tooltip("關卡預覽圖（可選）")]
    public Sprite previewImage;

    [Tooltip("難度描述")]
    public string difficulty = "Normal";

    [Tooltip("關卡說明")]
    [TextArea(2, 4)]
    public string description = "";

    [Header("解鎖與排序")]
    [Tooltip("是否預設解鎖")]
    public bool unlockedByDefault = true;

    [Tooltip("排序順序（數字越小越前面）")]
    public int sortOrder = 0;
}

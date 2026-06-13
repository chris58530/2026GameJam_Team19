# UI/場景系統整合指南（新架構 v2）

## 架構總覽

```
MainMenuScene → (LoadingScene) → LevelSelectorScene
                                        ↓ 選擇關卡
                                  (LoadingScene) → GameScene
                                                      ↓ 實例化
                                               Level Prefab (Level_01, Level_02...)
                                                      ↓ ESC
                                                 PauseMenu
                                                 ├─ Resume → 繼續遊戲
                                                 ├─ Retry  → 重新實例化關卡 Prefab
                                                 ├─ Level Selector → LevelSelectorScene
                                                 └─ Main Menu → MainMenuScene
                                                      ↓ 關卡完成
                                               LevelSelectorScene（循環）
```

## 核心概念

- **GameScene** 是一個穩定的「殼」場景，不會每次換關卡就重載
- **Level Prefab** 是每個關卡的實際內容，動態實例化到 GameScene 中
- **LevelRunContext** 是傳遞給關卡的執行時變數
- **LevelDefinition** 是描述每個關卡的 ScriptableObject

---

## 腳本說明

| 腳本 | 位置 | 用途 |
|------|------|------|
| `GameFlowManager.cs` | `Scripts/Core/` | 全域流程管理 (Singleton, DontDestroyOnLoad) |
| `SceneLoadManager.cs` | `Scripts/Core/` | 場景載入管理 (Singleton, DontDestroyOnLoad) |
| `LevelRunContext.cs` | `Scripts/Core/` | 關卡執行時上下文資料 |
| `ILevelInitializable.cs` | `Scripts/Core/` | 關卡初始化介面 |
| `LevelDefinition.cs` | `Scripts/Levels/` | 關卡定義 ScriptableObject |
| `LevelDatabase.cs` | `Scripts/Levels/` | 關卡資料庫 ScriptableObject |
| `GameSceneController.cs` | `Scripts/Levels/` | GameScene 控制器 |
| `LevelCompleteTrigger.cs` | `Scripts/Levels/` | 關卡完成觸發器範例 |
| `MainMenuController.cs` | `Scripts/UI/` | 主選單按鈕邏輯 |
| `LoadingScreenController.cs` | `Scripts/UI/` | Loading 畫面控制 |
| `LevelSelectorController.cs` | `Scripts/UI/` | 關卡選擇畫面 |
| `PauseMenuController.cs` | `Scripts/UI/` | 暫停選單邏輯 |

## 場景說明

| 場景 | Build Index | 用途 |
|------|-------------|------|
| `MainMenuScene` | 0 | 主選單（遊戲啟動場景） |
| `LoadingScene` | 1 | 載入畫面（過渡用） |
| `LevelSelectorScene` | 2 | 關卡選擇 |
| `GameScene` | 3 | 遊戲場景殼（動態載入關卡 Prefab） |

---

## Build Settings 設定

在 File → Build Settings 中加入以下場景（順序）：
1. `Assets/Scenes/MainMenuScene.unity`
2. `Assets/Scenes/LoadingScene.unity`
3. `Assets/Scenes/LevelSelectorScene.unity`
4. `Assets/Scenes/GameScene.unity`

注意：Level Prefab 不需要加入 Build Settings，它們透過 LevelDefinition 的參考自動包含在 Build 中。

---

## 場景內容設定

### MainMenuScene
- Canvas
  - Title Text
  - Start Game Button → `MainMenuController.StartGame()`
  - Quit Game Button → `MainMenuController.QuitGame()`
  - Settings Button → `MainMenuController.OpenSettings()`
- EventSystem
- SceneLoadManager（空 GameObject + 腳本）
- GameFlowManager（空 GameObject + 腳本）

### LoadingScene
- Canvas
  - Loading Text (TMP_Text)
  - Progress Bar (Slider)
- LoadingScreenController（掛在 Canvas 上）
- EventSystem

### LevelSelectorScene
- Canvas
  - Title Text "Level Select"
  - Level List Panel（加 VerticalLayoutGroup 或 GridLayoutGroup）
  - Back Button → `LevelSelectorController.OnBackButtonClicked()`
- LevelSelectorController（掛在 Canvas 上）
  - 設定 levelDatabase、levelButtonPrefab、levelListParent、backButton
- EventSystem

### GameScene
- GameSceneController（空 GameObject + 腳本）
  - LevelContainer（子物件，空 Transform）
- PauseMenuCanvas（Prefab 實例）
- EventSystem
- Main Camera（或你的相機設定）

---

## ScriptableObject 建立

### 建立 LevelDefinition
1. Project 面板右鍵 → Create → GameJam → Level Definition
2. 命名為 `Level_01`
3. 填入：
   - levelId: `level_01`
   - displayName: `第一關`
   - levelPrefab: 拖入 Level_01.prefab
   - difficulty: `Easy`
   - sortOrder: `1`
   - unlockedByDefault: `true`

### 建立 LevelDatabase
1. Project 面板右鍵 → Create → GameJam → Level Database
2. 命名為 `LevelDatabase`
3. 將所有 LevelDefinition asset 拖入 levels 列表

---

## 按鈕連接指南

### MainMenuScene
| 按鈕 | 目標物件 | 函數 |
|------|---------|------|
| Start Game | MainMenuController | `StartGame()` |
| Quit Game | MainMenuController | `QuitGame()` |

### LevelSelectorScene
| 按鈕 | 目標物件 | 函數 |
|------|---------|------|
| Back | LevelSelectorController | `OnBackButtonClicked()` |
| 關卡按鈕 | 自動產生 | 自動連接 |

### GameScene (PauseMenuCanvas)
| 按鈕 | 目標物件 | 函數 |
|------|---------|------|
| Resume | PauseMenuController | `ResumeGame()` |
| Retry | PauseMenuController | `RetryGame()` |
| Level Select | PauseMenuController | `ReturnToLevelSelector()` |
| Main Menu | PauseMenuController | `ReturnToMainMenu()` |

---

## 隊友整合步驟

### 你需要做的事（建立關卡 Prefab）：

#### 1. 將 Platformer2D 的內容轉為 Prefab
1. 打開 Platformer2D 場景
2. 選取所有關卡內容（地形、敵人、玩家、道具等）
3. 打包成一個空的父物件（例如命名為 "Level_01"）
4. 將該父物件拖入 `Assets/Prefabs/Levels/` 建立 Prefab
5. 如果需要接收執行時資料，在根物件上加一個實作 `ILevelInitializable` 的腳本

#### 2. 建立 LevelDefinition asset
1. 右鍵 → Create → GameJam → Level Definition
2. 填入關卡資訊
3. 拖入關卡 Prefab

#### 3. 加入 LevelDatabase
1. 將新的 LevelDefinition 加入 LevelDatabase.levels 列表

### 你不需要擔心的事
- ❌ 不會修改 `PlayerController2D.cs`
- ❌ 不會修改 `PlayerMovement.cs`
- ❌ 不會修改 `GameController.cs`
- ❌ 不會修改 `Ghost.cs`、`LoopManager.cs` 等遊戲邏輯
- ❌ 不會改變相機、物理、輸入系統設定

### 關卡完成處理
在你的關卡中，當玩家完成關卡時，呼叫：
```csharp
// 方式 1：使用 GameSceneController
if (GameSceneController.Instance != null)
    GameSceneController.Instance.ReturnToLevelSelector();

// 方式 2：使用 GameFlowManager
if (GameFlowManager.Instance != null)
    GameFlowManager.Instance.GoToLevelSelector();

// 方式 3：使用 LevelCompleteTrigger 元件（拖入場景即可）
```

### 接收執行時資料（可選）
如果你的關卡需要接收外部變數（如難度、種子等），實作 `ILevelInitializable`：
```csharp
public class MyLevelSetup : MonoBehaviour, ILevelInitializable
{
    public void Initialize(LevelRunContext context)
    {
        string difficulty = context.difficulty;
        int seed = context.seed;
        // 使用這些資料設定你的關卡
    }
}
```

---

## ESC 暫停注意事項

- 暫停使用 `Input.GetKeyDown(KeyCode.Escape)` 偵測
- 如果你的遊戲有用 ESC 做其他事，可能需要協調
- 暫停時 `Time.timeScale = 0`，所有基於 Time 的動畫/物理都會暫停
- 如果你有用 `Time.unscaledDeltaTime` 的邏輯，暫停時仍會執行

---

## 資料夾結構

```
Assets/
├── Scripts/
│   ├── Core/
│   │   ├── GameFlowManager.cs
│   │   ├── SceneLoadManager.cs
│   │   ├── LevelRunContext.cs
│   │   └── ILevelInitializable.cs
│   ├── Levels/
│   │   ├── LevelDefinition.cs
│   │   ├── LevelDatabase.cs
│   │   ├── GameSceneController.cs
│   │   └── LevelCompleteTrigger.cs
│   └── UI/
│       ├── MainMenuController.cs
│       ├── LoadingScreenController.cs
│       ├── LevelSelectorController.cs
│       └── PauseMenuController.cs
├── Scenes/
│   ├── MainMenuScene.unity
│   ├── LoadingScene.unity
│   ├── LevelSelectorScene.unity
│   └── GameScene.unity
├── Prefabs/
│   ├── UI/
│   │   ├── PauseMenuCanvas.prefab
│   │   └── LevelButton.prefab
│   └── Levels/
│       ├── Level_01.prefab
│       ├── Level_02.prefab
│       └── Level_03.prefab
└── ScriptableObjects/
    └── Levels/
        ├── LevelDatabase.asset
        ├── Level_01.asset
        ├── Level_02.asset
        └── Level_03.asset
```

---

## 測試清單

- [ ] 開啟 MainMenuScene
- [ ] 按 Start Game
- [ ] Loading 畫面出現
- [ ] LevelSelectorScene 載入
- [ ] 關卡按鈕正確顯示
- [ ] 選擇 Level 01
- [ ] Loading 畫面出現
- [ ] GameScene 載入
- [ ] Level_01 Prefab 在 LevelContainer 下實例化
- [ ] LevelRunContext 傳遞給 ILevelInitializable 元件
- [ ] 按 ESC
- [ ] 暫停選單開啟
- [ ] Time.timeScale 變成 0
- [ ] 按 Resume
- [ ] 暫停選單關閉
- [ ] Time.timeScale 變成 1
- [ ] 再按 ESC
- [ ] 按 Retry
- [ ] 當前關卡 Prefab 被銷毀並重新實例化
- [ ] GameScene 殼保持穩定
- [ ] 按 Level Selector
- [ ] 返回 LevelSelectorScene
- [ ] 選擇另一個關卡
- [ ] GameScene 載入並實例化另一個關卡 Prefab
- [ ] 完成關卡
- [ ] 返回 LevelSelectorScene
- [ ] 按 Main Menu
- [ ] 返回 MainMenuScene
- [ ] 每次切換後 Time.timeScale 正確恢復為 1

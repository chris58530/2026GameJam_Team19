# Story Mode 線性流程系統 — 設定指南

## 概述

此系統實現了線性的遊戲流程：
```
TitleMenu → OpeningAnimation → Level01 → Level02 → Level03 → Ending (Victory/Fail)
```

**重要：此系統與現有的選關模式 (GameFlowManager) 完全獨立，不會互相干擾。**

---

## 第一步：建立缺少的場景

以下場景需要建立（目前不存在）：
- `Assets/Scenes/TitleMenu.unity`
- `Assets/Scenes/OpeningAnimation.unity`
- `Assets/Scenes/Level01.unity`
- `Assets/Scenes/Level02.unity`
- `Assets/Scenes/Level03.unity`
- `Assets/Scenes/Ending.unity`

### 注意事項
- 你的專案中已有 `Game0.unity`, `Game1.unity`, `Game2.unity` 場景
- 如果 Game0=Level01, Game1=Level02, Game2=Level03，你可以：
  - 選項 A：在 StoryFlowManager 的 Inspector 中把 levelScenes 改成 ["Game0", "Game1", "Game2"]
  - 選項 B：建立新的 Level01/02/03 場景（重新設計關卡）
  - 選項 C：複製 Game0/1/2 並重新命名為 Level01/02/03

### 建立 placeholder 場景的步驟
1. Unity 選單 → File → New Scene
2. 儲存到 Assets/Scenes/ 目錄
3. 對每個需要的場景重複此步驟

---

## 第二步：設定 StoryFlowManager

1. 開啟 `TitleMenu` 場景
2. 建立空 GameObject，命名為 **"StoryFlowManager"**
3. 掛上 `StoryFlowManager.cs` 腳本
4. 在 Inspector 中設定場景名稱：
   - titleMenuScene: `TitleMenu`
   - openingAnimationScene: `OpeningAnimation`
   - levelScenes: `["Level01", "Level02", "Level03"]`（可自由增減！）
   - endingScene: `Ending`

**持久化說明：**
StoryFlowManager 使用 DontDestroyOnLoad，會在所有場景之間持續存在。
只需要在第一個場景（TitleMenu）放置即可。

### 如何增加更多關卡
只要在 Inspector 的 levelScenes 陣列中加入新的場景名稱，例如：
```
levelScenes: ["Level01", "Level02", "Level03", "Level04", "BonusLevel"]
```
然後建立對應的場景並加入 Build Settings。

---

## 第三步：設定 TitleMenu 場景

1. 開啟 `TitleMenu` 場景
2. 建立 Canvas（UI → Canvas）
3. 在 Canvas 下建立 Button（UI → Button - TextMeshPro）
4. 按鈕文字設為 "Start"
5. 在 Canvas 上掛上 `TitleMenuUI.cs`
6. 將按鈕拖入 Inspector 的 startButton 欄位
   **或** 在按鈕的 OnClick() 中：
   - 拖入有 TitleMenuUI 的 GameObject
   - 選擇函式：TitleMenuUI → OnStartButtonClicked()

---

## 第四步：設定 OpeningAnimation 場景

1. 開啟 `OpeningAnimation` 場景
2. 建立空 GameObject，命名為 "OpeningAnimation"
3. 掛上 `OpeningAnimationController.cs`
4. Inspector 設定：
   - **如果還沒有動畫**：useAutoTimer = true, autoTimerDuration = 3 秒
   - **如果有動畫**：useAutoTimer = false，然後在動畫 Clip 最後一帧加 Animation Event → 呼叫 `OnOpeningAnimationFinished`

### Animation Event 連接步驟
1. 打開 Animation 視窗
2. 選擇你的開場動畫 Clip
3. 移到最後一帧
4. 點上方 "Add Event" 按鈕
5. 在彈出面板中選擇函式：`OnOpeningAnimationFinished`

---

## 第五步：設定每個關卡場景

對每個 Level 場景（Level01, Level02, Level03...）：

1. 建立空 GameObject，命名為 **"LevelManager"**
2. 掛上 `LevelManager.cs`
3. 設定延遲（可選）：
   - clearDelay: 通關後延遲幾秒再切換（播放動畫用）
   - failDelay: 失敗後延遲幾秒再切換

### 如何觸發通關/失敗

在你的關卡邏輯中（例如 LoopManager），呼叫：

```csharp
// 玩家通關時
LevelManager.Instance.OnLevelCleared();

// 玩家失敗時
LevelManager.Instance.OnLevelFailed();
```

### 整合現有 LoopManager 的範例

在 `LoopManager.cs` 的 `TryExit()` 方法中，勝利後加上：
```csharp
public void TryExit()
{
    if (!Won && DoorOpen)
    {
        Won = true;
        // ... 現有邏輯 ...

        // 新增：通知 StoryFlowManager 進入下一關
        if (LevelManager.Instance != null)
            LevelManager.Instance.OnLevelCleared();
    }
}
```

在失敗後（如果想直接進 Fail Ending 而非重來同關）：
```csharp
// 在你想要直接結束遊戲的地方
if (LevelManager.Instance != null)
    LevelManager.Instance.OnLevelFailed();
```

---

## 第六步：設定 Ending 場景

1. 開啟 `Ending` 場景
2. 建立 Canvas
3. 建立以下物件：
   - VictoryAnimObject：勝利時顯示的動畫/圖片
   - FailAnimObject：失敗時顯示的動畫/圖片
   - ButtonsPanel：包含三個按鈕的面板
     - RetryButton（重試）
     - BackToTitleButton（返回標題）
     - QuitButton（退出）
4. 在 Canvas 上掛上 `EndingUI.cs`
5. 在 Inspector 中連接所有參考

### EndingAnimationController（可選）
如果你有結局動畫：
1. 在動畫物件上掛 `EndingAnimationController.cs`
2. 在動畫 Clip 最後一帧加 Animation Event → `OnEndingAnimationFinished`
3. 在 EndingUI 中設定 useAutoShowButtons = false

如果沒有動畫：
- EndingUI 中保持 useAutoShowButtons = true
- 按鈕會在設定的秒數後自動出現

---

## 第七步：更新 Build Settings

使用 Editor 工具自動加入：
1. Unity 選單 → **Tools → Story Mode → Add Scenes to Build Settings**
2. 或使用 **Tools → Story Mode → Check Scene Status** 檢查狀態

**手動加入方式：**
1. File → Build Settings
2. 將以下場景拖入列表（保持順序）：
   - TitleMenu
   - OpeningAnimation
   - Level01
   - Level02
   - Level03
   - Ending
3. **不要移除** 已存在的場景（MainMenuScene, LoadingScene 等）

---

## 如果場景已存在但名稱不同

| 需要的名稱 | 可能已存在的場景 | 建議 |
|---|---|---|
| TitleMenu | MainMenuScene（已存在，但連接選關模式） | 建立新場景 TitleMenu |
| Level01 | Game0（可能） | 在 Inspector 中改 levelScenes 為 "Game0" |
| Level02 | Game1（可能） | 在 Inspector 中改 levelScenes 為 "Game1" |
| Level03 | Game2（可能） | 在 Inspector 中改 levelScenes 為 "Game2" |

---

## 如果缺少場景

如果某個場景尚未建立，系統會在 Console 中輸出警告訊息。
先建立 placeholder 場景，之後再慢慢填入內容。

---

## 架構圖

```
[TitleMenu]
    └─ StoryFlowManager (DontDestroyOnLoad, 持久)
    └─ Canvas + TitleMenuUI
         └─ Start Button → StartOpeningAnimation()

[OpeningAnimation]
    └─ OpeningAnimationController
         └─ Animation Event / Auto Timer → StartGameLoop()

[Level01] [Level02] [Level03] ...
    └─ LevelManager
         └─ OnLevelCleared() → CompleteLevel()
         └─ OnLevelFailed() → FailLevel()
    └─ 關卡遊玩內容（LoopManager、玩家、機關等）

[Ending]
    └─ EndingUI
         └─ Victory/Fail 動畫
         └─ 按鈕面板
              └─ Retry → RetryGame()
              └─ Back to Title → BackToTitle()
              └─ Quit → QuitGame()
```

---

## 常見問題

**Q: 如果我想在失敗時重試當前關卡而不是去 Ending？**
A: 不要呼叫 `LevelManager.Instance.OnLevelFailed()`，
   而是用你自己的重試邏輯（如 LoopManager.ResetLevel()）。
   只在真正想結束遊戲時才呼叫 OnLevelFailed()。

**Q: 如何在關卡之間保存資料？**
A: StoryFlowManager 是 DontDestroyOnLoad，你可以在上面加自訂欄位。

**Q: 這個系統和原來的 GameFlowManager 會衝突嗎？**
A: 不會。兩套系統完全獨立。
   - 如果從 TitleMenu 開始 → 走 StoryFlowManager 路線
   - 如果從 MainMenuScene 開始 → 走原有的 GameFlowManager 路線

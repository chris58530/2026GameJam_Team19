# UI/場景系統整合指南

## 架構總覽

```
MainMenuScene → (LoadingScene) → Platformer2D
                                      ↓ ESC
                                 PauseMenu
                                 ├─ Resume → 繼續遊戲
                                 ├─ Retry  → (LoadingScene) → 重載當前場景
                                 └─ Main Menu → (LoadingScene) → MainMenuScene
```

## 腳本說明

| 腳本 | 位置 | 用途 |
|------|------|------|
| `SceneLoadManager.cs` | `Assets/Scripts/Core/` | 全域場景載入管理器 (Singleton, DontDestroyOnLoad) |
| `MainMenuController.cs` | `Assets/Scripts/UI/` | 主選單按鈕邏輯 |
| `LoadingScreenController.cs` | `Assets/Scripts/UI/` | Loading 畫面控制 |
| `PauseMenuController.cs` | `Assets/Scripts/UI/` | 暫停選單邏輯 |

## 場景說明

| 場景 | Build Index | 用途 |
|------|-------------|------|
| `MainMenuScene` | 0 | 主選單（遊戲啟動場景） |
| `LoadingScene` | 1 | 載入畫面（過渡用） |
| `Platformer2D` | 2 | 遊戲關卡 |

---

## 隊友整合步驟（超簡單！）

### 你只需要做 2 件事：

#### 1. 確認 Platformer2D 場景中有這些物件：
- ✅ `SceneLoadManager` (已加入，帶有 SceneLoadManager 腳本)
- ✅ `PauseMenuCanvas` (已加入，帶有暫停選單 Prefab)
- ✅ `EventSystem` (已加入，處理 UI 點擊事件)

> 以上我已經全部設定好了！你不需要額外操作。

#### 2. 如果你重建場景或需要手動添加暫停選單：
1. 在 `Assets/Prefabs/UI/` 找到 `PauseMenuCanvas.prefab`
2. 拖入你的遊戲場景
3. 確認場景中有 `EventSystem`
4. 確認場景中有 `SceneLoadManager`（空 GameObject + SceneLoadManager 腳本）

---

## 不會影響你的東西

- ❌ 不會修改 `PlayerController2D.cs`
- ❌ 不會修改 `PlayerMovement.cs`
- ❌ 不會修改 `GameController.cs`
- ❌ 不會修改 `LevelExit.cs`
- ❌ 不會改變相機、物理、輸入系統設定

## ESC 暫停注意事項

- 暫停使用 `Input.GetKeyDown(KeyCode.Escape)` 偵測
- 如果你的遊戲有用 ESC 做其他事（例如關閉背包），可能需要協調
- 暫停時 `Time.timeScale = 0`，所有基於 Time 的動畫/物理都會暫停
- 如果你有用 `Time.unscaledDeltaTime` 的邏輯，暫停時仍會執行

## 未來擴充

如果要加新關卡（例如 Level02）：
1. 建立新場景
2. 加入 Build Settings
3. 在 MainMenuController 的 Inspector 中修改 `defaultGameplaySceneName`
4. 或在程式中呼叫 `SceneLoadManager.Instance.LoadSceneWithLoading("Level02")`
5. PauseMenuCanvas Prefab 拖進新場景即可使用（Retry 會自動重載當前場景）

---

## 測試清單

- [ ] 開啟 MainMenuScene，按 Start Game
- [ ] Loading 畫面出現
- [ ] Platformer2D 正確載入
- [ ] 按 ESC 開啟暫停選單
- [ ] Time.timeScale 變成 0（遊戲凍結）
- [ ] 按 Resume，暫停選單關閉，遊戲恢復
- [ ] 再按 ESC 暫停
- [ ] 按 Retry，Loading 畫面出現，場景重新載入
- [ ] 按 ESC 暫停後按 Main Menu
- [ ] 正確返回 MainMenuScene
- [ ] 每次場景切換後 Time.timeScale 都是 1

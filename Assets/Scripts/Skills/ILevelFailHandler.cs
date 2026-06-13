/// <summary>
/// 關卡失敗處理介面。
/// 由關卡管理器 (LoopManager / DeadBodyManager 等) 實作。
/// Hazard 等「非自身機制死亡」來源透過此介面通知失敗,不直接依賴特定管理器。
/// </summary>
public interface ILevelFailHandler
{
    /// <summary>宣告關卡失敗 (顯示失敗文字後整關重來)。</summary>
    void FailLevel(string reason);
}

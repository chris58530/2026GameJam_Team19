/// <summary>
/// 關卡 Prefab 中的元件若需要接收執行時資料，需實作此介面。
/// GameSceneController 會在實例化關卡 Prefab 後，
/// 自動找到所有實作此介面的元件並呼叫 Initialize()。
/// 
/// 使用方式：
///   在你的關卡腳本上加上 : ILevelInitializable
///   然後實作 Initialize(LevelRunContext context) 方法。
/// 
/// 範例：
///   public class LevelStartController : MonoBehaviour, ILevelInitializable
///   {
///       public void Initialize(LevelRunContext context)
///       {
///           Debug.Log("關卡 ID: " + context.selectedLevelId);
///           Debug.Log("難度: " + context.difficulty);
///       }
///   }
/// </summary>
public interface ILevelInitializable
{
    void Initialize(LevelRunContext context);
}

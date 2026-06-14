/// <summary>
/// Components in a level Prefab that need to receive runtime data should implement this interface.
/// After instantiating the level Prefab, GameSceneController automatically finds all components
/// implementing this interface and calls Initialize().
/// 
/// Usage:
///   Add : ILevelInitializable to your level script
///   then implement the Initialize(LevelRunContext context) method.
/// 
/// Example:
///   public class LevelStartController : MonoBehaviour, ILevelInitializable
///   {
///       public void Initialize(LevelRunContext context)
///       {
///           Debug.Log("Level ID: " + context.selectedLevelId);
///           Debug.Log("Difficulty: " + context.difficulty);
///       }
///   }
/// </summary>
public interface ILevelInitializable
{
    void Initialize(LevelRunContext context);
}

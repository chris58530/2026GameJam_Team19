/// <summary>
/// Level-failure handling interface.
/// Implemented by the level manager (LoopManager / DeadBodyManager, etc.).
/// Sources of "death not caused by the player's own mechanics" (such as Hazard) notify failure through this interface instead of depending directly on a specific manager.
/// </summary>
public interface ILevelFailHandler
{
    /// <summary>Declares the level failed (shows the failure text, then restarts the whole level).</summary>
    void FailLevel(string reason);
}

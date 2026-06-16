using UnityEngine;

/// <summary>
/// Ending animation controller (optional/backup).
/// If you do not use VideoPlayer but instead build the ending animation with the Unity Animator,
/// you can add an Animation Event on the last frame to call OnEndingAnimationFinished().
/// 
/// Currently EndingUI has built-in VideoPlayer support; the buttons are shown automatically when the video finishes.
/// This script is only needed when you build the ending animation with the Animator (not VideoPlayer).
/// </summary>
public class EndingAnimationController : MonoBehaviour
{
    private bool hasFinished = false;

    /// <summary>
    /// Call this method after the ending animation finishes playing.
    /// How to connect: an Animation Event on the last frame calls this function.
    /// </summary>
    public void OnEndingAnimationFinished()
    {
        if (hasFinished) return;
        hasFinished = true;

        Debug.Log("[EndingAnimationController] Ending animation finished, showing buttons.");

        if (EndingUI.Instance != null)
        {
            EndingUI.Instance.ShowButtons();
        }
        else
        {
            Debug.LogWarning("[EndingAnimationController] EndingUI.Instance does not exist!");
        }
    }
}

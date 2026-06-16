using System.Collections;

/// <summary>
/// Card-draw cinematic interface. Called by CorpseSkillSystem at two points in the draw flow, decoupling the cinematic from the draw logic.
/// Both methods are coroutines and the system "waits until they finish" before continuing, so you can do camera zoom, animations, etc. inside them.
/// The game is paused (timeScale=0) for the whole draw, so implementations should use unscaled time (DOTween SetUpdate(true) / WaitForSecondsRealtime).
/// </summary>
public interface ISkillDrawPresenter
{
    /// <summary>At the start of the draw, before the card is shown. Usually zooms in on the player.</summary>
    IEnumerator PlayIntro();

    /// <summary>After the result is drawn, before resuming the game. Usually plays the drinking animation + zoom out.</summary>
    IEnumerator PlayOutro(CorpseSkillType result);
}

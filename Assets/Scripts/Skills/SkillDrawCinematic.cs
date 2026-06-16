using System.Collections;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// Card-draw cinematic (camera zoom + drinking animation). Implements ISkillDrawPresenter, attached to the GameManager;
/// CorpseSkillSystem picks it up automatically and calls it before and after a draw. It is decoupled from the draw logic and can be removed directly if not wanted.
///
/// Flow:
///   PlayIntro  -> disable camera follow, zoom in on the player.
///   (middle)   -> CorpseSkillSystem shows the card / slot machine.
///   PlayOutro  -> play the drinking animation (Animator or placeholder tween) -> zoom out -> re-enable camera follow.
///
/// The whole section is paused, so everything uses unscaled time.
/// </summary>
public class SkillDrawCinematic : MonoBehaviour, ISkillDrawPresenter
{
    [Header("Camera")]
    [Tooltip("The camera to zoom. Leave empty to auto-grab Camera.main")]
    public Camera cam;

    [Tooltip("Camera follow script (disabled during zoom). Leave empty to auto-find it on the camera")]
    public CameraFollow2D follow;

    [Tooltip("Orthographic size after zooming in (smaller = closer)")]
    public float zoomInSize = 3.5f;

    [Tooltip("Duration of zoom in / out in seconds")]
    public float zoomDuration = 0.4f;

    [Tooltip("Camera offset when focusing during zoom in (the point the camera looks at relative to the player). A positive Y raises the camera center upward so the character sits in the lower part of the screen, leaving the top for the card-draw UI.")]
    public Vector2 focusOffset = new Vector2(0f, 2f);

    [Header("Character animation (card-draw cinematic)")]
    [Tooltip("Player Animator (optional). Leave empty to auto-find it on the player (including children)")]
    public Animator playerAnimator;

    [Tooltip("Name of the Idle animation state played continuously during the draw")]
    public string idleState = "idle";

    [Tooltip("Name of the drinking animation state played after the draw (automatically skipped if the state does not exist)")]
    public string drinkState = "drink";

    [Tooltip("Duration of the drinking cinematic in seconds")]
    public float drinkDuration = 0.8f;

    [Tooltip("Use a DOTween placeholder (player gulp scale) when there is no Animator or the drink state can't be found")]
    public bool usePlaceholderIfNoAnimator = true;

    [Tooltip("Player Tag")]
    public string playerTag = "Player";

    private Transform _player;
    private float _baseSize;
    private Vector3 _baseCamPos;

    // During the draw, temporarily switch the Animator to UnscaledTime (so it plays even at timeScale=0), then restore it when done
    private AnimatorUpdateMode _origUpdateMode;
    private bool _animOverridden;

    private void Awake()
    {
        if (cam == null) cam = Camera.main;
        if (follow == null && cam != null) follow = cam.GetComponent<CameraFollow2D>();
        if (follow == null) follow = FindAnyObjectByType<CameraFollow2D>();
        ResolvePlayer();
    }

    private void ResolvePlayer()
    {
        if (_player == null)
        {
            var p = GameObject.FindGameObjectWithTag(playerTag);
            if (p != null) _player = p.transform;
        }
        // Auto-grab the Animator from the player (including children) for idle / drink during the draw
        if (playerAnimator == null && _player != null)
            playerAnimator = _player.GetComponentInChildren<Animator>();
    }

    private CorpseSkillSystem _skillSystem;

    /// <summary>Gets the color matching the drawn skill (reuses the color scheme from CorpseSkillSystem).</summary>
    private Color ResolveSkillColor(CorpseSkillType result)
    {
        if (_skillSystem == null)
            _skillSystem = GetComponent<CorpseSkillSystem>();
        if (_skillSystem == null)
            _skillSystem = FindAnyObjectByType<CorpseSkillSystem>();
        return _skillSystem != null ? _skillSystem.ColorFor(result) : Color.white;
    }

    public IEnumerator PlayIntro()
    {
        if (cam == null) yield break;
        ResolvePlayer();

        if (follow != null) follow.enabled = false;

        // During the draw the character keeps playing idle (it must play even while paused -> switch to UnscaledTime)
        BeginAnimOverride();
        PlayState(idleState);

        _baseSize = cam.orthographicSize;
        _baseCamPos = cam.transform.position;

        if (_player != null)
        {
            Vector3 target = new Vector3(
                _player.position.x + focusOffset.x,
                _player.position.y + focusOffset.y,
                cam.transform.position.z);
            cam.transform.DOMove(target, zoomDuration).SetUpdate(true).SetEase(Ease.OutCubic);
        }
        DOTween.To(() => cam.orthographicSize, x => cam.orthographicSize = x, zoomInSize, zoomDuration)
            .SetUpdate(true).SetEase(Ease.OutCubic);

        yield return new WaitForSecondsRealtime(zoomDuration);
    }

    public IEnumerator PlayOutro(CorpseSkillType result)
    {
        ResolvePlayer();

        // Play the drinking animation right after the draw; fall back to the placeholder tween if the drink state does not exist
        if (playerAnimator != null && HasState(drinkState))
        {
            PlayState(drinkState);
            yield return new WaitForSecondsRealtime(drinkDuration);
        }
        else if (usePlaceholderIfNoAnimator && _player != null)
        {
            // Placeholder "gulp" feel: DOPunchScale automatically returns to the original scale, leaving no residue
            _player.DOComplete();
            _player.DOPunchScale(new Vector3(0.18f, 0.18f, 0f), drinkDuration, 4, 0.6f).SetUpdate(true);
            yield return new WaitForSecondsRealtime(drinkDuration);
        }

        // The moment the drink finishes: raise an aura of "the drawn skill's color" at the player's feet (a rising vibe for the upgrade feel),
        // tying the drinking cinematic to the drawn skill. timeScale=0, so use unscaled time.
        if (_player != null)
        {
            Color skillColor = ResolveSkillColor(result);
            JuiceFX.RisingAura(_player.position + Vector3.down * 0.5f, skillColor,
                width: 0.9f, riseSpeed: 4.5f, duration: 0.7f, particleLifetime: 0.95f,
                size: 0.28f, rate: 60, sortingOrder: 50, unscaled: true);
            JuiceFX.Shake(0.16f, 0.25f);
        }

        // After drinking, return to idle (keep idle during zoom out; hand control back to PlayerController2D once the game resumes)
        PlayState(idleState);

        // Zoom out back to the original
        if (cam != null)
        {
            // Target position: align directly to "the player's current position + follow offset", not the old camera coordinate (_baseCamPos) recorded before the cinematic.
            // Otherwise, if the camera had not yet snapped to the player during the draw (e.g. still chasing right after a jump), zooming out would first fly back to the old point, then follow would re-enable and pull to the character,
            // which looks like a two-stage jump of "move somewhere first, then move to the character".
            // While paused Time.timeScale = 0, so CameraFollow2D won't update on its own; here we use a tween to bring the camera to the correct landing spot.
            Vector3 outTarget = _baseCamPos;
            if (_player != null)
            {
                Vector3 followOffset = (follow != null)
                    ? follow.offset
                    : new Vector3(focusOffset.x, focusOffset.y, 0f);
                outTarget = new Vector3(
                    _player.position.x + followOffset.x,
                    _player.position.y + followOffset.y,
                    cam.transform.position.z);
            }

            cam.transform.DOMove(outTarget, zoomDuration).SetUpdate(true).SetEase(Ease.InOutCubic);
            DOTween.To(() => cam.orthographicSize, x => cam.orthographicSize = x, _baseSize, zoomDuration)
                .SetUpdate(true).SetEase(Ease.InOutCubic);
            yield return new WaitForSecondsRealtime(zoomDuration);
        }

        // Restore the Animator update mode (after the game resumes, PlayerController2D takes over driving the animation)
        EndAnimOverride();

        if (follow != null) follow.enabled = true;
    }

    // ---------------- Character animation helpers ----------------

    /// <summary>During the draw, switch the Animator to UnscaledTime so the animation still plays while timeScale=0.</summary>
    private void BeginAnimOverride()
    {
        if (playerAnimator == null || _animOverridden) return;
        _origUpdateMode = playerAnimator.updateMode;
        playerAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
        _animOverridden = true;
    }

    /// <summary>Restore the Animator's original update mode.</summary>
    private void EndAnimOverride()
    {
        if (playerAnimator == null || !_animOverridden) return;
        playerAnimator.updateMode = _origUpdateMode;
        _animOverridden = false;
    }

    /// <summary>Play the given animation state (skipped if the state does not exist).</summary>
    private void PlayState(string state)
    {
        if (!HasState(state)) return;
        playerAnimator.Play(Animator.StringToHash(state), 0, 0f);
    }

    /// <summary>Whether the given state exists on the Animator's Base Layer.</summary>
    private bool HasState(string state)
    {
        if (playerAnimator == null || string.IsNullOrEmpty(state)) return false;
        return playerAnimator.HasState(0, Animator.StringToHash(state));
    }
}

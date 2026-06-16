using UnityEngine;

/// <summary>
/// Lightweight screen shake. It is layered "on top of" the position computed by the camera
/// follow (CameraFollow2D), so it does not affect the follow logic, does not change
/// Time.timeScale, and the camera itself has no Collider, so it never touches any game
/// logic or collisions.
///
/// Key technique for not polluting the follow:
///   - In the Update phase (always before all LateUpdates): first "revert" the offset
///     applied last frame so the camera returns to a clean position.
///   - CameraFollow2D reads the clean position in LateUpdate (order 100) and works normally.
///   - This component applies the new shake offset in LateUpdate (very high order).
/// This way CameraFollow2D never reads coordinates polluted by the shake.
///
/// No manual attachment needed: JuiceFX.Shake() automatically creates this component on the main camera.
/// </summary>
[DefaultExecutionOrder(10000)]
public class ScreenShake : MonoBehaviour
{
    private float _duration;
    private float _elapsed;
    private float _strength;

    [Tooltip("Shake frequency (higher = faster shaking)")]
    public float frequency = 26f;

    private Vector3 _appliedOffset;
    private Vector2 _seed;

    /// <summary>Triggers a shake. A stronger shake will not be overridden by a weaker one.</summary>
    public void Shake(float strength, float duration)
    {
        if (strength <= 0f || duration <= 0f) return;

        // A stronger shake still in progress is not interrupted by a weaker one
        if (strength >= _strength || _elapsed >= _duration)
        {
            _strength = strength;
            _duration = duration;
            _elapsed = 0f;
            _seed = new Vector2(Random.value * 100f, Random.value * 100f);
        }
    }

    private void Update()
    {
        // Before all LateUpdates, revert the offset applied last frame -> CameraFollow2D reads a clean position
        if (_appliedOffset != Vector3.zero)
        {
            transform.position -= _appliedOffset;
            _appliedOffset = Vector3.zero;
        }
    }

    private void LateUpdate()
    {
        if (_elapsed >= _duration)
        {
            _strength = 0f;
            return;
        }

        _elapsed += Time.unscaledDeltaTime;

        float damper = 1f - Mathf.Clamp01(_elapsed / _duration);
        float amp = _strength * damper * damper; // squared falloff, smoother at the tail

        float t = Time.unscaledTime * frequency;
        // PerlinNoise produces a more natural shake trajectory than pure random
        float ox = (Mathf.PerlinNoise(_seed.x, t) - 0.5f) * 2f;
        float oy = (Mathf.PerlinNoise(_seed.y, t) - 0.5f) * 2f;

        _appliedOffset = new Vector3(ox, oy, 0f) * amp;
        transform.position += _appliedOffset;
    }
}

using UnityEngine;

/// <summary>
/// 輕量螢幕震動。疊加在相機跟隨 (CameraFollow2D) 算完的位置「之上」,
/// 不影響跟隨邏輯、不改 Time.timeScale、相機本身也沒有 Collider,
/// 因此不會動到任何遊戲邏輯或碰撞。
///
/// 不污染跟隨的關鍵作法:
///   - Update 階段 (一定早於所有 LateUpdate):先把上一幀疊加的偏移「還原」,
///     讓相機回到乾淨位置。
///   - CameraFollow2D 在 LateUpdate (order 100) 讀到的是乾淨位置,正常運作。
///   - 本元件在 LateUpdate (超高 order) 才疊加新的震動偏移。
/// 如此 CameraFollow2D 永遠讀不到被震動污染的座標。
///
/// 不需手動掛載:JuiceFX.Shake() 會自動在主相機上建立此元件。
/// </summary>
[DefaultExecutionOrder(10000)]
public class ScreenShake : MonoBehaviour
{
    private float _duration;
    private float _elapsed;
    private float _strength;

    [Tooltip("震動頻率 (越大抖越快)")]
    public float frequency = 26f;

    private Vector3 _appliedOffset;
    private Vector2 _seed;

    /// <summary>觸發一次震動。較強的震動不會被較弱的覆蓋。</summary>
    public void Shake(float strength, float duration)
    {
        if (strength <= 0f || duration <= 0f) return;

        // 仍在進行的較強震動不被弱震打斷
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
        // 在所有 LateUpdate 之前,還原上一幀疊加的偏移 → CameraFollow2D 讀到乾淨位置
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
        float amp = _strength * damper * damper; // 平方衰減,尾段更柔順

        float t = Time.unscaledTime * frequency;
        // PerlinNoise 產生比純亂數更自然的搖晃軌跡
        float ox = (Mathf.PerlinNoise(_seed.x, t) - 0.5f) * 2f;
        float oy = (Mathf.PerlinNoise(_seed.y, t) - 0.5f) * 2f;

        _appliedOffset = new Vector3(ox, oy, 0f) * amp;
        transform.position += _appliedOffset;
    }
}

using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// A pure-code "game feel" toolbox: particle bursts, squash &amp; stretch scaling, and screen shake.
///
/// Design priorities (consistent with "don't affect logic, don't touch Colliders"):
///   - Particles are always independent, Collider-less temporary objects that auto-destroy when done.
///   - No art assets required: textures are drawn in code as soft-edged dots, using the built-in Sprites/Default material.
///   - Scaling only affects the "visual Transform" specified by the caller (e.g. the player's Visual child object),
///     never the Collider attached elsewhere.
///   - Shake goes through ScreenShake, only layering the camera display position, without changing Time.timeScale.
/// </summary>
public static class JuiceFX
{
    private static Texture2D _softTex;
    private static Material _alphaMat;

    // ───────────────────────── Resources (lazy creation) ─────────────────────────

    /// <summary>Draws a dot texture in code with a solid center and soft edges, used as a generic particle image.</summary>
    private static Texture2D SoftTex()
    {
        if (_softTex != null) return _softTex;

        const int size = 32;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            name = "JuiceFX_SoftDot"
        };

        float r = size * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) - r;
                float dy = (y + 0.5f) - r;
                float d = Mathf.Sqrt(dx * dx + dy * dy) / r; // 0=center 1=edge
                float a = Mathf.Clamp01(1f - d);
                a *= a; // soft edge
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();
        _softTex = tex;
        return tex;
    }

    private static Material AlphaMat()
    {
        if (_alphaMat != null) return _alphaMat;

        var shader = Shader.Find("Sprites/Default");
        _alphaMat = new Material(shader)
        {
            name = "JuiceFX_ParticleMat",
            mainTexture = SoftTex()
        };
        return _alphaMat;
    }

    private static Sprite _softSprite;

    /// <summary>Soft-edged dot Sprite (used by the Image of UI particles).</summary>
    public static Sprite SoftSprite()
    {
        if (_softSprite != null) return _softSprite;
        var tex = SoftTex();
        _softSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
            new Vector2(0.5f, 0.5f), tex.width);
        _softSprite.name = "JuiceFX_SoftSprite";
        return _softSprite;
    }

    // ───────────────────────── Particles ─────────────────────────

    /// <summary>
    /// Emits a ring of particles at the given position (a one-shot burst that auto-destroys when done).
    /// When unscaled=true it uses unscaled time, so particles can still play when Time.timeScale=0 (e.g. a draw-card pause).
    /// </summary>
    public static void Burst(
        Vector3 position,
        Color color,
        int count = 12,
        float speed = 4f,
        float size = 0.25f,
        float lifetime = 0.5f,
        float gravity = 0f,
        int sortingOrder = 50,
        bool unscaled = false)
    {
        var go = new GameObject("FX_Burst");
        go.transform.position = position;

        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmitting); // stop before setting parameters

        var main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.duration = lifetime + 0.1f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.65f, lifetime);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.35f, speed);
        main.startSize = new ParticleSystem.MinMaxCurve(size * 0.55f, size);
        main.startColor = color;
        main.gravityModifier = gravity;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.useUnscaledTime = unscaled; // must still play during a draw-card pause (timeScale=0)
        main.stopAction = ParticleSystemStopAction.Destroy; // auto-destroy the whole object after the system ends

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)Mathf.Max(1, count)) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.08f;
        shape.radiusThickness = 1f;

        // Gradually shrink + fade out for a more natural finish
        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, SizeCurve());

        var col = ps.colorOverLifetime;
        col.enabled = true;
        col.color = FadeGradient(color);

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = AlphaMat();
        renderer.sortingOrder = sortingOrder;

        ps.Play();

        // Safety net: clean up even if stopAction doesn't fire
        Object.Destroy(go, main.duration + lifetime + 1f);
    }

    /// <summary>Grayish-white dust burst (footsteps / landing / jumping).</summary>
    public static void Dust(Vector3 position, int count = 10, float strength = 1f)
    {
        Burst(
            position,
            new Color(0.85f, 0.82f, 0.72f, 0.85f),
            count: count,
            speed: 3.2f * strength,
            size: 0.26f,
            lifetime: 0.45f,
            gravity: 0.12f);
    }

    /// <summary>
    /// Death burst: a cluster of colored fragments explodes outward + a ring of white flash particles.
    /// Used for the visual feedback of a "fatal death" (stepping into a Hazard / falling out of bounds).
    /// </summary>
    public static void DeathBurst(Vector3 position, Color tint)
    {
        Burst(position, tint, count: 28, speed: 7.5f, size: 0.32f, lifetime: 0.6f, gravity: 0.7f);
        Burst(position, Color.white, count: 12, speed: 5f, size: 0.28f, lifetime: 0.4f, gravity: 0f);
    }

    /// <summary>
    /// Rising aura (sense of leveling up). Particles continuously drift upward from a horizontal band at footPosition,
    /// sway slightly left and right, and shrink and fade out, like the rising wind of an RPG level-up. 2D friendly:
    /// everything stays on the XY plane and does not spread out in depth.
    /// Emits for duration seconds; when unscaled=true it can play at timeScale=0 (e.g. a drinking pause), and auto-destroys when done.
    /// </summary>
    public static void RisingAura(
        Vector3 footPosition,
        Color color,
        float width = 0.9f,
        float riseSpeed = 4.2f,
        float duration = 0.7f,
        float particleLifetime = 0.9f,
        float size = 0.26f,
        int rate = 55,
        int sortingOrder = 50,
        bool unscaled = false)
    {
        var go = new GameObject("FX_RisingAura");
        go.transform.position = footPosition;

        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        var main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.duration = duration;
        main.startLifetime = new ParticleSystem.MinMaxCurve(particleLifetime * 0.7f, particleLifetime);
        main.startSpeed = 0f; // rise is driven by velocityOverLifetime to avoid radial spraying
        main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size);
        main.startColor = color;
        main.gravityModifier = -0.04f; // slightly negative, keeps drifting upward
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.useUnscaledTime = unscaled;
        main.stopAction = ParticleSystemStopAction.Destroy;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = rate;

        // A flat horizontal band at the feet (2D: distributed only on the XY plane)
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(width, 0.1f, 0.01f);

        // Rise + slight left-right sway
        var vol = ps.velocityOverLifetime;
        vol.enabled = true;
        vol.space = ParticleSystemSimulationSpace.Local;
        vol.x = new ParticleSystem.MinMaxCurve(-0.6f, 0.6f);
        vol.y = new ParticleSystem.MinMaxCurve(riseSpeed * 0.7f, riseSpeed);
        vol.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        // Shrink + fade out
        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, SizeCurve());

        var col = ps.colorOverLifetime;
        col.enabled = true;
        col.color = FadeGradient(color);

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = AlphaMat();
        renderer.sortingOrder = sortingOrder;

        ps.Play();

        DOVirtual.DelayedCall(duration + particleLifetime + 0.5f, () =>
        {
            if (go != null) Object.Destroy(go);
        }, true);
    }

    private static ParticleSystem.MinMaxGradient FadeGradient(Color c)
    {
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(c, 0f), new GradientColorKey(c, 1f) },
            new[] { new GradientAlphaKey(c.a, 0f), new GradientAlphaKey(0f, 1f) });
        return new ParticleSystem.MinMaxGradient(grad);
    }

    private static AnimationCurve SizeCurve()
    {
        var curve = new AnimationCurve();
        curve.AddKey(0f, 1f);
        curve.AddKey(1f, 0f);
        return curve;
    }

    // ───────────────────────── Scale Elasticity ─────────────────────────

    /// <summary>
    /// Squash &amp; stretch: first squash/stretch the visual Transform to the target ratio, then spring back to the baseline.
    /// sx/sy are multipliers relative to baseScale (e.g. jump 0.8,1.25; land 1.25,0.7).
    /// Only affects the passed-in visual Transform, never any Collider.
    /// </summary>
    public static void Squash(Transform visual, Vector3 baseScale, float sx, float sy, float duration = 0.2f)
    {
        if (visual == null) return;

        visual.DOKill(true); // complete and kill the previous segment to avoid leftover stacking
        visual.localScale = baseScale;

        var target = new Vector3(baseScale.x * sx, baseScale.y * sy, baseScale.z);

        Sequence seq = DOTween.Sequence();
        seq.SetTarget(visual);
        seq.Append(visual.DOScale(target, duration * 0.35f).SetEase(Ease.OutQuad));
        seq.Append(visual.DOScale(baseScale, duration * 0.65f).SetEase(Ease.OutBack));
    }

    /// <summary>Performs a single elastic punch on the visual Transform (good for pickups, hits, etc.).</summary>
    public static void Punch(Transform visual, Vector3 baseScale, float strength = 0.25f, float duration = 0.3f)
    {
        if (visual == null) return;
        visual.DOKill(true);
        visual.localScale = baseScale;
        visual.DOPunchScale(baseScale * strength, duration, 6, 0.7f).SetTarget(visual);
    }

    // ───────────────────────── Shake ─────────────────────────

    /// <summary>Triggers a screen shake (automatically creates ScreenShake on the main camera; doesn't change timeScale or touch Colliders).</summary>
    public static void Shake(float strength, float duration)
    {
        var cam = Camera.main;
        if (cam == null) return;

        var shake = cam.GetComponent<ScreenShake>();
        if (shake == null) shake = cam.gameObject.AddComponent<ScreenShake>();
        shake.Shake(strength, duration);
    }

    // ───────────────────────── Full-Screen Flash ─────────────────────────

    /// <summary>
    /// Full-screen color flash (e.g. the red flash on failure). Creates an independent Overlay Canvas + Image,
    /// fades out from the given color and then auto-destroys. Does not receive clicks, and does not affect any logic or Collider.
    /// Uses unscaled time so it can play even when the game is paused (timeScale=0).
    /// </summary>
    public static void ScreenFlash(Color color, float duration = 0.45f)
    {
        var go = new GameObject("FX_ScreenFlash");

        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000;

        var img = go.AddComponent<Image>();
        img.raycastTarget = false;
        img.color = color;

        var rt = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        img.DOFade(0f, duration)
            .SetUpdate(true)
            .SetEase(Ease.OutQuad)
            .OnComplete(() => Object.Destroy(go));
    }

    // ───────────────────────── UI Particle Burst ─────────────────────────

    /// <summary>
    /// Emits a ring of radial particles on the UI (using Image + DOTween, guaranteed to render above the draw-card UI).
    /// Creates an Overlay Canvas at a higher layer than the draw-card Canvas; particles radiate outward from the screen
    /// center (an offset can be added) + shrink and fade out, like the celebratory burst of "congrats on the draw!".
    /// Uses unscaled time throughout so it can play during a draw-card pause (timeScale=0), and auto-destroys when done.
    /// </summary>
    public static void UIBurst(
        Color color,
        int count = 22,
        float spread = 280f,
        float size = 38f,
        float duration = 0.85f,
        Vector2 centerOffset = default)
    {
        var go = new GameObject("FX_UIBurst");

        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 6000; // higher than the draw-card UI (1000) and the full-screen flash (5000)

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        var rootRT = (RectTransform)go.transform;
        var sprite = SoftSprite();

        for (int i = 0; i < count; i++)
        {
            var pGo = new GameObject("p", typeof(RectTransform));
            var rt = (RectTransform)pGo.transform;
            rt.SetParent(rootRT, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = centerOffset;

            float ps = size * Random.Range(0.55f, 1.2f);
            rt.sizeDelta = new Vector2(ps, ps);

            var img = pGo.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;

            // Radial direction: even distribution + jitter to avoid being too regular
            float ang = (i / (float)count) * Mathf.PI * 2f + Random.Range(-0.25f, 0.25f);
            float dist = spread * Random.Range(0.6f, 1.15f);
            Vector2 dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
            Vector2 dest = centerOffset + dir * dist;
            dest.y -= dist * 0.25f; // droop slightly at the tail, like fireworks falling

            float dur = duration * Random.Range(0.7f, 1f);
            rt.DOAnchorPos(dest, dur).SetUpdate(true).SetEase(Ease.OutCubic);
            rt.DOScale(0.15f, dur).SetUpdate(true).SetEase(Ease.InQuad);
            img.DOFade(0f, dur).SetUpdate(true).SetEase(Ease.InQuad);
        }

        // Use an unscaled delayed call to destroy (Object.Destroy(go, t) won't fire at timeScale=0)
        DOVirtual.DelayedCall(duration + 0.4f, () =>
        {
            if (go != null) Object.Destroy(go);
        }, true);
    }
}

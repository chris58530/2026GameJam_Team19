using UnityEngine;
using DG.Tweening;

/// <summary>
/// 純程式的「回饋感」工具箱:粒子爆發、縮放彈性 (squash &amp; stretch)、螢幕震動。
///
/// 設計重點 (符合「不影響邏輯、不碰 Collider」):
///   - 粒子一律是獨立、無 Collider 的暫時物件,播完自動銷毀。
///   - 不需要任何美術素材:貼圖由程式畫出柔邊圓點,材質用內建 Sprites/Default。
///   - 縮放只作用在呼叫端指定的「視覺 Transform」(例如玩家的 Visual 子物件),
///     不會動到掛在別處的 Collider。
///   - 震動透過 ScreenShake,只疊加相機顯示位置,不改 Time.timeScale。
/// </summary>
public static class JuiceFX
{
    private static Texture2D _softTex;
    private static Material _alphaMat;

    // ───────────────────────── 資源 (懶建立) ─────────────────────────

    /// <summary>程式畫一張中心實、邊緣柔的圓點貼圖,當作通用粒子圖。</summary>
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
                float d = Mathf.Sqrt(dx * dx + dy * dy) / r; // 0=中心 1=邊緣
                float a = Mathf.Clamp01(1f - d);
                a *= a; // 柔邊
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

    // ───────────────────────── 粒子 ─────────────────────────

    /// <summary>
    /// 在指定位置噴出一圈粒子 (一次性爆發,播完自動銷毀)。
    /// </summary>
    public static void Burst(
        Vector3 position,
        Color color,
        int count = 12,
        float speed = 4f,
        float size = 0.25f,
        float lifetime = 0.5f,
        float gravity = 0f,
        int sortingOrder = 50)
    {
        var go = new GameObject("FX_Burst");
        go.transform.position = position;

        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmitting); // 設定參數前先停

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
        main.stopAction = ParticleSystemStopAction.Destroy; // 系統結束後自動銷毀整個物件

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)Mathf.Max(1, count)) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.08f;
        shape.radiusThickness = 1f;

        // 漸漸縮小 + 淡出,收尾更自然
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

        // 保險:即使 stopAction 沒觸發也會清掉
        Object.Destroy(go, main.duration + lifetime + 1f);
    }

    /// <summary>灰白色塵土爆發 (腳步 / 落地 / 起跳)。</summary>
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

    // ───────────────────────── 縮放彈性 ─────────────────────────

    /// <summary>
    /// Squash &amp; stretch:把視覺 Transform 先壓/拉到目標比例,再用彈性回到基準。
    /// sx/sy 是相對 baseScale 的倍率 (例如起跳 0.8,1.25;落地 1.25,0.7)。
    /// 只作用在傳入的視覺 Transform,不影響任何 Collider。
    /// </summary>
    public static void Squash(Transform visual, Vector3 baseScale, float sx, float sy, float duration = 0.2f)
    {
        if (visual == null) return;

        visual.DOKill(true); // 完成並終止前一段,避免疊加殘留
        visual.localScale = baseScale;

        var target = new Vector3(baseScale.x * sx, baseScale.y * sy, baseScale.z);

        Sequence seq = DOTween.Sequence();
        seq.SetTarget(visual);
        seq.Append(visual.DOScale(target, duration * 0.35f).SetEase(Ease.OutQuad));
        seq.Append(visual.DOScale(baseScale, duration * 0.65f).SetEase(Ease.OutBack));
    }

    /// <summary>對視覺 Transform 做一次彈性 punch (適合拾取、命中等)。</summary>
    public static void Punch(Transform visual, Vector3 baseScale, float strength = 0.25f, float duration = 0.3f)
    {
        if (visual == null) return;
        visual.DOKill(true);
        visual.localScale = baseScale;
        visual.DOPunchScale(baseScale * strength, duration, 6, 0.7f).SetTarget(visual);
    }

    // ───────────────────────── 震動 ─────────────────────────

    /// <summary>觸發螢幕震動 (自動在主相機上建立 ScreenShake;不改 timeScale、不碰 Collider)。</summary>
    public static void Shake(float strength, float duration)
    {
        var cam = Camera.main;
        if (cam == null) return;

        var shake = cam.GetComponent<ScreenShake>();
        if (shake == null) shake = cam.gameObject.AddComponent<ScreenShake>();
        shake.Shake(strength, duration);
    }
}

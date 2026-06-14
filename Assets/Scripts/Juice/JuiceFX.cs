using UnityEngine;
using UnityEngine.UI;
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

    private static Sprite _softSprite;

    /// <summary>柔邊圓點 Sprite (給 UI 粒子的 Image 使用)。</summary>
    public static Sprite SoftSprite()
    {
        if (_softSprite != null) return _softSprite;
        var tex = SoftTex();
        _softSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
            new Vector2(0.5f, 0.5f), tex.width);
        _softSprite.name = "JuiceFX_SoftSprite";
        return _softSprite;
    }

    // ───────────────────────── 粒子 ─────────────────────────

    /// <summary>
    /// 在指定位置噴出一圈粒子 (一次性爆發,播完自動銷毀)。
    /// unscaled=true 時用未縮放時間,讓粒子在 Time.timeScale=0 (例如抽卡暫停) 也能播。
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
        main.useUnscaledTime = unscaled; // 抽卡 (timeScale=0) 時仍要能播
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

    /// <summary>
    /// 死亡爆裂:一團彩色碎片向外炸開 + 一圈白色閃光粒子。
    /// 用於「失敗死亡」(踏入 Hazard / 掉出區域) 的視覺表現。
    /// </summary>
    public static void DeathBurst(Vector3 position, Color tint)
    {
        Burst(position, tint, count: 28, speed: 7.5f, size: 0.32f, lifetime: 0.6f, gravity: 0.7f);
        Burst(position, Color.white, count: 12, speed: 5f, size: 0.28f, lifetime: 0.4f, gravity: 0f);
    }

    /// <summary>
    /// 升騰光氣 (升級感)。粒子從 footPosition 的一條水平帶持續往上飄、左右輕擺、縮小淡出,
    /// 像 RPG 升級時的上升風。2D 友善:全部落在 XY 平面,不會往深度散開。
    /// 持續 duration 秒發射,unscaled=true 時於 timeScale=0 (喝酒暫停) 也能播,播完自動銷毀。
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
        main.startSpeed = 0f; // 由 velocityOverLifetime 控制上升,避免徑向亂噴
        main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size);
        main.startColor = color;
        main.gravityModifier = -0.04f; // 微負,持續上飄
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.useUnscaledTime = unscaled;
        main.stopAction = ParticleSystemStopAction.Destroy;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = rate;

        // 腳底一條扁平水平帶 (2D:只在 XY 平面分布)
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(width, 0.1f, 0.01f);

        // 上升 + 左右輕擺
        var vol = ps.velocityOverLifetime;
        vol.enabled = true;
        vol.space = ParticleSystemSimulationSpace.Local;
        vol.x = new ParticleSystem.MinMaxCurve(-0.6f, 0.6f);
        vol.y = new ParticleSystem.MinMaxCurve(riseSpeed * 0.7f, riseSpeed);
        vol.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        // 縮小 + 淡出
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

    // ───────────────────────── 全螢幕閃光 ─────────────────────────

    /// <summary>
    /// 全螢幕色閃 (例如失敗時的紅閃)。建立一個獨立的 Overlay Canvas + Image,
    /// 從指定顏色淡出後自動銷毀。不接收點擊、不影響任何邏輯或 Collider。
    /// 使用 unscaled 時間,即使遊戲暫停 (timeScale=0) 也能播。
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

    // ───────────────────────── UI 粒子爆發 ─────────────────────────

    /// <summary>
    /// 在 UI 上噴一圈放射狀粒子 (用 Image + DOTween,保證顯示在抽卡 UI 之上)。
    /// 自建一個比抽卡 Canvas 更高層的 Overlay Canvas,粒子從畫面中心 (可加 offset)
    /// 向外放射 + 縮小淡出,像「恭喜抽到!」的慶祝爆發。
    /// 全程用未縮放時間,於抽卡暫停 (timeScale=0) 也能播,播完自動銷毀。
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
        canvas.sortingOrder = 6000; // 高於抽卡 UI(1000)與全螢幕閃光(5000)

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

            // 放射方向:均勻分布 + 抖動,避免太規律
            float ang = (i / (float)count) * Mathf.PI * 2f + Random.Range(-0.25f, 0.25f);
            float dist = spread * Random.Range(0.6f, 1.15f);
            Vector2 dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
            Vector2 dest = centerOffset + dir * dist;
            dest.y -= dist * 0.25f; // 尾段略微下垂,像煙火灑落

            float dur = duration * Random.Range(0.7f, 1f);
            rt.DOAnchorPos(dest, dur).SetUpdate(true).SetEase(Ease.OutCubic);
            rt.DOScale(0.15f, dur).SetUpdate(true).SetEase(Ease.InQuad);
            img.DOFade(0f, dur).SetUpdate(true).SetEase(Ease.InQuad);
        }

        // 用未縮放的延遲呼叫銷毀 (timeScale=0 下 Object.Destroy(go, t) 不會觸發)
        DOVirtual.DelayedCall(duration + 0.4f, () =>
        {
            if (go != null) Object.Destroy(go);
        }, true);
    }
}

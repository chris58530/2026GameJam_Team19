using System.Collections;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 抽卡演出 (相機 zoom + 喝酒動畫)。實作 ISkillDrawPresenter,掛在 GameManager 上,
/// CorpseSkillSystem 會自動抓到並在抽卡前後呼叫。與抽卡邏輯解耦,不要可直接移除元件。
///
/// 流程:
///   PlayIntro  → 停用相機跟隨、zoom in 到玩家。
///   (中間)     → CorpseSkillSystem 顯示卡片 / 拉霸。
///   PlayOutro  → 播喝酒動畫 (Animator 或佔位 tween) → zoom out → 恢復相機跟隨。
///
/// 整段為暫停狀態,全部用未縮放時間。
/// </summary>
public class SkillDrawCinematic : MonoBehaviour, ISkillDrawPresenter
{
    [Header("相機")]
    [Tooltip("要 zoom 的相機。留空自動抓 Camera.main")]
    public Camera cam;

    [Tooltip("相機跟隨腳本 (zoom 期間會停用)。留空自動從相機上找")]
    public CameraFollow2D follow;

    [Tooltip("zoom in 後的正交 size (越小越近)")]
    public float zoomInSize = 3.5f;

    [Tooltip("zoom in / out 的秒數")]
    public float zoomDuration = 0.4f;

    [Tooltip("zoom in 聚焦時的相機偏移 (相機看的點相對玩家)。Y 設正值會把相機中心往上抬,讓主角落在畫面下方、上方留給抽牌 UI。")]
    public Vector2 focusOffset = new Vector2(0f, 2f);

    [Header("喝酒動畫")]
    [Tooltip("玩家 Animator (可選)。留空會自動從玩家身上找;再沒有就用佔位 tween")]
    public Animator playerAnimator;

    [Tooltip("Animator 的喝酒 Trigger 名稱")]
    public string drinkTrigger = "Drink";

    [Tooltip("喝酒演出持續秒數")]
    public float drinkDuration = 0.8f;

    [Tooltip("沒有 Animator 時,用 DOTween 佔位 (玩家咕嘟縮放)")]
    public bool usePlaceholderIfNoAnimator = true;

    [Tooltip("玩家 Tag")]
    public string playerTag = "Player";

    private Transform _player;
    private float _baseSize;
    private Vector3 _baseCamPos;

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
        // 注意:不自動抓 Animator。預設走佔位 tween;
        // 有喝酒動畫時再到 Inspector 指定 playerAnimator + drinkTrigger。
    }

    public IEnumerator PlayIntro()
    {
        if (cam == null) yield break;
        ResolvePlayer();

        if (follow != null) follow.enabled = false;

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

        // 喝酒
        if (playerAnimator != null && !string.IsNullOrEmpty(drinkTrigger))
        {
            var prev = playerAnimator.updateMode;
            playerAnimator.updateMode = AnimatorUpdateMode.UnscaledTime; // 暫停時也要能播
            playerAnimator.SetTrigger(drinkTrigger);
            yield return new WaitForSecondsRealtime(drinkDuration);
            playerAnimator.updateMode = prev;
        }
        else if (usePlaceholderIfNoAnimator && _player != null)
        {
            // 佔位「咕嘟」手感:DOPunchScale 自動回到原本縮放,不留殘留
            _player.DOComplete();
            _player.DOPunchScale(new Vector3(0.18f, 0.18f, 0f), drinkDuration, 4, 0.6f).SetUpdate(true);
            yield return new WaitForSecondsRealtime(drinkDuration);
        }

        // zoom out 回原本
        if (cam != null)
        {
            // 目標位置:直接對齊「玩家當前位置 + 跟隨偏移」,而不是進演出前記下的舊相機座標 (_baseCamPos)。
            // 否則若抽卡時相機還沒貼齊玩家 (例如剛跳完還在追),zoom out 會先飛回舊點、follow 再啟用又拉到主角,
            // 看起來就是「先移動到某處才移動到主角」的兩段式跳動。
            // 暫停期間 Time.timeScale = 0,CameraFollow2D 不會自行更新,所以這裡用 tween 把相機帶到正確落點。
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

        if (follow != null) follow.enabled = true;
    }
}

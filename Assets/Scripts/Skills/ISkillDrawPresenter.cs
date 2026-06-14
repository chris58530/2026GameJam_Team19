using System.Collections;

/// <summary>
/// 抽卡演出介面。由 CorpseSkillSystem 在抽卡流程的兩個時機呼叫,讓演出與抽卡邏輯解耦。
/// 兩個方法都是 coroutine,系統會「等它跑完」才繼續,所以可在裡面做相機 zoom、動畫等。
/// 整段抽卡期間遊戲為暫停 (timeScale=0),實作請用未縮放時間 (DOTween SetUpdate(true) / WaitForSecondsRealtime)。
/// </summary>
public interface ISkillDrawPresenter
{
    /// <summary>抽卡開始、卡片顯示前。通常做 zoom in 玩家。</summary>
    IEnumerator PlayIntro();

    /// <summary>抽到結果、恢復遊戲前。通常做喝酒動畫 + zoom out。</summary>
    IEnumerator PlayOutro(CorpseSkillType result);
}

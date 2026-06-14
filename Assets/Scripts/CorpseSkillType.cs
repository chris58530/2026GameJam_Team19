using UnityEngine;

/// <summary>
/// 屍體技能種類。
/// 玩家在每條命開始時抽卡選一種,該條命留下的屍體就會具備此技能。
/// </summary>
public enum CorpseSkillType
{
    /// <summary>普通屍體,單純當作可踩的平台。</summary>
    Normal,

    /// <summary>加速:碰撞器接觸到該屍體時,玩家「水平移動速度」倍增;離開後隨時間遞減回原速。</summary>
    Speed,

    /// <summary>左右橫擺:屍體沿水平方向來回移動,碰到障礙物提前折返。</summary>
    HorizontalSway,

    /// <summary>上下搖擺:屍體沿垂直方向來回移動,碰到障礙物提前折返。</summary>
    VerticalSway,

    /// <summary>向下傳送:玩家經過該屍體時,被傳送到正下方第一個平台上。</summary>
    TeleportDown,

    /// <summary>彈跳:站在這具屍體上按跳,跳躍力倍增(只影響跳躍那一下)。</summary>
    Bounce,

    /// <summary>隨機彈射 (百威):玩家碰到瞬間自動朝上隨機角度彈飛,力道用玩家原始跳躍力。</summary>
    RandomLaunch,

    /// <summary>消失再現 (Asahi):玩家碰到後延遲一段時間消失,1 秒後再出現。</summary>
    BlinkOut,

    /// <summary>限次使用 (冰結):每次踩上/觸碰算一次,用滿次數後屍體消失 (以透明度提示剩餘次數)。</summary>
    LimitedUse,
}

/// <summary>
/// 一張技能卡的設定 (用於關卡牌庫)。
/// type = 技能種類; count = 數量 (作為隨機抽牌的權重,數量越多越容易被抽到)。
/// 採「無限抽、不消耗」:牌庫永遠不會變少,count 只影響出現機率。
/// </summary>
[System.Serializable]
public class CorpseSkillCard
{
    [Tooltip("技能種類")]
    public CorpseSkillType type = CorpseSkillType.Normal;

    [Tooltip("數量 (抽牌權重,越大越常被抽到)")]
    [Min(1)]
    public int count = 1;
}

/// <summary>
/// 技能種類的中文顯示名稱工具。
/// </summary>
public static class CorpseSkillNames
{
    public static string ToDisplay(CorpseSkillType type)
    {
        switch (type)
        {
            case CorpseSkillType.Normal: return "普通";
            case CorpseSkillType.Speed: return "加速";
            case CorpseSkillType.Bounce: return "彈跳";
            case CorpseSkillType.HorizontalSway: return "左右橫擺";
            case CorpseSkillType.VerticalSway: return "上下搖擺";
            case CorpseSkillType.TeleportDown: return "向下傳送";
            case CorpseSkillType.RandomLaunch: return "隨機彈射";
            case CorpseSkillType.BlinkOut: return "消失再現";
            case CorpseSkillType.LimitedUse: return "限次使用";
            default: return type.ToString();
        }
    }
}

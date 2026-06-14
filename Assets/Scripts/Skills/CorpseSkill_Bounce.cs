using UnityEngine;

/// <summary>
/// 彈跳屍體標記。
/// 由 PlayerController2D 在跳躍時偵測:若玩家所踩的地面 collider 帶有此元件,
/// 跳躍力 × jumpMultiplier (預設 2)。屍體本身仍是可踩的實體平台。
/// </summary>
public class CorpseSkill_Bounce : MonoBehaviour
{
    [Tooltip("跳躍力倍率")]
    public float jumpMultiplier = 2f;

    public void Configure(float multiplier)
    {
        jumpMultiplier = multiplier;
    }
}

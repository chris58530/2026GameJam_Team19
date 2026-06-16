using UnityEngine;

/// <summary>
/// Corpse skill type.
/// At the start of each life the player draws a card to pick one, and the corpse left behind during that life gains this skill.
/// </summary>
public enum CorpseSkillType
{
    /// <summary>Normal corpse, simply acts as a stand-able platform.</summary>
    Normal,

    /// <summary>Speed: when the collider touches this corpse, the player's "horizontal movement speed" is multiplied; it decays back to normal over time after leaving.</summary>
    Speed,

    /// <summary>Horizontal sway: the corpse moves back and forth horizontally, turning around early when it hits an obstacle.</summary>
    HorizontalSway,

    /// <summary>Vertical sway: the corpse moves back and forth vertically, turning around early when it hits an obstacle.</summary>
    VerticalSway,

    /// <summary>Teleport down: when the player passes this corpse, they are teleported onto the first platform directly below.</summary>
    TeleportDown,

    /// <summary>Bounce: jumping while standing on this corpse multiplies the jump force (affects only that one jump).</summary>
    Bounce,

    /// <summary>Random launch (Budweiser): the moment the player touches it, they are automatically launched upward at a random angle, using the player's original jump force.</summary>
    RandomLaunch,

    /// <summary>Blink out (Asahi): after the player touches it, the corpse disappears after a delay and reappears 1 second later.</summary>
    BlinkOut,

    /// <summary>Limited use (Ice): each stand/touch counts as one use; the corpse disappears once the uses run out (remaining uses are indicated by opacity).</summary>
    LimitedUse,
}

/// <summary>
/// Configuration of a single skill card (used for the level deck).
/// type = skill type; count = quantity (used as the weight for random draws; the higher the count, the more likely it is drawn).
/// Uses "infinite draw, no consumption": the deck never shrinks, count only affects the draw probability.
/// </summary>
[System.Serializable]
public class CorpseSkillCard
{
    [Tooltip("Skill type")]
    public CorpseSkillType type = CorpseSkillType.Normal;

    [Tooltip("Quantity (draw weight, the higher the more often it is drawn)")]
    [Min(1)]
    public int count = 1;
}

/// <summary>
/// Visual configuration of a skill on the "draw card" display.
/// type = skill type; bottleSprite = the corresponding bottle image (assigned by dragging, leave empty to show only a solid-color backplate).
/// Colors reuse the colorXxx fields on CorpseSkillSystem (ColorFor) and are not configured again here.
/// </summary>
[System.Serializable]
public class SkillCardVisual
{
    [Tooltip("Skill type")]
    public CorpseSkillType type = CorpseSkillType.Normal;

    [Tooltip("The bottle image shown on the card for this skill (leave empty to show only a solid-color backplate)")]
    public Sprite bottleSprite;
}

/// <summary>
/// Display-name helper for skill types (English, to ensure it displays correctly in Web builds).
/// </summary>
public static class CorpseSkillNames
{
    public static string ToDisplay(CorpseSkillType type)
    {
        switch (type)
        {
            case CorpseSkillType.Normal: return "Normal";
            case CorpseSkillType.Speed: return "Speed";
            case CorpseSkillType.Bounce: return "Bounce";
            case CorpseSkillType.HorizontalSway: return "Horizontal Sway";
            case CorpseSkillType.VerticalSway: return "Vertical Sway";
            case CorpseSkillType.TeleportDown: return "Teleport Down";
            case CorpseSkillType.RandomLaunch: return "Random Launch";
            case CorpseSkillType.BlinkOut: return "Blink Out";
            case CorpseSkillType.LimitedUse: return "Limited Use";
            default: return type.ToString();
        }
    }
}

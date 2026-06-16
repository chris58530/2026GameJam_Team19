using UnityEngine;

/// <summary>
/// Bounce corpse marker.
/// Detected by PlayerController2D when jumping: if the ground collider the player stands on has this component,
/// the jump force is multiplied by jumpMultiplier (default 2). The corpse is otherwise a solid platform the player can stand on.
/// </summary>
public class CorpseSkill_Bounce : MonoBehaviour
{
    [Tooltip("Jump force multiplier")]
    public float jumpMultiplier = 2f;

    public void Configure(float multiplier)
    {
        jumpMultiplier = multiplier;
    }
}

using UnityEngine;

/// <summary>
/// Level exit: shows a prompt message when the player enters the trigger zone.
/// Can later be extended in OnReached for level clear / reload / scene transition.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class LevelExit : MonoBehaviour
{
    [Tooltip("Tag used to identify the player")]
    public string playerTag = "Player";

    [Tooltip("Message shown when the exit is reached")]
    public string message = "Reached the exit!";

    private bool _reached;

    private void Reset()
    {
        // Make sure the Collider is a trigger
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_reached) return;
        if (!other.CompareTag(playerTag)) return;

        _reached = true;
        Debug.Log(message);
    }
}

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Reads LoopManager state and updates the on-screen HUD (uGUI Text).
/// </summary>
public class HUDController : MonoBehaviour
{
    public LoopManager manager;

    [Header("Persistent HUD")]
    public Text timeText;
    public Text loopText;
    public Text progressText;

    [Header("Clear Screen")]
    public GameObject clearPanel;
    public Text clearLoopsText;

    private static readonly Color White = Color.white;
    private static readonly Color Red = new Color(0.9f, 0.3f, 0.25f);
    private static readonly Color Yellow = new Color(1f, 0.85f, 0.2f);
    private static readonly Color Green = new Color(0.3f, 0.85f, 0.4f);

    private void Update()
    {
        if (manager == null) return;

        if (timeText != null)
        {
            float sec = Mathf.Max(0f, manager.TimeLeft);
            timeText.text = sec.ToString("F3") + " sec";
            timeText.color = manager.TimeLeft <= 3f ? Red : White;
        }

        if (loopText != null)
            loopText.text = "LOOP: " + manager.LoopCount;

        if (progressText != null)
        {
            int total = manager.buttons != null ? manager.buttons.Length : 3;
            progressText.text = "BUTTONS: " + manager.PressedCount + " / " + total;
            progressText.color = manager.DoorOpen ? Green : Yellow;
        }

        if (clearPanel != null && clearPanel.activeSelf != manager.Won)
            clearPanel.SetActive(manager.Won);

        if (manager.Won && clearLoopsText != null)
            clearLoopsText.text = "Loops Used: " + manager.LoopCount;
    }
}

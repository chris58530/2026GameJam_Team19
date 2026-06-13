using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 讀取 LoopManager 狀態並更新畫面 HUD (uGUI Text)。
/// </summary>
public class HUDController : MonoBehaviour
{
    public LoopManager manager;

    [Header("常駐 HUD")]
    public Text timeText;
    public Text loopText;
    public Text progressText;

    [Header("通關畫面")]
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
            int sec = Mathf.Max(0, Mathf.CeilToInt(manager.TimeLeft));
            timeText.text = "TIME: " + sec + " s";
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

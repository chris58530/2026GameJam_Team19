using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 「Over My Dead Body」按鈕輪迴關卡管理器。
/// - 每輪 loopTime 秒;時間到或按 K → 在原地留下殘影、玩家回起點、輪數+1、計時重置。
/// - 殘影是實體平台 (可踩),也能壓住按鈕。
/// - A/B/C 三按鈕同時被壓下 → 門開;門開時玩家走到門口即通關。
/// - R 從第 1 輪重來。
/// </summary>
public class LoopManager : MonoBehaviour
{
    [Header("場景參考")]
    public Transform spawnPoint;
    public PressButton[] buttons;

    [Header("殘影設定")]
    public Sprite ghostSprite;
    public Color ghostColor = new Color(0.85f, 0.3f, 0.3f, 0.7f);
    public Vector2 ghostScale = Vector2.one;
    [Tooltip("殘影碰撞體大小 (世界單位)。與玩家碰撞體一致,不受殘影圖片大小影響。")]
    public Vector2 ghostColliderSize = Vector2.one;
    [Tooltip("殘影是否上下顛倒 (倒立)。玩家圖片變成殘影時翻轉。")]
    public bool ghostFlipY = true;
    [Tooltip("殘影圖層 (應為 Ground,才能當平台被踩)")]
    public int ghostLayer;
    public int ghostSortingOrder = 5;

    [Header("輪迴設定")]
    public float loopTime = 10f;

    public float TimeLeft { get; private set; }
    public int LoopCount { get; private set; } = 1;
    public bool Won { get; private set; }
    public int PressedCount { get; private set; }
    public bool DoorOpen { get; private set; }

    private GameObject _player;
    private Rigidbody2D _rb;
    private PlayerController2D _pc;
    private readonly List<GameObject> _ghosts = new List<GameObject>();

    private void Start()
    {
        _player = GameObject.FindGameObjectWithTag("Player");
        if (_player != null)
        {
            _rb = _player.GetComponent<Rigidbody2D>();
            _pc = _player.GetComponent<PlayerController2D>();
        }
        if (ghostLayer == 0)
        {
            int g = LayerMask.NameToLayer("Ground");
            if (g >= 0) ghostLayer = g;
        }
        TimeLeft = loopTime;
    }

    private void Update()
    {
        var kb = Keyboard.current;

        if (kb != null && kb.rKey.wasPressedThisFrame)
        {
            ResetLevel();
            return;
        }

        // 按鈕狀態 / 門
        PressedCount = 0;
        if (buttons != null)
            foreach (var b in buttons)
                if (b != null && b.IsPressed) PressedCount++;
        DoorOpen = buttons != null && buttons.Length > 0 && PressedCount == buttons.Length;

        if (Won) return;

        TimeLeft -= Time.deltaTime;
        if (TimeLeft <= 0f)
            LeaveGhostAndRespawn();

        if (kb != null && kb.kKey.wasPressedThisFrame)
            LeaveGhostAndRespawn();
    }

    private void LeaveGhostAndRespawn()
    {
        if (_player == null || spawnPoint == null) return;

        SpawnGhost(_player.transform.position);
        _player.transform.position = spawnPoint.position;
        if (_rb != null) _rb.linearVelocity = Vector2.zero;

        TimeLeft = loopTime;
        LoopCount++;
    }

    private void SpawnGhost(Vector3 pos)
    {
        var go = new GameObject("Ghost_" + (_ghosts.Count + 1));
        go.transform.position = pos;
        go.transform.localScale = new Vector3(ghostScale.x, ghostScale.y, 1f);
        if (ghostLayer >= 0) go.layer = ghostLayer;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = ghostSprite;
        sr.color = ghostColor;
        sr.sortingOrder = ghostSortingOrder;
        sr.flipY = ghostFlipY;   // 殘影上下顛倒 (玩家圖片倒立)

        var col = go.AddComponent<BoxCollider2D>();
        // 維持殘影碰撞體為固定世界大小 (與玩家一致),抵銷縮放與大圖的影響
        float sx = Mathf.Approximately(ghostScale.x, 0f) ? 1f : Mathf.Abs(ghostScale.x);
        float sy = Mathf.Approximately(ghostScale.y, 0f) ? 1f : Mathf.Abs(ghostScale.y);
        col.size = new Vector2(ghostColliderSize.x / sx, ghostColliderSize.y / sy);

        go.AddComponent<Ghost>();
        _ghosts.Add(go);
    }

    /// <summary>由門呼叫:門開時玩家進門即通關。</summary>
    public void TryExit()
    {
        if (!Won && DoorOpen)
        {
            Won = true;
            if (_rb != null) _rb.linearVelocity = Vector2.zero;
            if (_pc != null) _pc.enabled = false;
        }
    }

    public void ResetLevel()
    {
        foreach (var g in _ghosts)
            if (g != null) Destroy(g);
        _ghosts.Clear();

        LoopCount = 1;
        TimeLeft = loopTime;
        Won = false;
        DoorOpen = false;
        PressedCount = 0;

        if (_pc != null) _pc.enabled = true;
        if (_player != null && spawnPoint != null)
        {
            _player.transform.position = spawnPoint.position;
            if (_rb != null) _rb.linearVelocity = Vector2.zero;
        }
    }
}

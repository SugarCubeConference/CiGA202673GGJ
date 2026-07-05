using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;


[RequireComponent(typeof(Collider2D))]
public sealed class LinkedBridge : MonoBehaviour
{
    [SerializeField] private string bridgeId;
    [SerializeField] private string defaultState = "solid";
    [SerializeField] private string activeState = "solid";

    // Tilemap 瓦片替换（可选）
    [Header("Tilemap Tile Swap (optional)")]
    [SerializeField] private Tilemap tilemap;
    [SerializeField] private TileBase defaultTile;
    [SerializeField] private TileBase activeTile;
    [SerializeField] private Vector3Int[] tilePositions;

    private Collider2D bridgeCollider;
    private SpriteRenderer spriteRenderer;
    private readonly HashSet<ButtonSwitch> _pressingButtons = new HashSet<ButtonSwitch>();

    public string BridgeId => bridgeId;

    private void Awake()
    {
        bridgeCollider = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        ApplyState(defaultState);
    }

    public void Configure(string id, string defaultState, string activeState)
    {
        bridgeId = id;
        this.defaultState = string.IsNullOrEmpty(defaultState) ? "solid" : defaultState;
        this.activeState = string.IsNullOrEmpty(activeState) ? "solid" : activeState;
        if (bridgeCollider != null)
        {
            ApplyState(this.defaultState);
        }
    }

    /// <summary>通知桥：某个按钮开始按压</summary>
    public void NotifyPressed(ButtonSwitch button)
    {
        _pressingButtons.Add(button);
        ApplyState(activeState);
    }

    /// <summary>通知桥：某个按钮释放</summary>
    public void NotifyReleased(ButtonSwitch button)
    {
        _pressingButtons.Remove(button);
        ApplyState(_pressingButtons.Count > 0 ? activeState : defaultState);
    }

    /// <summary>直接设置激活状态（兼容旧调用）</summary>
    public void SetActivated(bool activated)
    {
        ApplyState(activated ? activeState : defaultState);
    }

    private void ApplyState(string state)
    {
        bool solid = state == "solid";

        // 原有逻辑：Collider + SpriteRenderer
        if (bridgeCollider != null)
        {
            bridgeCollider.enabled = solid;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = solid
                ? new Color(0.35f, 0.83f, 0.95f, 1f)
                : new Color(0.35f, 0.83f, 0.95f, 0.25f);
        }

        // 新增逻辑：Tilemap 瓦片替换
        SwapTiles(solid);
    }

    private void SwapTiles(bool useActiveTile)
    {
        if (tilemap == null || tilePositions == null || tilePositions.Length == 0)
            return;

        TileBase targetTile = useActiveTile ? activeTile : defaultTile;
        if (targetTile == null)
            return;

        for (int i = 0; i < tilePositions.Length; i++)
        {
            tilemap.SetTile(tilePositions[i], targetTile);
        }
    }
}

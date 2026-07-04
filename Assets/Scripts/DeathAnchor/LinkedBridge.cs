using UnityEngine;

/// <summary>
/// 按钮控制的桥梁——受 ButtonSwitch 驱动，在 solid（实心）/空心 状态间切换。
/// 实心状态：碰撞体启用 + 不透明；空心状态：碰撞体禁用 + 半透明。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public sealed class LinkedBridge : MonoBehaviour
{
    [SerializeField] private string bridgeId;
    [SerializeField] private string defaultState = "solid";
    [SerializeField] private string activeState = "solid";

    private Collider2D bridgeCollider;
    private SpriteRenderer spriteRenderer;

    /// <summary>桥梁 ID</summary>
    public string BridgeId => bridgeId;

    private void Awake()
    {
        bridgeCollider = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        ApplyState(defaultState);
    }

    /// <summary>由 Baker 调用，配置桥梁参数</summary>
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

    /// <summary>按钮调用：激活/取消激活桥梁</summary>
    public void SetActivated(bool activated)
    {
        ApplyState(activated ? activeState : defaultState);
    }

    /// <summary>根据状态字符串 ("solid" / 其他) 切换碰撞与透明度</summary>
    private void ApplyState(string state)
    {
        bool solid = state == "solid";
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
    }
}
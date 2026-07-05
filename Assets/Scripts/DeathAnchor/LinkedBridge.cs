using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class LinkedBridge : MonoBehaviour
{
    [SerializeField] private string bridgeId;
    [SerializeField] private string defaultState = "solid";
    [SerializeField] private string activeState = "solid";
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite pressedSprite;

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
        if (bridgeCollider != null)
        {
            bridgeCollider.enabled = solid;
        }

        if (spriteRenderer != null)
        {
            if (normalSprite != null && pressedSprite != null)
            {
                spriteRenderer.sprite = solid ? normalSprite : pressedSprite;
            }
            else
            {
                spriteRenderer.color = solid
                    ? new Color(0.35f, 0.83f, 0.95f, 1f)
                    : new Color(0.35f, 0.83f, 0.95f, 0.25f);
            }
        }
    }
}

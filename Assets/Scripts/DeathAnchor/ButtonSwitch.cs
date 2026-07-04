using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 按钮开关——由玩家或分身踩踏触发，控制关联的桥梁状态。
/// 支持三种触发模式："player"（仅玩家）、"ghost"（仅分身）、"both"（两者均可）。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public sealed class ButtonSwitch : MonoBehaviour
{
    [SerializeField] private string buttonId;
    [SerializeField] private string pressedBy = "both";
    [SerializeField] private LinkedBridge[] linkedBridges;

    /// <summary>当前正在按压此按钮的角色集合</summary>
    private readonly HashSet<ActorIdentity> pressingActors = new HashSet<ActorIdentity>();
    private SpriteRenderer spriteRenderer;

    /// <summary>按钮 ID</summary>
    public string ButtonId => buttonId;
    /// <summary>是否被按压（有角色在触发区内）</summary>
    public bool IsPressed => pressingActors.Count > 0;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        bool pressed = IsPressed;
        // 按压时亮色，未按压时半透明
        if (spriteRenderer != null)
        {
            spriteRenderer.color = pressed ? new Color(1f, 0.78f, 0.18f, 1f) : new Color(1f, 0.78f, 0.18f, 0.45f);
        }

        // 同步所有关联桥梁的状态
        if (linkedBridges == null) return;

        for (int i = 0; i < linkedBridges.Length; i++)
        {
            if (linkedBridges[i] != null)
            {
                linkedBridges[i].SetActivated(pressed);
            }
        }
    }

    /// <summary>由 Baker 调用，配置按钮参数</summary>
    public void Configure(string id, string pressedBy, LinkedBridge[] linkedBridges)
    {
        buttonId = id;
        this.pressedBy = string.IsNullOrEmpty(pressedBy) ? "both" : pressedBy;
        this.linkedBridges = linkedBridges;
    }

    /// <summary>角色进入触发区</summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        ActorIdentity actor = other.GetComponent<ActorIdentity>();
        if (CanBePressedBy(actor))
        {
            pressingActors.Add(actor);
        }
    }

    /// <summary>角色离开触发区</summary>
    private void OnTriggerExit2D(Collider2D other)
    {
        ActorIdentity actor = other.GetComponent<ActorIdentity>();
        if (actor != null)
        {
            pressingActors.Remove(actor);
        }
    }

    /// <summary>判断角色是否有权按压此按钮</summary>
    private bool CanBePressedBy(ActorIdentity actor)
    {
        if (actor == null) return false;

        return pressedBy == "both"
            || (pressedBy == "player" && actor.IsPlayer)
            || (pressedBy == "ghost" && actor.IsGhost);
    }
}
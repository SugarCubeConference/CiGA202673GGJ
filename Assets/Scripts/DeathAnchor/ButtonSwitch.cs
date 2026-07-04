using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class ButtonSwitch : MonoBehaviour
{
    [SerializeField] private string buttonId;
    [SerializeField] private string pressedBy = "both";
    [SerializeField] private LinkedBridge[] linkedBridges;

    private readonly HashSet<ActorIdentity> pressingActors = new HashSet<ActorIdentity>();
    private SpriteRenderer spriteRenderer;

    public string ButtonId => buttonId;
    public bool IsPressed => pressingActors.Count > 0;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        bool pressed = IsPressed;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = pressed ? new Color(1f, 0.78f, 0.18f, 1f) : new Color(1f, 0.78f, 0.18f, 0.45f);
        }

        if (linkedBridges == null)
        {
            return;
        }

        for (int i = 0; i < linkedBridges.Length; i++)
        {
            if (linkedBridges[i] != null)
            {
                linkedBridges[i].SetActivated(pressed);
            }
        }
    }

    public void Configure(string id, string pressedBy, LinkedBridge[] linkedBridges)
    {
        buttonId = id;
        this.pressedBy = string.IsNullOrEmpty(pressedBy) ? "both" : pressedBy;
        this.linkedBridges = linkedBridges;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        ActorIdentity actor = other.GetComponent<ActorIdentity>();
        if (CanBePressedBy(actor))
        {
            pressingActors.Add(actor);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        ActorIdentity actor = other.GetComponent<ActorIdentity>();
        if (actor != null)
        {
            pressingActors.Remove(actor);
        }
    }

    private bool CanBePressedBy(ActorIdentity actor)
    {
        if (actor == null)
        {
            return false;
        }

        return pressedBy == "both"
            || (pressedBy == "player" && actor.IsPlayer)
            || (pressedBy == "ghost" && actor.IsGhost);
    }
}

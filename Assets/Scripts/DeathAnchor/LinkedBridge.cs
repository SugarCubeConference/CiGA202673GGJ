using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class LinkedBridge : MonoBehaviour
{
    [SerializeField] private string bridgeId;
    [SerializeField] private string defaultState = "solid";
    [SerializeField] private string activeState = "solid";

    private Collider2D bridgeCollider;
    private SpriteRenderer spriteRenderer;

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
            spriteRenderer.color = solid
                ? new Color(0.35f, 0.83f, 0.95f, 1f)
                : new Color(0.35f, 0.83f, 0.95f, 0.25f);
        }
    }
}

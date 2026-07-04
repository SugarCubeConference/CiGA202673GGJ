using UnityEngine;

/// <summary>
/// 钥匙门——玩家需要携带指定 ID 的钥匙才能通过。
/// 开门后禁用所有碰撞体并变为半透明。
/// </summary>
public sealed class KeyDoor : MonoBehaviour
{
    [SerializeField] private string requiredKey;

    private DeathAnchorGameManager gameManager;
    private Collider2D[] colliders;
    private SpriteRenderer spriteRenderer;
    private bool opened;

    private void Awake()
    {
        gameManager = FindObjectOfType<DeathAnchorGameManager>();
        colliders = GetComponents<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    /// <summary>由 Baker 调用，设置所需钥匙 ID</summary>
    public void Configure(string requiredKey)
    {
        this.requiredKey = requiredKey;
    }

    /// <summary>玩家触碰触发器时检查钥匙并开门</summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (opened) return;

        ActorIdentity actor = other.GetComponent<ActorIdentity>();
        if (actor == null || !actor.IsPlayer || gameManager == null || !gameManager.PlayerHasKey(requiredKey))
            return;

        Open();
    }

    /// <summary>开门：禁用碰撞 + 半透明</summary>
    private void Open()
    {
        opened = true;
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(1f, 0.45f, 0.25f, 0.2f);
        }
    }
}
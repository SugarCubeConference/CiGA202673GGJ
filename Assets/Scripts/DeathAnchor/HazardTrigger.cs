using UnityEngine;

/// <summary>
/// 陷阱触发器——玩家触碰时通知 GameManager 杀死玩家（触发幽灵锚点逻辑）。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public sealed class HazardTrigger : MonoBehaviour
{
    [SerializeField] private DeathAnchorGameManager gameManager;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
        if (gameManager == null)
        {
            gameManager = FindObjectOfType<DeathAnchorGameManager>();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        ActorIdentity actor = other.GetComponent<ActorIdentity>();
        if (actor != null && actor.IsPlayer && gameManager != null)
        {
            gameManager.KillPlayer();
        }
    }
}
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class DeathAnchorGoalTrigger : MonoBehaviour
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
            gameManager.OnGoalReached();
        }
    }
}

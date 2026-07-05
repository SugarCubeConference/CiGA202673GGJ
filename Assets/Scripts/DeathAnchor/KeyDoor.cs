using UnityEngine;

public sealed class KeyDoor : MonoBehaviour
{
    [SerializeField] private string requiredKey;

    private DeathAnchorGameManager gameManager;
    private Collider2D[] colliders;
    private SpriteRenderer spriteRenderer;
    private bool opened;
    private Animator animator;

    private void Awake()
    {
        gameManager = FindObjectOfType<DeathAnchorGameManager>();
        colliders = GetComponents<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
    }

    public void Configure(string requiredKey)
    {
        this.requiredKey = requiredKey;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (opened)
        {
            return;
        }

        ActorIdentity actor = other.GetComponent<ActorIdentity>();
        if (actor == null || !actor.IsPlayer || gameManager == null || !gameManager.PlayerHasKey(requiredKey))
        {
            return;
        }

        Open();
    }

    private void Open()
    {
        opened = true;
        if (animator != null)
        {
            animator.SetBool("IsOpen", true);
        }
        DeathAnchorWwiseAudio.Post(gameObject, DeathAnchorWwiseEvents.DoorUnlock);

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

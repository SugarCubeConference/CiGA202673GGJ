using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class KeyPickup : MonoBehaviour
{
    [SerializeField] private string keyId;
    [SerializeField] private Vector3 carryOffset = new Vector3(0.35f, 0.35f, 0f);

    private DeathAnchorGameManager gameManager;
    private Transform carrier;
    private Vector3 homePosition;
    private Collider2D keyCollider;

    public string Id => keyId;
    public bool IsCarried => carrier != null;

    private void Awake()
    {
        keyCollider = GetComponent<Collider2D>();
        keyCollider.isTrigger = true;
        homePosition = transform.position;
        gameManager = FindObjectOfType<DeathAnchorGameManager>();
        if (gameManager != null)
        {
            gameManager.RegisterKey(this);
        }
    }

    private void LateUpdate()
    {
        if (carrier != null)
        {
            transform.position = carrier.position + carryOffset;
        }
    }

    public void Configure(string id)
    {
        keyId = id;
    }

    public void ResetToHome()
    {
        carrier = null;
        transform.position = homePosition;
        gameObject.SetActive(true);
    }

    public void Consume()
    {
        carrier = null;
        gameObject.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        ActorIdentity actor = other.GetComponent<ActorIdentity>();
        if (actor != null && actor.IsPlayer)
        {
            carrier = other.transform;
        }
    }
}

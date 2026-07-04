using UnityEngine;

/// <summary>
/// 钥匙拾取物——玩家触碰后自动吸附在身上。
/// GameManager 在玩家死亡时通过 ResetToHome 将钥匙放回原位。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public sealed class KeyPickup : MonoBehaviour
{
    [SerializeField] private string keyId;
    [SerializeField] private Vector3 carryOffset = new Vector3(0.35f, 0.35f, 0f);

    private DeathAnchorGameManager gameManager;
    private Transform carrier;
    private Vector3 homePosition;
    private Collider2D keyCollider;

    /// <summary>钥匙 ID</summary>
    public string Id => keyId;
    /// <summary>是否正被玩家携带</summary>
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

    /// <summary>LateUpdate：如果被携带，跟随载体位置</summary>
    private void LateUpdate()
    {
        if (carrier != null)
        {
            transform.position = carrier.position + carryOffset;
        }
    }

    /// <summary>由 Baker 调用，设置钥匙 ID</summary>
    public void Configure(string id)
    {
        keyId = id;
    }

    /// <summary>玩家死亡时 GameManager 调用，钥匙回到初始位置</summary>
    public void ResetToHome()
    {
        carrier = null;
        transform.position = homePosition;
        gameObject.SetActive(true);
    }

    /// <summary>消耗钥匙（暂未使用）</summary>
    public void Consume()
    {
        carrier = null;
        gameObject.SetActive(false);
    }

    /// <summary>玩家触碰时吸附</summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        ActorIdentity actor = other.GetComponent<ActorIdentity>();
        if (actor != null && actor.IsPlayer)
        {
            carrier = other.transform;
        }
    }
}
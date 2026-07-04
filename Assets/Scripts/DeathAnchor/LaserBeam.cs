using UnityEngine;

/// <summary>
/// Laser beam that emits a raycast in a fixed direction.
/// Kills the player on contact. Can be blocked by terrain, bridges, and moving platforms.
/// Attach to a child object positioned at the laser origin; set emissionDirection in Inspector.
/// </summary>
public sealed class LaserBeam : MonoBehaviour
{
    [Header("Beam Settings")]
    [SerializeField] private Vector2 emissionDirection = Vector2.right;
    [SerializeField] private float maxDistance = 20f;
    [SerializeField] private LayerMask blockMask;
    [SerializeField] private Collider2D ignoredBlocker;

    [Header("Damage")]
    [SerializeField] private float damageCooldown = 0.5f;

    [Header("Visual")]
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private Color beamColor = new Color(1f, 0.15f, 0.15f, 0.9f);
    [SerializeField] private float beamWidth = 0.08f;

    private float lastDamageTime = -999f;
    private Vector2 hitPoint;
    private bool isBlocked;
    private DeathAnchorGameManager gameManager;

    public Vector2 HitPoint => hitPoint;
    public bool IsBlocked => isBlocked;
    public float CurrentLength => hitPoint != Vector2.zero
        ? Vector2.Distance(transform.position, hitPoint)
        : maxDistance;

    private void Awake()
    {
        emissionDirection = emissionDirection.normalized;
        gameManager = FindObjectOfType<DeathAnchorGameManager>();

        if (lineRenderer == null)
        {
            lineRenderer = GetComponent<LineRenderer>();
        }

        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 2;
            lineRenderer.startWidth = beamWidth;
            lineRenderer.endWidth = beamWidth;
            lineRenderer.startColor = beamColor;
            lineRenderer.endColor = beamColor;
            lineRenderer.useWorldSpace = true;
        }
    }

    private void FixedUpdate()
    {
        Vector2 origin = (Vector2)transform.position;
        hitPoint = origin + emissionDirection * maxDistance;
        isBlocked = false;

        RaycastHit2D hit = FindFirstBlocker(origin);
        if (hit.collider != null)
        {
            hitPoint = hit.point;
            isBlocked = true;
        }

        TryDamagePlayerOnBeam(origin, Vector2.Distance(origin, hitPoint));
    }

    private void Update()
    {
        // Update visual
        if (lineRenderer != null)
        {
            Vector3 start = transform.position;
            Vector3 end = new Vector3(hitPoint.x, hitPoint.y, transform.position.z);
            lineRenderer.SetPosition(0, start);
            lineRenderer.SetPosition(1, end);

            // Dim when blocked at source
            float alpha = isBlocked ? 0.5f : 1f;
            lineRenderer.startColor = new Color(beamColor.r, beamColor.g, beamColor.b, alpha);
            lineRenderer.endColor = new Color(beamColor.r, beamColor.g, beamColor.b, alpha);
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (Time.time - lastDamageTime < damageCooldown)
        {
            return;
        }

        ActorIdentity actor = other.GetComponent<ActorIdentity>();
        if (actor != null && actor.IsPlayer)
        {
            DeathAnchorGameManager gm = FindObjectOfType<DeathAnchorGameManager>();
            if (gm != null)
            {
                gm.KillPlayer();
                lastDamageTime = Time.time;
            }
        }
    }

    /// <summary>
    /// Call this to change beam direction at runtime (e.g., rotating laser).
    /// </summary>
    public void SetDirection(Vector2 newDirection)
    {
        emissionDirection = newDirection.normalized;
    }

    public void Configure(Vector2 direction, float maxDistance, LayerMask blockMask, Color beamColor)
    {
        Configure(direction, maxDistance, blockMask, beamColor, null);
    }

    public void Configure(Vector2 direction, float maxDistance, LayerMask blockMask, Color beamColor, Collider2D ignoredBlocker)
    {
        emissionDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
        this.maxDistance = Mathf.Max(0.05f, maxDistance);
        this.blockMask = blockMask;
        this.beamColor = beamColor;
        this.ignoredBlocker = ignoredBlocker;

        if (lineRenderer == null)
        {
            lineRenderer = GetComponent<LineRenderer>();
        }

        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 2;
            lineRenderer.startWidth = beamWidth;
            lineRenderer.endWidth = beamWidth;
            lineRenderer.startColor = beamColor;
            lineRenderer.endColor = beamColor;
            lineRenderer.useWorldSpace = true;
        }
    }

    private RaycastHit2D FindFirstBlocker(Vector2 origin)
    {
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, emissionDirection, maxDistance, blockMask);
        RaycastHit2D best = default;
        float bestDistance = float.MaxValue;
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D candidate = hits[i].collider;
            if (candidate == null || candidate.isTrigger || candidate == ignoredBlocker)
            {
                continue;
            }

            if (hits[i].distance < bestDistance)
            {
                best = hits[i];
                bestDistance = hits[i].distance;
            }
        }

        return best;
    }

    private void TryDamagePlayerOnBeam(Vector2 origin, float beamLength)
    {
        if (Time.time - lastDamageTime < damageCooldown)
        {
            return;
        }

        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, emissionDirection, beamLength);
        for (int i = 0; i < hits.Length; i++)
        {
            ActorIdentity actor = hits[i].collider != null ? hits[i].collider.GetComponent<ActorIdentity>() : null;
            if (actor == null || !actor.IsPlayer)
            {
                continue;
            }

            if (gameManager == null)
            {
                gameManager = FindObjectOfType<DeathAnchorGameManager>();
            }

            if (gameManager != null)
            {
                gameManager.KillPlayer();
                lastDamageTime = Time.time;
            }

            return;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 origin = transform.position;
        Vector3 end = origin + (Vector3)(emissionDirection * maxDistance);
        Gizmos.DrawLine(origin, end);

        if (isBlocked)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(new Vector3(hitPoint.x, hitPoint.y, origin.z), 0.15f);
        }
    }
}

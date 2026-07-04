using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(ActorIdentity))]
public sealed class DeathAnchorPlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2.85f;
    [SerializeField] private float jumpSpeed = 6.7f;
    [SerializeField] private float gravity = 15f;
    [SerializeField] private float fallGravityMultiplier = 1.22f;
    [SerializeField] private float maxFallSpeed = 7f;
    [SerializeField] private float coyoteTime = 0.095f;
    [SerializeField] private float jumpBufferTime = 0.13f;
    [SerializeField] private float jumpCutMultiplier = 0.52f;

    [Header("Wall Slide")]
    [SerializeField] private bool wallSlideEnabled = true;
    [SerializeField] private float wallSlideMaxSpeed = 1.25f;

    [Header("Collision")]
    [SerializeField] private LayerMask solidMask;
    [SerializeField] private float skinWidth = 0.02f;

    private readonly RaycastHit2D[] castHits = new RaycastHit2D[8];

    private Rigidbody2D rb;
    private BoxCollider2D box;
    private ContactFilter2D solidFilter;
    private float verticalSpeed;
    private float lastGroundedAt = -999f;
    private float lastJumpPressedAt = -999f;
    private int facing = 1;
    private bool grounded;
    private Collider2D groundCollider;
    private float nextDebugTime;

    public Vector2 FootPosition
    {
        get
        {
            EnsureCachedComponents();
            return rb.position + Vector2.down * (box.size.y * 0.5f - box.offset.y);
        }
    }
    public int Facing => facing;
    public bool Grounded => grounded;

    private void Start()
    {
        // Fallback: existing baked scenes may have solidMask = 0 (pre-merge scenes).
        // Auto-detect Ground layer and rebuild filter so the player collides with the world.
        if (solidMask == 0)
        {
            solidMask = LayerMask.GetMask("Ground");
            solidFilter = new ContactFilter2D
            {
                useLayerMask = true,
                useTriggers = false,
                layerMask = solidMask
            };
        }
    }

    private void Awake()
    {
        EnsureCachedComponents();

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.freezeRotation = true;
        rb.useFullKinematicContacts = true;

        GetComponent<ActorIdentity>().SetKind(DeathAnchorActorKind.Player);

        solidFilter = new ContactFilter2D
        {
            useLayerMask = true,
            useTriggers = false,
            layerMask = LayerMask.GetMask("Ground", "Ghost")
        };

        Debug.Log($"[PlayerController] solidFilter.layerMask={solidFilter.layerMask.value}, box.size={box.size}, rb.position={rb.position}");
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            lastJumpPressedAt = Time.time;
        }

        if ((Input.GetKeyUp(KeyCode.W) || Input.GetKeyUp(KeyCode.Space) || Input.GetKeyUp(KeyCode.UpArrow)) && verticalSpeed > 0f)
        {
            verticalSpeed *= jumpCutMultiplier;
        }
    }

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        float horizontalInput = GetHorizontalInput();

        if (Mathf.Abs(horizontalInput) > 0.01f)
        {
            facing = horizontalInput > 0f ? 1 : -1;
        }

        ProbeGround();
        ApplyCarrierDelta();

        bool jumpBuffered = Time.time - lastJumpPressedAt <= jumpBufferTime;
        bool canJump = Time.time - lastGroundedAt <= coyoteTime;
        if (jumpBuffered && canJump)
        {
            verticalSpeed = jumpSpeed;
            lastJumpPressedAt = -999f;
            lastGroundedAt = -999f;
            grounded = false;
            groundCollider = null;
        }

        bool wallSliding = false;
        if (!grounded && wallSlideEnabled && verticalSpeed < 0f && Mathf.Abs(horizontalInput) > 0.01f && IsTouchingWall(Mathf.Sign(horizontalInput)))
        {
            wallSliding = true;
            verticalSpeed = Mathf.Max(verticalSpeed, -wallSlideMaxSpeed);
        }

        if (!wallSliding)
        {
            float gravityThisFrame = verticalSpeed < 0f ? gravity * fallGravityMultiplier : gravity;
            verticalSpeed = Mathf.Max(verticalSpeed - gravityThisFrame * dt, -maxFallSpeed);
        }

        Move(Vector2.right, horizontalInput * moveSpeed * dt);
        Move(Vector2.up, verticalSpeed * dt);

        if (Time.time >= nextDebugTime)
        {
            nextDebugTime = Time.time + 1f;
            Debug.Log($"[PlayerController] input={horizontalInput:F2} vSpeed={verticalSpeed:F2} grounded={grounded} pos=({rb.position.x:F3},{rb.position.y:F3}) box.size=({box.size.x},{box.size.y})");
        }
    }

    public void Configure(float playerWidthUnits, float playerHeightUnits, LayerMask solidMask, bool wallSlideEnabled, float wallSlideMaxSpeedUnits)
    {
        EnsureCachedComponents();

        box.size = new Vector2(playerWidthUnits, playerHeightUnits);
        box.offset = Vector2.zero;
        this.solidMask = solidMask;
        this.wallSlideEnabled = wallSlideEnabled;
        wallSlideMaxSpeed = wallSlideMaxSpeedUnits;
        solidFilter.useLayerMask = true;
        solidFilter.useTriggers = false;
        solidFilter.layerMask = solidMask;
    }

    public void SpawnAtFootPosition(Vector2 footPosition)
    {
        EnsureCachedComponents();

        Vector2 bodyPosition = footPosition + Vector2.up * (box.size.y * 0.5f - box.offset.y);
        transform.position = bodyPosition;
        rb.position = bodyPosition;
        verticalSpeed = 0f;
        lastJumpPressedAt = -999f;
    }

    private void EnsureCachedComponents()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        if (box == null)
        {
            box = GetComponent<BoxCollider2D>();
        }
    }

    private float GetHorizontalInput()
    {
        float input = 0f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            input -= 1f;
        }

        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            input += 1f;
        }

        return Mathf.Clamp(input, -1f, 1f);
    }

    private void ProbeGround()
    {
        int count = box.Cast(Vector2.down, solidFilter, castHits, skinWidth + 0.04f);
        grounded = false;
        groundCollider = null;

        for (int i = 0; i < count; i++)
        {
            if (castHits[i].collider == null || castHits[i].collider.isTrigger)
            {
                continue;
            }

            if (castHits[i].normal.y > 0.45f)
            {
                grounded = true;
                groundCollider = castHits[i].collider;
                lastGroundedAt = Time.time;
                if (verticalSpeed < 0f)
                {
                    verticalSpeed = 0f;
                }
                return;
            }
        }
    }

    private void ApplyCarrierDelta()
    {
        if (!grounded || groundCollider == null)
        {
            return;
        }

        MovingPlatform2D movingPlatform = groundCollider.GetComponent<MovingPlatform2D>();
        if (movingPlatform != null)
        {
            rb.position += movingPlatform.LastDelta;
            return;
        }

        GhostReplayController ghost = groundCollider.GetComponent<GhostReplayController>();
        if (ghost != null)
        {
            rb.position += ghost.LastDelta;
        }
    }

    private bool IsTouchingWall(float direction)
    {
        int count = box.Cast(Vector2.right * Mathf.Sign(direction), solidFilter, castHits, skinWidth + 0.03f);
        for (int i = 0; i < count; i++)
        {
            if (castHits[i].collider != null
                && !castHits[i].collider.isTrigger
                && !IsGhostCollider(castHits[i].collider)
                && Mathf.Abs(castHits[i].normal.x) > 0.45f)
            {
                return true;
            }
        }

        return false;
    }

    private void Move(Vector2 direction, float distance)
    {
        if (Mathf.Abs(distance) <= 0.0001f)
        {
            return;
        }

        float sign = Mathf.Sign(distance);
        float magnitude = Mathf.Abs(distance);
        Vector2 castDirection = direction * sign;
        int count = box.Cast(castDirection, solidFilter, castHits, magnitude + skinWidth);
        float allowedDistance = magnitude;
        bool isHorizontal = Mathf.Abs(direction.x) > 0.5f;

        for (int i = 0; i < count; i++)
        {
            if (castHits[i].collider == null || castHits[i].collider.isTrigger)
            {
                continue;
            }

            if (IsGhostCollider(castHits[i].collider) && !ShouldCollideWithGhost(castDirection, castHits[i]))
            {
                continue;
            }

            if (IsInitialOverlap(castHits[i]))
            {
                continue;
            }

            // Skip ground (vertical normals) when moving horizontally,
            // and skip walls (horizontal normals) when moving vertically
            if (isHorizontal && Mathf.Abs(castHits[i].normal.y) > 0.7f) continue;
            if (!isHorizontal && Mathf.Abs(castHits[i].normal.x) > 0.7f) continue;

            allowedDistance = Mathf.Min(allowedDistance, Mathf.Max(0f, castHits[i].distance - skinWidth));
        }

        rb.position += castDirection * allowedDistance;
        if (allowedDistance < magnitude && direction.y != 0f)
        {
            verticalSpeed = 0f;
        }
    }

    private bool IsGhostCollider(Collider2D candidate)
    {
        if (candidate == null)
        {
            return false;
        }

        return candidate.GetComponent<GhostReplayController>() != null
            || candidate.GetComponentInParent<GhostReplayController>() != null;
    }

    private bool IsInitialOverlap(RaycastHit2D hit)
    {
        if (hit.collider == null) return false;
        ColliderDistance2D d = Physics2D.Distance(box, hit.collider);
        return d.isOverlapped;
    }
}

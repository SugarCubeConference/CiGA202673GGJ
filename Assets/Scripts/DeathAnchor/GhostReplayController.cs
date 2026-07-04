using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(ActorIdentity))]
public sealed class GhostReplayController : MonoBehaviour
{
    [Header("Physics")]
    [SerializeField] private float moveSpeed = 2.85f;
    [SerializeField] private float jumpSpeed = 6.7f;
    [SerializeField] private float gravity = 20.5f;
    [SerializeField] private float fallGravityMultiplier = 1.22f;
    [SerializeField] private float maxFallSpeed = 9.2f;

<<<<<<< Updated upstream
    [Header("Reverse Gravity（1 = 正常方向，-1 = 反转）")]
=======
    [Header("Reverse Gravity (1 = normal, -1 = reverse)")]
>>>>>>> Stashed changes
    [SerializeField] private float gravityDirection = 1f;

    [Header("Collision")]
    [SerializeField] private LayerMask solidMask;
    [SerializeField] private float skinWidth = 0.02f;

    [Header("Interaction")]
    [SerializeField] private float playerHeight = 0.42f;
    [SerializeField] private LayerMask playerMask;

    private readonly List<DeathAnchorReplayFrame> frames = new List<DeathAnchorReplayFrame>();
    private readonly RaycastHit2D[] castHits = new RaycastHit2D[8];

    private Rigidbody2D rb;
    private BoxCollider2D box;
    private ContactFilter2D solidFilter;
    private Vector2 anchorFootPosition;
    private float startedAt;
    private float duration;
    private float previousLocalTime;
    private bool hasRecord;
    private float verticalSpeed;
    private bool grounded;
    private Collider2D groundCollider;
    private Vector2 previousPosition;

    public Vector2 LastDelta { get; private set; }
    public bool LoopedThisFrame { get; private set; }
    public float GravityDirection => gravityDirection;
<<<<<<< Updated upstream

    private void Start()
    {
        // Fallback: existing baked scenes may have solidMask = 0 (old Configure didn't pass it).
        // Auto-detect Ground layer and rebuild filter so ghost collides with the world.
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
=======
>>>>>>> Stashed changes

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        box = GetComponent<BoxCollider2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.freezeRotation = true;
        rb.useFullKinematicContacts = true;
        GetComponent<ActorIdentity>().SetKind(DeathAnchorActorKind.Ghost);

        solidFilter = new ContactFilter2D
        {
            useLayerMask = true,
            useTriggers = false,
<<<<<<< Updated upstream
            layerMask = solidMask
=======
            layerMask = LayerMask.GetMask("Ground")
>>>>>>> Stashed changes
        };
    }

    private void FixedUpdate()
    {
        if (!hasRecord || frames.Count == 0)
        {
            LastDelta = Vector2.zero;
            LoopedThisFrame = false;
            return;
        }

        float dt = Time.fixedDeltaTime;
        float localTime = Mathf.Repeat(Time.time - startedAt, duration);
        LoopedThisFrame = localTime < previousLocalTime;
        previousLocalTime = localTime;

        if (LoopedThisFrame)
        {
<<<<<<< Updated upstream
            // Reset to anchor position on loop so ghost starts fresh each cycle
=======
>>>>>>> Stashed changes
            Vector2 footPos = gravityDirection > 0f
                ? anchorFootPosition
                : anchorFootPosition + Vector2.up * playerHeight;
            rb.position = footPos + Vector2.up * playerHeight * 0.5f;
            verticalSpeed = 0f;
            grounded = false;
            groundCollider = null;
        }

        DeathAnchorReplayFrame frame = Sample(localTime);
        float input = frame.horizontalInput;

        ProbeGround();

<<<<<<< Updated upstream
        // Jump
=======
>>>>>>> Stashed changes
        if (frame.jumpPressed && grounded)
        {
            verticalSpeed = jumpSpeed * gravityDirection;
            grounded = false;
            groundCollider = null;
        }

<<<<<<< Updated upstream
        // Gravity
        if (!grounded)
        {
            float gravityThisFrame = verticalSpeed * gravityDirection < 0f
                ? gravity * fallGravityMultiplier
                : gravity;
            verticalSpeed -= gravityDirection * gravityThisFrame * dt;
=======
        if (!grounded)
        {
            float gf = verticalSpeed * gravityDirection < 0f
                ? gravity * fallGravityMultiplier
                : gravity;
            verticalSpeed -= gravityDirection * gf * dt;
>>>>>>> Stashed changes
            verticalSpeed = Mathf.Clamp(verticalSpeed, -maxFallSpeed, maxFallSpeed);
        }
        else if (verticalSpeed * gravityDirection < 0f)
        {
            verticalSpeed = 0f;
        }

<<<<<<< Updated upstream
        // Horizontal movement
        Move(Vector2.right, input * moveSpeed * dt);

        // Vertical movement — verticalSpeed already encodes direction via gravityDirection
=======
        Move(Vector2.right, input * moveSpeed * dt);
>>>>>>> Stashed changes
        Move(Vector2.up, verticalSpeed * dt);

        PlaceOnPlayerIfNeeded();
    }

    public void Configure(float width, float height, LayerMask solidMask, LayerMask playerMask)
    {
        if (box == null) box = GetComponent<BoxCollider2D>();
        box.size = new Vector2(width, height);
        box.offset = Vector2.zero;
        playerHeight = height;
        this.solidMask = solidMask;
        this.playerMask = playerMask;
        solidFilter = new ContactFilter2D
        {
            useLayerMask = true,
            useTriggers = false,
            layerMask = solidMask
        };
    }

    public void Play(Vector2 anchorFootPosition, IReadOnlyList<DeathAnchorReplayFrame> sourceFrames)
    {
        this.anchorFootPosition = anchorFootPosition;
        frames.Clear();
        frames.AddRange(sourceFrames);
        duration = Mathf.Max(0.1f, frames[frames.Count - 1].time);
        startedAt = Time.time;
        previousLocalTime = 0f;
        hasRecord = true;
        verticalSpeed = 0f;
        grounded = false;
        groundCollider = null;
        gameObject.SetActive(true);

<<<<<<< Updated upstream
        // Place ghost at anchor position
=======
>>>>>>> Stashed changes
        Vector2 footPos = gravityDirection > 0f
            ? anchorFootPosition
            : anchorFootPosition + Vector2.up * playerHeight;
        rb.position = footPos + Vector2.up * playerHeight * 0.5f;
        previousPosition = rb.position;
        LastDelta = Vector2.zero;
    }

<<<<<<< Updated upstream
    /// <summary>
    /// Set gravity direction. +1 = normal (down), -1 = reverse (up).
    /// </summary>
=======
>>>>>>> Stashed changes
    public void SetGravityDirection(float direction)
    {
        gravityDirection = Mathf.Sign(direction);
        if (gravityDirection == 0f) gravityDirection = 1f;
    }

    private DeathAnchorReplayFrame Sample(float time)
    {
        if (frames.Count == 1 || time <= frames[0].time)
            return frames[0];

        for (int i = 1; i < frames.Count; i++)
        {
<<<<<<< Updated upstream
            if (time > frames[i].time)
            {
                continue;
            }

            // Use the earlier frame's input (no interpolation for inputs)
            // If closer to next frame, use next frame
            DeathAnchorReplayFrame previous = frames[i - 1];
            DeathAnchorReplayFrame next = frames[i];
            float t = Mathf.InverseLerp(previous.time, next.time, time);
            return t < 0.5f ? previous : next;
=======
            if (time > frames[i].time) continue;
            float t = Mathf.InverseLerp(frames[i - 1].time, frames[i].time, time);
            return t < 0.5f ? frames[i - 1] : frames[i];
>>>>>>> Stashed changes
        }

        return frames[frames.Count - 1];
    }

    private void ProbeGround()
    {
<<<<<<< Updated upstream
        Vector2 castDirection = gravityDirection > 0f ? Vector2.down : Vector2.up;
        int count = box.Cast(castDirection, solidFilter, castHits, skinWidth + 0.04f);
=======
        Vector2 cd = gravityDirection > 0f ? Vector2.down : Vector2.up;
        int count = box.Cast(cd, solidFilter, castHits, skinWidth + 0.04f);
>>>>>>> Stashed changes
        grounded = false;
        groundCollider = null;

        for (int i = 0; i < count; i++)
        {
            if (castHits[i].collider == null || castHits[i].collider.isTrigger)
<<<<<<< Updated upstream
            {
                continue;
            }

            float expectedNormal = gravityDirection > 0f ? 1f : -1f;
            if (castHits[i].normal.y * expectedNormal > 0.45f)
            {
                grounded = true;
                groundCollider = castHits[i].collider;
                if (verticalSpeed * gravityDirection < 0f)
                {
                    verticalSpeed = 0f;
                }
=======
                continue;

            float en = gravityDirection > 0f ? 1f : -1f;
            if (castHits[i].normal.y * en > 0.45f)
            {
                grounded = true;
                groundCollider = castHits[i].collider;
                if (verticalSpeed * gravityDirection < 0f) verticalSpeed = 0f;
>>>>>>> Stashed changes
                return;
            }
        }
    }

    private void Move(Vector2 direction, float distance)
    {
<<<<<<< Updated upstream
        if (Mathf.Abs(distance) <= 0.0001f)
        {
            return;
        }

        float sign = Mathf.Sign(distance);
        float magnitude = Mathf.Abs(distance);
        Vector2 castDirection = direction * sign;
        int count = box.Cast(castDirection, solidFilter, castHits, magnitude + skinWidth);
        float allowedDistance = magnitude;

        for (int i = 0; i < count; i++)
        {
            if (castHits[i].collider == null || castHits[i].collider.isTrigger)
            {
                continue;
            }

            if (IsInitialOverlap(castHits[i]))
            {
                continue;
            }

            allowedDistance = Mathf.Min(allowedDistance, Mathf.Max(0f, castHits[i].distance - skinWidth));
        }

        Vector2 delta = castDirection * allowedDistance;
        rb.position += delta;

        if (allowedDistance < magnitude && direction.y != 0f)
        {
            verticalSpeed = 0f;
        }
=======
        if (Mathf.Abs(distance) <= 0.0001f) return;

        float sign = Mathf.Sign(distance);
        float mag = Mathf.Abs(distance);
        Vector2 cd = direction * sign;
        int count = box.Cast(cd, solidFilter, castHits, mag + skinWidth);
        float allowed = mag;
        bool isHorizontal = Mathf.Abs(direction.x) > 0.5f;

        for (int i = 0; i < count; i++)
        {
            if (castHits[i].collider == null || castHits[i].collider.isTrigger) continue;
            if (IsInitialOverlap(castHits[i])) continue;
            // Skip ground when moving horizontally, skip walls when moving vertically
            if (isHorizontal && Mathf.Abs(castHits[i].normal.y) > 0.7f) continue;
            if (!isHorizontal && Mathf.Abs(castHits[i].normal.x) > 0.7f) continue;
            allowed = Mathf.Min(allowed, Mathf.Max(0f, castHits[i].distance - skinWidth));
        }

        rb.position += cd * allowed;
        if (allowed < mag && direction.y != 0f) verticalSpeed = 0f;
>>>>>>> Stashed changes
    }

    private bool IsInitialOverlap(RaycastHit2D hit)
    {
<<<<<<< Updated upstream
        if (hit.collider == null)
        {
            return false;
        }

        ColliderDistance2D distance = Physics2D.Distance(box, hit.collider);
        return distance.isOverlapped;
=======
        if (hit.collider == null) return false;
        ColliderDistance2D d = Physics2D.Distance(box, hit.collider);
        return d.isOverlapped;
>>>>>>> Stashed changes
    }

    private void PlaceOnPlayerIfNeeded()
    {
        Collider2D overlap = Physics2D.OverlapBox(rb.position, box.size, 0f, playerMask);
        if (overlap == null) return;

        Bounds pb = overlap.bounds;
        Bounds gb = box.bounds;
        if (LastDelta.y <= 0f && gb.min.y >= pb.center.y)
        {
            rb.position = new Vector2(rb.position.x, pb.max.y + box.size.y * 0.5f);
            LastDelta = Vector2.zero;
        }
    }
}
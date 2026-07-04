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

    [Header("Reverse Gravity（1 = 正常方向，-1 = 反转）")]
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
            layerMask = solidMask
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
            // Reset to anchor position on loop so ghost starts fresh each cycle
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

        // Jump
        if (frame.jumpPressed && grounded)
        {
            verticalSpeed = jumpSpeed * gravityDirection;
            grounded = false;
            groundCollider = null;
        }

        // Gravity
        if (!grounded)
        {
            float gravityThisFrame = verticalSpeed * gravityDirection < 0f
                ? gravity * fallGravityMultiplier
                : gravity;
            verticalSpeed -= gravityDirection * gravityThisFrame * dt;
            verticalSpeed = Mathf.Clamp(verticalSpeed, -maxFallSpeed, maxFallSpeed);
        }
        else if (verticalSpeed * gravityDirection < 0f)
        {
            verticalSpeed = 0f;
        }

        // Horizontal movement
        Move(Vector2.right, input * moveSpeed * dt);

        // Vertical movement — verticalSpeed already encodes direction via gravityDirection
        Move(Vector2.up, verticalSpeed * dt);

        PlaceOnPlayerIfNeeded();
    }

    public void Configure(float width, float height, LayerMask solidMask, LayerMask playerMask)
    {
        if (box == null)
        {
            box = GetComponent<BoxCollider2D>();
        }

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

        // Place ghost at anchor position
        Vector2 footPos = gravityDirection > 0f
            ? anchorFootPosition
            : anchorFootPosition + Vector2.up * playerHeight;
        rb.position = footPos + Vector2.up * playerHeight * 0.5f;
        previousPosition = rb.position;
        LastDelta = Vector2.zero;
    }

    /// <summary>
    /// Set gravity direction. +1 = normal (down), -1 = reverse (up).
    /// </summary>
    public void SetGravityDirection(float direction)
    {
        gravityDirection = Mathf.Sign(direction);
        if (gravityDirection == 0f) gravityDirection = 1f;
    }

    private DeathAnchorReplayFrame Sample(float time)
    {
        if (frames.Count == 1 || time <= frames[0].time)
        {
            return frames[0];
        }

        for (int i = 1; i < frames.Count; i++)
        {
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
        }

        return frames[frames.Count - 1];
    }

    private void ProbeGround()
    {
        Vector2 castDirection = gravityDirection > 0f ? Vector2.down : Vector2.up;
        int count = box.Cast(castDirection, solidFilter, castHits, skinWidth + 0.04f);
        grounded = false;
        groundCollider = null;

        for (int i = 0; i < count; i++)
        {
            if (castHits[i].collider == null || castHits[i].collider.isTrigger)
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
                return;
            }
        }
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
    }

    private bool IsInitialOverlap(RaycastHit2D hit)
    {
        if (hit.collider == null)
        {
            return false;
        }

        ColliderDistance2D distance = Physics2D.Distance(box, hit.collider);
        return distance.isOverlapped;
    }

    private void PlaceOnPlayerIfNeeded()
    {
        Collider2D overlap = Physics2D.OverlapBox(rb.position, box.size, 0f, playerMask);
        if (overlap == null)
        {
            return;
        }

        Bounds playerBounds = overlap.bounds;
        Bounds ghostBounds = box.bounds;
        if (LastDelta.y <= 0f && ghostBounds.min.y >= playerBounds.center.y)
        {
            rb.position = new Vector2(rb.position.x, playerBounds.max.y + box.size.y * 0.5f);
            LastDelta = Vector2.zero;
        }
    }
}
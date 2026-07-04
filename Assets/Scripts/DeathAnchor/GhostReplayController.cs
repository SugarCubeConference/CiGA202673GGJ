using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(ActorIdentity))]
public sealed class GhostReplayController : MonoBehaviour
{
    [SerializeField] private float playerHeight = 0.42f;
    [SerializeField] private LayerMask playerMask;

    private readonly List<DeathAnchorReplayFrame> frames = new List<DeathAnchorReplayFrame>();
    private Rigidbody2D rb;
    private BoxCollider2D box;
    private Vector2 anchorFootPosition;
    private float startedAt;
    private float duration;
    private float previousLocalTime;
    private bool hasRecord;

    public Vector2 LastDelta { get; private set; }
    public bool LoopedThisFrame { get; private set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        box = GetComponent<BoxCollider2D>();
        rb.freezeRotation = true;
        rb.useFullKinematicContacts = true;
        GetComponent<ActorIdentity>().SetKind(DeathAnchorActorKind.Ghost);
    }

    private void FixedUpdate()
    {
        if (!hasRecord || frames.Count == 0)
        {
            LastDelta = Vector2.zero;
            LoopedThisFrame = false;
            return;
        }

        float localTime = Mathf.Repeat(Time.time - startedAt, duration);
        LoopedThisFrame = localTime < previousLocalTime;
        previousLocalTime = localTime;

        DeathAnchorReplayFrame frame = Sample(localTime);
        Vector2 nextFoot = anchorFootPosition + frame.footOffset;
        Vector2 nextPosition = nextFoot + Vector2.up * playerHeight * 0.5f;
        LastDelta = LoopedThisFrame ? Vector2.zero : nextPosition - rb.position;
        rb.position = nextPosition;

        PlaceOnPlayerIfNeeded();
    }

    public void Configure(float width, float height, LayerMask playerMask)
    {
        if (box == null)
        {
            box = GetComponent<BoxCollider2D>();
        }

        box.size = new Vector2(width, height);
        box.offset = Vector2.zero;
        playerHeight = height;
        this.playerMask = playerMask;
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
        gameObject.SetActive(true);

        DeathAnchorReplayFrame first = frames[0];
        rb.position = anchorFootPosition + first.footOffset + Vector2.up * playerHeight * 0.5f;
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

            DeathAnchorReplayFrame previous = frames[i - 1];
            DeathAnchorReplayFrame next = frames[i];
            float t = Mathf.InverseLerp(previous.time, next.time, time);
            return new DeathAnchorReplayFrame(
                time,
                Vector2.Lerp(previous.footOffset, next.footOffset, t),
                t < 0.5f ? previous.facing : next.facing);
        }

        return frames[frames.Count - 1];
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

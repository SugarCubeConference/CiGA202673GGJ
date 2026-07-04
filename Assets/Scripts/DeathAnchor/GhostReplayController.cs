using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 分身幽灵控制器——回放录制的玩家输入，而非位置。
/// 拥有独立的物理引擎（重力、跳跃、碰撞检测），
/// 因此可以独立修改物理参数实现反重力分身等变体。
/// 
/// 核心设计：
///   - 从 DeathAnchorReplayFrame 读取输入（水平方向 + 跳跃按下）
///   - 用 BoxCast 进行碰撞检测（与玩家相同的底层碰撞逻辑）
///   - 支持 gravityDirection：1=正常重力，-1=反重力
///   - 每轮循环结束后重置到锚点位置重新回放
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(ActorIdentity))]
public sealed class GhostReplayController : MonoBehaviour
{
    [Header("物理参数（独立于玩家）")]
    [SerializeField] private float moveSpeed = 2.85f;
    [SerializeField] private float jumpSpeed = 6.7f;
    [SerializeField] private float gravity = 20.5f;
    [SerializeField] private float fallGravityMultiplier = 1.22f;
    [SerializeField] private float maxFallSpeed = 9.2f;

    [Header("反重力（1 = 正常向下, -1 = 反转向上）")]
    [SerializeField] private float gravityDirection = 1f;

    [Header("碰撞")]
    [SerializeField] private LayerMask solidMask;
    [SerializeField] private float skinWidth = 0.02f;

    [Header("交互")]
    [SerializeField] private float playerHeight = 0.42f;   // 玩家(及分身)的高度
    [SerializeField] private LayerMask playerMask;          // 玩家层（用于站在玩家头上的检测）

    private readonly List<DeathAnchorReplayFrame> frames = new List<DeathAnchorReplayFrame>();
    private readonly RaycastHit2D[] castHits = new RaycastHit2D[8];

    private Rigidbody2D rb;
    private BoxCollider2D box;
    private ContactFilter2D solidFilter;
    private Vector2 anchorFootPosition;    // 锚点脚底位置
    private float startedAt;               // 回放开始时间
    private float duration;                // 回放总时长
    private float previousLocalTime;       // 上一帧的本地时间（用于检测循环）
    private bool hasRecord;
    private float verticalSpeed;
    private bool grounded;
    private Collider2D groundCollider;
    private Vector2 previousPosition;      // 上一帧位置（未使用，保留给 LastDelta 计算）

    /// <summary>最近一帧的位移增量（站在上面的角色用它来同步移动）</summary>
    public Vector2 LastDelta { get; private set; }
    /// <summary>本帧是否发生了循环重置</summary>
    public bool LoopedThisFrame { get; private set; }
    /// <summary>当前重力方向</summary>
    public float GravityDirection => gravityDirection;

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
            layerMask = LayerMask.GetMask("Ground")
        };
    }

    /// <summary>FixedUpdate：从录制帧读取输入，驱动独立物理模拟</summary>
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

        // 循环时重置到锚点位置
        if (LoopedThisFrame)
        {
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

        // 跳跃
        if (frame.jumpPressed && grounded)
        {
            verticalSpeed = jumpSpeed * gravityDirection;
            grounded = false;
            groundCollider = null;
        }

        // 重力（考虑重力方向）
        if (!grounded)
        {
            float gf = verticalSpeed * gravityDirection < 0f
                ? gravity * fallGravityMultiplier
                : gravity;
            verticalSpeed -= gravityDirection * gf * dt;
            verticalSpeed = Mathf.Clamp(verticalSpeed, -maxFallSpeed, maxFallSpeed);
        }
        else if (verticalSpeed * gravityDirection < 0f)
        {
            verticalSpeed = 0f;
        }

        // 移动
        Move(Vector2.right, input * moveSpeed * dt);
        Move(Vector2.up, verticalSpeed * dt);

        PlaceOnPlayerIfNeeded();
    }

    /// <summary>由 Baker 调用，配置分身尺寸与碰撞层</summary>
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

    /// <summary>开始回放——由 GameManager 调用</summary>
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

        // 将分身放置在锚点
        Vector2 footPos = gravityDirection > 0f
            ? anchorFootPosition
            : anchorFootPosition + Vector2.up * playerHeight;
        rb.position = footPos + Vector2.up * playerHeight * 0.5f;
        previousPosition = rb.position;
        LastDelta = Vector2.zero;
    }

    /// <summary>设置重力方向。+1=正常向下, -1=反转向上</summary>
    public void SetGravityDirection(float direction)
    {
        gravityDirection = Mathf.Sign(direction);
        if (gravityDirection == 0f) gravityDirection = 1f;
    }

    /// <summary>根据时间采样最近的输入帧（非插值，取最近的帧）</summary>
    private DeathAnchorReplayFrame Sample(float time)
    {
        if (frames.Count == 1 || time <= frames[0].time) return frames[0];

        for (int i = 1; i < frames.Count; i++)
        {
            if (time > frames[i].time) continue;
            float t = Mathf.InverseLerp(frames[i - 1].time, frames[i].time, time);
            return t < 0.5f ? frames[i - 1] : frames[i];
        }

        return frames[frames.Count - 1];
    }

    /// <summary>根据重力方向检测地面/天花板</summary>
    private void ProbeGround()
    {
        Vector2 cd = gravityDirection > 0f ? Vector2.down : Vector2.up;
        int count = box.Cast(cd, solidFilter, castHits, skinWidth + 0.04f);
        grounded = false;
        groundCollider = null;

        for (int i = 0; i < count; i++)
        {
            if (castHits[i].collider == null || castHits[i].collider.isTrigger) continue;

            float en = gravityDirection > 0f ? 1f : -1f;
            if (castHits[i].normal.y * en > 0.45f)
            {
                grounded = true;
                groundCollider = castHits[i].collider;
                if (verticalSpeed * gravityDirection < 0f) verticalSpeed = 0f;
                return;
            }
        }
    }

    /// <summary>BoxCast 移动——处理碰撞并限制移动距离。水平时忽略地面法线，垂直时忽略墙壁法线</summary>
    private void Move(Vector2 direction, float distance)
    {
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
            // 水平移动时忽略地面法线，垂直移动时忽略墙壁法线
            if (isHorizontal && Mathf.Abs(castHits[i].normal.y) > 0.7f) continue;
            if (!isHorizontal && Mathf.Abs(castHits[i].normal.x) > 0.7f) continue;
            allowed = Mathf.Min(allowed, Mathf.Max(0f, castHits[i].distance - skinWidth));
        }

        rb.position += cd * allowed;
        if (allowed < mag && direction.y != 0f) verticalSpeed = 0f;
    }

    private bool IsInitialOverlap(RaycastHit2D hit)
    {
        if (hit.collider == null) return false;
        ColliderDistance2D d = Physics2D.Distance(box, hit.collider);
        return d.isOverlapped;
    }

    /// <summary>如果分身落在玩家头顶上，调整位置以站在玩家头上</summary>
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
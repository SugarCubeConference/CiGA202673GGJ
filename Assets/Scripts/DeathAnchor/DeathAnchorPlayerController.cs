using UnityEngine;

/// <summary>
/// Death Anchor 玩家控制器——基于 Kinematic Rigidbody2D 的自定义物理系统。
/// 使用 BoxCast 进行碰撞检测，支持移动、跳跃、土狼时间、跳跃缓冲、
/// 跳跃截断、蹬墙滑行、移动平台搭载。
/// 不与分身幽灵发生物理碰撞（通过 IsGhostCollider 忽略）。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(ActorIdentity))]
public sealed class DeathAnchorPlayerController : MonoBehaviour
{
    [Header("移动")]
    [SerializeField] private float moveSpeed = 2.85f;
    [SerializeField] private float jumpSpeed = 6.7f;
    [SerializeField] private float gravity = 15f;
    [SerializeField] private float fallGravityMultiplier = 1.22f;  // 下落时重力倍率
    [SerializeField] private float maxFallSpeed = 7f;
    [SerializeField] private float coyoteTime = 0.095f;       // 土狼时间（离地后仍可跳跃的宽限期）
    [SerializeField] private float jumpBufferTime = 0.13f;    // 跳跃缓冲（提前按键的有效时间）
    [SerializeField] private float jumpCutMultiplier = 0.52f; // 松键时速度缩放

    [Header("蹬墙滑行")]
    [SerializeField] private bool wallSlideEnabled = true;
    [SerializeField] private float wallSlideMaxSpeed = 1.25f;

    [Header("碰撞")]
    [SerializeField] private LayerMask solidMask;
    [SerializeField] private float skinWidth = 0.02f;  // 碰撞皮肤宽度

    /// <summary>BoxCast 命中缓冲区（复用，避免 GC）</summary>
    private readonly RaycastHit2D[] castHits = new RaycastHit2D[8];

    private Rigidbody2D rb;
    private BoxCollider2D box;
    private ContactFilter2D solidFilter;     // 碰撞过滤器
    private float verticalSpeed;             // 当前垂直速度
    private float lastGroundedAt = -999f;    // 最后一次着地的时间
    private float lastJumpPressedAt = -999f; // 最后一次按跳跃键的时间
    private int facing = 1;                  // 朝向 (1=右, -1=左)
    private bool grounded;                   // 是否着地
    private Collider2D groundCollider;       // 当前脚下的碰撞体

    /// <summary>脚底世界坐标（用于锚点定位）</summary>
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
    }

    /// <summary>Update：处理跳跃输入（按键和松键截断）</summary>
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            lastJumpPressedAt = Time.time;
        }

        // 跳跃截断：上升中松键时减小垂直速度
        if ((Input.GetKeyUp(KeyCode.W) || Input.GetKeyUp(KeyCode.Space) || Input.GetKeyUp(KeyCode.UpArrow)) && verticalSpeed > 0f)
        {
            verticalSpeed *= jumpCutMultiplier;
        }
    }

    /// <summary>FixedUpdate：物理更新——检测地面、处理输入、施加重力和移动</summary>
    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        float horizontalInput = GetHorizontalInput();

        if (Mathf.Abs(horizontalInput) > 0.01f)
            facing = horizontalInput > 0f ? 1 : -1;

        ProbeGround();         // 检测地面
        ApplyCarrierDelta();   // 搭载移动平台

        // 跳跃判断（土狼时间 + 跳跃缓冲）
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

        // 蹬墙滑行
        bool wallSliding = false;
        if (!grounded && wallSlideEnabled && verticalSpeed < 0f
            && Mathf.Abs(horizontalInput) > 0.01f
            && IsTouchingWall(Mathf.Sign(horizontalInput)))
        {
            wallSliding = true;
            verticalSpeed = Mathf.Max(verticalSpeed, -wallSlideMaxSpeed);
        }

        // 重力
        if (!wallSliding)
        {
            float gravityThisFrame = verticalSpeed < 0f ? gravity * fallGravityMultiplier : gravity;
            verticalSpeed = Mathf.Max(verticalSpeed - gravityThisFrame * dt, -maxFallSpeed);
        }

        // 执行移动
        Move(Vector2.right, horizontalInput * moveSpeed * dt);
        Move(Vector2.up, verticalSpeed * dt);
    }

    /// <summary>由 Baker 调用，配置玩家参数</summary>
    public void Configure(float playerWidthUnits, float playerHeightUnits, LayerMask solidMask,
        bool wallSlideEnabled, float wallSlideMaxSpeedUnits)
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

    /// <summary>将玩家传送到指定脚底位置</summary>
    public void SpawnAtFootPosition(Vector2 footPosition)
    {
        EnsureCachedComponents();

        Vector2 bodyPosition = footPosition + Vector2.up * (box.size.y * 0.5f - box.offset.y);
        transform.position = bodyPosition;
        rb.position = bodyPosition;
        verticalSpeed = 0f;
        lastJumpPressedAt = -999f;
    }

    /// <summary>延迟缓存组件引用</summary>
    private void EnsureCachedComponents()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (box == null) box = GetComponent<BoxCollider2D>();
    }

    /// <summary>获取水平输入（A/D 或 左/右箭头）</summary>
    private float GetHorizontalInput()
    {
        float input = 0f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) input -= 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) input += 1f;
        return Mathf.Clamp(input, -1f, 1f);
    }

    /// <summary>向下 BoxCast 检测地面</summary>
    private void ProbeGround()
    {
        int count = box.Cast(Vector2.down, solidFilter, castHits, skinWidth + 0.04f);
        grounded = false;
        groundCollider = null;

        for (int i = 0; i < count; i++)
        {
            if (castHits[i].collider == null || castHits[i].collider.isTrigger) continue;
            if (IsGhostCollider(castHits[i].collider)) continue;  // 忽略分身碰撞

            if (castHits[i].normal.y > 0.45f)
            {
                grounded = true;
                groundCollider = castHits[i].collider;
                lastGroundedAt = Time.time;
                if (verticalSpeed < 0f) verticalSpeed = 0f; // 落地时停止下落
                return;
            }
        }
    }

    /// <summary>搭载移动平台：随平台位移同步移动</summary>
    private void ApplyCarrierDelta()
    {
        if (!grounded || groundCollider == null) return;

        MovingPlatform2D movingPlatform = groundCollider.GetComponent<MovingPlatform2D>();
        if (movingPlatform != null)
        {
            rb.position += movingPlatform.LastDelta;
            return;
        }

        // 分身不作为玩家的载体
    }

    /// <summary>检测是否贴墙（用于蹬墙滑行）</summary>
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

    /// <summary>BoxCast 移动——处理碰撞并限制移动距离</summary>
    private void Move(Vector2 direction, float distance)
    {
        if (Mathf.Abs(distance) <= 0.0001f) return;

        float sign = Mathf.Sign(distance);
        float magnitude = Mathf.Abs(distance);
        Vector2 castDirection = direction * sign;
        int count = box.Cast(castDirection, solidFilter, castHits, magnitude + skinWidth);
        float allowedDistance = magnitude;
        bool isHorizontal = Mathf.Abs(direction.x) > 0.5f;

        for (int i = 0; i < count; i++)
        {
            if (castHits[i].collider == null || castHits[i].collider.isTrigger) continue;
            if (IsGhostCollider(castHits[i].collider)) continue;       // 忽略分身
            if (IsInitialOverlap(castHits[i])) continue;                // 忽略初始重叠

            // 水平移动时忽略地面法线（防止被地面卡住）
            if (isHorizontal && Mathf.Abs(castHits[i].normal.y) > 0.7f) continue;
            // 垂直移动时忽略墙壁法线
            if (!isHorizontal && Mathf.Abs(castHits[i].normal.x) > 0.7f) continue;

            allowedDistance = Mathf.Min(allowedDistance, Mathf.Max(0f, castHits[i].distance - skinWidth));
        }

        rb.position += castDirection * allowedDistance;
        if (allowedDistance < magnitude && direction.y != 0f)
        {
            verticalSpeed = 0f;  // 垂直碰撞时清除速度
        }
    }

    /// <summary>判断碰撞体是否属于分身幽灵</summary>
    private bool IsGhostCollider(Collider2D candidate)
    {
        if (candidate == null) return false;
        return candidate.GetComponent<GhostReplayController>() != null
            || candidate.GetComponentInParent<GhostReplayController>() != null;
    }

    /// <summary>检查命中是否为初始重叠（Box 已嵌入碰撞体内部）</summary>
    private bool IsInitialOverlap(RaycastHit2D hit)
    {
        if (hit.collider == null) return false;
        ColliderDistance2D d = Physics2D.Distance(box, hit.collider);
        return d.isOverlapped;
    }
}
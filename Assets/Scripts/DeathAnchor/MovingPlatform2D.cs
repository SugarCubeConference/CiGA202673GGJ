using UnityEngine;

/// <summary>
/// 移动平台——在起点和目标位置之间 PingPong 循环移动。
/// 角色站在上面时会通过 CarrierDelta 机制被平台带着一起移动。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public sealed class MovingPlatform2D : MonoBehaviour
{
    [SerializeField] private Vector2 targetOffset = new Vector2(2f, 0f);
    [SerializeField] private float periodSec = 3f;

    private Rigidbody2D rb;
    private Vector2 startPosition;
    private Vector2 previousPosition;

    /// <summary>最近一帧的位移增量，角色用它来同步移动</summary>
    public Vector2 LastDelta { get; private set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.freezeRotation = true;
        startPosition = rb.position;
        previousPosition = startPosition;
    }

    /// <summary>FixedUpdate：计算 PingPong 位置并更新 LastDelta</summary>
    private void FixedUpdate()
    {
        float period = Mathf.Max(0.1f, periodSec);
        float t = Mathf.PingPong(Time.time / period, 1f);
        Vector2 nextPosition = Vector2.Lerp(startPosition, startPosition + targetOffset, t);
        LastDelta = nextPosition - previousPosition;
        previousPosition = nextPosition;
        rb.MovePosition(nextPosition);
    }

    /// <summary>由 Baker 调用，配置目标位置和周期</summary>
    public void Configure(Vector2 targetWorldPosition, float periodSec)
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();

        startPosition = transform.position;
        previousPosition = startPosition;
        targetOffset = targetWorldPosition - startPosition;
        this.periodSec = periodSec > 0f ? periodSec : 3f;
    }
}
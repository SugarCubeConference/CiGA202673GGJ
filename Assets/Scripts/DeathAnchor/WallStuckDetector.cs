using UnityEngine;

/// <summary>
/// 检测 Player 的 BoxCollider2D 是否与墙体（solidMask）重叠，
/// 如果卡住则向最近的空闲方向弹出。
/// 挂在同一个 GameObject 上即可，与 DeathAnchorPlayerController 独立工作。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public sealed class WallStuckDetector : MonoBehaviour
{
    [Header("Detection")]
    [Tooltip("与墙壁检测相同的 LayerMask（应和 PlayerController 的 solidMask 一致）")]
    [SerializeField] private LayerMask solidMask;

    [Tooltip("检测频率：每隔几帧检测一次，0 = 每帧")]
    [SerializeField] private int checkEveryNFrames = 2;

    [Header("Ejection")]
    [Tooltip("每个方向尝试弹出的最大距离（世界单位）")]
    [SerializeField] private float maxPushDistance = 2.0f;

    [Tooltip("弹出时重置竖直速度")]
    [SerializeField] private bool resetVerticalSpeedOnEject = true;

    [Header("Debug")]
    [SerializeField] private bool debugLog = false;

    private Rigidbody2D rb;
    private BoxCollider2D box;
    private int frameCounter;

    // 8 个方向：N, NE, E, SE, S, SW, W, NW
    private static readonly Vector2[] Directions = new Vector2[]
    {
        Vector2.up,
        (Vector2.up + Vector2.right).normalized,
        Vector2.right,
        (Vector2.down + Vector2.right).normalized,
        Vector2.down,
        (Vector2.down + Vector2.left).normalized,
        Vector2.left,
        (Vector2.up + Vector2.left).normalized,
    };

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        box = GetComponent<BoxCollider2D>();
    }

    private void FixedUpdate()
    {
        if (checkEveryNFrames > 0)
        {
            frameCounter++;
            if (frameCounter % checkEveryNFrames != 0)
            {
                return;
            }
        }

        if (!IsStuck())
        {
            return;
        }

        if (debugLog)
        {
            Debug.Log("[WallStuckDetector] Player stuck at " + rb.position + ", searching ejection...", this);
        }

        Vector2 ejection = FindBestEjection();

        if (ejection.sqrMagnitude > 0.0001f)
        {
            rb.position += ejection;

            if (debugLog)
            {
                Debug.Log("[WallStuckDetector] Ejected by " + ejection + " to " + rb.position, this);
            }
        }
        else
        {
            // 极端情况：所有方向都被堵，尝试向上弹出
            Vector2 upPush = Vector2.up * maxPushDistance;
            rb.position += upPush;

            if (debugLog)
            {
                Debug.LogWarning("[WallStuckDetector] All directions blocked! Force push up to " + rb.position, this);
            }
        }
    }

    /// <summary>
    /// 检测当前位置是否与 solidMask 层的任何碰撞体重叠。
    /// 使用 Physics2D.OverlapBox，兼容所有 Unity 2021+ 版本。
    /// </summary>
    private bool IsStuck()
    {
        Vector2 center = (Vector2)transform.position + box.offset;
        Collider2D hit = Physics2D.OverlapBox(center, box.size, 0f, solidMask);
        return hit != null && !hit.isTrigger;
    }

    /// <summary>
    /// 尝试 8 个方向，找到最短的有效弹出向量。
    /// 逐步加大距离档位，找到第一个不重叠的位置。
    /// </summary>
    private Vector2 FindBestEjection()
    {
        float[] distances = { 0.05f, 0.1f, 0.2f, 0.35f, 0.5f, 0.8f, 1.2f, maxPushDistance };

        foreach (float dist in distances)
        {
            foreach (Vector2 dir in Directions)
            {
                Vector2 offset = dir * dist;
                Vector2 testCenter = (Vector2)transform.position + box.offset + offset;

                Collider2D hit = Physics2D.OverlapBox(testCenter, box.size, 0f, solidMask);
                if (hit == null || hit.isTrigger)
                {
                    return offset;
                }
            }
        }

        return Vector2.zero;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (box == null)
        {
            box = GetComponent<BoxCollider2D>();
        }
        if (box == null) return;

        // 绘制当前检测范围
        Gizmos.color = IsStuck() ? Color.red : Color.green;
        Vector3 center = transform.position + (Vector3)box.offset;
        Vector3 size = box.size;
        Gizmos.DrawWireCube(center, size);

        // 绘制弹出方向
        if (Application.isPlaying && IsStuck())
        {
            Gizmos.color = Color.yellow;
            foreach (Vector2 dir in Directions)
            {
                Vector3 from = transform.position;
                Vector3 to = from + (Vector3)(dir * 0.3f);
                Gizmos.DrawLine(from, to);
                Gizmos.DrawSphere(to, 0.02f);
            }
        }
    }
#endif
}

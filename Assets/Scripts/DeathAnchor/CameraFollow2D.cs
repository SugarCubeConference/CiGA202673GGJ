using UnityEngine;

/// <summary>
/// 2D 相机跟随——平滑追踪目标位置，并限制在关卡边界内。
/// </summary>
public sealed class CameraFollow2D : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector2 minPosition;
    [SerializeField] private Vector2 maxPosition;
    [SerializeField] private float smoothTime = 0.12f;

    /// <summary>SmoothDamp 速度缓存</summary>
    private Vector3 velocity;

    /// <summary>每帧 LateUpdate 执行（在角色移动之后），确保相机平滑跟随</summary>
    private void LateUpdate()
    {
        if (target == null) return;

        // 计算目标位置，并用 Clamp 限制在关卡边界内
        Vector3 desired = new Vector3(
            Mathf.Clamp(target.position.x, minPosition.x, maxPosition.x),
            Mathf.Clamp(target.position.y, minPosition.y, maxPosition.y),
            transform.position.z);
        transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
    }

    /// <summary>由 Baker 调用，配置跟踪目标与边界</summary>
    public void Configure(Transform target, Vector2 minPosition, Vector2 maxPosition)
    {
        this.target = target;
        this.minPosition = minPosition;
        this.maxPosition = maxPosition;
    }
}
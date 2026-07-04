using UnityEngine;

public sealed class CameraFollow2D : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector2 minPosition;
    [SerializeField] private Vector2 maxPosition;
    [SerializeField] private float smoothTime = 0.12f;

    private Vector3 velocity;

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 desired = new Vector3(
            Mathf.Clamp(target.position.x, minPosition.x, maxPosition.x),
            Mathf.Clamp(target.position.y, minPosition.y, maxPosition.y),
            transform.position.z);
        transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
    }

    public void Configure(Transform target, Vector2 minPosition, Vector2 maxPosition)
    {
        this.target = target;
        this.minPosition = minPosition;
        this.maxPosition = maxPosition;
    }
}

using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class MovingPlatform2D : MonoBehaviour
{
    [SerializeField] private Vector2 targetOffset = new Vector2(2f, 0f);
    [SerializeField] private float periodSec = 3f;
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private string motionMode = "pingpong";
    [SerializeField] private bool activated = true;

    private Rigidbody2D rb;
    private Vector2 startPosition;
    private Vector2 previousPosition;
    private float progress;
    private int direction = 1;

    public Vector2 LastDelta { get; private set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.freezeRotation = true;
        startPosition = rb.position;
        previousPosition = startPosition;
    }

    private void FixedUpdate()
    {
        bool automatic = IsAutomaticMotion();
        if (!automatic && !activated && progress <= 0f)
        {
            LastDelta = Vector2.zero;
            previousPosition = rb.position;
            return;
        }

        float step = Time.fixedDeltaTime / Mathf.Max(0.1f, periodSec);
        if (automatic)
        {
            progress = Mathf.Clamp01(progress + step * direction);
            if (progress >= 1f)
            {
                direction = -1;
            }
            else if (progress <= 0f)
            {
                direction = 1;
            }
        }
        else
        {
            progress = Mathf.Clamp01(progress + (activated ? step : -step));
        }

        Vector2 nextPosition = Vector2.Lerp(startPosition, startPosition + targetOffset, progress);
        LastDelta = nextPosition - previousPosition;
        previousPosition = nextPosition;
        rb.MovePosition(nextPosition);
    }

    public void Configure(Vector2 targetWorldPosition, float periodSec)
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        startPosition = transform.position;
        previousPosition = startPosition;
        targetOffset = targetWorldPosition - startPosition;
        this.periodSec = periodSec > 0f ? periodSec : 3f;
        progress = 0f;
        direction = 1;
    }

    public void Configure(Vector2 targetWorldPosition, float periodSec, float moveSpeed, string motionMode)
    {
        Configure(targetWorldPosition, periodSec);
        this.moveSpeed = moveSpeed > 0f ? moveSpeed : 3f;
        this.motionMode = string.IsNullOrEmpty(motionMode) ? "pingpong" : motionMode;
        activated = IsAutomaticMotion();
    }

    public void SetActivated(bool active)
    {
        activated = active;
    }

    private bool IsAutomaticMotion()
    {
        return !motionMode.ToLowerInvariant().Contains("button");
    }
}

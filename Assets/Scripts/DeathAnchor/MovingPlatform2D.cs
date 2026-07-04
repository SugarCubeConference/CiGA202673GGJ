using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class MovingPlatform2D : MonoBehaviour
{
    [SerializeField] private Vector2 targetOffset = new Vector2(2f, 0f);
    [SerializeField] private float periodSec = 3f;

    private Rigidbody2D rb;
    private Vector2 startPosition;
    private Vector2 previousPosition;

    public Vector2 LastDelta { get; private set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        startPosition = rb.position;
        previousPosition = startPosition;
    }

    private void FixedUpdate()
    {
        float period = Mathf.Max(0.1f, periodSec);
        float t = Mathf.PingPong(Time.time / period, 1f);
        Vector2 nextPosition = Vector2.Lerp(startPosition, startPosition + targetOffset, t);
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
    }
}

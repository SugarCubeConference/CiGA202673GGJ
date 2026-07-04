using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach this to a world-space Canvas (note) object. When the player touches it,
/// the object moves to the bottom-center of the camera, scales up, and raises
/// its Canvas sorting order to render on top. Only one text can be expanded at
/// once. Leaving the collision area immediately collapses it and restores the
/// original sorting order.
/// BoxCollider2D is manually configured in the Inspector (isTrigger = true).
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D))]
public sealed class InteractableText : MonoBehaviour
{
    private static InteractableText activeText;

    [Header("Player")]
    [SerializeField] private float showDistance = 1.25f;
    [SerializeField] private float hideDistance = 2.25f;

    [Header("Delay")]
    [SerializeField] private float collapseDelay = 0.5f;
    [Header("Camera Layout")]
    [SerializeField] private Vector2 viewportAnchor = new Vector2(0.5f, 0.75f);
    [SerializeField] private float cameraPlaneZ = 0f;
    [SerializeField] private float expandedScale = 3.2f;

    [Header("Sorting")]
    [Tooltip("展开时的排序层级（高于原始值即可）")]
    [SerializeField] private int expandedSortingOrder = 100;

    [Header("Animation")]
    [SerializeField] private float showDuration = 0.18f;
    [SerializeField] private float hideDuration = 0.14f;

    private BoxCollider2D trigger;
    private Canvas canvas;
    private Transform player;
    private Transform originalParent;
    private int originalSiblingIndex;
    private int originalSortingOrder;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Vector3 originalScale;
    private Coroutine animationRoutine;
    private bool isExpanded;
    private float collapseTimer;
    private void Awake()
    {
        CacheOriginalTransform();
        trigger = GetComponent<BoxCollider2D>();
        canvas = GetComponent<Canvas>();
    }

    private void OnEnable()
    {
        if (originalParent == null)
        {
            CacheOriginalTransform();
        }
    }

    private void OnDisable()
    {
        if (activeText == this)
        {
            activeText = null;
        }
    }

    private void Update()
    {
        if (!isExpanded || player == null)
        {
            return;
        }

                if (Vector2.Distance(originalPosition, player.position) > hideDistance)
        {
            if (collapseTimer <= 0f)
            {
                collapseTimer = collapseDelay;
            }
        }
        else
        {
            collapseTimer = 0f;
        }

        if (collapseTimer > 0f)
        {
            collapseTimer -= Time.deltaTime;
            if (collapseTimer <= 0f)
            {
                Collapse();
                return;
            }
        }
        else
        {
            MoveToCameraAnchor();
        }}

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryExpand(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryExpand(other);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // The collider moves with the text while expanded, so Update handles the
        // real leave check against the text's original world position.
    }

    public void OnDialogDismissed()
    {
        Collapse();
    }

    private void TryExpand(Collider2D other)
    {
        if (!IsPlayer(other))
        {
            return;
        }

        player = other.transform;

        if (isExpanded)
        {
            MoveToCameraAnchor();
            return;
        }

        Expand();
    }

    private bool IsPlayer(Collider2D other)
    {
        if (other == null)
        {
            return false;
        }

        ActorIdentity actor = other.GetComponentInParent<ActorIdentity>();
        return actor != null && actor.IsPlayer;
    }

    private void Expand()
    {
        if (activeText != null && activeText != this)
        {
            activeText.Collapse();
        }

        activeText = this;
        isExpanded = true;
        transform.SetAsLastSibling();

        // 提高排序层级
        if (canvas != null)
        {
            canvas.sortingOrder = expandedSortingOrder;
        }

        Vector3 targetPosition = GetCameraAnchorPosition();
        Vector3 targetScale = originalScale * expandedScale;
        StartAnimation(targetPosition, targetScale, showDuration, true);
    }

    private void Collapse()
    {
        if (!isExpanded && activeText != this)
        {
            return;
        }

        if (activeText == this)
        {
            activeText = null;
        }

        isExpanded = false;
        player = null;

        // 立即恢复排序层级
        if (canvas != null)
        {
            canvas.sortingOrder = originalSortingOrder;
        }

        StartAnimation(originalPosition, originalScale, hideDuration, false);
    }

    private void StartAnimation(Vector3 targetPosition, Vector3 targetScale, float duration, bool keepAnchoredToCamera)
    {
        if (animationRoutine != null)
        {
            StopCoroutine(animationRoutine);
        }

        animationRoutine = StartCoroutine(AnimateTo(targetPosition, targetScale, duration, keepAnchoredToCamera));
    }

    private IEnumerator AnimateTo(Vector3 targetPosition, Vector3 targetScale, float duration, bool keepAnchoredToCamera)
    {
        Vector3 startPosition = transform.position;
        Vector3 startScale = transform.localScale;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            Vector3 nextTarget = keepAnchoredToCamera ? GetCameraAnchorPosition() : targetPosition;

            transform.position = Vector3.Lerp(startPosition, nextTarget, eased);
            transform.localScale = Vector3.Lerp(startScale, targetScale, eased);
            yield return null;
        }

        transform.position = keepAnchoredToCamera ? GetCameraAnchorPosition() : targetPosition;
        transform.localScale = targetScale;

        if (!keepAnchoredToCamera)
        {
            transform.SetParent(originalParent, true);
            transform.SetSiblingIndex(originalSiblingIndex);
            transform.rotation = originalRotation;
        }

        animationRoutine = null;
    }

    private void MoveToCameraAnchor()
    {
        transform.position = GetCameraAnchorPosition();
        transform.localScale = originalScale * expandedScale;
    }

    private Vector3 GetCameraAnchorPosition()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            return originalPosition;
        }

        if (cam.orthographic)
        {
            float halfHeight = cam.orthographicSize;
            float halfWidth = halfHeight * cam.aspect;
            Vector3 camPos = cam.transform.position;
            return new Vector3(
                camPos.x + Mathf.Lerp(-halfWidth, halfWidth, viewportAnchor.x),
                camPos.y + Mathf.Lerp(-halfHeight, halfHeight, viewportAnchor.y),
                cameraPlaneZ);
        }

        float distance = Mathf.Abs(cameraPlaneZ - cam.transform.position.z);
        Vector3 world = cam.ViewportToWorldPoint(new Vector3(viewportAnchor.x, viewportAnchor.y, distance));
        world.z = cameraPlaneZ;
        return world;
    }

    private void CacheOriginalTransform()
    {
        originalParent = transform.parent;
        originalSiblingIndex = transform.GetSiblingIndex();
        originalPosition = transform.position;
        originalRotation = transform.rotation;
        originalScale = transform.localScale;

        if (canvas == null) canvas = GetComponent<Canvas>();
        if (canvas != null) originalSortingOrder = canvas.sortingOrder;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.1f, 0.8f, 1f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, showDistance);
        Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, hideDistance);
    }
}

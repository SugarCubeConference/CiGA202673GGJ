using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class CameraBackgroundFitter : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private float distanceFromCamera = 10f;

    private SpriteRenderer spriteRenderer;
    private float lastOrthographicSize = -1f;
    private float lastAspect = -1f;
    private Sprite lastSprite;

    private void OnEnable()
    {
        TryGetComponent(out spriteRenderer);
        ResolveCamera();
        FitToCamera();
    }

    private void LateUpdate()
    {
        ResolveCamera();
        if (targetCamera == null || spriteRenderer == null)
        {
            return;
        }

        if (!Mathf.Approximately(lastOrthographicSize, targetCamera.orthographicSize)
            || !Mathf.Approximately(lastAspect, targetCamera.aspect)
            || lastSprite != spriteRenderer.sprite)
        {
            FitToCamera();
        }
    }

    public void Configure(Camera camera, float distance)
    {
        targetCamera = camera;
        distanceFromCamera = distance;
        TryGetComponent(out spriteRenderer);
        FitToCamera();
    }

    private void ResolveCamera()
    {
        if (targetCamera == null)
        {
            targetCamera = GetComponentInParent<Camera>();
        }
    }

    private void FitToCamera()
    {
        if (targetCamera == null || spriteRenderer == null || spriteRenderer.sprite == null)
        {
            return;
        }

        transform.localPosition = new Vector3(0f, 0f, distanceFromCamera);
        transform.localRotation = Quaternion.identity;

        float viewHeight = targetCamera.orthographicSize * 2f;
        float viewWidth = viewHeight * targetCamera.aspect;
        Vector2 spriteSize = spriteRenderer.sprite.bounds.size;
        transform.localScale = new Vector3(
            viewWidth / Mathf.Max(0.01f, spriteSize.x),
            viewHeight / Mathf.Max(0.01f, spriteSize.y),
            1f);

        lastOrthographicSize = targetCamera.orthographicSize;
        lastAspect = targetCamera.aspect;
        lastSprite = spriteRenderer.sprite;
    }
}

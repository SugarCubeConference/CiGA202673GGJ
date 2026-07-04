using UnityEngine;

/// <summary>
/// 挂载在摄像机上，从屏幕下方不断生成彩色透明气泡。
/// 气泡大小、颜色随机，匀速上升，超出屏幕后自动销毁。
/// 需要项目中存在一个白色圆形精灵（默认 "Circle" 或自动生成）。
/// </summary>
public class BubbleEmitter : MonoBehaviour
{
    [Header("生成")]
    [SerializeField] private float spawnInterval = 0.25f;      // 生成间隔（秒）
    [SerializeField] private float minSize = 0.08f;            // 最小气泡半径
    [SerializeField] private float maxSize = 0.35f;            // 最大气泡半径
    [SerializeField] private float riseSpeed = 1.2f;           // 上升速度
    [SerializeField] private float lifeTime = 8f;              // 气泡存活时间（秒）

    [Header("颜色（随机插值）")]
    [SerializeField] private Color minColor = new Color(0.55f, 0.78f, 1f, 0.25f);
    [SerializeField] private Color maxColor = new Color(0.9f, 0.95f, 1f, 0.7f);

    [Header("父节点")]
    [SerializeField] private Transform bubbleParent;

    private Camera cam;
    private float nextSpawnTime;
    private Sprite circleSprite;
    private Material bubbleMaterial;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;

        EnsureCircleSprite();

        if (bubbleParent == null)
        {
            var go = new GameObject("Bubbles");
            go.transform.SetParent(transform);
            bubbleParent = go.transform;
        }
    }

    private void Update()
    {
        if (Time.time < nextSpawnTime) return;
        nextSpawnTime = Time.time + spawnInterval;
        SpawnBubble();
    }

    private void EnsureCircleSprite()
    {
        // 程序化生成一个白色柔边圆形纹理
        Texture2D tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[32 * 32];
        float center = 15.5f;
        float radius = 15f;
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                float dist = Mathf.Sqrt((x - center) * (x - center) + (y - center) * (y - center));
                float alpha = 1f - Mathf.Clamp01(dist / radius);
                alpha = alpha * alpha;
                pixels[y * 32 + x] = new Color(1, 1, 1, alpha);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        circleSprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));
        circleSprite.name = "BubbleCircle";
    }

    private void SpawnBubble()
    {
        if (circleSprite == null) return;

        GameObject bubble = new GameObject("Bubble");
        bubble.transform.SetParent(bubbleParent);

        // 随机横向位置（屏幕底部）
        float screenX = Random.Range(0.05f, 0.95f);
        Vector3 worldPos = new Vector3(0, 0, 0);
        if (cam != null && cam.orthographic)
        {
            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;
            worldPos = new Vector3(
                Mathf.Lerp(-halfW, halfW, screenX),
                -halfH - 0.5f,
                0);
        }

        bubble.transform.position = worldPos;

        // 随机尺寸
        float size = Random.Range(minSize, maxSize);
        bubble.transform.localScale = Vector3.one * size;

        // SpriteRenderer
        SpriteRenderer sr = bubble.AddComponent<SpriteRenderer>();
        sr.sprite = circleSprite;
        sr.color = Color.Lerp(minColor, maxColor, Random.value);
        sr.sortingOrder = 100; // 确保在主菜单 UI 之上可见

        // 上升逻辑
        BubbleRise rise = bubble.AddComponent<BubbleRise>();
        rise.speed = riseSpeed;
        rise.cameraRef = cam;
        rise.lifeTime = lifeTime;
    }

    /// <summary>内部控制气泡的上升和销毁</summary>
    private class BubbleRise : MonoBehaviour
    {
        public float speed;
        public Camera cameraRef;
        public float lifeTime;
        private float elapsed;

        private void Update()
        {
            elapsed += Time.deltaTime;
            if (elapsed > lifeTime)
            {
                Destroy(gameObject);
                return;
            }

            transform.position += Vector3.up * speed * Time.deltaTime;

            // 超出屏幕顶界则销毁
            if (cameraRef != null && cameraRef.orthographic)
            {
                float top = cameraRef.orthographicSize + 1f;
                if (transform.position.y > top) Destroy(gameObject);
            }
        }
    }
}
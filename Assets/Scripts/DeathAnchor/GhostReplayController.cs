using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(ActorIdentity))]
public sealed class GhostReplayController : MonoBehaviour
{
    [Header("Replay")]
    [SerializeField] private float playerHeight = 0.42f;
    [SerializeField] private LayerMask playerMask;

    [Header("Afterimage")]
    [Tooltip("残影生成间隔（秒），越小越密集")]
    [SerializeField] private float afterimageInterval = 0.06f;
    [Tooltip("残影生命周期（秒），越长拖影越长")]
    [SerializeField] private float afterimageLifeCycle = 0.6f;
    [Tooltip("残影颜色（推荐半透明）")]
    [SerializeField] private Color afterimageColor = new Color(0.3f, 0.6f, 1f, 0.5f);
    [Tooltip("残影是否逐渐缩小消失")]
    [SerializeField] private bool afterimageScaleDown = true;
    [Tooltip("残影是否逐渐变暗")]
    [SerializeField] private bool afterimageFadeToDark = true;
    [Tooltip("最大同时存在的残影数量")]
    [SerializeField] private int afterimageMaxCount = 25;
    [Tooltip("残影 sortingOrder 偏移（负值=在分身后面）")]
    [SerializeField] private int afterimageSortingOffset = -1;

    private readonly List<DeathAnchorReplayFrame> frames = new List<DeathAnchorReplayFrame>();
    private Rigidbody2D rb;
    private BoxCollider2D box;
    private SpriteRenderer spriteRenderer;
    private Vector2 anchorFootPosition;
    private float startedAt;
    private float duration;
    private float previousLocalTime;
    private bool hasRecord;

    // Afterimage state
    private Material afterimageMaterial;
    private Sprite fallbackSprite;
    private Transform afterimageContainer;
    private readonly List<GameObject> afterimagePool = new List<GameObject>();
    private float afterimageTimer;
    private int lastFacing;

    public Vector2 LastDelta { get; private set; }
    public bool LoopedThisFrame { get; private set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        box = GetComponent<BoxCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.freezeRotation = true;
        rb.useFullKinematicContacts = true;

        GetComponent<ActorIdentity>().SetKind(DeathAnchorActorKind.Ghost);

        // If no sprite assigned, generate a procedural white square matching collider size
        if (spriteRenderer != null && spriteRenderer.sprite == null)
        {
            spriteRenderer.sprite = CreateFallbackSprite();
            spriteRenderer.color = afterimageColor;
        }

        // Create afterimage container at scene root
        afterimageContainer = new GameObject($"{gameObject.name}_Afterimages").transform;

        // Create afterimage material from CanYingShader2D
        Shader shader = Shader.Find("Custom/CanYingShader2D");
        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        afterimageMaterial = new Material(shader);
    }

    /// <summary>
    /// Creates a procedural white square sprite sized to match the BoxCollider2D.
    /// </summary>
    private Sprite CreateFallbackSprite()
    {
        float w = box.size.x;
        float h = box.size.y;

        // Use a small texture (4x4 is enough - it will be stretched)
        int texW = 4;
        int texH = 4;
        Texture2D tex = new Texture2D(texW, texH, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[texW * texH];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.white;
        }
        tex.SetPixels(pixels);
        tex.Apply(false, false);
        tex.filterMode = FilterMode.Point;

        // PPU = pixels-per-unit. We want the sprite to be exactly w x h in world units.
        // Sprite size in world = textureSize / PPU, so PPU = textureSize / worldSize.
        float ppuX = texW / w;
        float ppuY = texH / h;
        float ppu = Mathf.Min(ppuX, ppuY);

        fallbackSprite = Sprite.Create(tex, new Rect(0, 0, texW, texH), new Vector2(0.5f, 0.5f), ppu);
        fallbackSprite.name = "GhostFallbackSquare";

        // Set sprite size to exactly match collider
        spriteRenderer.sprite = fallbackSprite;
        spriteRenderer.drawMode = SpriteDrawMode.Simple;

        return fallbackSprite;
    }

    private void FixedUpdate()
    {
        if (!hasRecord || frames.Count == 0)
        {
            LastDelta = Vector2.zero;
            LoopedThisFrame = false;
            return;
        }

        float localTime = Mathf.Repeat(Time.time - startedAt, duration);
        LoopedThisFrame = localTime < previousLocalTime;
        previousLocalTime = localTime;

        DeathAnchorReplayFrame frame = Sample(localTime);
        Vector2 nextFoot = anchorFootPosition + frame.footOffset;
        Vector2 nextPosition = nextFoot + Vector2.up * playerHeight * 0.5f;
        LastDelta = LoopedThisFrame ? Vector2.zero : nextPosition - rb.position;
        rb.position = nextPosition;

        // Sync sprite facing
        SyncVisual(frame);

        PlaceOnPlayerIfNeeded();
    }

    private void Update()
    {
        if (!hasRecord || !gameObject.activeInHierarchy) return;

        // Spawn afterimage at interval
        afterimageTimer += Time.deltaTime;
        if (afterimageTimer >= afterimageInterval && spriteRenderer != null && spriteRenderer.sprite != null)
        {
            afterimageTimer = 0f;
            SpawnAfterimage();
        }

        // Update existing afterimages
        UpdateAfterimages();
    }

    private void OnDisable()
    {
        ClearAfterimages();
    }

    private void OnDestroy()
    {
        ClearAfterimages();
        if (afterimageMaterial != null) Destroy(afterimageMaterial);
        if (fallbackSprite != null)
        {
            if (fallbackSprite.texture != null) Destroy(fallbackSprite.texture);
            Destroy(fallbackSprite);
        }
        if (afterimageContainer != null) Destroy(afterimageContainer.gameObject);
    }

    public void Configure(float width, float height, LayerMask playerMask)
    {
        if (box == null)
        {
            box = GetComponent<BoxCollider2D>();
        }

        float colliderSide = Mathf.Max(0.05f, Mathf.Min(width, height));
        box.size = Vector2.one * colliderSide;
        box.offset = Vector2.zero;
        playerHeight = colliderSide;
        this.playerMask = playerMask;
    }

    public void Play(Vector2 anchorFootPosition, IReadOnlyList<DeathAnchorReplayFrame> sourceFrames)
    {
        this.anchorFootPosition = anchorFootPosition;
        frames.Clear();
        frames.AddRange(sourceFrames);
        duration = Mathf.Max(0.1f, frames[frames.Count - 1].time);
        startedAt = Time.time;
        previousLocalTime = 0f;
        hasRecord = true;
        gameObject.SetActive(true);

        DeathAnchorReplayFrame first = frames[0];
        rb.position = anchorFootPosition + first.footOffset + Vector2.up * playerHeight * 0.5f;

        // Initialize afterimage state
        afterimageTimer = 0f;
        lastFacing = first.facing;
        SyncVisual(first);
    }

    private DeathAnchorReplayFrame Sample(float time)
    {
        if (frames.Count == 1 || time <= frames[0].time)
        {
            return frames[0];
        }

        for (int i = 1; i < frames.Count; i++)
        {
            if (time > frames[i].time)
            {
                continue;
            }

            DeathAnchorReplayFrame previous = frames[i - 1];
            DeathAnchorReplayFrame next = frames[i];
            float t = Mathf.InverseLerp(previous.time, next.time, time);
            return new DeathAnchorReplayFrame(
                time,
                Vector2.Lerp(previous.footOffset, next.footOffset, t),
                t < 0.5f ? previous.facing : next.facing);
        }

        return frames[frames.Count - 1];
    }

    private void PlaceOnPlayerIfNeeded()
    {
        Collider2D overlap = Physics2D.OverlapBox(rb.position, box.size, 0f, playerMask);
        if (overlap == null)
        {
            return;
        }

        Bounds playerBounds = overlap.bounds;
        Bounds ghostBounds = box.bounds;
        if (LastDelta.y <= 0f && ghostBounds.min.y >= playerBounds.center.y)
        {
            rb.position = new Vector2(rb.position.x, playerBounds.max.y + box.size.y * 0.5f);
            LastDelta = Vector2.zero;
        }
    }

    // ==================== Afterimage ====================

    private void SyncVisual(DeathAnchorReplayFrame frame)
    {
        if (spriteRenderer == null) return;
        spriteRenderer.flipX = frame.facing < 0;
        lastFacing = frame.facing;
    }

    private void SpawnAfterimage()
    {
        if (afterimagePool.Count >= afterimageMaxCount)
        {
            DestroyOldestAfterimage();
        }

        GameObject go = new GameObject("Afterimage");
        go.transform.SetParent(afterimageContainer);
        go.transform.position = transform.position;
        go.transform.rotation = transform.rotation;
        go.transform.localScale = transform.localScale;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = spriteRenderer.sprite;
        sr.flipX = lastFacing < 0;
        sr.sortingLayerID = spriteRenderer.sortingLayerID;
        sr.sortingOrder = spriteRenderer.sortingOrder + afterimageSortingOffset;

        Material mat = new Material(afterimageMaterial);
        mat.color = afterimageColor;
        sr.material = mat;

        AfterimageData data = go.AddComponent<AfterimageData>();
        data.birthTime = Time.time;
        data.lifeCycle = afterimageLifeCycle;
        data.startScale = transform.localScale.x;
        data.baseColor = afterimageColor;
        data.scaleDown = afterimageScaleDown;
        data.fadeToDark = afterimageFadeToDark;

        afterimagePool.Add(go);
    }

    private void UpdateAfterimages()
    {
        float now = Time.time;
        for (int i = afterimagePool.Count - 1; i >= 0; i--)
        {
            GameObject go = afterimagePool[i];
            if (go == null)
            {
                afterimagePool.RemoveAt(i);
                continue;
            }

            AfterimageData data = go.GetComponent<AfterimageData>();
            if (data == null)
            {
                Destroy(go);
                afterimagePool.RemoveAt(i);
                continue;
            }

            float progress = Mathf.Clamp01((now - data.birthTime) / data.lifeCycle);
            if (progress >= 1f)
            {
                Destroy(go);
                afterimagePool.RemoveAt(i);
                continue;
            }

            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            if (sr == null) continue;

            Color c = data.baseColor;
            c.a *= (1f - progress);
            if (data.fadeToDark)
            {
                c.r *= (1f - progress * 0.4f);
                c.g *= (1f - progress * 0.4f);
                c.b *= (1f - progress * 0.4f);
            }
            sr.color = c;

            if (data.scaleDown)
            {
                float s = Mathf.Lerp(data.startScale, data.startScale * 0.3f, progress);
                go.transform.localScale = new Vector3(s, s, 1f);
            }
        }
    }

    private void DestroyOldestAfterimage()
    {
        if (afterimagePool.Count == 0) return;
        GameObject oldest = afterimagePool[0];
        afterimagePool.RemoveAt(0);
        if (oldest != null) Destroy(oldest);
    }

    public void ClearAfterimages()
    {
        for (int i = afterimagePool.Count - 1; i >= 0; i--)
        {
            if (afterimagePool[i] != null) Destroy(afterimagePool[i]);
        }
        afterimagePool.Clear();
    }

    /// <summary>运行时调整残影颜色</summary>
    public void SetAfterimageColor(Color color) { afterimageColor = color; }

    /// <summary>运行时调整残影密度（越大越密集）</summary>
    public void SetAfterimageDensity(float density)
    {
        afterimageInterval = Mathf.Max(0.02f, 0.2f / Mathf.Max(1f, density));
    }

    /// <summary>运行时调整残影长度</summary>
    public void SetAfterimageLength(float length)
    {
        afterimageLifeCycle = Mathf.Max(0.1f, length);
    }
}


/// <summary>
/// 残影数据组件 - 附加到每个残影 GameObject 上
/// </summary>
public class AfterimageData : MonoBehaviour
{
    public float birthTime;
    public float lifeCycle;
    public float startScale;
    public Color baseColor;
    public bool scaleDown;
    public bool fadeToDark;
}

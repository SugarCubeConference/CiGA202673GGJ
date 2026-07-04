using UnityEngine;
using UnityEngine.UI;

public sealed class AnchorCountdownHud : MonoBehaviour
{
    private const int BlockCount = 5;
    private const float TickPulseDuration = 0.24f;

    private static readonly Color ActiveColor = new Color(0.96f, 0.79f, 0.36f, 1f);
    private static readonly Color ActiveAltColor = new Color(0.71f, 0.61f, 1f, 1f);
    private static readonly Color EmptyColor = new Color(0.27f, 0.29f, 0.31f, 0.92f);
    private static readonly Color FrameColor = new Color(0.08f, 0.1f, 0.13f, 0.82f);
    private static readonly Color FrameOutlineColor = new Color(0.18f, 0.22f, 0.27f, 1f);
    private static readonly Color TickFlashColor = new Color(1f, 0.39f, 0.43f, 1f);

    [Header("Legacy UI References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Text titleText;
    [SerializeField] private Text timeText;
    [SerializeField] private Text hintText;
    [SerializeField] private Image progressFill;
    [SerializeField] private Image progressGlow;
    [SerializeField] private Image[] blocks;

    [Header("World Countdown")]
    [SerializeField] private Sprite blockSprite;
    [SerializeField] private Vector2 blockSize = new Vector2(0.88f, 0.56f);
    [SerializeField] private float blockSpacing = 0.22f;
    [SerializeField] private float topMargin = 0.95f;
    [SerializeField] private float sidePadding = 0.42f;
    [SerializeField] private int sortingOrder = 260;

    private readonly float[] tickPulseTimers = new float[BlockCount];
    private readonly SpriteRenderer[] worldBlocks = new SpriteRenderer[BlockCount];

    private float totalDuration = 5f;
    private int visibleBlocks = BlockCount;
    private int lastVisibleBlocks = -1;
    private bool shouldPlayTicks;
    private bool isVisible;

    private Transform worldRoot;
    private SpriteRenderer frameFill;
    private SpriteRenderer frameOutline;
    private Camera targetCamera;
    private Sprite generatedSprite;

    private void Awake()
    {
        DisableLegacyUi();
        EnsureWorldVisuals();
        SetWorldVisible(false);
    }

    private void LateUpdate()
    {
        DisableLegacyUi();
        EnsureWorldVisuals();
        UpdateWorldPlacement();
        AnimateBlocks();
    }

    public void Configure(float durationSeconds)
    {
        totalDuration = Mathf.Max(0.01f, durationSeconds);
        shouldPlayTicks = false;
        lastVisibleBlocks = -1;
        DisableLegacyUi();
        EnsureWorldVisuals();
        SetRemaining(totalDuration);
        SetWorldVisible(isVisible);
    }

    public void SetVisible(bool visible)
    {
        isVisible = visible;
        shouldPlayTicks = visible;
        DisableLegacyUi();
        EnsureWorldVisuals();

        if (visible)
        {
            lastVisibleBlocks = -1;
            SetRemaining(totalDuration);
        }

        SetWorldVisible(visible);
    }

    public void SetRemaining(float remainingSeconds)
    {
        DisableLegacyUi();
        EnsureWorldVisuals();

        float clamped = Mathf.Clamp(remainingSeconds, 0f, totalDuration);
        float secondsPerBlock = totalDuration / BlockCount;
        visibleBlocks = Mathf.Clamp(Mathf.CeilToInt(clamped / secondsPerBlock), 0, BlockCount);

        if (lastVisibleBlocks >= 0 && visibleBlocks < lastVisibleBlocks)
        {
            int removedBlockIndex = Mathf.Clamp(visibleBlocks, 0, BlockCount - 1);
            tickPulseTimers[removedBlockIndex] = TickPulseDuration;
            if (shouldPlayTicks)
            {
                DeathAnchorWwiseAudio.Post(gameObject, DeathAnchorWwiseEvents.AnchorTick);
            }
        }

        lastVisibleBlocks = visibleBlocks;
        PaintBlocks();
    }

    private void DisableLegacyUi()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        SetLegacyGraphicVisible(titleText, false);
        SetLegacyGraphicVisible(timeText, false);
        SetLegacyGraphicVisible(hintText, false);
        SetLegacyGraphicVisible(progressFill, false);
        SetLegacyGraphicVisible(progressGlow, false);

        if (blocks != null)
        {
            for (int i = 0; i < blocks.Length; i++)
            {
                SetLegacyGraphicVisible(blocks[i], false);
            }
        }
    }

    private bool EnsureWorldVisuals()
    {
        if (worldRoot != null)
        {
            return true;
        }

        targetCamera = FindTargetCamera();
        if (targetCamera == null)
        {
            return false;
        }

        Transform existingRoot = targetCamera.transform.Find("Anchor Countdown World Root");
        if (existingRoot != null)
        {
            worldRoot = existingRoot;
        }
        else
        {
            worldRoot = new GameObject("Anchor Countdown World Root").transform;
            worldRoot.SetParent(targetCamera.transform, false);
        }

        worldRoot.localRotation = Quaternion.identity;
        worldRoot.localScale = Vector3.one;

        EnsureFrame();
        EnsureBlockRenderers();
        UpdateWorldPlacement();
        PaintBlocks();
        return true;
    }

    private Camera FindTargetCamera()
    {
        if (targetCamera != null)
        {
            return targetCamera;
        }

        targetCamera = GetComponentInParent<Camera>();
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        return targetCamera;
    }

    private void EnsureFrame()
    {
        Transform frameFillTransform = worldRoot.Find("Frame Fill");
        if (frameFillTransform == null)
        {
            frameFillTransform = new GameObject("Frame Fill").transform;
            frameFillTransform.SetParent(worldRoot, false);
        }

        frameFill = GetOrAddSpriteRenderer(frameFillTransform.gameObject);
        frameFill.sprite = GetBlockSprite();
        frameFill.color = FrameColor;
        frameFill.sortingOrder = sortingOrder;

        Transform frameOutlineTransform = worldRoot.Find("Frame Outline");
        if (frameOutlineTransform == null)
        {
            frameOutlineTransform = new GameObject("Frame Outline").transform;
            frameOutlineTransform.SetParent(worldRoot, false);
        }

        frameOutline = GetOrAddSpriteRenderer(frameOutlineTransform.gameObject);
        frameOutline.sprite = GetBlockSprite();
        frameOutline.color = FrameOutlineColor;
        frameOutline.sortingOrder = sortingOrder - 1;

        float totalWidth = (blockSize.x * BlockCount) + (blockSpacing * (BlockCount - 1)) + (sidePadding * 2f);
        frameOutline.transform.localPosition = new Vector3(0f, 0f, 0.02f);
        frameOutline.transform.localScale = new Vector3(totalWidth, blockSize.y + 0.34f, 1f);
        frameFill.transform.localPosition = Vector3.zero;
        frameFill.transform.localScale = new Vector3(totalWidth - 0.12f, blockSize.y + 0.22f, 1f);
    }

    private void EnsureBlockRenderers()
    {
        float totalWidth = (blockSize.x * BlockCount) + (blockSpacing * (BlockCount - 1));
        float startX = -totalWidth * 0.5f + blockSize.x * 0.5f;

        for (int i = 0; i < BlockCount; i++)
        {
            Transform blockTransform = worldRoot.Find($"Block {i + 1}");
            if (blockTransform == null)
            {
                blockTransform = new GameObject($"Block {i + 1}").transform;
                blockTransform.SetParent(worldRoot, false);
            }

            SpriteRenderer spriteRenderer = GetOrAddSpriteRenderer(blockTransform.gameObject);
            spriteRenderer.sprite = GetBlockSprite();
            spriteRenderer.sortingOrder = sortingOrder + 1;
            spriteRenderer.transform.localPosition = new Vector3(startX + i * (blockSize.x + blockSpacing), 0f, -0.02f);
            spriteRenderer.transform.localScale = new Vector3(blockSize.x, blockSize.y, 1f);
            worldBlocks[i] = spriteRenderer;
        }
    }

    private void UpdateWorldPlacement()
    {
        if (worldRoot == null || targetCamera == null)
        {
            return;
        }

        float yOffset = targetCamera.orthographicSize - topMargin;
        worldRoot.localPosition = new Vector3(0f, yOffset, 9f);
    }

    private void PaintBlocks()
    {
        for (int i = 0; i < BlockCount; i++)
        {
            SpriteRenderer block = worldBlocks[i];
            if (block == null)
            {
                continue;
            }

            bool active = i < visibleBlocks;
            Color activeColor = Color.Lerp(ActiveColor, ActiveAltColor, i / (float)(BlockCount - 1));
            block.color = active ? activeColor : EmptyColor;
        }
    }

    private void AnimateBlocks()
    {
        for (int i = 0; i < BlockCount; i++)
        {
            SpriteRenderer block = worldBlocks[i];
            if (block == null)
            {
                continue;
            }

            float pulse = 0f;
            if (tickPulseTimers[i] > 0f)
            {
                tickPulseTimers[i] = Mathf.Max(0f, tickPulseTimers[i] - Time.unscaledDeltaTime);
                pulse = Mathf.Sin((tickPulseTimers[i] / TickPulseDuration) * Mathf.PI);
                block.color = Color.Lerp(EmptyColor, TickFlashColor, pulse);
            }

            bool active = i < visibleBlocks;
            float idlePulse = active ? 0.045f * Mathf.Sin(Time.unscaledTime * 5.5f + i * 0.45f) : 0f;
            float scale = 1f + idlePulse + pulse * 0.22f;
            block.transform.localScale = new Vector3(blockSize.x * scale, blockSize.y * scale, 1f);
        }
    }

    private void SetWorldVisible(bool visible)
    {
        if (EnsureWorldVisuals())
        {
            worldRoot.gameObject.SetActive(visible);
        }
    }

    private Sprite GetBlockSprite()
    {
        if (blockSprite != null)
        {
            return blockSprite;
        }

        if (generatedSprite == null)
        {
            Texture2D texture = new Texture2D(8, 8, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[64];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.white;
            }

            texture.SetPixels(pixels);
            texture.Apply();
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            generatedSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), texture.width);
            generatedSprite.name = "AnchorCountdownRuntimeSprite";
        }

        return generatedSprite;
    }

    private static void SetLegacyGraphicVisible(Graphic graphic, bool visible)
    {
        if (graphic != null)
        {
            graphic.gameObject.SetActive(visible);
        }
    }

    private static SpriteRenderer GetOrAddSpriteRenderer(GameObject target)
    {
        SpriteRenderer spriteRenderer = target.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = target.AddComponent<SpriteRenderer>();
        }

        return spriteRenderer;
    }
}

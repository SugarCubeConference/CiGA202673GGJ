using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Death Anchor 关卡烘焙器——将 .level.json 关卡描述文件程序化生成完整的 Unity 场景。
/// </summary>
public static class DeathAnchorLevelBaker
{
    private const float PixelsPerUnit = 100f;
    private const string PrototypeLevelsDirectory = "Tools/DeathAnchorLevelEditor/level";
    private const string OutputSceneDirectory = "Assets/Scenes/DeathAnchor";
    private const string ArtDirectory = "Assets/Art/DeathAnchor";
    private const string BackgroundSpritePath = "Assets/Art/Background.png";
    private const string PlayerPhysicsConfigPath = "Assets/StreamingAssets/DeathAnchor/player-physics.json";
    private const string BakedRootName = "BakedLevelRoot";

        // ===== 颜色常量 =====
private static readonly Color PlatformColor = new Color(0.39f, 0.45f, 0.52f, 1f);
    private static readonly Color MovingPlatformColor = new Color(0.55f, 0.82f, 0.49f, 1f);
    private static readonly Color SpikeColor = new Color(1f, 0.27f, 0.34f, 1f);
    private static readonly Color PlayerColor = new Color(1f, 0.72f, 0.26f, 1f);
    private static readonly Color GhostColor = new Color(0.18f, 0.82f, 1f, 0.55f);
    private static readonly Color GoalColor = new Color(0.3f, 1f, 0.58f, 1f);
    private static readonly Color PlayerLightColor = new Color(1f, 0.8f, 0.2f, 1f);
    private static readonly Color GhostLightColor = new Color(0.2f, 0.8f, 1f, 1f);
    private static readonly Color ExitLightColor = new Color(0.1f, 1f, 0.4f, 1f);
    private static readonly Color GlobalLightColor = new Color(0.62f, 0.64f, 0.62f, 1f);
    private static readonly Color KeyColor = new Color(1f, 0.78f, 0.18f, 1f);
    private static readonly Color DoorColor = new Color(1f, 0.45f, 0.25f, 1f);
    private static readonly Color AnchorZoneColor = new Color(0.68f, 0.55f, 1f, 0.2f);

        // ===== 菜单入口 =====

    /// <summary>菜单：烘焙选中的 JSON 或手动选择 JSON 文件</summary>
[MenuItem("Tools/Death Anchor/Bake Selected Level Json")]
    public static void BakeSelectedLevelJson()
    {
        Object selected = Selection.activeObject;
        string assetPath = selected != null ? AssetDatabase.GetAssetPath(selected) : string.Empty;
        if (string.IsNullOrEmpty(assetPath) || !assetPath.EndsWith(".level.json"))
        {
            string pickedPath = EditorUtility.OpenFilePanel("Select Death Anchor level json", PrototypeLevelsDirectory, "json");
            if (string.IsNullOrEmpty(pickedPath))
            {
                return;
            }

            assetPath = pickedPath;
        }

        DeathAnchorLevelData level = ReadLevel(assetPath);
        string scenePath = $"{OutputSceneDirectory}/{SafeFileName(level.title)}.unity";
        BakeLevel(assetPath, scenePath);
    }

        /// <summary>菜单：批量烘焙原型目录中所有关卡</summary>
    [MenuItem("Tools/Death Anchor/Bake All Prototype Levels")]
    public static void BakeAllPrototypeLevels()
    {
        DeleteExistingBakedScenes(OutputSceneDirectory);
        BakeDirectory(ToProjectAbsolutePath(PrototypeLevelsDirectory), OutputSceneDirectory);
    }

        /// <summary>批量烘焙指定目录中的所有 .level.json 文件</summary>
public static void BakeDirectory(string sourceDirectory, string outputSceneDirectory)
    {
        EnsureFolder(outputSceneDirectory);
        string[] files = Directory.GetFiles(sourceDirectory, "*.json", SearchOption.TopDirectoryOnly);
        files = System.Array.FindAll(files, IsSupportedLevelJsonPath);
        System.Array.Sort(files, CompareLevelJsonPaths);
        List<string> scenePaths = new List<string>();

        for (int i = 0; i < files.Length; i++)
        {
            DeathAnchorLevelData level = ReadLevel(files[i]);
            string scenePath = $"{outputSceneDirectory}/{SafeFileName(level.title)}.unity";
            BakeLevel(files[i], scenePath);
            scenePaths.Add(scenePath);
        }

        ReplaceScenesInBuildSettings(outputSceneDirectory, scenePaths);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Baked {files.Length} Death Anchor level scene(s).");
    }

        /// <summary>
    /// 核心：将一个 .level.json 文件烘焙为完整的 Unity 场景。
    /// 包括环境、机关、角色、GameManager、相机、灯光和 UI。
    /// </summary>
public static void BakeLevel(string jsonPath, string scenePath)
    {
        EnsureFolder(Path.GetDirectoryName(scenePath).Replace("\\", "/"));
        EnsureFolder(ArtDirectory);

        DeathAnchorLevelData level = ReadLevel(jsonPath);
        Sprite squareSprite = EnsureSquareSprite();

        Scene scene = File.Exists(scenePath)
            ? EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single)
            : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        ClearBakedRoot(scene);

        GameObject root = new GameObject(BakedRootName);
        LevelMetadata metadata = root.AddComponent<LevelMetadata>();
        metadata.Initialize(Path.GetFileName(jsonPath), level);

        int groundLayer = LayerOrDefault("Ground");
        int playerLayer = LayerOrDefault("Player");
        int ghostLayer = LayerOrDefault("Ghost");
        int hazardLayer = LayerOrDefault("Hazard");
        int interactableLayer = LayerOrDefault("Interactable");

        Dictionary<string, LinkedBridge> bridgesById = new Dictionary<string, LinkedBridge>();
        Dictionary<string, MovingPlatform2D> movingPlatformsById = new Dictionary<string, MovingPlatform2D>();

        Transform environmentRoot = CreateChild(root.transform, "Environment");
        Transform interactableRoot = CreateChild(root.transform, "Interactables");
        Transform actorRoot = CreateChild(root.transform, "Actors");

        BakeObjects(level.platforms, environmentRoot, squareSprite, PlatformColor, groundLayer, AddStaticSolid);
        BakeMovingPlatforms(level.movingPlatforms, environmentRoot, squareSprite, groundLayer, movingPlatformsById);
        BakeBridges(level.bridges, environmentRoot, squareSprite, groundLayer, bridgesById);
        BakeSpikes(level.spikes, environmentRoot, squareSprite, hazardLayer);
        BakeLasers(level.lasers, environmentRoot, interactableLayer, Mask("Ground"), movingPlatformsById);
        BakeAnchorZones(level.anchorZones, environmentRoot, squareSprite, interactableLayer);

        Transform spawnPoint = new GameObject("SpawnPoint").transform;
        spawnPoint.SetParent(root.transform);
        spawnPoint.position = FootPosition(level.spawn);

        GameObject managerObject = new GameObject("GameManager");
        managerObject.transform.SetParent(root.transform);
        DeathAnchorGameManager manager = managerObject.AddComponent<DeathAnchorGameManager>();

        GameObject player = CreateActor("Player", actorRoot, squareSprite, PlayerColor, playerLayer, level, false);
        DeathAnchorPlayerController playerController = player.AddComponent<DeathAnchorPlayerController>();
        Vector2 playerColliderSize = PlayerColliderSize(level);
        playerController.Configure(
            playerColliderSize.x,
            playerColliderSize.y,
            Mask("Ground", "Ghost"),
            level.rules == null || level.rules.playerWallSlide,
            WallSlideUnits(level));
        ApplyPlayerPhysics(level, playerController);
        player.AddComponent<AkGameObj>();
        player.AddComponent<PlayerWwiseAudio>();
        player.AddComponent<PlayerDustParticles>();
        player.transform.position = spawnPoint.position + Vector3.up * (playerColliderSize.y * 0.5f);
        playerController.SpawnAtFootPosition(spawnPoint.position);

        GameObject ghost = CreateActor("Ghost", actorRoot, squareSprite, GhostColor, ghostLayer, level, true);
        GhostReplayController ghostReplay = ghost.AddComponent<GhostReplayController>();
        ghostReplay.Configure(playerColliderSize.x, playerColliderSize.y, Mask("Player"));
        ghost.SetActive(false);

        GameObject anchorMarker = CreateBlock("AnchorMarker", root.transform, spawnPoint.position, new Vector2(0.35f, 0.08f), squareSprite, new Color(0.75f, 0.55f, 1f, 0.9f), interactableLayer);
        Object.DestroyImmediate(anchorMarker.GetComponent<Collider2D>());
        anchorMarker.SetActive(false);

        Camera camera = CreateCamera(root.transform, player.transform, level);
        AnchorCountdownHud countdownHud = CreateCountdownUi(camera.transform, squareSprite);
        manager.Configure(level.rules != null ? level.rules.recordWindowSec : 5f, playerController, ghostReplay, spawnPoint, anchorMarker.transform, countdownHud, null);

        BakeKeys(level.keys, interactableRoot, squareSprite, interactableLayer);
        BakeDoors(level.doors, interactableRoot, squareSprite, groundLayer);
        BakeButtons(level.buttons, level.bridges, level.movingPlatforms, interactableRoot, squareSprite, interactableLayer, bridgesById, movingPlatformsById);
        BakeGoals(level.goals, interactableRoot, squareSprite, interactableLayer);
        BakeNotes(level.notes, interactableRoot);

        CreateLight(root.transform);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, scenePath);
    }

        // ===== JSON 读取 =====

    /// <summary>从 JSON 文件读取并解析为关卡数据结构</summary>
private static DeathAnchorLevelData ReadLevel(string jsonPath)
    {
        string json = File.ReadAllText(jsonPath);
        DeathAnchorLevelData level = JsonUtility.FromJson<DeathAnchorLevelData>(json);
        if (level == null)
        {
            throw new InvalidDataException($"Could not parse level json: {jsonPath}");
        }

        if (level.rules == null)
        {
            level.rules = new DeathAnchorRules();
        }

        if (level.world == null)
        {
            level.world = new DeathAnchorWorld();
        }

        return level;
    }

        // ===== 各类物件烘焙方法 =====

    /// <summary>烘焙普通物件（平台等），支持自定义后处理回调</summary>
private static void BakeObjects(DeathAnchorLevelObject[] objects, Transform parent, Sprite sprite, Color color, int layer, System.Action<GameObject> configure)
    {
        if (objects == null)
        {
            return;
        }

        for (int i = 0; i < objects.Length; i++)
        {
            GameObject go = CreateBlock(objects[i].id, parent, Center(objects[i]), Size(objects[i]), sprite, color, layer);
            go.transform.rotation = Quaternion.Euler(0f, 0f, -objects[i].rotation);
            configure(go);
        }
    }

        /// <summary>烘焙移动平台</summary>
private static void BakeMovingPlatforms(DeathAnchorLevelObject[] objects, Transform parent, Sprite sprite, int layer, Dictionary<string, MovingPlatform2D> movingPlatformsById)
    {
        if (objects == null)
        {
            return;
        }

        for (int i = 0; i < objects.Length; i++)
        {
            DeathAnchorLevelObject item = objects[i];
            GameObject go = CreateBlock(item.id, parent, Center(item), Size(item), sprite, MovingPlatformColor, layer);
            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            MovingPlatform2D platform = go.AddComponent<MovingPlatform2D>();
            Vector2 target = item.moveTargetX != 0f || item.moveTargetY != 0f
                ? Point(item.moveTargetX + item.w * 0.5f, item.moveTargetY + item.h * 0.5f)
                : Center(item) + Vector2.right * 2f;
            string moveMode = string.IsNullOrEmpty(item.motionMode) ? item.mode : item.motionMode;
            platform.Configure(target, item.periodSec > 0f ? item.periodSec : 3f, item.moveSpeed > 0f ? item.moveSpeed : 3f, moveMode);
            if (!string.IsNullOrEmpty(item.id))
            {
                movingPlatformsById[item.id] = platform;
            }
        }
    }

        /// <summary>烘焙虚实桥（按钮控制）</summary>
private static void BakeBridges(DeathAnchorLevelObject[] objects, Transform parent, Sprite sprite, int layer, Dictionary<string, LinkedBridge> bridgesById)
    {
        if (objects == null)
        {
            return;
        }

        for (int i = 0; i < objects.Length; i++)
        {
            DeathAnchorLevelObject item = objects[i];
            GameObject go = CreateBlock(item.id, parent, Center(item), Size(item), sprite, new Color(0.35f, 0.83f, 0.95f, 1f), layer);
            AddStaticSolid(go);
            LinkedBridge bridge = go.AddComponent<LinkedBridge>();
            bridge.Configure(item.id, item.defaultState, item.activeState);
            bridgesById[item.id] = bridge;
        }
    }

        /// <summary>烘焙陷阱（HazardTrigger）</summary>
private static void BakeSpikes(DeathAnchorLevelObject[] objects, Transform parent, Sprite sprite, int layer)
    {
        if (objects == null)
        {
            return;
        }

        for (int i = 0; i < objects.Length; i++)
        {
            DeathAnchorLevelObject item = objects[i];
            GameObject go = CreateBlock(item.id, parent, Center(item), Size(item), sprite, SpikeColor, layer);
            go.transform.rotation = Quaternion.Euler(0f, 0f, -item.rotation);
            go.AddComponent<HazardTrigger>();
        }
    }

        /// <summary>烘焙激光</summary>
private static void BakeLasers(DeathAnchorLevelObject[] objects, Transform parent, int layer, LayerMask blockMask, Dictionary<string, MovingPlatform2D> movingPlatformsById)
    {
        if (objects == null)
        {
            return;
        }

        for (int i = 0; i < objects.Length; i++)
        {
            DeathAnchorLevelObject item = objects[i];
            GameObject go = new GameObject(string.IsNullOrEmpty(item.id) ? "Laser" : item.id);
            Transform laserParent = parent;
            Collider2D ignoredBlocker = null;
            if (!string.IsNullOrEmpty(item.attachedTo) && movingPlatformsById != null && movingPlatformsById.TryGetValue(item.attachedTo, out MovingPlatform2D attachedPlatform))
            {
                laserParent = attachedPlatform.transform;
                ignoredBlocker = attachedPlatform.GetComponent<Collider2D>();
            }

            go.transform.SetParent(laserParent);
            go.transform.position = LaserOrigin(item);
            go.layer = layer;

            LineRenderer line = go.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.useWorldSpace = true;
            line.sortingOrder = 20;
            line.material = new Material(Shader.Find("Sprites/Default"));

            LaserBeam laser = go.AddComponent<LaserBeam>();
            Vector2 direction = LaserDirection(item);
            float distance = item.maxDistance > 0f ? item.maxDistance / PixelsPerUnit : LaserDistance(item);
            laser.Configure(direction, distance, blockMask, ColorFromRgb(item.beamColor, new Color(1f, 0.15f, 0.15f, 0.9f)), ignoredBlocker);
        }
    }

        /// <summary>烘焙锚点区域（纯触发器，无碰撞）</summary>
private static void BakeAnchorZones(DeathAnchorLevelObject[] objects, Transform parent, Sprite sprite, int layer)
    {
        if (objects == null)
        {
            return;
        }

        for (int i = 0; i < objects.Length; i++)
        {
            GameObject go = CreateBlock(objects[i].id, parent, Center(objects[i]), Size(objects[i]), sprite, AnchorZoneColor, layer);
            go.GetComponent<Collider2D>().isTrigger = true;
        }
    }

        /// <summary>烘焙钥匙</summary>
private static void BakeKeys(DeathAnchorLevelObject[] objects, Transform parent, Sprite sprite, int layer)
    {
        if (objects == null)
        {
            return;
        }

        for (int i = 0; i < objects.Length; i++)
        {
            DeathAnchorLevelObject item = objects[i];
            GameObject go = CreateBlock(item.id, parent, Center(item), Size(item), sprite, KeyColor, layer);
            KeyPickup key = go.AddComponent<KeyPickup>();
            key.Configure(item.id);
        }
    }

        /// <summary>烘焙钥匙门</summary>
private static void BakeDoors(DeathAnchorLevelObject[] objects, Transform parent, Sprite sprite, int layer)
    {
        if (objects == null)
        {
            return;
        }

        for (int i = 0; i < objects.Length; i++)
        {
            DeathAnchorLevelObject item = objects[i];
            GameObject go = CreateBlock(item.id, parent, Center(item), Size(item), sprite, DoorColor, layer);
            AddStaticSolid(go);
            BoxCollider2D trigger = go.AddComponent<BoxCollider2D>();
            trigger.size = Vector2.one * 1.08f;
            trigger.isTrigger = true;
            KeyDoor door = go.AddComponent<KeyDoor>();
            door.Configure(item.requiredKey);
        }
    }

        /// <summary>烘焙按钮开关（关联桥梁）</summary>
private static void BakeButtons(DeathAnchorLevelObject[] objects, DeathAnchorLevelObject[] bridges, DeathAnchorLevelObject[] movingPlatforms, Transform parent, Sprite sprite, int layer, Dictionary<string, LinkedBridge> bridgesById, Dictionary<string, MovingPlatform2D> movingPlatformsById)
    {
        if (objects == null)
        {
            return;
        }

        for (int i = 0; i < objects.Length; i++)
        {
            DeathAnchorLevelObject item = objects[i];
            GameObject go = CreateBlock(item.id, parent, Center(item), Size(item), sprite, KeyColor, layer);
            List<LinkedBridge> linked = new List<LinkedBridge>();
            List<MovingPlatform2D> linkedPlatforms = new List<MovingPlatform2D>();
            if (item.links != null)
            {
                for (int j = 0; j < item.links.Length; j++)
                {
                    if (bridgesById.TryGetValue(item.links[j], out LinkedBridge bridge))
                    {
                        linked.Add(bridge);
                    }

                    if (movingPlatformsById.TryGetValue(item.links[j], out MovingPlatform2D platform))
                    {
                        linkedPlatforms.Add(platform);
                    }
                }
            }

            AddRequiredButtonLinks(item, bridges, movingPlatforms, bridgesById, movingPlatformsById, linked, linkedPlatforms);

            ButtonSwitch button = go.AddComponent<ButtonSwitch>();
            button.Configure(item.id, item.pressedBy, linked.ToArray(), linkedPlatforms.ToArray());
        }
    }

        /// <summary>烘焙终点触发器</summary>
private static void BakeGoals(DeathAnchorLevelObject[] objects, Transform parent, Sprite sprite, int layer)
    {
        if (objects == null)
        {
            return;
        }

        for (int i = 0; i < objects.Length; i++)
        {
            GameObject go = CreateBlock(objects[i].id, parent, Center(objects[i]), Size(objects[i]), sprite, GoalColor, layer);
            ApplyLitShader(go, "Custom/ExitLit");
            AddPointLight2D(go, "ExitLight", ExitLightColor, 3.5f, 2f, 5f, 0.5f);
            go.AddComponent<DeathAnchorGoalTrigger>();
        }
    }

    private static void BakeNotes(DeathAnchorLevelObject[] objects, Transform parent)
    {
        if (objects == null)
        {
            return;
        }

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        for (int i = 0; i < objects.Length; i++)
        {
            DeathAnchorLevelObject item = objects[i];
            string textValue = !string.IsNullOrWhiteSpace(item.notes) ? item.notes : item.label;
            if (string.IsNullOrWhiteSpace(textValue))
            {
                continue;
            }

            GameObject noteObject = new GameObject(string.IsNullOrEmpty(item.id) ? "Runtime Note" : item.id);
            noteObject.transform.SetParent(parent);
            noteObject.transform.position = Center(item);
            noteObject.transform.rotation = Quaternion.Euler(0f, 0f, -item.rotation);
            noteObject.transform.localScale = new Vector3(1f / PixelsPerUnit, 1f / PixelsPerUnit, 1f);

            Canvas canvas = noteObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 30;

            RectTransform panelRect = noteObject.GetComponent<RectTransform>();
            panelRect.sizeDelta = new Vector2(Mathf.Max(32f, item.w), Mathf.Max(32f, item.h));

            Image background = noteObject.AddComponent<Image>();
            background.color = new Color(0.04f, 0.05f, 0.07f, 0.78f);
            background.raycastTarget = false;

            GameObject textObject = new GameObject("Text");
            textObject.transform.SetParent(noteObject.transform, false);
            Text text = textObject.AddComponent<Text>();
            text.text = textValue;
            text.alignment = TextAnchor.MiddleCenter;
            text.font = font;
            text.fontSize = 20;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 10;
            text.resizeTextMaxSize = 24;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.color = new Color(0.96f, 0.93f, 0.86f, 1f);
            text.raycastTarget = false;

            RectTransform textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(8f, 6f);
            textRect.offsetMax = new Vector2(-8f, -6f);
        }
    }

        // ===== 角色和物块创建 =====

    /// <summary>创建一个角色（玩家或分身）：包含 Rigidbody2D、BoxCollider2D、ActorIdentity 和视觉子物体</summary>
    private static GameObject CreateActor(string name, Transform parent, Sprite sprite, Color color, int layer, DeathAnchorLevelData level, bool ghost)
    {
        Vector2 size = PlayerColliderSize(level);
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent);
        go.layer = layer;

        BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
        collider.size = size;
        collider.offset = Vector2.zero;

        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(go.transform);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localScale = new Vector3(size.x, size.y, 1f);
        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sharedMaterial = CreateLitMaterial(ghost ? "Custom/GhostLit" : "Custom/PlayerLit");

        AddPointLight2D(
            go,
            ghost ? "GhostLight" : "PlayerLight",
            ghost ? GhostLightColor : PlayerLightColor,
            ghost ? 2.5f : 3f,
            ghost ? 1.4f : 0.87f,
            ghost ? 3.5f : 5.47f,
            ghost ? 0.5f : 0.26f);

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.freezeRotation = true;
        ActorIdentity identity = go.AddComponent<ActorIdentity>();
        identity.SetKind(ghost ? DeathAnchorActorKind.Ghost : DeathAnchorActorKind.Player);
        return go;
    }

        /// <summary>创建一个矩形物块（平台、陷阱等）：包含 SpriteRenderer 和 BoxCollider2D</summary>
private static GameObject CreateBlock(string name, Transform parent, Vector2 center, Vector2 size, Sprite sprite, Color color, int layer)
    {
        GameObject go = new GameObject(string.IsNullOrEmpty(name) ? "BakedObject" : name);
        go.transform.SetParent(parent);
        go.transform.position = center;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        go.layer = layer;

        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;

        BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
        collider.size = Vector2.one;
        return go;
    }

        // ===== 着色器和灯光 =====

    /// <summary>应用 Lit 着色器</summary>
private static void ApplyLitShader(GameObject go, string shaderName)
    {
        SpriteRenderer renderer = go.GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            return;
        }

        renderer.sharedMaterial = CreateLitMaterial(shaderName);
    }

        /// <summary>创建 Lit 材质</summary>
private static Material CreateLitMaterial(string shaderName)
    {
        Shader shader = Shader.Find(shaderName);
        if (shader == null)
        {
            Debug.LogWarning($"[DeathAnchorLevelBaker] Shader '{shaderName}' not found. Falling back to default sprite material.");
            return null;
        }

        return new Material(shader);
    }

        /// <summary>添加点光源</summary>
private static Light2D AddPointLight2D(GameObject go, string lightName, Color color, float intensity, float innerRadius, float outerRadius, float falloffIntensity)
    {
        Light2D light = go.AddComponent<Light2D>();
        light.name = lightName;
        light.lightType = Light2D.LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.pointLightInnerRadius = innerRadius;
        light.pointLightOuterRadius = outerRadius;
        light.falloffIntensity = falloffIntensity;
        return light;
    }

        /// <summary>创建空子节点</summary>
private static Transform CreateChild(Transform parent, string name)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent);
        return child.transform;
    }

        /// <summary>将物块设为 Static（用于地面优化）</summary>
private static void AddStaticSolid(GameObject go)
    {
        go.isStatic = true;
    }

        // ===== 背景 =====

    /// <summary>创建背景精灵</summary>
private static void CreateBackground(Camera camera)
    {
        Sprite backgroundSprite = EnsureBackgroundSprite();
        if (backgroundSprite == null)
        {
            Debug.LogWarning($"[DeathAnchorLevelBaker] Background sprite not found at '{BackgroundSpritePath}'.");
            return;
        }

        GameObject background = new GameObject("Background");
        background.transform.SetParent(camera.transform, false);

        SpriteRenderer renderer = background.AddComponent<SpriteRenderer>();
        renderer.sprite = backgroundSprite;
        renderer.color = Color.white;
        renderer.sortingOrder = -100;

        CameraBackgroundFitter fitter = background.AddComponent<CameraBackgroundFitter>();
        fitter.Configure(camera, 10f);
    }

    private static Sprite EnsureBackgroundSprite()
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundSpritePath);
        if (sprite != null)
        {
            return sprite;
        }

        TextureImporter importer = AssetImporter.GetAtPath(BackgroundSpritePath) as TextureImporter;
        if (importer == null)
        {
            return null;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundSpritePath);
    }

        // ===== 相机和灯光 =====

    /// <summary>创建正交相机和跟随脚本</summary>
private static Camera CreateCamera(Transform root, Transform target, DeathAnchorLevelData level)
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.transform.SetParent(root);
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.backgroundColor = new Color(0.22f, 0.23f, 0.23f, 1f);

        // 根据关卡尺寸自动计算摄像机参数
        float worldWidthUnits = level.world.w / PixelsPerUnit;
        float worldHeightUnits = level.world.h / PixelsPerUnit;
        float halfWidth = worldWidthUnits / 2f;
        float halfHeight = worldHeightUnits / 2f;

        // orthographicSize 需要同时覆盖高度和宽度
        // viewWidth = orthoSize * 2 * aspect，所以 fit 宽度需要 orthoSize >= halfWidth / aspect
        float aspect = 16f / 9f;
        camera.orthographicSize = Mathf.Max(halfHeight, halfWidth / aspect);

        // 摄像机关卡中心：X 正向右，Y 像素坐标向下所以取负
        cameraObject.transform.position = new Vector3(halfWidth, -halfHeight, -10f);

        CreateBackground(camera);
        return camera;
    }

        /// <summary>创建方向光</summary>
private static void CreateLight(Transform root)
    {
        GameObject lightObject = new GameObject("GlobalLight2D");
        lightObject.transform.SetParent(root);
        Light2D light = lightObject.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Global;
        light.color = GlobalLightColor;
        light.intensity = 5.84f;
    }

        // ===== UI =====

    /// <summary>创建游戏内倒计时方块，让其参与场景 shader 效果</summary>
private static AnchorCountdownHud CreateCountdownUi(Transform cameraTransform, Sprite squareSprite)
    {
        GameObject hudObject = new GameObject("Anchor Countdown HUD");
        hudObject.transform.SetParent(cameraTransform, false);
        hudObject.transform.localPosition = Vector3.zero;
        hudObject.transform.localRotation = Quaternion.identity;
        hudObject.transform.localScale = Vector3.one;

        AnchorCountdownHud hud = hudObject.AddComponent<AnchorCountdownHud>();
        SerializedObject serializedHud = new SerializedObject(hud);
        serializedHud.FindProperty("blockSprite").objectReferenceValue = squareSprite;
        serializedHud.FindProperty("blockSize").vector2Value = new Vector2(0.88f, 0.56f);
        serializedHud.FindProperty("blockSpacing").floatValue = 0.26f;
        serializedHud.FindProperty("topMargin").floatValue = 0.95f;
        serializedHud.FindProperty("sidePadding").floatValue = 0.7f;
        serializedHud.FindProperty("sortingOrder").intValue = 260;
        serializedHud.ApplyModifiedPropertiesWithoutUndo();
        return hud;
    }

        // ===== 工具方法 =====

    /// <summary>确保烘焙用的白色方块精灵存在</summary>
private static Sprite EnsureSquareSprite()
    {
        EnsureFolder(ArtDirectory);
        string path = $"{ArtDirectory}/BakedSquareSprite.asset";
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (texture == null)
        {
            texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[16 * 16];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.white;
            }

            texture.SetPixels(pixels);
            texture.Apply();
            texture.filterMode = FilterMode.Point;
            AssetDatabase.CreateAsset(texture, path);
            Sprite created = Sprite.Create(texture, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16f);
            created.name = "BakedSquareSprite";
            AssetDatabase.AddObjectToAsset(created, texture);
            AssetDatabase.ImportAsset(path);
        }

        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is Sprite sprite)
            {
                return sprite;
            }
        }

        throw new MissingReferenceException("Could not create baked square sprite.");
    }

        /// <summary>清除场景中已有的 BakedLevelRoot（重新烘焙时覆盖）</summary>
private static void ClearBakedRoot(Scene scene)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i].name == BakedRootName)
            {
                Object.DestroyImmediate(roots[i]);
            }
        }
    }

        // ===== 坐标转换 =====

    /// <summary>计算物体中心（Unity 世界坐标）</summary>
private static Vector2 Center(DeathAnchorLevelObject item)
    {
        return Point(item.x + item.w * 0.5f, item.y + item.h * 0.5f);
    }

        /// <summary>计算物体脚底位置（用于出生点）</summary>
private static Vector2 FootPosition(DeathAnchorLevelObject item)
    {
        return Point(item.x + item.w * 0.5f, item.y + item.h);
    }

        /// <summary>将像素坐标转换为 Unity 世界坐标（Y 轴翻转）</summary>
private static Vector2 Point(float x, float y)
    {
        return new Vector2(x / PixelsPerUnit, -y / PixelsPerUnit);
    }

        /// <summary>将像素尺寸转换为 Unity 单位尺寸</summary>
private static Vector2 Size(DeathAnchorLevelObject item)
    {
        return new Vector2(Mathf.Max(0.05f, item.w / PixelsPerUnit), Mathf.Max(0.05f, item.h / PixelsPerUnit));
    }

    private static Color ColorFromRgb(float[] rgb, Color fallback)
    {
        if (rgb == null || rgb.Length < 3)
        {
            return fallback;
        }

        return new Color(rgb[0], rgb[1], rgb[2], fallback.a);
    }

    private static Vector2 LaserOrigin(DeathAnchorLevelObject item)
    {
        float rotation = NormalizeRotation(item.rotation);
        if (Mathf.Approximately(rotation, 90f))
        {
            return Point(item.x + item.w * 0.5f, item.y);
        }

        if (Mathf.Approximately(rotation, 180f))
        {
            return Point(item.x + item.w, item.y + item.h * 0.5f);
        }

        if (Mathf.Approximately(rotation, 270f))
        {
            return Point(item.x + item.w * 0.5f, item.y + item.h);
        }

        return Point(item.x, item.y + item.h * 0.5f);
    }

    private static Vector2 LaserDirection(DeathAnchorLevelObject item)
    {
        Vector2 explicitDirection = new Vector2(item.dirX, item.dirY);
        if (explicitDirection.sqrMagnitude > 0.0001f)
        {
            return explicitDirection.normalized;
        }

        float rotation = NormalizeRotation(item.rotation);
        if (Mathf.Approximately(rotation, 90f))
        {
            return Vector2.down;
        }

        if (Mathf.Approximately(rotation, 180f))
        {
            return Vector2.left;
        }

        if (Mathf.Approximately(rotation, 270f))
        {
            return Vector2.up;
        }

        return Vector2.right;
    }

    private static float LaserDistance(DeathAnchorLevelObject item)
    {
        float rotation = NormalizeRotation(item.rotation);
        float pixels = Mathf.Approximately(rotation, 90f) || Mathf.Approximately(rotation, 270f) ? item.h : item.w;
        return Mathf.Max(0.05f, pixels / PixelsPerUnit);
    }

    private static float NormalizeRotation(float value)
    {
        float rounded = Mathf.Round(value / 90f) * 90f;
        return Mathf.Repeat(rounded, 360f);
    }

    private static void AddRequiredButtonLinks(
        DeathAnchorLevelObject button,
        DeathAnchorLevelObject[] bridges,
        DeathAnchorLevelObject[] movingPlatforms,
        Dictionary<string, LinkedBridge> bridgesById,
        Dictionary<string, MovingPlatform2D> movingPlatformsById,
        List<LinkedBridge> linkedBridges,
        List<MovingPlatform2D> linkedPlatforms)
    {
        if (bridges != null)
        {
            for (int i = 0; i < bridges.Length; i++)
            {
                DeathAnchorLevelObject bridge = bridges[i];
                if (bridge == null || !ButtonControlsObject(button, bridge))
                {
                    continue;
                }

                if (bridgesById.TryGetValue(bridge.id, out LinkedBridge linkedBridge) && linkedBridge != null && !linkedBridges.Contains(linkedBridge))
                {
                    linkedBridges.Add(linkedBridge);
                }
            }
        }

        if (movingPlatforms == null)
        {
            return;
        }

        for (int i = 0; i < movingPlatforms.Length; i++)
        {
            DeathAnchorLevelObject platform = movingPlatforms[i];
            if (platform == null || platform.motionMode == "auto" || !ButtonControlsObject(button, platform))
            {
                continue;
            }

            if (movingPlatformsById.TryGetValue(platform.id, out MovingPlatform2D linkedPlatform) && linkedPlatform != null && !linkedPlatforms.Contains(linkedPlatform))
            {
                linkedPlatforms.Add(linkedPlatform);
            }
        }
    }

    private static bool ButtonControlsObject(DeathAnchorLevelObject button, DeathAnchorLevelObject controlled)
    {
        if (button == null || controlled == null)
        {
            return false;
        }

        if (!string.IsNullOrEmpty(controlled.requiredButton) && controlled.requiredButton == button.id)
        {
            return true;
        }

        if (!string.IsNullOrEmpty(button.channel) && !string.IsNullOrEmpty(controlled.channel) && button.channel == controlled.channel)
        {
            return true;
        }

        return false;
    }

    private static Vector2 PlayerColliderSize(DeathAnchorLevelData level)
    {
        Vector2 configured = PlayerConfiguredSize(level);
        float side = Mathf.Max(0.05f, Mathf.Min(configured.x, configured.y));
        return Vector2.one * side;
    }

    private static void ApplyPlayerPhysics(DeathAnchorLevelData level, DeathAnchorPlayerController controller)
    {
        PlayerPhysicsConfig physicsConfig = ReadPlayerPhysicsConfig();
        PlayerPhysicsUnityConfig unity = physicsConfig != null ? physicsConfig.unity : null;
        DeathAnchorPlayerPhysicsSpec exported = level.player != null ? level.player.physics : null;

        float moveSpeed = FirstPositive(
            unity != null ? unity.moveSpeedUnitsPerSec : 0f,
            exported != null ? exported.moveSpeed / PixelsPerUnit : 0f);
        float jumpSpeed = FirstPositive(
            unity != null ? unity.jumpSpeedUnitsPerSec : 0f,
            exported != null ? exported.jumpSpeed / PixelsPerUnit : 0f);
        float gravity = FirstPositive(
            unity != null ? unity.gravityUnitsPerSec2 : 0f,
            exported != null ? exported.gravity / PixelsPerUnit : 0f);
        float fallGravityMultiplier = FirstPositive(
            unity != null ? unity.fallGravityMultiplier : 0f,
            exported != null ? exported.fallGravityMultiplier : 0f);
        float maxFallSpeed = FirstPositive(
            unity != null ? unity.maxFallSpeedUnitsPerSec : 0f,
            exported != null ? exported.maxFallSpeed / PixelsPerUnit : 0f);
        float coyoteTime = FirstNonNegative(
            unity != null ? unity.coyoteTimeSec : -1f,
            exported != null ? exported.coyoteTimeMs / 1000f : -1f);
        float jumpBufferTime = FirstNonNegative(
            unity != null ? unity.jumpBufferSec : -1f,
            exported != null ? exported.jumpBufferMs / 1000f : -1f);
        float jumpCutMultiplier = FirstPositive(
            unity != null ? unity.jumpCutMultiplier : 0f,
            exported != null ? exported.jumpCutMultiplier : 0f);

        controller.ConfigureMovement(
            moveSpeed,
            jumpSpeed,
            gravity,
            fallGravityMultiplier,
            maxFallSpeed,
            coyoteTime,
            jumpBufferTime,
            jumpCutMultiplier);
    }

    private static Vector2 PlayerConfiguredSize(DeathAnchorLevelData level)
    {
        PlayerPhysicsConfig physicsConfig = ReadPlayerPhysicsConfig();
        if (physicsConfig != null && physicsConfig.unity != null)
        {
            float configuredWidth = physicsConfig.unity.playerWidthUnits;
            float configuredHeight = physicsConfig.unity.playerHeightUnits;
            if (configuredWidth > 0f && configuredHeight > 0f)
            {
                return new Vector2(configuredWidth, configuredHeight);
            }
        }

        return new Vector2(PlayerWidth(level), PlayerHeight(level));
    }

        private static float PlayerWidth(DeathAnchorLevelData level)
    {
        return (level.player != null && level.player.w > 0f ? level.player.w : 32f) / PixelsPerUnit;
    }

    private static float PlayerHeight(DeathAnchorLevelData level)
    {
        return (level.player != null && level.player.h > 0f ? level.player.h : 32f) / PixelsPerUnit;
    }

    private static PlayerPhysicsConfig ReadPlayerPhysicsConfig()
    {
        TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(PlayerPhysicsConfigPath);
        if (asset == null)
        {
            return null;
        }

        return JsonUtility.FromJson<PlayerPhysicsConfig>(asset.text);
    }

    private static float WallSlideUnits(DeathAnchorLevelData level)
    {
        PlayerPhysicsConfig physicsConfig = ReadPlayerPhysicsConfig();
        if (physicsConfig != null && physicsConfig.unity != null && physicsConfig.unity.wallSlideMaxSpeedUnitsPerSec > 0f)
        {
            return physicsConfig.unity.wallSlideMaxSpeedUnitsPerSec;
        }

        DeathAnchorPlayerPhysicsSpec exported = level.player != null ? level.player.physics : null;
        if (exported != null && exported.wallSlideMaxSpeed > 0f)
        {
            return exported.wallSlideMaxSpeed / PixelsPerUnit;
        }

        float speed = level.rules != null ? level.rules.wallSlideMaxSpeed : 125f;
        return speed / PixelsPerUnit;
    }

    private static float FirstPositive(params float[] values)
    {
        for (int i = 0; i < values.Length; i++)
        {
            if (values[i] > 0f)
            {
                return values[i];
            }
        }

        return 0f;
    }

    private static float FirstNonNegative(params float[] values)
    {
        for (int i = 0; i < values.Length; i++)
        {
            if (values[i] >= 0f)
            {
                return values[i];
            }
        }

        return -1f;
    }

        // ===== Layer 工具 =====

    /// <summary>将层名字符串数组组合为一个 LayerMask</summary>
private static LayerMask Mask(params string[] layers)
    {
        int mask = 0;
        for (int i = 0; i < layers.Length; i++)
        {
            int layer = LayerMask.NameToLayer(layers[i]);
            if (layer >= 0)
            {
                mask |= 1 << layer;
            }
        }

        return mask == 0 ? 1 << 0 : mask;
    }

        /// <summary>根据名称获取层索引，不存在则返回 0</summary>
private static int LayerOrDefault(string layerName)
    {
        int layer = LayerMask.NameToLayer(layerName);
        return layer >= 0 ? layer : 0;
    }

        // ===== 文件工具 =====

    /// <summary>将标题转换为安全的文件名</summary>
private static string SafeFileName(string value)
    {
        string safe = string.IsNullOrWhiteSpace(value) ? "DeathAnchorLevel" : value;
        foreach (char invalid in Path.GetInvalidFileNameChars())
        {
            safe = safe.Replace(invalid, '_');
        }

        return safe;
    }

        /// <summary>递归创建资源文件夹</summary>
private static void EnsureFolder(string assetFolder)
    {
        if (string.IsNullOrEmpty(assetFolder) || AssetDatabase.IsValidFolder(assetFolder))
        {
            return;
        }

        string parent = Path.GetDirectoryName(assetFolder).Replace("\\", "/");
        string name = Path.GetFileName(assetFolder);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }

        /// <summary>清除指定目录下现有关卡场景，准备全量重烘焙</summary>
    private static void DeleteExistingBakedScenes(string outputSceneDirectory)
    {
        if (!AssetDatabase.IsValidFolder(outputSceneDirectory))
        {
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Scene", new[] { outputSceneDirectory });
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.DeleteAsset(path);
            }
        }
    }

        /// <summary>将输出目录对应的场景列表与 Build Settings 同步</summary>
    private static void ReplaceScenesInBuildSettings(string outputSceneDirectory, List<string> scenePaths)
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>();
        for (int i = 0; i < EditorBuildSettings.scenes.Length; i++)
        {
            EditorBuildSettingsScene scene = EditorBuildSettings.scenes[i];
            if (scene.path.StartsWith(outputSceneDirectory + "/"))
            {
                continue;
            }

            scenes.Add(scene);
        }

        for (int i = 0; i < scenePaths.Count; i++)
        {
            scenes.Add(new EditorBuildSettingsScene(scenePaths[i], true));
        }

        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static int CompareLevelJsonPaths(string leftPath, string rightPath)
    {
        string leftName = GetLevelFileStem(leftPath);
        string rightName = GetLevelFileStem(rightPath);
        int leftNumeric = ExtractNumericLevelOrder(leftName);
        int rightNumeric = ExtractNumericLevelOrder(rightName);
        if (leftNumeric >= 0 && rightNumeric >= 0 && leftNumeric != rightNumeric)
        {
            return leftNumeric.CompareTo(rightNumeric);
        }

        return CompareChineseLevelName(leftName, rightName);
    }

    private static bool IsSupportedLevelJsonPath(string path)
    {
        string fileName = Path.GetFileName(path);
        if (string.IsNullOrEmpty(fileName))
        {
            return false;
        }

        if (fileName.EndsWith(".level.json", System.StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        string stem = Path.GetFileNameWithoutExtension(path);
        return ExtractNumericLevelOrder(stem) >= 0;
    }

    private static string GetLevelFileStem(string path)
    {
        string fileName = Path.GetFileName(path);
        if (fileName.EndsWith(".level.json", System.StringComparison.OrdinalIgnoreCase))
        {
            return Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(path));
        }

        return Path.GetFileNameWithoutExtension(path);
    }

    private static int CompareChineseLevelName(string left, string right)
    {
        int leftOrder = ExtractChineseLevelOrder(left);
        int rightOrder = ExtractChineseLevelOrder(right);
        if (leftOrder >= 0 && rightOrder >= 0 && leftOrder != rightOrder)
        {
            return leftOrder.CompareTo(rightOrder);
        }

        return string.CompareOrdinal(left, right);
    }

    private static int ExtractChineseLevelOrder(string value)
    {
        if (string.IsNullOrEmpty(value) || !value.StartsWith("第") || !value.EndsWith("关"))
        {
            return -1;
        }

        string numberPart = value.Substring(1, value.Length - 2);
        switch (numberPart)
        {
            case "一": return 1;
            case "二": return 2;
            case "三": return 3;
            case "四": return 4;
            case "五": return 5;
            case "六": return 6;
            case "七": return 7;
            case "八": return 8;
            case "九": return 9;
            case "十": return 10;
            default: return -1;
        }
    }

    private static int ExtractNumericLevelOrder(string value)
    {
        int order;
        return int.TryParse(value, out order) ? order : -1;
    }

    private static string ToProjectAbsolutePath(string path)
    {
        if (Path.IsPathRooted(path))
        {
            return path;
        }

        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        return Path.GetFullPath(Path.Combine(projectRoot, path));
    }

    [System.Serializable]
    private sealed class PlayerPhysicsConfig
    {
        public PlayerPhysicsUnityConfig unity;
    }

    [System.Serializable]
    private sealed class PlayerPhysicsUnityConfig
    {
        public float playerWidthUnits;
        public float playerHeightUnits;
        public float gravityUnitsPerSec2;
        public float fallGravityMultiplier;
        public float moveSpeedUnitsPerSec;
        public bool instantHorizontalMovement;
        public float jumpSpeedUnitsPerSec;
        public float maxFallSpeedUnitsPerSec;
        public float coyoteTimeSec;
        public float jumpBufferSec;
        public float jumpCutMultiplier;
        public float wallSlideMaxSpeedUnitsPerSec;
    }
}

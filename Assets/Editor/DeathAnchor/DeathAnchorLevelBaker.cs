using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Death Anchor 关卡烘焙器——将 .level.json 关卡描述文件程序化生成完整的 Unity 场景。
/// 
/// 使用方式：
///   Tools > Death Anchor > Bake Selected Level Json（烘焙单个 JSON）
///   Tools > Death Anchor > Bake All Prototype Levels（批量烘焙整个目录）
/// 
/// 烘焙流程：
///   1. 读取 .level.json 文件，解析为 DeathAnchorLevelData
///   2. 创建新场景（或打开已有场景覆盖）
///   3. 程序化生成所有关卡元素（平台、移动平台、桥梁、陷阱、钥匙、门、按钮、终点等）
///   4. 创建玩家和分身（设置碰撞层和控制器）
///   5. 创建 GameManager、相机、灯光、倒计时 UI
///   6. 保存场景并将场景添加到 Build Settings
/// </summary>
public static class DeathAnchorLevelBaker
{
    // ===== 常量 =====
    private const float PixelsPerUnit = 100f;  // 像素到 Unity 单位的转换比率
    // 原型关卡 JSON 目录（可根据需要修改为项目内路径）
    private const string PrototypeLevelsDirectory = "D:/Users/LanluZ/Desktop/death-anchor-editor-gdd-handoff-20260704/levels";
    private const string OutputSceneDirectory = "Assets/Scenes/DeathAnchor";        // 场景输出目录
    private const string ArtDirectory = "Assets/Art/DeathAnchor";                    // 美术资源目录
    private const string BakedRootName = "BakedLevelRoot";                           // 关卡根节点名称

    // ===== 颜色常量 =====
    private static readonly Color PlatformColor      = new Color(0.39f, 0.45f, 0.52f, 1f);  // 静态平台：灰蓝
    private static readonly Color MovingPlatformColor = new Color(0.55f, 0.82f, 0.49f, 1f);  // 移动平台：浅绿
    private static readonly Color SpikeColor          = new Color(1f, 0.27f, 0.34f, 1f);      // 陷阱：红色
    private static readonly Color PlayerColor         = new Color(1f, 0.72f, 0.26f, 1f);      // 玩家：金色
    private static readonly Color GhostColor          = new Color(0.18f, 0.82f, 1f, 0.55f);   // 分身：青色半透明
    private static readonly Color GoalColor           = new Color(0.3f, 1f, 0.58f, 1f);       // 终点：绿色
    private static readonly Color KeyColor            = new Color(1f, 0.78f, 0.18f, 1f);      // 钥匙：金色
    private static readonly Color DoorColor           = new Color(1f, 0.45f, 0.25f, 1f);      // 门：橙色
    private static readonly Color AnchorZoneColor     = new Color(0.68f, 0.55f, 1f, 0.2f);    // 锚点区域：紫色透明

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
            if (string.IsNullOrEmpty(pickedPath)) return;
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
        BakeDirectory(PrototypeLevelsDirectory, OutputSceneDirectory);
    }

    /// <summary>批量烘焙指定目录中的所有 .level.json 文件</summary>
    public static void BakeDirectory(string sourceDirectory, string outputSceneDirectory)
    {
        EnsureFolder(outputSceneDirectory);
        string[] files = Directory.GetFiles(sourceDirectory, "*.level.json", SearchOption.TopDirectoryOnly);
        List<string> scenePaths = new List<string>();

        for (int i = 0; i < files.Length; i++)
        {
            DeathAnchorLevelData level = ReadLevel(files[i]);
            string scenePath = $"{outputSceneDirectory}/{SafeFileName(level.title)}.unity";
            BakeLevel(files[i], scenePath);
            scenePaths.Add(scenePath);
        }

        AddScenesToBuildSettings(scenePaths);
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
        Sprite squareSprite = EnsureSquareSprite();  // 确保有白色方块精灵用于所有物体

        Scene scene = File.Exists(scenePath)
            ? EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single)
            : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        ClearBakedRoot(scene);

        // 创建关卡根节点和元数据
        GameObject root = new GameObject(BakedRootName);
        LevelMetadata metadata = root.AddComponent<LevelMetadata>();
        metadata.Initialize(Path.GetFileName(jsonPath), level);

        // 获取 Unity 层索引
        int groundLayer = LayerOrDefault("Ground");
        int playerLayer = LayerOrDefault("Player");
        int ghostLayer = LayerOrDefault("Ghost");
        int hazardLayer = LayerOrDefault("Hazard");
        int interactableLayer = LayerOrDefault("Interactable");

        Dictionary<string, LinkedBridge> bridgesById = new Dictionary<string, LinkedBridge>();

        // 创建层级结构
        Transform environmentRoot = CreateChild(root.transform, "Environment");     // 环境物件
        Transform interactableRoot = CreateChild(root.transform, "Interactables");  // 交互物件
        Transform actorRoot = CreateChild(root.transform, "Actors");                // 角色

        // ---- 烘焙环境 ----
        BakeObjects(level.platforms, environmentRoot, squareSprite, PlatformColor, groundLayer, AddStaticSolid);
        BakeMovingPlatforms(level.movingPlatforms, environmentRoot, squareSprite, groundLayer);
        BakeBridges(level.bridges, environmentRoot, squareSprite, groundLayer, bridgesById);
        BakeSpikes(level.spikes, environmentRoot, squareSprite, hazardLayer);
        BakeAnchorZones(level.anchorZones, environmentRoot, squareSprite, interactableLayer);

        // 出生点
        Transform spawnPoint = new GameObject("SpawnPoint").transform;
        spawnPoint.SetParent(root.transform);
        spawnPoint.position = FootPosition(level.spawn);

        // ---- 创建 GameManager ----
        GameObject managerObject = new GameObject("GameManager");
        managerObject.transform.SetParent(root.transform);
        DeathAnchorGameManager manager = managerObject.AddComponent<DeathAnchorGameManager>();

        // ---- 创建玩家 ----
        GameObject player = CreateActor("Player", actorRoot, squareSprite, PlayerColor, playerLayer, level, false);
        DeathAnchorPlayerController playerController = player.AddComponent<DeathAnchorPlayerController>();
        playerController.Configure(
            PlayerWidth(level),
            PlayerHeight(level),
            Mask("Ground", "Ghost"),
            level.rules == null || level.rules.playerWallSlide,
            WallSlideUnits(level));
        player.transform.position = spawnPoint.position + Vector3.up * (PlayerHeight(level) * 0.5f);
        playerController.SpawnAtFootPosition(spawnPoint.position);

        // ---- 创建分身幽灵（初始隐藏） ----
        GameObject ghost = CreateActor("Ghost", actorRoot, squareSprite, GhostColor, ghostLayer, level, true);
        GhostReplayController ghostReplay = ghost.AddComponent<GhostReplayController>();
        ghostReplay.Configure(PlayerWidth(level), PlayerHeight(level), Mask("Ground"), Mask("Player"));
        ghost.SetActive(false);

        // 锚点标记
        GameObject anchorMarker = CreateBlock("AnchorMarker", root.transform, spawnPoint.position,
            new Vector2(0.35f, 0.08f), squareSprite, new Color(0.75f, 0.55f, 1f, 0.9f), interactableLayer);
        Object.DestroyImmediate(anchorMarker.GetComponent<Collider2D>());
        anchorMarker.SetActive(false);

        // 倒计时 UI
        Text countdownText = CreateCountdownUi(root.transform);
        manager.Configure(level.rules != null ? level.rules.recordWindowSec : 5f,
            playerController, ghostReplay, spawnPoint, anchorMarker.transform, countdownText);

        // ---- 烘焙交互物件 ----
        BakeKeys(level.keys, interactableRoot, squareSprite, interactableLayer);
        BakeDoors(level.doors, interactableRoot, squareSprite, groundLayer);
        BakeButtons(level.buttons, interactableRoot, squareSprite, interactableLayer, bridgesById);
        BakeGoals(level.goals, interactableRoot, squareSprite, interactableLayer);

        // 创建相机和灯光
        CreateCamera(root.transform, player.transform, level);
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
            throw new InvalidDataException($"Could not parse level json: {jsonPath}");

        if (level.rules == null) level.rules = new DeathAnchorRules();
        if (level.world == null) level.world = new DeathAnchorWorld();

        return level;
    }

    // ===== 各类物件烘焙方法 =====

    /// <summary>烘焙普通物件（平台等），支持自定义后处理回调</summary>
    private static void BakeObjects(DeathAnchorLevelObject[] objects, Transform parent, Sprite sprite, Color color, int layer, System.Action<GameObject> configure)
    {
        if (objects == null) return;
        for (int i = 0; i < objects.Length; i++)
        {
            GameObject go = CreateBlock(objects[i].id, parent, Center(objects[i]), Size(objects[i]), sprite, color, layer);
            go.transform.rotation = Quaternion.Euler(0f, 0f, -objects[i].rotation);
            configure(go);
        }
    }

    /// <summary>烘焙移动平台</summary>
    private static void BakeMovingPlatforms(DeathAnchorLevelObject[] objects, Transform parent, Sprite sprite, int layer)
    {
        if (objects == null) return;
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
            platform.Configure(target, item.periodSec > 0f ? item.periodSec : 3f);
        }
    }

    /// <summary>烘焙虚实桥（按钮控制）</summary>
    private static void BakeBridges(DeathAnchorLevelObject[] objects, Transform parent, Sprite sprite, int layer, Dictionary<string, LinkedBridge> bridgesById)
    {
        if (objects == null) return;
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
        if (objects == null) return;
        for (int i = 0; i < objects.Length; i++)
        {
            DeathAnchorLevelObject item = objects[i];
            GameObject go = CreateBlock(item.id, parent, Center(item), Size(item), sprite, SpikeColor, layer);
            go.transform.rotation = Quaternion.Euler(0f, 0f, -item.rotation);
            go.AddComponent<HazardTrigger>();
        }
    }

    /// <summary>烘焙锚点区域（纯触发器，无碰撞）</summary>
    private static void BakeAnchorZones(DeathAnchorLevelObject[] objects, Transform parent, Sprite sprite, int layer)
    {
        if (objects == null) return;
        for (int i = 0; i < objects.Length; i++)
        {
            GameObject go = CreateBlock(objects[i].id, parent, Center(objects[i]), Size(objects[i]), sprite, AnchorZoneColor, layer);
            go.GetComponent<Collider2D>().isTrigger = true;
        }
    }

    /// <summary>烘焙钥匙</summary>
    private static void BakeKeys(DeathAnchorLevelObject[] objects, Transform parent, Sprite sprite, int layer)
    {
        if (objects == null) return;
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
        if (objects == null) return;
        for (int i = 0; i < objects.Length; i++)
        {
            DeathAnchorLevelObject item = objects[i];
            GameObject go = CreateBlock(item.id, parent, Center(item), Size(item), sprite, DoorColor, layer);
            AddStaticSolid(go);
            BoxCollider2D trigger = go.AddComponent<BoxCollider2D>();
            trigger.size = Vector2.one * 1.08f;  // 触发器稍大一些方便检测
            trigger.isTrigger = true;
            KeyDoor door = go.AddComponent<KeyDoor>();
            door.Configure(item.requiredKey);
        }
    }

    /// <summary>烘焙按钮开关（关联桥梁）</summary>
    private static void BakeButtons(DeathAnchorLevelObject[] objects, Transform parent, Sprite sprite, int layer, Dictionary<string, LinkedBridge> bridgesById)
    {
        if (objects == null) return;
        for (int i = 0; i < objects.Length; i++)
        {
            DeathAnchorLevelObject item = objects[i];
            GameObject go = CreateBlock(item.id, parent, Center(item), Size(item), sprite, KeyColor, layer);
            List<LinkedBridge> linked = new List<LinkedBridge>();
            if (item.links != null)
            {
                for (int j = 0; j < item.links.Length; j++)
                {
                    if (bridgesById.TryGetValue(item.links[j], out LinkedBridge bridge))
                        linked.Add(bridge);
                }
            }
            ButtonSwitch button = go.AddComponent<ButtonSwitch>();
            button.Configure(item.id, item.pressedBy, linked.ToArray());
        }
    }

    /// <summary>烘焙终点触发器</summary>
    private static void BakeGoals(DeathAnchorLevelObject[] objects, Transform parent, Sprite sprite, int layer)
    {
        if (objects == null) return;
        for (int i = 0; i < objects.Length; i++)
        {
            GameObject go = CreateBlock(objects[i].id, parent, Center(objects[i]), Size(objects[i]), sprite, GoalColor, layer);
            go.AddComponent<DeathAnchorGoalTrigger>();
        }
    }

    // ===== 角色和物块创建 =====

    /// <summary>创建一个角色（玩家或分身）：包含 Rigidbody2D、BoxCollider2D、ActorIdentity 和视觉子物体</summary>
    private static GameObject CreateActor(string name, Transform parent, Sprite sprite, Color color, int layer,
        DeathAnchorLevelData level, bool ghost)
    {
        Vector2 size = new Vector2(PlayerWidth(level), PlayerHeight(level));
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent);
        go.layer = layer;

        BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
        collider.size = size;
        collider.offset = Vector2.zero;

        // 视觉子节点（SpriteRenderer）
        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(go.transform);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localScale = new Vector3(size.x, size.y, 1f);
        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;

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
        collider.size = Vector2.one;  // 缩放已经被 localScale 处理
        return go;
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

    // ===== 相机和灯光 =====

    /// <summary>创建正交相机和跟随脚本</summary>
    private static void CreateCamera(Transform root, Transform target, DeathAnchorLevelData level)
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.transform.SetParent(root);
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 3.6f;
        camera.backgroundColor = new Color(0.12f, 0.13f, 0.16f, 1f);
        cameraObject.transform.position = new Vector3(target.position.x, target.position.y, -10f);

        CameraFollow2D follow = cameraObject.AddComponent<CameraFollow2D>();
        float halfHeight = camera.orthographicSize;
        float halfWidth = halfHeight * 16f / 9f;
        Vector2 min = new Vector2(halfWidth, -level.world.h / PixelsPerUnit + halfHeight);
        Vector2 max = new Vector2(level.world.w / PixelsPerUnit - halfWidth, -halfHeight);
        follow.Configure(target, min, max);
    }

    /// <summary>创建方向光</summary>
    private static void CreateLight(Transform root)
    {
        GameObject lightObject = new GameObject("Directional Light");
        lightObject.transform.SetParent(root);
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1f;
        lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
    }

    // ===== UI =====

    /// <summary>创建 HUD Canvas 和倒计时文字</summary>
    private static Text CreateCountdownUi(Transform root)
    {
        GameObject canvasObject = new GameObject("HUD Canvas");
        canvasObject.transform.SetParent(root);
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject textObject = new GameObject("Anchor Countdown Text");
        textObject.transform.SetParent(canvasObject.transform);
        Text text = textObject.AddComponent<Text>();
        text.text = "ANCHOR REC 5.0s";
        text.alignment = TextAnchor.UpperCenter;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 44;
        text.fontStyle = FontStyle.Bold;
        text.color = new Color(0.78f, 0.58f, 1f, 1f);
        text.raycastTarget = false;

        RectTransform rect = text.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -24f);
        rect.sizeDelta = new Vector2(700f, 80f);

        textObject.SetActive(false);
        return text;
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
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
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
            if (assets[i] is Sprite sprite) return sprite;
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
                Object.DestroyImmediate(roots[i]);
        }
    }

    // ===== 坐标转换 =====

    /// <summary>计算物体中心（Unity 世界坐标）</summary>
    private static Vector2 Center(DeathAnchorLevelObject item) => Point(item.x + item.w * 0.5f, item.y + item.h * 0.5f);

    /// <summary>计算物体脚底位置（用于出生点）</summary>
    private static Vector2 FootPosition(DeathAnchorLevelObject item) => Point(item.x + item.w * 0.5f, item.y + item.h);

    /// <summary>将像素坐标转换为 Unity 世界坐标（Y 轴翻转）</summary>
    private static Vector2 Point(float x, float y) => new Vector2(x / PixelsPerUnit, -y / PixelsPerUnit);

    /// <summary>将像素尺寸转换为 Unity 单位尺寸</summary>
    private static Vector2 Size(DeathAnchorLevelObject item)
        => new Vector2(Mathf.Max(0.05f, item.w / PixelsPerUnit), Mathf.Max(0.05f, item.h / PixelsPerUnit));

    // ===== 玩家规格计算 =====

    private static float PlayerWidth(DeathAnchorLevelData level)
        => (level.player != null && level.player.w > 0f ? level.player.w : 30f) / PixelsPerUnit;

    private static float PlayerHeight(DeathAnchorLevelData level)
        => (level.player != null && level.player.h > 0f ? level.player.h : 42f) / PixelsPerUnit;

    private static float WallSlideUnits(DeathAnchorLevelData level)
    {
        float speed = level.rules != null ? level.rules.wallSlideMaxSpeed : 125f;
        return speed / PixelsPerUnit;
    }

    // ===== Layer 工具 =====

    /// <summary>将层名字符串数组组合为一个 LayerMask</summary>
    private static LayerMask Mask(params string[] layers)
    {
        int mask = 0;
        for (int i = 0; i < layers.Length; i++)
        {
            int layer = LayerMask.NameToLayer(layers[i]);
            if (layer >= 0) mask |= 1 << layer;
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
            safe = safe.Replace(invalid, '_');
        return safe;
    }

    /// <summary>递归创建资源文件夹</summary>
    private static void EnsureFolder(string assetFolder)
    {
        if (string.IsNullOrEmpty(assetFolder) || AssetDatabase.IsValidFolder(assetFolder)) return;

        string parent = Path.GetDirectoryName(assetFolder).Replace("\\", "/");
        string name = Path.GetFileName(assetFolder);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }

    /// <summary>将场景路径添加到 Build Settings（避免重复）</summary>
    private static void AddScenesToBuildSettings(List<string> scenePaths)
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        for (int i = 0; i < scenePaths.Count; i++)
        {
            if (scenes.Exists(scene => scene.path == scenePaths[i])) continue;
            scenes.Add(new EditorBuildSettingsScene(scenePaths[i], true));
        }
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class DeathAnchorLevelBaker
{
    private const float PixelsPerUnit = 100f;
    private const string PrototypeLevelsDirectory = "D:/Users/LanluZ/Desktop/death-anchor-editor-gdd-handoff-20260704/levels";
    private const string OutputSceneDirectory = "Assets/Scenes/DeathAnchor";
    private const string ArtDirectory = "Assets/Art/DeathAnchor";
    private const string BackgroundSpritePath = "Assets/Art/Background.png";
    private const string BakedRootName = "BakedLevelRoot";

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

    [MenuItem("Tools/Death Anchor/Bake All Prototype Levels")]
    public static void BakeAllPrototypeLevels()
    {
        BakeDirectory(PrototypeLevelsDirectory, OutputSceneDirectory);
    }

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
        BakeLasers(level.lasers, environmentRoot, interactableLayer, Mask("Ground"));
        BakeAnchorZones(level.anchorZones, environmentRoot, squareSprite, interactableLayer);

        Transform spawnPoint = new GameObject("SpawnPoint").transform;
        spawnPoint.SetParent(root.transform);
        spawnPoint.position = FootPosition(level.spawn);

        GameObject managerObject = new GameObject("GameManager");
        managerObject.transform.SetParent(root.transform);
        DeathAnchorGameManager manager = managerObject.AddComponent<DeathAnchorGameManager>();

        GameObject player = CreateActor("Player", actorRoot, squareSprite, PlayerColor, playerLayer, level, false);
        DeathAnchorPlayerController playerController = player.AddComponent<DeathAnchorPlayerController>();
        playerController.Configure(
            PlayerWidth(level),
            PlayerHeight(level),
            Mask("Ground", "Ghost"),
            level.rules == null || level.rules.playerWallSlide,
            WallSlideUnits(level));
        player.AddComponent<AkGameObj>();
        player.AddComponent<PlayerWwiseAudio>();
        player.AddComponent<PlayerDustParticles>();
        player.transform.position = spawnPoint.position + Vector3.up * (PlayerHeight(level) * 0.5f);
        playerController.SpawnAtFootPosition(spawnPoint.position);

        GameObject ghost = CreateActor("Ghost", actorRoot, squareSprite, GhostColor, ghostLayer, level, true);
        GhostReplayController ghostReplay = ghost.AddComponent<GhostReplayController>();
        ghostReplay.Configure(PlayerWidth(level), PlayerHeight(level), Mask("Player"));
        ghost.SetActive(false);

        GameObject anchorMarker = CreateBlock("AnchorMarker", root.transform, spawnPoint.position, new Vector2(0.35f, 0.08f), squareSprite, new Color(0.75f, 0.55f, 1f, 0.9f), interactableLayer);
        Object.DestroyImmediate(anchorMarker.GetComponent<Collider2D>());
        anchorMarker.SetActive(false);

        Text countdownText = CreateCountdownUi(root.transform);
        manager.Configure(level.rules != null ? level.rules.recordWindowSec : 5f, playerController, ghostReplay, spawnPoint, anchorMarker.transform, countdownText);

        BakeKeys(level.keys, interactableRoot, squareSprite, interactableLayer);
        BakeDoors(level.doors, interactableRoot, squareSprite, groundLayer);
        BakeButtons(level.buttons, interactableRoot, squareSprite, interactableLayer, bridgesById, movingPlatformsById);
        BakeGoals(level.goals, interactableRoot, squareSprite, interactableLayer);
        BakeNotes(level.notes, interactableRoot);

        CreateCamera(root.transform, player.transform, level);
        CreateLight(root.transform);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, scenePath);
    }

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

    private static void BakeLasers(DeathAnchorLevelObject[] objects, Transform parent, int layer, LayerMask blockMask)
    {
        if (objects == null)
        {
            return;
        }

        for (int i = 0; i < objects.Length; i++)
        {
            DeathAnchorLevelObject item = objects[i];
            GameObject go = new GameObject(string.IsNullOrEmpty(item.id) ? "Laser" : item.id);
            go.transform.SetParent(parent);
            go.transform.position = Point(item.x, item.y);
            go.layer = layer;

            LineRenderer line = go.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.useWorldSpace = true;
            line.sortingOrder = 20;
            line.material = new Material(Shader.Find("Sprites/Default"));

            LaserBeam laser = go.AddComponent<LaserBeam>();
            Vector2 direction = new Vector2(item.dirX, item.dirY);
            if (direction.sqrMagnitude <= 0.0001f)
            {
                direction = Vector2.right;
            }

            float distance = item.maxDistance > 0f ? item.maxDistance / PixelsPerUnit : 5f;
            laser.Configure(direction, distance, blockMask, ColorFromRgb(item.beamColor, new Color(1f, 0.15f, 0.15f, 0.9f)));
        }
    }

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

    private static void BakeButtons(DeathAnchorLevelObject[] objects, Transform parent, Sprite sprite, int layer, Dictionary<string, LinkedBridge> bridgesById, Dictionary<string, MovingPlatform2D> movingPlatformsById)
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

            ButtonSwitch button = go.AddComponent<ButtonSwitch>();
            button.Configure(item.id, item.pressedBy, linked.ToArray(), linkedPlatforms.ToArray());
        }
    }

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

    private static GameObject CreateActor(string name, Transform parent, Sprite sprite, Color color, int layer, DeathAnchorLevelData level, bool ghost)
    {
        Vector2 size = new Vector2(PlayerWidth(level), PlayerHeight(level));
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

    private static void ApplyLitShader(GameObject go, string shaderName)
    {
        SpriteRenderer renderer = go.GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            return;
        }

        renderer.sharedMaterial = CreateLitMaterial(shaderName);
    }

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

    private static Transform CreateChild(Transform parent, string name)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent);
        return child.transform;
    }

    private static void AddStaticSolid(GameObject go)
    {
        go.isStatic = true;
    }

    private static void CreateBackground(Camera camera)
    {
        Sprite backgroundSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundSpritePath);
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

    private static void CreateCamera(Transform root, Transform target, DeathAnchorLevelData level)
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.transform.SetParent(root);
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 3.6f;
        camera.backgroundColor = new Color(0.22f, 0.23f, 0.23f, 1f);
        cameraObject.transform.position = new Vector3(target.position.x, target.position.y, -10f);
        CreateBackground(camera);
        CameraFollow2D follow = cameraObject.AddComponent<CameraFollow2D>();
        float halfHeight = camera.orthographicSize;
        float halfWidth = halfHeight * 16f / 9f;
        Vector2 min = new Vector2(halfWidth, -level.world.h / PixelsPerUnit + halfHeight);
        Vector2 max = new Vector2(level.world.w / PixelsPerUnit - halfWidth, -halfHeight);
        follow.Configure(target, min, max);
    }

    private static void CreateLight(Transform root)
    {
        GameObject lightObject = new GameObject("GlobalLight2D");
        lightObject.transform.SetParent(root);
        Light2D light = lightObject.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Global;
        light.color = GlobalLightColor;
        light.intensity = 5.84f;
    }

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

    private static Vector2 Center(DeathAnchorLevelObject item)
    {
        return Point(item.x + item.w * 0.5f, item.y + item.h * 0.5f);
    }

    private static Vector2 FootPosition(DeathAnchorLevelObject item)
    {
        return Point(item.x + item.w * 0.5f, item.y + item.h);
    }

    private static Vector2 Point(float x, float y)
    {
        return new Vector2(x / PixelsPerUnit, -y / PixelsPerUnit);
    }

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

    private static float PlayerWidth(DeathAnchorLevelData level)
    {
        return PlayerHeight(level);
    }

    private static float PlayerHeight(DeathAnchorLevelData level)
    {
        return (level.player != null && level.player.h > 0f ? level.player.h : 42f) / PixelsPerUnit;
    }

    private static float WallSlideUnits(DeathAnchorLevelData level)
    {
        float speed = level.rules != null ? level.rules.wallSlideMaxSpeed : 125f;
        return speed / PixelsPerUnit;
    }

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

    private static int LayerOrDefault(string layerName)
    {
        int layer = LayerMask.NameToLayer(layerName);
        return layer >= 0 ? layer : 0;
    }

    private static string SafeFileName(string value)
    {
        string safe = string.IsNullOrWhiteSpace(value) ? "DeathAnchorLevel" : value;
        foreach (char invalid in Path.GetInvalidFileNameChars())
        {
            safe = safe.Replace(invalid, '_');
        }

        return safe;
    }

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

    private static void AddScenesToBuildSettings(List<string> scenePaths)
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        for (int i = 0; i < scenePaths.Count; i++)
        {
            if (scenes.Exists(scene => scene.path == scenePaths[i]))
            {
                continue;
            }

            scenes.Add(new EditorBuildSettingsScene(scenePaths[i], true));
        }

        EditorBuildSettings.scenes = scenes.ToArray();
    }
}

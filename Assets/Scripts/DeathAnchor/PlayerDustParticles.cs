using UnityEngine;

/// <summary>
/// Programmatically creates a pixel-style dust ParticleSystem on the player.
/// Emits small fading squares for walking, jumping, and landing.
/// Attach to the same GameObject as DeathAnchorPlayerController.
/// </summary>
[RequireComponent(typeof(DeathAnchorPlayerController))]
public sealed class PlayerDustParticles : MonoBehaviour
{
    [Header("Walk Dust")]
    [SerializeField] private float walkInputThreshold = 0.1f;
    [SerializeField] private float walkDustInterval = 0.32f;
    [SerializeField] private int walkDustCount = 3;

    [Header("Jump Dust")]
    [SerializeField] private int jumpDustCount = 10;

    [Header("Land Dust")]
    [SerializeField] private int landDustCount = 12;

    [Header("Wall Slide Dust")]
    [SerializeField] private float wallDustInterval = 0.15f;
    [SerializeField] private int wallDustCount = 2;
    [SerializeField] private float wallDustOffsetX = 0.03f;

    [Header("Particle Look")]
    [SerializeField] private float particleStartSize = 0.04f;
    [SerializeField] private float particleLifetime = 0.4f;
    [SerializeField] private Color particleColor = new Color(0.7f, 0.65f, 0.55f, 0.8f);

    private DeathAnchorPlayerController controller;
    private ParticleSystem ps;
    private bool wasGrounded;
    private float nextWalkDustAt;
    private float nextWallDustAt;

    private void Awake()
    {
        controller = GetComponent<DeathAnchorPlayerController>();
        CreateParticleSystem();
    }

    private void CreateParticleSystem()
    {
        GameObject dustGo = new GameObject("DustParticles");
        dustGo.transform.SetParent(transform, false);
        dustGo.transform.localPosition = Vector3.zero;

        ps = dustGo.AddComponent<ParticleSystem>();

        // --- Main module ---
        var main = ps.main;
        main.duration = 5f;
        main.loop = true;
        main.startLifetime = particleLifetime;
        main.startSpeed = 0.6f;
        main.startSize = particleStartSize;
        main.startColor = particleColor;
        main.gravityModifier = 0.4f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 200;
        main.playOnAwake = false;

        // --- Emission: driven via Emit(), so zero rate ---
        var emission = ps.emission;
        emission.enabled = false;

        // --- Shape: hemisphere upward for natural spread ---
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Hemisphere;
        shape.radius = 0.03f;
        shape.arc = 180f;

        // --- Color over lifetime: fade alpha to 0 ---
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(particleColor, 0f),
                new GradientColorKey(particleColor, 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0.8f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;

        // --- Size over lifetime: shrink slightly ---
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(1f, 0.3f)
        );
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // --- Renderer: pixel square material ---
        var psr = dustGo.GetComponent<ParticleSystemRenderer>();
        psr.material = CreatePixelMaterial();

        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private Material CreatePixelMaterial()
    {
        // Try to load the baked square sprite texture
        Texture2D tex = null;

#if UNITY_EDITOR
        tex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(
            "Assets/Art/DeathAnchor/BakedSquareSprite.asset");
#endif

        if (tex == null)
        {
            // Fallback: create a small white texture at runtime
            tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            Color[] px = new Color[16];
            for (int i = 0; i < 16; i++) px[i] = Color.white;
            tex.SetPixels(px);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
        }

        // URP 2D sprite shader — same as project sprite assets
        Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Texture");
        if (shader == null) shader = Shader.Find("Sprites/Default");

        Material mat = new Material(shader);
        mat.mainTexture = tex;
        mat.mainTextureScale = Vector2.one;
        mat.mainTextureOffset = Vector2.zero;

        return mat;
    }

    private void Update()
    {
        float horizontalInput = 0f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) horizontalInput -= 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) horizontalInput += 1f;

        bool isMovingOnGround = controller.Grounded && Mathf.Abs(horizontalInput) > walkInputThreshold;

        // --- Walk dust: emit small puffs on timer ---
        if (isMovingOnGround && Time.time >= nextWalkDustAt)
        {
            EmitAtFoot(walkDustCount, 0.4f);
            nextWalkDustAt = Time.time + Mathf.Max(0.05f, walkDustInterval);
        }
        else if (!isMovingOnGround)
        {
            nextWalkDustAt = Time.time;
        }

        // --- Jump dust: grounded -> airborne transition ---
        if (wasGrounded && !controller.Grounded)
        {
            EmitAtFoot(jumpDustCount, 1.2f);
        }

        // --- Land dust: airborne -> grounded transition ---
        if (!wasGrounded && controller.Grounded)
        {
            EmitAtFoot(landDustCount, 1.5f);
        }

        // --- Wall slide dust: emit puffs from wall contact side ---
        if (controller.WallSliding)
        {
            if (Time.time >= nextWallDustAt)
            {
                Vector3 wallPos = controller.FootPosition;
                wallPos.x += -controller.Facing * wallDustOffsetX;
                wallPos.y += 0.05f;
                EmitAt(wallPos, wallDustCount, 0.5f);
                nextWallDustAt = Time.time + Mathf.Max(0.05f, wallDustInterval);
            }
        }
        else
        {
            nextWallDustAt = Time.time;
        }

        wasGrounded = controller.Grounded;
    }

    private void EmitAtFoot(int count, float speedMultiplier)
    {
        EmitAt(controller.FootPosition, count, speedMultiplier);
    }

    private void EmitAt(Vector3 position, int count, float speedMultiplier)
    {
        if (ps == null) return;

        ps.transform.position = position;

        var main = ps.main;
        float originalSpeed = main.startSpeed.constant;
        main.startSpeed = speedMultiplier;

        ps.Emit(count);

        main.startSpeed = originalSpeed;
    }
}

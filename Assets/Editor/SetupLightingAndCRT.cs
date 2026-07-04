using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEditor;

/// <summary>
/// Editor tool: sets up Light2D point lights on Player, Ghost, and Exit objects,
/// applies corresponding lit shaders, and adds the CRT Display renderer feature.
/// Menu: Tools > Setup Lighting & CRT
/// </summary>
public static class SetupLightingAndCRT
{
    [MenuItem("Tools/Setup Lighting + CRT")]
    public static void Setup()
    {
        SetupLights();
        SetupCRTFeature();
        Debug.Log("[LightingSetup] Done! Light2D added to Player/Ghost, CRT feature added to Renderer2D.");
    }

    static void SetupLights()
    {
        // === Player Light2D ===
        var player = GameObject.Find("Player");
        if (player != null)
        {
            AddOrGetLight2D(player, "PlayerLight", new Color(1f, 0.8f, 0.2f), 3f, 4f);
            ApplyShader(player, "Custom/PlayerLit");
            Debug.Log("[LightingSetup] Player: Light2D + PlayerLit shader applied");
        }
        else
        {
            Debug.LogWarning("[LightingSetup] Player not found in scene");
        }

        // === Ghost Light2D ===
        var ghost = GameObject.Find("Ghost");
        if (ghost != null)
        {
            AddOrGetLight2D(ghost, "GhostLight", new Color(0.2f, 0.8f, 1f), 2.5f, 3.5f);
            ApplyShader(ghost, "Custom/GhostLit");
            Debug.Log("[LightingSetup] Ghost: Light2D + GhostLit shader applied");
        }
        else
        {
            Debug.LogWarning("[LightingSetup] Ghost not found in scene");
        }

        // === Exit Light2D (search by ActorIdentity or name) ===
        var exit = GameObject.Find("Exit") ?? GameObject.Find("Goal") ?? GameObject.Find("出口");
        if (exit != null)
        {
            AddOrGetLight2D(exit, "ExitLight", new Color(0.1f, 1f, 0.4f), 3.5f, 5f);
            ApplyShader(exit, "Custom/ExitLit");
            Debug.Log("[LightingSetup] Exit: Light2D + ExitLit shader applied");
        }
        else
        {
            Debug.LogWarning("[LightingSetup] Exit/Goal not found in scene — add Light2D manually");
        }

        // === Global ambient light (dim background so lights stand out) ===
        var globalLight = GameObject.Find("GlobalLight2D");
        if (globalLight == null)
        {
            globalLight = new GameObject("GlobalLight2D");
            globalLight.transform.SetParent(GameObject.Find("BakedLevelRoot")?.transform);
        }
        var gl = AddOrGetLight2D(globalLight, "GlobalLight2D", new Color(0.15f, 0.15f, 0.2f), 0.4f, 0f);
        gl.lightType = Light2D.LightType.Global;
        gl.intensity = 0.3f;
        gl.pointLightInnerRadius = 0;
        gl.pointLightOuterRadius = 0;
        EditorUtility.SetDirty(gl);
        Debug.Log("[LightingSetup] Global dim light created (intensity 0.3)");
    }

    static Light2D AddOrGetLight2D(GameObject go, string lightName, Color color, float intensity, float radius)
    {
        var light = go.GetComponent<Light2D>();
        if (light == null)
        {
            light = go.AddComponent<Light2D>();
        }

        light.lightType = Light2D.LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.pointLightInnerRadius = radius * 0.4f;
        light.pointLightOuterRadius = radius;
        EditorUtility.SetDirty(light);
        return light;
    }

    static void ApplyShader(GameObject go, string shaderName)
    {
        var sr = go.GetComponent<SpriteRenderer>();
        if (sr == null) return;

        var shader = Shader.Find(shaderName);
        if (shader == null)
        {
            Debug.LogWarning($"[LightingSetup] Shader '{shaderName}' not found");
            return;
        }

        var mat = sr.material;
        if (mat == null)
        {
            mat = new Material(shader);
        }
        else
        {
            mat.shader = shader;
        }
        sr.material = mat;
        EditorUtility.SetDirty(mat);
    }

    static void SetupCRTFeature()
    {
        string rendererPath = "Assets/Settings/Renderer2D.asset";
        var rendererData = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(rendererPath);
        if (rendererData == null)
        {
            Debug.LogError("[LightingSetup] Could not load Renderer2D at " + rendererPath);
            return;
        }

        var rendererType = rendererData.GetType();
        var featuresField = rendererType.GetField("m_RendererFeatures",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (featuresField == null) return;

        var featuresList = featuresField.GetValue(rendererData) as System.Collections.IList;
        if (featuresList == null) return;

        // Check if CRT feature already exists
        foreach (var feature in featuresList)
        {
            if (feature != null && feature.GetType().Name == "CRTDisplayRendererFeature")
            {
                Debug.Log("[LightingSetup] CRTDisplayRendererFeature already exists");
                return;
            }
        }

        var featureInstance = ScriptableObject.CreateInstance<CRTDisplayRendererFeature>();
        featureInstance.name = "CRTDisplayRendererFeature";
        AssetDatabase.AddObjectToAsset(featureInstance, rendererPath);
        featuresList.Add(featureInstance);
        EditorUtility.SetDirty(rendererData);
        EditorUtility.SetDirty(featureInstance);
        AssetDatabase.SaveAssets();
        Debug.Log("[LightingSetup] CRTDisplayRendererFeature added to Renderer2D");
    }
}


using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public static class SetupPixelFisheye
{
    [MenuItem("Tools/Setup Pixel+Fisheye Effect")]
    public static void Setup()
    {
        string rendererPath = "Assets/Settings/Renderer2D.asset";
        var rendererData = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(rendererPath);
        if (rendererData == null)
        {
            Debug.LogError("Could not load Renderer2D at " + rendererPath);
            return;
        }
        Debug.Log("Loaded renderer: " + rendererData.GetType().FullName);

        var rendererType = rendererData.GetType();
        var featuresField = rendererType.GetField("m_RendererFeatures", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (featuresField == null)
        {
            Debug.LogError("Could not find m_RendererFeatures field");
            return;
        }
        var featuresList = featuresField.GetValue(rendererData) as System.Collections.IList;
        if (featuresList == null)
        {
            Debug.LogError("Features list is null");
            return;
        }

        foreach (var feature in featuresList)
        {
            if (feature != null && feature.GetType().Name == "PixelFisheyeRendererFeature")
            {
                Debug.Log("PixelFisheyeRendererFeature already exists.");
                return;
            }
        }

        var featureInstance = ScriptableObject.CreateInstance<PixelFisheyeRendererFeature>();
        featureInstance.name = "PixelFisheyeRendererFeature";
        
        // Add as sub-asset so it gets serialized
        AssetDatabase.AddObjectToAsset(featureInstance, rendererPath);
        
        featuresList.Add(featureInstance);
        EditorUtility.SetDirty(rendererData);
        EditorUtility.SetDirty(featureInstance);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("SUCCESS: PixelFisheyeRendererFeature added to Renderer2D.asset as sub-asset");
    }
}

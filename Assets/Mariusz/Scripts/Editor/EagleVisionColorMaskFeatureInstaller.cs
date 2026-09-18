#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public static class EagleVisionColorMaskFeatureInstaller
{
    private const string FeatureName = "Eagle Vision Color Mask";
    private const string ShaderPath = "Assets/Mariusz/NOWE/VFXGraph/EagleVisionColorMask.shader";

    [InitializeOnLoadMethod]
    private static void ScheduleInstall()
    {
        EditorApplication.delayCall += InstallOnGameplayRenderers;
    }

    [MenuItem("Tools/Eagle Vision/Install Color Mask Renderer Feature")]
    public static void InstallOnGameplayRenderers()
    {
        Shader colorMaskShader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
        if (colorMaskShader == null)
        {
            Debug.LogError("Eagle Vision Color Mask: shader was not found at " + ShaderPath);
            return;
        }

        string[] rendererGuids = AssetDatabase.FindAssets("t:UniversalRendererData", new[] { "Assets/Settings", "Assets" });
        bool changed = false;

        foreach (string guid in rendererGuids.Distinct())
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!IsGameplayRendererPath(path))
                continue;

            UniversalRendererData rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
            if (rendererData == null)
                continue;

            EagleVisionColorMaskRendererFeature feature = rendererData.rendererFeatures
                .OfType<EagleVisionColorMaskRendererFeature>()
                .FirstOrDefault();
            if (feature == null)
            {
                feature = ScriptableObject.CreateInstance<EagleVisionColorMaskRendererFeature>();
                feature.name = FeatureName;
                AssetDatabase.AddObjectToAsset(feature, rendererData);
                rendererData.rendererFeatures.Add(feature);
                changed = true;
            }

            SerializedObject serializedFeature = new SerializedObject(feature);
            SerializedProperty shaderProperty = serializedFeature.FindProperty("colorMaskShader");
            if (shaderProperty.objectReferenceValue != colorMaskShader)
            {
                shaderProperty.objectReferenceValue = colorMaskShader;
                serializedFeature.ApplyModifiedPropertiesWithoutUndo();
                changed = true;
            }

            rendererData.SetDirty();
            EditorUtility.SetDirty(feature);
            EditorUtility.SetDirty(rendererData);
        }

        if (changed)
        {
            AssetDatabase.SaveAssets();
            Debug.Log("Eagle Vision Color Mask: renderer feature added to gameplay renderer assets.");
        }
    }

    private static bool IsGameplayRendererPath(string path)
    {
        if (path.StartsWith("Assets/Settings/") && path.EndsWith("Renderer.asset"))
            return true;

        return path == "Assets/Eagle Renderer.asset";
    }
}
#endif

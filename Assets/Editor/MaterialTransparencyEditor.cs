using UnityEditor;
using UnityEngine;
using System.IO;

public class MaterialTransparencyEditor : EditorWindow
{
    private string folderPath = "Assets/Materials/ObjectsMats"; // Default folder path
    private float newAlpha = 0.5f; // Default alpha value

    [MenuItem("Tools/Change Material Transparency")]
    public static void ShowWindow()
    {
        GetWindow<MaterialTransparencyEditor>("Change Material Transparency");
    }

    void OnGUI()
    {
        GUILayout.Label("Transparency Settings", EditorStyles.boldLabel);

        folderPath = EditorGUILayout.TextField("Folder Path", folderPath);
        newAlpha = EditorGUILayout.Slider("Alpha (0.0 - 1.0)", newAlpha, 0f, 1f);

        if (GUILayout.Button("Apply Transparency"))
        {
            ApplyTransparencyToMaterials(folderPath, newAlpha);
        }
    }

    void ApplyTransparencyToMaterials(string path, float alpha)
    {
        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { path });

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);

            if (mat.HasProperty("_Color"))
            {
                Color col = mat.color;
                col.a = alpha;
                mat.color = col;

                // Ensure shader supports transparency
                SetMaterialToTransparent(mat);

                EditorUtility.SetDirty(mat);
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Updated {guids.Length} material(s) in '{path}' with alpha = {alpha}");
    }

    void SetMaterialToTransparent(Material mat)
    {
        mat.SetFloat("_Mode", 3);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }
}
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>Cria copias de runtime dos dois assets autorais recebidos para a arena.</summary>
[InitializeOnLoad]
public static class ArenaAssetPrefabBuilder
{
    private const string BuildingSource = "Assets/MeshyImports/shopping-center-building_20260910_174914/Meshy_AI_shopping_center_build_0910205336_image-to-3d-texture.fbx";
    private const string TreeSource = "Assets/MeshyImports/Meshy_Model_20260910_175439/Meshy_AI_Extract_and_reimagine_0910205917_texture.fbx";

    static ArenaAssetPrefabBuilder() => EditorApplication.delayCall += Build;

    [MenuItem("UnDFight/Rebuild Arena Runtime Assets")]
    public static void Build()
    {
        CreateRuntimePrefab(BuildingSource, "Assets/Resources/Arena/DistanceBuilding.prefab");
        CreateRuntimePrefab(TreeSource, "Assets/Resources/Arena/FeaturedTree.prefab");
        AssetDatabase.SaveAssets();
    }

    private static void CreateRuntimePrefab(string sourcePath, string prefabPath)
    {
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
        if (source == null || AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null) return;
        System.IO.Directory.CreateDirectory("Assets/Resources/Arena");
        PrefabUtility.SaveAsPrefabAsset(source, prefabPath);
    }
}
#endif

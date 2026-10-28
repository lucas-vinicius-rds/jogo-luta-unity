using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Reproducible, scoped lighting pass for stage environment prefabs.
/// It only configures the existing directional light in each stage prefab;
/// it does not touch scenes, URP assets, stage layout, or authored materials.
/// </summary>
public static class StageGraphicsUpgrade
{
    private const string StageFolder = "Assets/Stages";

    [MenuItem("Tools/UnDFight/Graphics/Upgrade Stage Lighting")]
    public static void UpgradeStageLighting()
    {
        string[] stageGuids = AssetDatabase.FindAssets("t:StageDefinition", new[] { StageFolder });
        var updated = new List<string>();
        var skipped = new List<string>();

        foreach (string guid in stageGuids)
        {
            string definitionPath = AssetDatabase.GUIDToAssetPath(guid);
            var definition = AssetDatabase.LoadAssetAtPath<StageDefinition>(definitionPath);
            if (definition == null || definition.environmentPrefab == null)
            {
                skipped.Add(definitionPath + " (sem prefab de ambiente)");
                continue;
            }

            string prefabPath = AssetDatabase.GetAssetPath(definition.environmentPrefab);
            if (string.IsNullOrEmpty(prefabPath) || !prefabPath.StartsWith(StageFolder + "/"))
            {
                skipped.Add(definitionPath + " (prefab fora de Assets/Stages)");
                continue;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                Light sun = FindDirectionalLight(root.transform);
                if (sun == null)
                {
                    skipped.Add(definition.stableId + " (sem luz direcional existente)");
                    continue;
                }

                ConfigureDirectionalLight(sun);
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                updated.Add(definition.stableId + " → " + prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[StageGraphicsUpgrade] Atualizados: " + string.Join(", ", updated));
        if (skipped.Count > 0)
            Debug.LogWarning("[StageGraphicsUpgrade] Ignorados: " + string.Join(", ", skipped));
        // Console feedback also works in unattended Editor verification; no modal dialog.
    }

    [MenuItem("Tools/UnDFight/Graphics/Audit Stage Lighting")]
    public static void AuditStageLighting()
    {
        string[] stageGuids = AssetDatabase.FindAssets("t:StageDefinition", new[] { StageFolder });
        foreach (string guid in stageGuids)
        {
            string definitionPath = AssetDatabase.GUIDToAssetPath(guid);
            var definition = AssetDatabase.LoadAssetAtPath<StageDefinition>(definitionPath);
            if (definition == null || definition.environmentPrefab == null)
            {
                Debug.LogWarning("[StageGraphicsUpgrade] Stage sem prefab: " + definitionPath);
                continue;
            }

            string prefabPath = AssetDatabase.GetAssetPath(definition.environmentPrefab);
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                int directional = 0;
                int local = 0;
                int materialCount = 0;
                var seenMaterials = new HashSet<Material>();
                foreach (Light light in root.GetComponentsInChildren<Light>(true))
                {
                    if (light.type == LightType.Directional) directional++;
                    else local++;
                }
                foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                    foreach (Material material in renderer.sharedMaterials)
                        if (material != null && seenMaterials.Add(material)) materialCount++;

                Debug.Log("[StageGraphicsUpgrade] " + definition.stableId +
                    ": directional=" + directional + ", local=" + local +
                    ", shared materials=" + materialCount + ", prefab=" + prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        Debug.Log("[StageGraphicsUpgrade] PC URP: sombras suaves e AO SSAO estão disponíveis. " +
            "Mobile URP: sombras suaves e sombras de luzes adicionais estão desativadas; " +
            "a luz direcional usa somente a sombra principal suportada pelo perfil.");
    }

    private static Light FindDirectionalLight(Transform root)
    {
        foreach (Light light in root.GetComponentsInChildren<Light>(true))
            if (light.type == LightType.Directional)
                return light;
        return null;
    }

    private static void ConfigureDirectionalLight(Light sun)
    {
        sun.shadows = LightShadows.Soft;
        sun.shadowResolution = LightShadowResolution.Medium;
        EditorUtility.SetDirty(sun);
    }
}

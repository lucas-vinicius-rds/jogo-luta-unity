using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class CombatPresentationRound
{
    [MenuItem("Tools/UnDFight/Presentation/Prepare Authored Round")]
    public static void Prepare()
    {
        if(EditorApplication.isPlaying) throw new InvalidOperationException("Prepare outside Play Mode");
        if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Preserve unsaved scene changes before preparation");
        AuthoredFighterMotionBuilder.Generate();
        StageGraphicsUpgrade.UpgradeStageLighting();
        ConfigureBrandImport();
        UpgradeSurfaces();
        foreach(string path in new[]{"Assets/Scenes/FightingPrototype.unity","Assets/Scenes/RuaUrbana.unity"})
        {
            var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
            foreach(var camera in UnityEngine.Object.FindObjectsByType<Camera>())
            {
                var data=camera.GetUniversalAdditionalCameraData(); data.renderPostProcessing=true;
                data.antialiasing=AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                data.antialiasingQuality=AntialiasingQuality.High; data.dithering=true;
            }
            EditorSceneManager.SaveScene(scene);
        }
        EditorSceneManager.OpenScene("Assets/Scenes/FightingPrototype.unity",OpenSceneMode.Single);
        AssetDatabase.SaveAssets();
        Directory.CreateDirectory("Docs/validation/presentation");
        File.WriteAllText("Docs/validation/presentation/prepared.txt",DateTime.UtcNow.ToString("O")+"\nAuthored motion, stage shadows, surface normals, SMAA, brand assets prepared.");
    }
    static void ConfigureBrandImport()
    {
        foreach(string guid in AssetDatabase.FindAssets("t:Texture2D",new[]{"Assets/Resources/UI/Brand"}))
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid));
            importer.mipmapEnabled=false; importer.maxTextureSize=2048; importer.npotScale=TextureImporterNPOTScale.None;
            importer.textureCompression=TextureImporterCompression.CompressedHQ; importer.alphaIsTransparency=true; importer.SaveAndReimport();
        }
    }
    static void UpgradeSurfaces()
    {
        const string folder="Assets/Stages/Art/SurfaceNormals";
        Directory.CreateDirectory(folder); AssetDatabase.Refresh();
        var normal=AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/FineSurface.asset");
        if(normal==null)
        {
            const int size=128; normal=new Texture2D(size,size,TextureFormat.RGBA32,true,true) {name="FineSurfaceNormal",wrapMode=TextureWrapMode.Repeat};
            var colors=new Color[size*size];
            for(int y=0;y<size;y++) for(int x=0;x<size;x++)
            {
                float u=x/(float)size,v=y/(float)size;
                Func<float,float,float> h=(a,b)=>Mathf.Sin(a*Mathf.PI*16)*Mathf.Cos(b*Mathf.PI*16)*.04f+Mathf.PerlinNoise(a*32,b*32)*.012f;
                var n=new Vector3((h(u-.008f,v)-h(u+.008f,v))*2,(h(u,v-.008f)-h(u,v+.008f))*2,1).normalized;
                colors[y*size+x]=new Color(n.x*.5f+.5f,n.y*.5f+.5f,n.z*.5f+.5f,1);
            }
            normal.SetPixels(colors); normal.Apply(); AssetDatabase.CreateAsset(normal,folder+"/FineSurface.asset");
        }
        foreach(string guid in AssetDatabase.FindAssets("t:Material",new[]{"Assets/Stages/Art"}))
        {
            var material=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
            if(material.shader.name!="Universal Render Pipeline/Lit" || !material.HasProperty("_BumpMap")) continue;
            string name=material.name.ToLowerInvariant();
            if(name.Contains("glass") || name.Contains("signal") || name.Contains("paper")) continue;
            material.SetTexture("_BumpMap",normal); material.SetFloat("_BumpScale",name.Contains("asphalt")?.35f:.2f); material.EnableKeyword("_NORMALMAP");
            EditorUtility.SetDirty(material);
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class StageManager : MonoBehaviour
{
    public StageRegistry Registry { get; private set; }
    public StageDefinition Selected { get; private set; }
    public StageEnvironmentController Loaded { get; private set; }
    public double LastLoadMilliseconds { get; private set; }
    readonly Dictionary<GameObject,bool> legacy = new Dictionary<GameObject,bool>();
    Color ambient, fogColor;
    bool fog, captured;
    float density, intensity;
    Material sky;
    Light sun;
    UnityEngine.Rendering.AmbientMode mode;
    public void Initialize(StageRegistry registry) => Registry = registry;
    public bool Select(string id)
    {
        if(GameFlowController.Instance != null && GameFlowController.Instance.IsFightScreen) return false;
        if(Registry == null || !Registry.TryGet(id, out var definition)) return false;
        if(Selected != definition) Unload();
        Selected = definition; return true;
    }
    public bool Prepare(Scene scene, FighterMovement movement, out string error)
    {
        error = null;
        if(Selected == null) return true;
        if(!Selected.environmentPrefab.Validate(out error) || !movement.MatchesStageBounds(Selected.environmentPrefab.combatBounds)) { error = error ?? "Limites incompatíveis com o combate"; return false; }
        if(Loaded != null && Loaded.gameObject.scene == scene) return true;
        Unload();
        ambient = RenderSettings.ambientLight; mode = RenderSettings.ambientMode; intensity = RenderSettings.ambientIntensity;
        fog = RenderSettings.fog; fogColor = RenderSettings.fogColor; density = RenderSettings.fogDensity; sky = RenderSettings.skybox; sun = RenderSettings.sun; captured = true;
        foreach(var root in scene.GetRootGameObjects())
        {
            var binding = root.GetComponent<StageSceneBinding>();
            if(binding == null) continue;
            foreach(var item in binding.legacyEnvironment) if(item != null) { legacy[item] = item.activeSelf; item.SetActive(false); }
        }
        var watch=System.Diagnostics.Stopwatch.StartNew();
        Loaded = Instantiate(Selected.environmentPrefab, Vector3.zero, Quaternion.identity);
        SceneManager.MoveGameObjectToScene(Loaded.gameObject, scene);
        Loaded.Apply(Selected); watch.Stop(); LastLoadMilliseconds=watch.Elapsed.TotalMilliseconds; return true;
    }
    public void UseLegacy() { Unload(); Selected = null; }
    public void Unload()
    {
        if(Loaded != null) { Loaded.gameObject.SetActive(false); Destroy(Loaded.gameObject); Loaded = null; }
        foreach(var pair in legacy) if(pair.Key != null) pair.Key.SetActive(pair.Value);
        legacy.Clear();
        if(!captured) return;
        RenderSettings.ambientLight = ambient; RenderSettings.ambientMode = mode; RenderSettings.ambientIntensity = intensity;
        RenderSettings.fog = fog; RenderSettings.fogColor = fogColor; RenderSettings.fogDensity = density; RenderSettings.skybox = sky; RenderSettings.sun = sun; captured = false;
    }
    void OnDestroy() => Unload();
}

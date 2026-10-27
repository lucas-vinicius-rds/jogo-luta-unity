using UnityEngine;

public sealed class StageEnvironmentController : MonoBehaviour
{
    public BoxCollider gameplaySurface;
    public Transform playerSpawn;
    public Transform opponentSpawn;
    public Bounds combatBounds;
    public GameObject effectsRoot;
    public bool Validate(out string error)
    {
        error = null;
        if(gameplaySurface == null || gameplaySurface.isTrigger || playerSpawn == null || opponentSpawn == null) error = "Faltam superfície sólida ou spawns";
        else if(transform.localScale != Vector3.one || playerSpawn.position.x >= opponentSpawn.position.x) error = "Escala ou ordem dos spawns inválida";
        else if(!combatBounds.Contains(playerSpawn.position) || !combatBounds.Contains(opponentSpawn.position)) error = "Spawn fora dos limites";
        else foreach(var c in GetComponentsInChildren<Collider>(true)) if(c.enabled && !c.isTrigger && c != gameplaySurface) { error = "Colisor decorativo: " + c.name; break; }
        return error == null;
    }
    public void Apply(StageDefinition stage)
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = stage.ambient;
        RenderSettings.fog = stage.fog;
        RenderSettings.fogColor = stage.fogColor;
        RenderSettings.fogDensity = stage.fogDensity;
        RenderSettings.skybox = stage.skybox;
        var sun = GetComponentInChildren<Light>();
        if(sun != null && sun.type == LightType.Directional) RenderSettings.sun = sun;
        if(stage.ambience != null && Application.isPlaying)
        {
            var audio = gameObject.AddComponent<AudioSource>();
            audio.clip = stage.ambience; audio.loop = true; audio.volume = stage.volume; audio.spatialBlend = 0; audio.Play();
        }
    }
}

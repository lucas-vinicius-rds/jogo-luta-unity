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
        else if(gameplaySurface.transform.rotation != Quaternion.identity) error = "Superfície deve estar nivelada";
        else
        {
            Vector3 center = gameplaySurface.transform.TransformPoint(gameplaySurface.center);
            Vector3 size = Vector3.Scale(gameplaySurface.size, gameplaySurface.transform.lossyScale);
            Bounds floor = new Bounds(center,size);
            if(floor.min.x > combatBounds.min.x || floor.max.x < combatBounds.max.x || floor.min.z > combatBounds.min.z || floor.max.z < combatBounds.max.z || Mathf.Abs(floor.max.y - playerSpawn.position.y) > .05f || Mathf.Abs(playerSpawn.position.y - opponentSpawn.position.y) > .01f) error = "Superfície não cobre limites/spawns";
        }
        if(error == null)
            foreach(var c in GetComponentsInChildren<Collider>(true)) if(c.enabled && !c.isTrigger && c != gameplaySurface) { error = "Colisor decorativo: " + c.name; break; }
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
        foreach(var light in GetComponentsInChildren<Light>()) if(light.type == LightType.Directional) { RenderSettings.sun=light; break; }
        if(stage.ambience != null && Application.isPlaying)
        {
            var audio = gameObject.AddComponent<AudioSource>();
            audio.clip = stage.ambience; audio.loop = true; audio.volume = stage.volume; audio.spatialBlend = 0; audio.Play();
        }
    }
}

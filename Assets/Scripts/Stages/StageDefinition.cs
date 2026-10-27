using UnityEngine;

[CreateAssetMenu(menuName = "UnDFight/Stage")]
public sealed class StageDefinition : ScriptableObject
{
    public string stableId;
    public string displayName;
    [TextArea] public string description;
    public Texture2D preview;
    public StageEnvironmentController environmentPrefab;
    public AudioClip ambience;
    public bool available = true;
    public int sortOrder;
    public Color ambient = new Color(.4f,.4f,.4f);
    public Color fogColor = Color.gray;
    public bool fog;
    [Min(0)] public float fogDensity = .012f;
    public Material skybox;
    [Range(0,1)] public float volume = .3f;
}

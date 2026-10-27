using UnityEngine;
public sealed class StageAmbientMotion : MonoBehaviour
{
    public bool sway;
    public float speed=1;
    Quaternion initial;
    void Awake() => initial = transform.localRotation;
    void Update() { float t=Time.time*speed; transform.localRotation = sway ? initial * Quaternion.Euler(0,Mathf.Sin(t*1.8f)*3,Mathf.Sin(t)*2) : initial * Quaternion.Euler(0,t*35,0); }
}

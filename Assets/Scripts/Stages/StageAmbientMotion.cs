using UnityEngine;
public sealed class StageAmbientMotion : MonoBehaviour
{
    public bool sway;
    Quaternion initial;
    void Awake() => initial = transform.localRotation;
    void Update() { transform.localRotation = sway ? initial * Quaternion.Euler(0,Mathf.Sin(Time.time*1.8f)*3,Mathf.Sin(Time.time)*2) : initial * Quaternion.Euler(0,Time.time*35,0); }
}

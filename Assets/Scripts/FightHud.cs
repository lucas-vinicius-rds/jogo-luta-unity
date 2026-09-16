using UnityEngine;

/// <summary>
/// HUD de combate independente do painel de depuração. Usa dimensões relativas para
/// permanecer legível em resoluções diferentes, sem depender da interface IMGUI dos lutadores.
/// </summary>
[DisallowMultipleComponent]
public sealed class FightHud : MonoBehaviour
{
    private GameFlowController flow;
    private GUIStyle nameStyle;
    private GUIStyle controlStyle;
    private GUIStyle pauseTitleStyle;
    private GUIStyle pauseButtonStyle;
    private float lastP1Health = -1f;
    private float lastP2Health = -1f;
    private float hitFlashUntil;
    private AudioSource feedbackAudio;
    private AudioClip lightHitClip;
    private AudioClip koClip;

    public void Initialize(GameFlowController owner) => flow = owner;

    private void Awake()
    {
        feedbackAudio = gameObject.AddComponent<AudioSource>();
        feedbackAudio.playOnAwake = false;
        feedbackAudio.volume = .32f;
        lightHitClip = CreateTone("UnDFightHit", 115f, .075f, .14f);
        koClip = CreateTone("UnDFightKO", 58f, .34f, .22f);
    }

    private void Update()
    {
        if (flow == null) return;
        TrackHealth(flow.CurrentPlayer, ref lastP1Health);
        TrackHealth(flow.CurrentOpponent, ref lastP2Health);
    }

    private void TrackHealth(FighterController fighter, ref float previous)
    {
        if (fighter == null || fighter.HealthSystem == null) { previous = -1f; return; }
        float current = fighter.HealthSystem.CurrentHealth;
        if (previous >= 0f && current < previous)
        {
            hitFlashUntil = Time.unscaledTime + .12f;
            if (feedbackAudio != null) feedbackAudio.PlayOneShot(current <= 0f ? koClip : lightHitClip);
        }
        previous = current;
    }

    private static AudioClip CreateTone(string clipName, float frequency, float duration, float volume)
    {
        const int sampleRate = 44100;
        int samples = Mathf.CeilToInt(sampleRate * duration);
        AudioClip clip = AudioClip.Create(clipName, samples, 1, sampleRate, false);
        float[] data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float progress = i / (float)samples;
            float envelope = (1f - progress) * (1f - progress);
            data[i] = Mathf.Sin(2f * Mathf.PI * frequency * i / sampleRate) * envelope * volume;
        }
        clip.SetData(data, 0);
        return clip;
    }

    private void OnGUI()
    {
        if (flow == null || flow.CurrentPlayer == null || flow.CurrentOpponent == null) return;
        BuildStyles();
        DrawFighterBar(flow.CurrentPlayer, "P1", new Rect(Screen.width * .045f, Screen.height * .035f, Screen.width * .34f, Screen.height * .045f), false);
        DrawFighterBar(flow.CurrentOpponent, "P2", new Rect(Screen.width * .615f, Screen.height * .035f, Screen.width * .34f, Screen.height * .045f), true);

        GUI.Label(new Rect(0f, Screen.height * .09f, Screen.width, 24f),
            "P1: WASD • ESPAÇO / CTRL • SHIFT guarda     |     P2: SETAS • J / K • L guarda     |     ESC pausa", controlStyle);

        if (Time.unscaledTime < hitFlashUntil)
        {
            GUI.color = new Color(1f, .18f, .12f, .12f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        if (flow.IsFightPaused) DrawPause();
    }

    private void BuildStyles()
    {
        if (nameStyle != null) return;
        int nameSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height * .027f), 16, 30);
        nameStyle = new GUIStyle(GUI.skin.label) { fontSize = nameSize, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
        controlStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height * .018f), 12, 20), normal = { textColor = new Color(.8f, .88f, 1f) } };
        pauseTitleStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height * .07f), 34, 64), fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
        pauseButtonStyle = new GUIStyle(GUI.skin.button) { fontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height * .028f), 17, 30), fontStyle = FontStyle.Bold };
    }

    private void DrawFighterBar(FighterController fighter, string label, Rect rect, bool rightAligned)
    {
        HealthSystem health = fighter.HealthSystem;
        float hp = health != null ? health.HealthNormalized : 0f;
        Color fill = hp > .5f ? new Color(.08f, .75f, 1f) : (hp > .25f ? new Color(1f, .72f, .12f) : new Color(1f, .16f, .14f));
        GUI.Label(new Rect(rect.x, rect.y - 28f, rect.width, 28f), label + (fighter.IsGuarding ? "  • GUARDA" : ""), nameStyle);
        GUI.color = new Color(.01f, .03f, .08f, .94f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        float width = rect.width * hp;
        Rect fillRect = rightAligned ? new Rect(rect.xMax - width, rect.y + 4f, width, rect.height - 8f) : new Rect(rect.x + 4f, rect.y + 4f, Mathf.Max(0f, width - 4f), rect.height - 8f);
        GUI.color = fill;
        GUI.DrawTexture(fillRect, Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    private void DrawPause()
    {
        GUI.color = new Color(0f, .02f, .08f, .88f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(new Rect(0, Screen.height * .24f, Screen.width, 90f), "PAUSADO", pauseTitleStyle);
        float width = Mathf.Min(460f, Screen.width * .5f);
        float x = (Screen.width - width) * .5f;
        float y = Screen.height * .43f;
        if (GUI.Button(new Rect(x, y, width, 58f), "CONTINUAR", pauseButtonStyle)) flow.SetFightPaused(false);
        if (GUI.Button(new Rect(x, y + 76f, width, 58f), "REINICIAR LUTA", pauseButtonStyle)) flow.RestartFight();
        if (GUI.Button(new Rect(x, y + 152f, width, 58f), "ESCOLHER LUTADOR", pauseButtonStyle)) flow.ReturnToCharacterSelect();
    }
}

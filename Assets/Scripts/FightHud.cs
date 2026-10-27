using UnityEngine;

/// <summary>
/// HUD de combate independente do painel de depuração. Usa dimensões relativas para
/// permanecer legível em resoluções diferentes, sem depender da interface IMGUI dos lutadores.
/// </summary>
[DisallowMultipleComponent]
public sealed class FightHud : MonoBehaviour
{
    private const int CanvasWidth = 1280;
    private const int CanvasHeight = 720;
    private GameFlowController flow;
    private GUIStyle nameStyle;
    private GUIStyle controlStyle;
    private GUIStyle pauseTitleStyle;
    private GUIStyle pauseButtonStyle;
    private float hitFlashUntil;
    private AudioSource feedbackAudio;
    private AudioClip lightHitClip;
    private AudioClip koClip;
    private int styleHeight;
    private readonly FightHudState model = new FightHudState();

    public void Initialize(GameFlowController owner) => flow = owner;

    private void Awake()
    {
        model.Damaged += OnDamaged;
        feedbackAudio = gameObject.AddComponent<AudioSource>();
        feedbackAudio.playOnAwake = false;
        feedbackAudio.volume = .32f;
        lightHitClip = CreateTone("UnDFightHit", 115f, .075f, .14f);
        koClip = CreateTone("UnDFightKO", 58f, .34f, .22f);
    }

    private void Update()
    {
        if (flow == null) return;
        model.Bind(flow.CurrentPlayer, flow.CurrentOpponent);
    }
    private void OnDamaged(bool knockout) { hitFlashUntil = Time.unscaledTime + .12f; if(feedbackAudio != null) feedbackAudio.PlayOneShot(knockout ? koClip : lightHitClip); }
    private void OnDestroy() { model.Dispose(); model.Damaged -= OnDamaged; if(lightHitClip != null) Destroy(lightHitClip); if(koClip != null) Destroy(koClip); }

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
        Matrix4x4 previous = GUI.matrix;
        float scale = Mathf.Min(Screen.width / (float)CanvasWidth, Screen.height / (float)CanvasHeight);
        GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - CanvasWidth * scale) * .5f, (Screen.height - CanvasHeight * scale) * .5f), Quaternion.identity, Vector3.one * scale);
        DrawHud();
        GUI.matrix = previous;
    }

    private void DrawHud()
    {
        if (flow == null || !flow.IsCombatScreen || flow.CurrentPlayer == null || flow.CurrentOpponent == null) return;
        BuildStyles();
        DrawFighterBar(flow.CurrentPlayer, model.PlayerName, new Rect(CanvasWidth * .045f, CanvasHeight * .07f, CanvasWidth * .34f, CanvasHeight * .035f), false);
        DrawFighterBar(flow.CurrentOpponent, model.OpponentName, new Rect(CanvasWidth * .615f, CanvasHeight * .07f, CanvasWidth * .34f, CanvasHeight * .035f), true);

        GUI.Label(new Rect(0f, CanvasHeight - 50f, CanvasWidth, 46f),
            "P1  A/D mover · W salto · S agachar · F soco · G chute · SHIFT defesa\nP2  SETAS mover · J soco · K chute · L defesa     |     ESC pausa", controlStyle);

        if (Time.unscaledTime < hitFlashUntil)
        {
            GUI.color = new Color(1f, .18f, .12f, .12f);
            GUI.DrawTexture(new Rect(0, 0, CanvasWidth, CanvasHeight), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        if (flow.IsFightPaused) DrawPause();
    }

    private void BuildStyles()
    {
        if (nameStyle != null && styleHeight == CanvasHeight) return;
        styleHeight = CanvasHeight;
        int nameSize = Mathf.Clamp(Mathf.RoundToInt(CanvasHeight * .027f), 16, 30);
        nameStyle = new GUIStyle(GUI.skin.label) { fontSize = nameSize, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
        controlStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.Clamp(Mathf.RoundToInt(CanvasHeight * .018f), 12, 20), normal = { textColor = new Color(.8f, .88f, 1f) } };
        controlStyle.wordWrap = true;
        pauseTitleStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.Clamp(Mathf.RoundToInt(CanvasHeight * .07f), 34, 64), fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
        pauseButtonStyle = new GUIStyle(GUI.skin.button) { fontSize = Mathf.Clamp(Mathf.RoundToInt(CanvasHeight * .028f), 17, 30), fontStyle = FontStyle.Bold };
    }

    private void DrawFighterBar(FighterController fighter, string label, Rect rect, bool rightAligned)
    {
        HealthSystem health = fighter.HealthSystem;
        float hp = rightAligned ? model.OpponentRatio : model.PlayerRatio;
        Color fill = hp > .25f ? new Color(1f, .78f, .16f) : new Color(1f, .16f, .14f);
        nameStyle.alignment = rightAligned ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
        float labelHeight = Mathf.Max(30f, nameStyle.fontSize + 8f);
        GUI.Label(new Rect(rect.x, rect.y - labelHeight, rect.width, labelHeight), label, nameStyle);
        GUI.color = new Color(.01f, .03f, .08f, .94f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        float width = Mathf.Max(0f, rect.width - 8f) * Mathf.Clamp01(hp);
        Rect fillRect = new Rect(rightAligned ? rect.xMax - 4f - width : rect.x + 4f, rect.y + 4f, width, Mathf.Max(1f, rect.height - 8f));
        GUI.color = fill;
        GUI.DrawTexture(fillRect, Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    private void DrawPause()
    {
        GUI.color = new Color(0f, .02f, .08f, .88f);
        GUI.DrawTexture(new Rect(0, 0, CanvasWidth, CanvasHeight), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(new Rect(0, CanvasHeight * .24f, CanvasWidth, 90f), "PAUSADO", pauseTitleStyle);
        float width = Mathf.Min(460f, CanvasWidth * .5f);
        float x = (CanvasWidth - width) * .5f;
        float y = CanvasHeight * .43f;
        if (GUI.Button(new Rect(x, y, width, 58f), "CONTINUAR", pauseButtonStyle)) flow.SetFightPaused(false);
        if (GUI.Button(new Rect(x, y + 76f, width, 58f), "REINICIAR LUTA", pauseButtonStyle)) flow.RestartFight();
        if (GUI.Button(new Rect(x, y + 152f, width, 58f), "ESCOLHER LUTADOR", pauseButtonStyle)) flow.ReturnToCharacterSelect();
    }
}

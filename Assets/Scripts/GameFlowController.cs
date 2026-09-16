using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>Fluxo UnDFight: menu principal, modo, seleção, VS e luta.</summary>
public sealed class GameFlowController : MonoBehaviour
{
    private enum FlowScreen { Main, Mode, Select, VS, Fight, Victory }
    private enum GameMode { Cpu, Local }

    [SerializeField] private GameObject[] characterPrefabs;
    [SerializeField] private string fightSceneName = "FightingPrototype";
    [SerializeField] private AnimationClip[] victoryClips;
    [SerializeField] private AnimationClip defeatedClip;

    private static GameFlowController instance;
    private static GameMode pendingMode;
    private static int pendingPlayer;
    private static int pendingOpponent;
    private static bool pendingFight;
    private FlowScreen screen = FlowScreen.Main;
    private GameMode mode;
    private int playerSelection;
    private int opponentSelection = 1;
    private int focusedSelection;
    private bool choosingOpponent;
    private bool playerOneConfirmed;
    private float confirmationPulse;
    private float vsUntil;
    private bool victoryShown;
    private FighterController currentPlayer;
    private FighterController currentOpponent;
    private string victoryWinnerName;
    private string victoryLoserName;
    private int victoryOption;
    private GameObject victoryWinnerObject;
    private GameObject victoryLoserObject;
    private Camera victoryWinnerCamera;
    private Camera victoryLoserCamera;
    private RenderTexture victoryWinnerTexture;
    private RenderTexture victoryLoserTexture;
    private int previewPlayer = -1;
    private int previewOpponent = -1;
    private GameObject previewPlayerObject;
    private GameObject previewOpponentObject;
    private Camera previewPlayerCamera;
    private Camera previewOpponentCamera;
    private RenderTexture previewPlayerTexture;
    private RenderTexture previewOpponentTexture;
    private GameObject[] cardPreviewObjects;
    private Camera[] cardPreviewCameras;
    private RenderTexture[] cardPreviewTextures;
    private Texture2D backgroundTexture;
    private AudioSource musicSource;
    private GUIStyle titleStyle;
    private GUIStyle subtitleStyle;
    private GUIStyle buttonStyle;
    private GUIStyle cardStyle;
    private GUIStyle selectedCardStyle;
    private GUIStyle labelStyle;

    private static readonly string[] DisplayNames =
    {
        "Tiago", "Isaac", "Lucas", "Enomoto", "Jompi", "Gabutas", "Ricardinho"
    };

    private static readonly string[] FighterClasses =
    {
        "EQUILIBRADO", "VELOCISTA", "BRIGADOR", "ESTRATEGISTA",
        "ACROBATA", "PESO-PESADO", "IMPREVISIVEL"
    };

    private static readonly string[] FighterDescriptions =
    {
        "Lutador versatil, com bons ataques e defesa solida.",
        "Rapido e agil, domina o espaco com movimentos velozes.",
        "Pressao constante e golpes diretos de curta distancia.",
        "Controla o ritmo da luta e pune os erros do adversario.",
        "Movimentos amplos e acrobaticos, dificeis de prever.",
        "Ataques fortes e grande resistencia, mas menor velocidade.",
        "Estilo incomum que alterna velocidade, alcance e potencia."
    };

    private static readonly int[,] FighterStats =
    {
        { 4, 3, 4, 3, 4 }, { 3, 5, 3, 4, 3 }, { 4, 3, 4, 3, 3 },
        { 3, 3, 4, 4, 5 }, { 4, 4, 3, 4, 4 }, { 5, 2, 5, 3, 3 },
        { 4, 4, 3, 4, 4 }
    };

    private static readonly string[] StatNames = { "FORCA", "VELOCIDADE", "DEFESA", "ALCANCE", "TECNICA" };

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        screen = pendingFight ? FlowScreen.VS : FlowScreen.Main;
        if (!pendingFight) RemoveSceneFighters();
        StartCoroutine(LoadMenuAssets());
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        DestroyPreview(ref previewPlayerObject, ref previewPlayerCamera, ref previewPlayerTexture);
        DestroyPreview(ref previewOpponentObject, ref previewOpponentCamera, ref previewOpponentTexture);
        DestroyCardPreviews();
        DestroyPreview(ref victoryWinnerObject, ref victoryWinnerCamera, ref victoryWinnerTexture);
        DestroyPreview(ref victoryLoserObject, ref victoryLoserCamera, ref victoryLoserTexture);
    }

    private IEnumerator LoadMenuAssets()
    {
        string imagePath = Path.Combine(Application.dataPath, "telaInicial.png");
        if (File.Exists(imagePath))
        {
            byte[] data = File.ReadAllBytes(imagePath);
            backgroundTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            backgroundTexture.LoadImage(data);
        }

        string musicPath = Path.Combine(Application.dataPath, "eltema.mp3");
        if (File.Exists(musicPath))
        {
            using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip("file:///" + musicPath.Replace("\\", "/"), AudioType.MPEG))
            {
                yield return request.SendWebRequest();
                if (request.result == UnityWebRequest.Result.Success)
                {
                    musicSource = gameObject.AddComponent<AudioSource>();
                    musicSource.clip = DownloadHandlerAudioClip.GetContent(request);
                    musicSource.loop = true;
                    musicSource.volume = 0.35f;
                    musicSource.Play();
                }
            }
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode loadMode)
    {
        if (!pendingFight || scene.name != fightSceneName) return;
        SetupFight();
        pendingFight = false;
        screen = FlowScreen.VS;
        vsUntil = Time.unscaledTime + 1.5f;
    }

    private void Update()
    {
        if (playerOneConfirmed) confirmationPulse += Time.unscaledDeltaTime;
        if (screen == FlowScreen.VS && Time.unscaledTime >= vsUntil) screen = FlowScreen.Fight;
        if (screen == FlowScreen.Victory)
        {
            if (Input.GetKeyDown(KeyCode.UpArrow)) victoryOption = Mathf.Max(0, victoryOption - 1);
            if (Input.GetKeyDown(KeyCode.DownArrow)) victoryOption = Mathf.Min(2, victoryOption + 1);
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) ConfirmVictoryOption();
            return;
        }
        if (screen != FlowScreen.Select) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (choosingOpponent) choosingOpponent = false;
            else screen = FlowScreen.Mode;
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow)) MoveFocus(-1, 0);
        if (Input.GetKeyDown(KeyCode.RightArrow)) MoveFocus(1, 0);
        if (Input.GetKeyDown(KeyCode.UpArrow)) MoveFocus(0, -1);
        if (Input.GetKeyDown(KeyCode.DownArrow)) MoveFocus(0, 1);
        if (Input.GetKeyDown(KeyCode.R)) SelectRandomFighter();
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) ConfirmCurrentSelection();
        if (screen == FlowScreen.Select) RefreshPreviews();
    }

    private void OnGUI()
    {
        if (screen == FlowScreen.Fight) return;
        BuildStyles();
        DrawBackdrop();
        if (screen == FlowScreen.Main) DrawMain();
        else if (screen == FlowScreen.Mode) DrawMode();
        else if (screen == FlowScreen.Select) DrawSelection();
        else if (screen == FlowScreen.Victory) DrawVictory();
        else DrawVS();
    }

    private void BuildStyles()
    {
        if (titleStyle != null) return;
        titleStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.Max(34, Screen.height / 15), fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
        subtitleStyle = new GUIStyle(titleStyle) { fontSize = Mathf.Max(15, Screen.height / 38), normal = { textColor = new Color(.55f, .85f, 1f) } };
        buttonStyle = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.Max(16, Screen.height / 42), fontStyle = FontStyle.Bold, fixedHeight = 78, normal = { textColor = Color.white } };
        cardStyle = new GUIStyle(buttonStyle) { fontSize = Mathf.Max(15, Screen.height / 42), fixedHeight = 0, stretchHeight = false, normal = { textColor = Color.white } };
        selectedCardStyle = new GUIStyle(cardStyle) { fontSize = cardStyle.fontSize + 1, normal = { textColor = Color.white } };
        labelStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 18, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
    }

    private void DrawBackdrop()
    {
        Rect full = new Rect(0, 0, Screen.width, Screen.height);
        if (backgroundTexture != null) GUI.DrawTexture(full, backgroundTexture, ScaleMode.ScaleAndCrop);
        else { GUI.color = new Color(.015f, .04f, .12f, 1f); GUI.DrawTexture(full, Texture2D.whiteTexture); GUI.color = Color.white; }
        GUI.color = new Color(.01f, .05f, .13f, .72f); GUI.DrawTexture(full, Texture2D.whiteTexture); GUI.color = Color.white;
    }

    private Rect CenteredButton(int width, int y) => new Rect((Screen.width - width) * .5f, y, width, 78);

    private void DrawMain()
    {
        GUI.Label(new Rect(0, Screen.height * .18f, Screen.width, 80), "UnDFight", titleStyle);
        GUI.Label(new Rect(0, Screen.height * .18f + 78, Screen.width, 35), "THE FIGHT STARTS HERE", subtitleStyle);
        if (GUI.Button(CenteredButton(340, (int)(Screen.height * .57f)), "VERSUS", buttonStyle)) screen = FlowScreen.Mode;
        if (GUI.Button(CenteredButton(340, (int)(Screen.height * .57f) + 96), "SAIR", buttonStyle)) Application.Quit();
    }

    private void DrawMode()
    {
        GUI.Label(new Rect(0, 95, Screen.width, 70), "VERSUS", titleStyle);
        int y = Mathf.Max(210, Screen.height / 3);
        if (GUI.Button(CenteredButton(430, y), "PLAYER VS CPU", buttonStyle)) BeginSelection(GameMode.Cpu);
        if (GUI.Button(CenteredButton(430, y + 96), "PLAYER VS PLAYER LOCAL", buttonStyle)) BeginSelection(GameMode.Local);
        if (GUI.Button(CenteredButton(250, y + 200), "VOLTAR", buttonStyle)) screen = FlowScreen.Main;
    }

    private void BeginSelection(GameMode selectedMode)
    {
        mode = selectedMode;
        playerSelection = 0;
        opponentSelection = characterPrefabs != null && characterPrefabs.Length > 1 ? 1 : 0;
        focusedSelection = playerSelection;
        choosingOpponent = false;
        playerOneConfirmed = false;
        confirmationPulse = 0f;
        previewPlayer = previewOpponent = -1;
        screen = FlowScreen.Select;
        SetMainCameraPreviewVisibility(false);
        RefreshPreviews();
        CreateCardPreviews();
    }

    private void DrawSelection()
    {
        const float canvasWidth = 1536f;
        const float canvasHeight = 864f;
        float scale = Mathf.Min(Screen.width / canvasWidth, Screen.height / canvasHeight);
        Vector2 offset = new Vector2((Screen.width - canvasWidth * scale) * .5f, (Screen.height - canvasHeight * scale) * .5f);
        Matrix4x4 oldMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.TRS(offset, Quaternion.identity, new Vector3(scale, scale, 1f));

        GUIStyle selectTitle = new GUIStyle(titleStyle) { alignment = TextAnchor.MiddleLeft, fontSize = 54 };
        GUIStyle selectSubtitle = new GUIStyle(subtitleStyle) { alignment = TextAnchor.MiddleLeft, fontSize = 19 };
        GUI.Label(new Rect(42, 28, 650, 62), "SELECT FIGHTER", selectTitle);
        GUI.Label(new Rect(46, 83, 430, 30), "ESCOLHA SEU LUTADOR", selectSubtitle);
        int visiblePlayer = choosingOpponent ? playerSelection : focusedSelection;
        int visibleOpponent = choosingOpponent ? focusedSelection : opponentSelection;
        DrawSelectionPreview(new Rect(52, 166, 350, 520), visiblePlayer, "PLAYER 1", new Color(.04f, .48f, 1f), previewPlayerTexture);
        DrawSelectionPreview(new Rect(1134, 166, 350, 520), visibleOpponent, mode == GameMode.Cpu ? "CPU" : "PLAYER 2", new Color(.95f, .2f, .2f), previewOpponentTexture);

        const float gridX = 426f;
        const float gridY = 166f;
        const float cardWidth = 218f;
        const float cardHeight = 190f;
        const float gap = 10f;
        bool mouseChangedFocus = false;
        for (int i = 0; i < characterPrefabs.Length; i++)
        {
            int row = i / 3, col = i % 3;
            float x = gridX + col * (cardWidth + gap);
            if (i == 6) x = gridX + cardWidth + gap;
            Rect rect = new Rect(x, gridY + row * (cardHeight + gap), cardWidth, cardHeight);
            bool focused = focusedSelection == i;
            bool blocked = choosingOpponent && i == playerSelection;
            Vector2 virtualMouse = GUI.matrix.inverse.MultiplyPoint(Event.current.mousePosition);
            if (!blocked && rect.Contains(virtualMouse) && focusedSelection != i)
            {
                focusedSelection = i;
                mouseChangedFocus = true;
                focused = true;
            }
            GUI.backgroundColor = blocked ? new Color(.12f, .14f, .18f) : focused ? new Color(.04f, .5f, 1f) : new Color(.025f, .09f, .16f);
            if (GUI.Button(rect, GUIContent.none, focused ? selectedCardStyle : cardStyle) && !blocked) focusedSelection = i;
            if (cardPreviewTextures != null && i < cardPreviewTextures.Length && cardPreviewTextures[i] != null)
                GUI.DrawTexture(new Rect(rect.x + 6, rect.y + 5, rect.width - 12, rect.height - 39), cardPreviewTextures[i], ScaleMode.ScaleAndCrop, true);
            GUIStyle cardNameStyle = new GUIStyle(labelStyle) { fontSize = 19 };
            GUI.Label(new Rect(rect.x + 5, rect.yMax - 35, rect.width - 10, 31), blocked ? "INDISPONIVEL" : DisplayNames[i].ToUpperInvariant(), cardNameStyle);
            DrawCardOutline(rect, blocked ? new Color(.25f, .55f, .6f) : focused ? new Color(.15f, .8f, 1f) : new Color(.12f, .28f, .42f), focused || blocked ? 5f : 2f);
            if (focused)
            {
                GUI.color = choosingOpponent ? new Color(.95f, .2f, .2f) : new Color(.05f, .55f, 1f);
                GUI.DrawTexture(new Rect(rect.x, rect.y, 32, 25), Texture2D.whiteTexture);
                GUI.color = Color.white;
                GUI.Label(new Rect(rect.x, rect.y - 1, 32, 25), choosingOpponent ? "P2" : "P1", labelStyle);
            }
            GUI.backgroundColor = Color.white;
        }
        if (mouseChangedFocus) RefreshPreviews();
        string confirmText = choosingOpponent ? "CONFIRMAR LUTADOR" : "CONFIRMAR PLAYER 1";
        GUI.backgroundColor = new Color(.04f, .38f, .8f);
        GUIStyle bottomButtonStyle = new GUIStyle(buttonStyle) { fontSize = 18, fixedHeight = 0 };
        if (GUI.Button(new Rect(572, 774, 392, 66), "ENTER   " + confirmText, bottomButtonStyle)) ConfirmCurrentSelection();
        GUI.backgroundColor = Color.white;
        if (GUI.Button(new Rect(52, 781, 190, 54), "ESC   VOLTAR", bottomButtonStyle))
        {
            if (choosingOpponent) choosingOpponent = false; else screen = FlowScreen.Mode;
        }
        if (GUI.Button(new Rect(270, 781, 218, 54), "R   ALEATORIO", bottomButtonStyle)) SelectRandomFighter();
        GUIStyle navigationStyle = new GUIStyle(subtitleStyle) { fontSize = 17 };
        GUI.Label(new Rect(1042, 787, 440, 40), "SETAS / MOUSE   NAVEGAR", navigationStyle);
        GUI.matrix = oldMatrix;
    }

    private void DrawSelectionPreview(Rect rect, int index, string owner, Color accent, RenderTexture texture)
    {
        Color oldColor = GUI.color;
        GUI.color = new Color(.01f, .04f, .09f, .95f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = accent;
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 7f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.y, 3f, rect.height), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.xMax - 3f, rect.y, 3f, rect.height), Texture2D.whiteTexture);
        GUI.color = oldColor;

        GUIStyle ownerStyle = new GUIStyle(labelStyle) { fontSize = 22 };
        GUI.color = accent;
        GUI.DrawTexture(new Rect(rect.x + 42, rect.y - 30, rect.width - 84, 31), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(new Rect(rect.x + 42, rect.y - 30, rect.width - 84, 31), owner, ownerStyle);

        Rect imageRect = new Rect(rect.x + 5, rect.y + 8, rect.width - 10, 304);
        if (texture != null) GUI.DrawTexture(imageRect, texture, ScaleMode.ScaleToFit, true);
        GUI.color = new Color(0f, 0f, 0f, .55f);
        GUI.DrawTexture(new Rect(rect.x + 4, rect.y + 306, rect.width - 8, rect.height - 310), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUIStyle nameStyle = new GUIStyle(labelStyle) { alignment = TextAnchor.MiddleLeft, fontSize = 27 };
        GUIStyle classStyle = new GUIStyle(labelStyle) { alignment = TextAnchor.MiddleLeft, fontSize = 15, normal = { textColor = accent } };
        GUIStyle descriptionStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperLeft, fontSize = 13, wordWrap = true, normal = { textColor = new Color(.82f, .86f, .92f) } };
        GUI.Label(new Rect(rect.x + 20, rect.y + 318, rect.width - 40, 34), DisplayNames[index].ToUpperInvariant(), nameStyle);
        GUI.Label(new Rect(rect.x + 20, rect.y + 350, rect.width - 40, 24), FighterClasses[index], classStyle);
        GUI.Label(new Rect(rect.x + 20, rect.y + 378, rect.width - 40, 45), FighterDescriptions[index], descriptionStyle);

        for (int stat = 0; stat < StatNames.Length; stat++)
        {
            float y = rect.y + 428 + stat * 16;
            GUIStyle statStyle = new GUIStyle(GUI.skin.label) { fontSize = 11, normal = { textColor = Color.white } };
            GUI.color = Color.white;
            GUI.Label(new Rect(rect.x + 20, y - 2, 82, 16), StatNames[stat], statStyle);
            for (int point = 0; point < 5; point++)
            {
                GUI.color = point < FighterStats[index, stat] ? accent : new Color(.18f, .2f, .25f, 1f);
                GUI.DrawTexture(new Rect(rect.x + 102 + point * 43, y, 37, 9), Texture2D.whiteTexture);
            }
        }
        GUI.color = oldColor;
    }

    private void SelectRandomFighter()
    {
        if (characterPrefabs == null || characterPrefabs.Length == 0) return;
        int candidate = focusedSelection;
        for (int attempt = 0; attempt < 12 && (candidate == focusedSelection || (choosingOpponent && candidate == playerSelection)); attempt++)
            candidate = UnityEngine.Random.Range(0, characterPrefabs.Length);
        if (choosingOpponent && candidate == playerSelection) candidate = (playerSelection + 1) % characterPrefabs.Length;
        focusedSelection = candidate;
        RefreshPreviews();
    }

    private void DrawCardOutline(Rect rect, Color color, float thickness)
    {
        Color old = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), Texture2D.whiteTexture);
        GUI.color = old;
    }

    private void DrawRenderedPreview(Rect rect, int index, string owner, Color accent, RenderTexture texture)
    {
        Color old = GUI.color; GUI.color = new Color(.01f, .08f, .18f, .9f); GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = accent;
        GUI.DrawTexture(new Rect(rect.x + 5, rect.y + 5, rect.width - 10, rect.height - 45), Texture2D.whiteTexture);
        GUI.color = old;
        if (texture != null) GUI.DrawTexture(new Rect(rect.x + 8, rect.y + 8, rect.width - 16, rect.height - 53), texture, ScaleMode.ScaleToFit, true);
        bool confirmed = owner == "PLAYER 1" && playerOneConfirmed;
        GUI.Label(new Rect(rect.x, rect.y + rect.height - 40, rect.width, 22), confirmed ? owner + " - READY" : owner, subtitleStyle);
        GUI.Label(new Rect(rect.x, rect.y + rect.height - 20, rect.width, 25), DisplayNames[index], labelStyle);
        if (confirmed)
        {
            float glow = .35f + Mathf.Abs(Mathf.Sin(confirmationPulse * 5f)) * .45f;
            GUI.color = new Color(.35f, .9f, 1f, glow);
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 4), Texture2D.whiteTexture);
            GUI.color = old;
        }
    }

    private void DrawVS()
    {
        GUI.Label(new Rect(0, Screen.height * .28f, Screen.width, 90), DisplayNames[pendingPlayer], titleStyle);
        GUIStyle vsStyle = new GUIStyle(titleStyle) { fontSize = Mathf.RoundToInt(titleStyle.fontSize * 1.8f), normal = { textColor = new Color(.35f, .78f, 1f) } };
        GUI.Label(new Rect(0, Screen.height * .44f, Screen.width, 120), "VS", vsStyle);
        GUI.Label(new Rect(0, Screen.height * .62f, Screen.width, 90), DisplayNames[pendingOpponent] + (pendingMode == GameMode.Cpu ? "  • CPU" : "  • PLAYER 2"), titleStyle);
    }

    private void DrawVictory()
    {
        GUIStyle koStyle = new GUIStyle(titleStyle) { fontSize = Mathf.RoundToInt(titleStyle.fontSize * 1.8f), normal = { textColor = new Color(.35f, .85f, 1f) } };
        DrawVictoryPreview(new Rect(35, Screen.height * .18f, Screen.width * .25f, Screen.height * .48f), victoryWinnerTexture, victoryWinnerName, new Color(.1f, .7f, 1f));
        DrawVictoryPreview(new Rect(Screen.width * .75f, Screen.height * .18f, Screen.width * .21f, Screen.height * .48f), victoryLoserTexture, victoryLoserName, new Color(.25f, .45f, .7f));
        GUI.Label(new Rect(0, Screen.height * .15f, Screen.width, 120), "K.O.", koStyle);
        GUI.Label(new Rect(0, Screen.height * .32f, Screen.width, 65), victoryWinnerName + " VENCEU", subtitleStyle);
        GUI.Label(new Rect(0, Screen.height * .4f, Screen.width, 45), "DERROTADO: " + victoryLoserName, labelStyle);
        string[] options = { "JOGAR NOVAMENTE", "ESCOLHER PERSONAGENS", "VOLTAR AO MENU" };
        for (int i = 0; i < options.Length; i++)
        {
            GUI.backgroundColor = victoryOption == i ? new Color(.05f, .55f, 1f) : new Color(.03f, .18f, .35f);
            if (GUI.Button(CenteredButton(520, (int)(Screen.height * .54f) + i * 92), options[i], buttonStyle))
            {
                victoryOption = i;
                ConfirmVictoryOption();
            }
        }
        GUI.backgroundColor = Color.white;
        GUI.Label(new Rect(0, Screen.height - 55, Screen.width, 32), "SETAS: escolher   ENTER ou MOUSE: confirmar", subtitleStyle);
    }

    private void DrawVictoryPreview(Rect rect, RenderTexture texture, string name, Color accent)
    {
        GUI.color = new Color(.01f, .06f, .14f, .94f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = accent;
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 6f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.yMax - 6f, rect.width, 6f), Texture2D.whiteTexture);
        GUI.color = Color.white;
        if (texture != null) GUI.DrawTexture(new Rect(rect.x + 8f, rect.y + 12f, rect.width - 16f, rect.height - 48f), texture, ScaleMode.ScaleToFit, true);
        GUI.Label(new Rect(rect.x, rect.yMax - 36f, rect.width, 30f), name, subtitleStyle);
    }

    private void OnFighterKnockout()
    {
        if (victoryShown) return;
        FighterController loser = currentPlayer != null && currentPlayer.HealthSystem != null && currentPlayer.HealthSystem.IsDead ? currentPlayer : currentOpponent;
        StartCoroutine(ShowVictoryAfterKnockout(loser));
    }

    private IEnumerator ShowVictoryAfterKnockout(FighterController loser)
    {
        if (loser != null && loser.Animator != null)
        {
            float wait = 0f;
            while (wait < 1f && !loser.Animator.GetCurrentAnimatorStateInfo(0).IsName("Dying"))
            {
                wait += Time.unscaledDeltaTime;
                yield return null;
            }
            float duration = loser.Animator.GetCurrentAnimatorStateInfo(0).length;
            yield return new WaitForSecondsRealtime(Mathf.Clamp(duration * .95f, 1.2f, 4f));
        }
        yield return new WaitForSecondsRealtime(.25f);
        if (victoryShown) yield break;
        victoryShown = true;
        FighterController winner = loser == currentPlayer ? currentOpponent : currentPlayer;
        victoryWinnerName = winner == currentPlayer ? DisplayNames[pendingPlayer] : DisplayNames[pendingOpponent];
        victoryLoserName = loser == currentPlayer ? DisplayNames[pendingPlayer] : DisplayNames[pendingOpponent];
        CreateVictoryDisplays(winner, loser);
        int winnerIndex = winner == currentPlayer ? pendingPlayer : pendingOpponent;
        int loserIndex = loser == currentPlayer ? pendingPlayer : pendingOpponent;
        PlayVictoryAnimation(winner, winnerIndex, true);
        PlayVictoryAnimation(loser, loserIndex, false);
        victoryOption = 0;
        screen = FlowScreen.Victory;
    }

    private void CreateVictoryDisplays(FighterController winner, FighterController loser)
    {
        DestroyPreview(ref victoryWinnerObject, ref victoryWinnerCamera, ref victoryWinnerTexture);
        DestroyPreview(ref victoryLoserObject, ref victoryLoserCamera, ref victoryLoserTexture);
        int winnerIndex = winner == currentPlayer ? pendingPlayer : pendingOpponent;
        int loserIndex = loser == currentPlayer ? pendingPlayer : pendingOpponent;
        CreatePreview(winnerIndex, ref victoryWinnerObject, ref victoryWinnerCamera, ref victoryWinnerTexture, "VictoryWinner", false, 18, false);
        CreatePreview(loserIndex, ref victoryLoserObject, ref victoryLoserCamera, ref victoryLoserTexture, "VictoryLoser", false, 19, false);
        PlayVictoryClip(victoryWinnerObject, winnerIndex, true);
        PlayVictoryClip(victoryLoserObject, loserIndex, false);
    }

    private void PlayVictoryAnimation(FighterController fighter, int characterIndex, bool winner)
    {
        if (fighter == null || fighter.Animator == null) return;
        PlayClipOnIdleState(fighter.Animator, GetVictoryClip(characterIndex, winner));
    }

    private void PlayVictoryClip(GameObject display, int characterIndex, bool winner)
    {
        if (display == null) return;
        Animator animator = display.GetComponentInChildren<Animator>();
        if (animator == null) return;
        PlayClipOnIdleState(animator, GetVictoryClip(characterIndex, winner));
    }

    private AnimationClip GetVictoryClip(int characterIndex, bool winner)
    {
        if (!winner) return defeatedClip;
        return victoryClips != null && characterIndex >= 0 && characterIndex < victoryClips.Length
            ? victoryClips[characterIndex]
            : null;
    }

    private void PlayClipOnIdleState(Animator animator, AnimationClip clip)
    {
        if (animator == null || clip == null || animator.runtimeAnimatorController == null) return;
        var overrideController = new AnimatorOverrideController(animator.runtimeAnimatorController);
        var overrides = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<AnimationClip, AnimationClip>>();
        overrideController.GetOverrides(overrides);
        for (int i = 0; i < overrides.Count; i++)
        {
            AnimationClip source = overrides[i].Key;
            if (source != null && (source.name.Contains("Bouncing Fight Idle") || source.name == "Fighter_Idle"))
                overrides[i] = new System.Collections.Generic.KeyValuePair<AnimationClip, AnimationClip>(source, clip);
        }
        overrideController.ApplyOverrides(overrides);
        animator.runtimeAnimatorController = overrideController;
        animator.enabled = true;
        animator.speed = 1f;
        animator.Play("Idle", 0, 0f);
        animator.Update(0f);
    }

    private void ConfirmVictoryOption()
    {
        if (victoryOption == 0)
        {
            victoryShown = false;
            SetupFight();
            screen = FlowScreen.VS;
            vsUntil = Time.unscaledTime + 1.5f;
        }
        else if (victoryOption == 1)
        {
            victoryShown = false;
            BeginSelection(pendingMode);
        }
        else
        {
            victoryShown = false;
            RemoveSceneFighters();
            screen = FlowScreen.Main;
        }
    }

    private void MoveFocus(int horizontal, int vertical)
    {
        int next = focusedSelection + horizontal + vertical * 3;
        if (next < 0 || next >= characterPrefabs.Length) return;
        if (choosingOpponent && next == playerSelection) return;
        focusedSelection = next;
        RefreshPreviews();
    }

    private void ConfirmCurrentSelection()
    {
        if (characterPrefabs == null || characterPrefabs.Length == 0) return;
        if (!choosingOpponent)
        {
            playerSelection = focusedSelection;
            choosingOpponent = true;
            playerOneConfirmed = true;
            confirmationPulse = 0f;
            focusedSelection = opponentSelection == playerSelection ? (playerSelection + 1) % characterPrefabs.Length : opponentSelection;
            if (focusedSelection == playerSelection) focusedSelection = (focusedSelection + 1) % characterPrefabs.Length;
            RefreshPreviews();
            return;
        }
        if (focusedSelection == playerSelection) return;
        pendingMode = mode;
        pendingPlayer = playerSelection;
        pendingOpponent = focusedSelection;
        pendingFight = true;
        DestroyPreview(ref previewPlayerObject, ref previewPlayerCamera, ref previewPlayerTexture);
        DestroyPreview(ref previewOpponentObject, ref previewOpponentCamera, ref previewOpponentTexture);
        DestroyCardPreviews();
        SetupFight();
        pendingFight = false;
        screen = FlowScreen.VS;
        vsUntil = Time.unscaledTime + 1.5f;
    }

    private void RefreshPreviews()
    {
        if (screen != FlowScreen.Select || characterPrefabs == null || characterPrefabs.Length == 0) return;
        int left = choosingOpponent ? playerSelection : focusedSelection;
        int right = choosingOpponent ? focusedSelection : opponentSelection;
        if (!choosingOpponent) right = opponentSelection;
        if (previewPlayer != left) { previewPlayer = left; CreatePreview(left, ref previewPlayerObject, ref previewPlayerCamera, ref previewPlayerTexture, "P1Preview", false, 30, false); }
        if (previewOpponent != right) { previewOpponent = right; CreatePreview(right, ref previewOpponentObject, ref previewOpponentCamera, ref previewOpponentTexture, "P2Preview", false, 29, false); }
    }

    private void CreatePreview(int index, ref GameObject preview, ref Camera camera, ref RenderTexture texture, string label, bool portrait = false, int previewLayer = 30, bool staticPose = false)
    {
        DestroyPreview(ref preview, ref camera, ref texture);
        preview = InstantiatePrefab(index);
        if (preview == null) return;
        preview.name = label;
        preview.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0f, 180f, 0f));
        foreach (Collider collider in preview.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
        foreach (MonoBehaviour behaviour in preview.GetComponentsInChildren<MonoBehaviour>(true)) behaviour.enabled = false;
        if (staticPose)
            foreach (Animator animator in preview.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
        foreach (Transform child in preview.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = previewLayer;
        Renderer[] renderers = preview.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers) renderer.enabled = true;
        Bounds bounds = new Bounds(new Vector3(0f, 1f, 0f), Vector3.one);
        bool hasBounds = false;
        foreach (Renderer renderer in renderers)
        {
            if (!hasBounds) { bounds = renderer.bounds; hasBounds = true; }
            else bounds.Encapsulate(renderer.bounds);
        }
        Vector3 target = hasBounds ? bounds.center : new Vector3(0f, 1f, 0f);
        float radius = hasBounds ? Mathf.Max(bounds.extents.x, bounds.extents.y) : 1.25f;
        if (portrait && hasBounds)
        {
            Animator portraitAnimator = preview.GetComponent<Animator>();
            Transform head = portraitAnimator != null && portraitAnimator.isHuman
                ? portraitAnimator.GetBoneTransform(HumanBodyBones.Head)
                : null;
            target = head != null ? head.position + Vector3.up * .055f : new Vector3(bounds.center.x, bounds.min.y + bounds.size.y * .82f, bounds.center.z);
            radius = .255f;
        }
        else if (hasBounds)
        {
            target = bounds.center - Vector3.up * (bounds.size.y * .055f);
            radius = bounds.extents.y * 1.12f;
        }
        camera = new GameObject(label + "Camera").AddComponent<Camera>();
        camera.transform.SetPositionAndRotation(target + Vector3.back * 6f, Quaternion.identity);
        camera.transform.LookAt(target);
        camera.orthographic = true;
        camera.orthographicSize = portrait ? radius : Mathf.Clamp(radius, 1f, 2.35f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.015f, .08f, .18f, 1f);
        camera.cullingMask = 1 << previewLayer;
        texture = new RenderTexture(512, 512, 16, RenderTextureFormat.ARGB32);
        texture.Create();
        camera.targetTexture = texture;
    }

    private void CreateCardPreviews()
    {
        DestroyCardPreviews();
        if (characterPrefabs == null) return;
        int count = characterPrefabs.Length;
        cardPreviewObjects = new GameObject[count];
        cardPreviewCameras = new Camera[count];
        cardPreviewTextures = new RenderTexture[count];
        for (int i = 0; i < count; i++)
            CreatePreview(i, ref cardPreviewObjects[i], ref cardPreviewCameras[i], ref cardPreviewTextures[i], "CardPreview" + i, true, 20 + i, true);
    }

    private void DestroyCardPreviews()
    {
        if (cardPreviewObjects == null) return;
        for (int i = 0; i < cardPreviewObjects.Length; i++)
            DestroyPreview(ref cardPreviewObjects[i], ref cardPreviewCameras[i], ref cardPreviewTextures[i]);
        cardPreviewObjects = null;
        cardPreviewCameras = null;
        cardPreviewTextures = null;
    }

    private void DestroyPreview(ref GameObject preview, ref Camera camera, ref RenderTexture texture)
    {
        if (preview != null) Destroy(preview);
        if (camera != null) Destroy(camera.gameObject);
        if (texture != null) { texture.Release(); Destroy(texture); }
        preview = null; camera = null; texture = null;
    }

    private void SetupFight()
    {
        if (currentPlayer != null && currentPlayer.HealthSystem != null) currentPlayer.HealthSystem.OnKnockout -= OnFighterKnockout;
        if (currentOpponent != null && currentOpponent.HealthSystem != null) currentOpponent.HealthSystem.OnKnockout -= OnFighterKnockout;
        victoryShown = false;
        foreach (FighterController fighter in FindObjectsByType<FighterController>(FindObjectsSortMode.None)) Destroy(fighter.gameObject);
        GameObject player = InstantiatePrefab(pendingPlayer);
        GameObject opponent = InstantiatePrefab(pendingOpponent);
        if (player == null || opponent == null)
        {
            if (player != null) Destroy(player);
            if (opponent != null) Destroy(opponent);
            Debug.LogError("UnDFight: não foi possível instanciar os prefabs dos lutadores selecionados.");
            screen = FlowScreen.Select;
            return;
        }
        player.name = "Player1_" + DisplayNames[pendingPlayer];
        opponent.name = (pendingMode == GameMode.Cpu ? "CPU_" : "Player2_") + DisplayNames[pendingOpponent];
        player.transform.SetPositionAndRotation(new Vector3(-2.5f, 0f, 0f), Quaternion.Euler(0f, 90f, 0f));
        opponent.transform.SetPositionAndRotation(new Vector3(2.5f, 0f, 0f), Quaternion.Euler(0f, -90f, 0f));
        FighterMovement pm = player.GetComponent<FighterMovement>();
        FighterMovement om = opponent.GetComponent<FighterMovement>();
        currentPlayer = player.GetComponent<FighterController>();
        currentOpponent = opponent.GetComponent<FighterController>();
        if (currentPlayer != null && currentPlayer.HealthSystem != null) currentPlayer.HealthSystem.OnKnockout += OnFighterKnockout;
        if (currentOpponent != null && currentOpponent.HealthSystem != null) currentOpponent.HealthSystem.OnKnockout += OnFighterKnockout;
        pm.IsPlayerControlled = true; om.IsPlayerControlled = false;
        pm.Opponent = opponent.transform; om.Opponent = player.transform;
        if (pendingMode == GameMode.Cpu) opponent.AddComponent<FighterSparringAI>().Difficulty = AIDifficulty.Easy;
        else opponent.AddComponent<LocalPlayerTwoInput>();
        TekkenCamera camera = FindFirstObjectByType<TekkenCamera>();
        if (camera != null) { camera.Fighter1 = player.transform; camera.Fighter2 = opponent.transform; }
        SetMainCameraPreviewVisibility(true);
    }

    private void SetMainCameraPreviewVisibility(bool visible)
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null) return;
        int previewLayers = 0;
        for (int layer = 20; layer <= 30; layer++) previewLayers |= 1 << layer;
        mainCamera.cullingMask = visible ? mainCamera.cullingMask | previewLayers : mainCamera.cullingMask & ~previewLayers;
    }

    private GameObject InstantiatePrefab(int index)
    {
        if (characterPrefabs == null || index < 0 || index >= characterPrefabs.Length)
        {
            Debug.LogError("UnDFight: índice de personagem inválido: " + index);
            return null;
        }

        UnityEngine.Object source = characterPrefabs[index];
        if (source == null)
        {
            Debug.LogError("UnDFight: prefab vazio no índice " + index);
            return null;
        }

        return UnityEngine.Object.Instantiate(source) as GameObject;
    }

    private void RemoveSceneFighters()
    {
        foreach (FighterController fighter in FindObjectsByType<FighterController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            Destroy(fighter.gameObject);
    }
}

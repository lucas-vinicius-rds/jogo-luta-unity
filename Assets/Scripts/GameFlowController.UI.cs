using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public sealed partial class GameFlowController
{
    private Texture2D backgroundTexture;
    private AudioSource musicSource;
    private GUIStyle titleStyle;
    private GUIStyle subtitleStyle;
    private GUIStyle buttonStyle;
    private GUIStyle cardStyle;
    private GUIStyle selectedCardStyle;
    private GUIStyle labelStyle;

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

    private void OnGUI()
    {
        if (modernMenu != null && modernMenu.IsReady && IsModernMenuVisible) return;
        if (selectionPresenter != null && selectionPresenter.IsReady && (IsSelecting || IsStageSelecting || IsStageLoading)) return;
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
            if (choosingOpponent) choosingOpponent = false;
            else
            {
                SetMainCameraPreviewVisibility(false);
                screen = FlowScreen.Mode;
            }
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
}

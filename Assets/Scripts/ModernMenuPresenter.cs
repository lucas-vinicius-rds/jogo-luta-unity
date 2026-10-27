using UnityEngine;
using UnityEngine.UIElements;

/// <summary>Menu nativo UI Toolkit. UXML define a estrutura e USS concentra o visual responsivo.</summary>
[DisallowMultipleComponent]
public sealed class ModernMenuPresenter : MonoBehaviour
{
    private GameFlowController flow;
    private UIDocument document;
    private VisualElement root;
    private VisualElement canvas;
    private string renderedScreen;
    public bool IsReady { get; private set; }

    public void Initialize(GameFlowController owner)
    {
        flow = owner;
        ApplyBackground();
    }

    private void OnEnable()
    {
        document = GetComponent<UIDocument>();
        if (document == null) document = gameObject.AddComponent<UIDocument>();
        if (document.panelSettings == null)
        {
            PanelSettings panel = ScriptableObject.CreateInstance<PanelSettings>();
            panel.themeStyleSheet = Resources.Load<ThemeStyleSheet>("UI/RuntimeTheme");
            document.panelSettings = panel;
        }
        document.panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
        document.panelSettings.referenceResolution = new Vector2Int(1280, 720);
        document.panelSettings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
        document.panelSettings.match = .5f;
        VisualTreeAsset layout = Resources.Load<VisualTreeAsset>("UI/ModernMenu");
        if (layout == null) return;
        document.visualTreeAsset = layout;
        root = document.rootVisualElement;
        root.style.flexGrow = 1;
        canvas = root.Q("undfight-menu");
        renderedScreen = null;
        StyleSheet style = Resources.Load<StyleSheet>("UI/ModernMenu");
        if (style != null) root.styleSheets.Add(style);
        ApplyBackground();
        IsReady = root != null;
    }

    private void Update()
    {
        if (!IsReady || flow == null) return;
        bool visible = flow.IsModernMenuVisible;
        root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        ApplyBackground();
        string current = flow.CurrentMenuScreen;
        if (visible && current != renderedScreen) Render(current);
    }

    private void ApplyBackground()
    {
        if (root == null || flow == null || flow.MenuBackground == null) return;
        // O UXML possui um filho que preenche o painel. Aplicar no root do
        // UIDocument nao o desenha por cima desse filho; por isso a textura
        // precisa ficar no elemento visual que realmente ocupa a tela.
        if (canvas == null) return;
        canvas.style.backgroundImage = new StyleBackground(flow.MenuBackground);
        canvas.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);
        canvas.style.backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Center);
        canvas.style.backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Center);
    }

    private void Render(string screen)
    {
        renderedScreen = screen;
        canvas.Clear();
        ScrollView scroll = new ScrollView(ScrollViewMode.Vertical);
        scroll.style.flexGrow = 1;
        canvas.Add(scroll);
        VisualElement page = new VisualElement();
        page.AddToClassList("page");
        scroll.Add(page);
        if (screen == "Main") DrawMain(page);
        else if (screen == "Mode") DrawMode(page);
        else DrawOptions(page);
        page.Q<Button>()?.Focus();
    }

    private void DrawMain(VisualElement page)
    {
        page.Add(Title("UNDFIGHT", "RIVALIDADE NA FLORESTA"));
        VisualElement actions = Actions();
        actions.Add(Button("ENTRAR NA ARENA", "primary", flow.OpenVersusMode));
        actions.Add(Button("OPÇÕES", "secondary", flow.OpenOptionsScreen));
        actions.Add(Button("SAIR", "ghost", Application.Quit));
        page.Add(actions);
        page.Add(Footer("PALAFITA • AMAZÔNIA\nVERSUS LOCAL  /  DESAFIO CPU"));
    }

    private void DrawMode(VisualElement page)
    {
        page.Add(Title("VERSUS", "ESCOLHA O FORMATO DA LUTA"));
        VisualElement actions = Actions();
        actions.Add(Button("JOGADOR VS CPU", "primary", flow.BeginCpuSelection));
        VisualElement difficulty = new VisualElement(); difficulty.AddToClassList("difficulty-row");
        difficulty.Add(Button("−", "small", () => { flow.ChangeCpuDifficulty(-1); renderedScreen = null; }));
        Label level = new Label("CPU: " + flow.CurrentCpuDifficulty); level.AddToClassList("difficulty-label"); difficulty.Add(level);
        difficulty.Add(Button("+", "small", () => { flow.ChangeCpuDifficulty(1); renderedScreen = null; }));
        actions.Add(difficulty);
        actions.Add(Button("JOGADOR VS JOGADOR", "secondary", flow.BeginLocalSelection));
        actions.Add(Button("VOLTAR", "ghost", flow.OpenMainMenu));
        page.Add(actions);
    }

    private void DrawOptions(VisualElement page)
    {
        page.Add(Title("OPÇÕES", "CONFIGURE A EXPERIÊNCIA"));
        VisualElement actions = Actions();
        Label volume = new Label("VOLUME GERAL  " + Mathf.RoundToInt(flow.MasterVolume * 100f) + "%"); volume.AddToClassList("setting-label"); actions.Add(volume);
        Slider slider = new Slider(0f, 1f) { value = flow.MasterVolume }; slider.AddToClassList("volume-slider"); slider.RegisterValueChangedCallback(evt => { flow.SetMasterVolume(evt.newValue); volume.text = "VOLUME GERAL  " + Mathf.RoundToInt(evt.newValue * 100f) + "%"; }); actions.Add(slider);
        actions.Add(Button(flow.IsFullscreen ? "TELA CHEIA: ATIVA" : "TELA CHEIA: DESATIVADA", "secondary", () => { flow.ToggleFullscreenOption(); renderedScreen = null; }));
        Vector2Int resolution = flow.CurrentResolution;
        actions.Add(Button("RESOLUCAO: " + resolution.x + " × " + resolution.y, "secondary", () => { flow.CycleResolutionOption(); renderedScreen = null; }));
        actions.Add(Button("VOLTAR", "ghost", flow.OpenMainMenu));
        page.Add(actions);
        page.Add(Footer("CONTROLES COMPARTILHADOS POR TODOS OS LUTADORES\nP1  A/D mover · W saltar · S agachar · F soco · G chute · SHIFT defender\nP2  ←/→ mover · ↑ saltar · ↓ agachar · J soco · K chute · L defender\nP1 também aceita ESPAÇO / CTRL. Controle: A soco · X chute · LB defesa."));
    }

    private static VisualElement Title(string heading, string caption)
    {
        VisualElement title = new VisualElement(); title.AddToClassList("title-group");
        Label headingLabel = new Label(heading); headingLabel.AddToClassList("title"); title.Add(headingLabel);
        Label captionLabel = new Label(caption); captionLabel.AddToClassList("caption"); title.Add(captionLabel);
        return title;
    }

    private static VisualElement Actions() { VisualElement actions = new VisualElement(); actions.AddToClassList("actions"); return actions; }
    private static Label Footer(string value) { Label footer = new Label(value); footer.AddToClassList("footer"); return footer; }
    private static Button Button(string label, string style, System.Action action)
    {
        Button button = new Button(action) { text = label }; button.AddToClassList("button"); button.AddToClassList(style); return button;
    }
}

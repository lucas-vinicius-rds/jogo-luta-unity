using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>View only. Character and stage transactions remain in GameFlowController.</summary>
public sealed class SelectionPresenter : MonoBehaviour
{
    GameFlowController flow;
    UIDocument document;
    PanelSettings panel;
    VisualElement page, cards;
    Image hero, rival;
    Label title, description, heading;
    Button confirm;
    readonly List<Button> buttons = new List<Button>();
    readonly MenuNavigationInput navigation = new MenuNavigationInput();
    List<StageDefinition> stages;
    string rendered;
    int focusedStage, lastFocus = -1;
    public bool IsReady => page != null;
    public void Initialize(GameFlowController owner)
    {
        flow = owner;
        var tree = Resources.Load<VisualTreeAsset>("UI/Selection"); if(tree == null) return;
        document = gameObject.AddComponent<UIDocument>();
        panel = ScriptableObject.CreateInstance<PanelSettings>();
        panel.themeStyleSheet = Resources.Load<ThemeStyleSheet>("UI/RuntimeTheme");
        panel.scaleMode = PanelScaleMode.ScaleWithScreenSize; panel.referenceResolution = new Vector2Int(1920,1080); panel.match = 1;
        document.panelSettings = panel; document.sortingOrder = 30; document.visualTreeAsset = tree;
        page = document.rootVisualElement; page.style.flexGrow = 1;
        var style = Resources.Load<StyleSheet>("UI/Selection"); if(style != null) page.styleSheets.Add(style);
        cards = page.Q("cards"); hero = page.Q<Image>("hero"); rival = page.Q<Image>("rival"); title = page.Q<Label>("name"); description = page.Q<Label>("description"); heading = page.Q<Label>("heading");
        confirm = page.Q<Button>("confirm"); confirm.clicked += Confirm;
        page.Q<Button>("back").clicked += () => flow.BackSelection();
        page.Q<Button>("random").clicked += Random;
    }
    void Update()
    {
        if(!IsReady || flow == null) return;
        string state = flow.IsSelecting ? "character" : flow.IsStageSelecting ? "stage" : flow.IsStageLoading ? "loading" : "hidden";
        page.style.display = state == "hidden" ? DisplayStyle.None : DisplayStyle.Flex;
        if(state == "hidden") { rendered = state; return; }
        if(state != rendered) { Build(state); rendered = state; }
        if(state == "loading") return;
        navigation.Read(out int x,out int y,out bool accept,out bool back,out bool random);
        if(state == "character") { if(x != 0 || y != 0) flow.NavigateCharacter(x,y); }
        else if(stages.Count > 0 && (x != 0 || y != 0)) focusedStage = (focusedStage + x + y + stages.Count)%stages.Count;
        if(back) flow.BackSelection(); else if(accept) Confirm(); else if(random) Random();
        Refresh(state);
    }
    void Build(string state)
    {
        cards.Clear(); buttons.Clear(); lastFocus = -1;
        heading.text = state == "character" ? "ESCOLHA SEU LUTADOR" : state == "stage" ? "ESCOLHA SEU TERRITÓRIO" : "PREPARANDO A ARENA";
        page.EnableInClassList("stage", state != "character");
        confirm.SetEnabled(state != "loading");
        if(state == "loading") return;
        if(state == "stage") { stages = flow.Stages.Registry.Available.ToList(); focusedStage = Mathf.Clamp(stages.FindIndex(s => s == flow.Stages.Selected),0,stages.Count-1); }
        int count = state == "character" ? flow.CharacterPrefabs.Length : stages.Count;
        for(int i = 0; i < count; i++)
        {
            int index = i;
            var button = new Button(() => { if(state == "character") flow.FocusCharacter(index); else focusedStage = index; });
            button.AddToClassList("card");
            var image = new Image { image = state == "character" ? flow.CardPreview(i) : stages[i].preview, scaleMode = ScaleMode.ScaleAndCrop }; button.Add(image);
            button.Add(new Label((i+1).ToString("00") + " / " + (state == "character" ? GameFlowController.CharacterDisplayNames[i] : stages[i].displayName)));
            button.RegisterCallback<PointerMoveEvent>(evt => { if(evt.deltaPosition.sqrMagnitude > .1f) { if(state == "character") flow.FocusCharacter(index); else focusedStage = index; } });
            cards.Add(button); buttons.Add(button);
        }
    }
    void Refresh(string state)
    {
        int focus = state == "character" ? flow.FocusedCharacter : focusedStage;
        if(state == "character") { hero.image = flow.PlayerPreview; rival.image = flow.OpponentPreview; }
        if(lastFocus == focus && state != "character") return;
        for(int i = 0; i < buttons.Count; i++) buttons[i].EnableInClassList("focused",i == focus);
        if(state == "character") { title.text = GameFlowController.CharacterDisplayNames[focus].ToUpperInvariant(); description.text = (flow.ChoosingOpponent ? "P2 / " : "P1 / ") + flow.FocusedDescription; }
        else if(stages.Count > 0) { hero.image = stages[focus].preview; title.text = stages[focus].displayName.ToUpperInvariant(); description.text = stages[focus].description; }
        if(lastFocus != focus) { hero.style.opacity = .4f; hero.schedule.Execute(() => hero.style.opacity = 1).StartingIn(20); }
        lastFocus = focus;
    }
    void Confirm() { if(flow.IsSelecting) flow.ConfirmCharacter(); else if(flow.IsStageSelecting && stages.Count > 0) flow.ConfirmStage(stages[focusedStage].stableId); }
    void Random() { if(flow.IsSelecting) flow.RandomCharacter(); else if(stages != null && stages.Count > 1) focusedStage = (focusedStage + UnityEngine.Random.Range(1,stages.Count))%stages.Count; }
    void OnDestroy() { if(panel != null) Destroy(panel); }
}

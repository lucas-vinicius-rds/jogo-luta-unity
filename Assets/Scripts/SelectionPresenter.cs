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
    bool lastChoosing;
    AudioSource feedback;
    AudioClip tick, accepted;
    VisualElement browser, backdrop;
    Texture2D characterBackground, stageBackground;
    public bool IsReady => page != null;
    public int FocusedStageIndex => focusedStage;
    public void FocusStage(string id) { if(stages == null) return; int i=stages.FindIndex(s=>s.stableId==id); if(i>=0) focusedStage=i; }
    public void Initialize(GameFlowController owner)
    {
        flow = owner;
        characterBackground = Resources.Load<Texture2D>("UI/Brand/CharacterBackground");
        stageBackground = Resources.Load<Texture2D>("UI/Brand/StageBackground");
        feedback=gameObject.AddComponent<AudioSource>(); feedback.playOnAwake=false; feedback.volume=.15f;
        tick=Tone(460,.035f); accepted=Tone(740,.075f);
        var tree = Resources.Load<VisualTreeAsset>("UI/Selection"); if(tree == null) return;
        document = gameObject.AddComponent<UIDocument>();
        panel = ScriptableObject.CreateInstance<PanelSettings>();
        panel.themeStyleSheet = Resources.Load<ThemeStyleSheet>("UI/RuntimeTheme");
        panel.scaleMode = PanelScaleMode.ScaleWithScreenSize; panel.referenceResolution = new Vector2Int(1920,1080); panel.match = 1;
        document.panelSettings = panel; document.sortingOrder = 30; document.visualTreeAsset = tree;
        page = document.rootVisualElement; page.style.flexGrow = 1;
        // This view uses the shared polling reader; prevent a second UI Toolkit submit/navigation path.
        page.RegisterCallback<NavigationMoveEvent>(evt=>evt.StopImmediatePropagation(),TrickleDown.TrickleDown);
        page.RegisterCallback<NavigationSubmitEvent>(evt=>evt.StopImmediatePropagation(),TrickleDown.TrickleDown);
        page.RegisterCallback<NavigationCancelEvent>(evt=>evt.StopImmediatePropagation(),TrickleDown.TrickleDown);
        var style = Resources.Load<StyleSheet>("UI/Selection"); if(style != null) page.styleSheets.Add(style);
        cards = page.Q("cards"); hero = page.Q<Image>("hero"); rival = page.Q<Image>("rival"); title = page.Q<Label>("name"); description = page.Q<Label>("description"); heading = page.Q<Label>("heading");
        backdrop = page.Q("selection-background");
        browser=page.Q<ScrollView>();
        rival.scaleMode=ScaleMode.ScaleAndCrop;
        ((ScrollView)browser).verticalScrollerVisibility=ScrollerVisibility.Hidden;
        ((ScrollView)browser).horizontalScrollerVisibility=ScrollerVisibility.Hidden;
        confirm = page.Q<Button>("confirm"); confirm.clicked += Confirm;
        page.Q<Button>("back").clicked += () => flow.BackSelection();
        page.Q<Button>("random").clicked += Random;
    }
    void Update()
    {
        if(!IsReady || flow == null) return;
        string state = flow.IsSelecting ? "character" : flow.IsStageSelecting ? "stage" : flow.IsStageLoading ? "loading" : "hidden";
        if(state != rendered) page.style.display = state == "hidden" ? DisplayStyle.None : DisplayStyle.Flex;
        if(state == "hidden") { rendered = state; return; }
        if(state != rendered) { Build(state); rendered = state; if(state != "loading") Refresh(state); return; }
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
        Texture2D background = state == "character" ? characterBackground : stageBackground;
        if (background == null) background = flow.MenuBackground;
        if (background != null) { backdrop.style.backgroundImage = new StyleBackground(background); backdrop.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover); }
        heading.text = state == "character" ? "ESCOLHA SEU LUTADOR" : state == "stage" ? "ESCOLHA SEU TERRITÓRIO" : "PREPARANDO A ARENA";
        page.EnableInClassList("stage", state != "character");
        if(state == "character") hero.PlaceBehind(browser); else browser.PlaceBehind(hero);
        hero.scaleMode=state == "character" ? ScaleMode.ScaleToFit : ScaleMode.ScaleAndCrop;
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
        if(lastFocus == focus && (state != "character" || lastChoosing == flow.ChoosingOpponent)) return;
        for(int i = 0; i < buttons.Count; i++) buttons[i].EnableInClassList("focused",i == focus);
        if(state == "character") { title.text = GameFlowController.CharacterDisplayNames[focus].ToUpperInvariant(); description.text = (flow.ChoosingOpponent ? "P2 / " : "P1 / ") + flow.FocusedDescription; page.Q<Label>("stats").text=flow.FocusedStats; }
        else if(stages.Count > 0) { hero.image = stages[focus].preview; title.text = stages[focus].displayName.ToUpperInvariant(); description.text = stages[focus].description; }
        if(lastFocus != focus) { hero.style.opacity = .4f; hero.schedule.Execute(() => hero.style.opacity = 1).StartingIn(20); if(lastFocus>=0) feedback.PlayOneShot(tick); if(focus < buttons.Count) page.Q<ScrollView>().ScrollTo(buttons[focus]); }
        lastFocus = focus; lastChoosing=flow.ChoosingOpponent;
    }
    void Confirm() { if(flow.IsSelecting) { feedback.PlayOneShot(accepted); flow.ConfirmCharacter(); } else if(flow.IsStageSelecting && stages.Count > 0 && flow.ConfirmStage(stages[focusedStage].stableId)) feedback.PlayOneShot(accepted); }
    void Random() { if(flow.IsSelecting) flow.RandomCharacter(); else if(stages != null && stages.Count > 1) focusedStage = (focusedStage + UnityEngine.Random.Range(1,stages.Count))%stages.Count; }
    static AudioClip Tone(float hz,float seconds) { int count=Mathf.RoundToInt(seconds*22050); var data=new float[count]; for(int i=0;i<count;i++) data[i]=Mathf.Sin(i*hz*2*Mathf.PI/22050)*.3f*(1-i/(float)count); var clip=AudioClip.Create("SelectionFeedback",count,1,22050,false); clip.SetData(data,0); return clip; }
    void OnDestroy() { if(panel != null) Destroy(panel); if(tick!=null) Destroy(tick); if(accepted!=null) Destroy(accepted); }
}

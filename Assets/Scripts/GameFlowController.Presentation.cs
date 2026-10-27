using System.Collections;
using UnityEngine;

public sealed partial class GameFlowController
{
    [SerializeField] StageRegistry stageRegistry;
    [SerializeField] Texture2D menuBackground;
    [SerializeField] AudioClip menuMusic;
    ModernMenuPresenter modernMenu;
    SelectionPresenter selectionPresenter;
    bool stageTransition;
    public StageManager Stages { get; private set; }
    public bool IsSelecting => screen == FlowScreen.Select;
    public bool IsStageSelecting => screen == FlowScreen.StageSelect;
    public bool IsStageLoading => screen == FlowScreen.Loading;
    public bool IsModernMenuVisible => screen == FlowScreen.Main || screen == FlowScreen.Mode || screen == FlowScreen.Options;
    public string CurrentMenuScreen => screen.ToString();
    public Texture2D MenuBackground => backgroundTexture;
    public bool IsCombatScreen => screen == FlowScreen.Fight;
    public bool IsFightPaused { get; private set; }
    public static bool AllowsCombat => Instance == null || (Instance.IsFightScreen && !Instance.IsFightPaused);
    public AIDifficulty CurrentCpuDifficulty { get; private set; } = AIDifficulty.Easy;
    public float MasterVolume => AudioListener.volume;
    public bool IsFullscreen => Screen.fullScreen;
    public Vector2Int CurrentResolution => new Vector2Int(Screen.width, Screen.height);
    public int FocusedCharacter => focusedSelection;
    public bool ChoosingOpponent => choosingOpponent;
    public Texture PlayerPreview => previewPlayerTexture;
    public Texture OpponentPreview => previewOpponentTexture;
    public Texture CardPreview(int index) => cardPreviewTextures != null && index < cardPreviewTextures.Length ? cardPreviewTextures[index] : null;
    public string FocusedDescription => FighterDescriptions[focusedSelection];
    public string FocusedStats => $"FORÇA {FighterStats[focusedSelection,0]}/5   VEL. {FighterStats[focusedSelection,1]}/5   DEF. {FighterStats[focusedSelection,2]}/5   ALC. {FighterStats[focusedSelection,3]}/5   TÉC. {FighterStats[focusedSelection,4]}/5";
    public static Vector3 StagePlayerSpawn => DefaultP1Position;
    public static Vector3 StageOpponentSpawn => DefaultP2Position;
    void InitializePresentation()
    {
        Stages = gameObject.AddComponent<StageManager>(); Stages.Initialize(stageRegistry);
        var menu = new GameObject("ModernMenu"); DontDestroyOnLoad(menu);
        modernMenu = menu.AddComponent<ModernMenuPresenter>(); modernMenu.Initialize(this);
        var selection = new GameObject("SelectionUI"); DontDestroyOnLoad(selection);
        selectionPresenter = selection.AddComponent<SelectionPresenter>(); selectionPresenter.Initialize(this);
        gameObject.AddComponent<FightHud>().Initialize(this);
    }
    void LateUpdate()
    {
        if (IsFightScreen && UnityEngine.InputSystem.Keyboard.current?.escapeKey.wasPressedThisFrame == true) SetFightPaused(!IsFightPaused);
    }
    public void BeginCpuSelection() => BeginSelection(GameMode.Cpu);
    public void BeginLocalSelection() => BeginSelection(GameMode.Local);
    public void OpenMainMenu() { SetFightPaused(false); Stages.Unload(); RemoveSceneFighters(); screen = FlowScreen.Main; }
    public void OpenVersusMode() => screen = FlowScreen.Mode;
    public void OpenOptionsScreen() => screen = FlowScreen.Options;
    public void ChangeCpuDifficulty(int delta) => CurrentCpuDifficulty = (AIDifficulty)Mathf.Clamp((int)CurrentCpuDifficulty + delta, 0, 2);
    public void SetMasterVolume(float value) => AudioListener.volume = Mathf.Clamp01(value);
    public void ToggleFullscreenOption() => Screen.fullScreen = !Screen.fullScreen;
    public void CycleResolutionOption() { var resolutions = Screen.resolutions; if(resolutions.Length == 0) return; int i = System.Array.FindIndex(resolutions, r => r.width == Screen.width && r.height == Screen.height); var next = resolutions[(i+1)%resolutions.Length]; Screen.SetResolution(next.width,next.height,Screen.fullScreenMode); }
    public void SetFightPaused(bool value) { IsFightPaused = value; Time.timeScale = value ? 0 : 1; }
    public void RestartFight() { SetFightPaused(false); Rematch(); }
    public void ReturnToCharacterSelect() { SetFightPaused(false); Stages.Unload(); RemoveSceneFighters(); BeginSelection(pendingMode); }
    public void FocusCharacter(int index) { if(!IsSelecting || index < 0 || index >= characterPrefabs.Length || (choosingOpponent && index == playerSelection)) return; focusedSelection = index; RefreshPreviews(); }
    public void NavigateCharacter(int x,int y) => MoveFocus(x,y);
    public void RandomCharacter() => SelectRandomFighter();
    public void ConfirmCharacter() => ConfirmCurrentSelection();
    public void BackSelection() { if(IsStageSelecting) { screen = FlowScreen.Select; choosingOpponent = true; focusedSelection = pendingOpponent; RefreshPreviews(); } else if(choosingOpponent) choosingOpponent = false; else screen = FlowScreen.Mode; }
    public bool ConfirmStage(string id)
    {
        if(!IsStageSelecting || stageTransition || !Stages.Select(id)) return false;
        StartCoroutine(EnterStage()); return true;
    }
    IEnumerator EnterStage()
    {
        stageTransition = true; screen = FlowScreen.VS;
        DestroyPreview(ref previewPlayerObject, ref previewPlayerCamera, ref previewPlayerTexture);
        DestroyPreview(ref previewOpponentObject, ref previewOpponentCamera, ref previewOpponentTexture); DestroyCardPreviews();
        yield return new WaitForSecondsRealtime(1.2f);
        screen = FlowScreen.Loading; yield return null; yield return null;
        SetupFight(); pendingFight = false;
        if(screen == FlowScreen.Loading) screen = FlowScreen.Fight;
        stageTransition = false;
    }
}

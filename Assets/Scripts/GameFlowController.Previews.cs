using System.Collections.Generic;
using UnityEngine;

public sealed partial class GameFlowController
{
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

    /// <summary>
    /// Destrói os modelos 3D, câmeras e texturas geradas para a tela de vitória, evitando duplicatas no Rematch.
    /// </summary>
    private void DestroyVictoryDisplays()
    {
        DestroyPreview(ref victoryWinnerObject, ref victoryWinnerCamera, ref victoryWinnerTexture);
        DestroyPreview(ref victoryLoserObject, ref victoryLoserCamera, ref victoryLoserTexture);
    }

    private void CreateVictoryDisplays(FighterController winner, FighterController loser)
    {
        DestroyVictoryDisplays();
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
        var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        overrideController.GetOverrides(overrides);
        for (int i = 0; i < overrides.Count; i++)
        {
            AnimationClip source = overrides[i].Key;
            if (source != null && (source.name.Contains("Bouncing Fight Idle") || source.name == "Fighter_Idle"))
                overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(source, clip);
        }
        overrideController.ApplyOverrides(overrides);
        animator.runtimeAnimatorController = overrideController;
        animator.enabled = true;
        animator.speed = 1f;
        animator.Play(IdleAnimHash, 0, 0f);
        animator.Update(0f);
    }

    /// <summary>
    /// Controla a visibilidade das camadas de preview (layers 18 a 30) na câmera principal/gameplay.
    /// Garante que fiquem visíveis apenas na tela de seleção e nunca vazem durante o combate.
    /// </summary>
    private void SetMainCameraPreviewVisibility(bool visible)
    {
        if (cachedMainCamera == null)
        {
            cachedMainCamera = Camera.main;
            if (cachedMainCamera == null)
            {
                TekkenCamera tc = FindAnyObjectByType<TekkenCamera>();
                if (tc != null) cachedMainCamera = tc.GetComponent<Camera>();
            }
        }

        if (cachedMainCamera == null) return;

        if (visible)
        {
            cachedMainCamera.cullingMask |= PreviewLayersMask;
        }
        else
        {
            cachedMainCamera.cullingMask &= ~PreviewLayersMask;
        }
    }
}

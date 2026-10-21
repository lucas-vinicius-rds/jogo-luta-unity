using UnityEngine;

public partial class FighterController
{
    private static readonly Rect CombatPanelRect = new Rect(15, 15, 390, 200);
    private static readonly Rect ControlsPanelRect = new Rect(15, 225, 390, 180);

    private void OnGUI()
    {
        // Só renderiza o painel no lutador controlado pelo jogador 1 para evitar sobreposição dupla
        if (!showOnScreenControls) return;
        if (movement == null || !movement.IsPlayerControlled) return;
        if (inputConfig != null && inputConfig.PlayerIndex != 1) return;

        GUI.color = Color.white;
        var boxStyle = GUI.skin.box;

        if (cachedP1Name == null)
        {
            cachedP1Name = gameObject.name.Replace("Player_", string.Empty).Replace("(Clone)", string.Empty).Trim();
        }

        if (movement.Opponent != cachedOpponentTransform)
        {
            cachedOpponentTransform = movement.Opponent;
            if (cachedOpponentTransform != null)
            {
                cachedOpponentTransform.TryGetComponent(out cachedOpponentController);
                cachedOpponentTransform.TryGetComponent(out cachedOpponentAI);
                cachedOpponentHealth = cachedOpponentController != null ? cachedOpponentController.HealthSystem : null;
                cachedP2Name = cachedOpponentTransform.gameObject.name.Replace("Opponent_", string.Empty).Replace("(Clone)", string.Empty).Trim();
            }
            else
            {
                cachedOpponentController = null;
                cachedOpponentAI = null;
                cachedOpponentHealth = null;
                cachedP2Name = "IA";
            }
        }

        var opponentController = cachedOpponentController;
        var opHealth = cachedOpponentHealth;
        var ai = cachedOpponentAI;

        // 1. Painel de Status & Barra de Vida em Tempo Real
        GUILayout.BeginArea(CombatPanelRect, boxStyle);
        GUILayout.Label("<b>PAINEL DE COMBATE & DIFICULDADE DA IA</b>");

        float p1Hp = healthSystem != null ? healthSystem.CurrentHealth : 100f;
        float p1Max = healthSystem != null ? healthSystem.MaxHealth : 100f;
        string p1Status = healthSystem != null && healthSystem.IsDead ? "<color=red>K.O. (Dying)</color>" : $"{p1Hp:F0}/{p1Max:F0}";
        string p1Name = cachedP1Name;
        string p2Name = cachedP2Name;
        GUILayout.Label($"P1 (Você - {p1Name}): <b>{p1Status}</b> | Estado: <b><color=yellow>{CurrentStateName}</color></b>");

        float p2Hp = opHealth != null ? opHealth.CurrentHealth : 100f;
        float p2Max = opHealth != null ? opHealth.MaxHealth : 100f;
        string p2Status = opHealth != null && opHealth.IsDead ? "<color=red>K.O. (Dying)</color>" : $"{p2Hp:F0}/{p2Max:F0}";
        GUILayout.Label($"P2 (Oponente - {p2Name}): <b>{p2Status}</b> | Estado: <b><color=yellow>{(opponentController != null ? opponentController.CurrentStateName : "N/A")}</color></b>");

        string diffText = ai != null ? ai.Difficulty.ToString() : "N/A";
        string diffColor = diffText == "Easy" ? "lime" : (diffText == "Medium" ? "yellow" : "red");
        GUILayout.Label($"Dificuldade da IA: <b><color={diffColor}>{diffText}</color></b> | Hitstop: <b>{defaultHitstopDuration * 1000f:F0}ms</b>");
        GUILayout.Label($"Faixa 2.5D: <b>{(movement.IsCrouching ? "Agachado" : "Em pé")}</b>");
        GUILayout.EndArea();

        // 2. Controles Virtuais Interativos
        GUILayout.BeginArea(ControlsPanelRect, boxStyle);
        GUILayout.Label("<b>CONTROLES & SELETOR DE DIFICULDADE:</b>");

        // Movimento P1 (A/D: faixa, W: salto, S: agachar)
        GUILayout.BeginHorizontal();
        if (GUILayout.RepeatButton("<< A (Órbita)", GUILayout.Height(30)))
        {
            movement.ExternalInput = new Vector2(-1f, 0f);
        }
        else if (GUILayout.RepeatButton("D (Direita) >>", GUILayout.Height(30)))
        {
            movement.ExternalInput = new Vector2(1f, 0f);
        }
        else if (GUILayout.RepeatButton("▲ W (Pular)", GUILayout.Height(30)))
        {
            movement.ExternalInput = new Vector2(0f, 1f);
        }
        else if (GUILayout.RepeatButton("▼ S (Agachar)", GUILayout.Height(30)))
        {
            movement.ExternalInput = new Vector2(0f, -1f);
        }
        else
        {
            movement.ExternalInput = Vector2.zero;
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(4);

        // Ações de Ataque
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("🥊 P1 SOCO (Espaço)", GUILayout.Height(32)))
        {
            TriggerAttack();
        }

        if (opponentController != null && GUILayout.Button("💥 P2 SOCO (IA)", GUILayout.Height(32)))
        {
            opponentController.TriggerAttack();
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(4);

        // Seletor de Dificuldade da IA e Reset
        GUILayout.BeginHorizontal();
        if (ai != null)
        {
            string btnDiff = $"🎯 Dificuldade: [{ai.Difficulty}]";
            if (GUILayout.Button(btnDiff, GUILayout.Height(30)))
            {
                ai.CycleDifficulty();
            }

            string aiToggle = ai.AutoSparring ? "IA: [ON]" : "IA: [OFF]";
            if (GUILayout.Button(aiToggle, GUILayout.Width(75), GUILayout.Height(30)))
            {
                ai.AutoSparring = !ai.AutoSparring;
            }
        }

        if (GUILayout.Button("🔄 REMATCH", GUILayout.Height(30)))
        {
            ResetMatch();
        }
        GUILayout.EndHorizontal();

        GUILayout.EndArea();
    }
}

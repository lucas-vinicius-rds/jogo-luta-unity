# 🤖 Relatório Técnico: Harness de Teste Automatizado (`AutoFightTester`) — UnDFight

Documento elaborado para planejamento de correções e refatorações no jogo **UnDFight** (Unity 6 / C#). Este documento consolida a arquitetura do harness, as alterações de código realizadas para viabilizá-lo, as informações extraídas dos testes e as anomalias diagnosticadas em todos os 7 personagens jogáveis.

---

## 1. Visão Geral do Harness (`AutoFightTester`)

O `AutoFightTester.cs` (`Assets/Scripts/Debug/AutoFightTester.cs`) é um sistema de teste automatizado e determinístico projetado para diagnosticar problemas de física, colisores, animações, estados de combate e espelhamento em **UnDFight**, sem intervenção manual do desenvolvedor.

- **Proteção:** Envolto por `#if UNITY_EDITOR || DEVELOPMENT_BUILD`.
- **Modo Estritamente Diagnóstico:** Não altera o gameplay; injeta comandos, extrai telemetria quadro a quadro e analisa os resultados contra critérios pré-definidos.
- **Visualização In-Camera:** Gera primitivas 3D translúcidas (`Universal Render Pipeline/Unlit`) que aparecem diretamente nas capturas de tela:
  - **Hurtbox:** Cápsula verde translúcida que acompanha a altura em tempo real.
  - **Hitbox Inativa:** Esfera laranja translúcida acompanhando o osso vinculado.
  - **Hitbox Ativa:** Esfera vermelho vivo translúcida no momento do impacto.

---

## 2. Mudanças no Código para Viabilizar o Harness

| Script | O que mudou | Motivo / Benefício |
|---|---|---|
| `Assets/Scripts/FighterInputConfig.cs` | Adicionadas variáveis de injeção simulada (`SimulatedMovement`, `SimulatedPunch`, `SimulatedAttack2`). | Permite que os testes injetem inputs pelo **mesmo pipeline real** que o jogador usa (`FighterController` e `FighterMovement`), respeitando buffers de input e edge-detection de 1 frame sem atalhos que pulem a máquina de estados. |
| `Assets/Scripts/GameFlowController.cs` | Expostas as propriedades `CharacterPrefabs` e `CharacterDisplayNames`. Adicionado método `StartDirectFightForTesting()`. | Permite ao harness descobrir dinamicamente todos os personagens jogáveis diretamente da fonte de verdade (sem listas hardcoded) e iniciar testes sem depender de cliques na tela de seleção. |
| `Assets/Scripts/FighterController.cs` | Expostos getters públicos `PrimaryAttackTiming`, `SecondaryAttackTiming` e `SecondaryAttackDuration`. | Permite leitura não-destrutiva de timings, membros de ataque e duração dos golpes para calibração diagnóstica do harness. |
| `Assets/Scripts/Debug/AutoFightTester.cs` | Implementada a suíte completa multi-personagem assíncrona, renderizador in-camera de debug 3D e gerador de relatórios. | Automatização total de 9 cenários por personagem em ambos os lados (**P1 e P2**), gravação de CSV/JSON por lutador e compilação do relatório consolidado `Logs/resumo.md`. |

---

## 3. Descoberta Dinâmica dos Personagens

O harness descobre automaticamente qualquer personagem registrado em `GameFlowController.characterPrefabs`. Foram encontrados **7 personagens**:

| # | Nome na Seleção | Prefab | Controller / Avatar | Membro Punch | Membro Attack 2 | Hurtbox Altura |
|---|---|---|---|---|---|---|
| 0 | **Tiago** | `CasualSmilingMan.prefab` | `CasualSmilingMan` | `RightHand` (0.85s) | `RightFoot` (Flying Kick, 1.00s) | 1.80m |
| 1 | **Isaac** | `EmeraldStrength.prefab` | `EmeraldStrength` | `RightHand` (0.85s) | `RightFoot` (Kicking, 1.00s) | **2.00m** ⚠️ |
| 2 | **Lucas** | `ManInBlack.prefab` | `ManInBlack` | `RightHand` (0.85s) | `Head` (Headbutt, 1.00s) | 1.80m |
| 3 | **Enomoto** | `ModernGentleman.prefab` | `ModernGentleman` | `LeftFoot` (0.85s) | `RightFoot` (Macaco Side, 1.00s) | 1.80m |
| 4 | **Jompi** | `ShadowSentinel.prefab` | `ShadowSentinel` | `RightFoot` (0.85s) | `RightHand` (Uppercut, 1.00s) | 1.80m |
| 5 | **Gabutas** | `StudioRocker.prefab` | `StudioRocker` | `Head` (0.85s) | `RightFoot` (Flying Kick, 1.00s) | 1.80m |
| 6 | **Ricardinho** | `VascoJacket.prefab` | `VascoJacket` | `RightHand` (0.85s) | `LeftFoot` (Martelo 2, 1.00s) | 1.80m |

> **Vínculo de Ossos:** Todos os 7 personagens possuem o avatar Humanoid configurado corretamente, e todos os 5 ossos de hitbox (`RightHand`, `LeftHand`, `RightFoot`, `LeftFoot`, `Head`) foram localizados e vinculados com sucesso.

---

## 4. Estrutura dos 9 Cenários Executados por Lutador

Cada personagem é instanciado e testado isoladamente como **P1** contra um oponente neutro padronizado (alvo fixo), e em seguida como **P2** (com vetores de movimento e espelhamento invertidos):

1. **C1 (Reset Inicial / Rematch):** Reseta posições e restaura vida para 100% e FSM para `NeutralState`.
2. **C2 (Locomoção Básica):** Andar para frente, recuar, agachar (valida encolhimento dinâmico da cápsula de `BodyHurtbox`) e pular (valida aterrissagem `IsGrounded`).
3. **C3 (Cruzamento de Lado e Turn180):** Salto cruzando o adversário para validar a rotação suave do `FaceOpponent` e o estado `Turn180`.
4. **C4 (Whiff à Distância):** Soco e Ataque 2 disparados fora de alcance para garantir que não há hits fantasmas.
5. **C5 (Soco Primário Acerta):** Posiciona a 0.9m e desfere soco. Valida dano aplicado, acionamento do hitstop e entrada do oponente em `HitStunState`.
6. **C6 (Ataque Secundário Acerta):** Desfere Attack 2 a 0.9m. Valida dano, hitstop e atordoamento.
7. **C7 (Golpe Agachado e Aéreo):** Soco a partir da postura agachada e soco durante o salto, finalizando com aterrissagem.
8. **C8 (Contra-Ataque / Dano Recebido):** Oponente desfere um golpe no personagem sob teste para validar se ele reage corretamente a impactos.
9. **C9 (Rematch e Reset Final):** Restauração completa de ambos os lutadores.

---

## 5. Resultados e Diagnósticos Obtidos na Bateria Completa

- **Métricas:** 7 personagens x 2 lados (14 suítes) = **36.578 frames** de telemetria e **331 screenshots** capturadas em **225.7 segundos** (~3.7 minutos).
- **Estrutura de Pastas Geradas:**
  ```text
  Logs/
  ├── resumo.md                                   # Relatório executivo consolidado
  ├── AutoFight_Telemetry_Consolidated.csv        # Telemetria geral de 36.578 frames
  ├── Tiago/
  │   ├── P1/ (AutoFight_Telemetry_Tiago_P1.csv/.json, Screenshots/...)
  │   └── P2/ (AutoFight_Telemetry_Tiago_P2.csv/.json, Screenshots/...)
  ├── Isaac/  (P1/ e P2/...)
  ├── Lucas/  (P1/ e P2/...)
  ├── Enomoto/(P1/ e P2/...)
  ├── Jompi/  (P1/ e P2/...)
  ├── Gabutas/(P1/ e P2/...)
  └── Ricardinho/ (P1/ e P2/...)
  ```

### Matriz de Resultados (Personagem x Cenário):

| Personagem | Lado | C1 (Reset) | C2 (Locom.) | C3 (Turn180) | C4 (Whiff) | C5 (Punch) | C6 (Atk 2) | C7 (Ar/Agach.) | C8 (Contra-Atk) | C9 (Reset Fim) |
|---|---|---|---|---|---|---|---|---|---|---|
| **Tiago** | P1/P2 | ✅ OK | ✅ OK | ❌ ANOMALIA | ✅ OK | ✅ OK | ❌ ANOMALIA | ✅ OK | ✅ OK | ✅ OK |
| **Isaac** | P1/P2 | ✅ OK | ✅ OK | ❌ ANOMALIA | ✅ OK | ✅ OK | ❌ ANOMALIA | ✅ OK | ✅ OK | ✅ OK |
| **Lucas** | P1/P2 | ✅ OK | ✅ OK | ❌ ANOMALIA | ✅ OK | ✅ OK | ✅ OK | ✅ OK | ✅ OK | ✅ OK |
| **Enomoto** | P1/P2 | ✅ OK | ✅ OK | ❌ ANOMALIA | ✅ OK | ✅ OK | ✅ OK | ✅ OK | ✅ OK | ✅ OK |
| **Jompi** | P1/P2 | ✅ OK | ✅ OK | ❌ ANOMALIA | ✅ OK | ✅ OK | ❌ ANOMALIA | ✅ OK | ✅ OK | ✅ OK |
| **Gabutas** | P1/P2 | ✅ OK | ✅ OK | ❌ ANOMALIA | ✅ OK | ✅ OK | ❌ ANOMALIA | ✅ OK | ✅ OK | ✅ OK |
| **Ricardinho** | P1/P2 | ✅ OK | ✅ OK | ❌ ANOMALIA | ✅ OK | ✅ OK | ✅ OK | ✅ OK | ✅ OK | ✅ OK |

---

## 6. Anomalias Diagnosticadas para Planejamento de Correção

### Anomalia A: Bloqueio Físico no Salto Cruzado (Cenário 3)
- **Problema:** Em todos os 7 personagens, o saltador não consegue passar para as costas do oponente (`Cruzou = False`).
- **Causa Raiz:** O `CharacterController` do defensor (cápsula sólida de 2.0m de altura) bloqueia o atacante no ar.
- **Ação Planejada:** Ajustar a física de salto / gravidade para atingir altura suficiente ou implementar `Physics.IgnoreCollision` temporário entre os dois `CharacterControllers` enquanto um lutador estiver no ar ultrapassando o adversário.

### Anomalia B: Falha de Alcance no Ataque Secundário (Cenário 6 em 4 Personagens)
- **Problema:** Tiago (`Flying Kick`), Isaac (`Kicking`), Jompi (`Surprise Uppercut`) e Gabutas (`Flying Kick`) falham em acertar o oponente a 0.9m, enquanto Lucas (`Headbutt`), Enomoto (`Macaco Side`) e Ricardinho (`Martelo 2`) acertam perfeitamente.
- **Causa Raiz:** No `Flying Kick`, a perna sobe muito antes de projetar para a frente, e a janela ativa (`activeStartNormalized` a `activeEndNormalized`) expira antes do membro atingir a Hurtbox, ou o offset local da hitbox na ponta do pé está desalinhado. No `Surprise Uppercut` de Jompi, o golpe tem trajetória muito vertical.
- **Ação Planejada:** Calibrar a janela normalizada dos clipes em `FightingPrototypeSetup.cs` ou ajustar os offsets de repouso das hitboxes desses membros.

### Anomalia C: Despadronização da Hurtbox de Isaac (`EmeraldStrength`)
- **Problema:** A `BodyHurtbox` de Isaac está com altura de **2.00m**, enquanto todos os outros 6 personagens possuem **1.80m**.
- **Ação Planejada:** Padronizar a altura da hurtbox em repouso de Isaac para 1.80m para consistência competitiva de gameplay.

### Anomalia D: Desproporção na Duração dos Clipes de Ataque
- **Problema:** As animações de *Surprise Uppercut* (3.48s) e *Kicking* (3.38s) duram mais que o triplo do soco de Tiago (1.03s).
- **Ação Planejada:** Ajustar o parâmetro `playbackSpeed` no `FighterController` para acelerar animações excessivamente longas, mantendo janelas de recovery balanceadas.

---

## 7. Como Executar os Testes

1. **Pelo Menu do Unity:**
   - `UnDFight > Testes > Executar Teste Rápido (Apenas Tiago)` (~30 segundos)
   - `UnDFight > Testes > Executar Teste Multi-Personagem (Todos)` (~3.7 minutos)
2. **Pelo Inspector do `AutoFightTester`:**
   - Selecione o objeto `AutoFightTester` na cena `RuaUrbana`.
   - Ajuste `Single Character Index` (`0` a `6` para um personagem específico, ou `-1` para todos).
   - Ajuste `Side Filter` (`Both`, `OnlyP1`, `OnlyP2`).
   - Dê Play.
3. **Por Linha de Comando (Batchmode):**
   ```powershell
   & "C:\Program Files\Unity\Hub\Editor\6000.5.9f1\Editor\Unity.exe" -projectPath "D:\Programas SSD\Unity\Unity Projects\Teste antigravity" -executeMethod AutoFightTester.RunBatchmode
   ```

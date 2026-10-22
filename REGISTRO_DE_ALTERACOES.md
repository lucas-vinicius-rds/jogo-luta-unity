# 📜 Registro de Alterações e Correções - UnDFight

Documento de acompanhamento contínuo das correções de bugs, refatorações pontuais, arquitetura e novas funcionalidades implementadas no jogo **UnDFight** (Unity 6 / C#).

Este registro consolida todo o histórico técnico, catálogo de animações e decisões de projeto para garantir a rastreabilidade e a integridade da arquitetura do projeto.

---

## 📑 Índice de Seções
0. [Ciclo 0 - Origem, Protótipo Inicial e Arquitetura Base](#-ciclo-0---origem-protótipo-inicial-e-arquitetura-base)
1. [Ciclo 1 (28/09/2026) - Correções de Input e Limpeza de APIs Obsoletas](#-ciclo-1-28092026---correções-de-input-e-limpeza-de-apis-obsoletas)
2. [Ciclo 2 (28/09/2026) - Correção de Hurtbox ao Agachar, Vínculo de Hitboxes a Ossos e Debug Gizmos](#-ciclo-2-28092026---correção-de-hurtbox-ao-agachar-vínculo-de-hitboxes-a-ossos-e-debug-gizmos)
3. [Ciclo 3 (28/09/2026) - Timing de HitStun no Hitstop e Transição Suave de Virada (Turn180)](#-ciclo-3-28092026---timing-de-hitstun-no-hitstop-e-transição-suave-de-virada-turn180)
4. [Ciclo 4 (28/09/2026) - Offsets de Hitbox no Espaço do Osso](#-ciclo-4-28092026---offsets-de-hitbox-no-espaço-do-osso)
5. [Ciclo 4.1 / Prompt 4 (29/09/2026) - Fluxo de Rematch e Camadas de Preview na Câmera](#-ciclo-41--prompt-4-29092026---fluxo-de-rematch-e-camadas-de-preview-na-câmera)
6. [Ciclo 5 (28/09/2026) - Harness de Teste Automático de Combate (AutoFightTester - FASE 1)](#-ciclo-5-28092026---harness-de-teste-automático-de-combate-autofighttester---fase-1)
7. [Ciclo 6 (01/10/2026) - Correção de Duplicação no Rematch e Fim de Luta em P1 vs P2 (Bug 1 e Bug 2)](#-ciclo-6-01102026---correção-de-duplicação-no-rematch-e-fim-de-luta-em-p1-vs-p2-bug-1-e-bug-2)
8. [Ciclo 6.1 (15/10/2026) - Rodada de Referência (Baseline) e Cenário 11 (Varredura de Alcance)](#-ciclo-61-15102026---rodada-de-referência-baseline-e-cenário-11-varredura-de-alcance)
9. [Ciclo 7 (21/10/2026) - Otimização de Performance e Limpeza de Código](#-ciclo-7-21102026---otimização-de-performance-e-limpeza-de-código)
10. [Catálogo Geral de Animações e Parâmetros (Mixamo)](#-catálogo-geral-de-animações-e-parâmetros-mixamo)

---

## 🏛 Ciclo 0 - Origem, Protótipo Inicial e Arquitetura Base

### 🎯 Visão Geral do Jogo
O **UnDFight** foi concebido e estruturado a partir de um protótipo de jogo de luta 2.5D no Unity, com o seguinte fluxo de telas:
```text
Menu Principal → Versus → Modo de Jogo → Seleção de Personagens → Tela VS → Luta → K.O. e Vitória
```

### 🖥 Telas e Menus
1. **Menu Principal:**
   - Título: `UnDFight`.
   - Opções: `VERSUS` e `SAIR`.
   - Identidade visual azul escuro e branco centralizada com imagem de fundo `Assets/telaInicial.png` e áudio tema `Assets/eltema.mp3`.
2. **Menu Versus:**
   - Opções: `PLAYER VS CPU`, `PLAYER VS PLAYER LOCAL` e `VOLTAR`.
3. **Seleção de Personagens:**
   - 7 lutadores com seleção independente para P1 (esquerda) e P2/CPU (direita).
   - Bloqueio de seleção duplicada (P2 não pode escolher o mesmo personagem que P1).
   - Destaque com contorno ciano para personagem em foco e verde-claro para confirmado.
   - Navegação por mouse ou setas/Enter, com cancelamento por Esc.
4. **Sistema de Previews Duplo:**
   - **Preview Lateral 3D:** Renderizado via `RenderTexture` em tempo real com Animator ativo e zoom óptico, com isolamento em camadas específicas de preview (`PreviewLayersMask`).
   - **Retratos dos Cards:** Câmeras estáticas dedicadas por personagem, focadas na cabeça/rosto com Animator desligado para os cards da grade.
5. **Tela de Vitória e K.O.:**
   - Disparada após o término da animação de queda do derrotado.
   - Apresenta vencedor e derrotado com suas poses de vitória/derrota.
   - Opções de ação: `JOGAR NOVAMENTE`, `ESCOLHER PERSONAGENS` e `VOLTAR AO MENU`.

### 👥 Personagens e Identidade
Configurados com prefabs originais adaptados para humanoid avatars e materiais Lit no URP:

| Nome Original do Prefab | Nome Exibido no Jogo | Animação de Vitória (`Assets/Animations/Mixamo/`) |
|---|---|---|
| Casual Smiling Man | **Tiago** | `tigas.fbx` |
| Emerald Strength | **Isaac** | `isaac.fbx` |
| Man in Black | **Lucas** | `lusca.fbx` |
| Modern Gentleman | **Enomoto** | `enomoto.fbx` |
| Shadow Sentinel | **Jompi** | `jompis.fbx` |
| Studio Rocker | **Gabutas** | `gabutas.fbx` |
| Vasco Jacket Avatar | **Ricardinho** | `ricardin.fbx` |

> Animação do Derrotado comum: `X Bot@Surprise Uppercut (1).fbx`.

### 🥊 Mecânicas de Combate e Locomoção
- **Movimento 2.5D:** Controle no plano X/Y com travamento no eixo Z. Suporte a caminhada frontal, recuo, pulos (parado, frente, trás) e agachamento (parado, caminhada agachada).
- **Máquina de Estados (FSM):** Arquitetura desacoplada com `NeutralState`, `AttackState`, `HitStunState` e `KnockoutState`.
- **Colisões e Dano:** Hitboxes vinculadas aos ossos dos membros (`followAnimatedBone`), com verificação de acerto único por golpe via `HashSet<FighterController>` e envio de `DamageData` para a `Hurtbox` do oponente.
- **Hitstop e Reação:** Congelamento temporário da animação do atacante e defensor no impacto, seguido de atordoamento (`HitStun`) com suporte a buffer de input.

---

## 🥊 Ciclo 1: 28/09/2026 - Correções de Input e Limpeza de APIs Obsoletas

### 🎯 Objetivos do Ciclo
1. Eliminar perda de comandos de ataque pressionados durante o atordoamento (*HitStun*).
2. Separar esquemas de controle de Player 1 e Player 2, acabando com teclas hardcoded e interferência mútua no teclado.
3. Desfazer sobreposição de botões no gamepad (`buttonWest` disparava Soco e Ataque 2 simultaneamente).
4. Substituir APIs de busca de objetos depreciadas no Unity 6 (`CS0618`).
5. Garantir compilação com zero erros no editor de código e no Unity.

### 🛠 Alterações Realizadas
- **Input Buffer no HitStun (Bug A):** Adicionado buffer de 0.15s em `FighterController.cs`, gerido por `HitStunState.cs` e consumido ao entrar em `NeutralState.cs`.
- **Separação de Teclas P1/P2 (Bug B):** Criado `FighterInputConfig.cs`. `FighterMovement.cs` e `LocalPlayerTwoInput.cs` agora utilizam configurações exclusivas sem teclas hardcoded.
- **Gamepad Sem Duplicidade (Bug C):** Separado `buttonSouth` para Soco e `buttonWest` para Ataque 2.
- **APIs Obsoletas Unity 6:** Atualizados `FightingPrototypeSetup.cs` e `GameFlowController.cs` para `FindObjectsByType<T>()` e `FindAnyObjectByType<T>()`.

---

## 🛡 Ciclo 2: 28/09/2026 - Correção de Hurtbox ao Agachar, Vínculo de Hitboxes a Ossos e Debug Gizmos

### 🎯 Objetivos do Ciclo
1. **Hurtbox ao agachar (Bug A):** Fazer a cápsula de dano do corpo (`BodyHurtbox`) acompanhar dinamicamente a altura e o centro do `CharacterController` ao agachar e retornar ao normal ao levantar, usando exatamente os mesmos valores para ambos.
2. **Hitboxes seguindo ossos (Bug B):** Ativar a flag `followAnimatedBone`, vinculando cada hitbox ao osso correspondente do Animator (mão direita/esquerda, pé direito/esquerdo, cabeça) com fallback na hierarquia e aviso explícito via `Debug.LogWarning` se algum osso não for encontrado.
3. **Modo de Debug com Gizmos:** Adicionar modo visual na Scene View com toggles `[SerializeField] private bool showDebugGizmos = true` para validar graficamente o alinhamento da Hurtbox e o trajeto das Hitboxes ativas e inativas.

### 🛠 Alterações Realizadas
- **Sincronização Dinâmica da Hurtbox ao Agachar (Bug A):** Implementado `SetBounds` em `Hurtbox.cs` e sincronizado com `CharacterController` no `FighterMovement.cs` e `FightingPrototypeSetup.cs`.
- **Vínculo de Hitboxes a Ossos Animados (Bug B):** `followAnimatedBone = true` ativado por padrão com busca hierárquica e emissão de `Debug.LogWarning`.
- **Gizmos de Debug:** Desenho visual de cápsula para Hurtboxes e esferas aramadas/preenchidas para Hitboxes.

---

## 🔄 Ciclo 3: 28/09/2026 - Timing de HitStun no Hitstop e Transição Suave de Virada (Turn180)

### 🎯 Objetivos do Ciclo
1. **Hitstop consumindo o HitStun (Bug A):** O congelamento de impacto (*hitstop* / frame freeze) pausa o `animator.speed = 0f`, mas o temporizador de atordoamento em `HitStunState.cs` continuava decaindo, reduzindo o tempo de punição real da vítima. Corrigir para pausar o decréscimo do timer enquanto o hitstop estiver ativo, utilizando uma flag/propriedade `IsInHitstop` no controller sem depender de `Time.timeScale`.
2. **Turn180 e Virada Suave (Bug B):** O método `TriggerTurn180()` existia no controller mas nunca era chamado, enquanto `FaceOpponent()` em `FighterMovement.cs` girava o lutador instantaneamente (snap abrupto de 180°). Fazer `FaceOpponent()` detectar quando o oponente cruza de lado, disparar `TriggerTurn180()` sem entrar em loop e rotacionar o personagem de forma suave com `Quaternion.RotateTowards`, ignorando a virada durante ataques, atordoamento ou saltos.
3. **Validação do Animator:** Verificar a existência do estado e de triggers no `BaseFighter.controller`.

### 🛠 Alterações Realizadas
- **Pausa do Temporizador de HitStun durante Hitstop (Bug A):** Propriedade `IsInHitstop => hitstopCoroutine != null` em `FighterController.cs` e avanço condicionado de `elapsedTime` em `HitStunState.cs`.
- **Virada Suave e Turn180 (Bug B):** Adicionado `turnSpeed = 720°/s`, bloqueio durante ataque/hitstun/ar e acionamento único de `TriggerTurn180()` em `FighterMovement.FaceOpponent()`.

### ⚠️ Nota Técnica sobre o Animator (BaseFighter.controller)
- O **Estado** `Turn180` **EXISTE** na camada base do Animator Controller (`BaseFighter.controller`), com clipe de virada e transição de saída com Exit Time (0.9) para `Locomotion`.
- O **Parâmetro Trigger** `Turn180` **NÃO EXISTE**. O controller executa `CrossFadeAnimation(Turn180AnimHash)` diretamente pelo hash numérico do estado.

---

## 🦴 Ciclo 4: 28/09/2026 - Offsets de Hitbox no Espaço do Osso

### 🎯 Alterações Realizadas
- A posição de projeto das hitboxes, criada no espaço do lutador, agora é convertida uma vez para o espaço local do osso em `Hitbox.BindBone()`. A sincronização usa esse offset convertido, em vez de substituir a posição pela origem do osso.
- O fallback de `HeadHitbox` foi corrigido para um ponto central à frente da cabeça, em vez de reutilizar o offset da mão esquerda.
- Os Gizmos agora mostram, para cada hitbox, o golpe configurado, o membro e o estado `ATIVA`/`inativa`.
- O prefab ativo do Player 2 (`EmeraldStrength`) foi alinhado: `BodyHurtbox.height = 2`, igual ao `CharacterController` e ao setup.

---

## 🔁 Ciclo 4.1 / Prompt 4: 29/09/2026 - Fluxo de Rematch e Camadas de Preview na Câmera

### 🎯 Objetivos do Ciclo
1. **Reiniciar partida mantendo os lutadores (Bug A do Prompt 4):**
   - Implementar opção "Jogar novamente" na tela de vitória que reinicie o combate instantaneamente sem recarregar a cena Unity nem passar pela tela de seleção.
   - Preservar as instâncias dos lutadores, restaurando posições e rotações iniciais exatas (`initialPlayerPosition`, `initialOpponentPosition`), vida máxima e estado neutro da FSM.
2. **Ocultar camadas de preview durante a luta (Bug B do Prompt 4):**
   - Ao transitar da seleção de personagens para a arena de combate, desativar da câmera de gameplay principal (`TekkenCamera`) a renderização das camadas de preview 3D (`cullingMask`), evitando sobreposição visual indesejada.

### 🛠 Alterações Realizadas
- **GameFlowController.cs:**
   - Adicionadas variáveis de estado `initialPlayerPosition`, `initialPlayerRotation`, `initialOpponentPosition`, `initialOpponentRotation` e flag `hasInitialTransforms`, capturadas no término de `SetupFight()`.
   - Implementado `ResetRound()`: restaura posições e rotações originais, zera velocidades e inércia do `FighterMovement`, chama `HealthSystem.ResetHealth()` e faz transição forçada para `NeutralState`.
   - Implementado `Rematch()`: executa `ResetRound()` e define `screen = FlowScreen.Fight`.
   - Implementado menu de vitória com navegação vertical entre "Jogar novamente" (índice 0) e "Escolher personagens" (índice 1).
   - Implementado `SetMainCameraPreviewVisibility(bool visible)` para ligar/desligar o `cullingMask` das camadas reservadas a previews.

---

## 🤖 Ciclo 5: 28/09/2026 - Harness de Teste Automático de Combate (AutoFightTester - FASE 1)

### 🎯 Objetivos do Ciclo
1. Criar um harness de testes roteirizados e automatizados (`AutoFightTester.cs`) protegido por `#if UNITY_EDITOR || DEVELOPMENT_BUILD` que execute cenários de combate completos em Play Mode sem intervenção manual.
2. Injetar inputs nos lutadores pelo caminho real do `FighterController` e `FighterMovement` (através de `FighterInputConfig`), preservando edge-detection e regras de input buffer de HitStun.
3. Cobrir cenários essenciais:
   - Locomoção (andar, recuar, agachar e pular).
   - Cruzamento de lado e acionamento suave de `Turn180`.
   - Golpes primários e secundários que erram (*Whiff*).
   - Golpes primários e secundários que acertam (*Hit* com *Hitstop* e *HitStun*).
   - Golpes agachados e no ar.
   - Golpes desferidos pelo Player 2.
   - Rematch e reset de posições/estados.
4. Coletar e exportar telemetria quadro a quadro em `Logs/AutoFight_Telemetry.csv` e `Logs/AutoFight_Telemetry.json`.
5. Salvar screenshots dos momentos de Início, Meio e Fim dos golpes em `Logs/Screenshots/` com visualização 3D de cápsula verde translúcida (Hurtbox), esferas laranja (Hitboxes inativas) e esferas vermelho vivo (Hitbox ativa no impacto).

### 🛠 Alterações Realizadas
- **FighterInputConfig.cs:** Adicionadas propriedades de injeção de input simulado (`SimulatedMovement`, `SimulatedPunch`, `SimulatedAttack2`) com edge-detection idêntico ao hardware físico, sem pular nenhuma verificação lógica do `FighterController`.
- **GameFlowController.cs:** Adicionado método `StartDirectFightForTesting()` para permitir início instantâneo da luta em modo local/teste, evitando travamento na tela de menu ou destruição indevida dos lutadores da cena.
- **Assets/Scripts/Debug/AutoFightTester.cs:** Implementado o script completo de automação com 9 cenários de teste, gravação de telemetria completa (FSM, Animator, parâmetros, posições, hitboxes e hurtboxes), captura automática de PNGs e renderização visual in-camera das caixas de colisão.

### 📂 Arquivos Afetados no Ciclo 5
| Arquivo | Tipo de Alteração | Descrição |
|---|---|---|
| `Assets/Scripts/FighterInputConfig.cs` | Modificado | Suporte a injeção de comandos simulados com edge-detection. |
| `Assets/Scripts/GameFlowController.cs` | Modificado | Adicionado método `StartDirectFightForTesting()`. |
| `Assets/Scripts/Debug/AutoFightTester.cs` | Criado | Script do harness de testes automatizados com telemetria e screenshots. |
| `REGISTRO_DE_ALTERACOES.md` | Modificado | Documentação do Ciclo 5. |

### 🧪 Status de Compilação e Execução
- **Compilação Unity MCP:** `0 Erros, 0 Avisos`.
- **Execução do Harness:** Bateria de testes concluída com sucesso:
   - **2343 frames** de telemetria gravados em CSV e JSON dentro de `Logs/`.
   - **21 screenshots** salvas em `Logs/Screenshots/` com caixas de colisão 3D claramente visíveis.

---

## 🥊 Ciclo 6: 01/10/2026 - Correção de Duplicação no Rematch e Fim de Luta em P1 vs P2 (Bug 1 e Bug 2)

### 🎯 Objetivos do Ciclo
1. **Modelos duplicados ao clicar em "Jogar novamente" (Bug 1):**
   - Ao clicar em "Jogar novamente" na tela de vitória/derrota, os modelos dos personagens apareciam duplicados e ficavam executando animações.
   - Diagnóstico: `CreateVictoryDisplays()` gerava clones 3D nas camadas 18 e 19 que não eram destruídos no `Rematch()`. Além disso, a câmera de combate estava configurada com máscara `~(1 << 20..30)`, deixando as camadas 18 e 19 visíveis. Adicionalmente, as animações de vitória sobrescreviam o `RuntimeAnimatorController` original do lutador sem restauração.
   - Solução: Implementar `DestroyVictoryDisplays()`, restaurar animators originais no `ResetRound()`, destruir os modelos em todas as rotas de saída e estender o culling mask da câmera para cobrir camadas 18 a 30.
2. **P1 vs P2 não vai para a tela de derrota (Bug 2):**
   - No modo local com Player 1 e Player 2, quando um lutador era derrotado, a luta não terminava: o lutador derrotado ficava parado e a tela de derrota não abria.
   - Diagnóstico: `GameFlowController` não possuía checagem ativa de fim de combate no `Update()` durante `FlowScreen.Fight`, confiando exclusivamente no evento C# `HealthSystem.OnKnockout`. Se o evento falhasse ou ocorresse oscilação/dessincronia no modo local com `LocalPlayerTwoInput`, o fluxo nunca era notificado e o personagem ficava congelado no `KnockoutState`.
   - Solução: Adicionar verificação contínua no `Update()` via `GetDefeatedFighter()` cobrindo `currentHealth <= 0`, `IsDead == true` e `KnockoutState` para ambos os lutadores, em qualquer modo (IA ou Local), com trava única `TriggerFighterKnockout(loser)`.
3. **Cenário 10 de Verificação no Harness (`AutoFightTester.cs`):**
   - Adicionar cenário cobrindo os 4 subcasos de nocaute e rematch:
     - Contra IA: derrotar P1 -> tela de derrota abre -> rematch -> exatamente 2 lutadores ativos e 0 previews.
     - Contra IA: derrotar P2 (IA) -> tela de derrota abre -> rematch -> exatamente 2 lutadores ativos e 0 previews.
     - Modo Local P1 vs P2: derrotar P1 -> tela de derrota abre -> rematch -> exatamente 2 lutadores ativos e 0 previews.
     - Modo Local P1 vs P2: derrotar P2 -> tela de derrota abre -> rematch -> exatamente 2 lutadores ativos e 0 previews.
   - Validar execução completa sem regressões e atualizar telemetria e `resumo.md`.

### 🛠 Alterações Realizadas
- **GameFlowController.cs:**
   - `DestroyVictoryDisplays()`: Destrói `victoryWinnerObject` e `victoryLoserObject`, suas câmeras dedicadas e libera os `RenderTexture`s. Invocado em `Rematch()`, `ConfirmVictoryOption()`, `BeginSelection()`, `SetupFight()` e `StartDirectFightForTesting()`.
   - `SetMainCameraPreviewVisibility()`: Ajustado para mascarar da camada 18 à 30 (`layers 18..30`), impedindo vazamento de previews na câmera de jogabilidade.
   - Preservação de Animators: Variáveis `originalPlayerAnimator` e `originalOpponentAnimator` salvas em `SetupFight()` e restauradas em `ResetRound()`.
   - `Update()` / `GetDefeatedFighter()`: Monitoramento contínuo do estado de derrota de ambos os personagens em combate com acionamento por trava única (`TriggerFighterKnockout`).
   - Getters de estado: Expostas propriedades públicas `IsVictoryScreen`, `IsFightScreen`, `CurrentPlayer` e `CurrentOpponent`.
- **AutoFightTester.cs:**
   - `ExecuteScenarioKnockoutAndRematchFlow()` e `TestKnockoutAndRematchSubcase()`: Implementação do Cenário 10 executando os 4 subcasos solicitados.
   - Atualização de `scenarioHeaders` para incluir C10 na matriz diagnóstica do relatório `resumo.md`.

### 📂 Arquivos Afetados no Ciclo 6
| Arquivo | Tipo de Alteração | Descrição |
|---|---|---|
| `Assets/Scripts/GameFlowController.cs` | Modificado | Limpeza de displays de vitória no Rematch, restauração de animators, culling mask 18..30 e detecção de derrota em P1 vs P2. |
| `Assets/Scripts/Debug/AutoFightTester.cs` | Modificado | Adicionado Cenário 10 cobrindo os 4 subcasos de nocaute/rematch e exibição na matriz de resumo. |
| `REGISTRO_DE_ALTERACOES.md` | Modificado | Documentação do Ciclo 6. |

### 🧪 Status de Compilação e Execução
- **Compilação Unity MCP:** `0 Erros, 0 Avisos`.
- **Execução do Harness:** Bateria de testes executada com sucesso:
   - **Cenário 10:** Validado com **✅ OK** em ambos os lados (P1 e P2), cobrindo todos os 4 subcasos de nocaute e rematch sem anomalias.
   - **58 screenshots** salvas (incluindo as telas de derrota dos 4 subcasos em `Logs/Tiago/P1/Screenshots/`).
   - **11.674 frames de telemetria** registrados e relatório `Logs/resumo.md` atualizado com a matriz completa de C1 a C10.

---

## 📊 Ciclo 6.1: 15/10/2026 - Rodada de Referência (Baseline) e Cenário 11 (Varredura de Alcance)

### 🎯 Objetivos do Ciclo
1. Confirmar que o Play normal no Unity Editor é estritamente opt-in (`AutoFightTester` permanece `enabled = false` no `Awake()` sem `SessionState` ou batchmode).
2. Executar a bateria multi-personagem completa (7 personagens, P1 e P2, cenários C1 a C11) e arquivar o resultado de referência em `Logs/baseline_resumo.md`.
3. Verificar a cobertura integral do Cenário 10 (nocaute de P1/P2 contra IA e em modo Local P1 vs P2, abertura de `FlowScreen.Victory` e `flow.Rematch()` real com exatamente 2 lutadores e 0 previews) e do Cenário 11 (Range Sweep de 0.5m a 1.6m).

### 🧪 Resultados da Rodada Baseline
- **Execução:** 7 personagens x 2 lados (14 suítes), **150.764 frames** de telemetria e **388 screenshots** em **632,2s** (~10,5 min).
- **Arquivos Gerados:** `Logs/resumo.md` e cópia idêntica `Logs/baseline_resumo.md`.
- **Cenário 10 (Nocaute e Rematch Real):** ✅ OK em todos os 7 personagens (P1 e P2).
- **Cenário 11 (Range Sweep 0.5m–1.6m):** Identificou alcance `0.0m` (nunca acerta) no `Attack2` de Tiago, Jompi e Gabutas, enquanto o `Attack2` de Isaac acerta até `1.4m` (apesar de falhar no C6 por janela/timing a 0.9m com input simulado).

---

## ⚡ Ciclo 7: 21/10/2026 - Otimização de Performance e Limpeza de Código

### 🎯 Objetivos do Ciclo
1. Otimizar e limpar a pasta `Assets/Scripts/` sem alterar em nada o comportamento do jogo (combate, colisões, estados, inputs e animações idênticos).
2. Eliminar gargalos críticos de alocação de lixo no heap (GC allocations) e tempo de CPU no `Update`.
3. Executar o trabalho em etapas atômicas com commits git intermediários, compilação limpa (0 erros) e validação pelo harness de testes.
4. Medir e comparar a performance no cenário padrão de 300 frames de sparring ativo (Tiago vs CPU Isaac).

### 🛠 Alterações Realizadas por Sistema

#### 1. `FighterMovement.cs`
- **Cache de Parâmetros do Animator:** Eliminadas chamadas repetidas a `animator.parameters` (que alocavam novos arrays a cada chamada dentro de `HasParameter`) e substituídas por flags cacheadas (`hasParamCrouch`, `hasParamJump`, etc.) e hashes numéricos pré-calculados (`CrouchHash`, `MoveDirectionHash`, etc.).
- **Cache de Rotações Fixas:** Rotações para a direita (90°) e esquerda (-90°) pré-armazenadas como `Quaternion` estáticos, eliminando invocações de `Quaternion.Euler` no `FaceOpponent()`.
- **Guarda de Agachamento:** `CharacterController.height`, `center` e `BodyHurtbox.SetBounds` agora só são recalculados quando há transição de postura, evitando reinvalidações contínuas de bounds no PhysX.

#### 2. `TekkenCamera.cs`
- **Cache de Componente:** Referência da `Camera` cacheada no `Awake()` e acessada diretamente, eliminando `GetComponent<Camera>()` a cada frame no `LateUpdate()`.
- **Limpeza de Linha Redundante:** Removida reatribuição supérflua de `targetPosition.y`.

#### 3. `Hitbox.cs` e `Hurtbox.cs`
- **TryGetComponent:** Substituído `GetComponent<Hurtbox>()` por `TryGetComponent` no processamento de impactos.
- **Proteção de Vínculo de Ossos:** Adicionada flag `boneBindingAttempted` para evitar varreduras completas da hierarquia a cada frame em caso de ausência de osso.
- **Guarda de Bounds na Hurtbox:** Adicionada verificação de tolerância em `Hurtbox.SetBounds()` para não marcar colliders como sujos desnecessariamente quando a altura e centro não mudaram.

#### 4. `FighterInputConfig.cs`
- **Eliminação de Exceções em Leitura de Teclas:** Substituído bloco `try-catch` per-frame por validação bounds-safe da enum `Key` (`key > Key.None && (int)key <= (int)Key.IMESelected`), evitando overhead de exceções na leitura contínua do teclado.
- **Cache de Strings de Entrada:** Strings de debug geradas para ataques foram pré-formatadas, eliminando interpolações de string no heap gerenciado durante comandos de combate.

#### 5. `FighterController.cs` e `AttackState.cs`
- **Cache de Oponente e Nomes no OnGUI:** Referências ao oponente (`FighterController`, `HealthSystem`, `FighterSparringAI`) e nomes formatados foram cacheados, evitando buscas de componentes e operações de `Replace()` a cada frame de GUI.
- **Duração de Ataque no AttackState:** `CurrentAttackDuration` e timing calculados e cacheados uma única vez no `Enter()` do estado de ataque, em vez de consultar `animator.GetCurrentAnimatorStateInfo(0)` múltiplos frames consecutivos no `Update()`.
- **Limpeza de Instanciação Duplicada:** Removida recriação redundante dos estados FSM no `Awake()`.
- **Remoção de Código Morto:** Removidos campos legados não lidos (`wasSpaceHeld`, `wasJHeld`, `wasKHeld`, `wasEnterHeld` e `lastDetectedInput`).

#### 6. `FighterSparringAI.cs`
- **Gestão Segura de Corrotinas:** Corrotina de movimentação da IA agora é rastreada e interrompida apropriadamente antes de novos comandos e limpa no `OnDisable()`.

#### 7. `GameFlowController.cs`
- **FindObjectsSortMode.None:** Consultas de busca de lutadores atualizadas para `FindObjectsByType<FighterController>(FindObjectsSortMode.None)`, evitando ordenação de objetos no Unity 6.
- **Uso de Referências Cacheadas:** `CharacterController` lido diretamente de `FighterMovement.CharacterController`.
- **Máscara de Layers Pré-calculada:** Visibilidade de previews agora usa constante binária `PreviewLayersMask = 0x7FFC0000`, eliminando loops repetidos.
- **Limpeza de Usings:** Removidos `System.Linq` e `UnityEditor` não utilizados.

---

### 🚀 Fase 2: Otimizações Estruturais de Performance Adicionais

#### 1. `Hitbox.cs`
- **Eliminação do `Update()`:** O método `Update()` foi completamente removido do componente `Hitbox`. A checagem de sobreposição (`CheckOverlaps()`) foi consolidada no `LateUpdate()`, logo após o alinhamento com o esqueleto (`SyncBonePosition()`). Isso eliminou 10 invocações desnecessárias do player loop do Unity por frame e removeu o duplo sincronismo de ossos por frame durante ataques ativos.

#### 2. `TekkenCamera.cs`
- **Remoção de Busca per-frame:** Removido o fallback `GetComponent<Camera>()` do `LateUpdate()`.
- **Eliminação de Matriz Suja:** `targetCamera.orthographic = true` foi movido para o `Awake()`, evitando reinvalidação contínua da matriz de projeção da câmera a cada frame.
- **Tolerância de OrthographicSize:** A alteração do zoom ortográfico agora só aplica valor quando a variação for superior a `0.001f`.

#### 3. `FighterController.cs`
- **Cache de Rects no OnGUI:** Retângulos de layout da interface (`CombatPanelRect`, `ControlsPanelRect`) transformados em campos estáticos imutáveis, eliminando instanciações repetidas de `Rect` a cada ciclo de GUI.
- **Eliminação de Reflexão per-frame:** Substituídas as chamadas `CurrentState?.GetType().Name` por leitura direta da propriedade cacheada `CurrentStateName`, zerando alocações de string por reflexão no loop visual.
- **TryGetComponent no Reset:** Referência ao oponente obtida com segurança e cacheada sem `GetComponent` repetido.

#### 4. `GameFlowController.cs`
- **Hashes Numéricos no Animator:** Substituídas consultas por string (`IsName("Dying")` e `"Idle"`) por hashes inteiros pré-calculados (`DyingAnimHash` e `IdleAnimHash`).
- **Cache de Câmera e Máscara Binária:** Eliminadas buscas de câmera e loops iterativos de layer em `SetMainCameraPreviewVisibility()`, utilizando `cachedMainCamera` e constante binária `PreviewLayersMask`.
- **TryGetComponent e Reuso de Propriedades:** `ResetFighterPhysicsAndState` passou a ler `fighter.Movement` diretamente e usar `TryGetComponent` para `Rigidbody` e `CharacterController`.

---


---

### Fase 3: Simplificar e Organizar (Refatoracao Arquitetural e Deduplicacao)

#### 1. Modularizacao Coesa de GameFlowController (Partial Classes)
Para resolver o arquivo monolitico de ~970 linhas sem criar camadas de abstracao artificiais e preservando 100% da serializacao do Inspector da Unity e das referencias em cenas/prefabs, a classe foi separada por dominios de responsabilidade via partial class:
- **Assets/Scripts/GameFlowController.cs (Nucleo):** Coordena o ciclo de vida da cena, a maquina de estados de fluxo (FlowScreen), a inicializacao e reset de partidas (SetupFight, ResetRound, Rematch), verificacoes unificadas de nocaute e o hook de testes automatizados (StartDirectFightForTesting).
- **Assets/Scripts/GameFlowController.UI.cs (Interface e Menus):** Concentra toda a renderizacao visual imediata (OnGUI), geracao de estilos (BuildStyles), layout responsivo da grade de selecao de lutadores, tela de Versus e tela de K.O./Vitoria.
- **Assets/Scripts/GameFlowController.Previews.cs (Modelos e Cameras de Preview):** Responsavel pela instanciacao, ciclo de vida e liberacao de memoria dos modelos 3D de exibicao, cameras ortograficas com culling masks isoladas (18 a 30), RenderTexture e injecao de clipes de vitoria/derrota no Animator.

#### 2. Modularizacao de FighterController
- **Assets/Scripts/FighterController.cs:** Focado estritamente nas regras de combate, coordenacao com o FighterMovement, HealthSystem, hitboxes e controle da FSM.
- **Assets/Scripts/FighterController.DebugUI.cs:** Painel de depuracao em tela (OnGUI) com controles virtuais e telemetria de combate do Player 1 isolado em arquivo proprio, mantendo o script principal enxuto.

#### 3. Refatoracao da Maquina de Estados (FSM)
- **FighterStateBase.cs:** Criada classe base abstrata implementando a interface IFighterState com metodos virtuais Enter, Update e Exit.
- **Simplificacao dos Estados:** NeutralState, AttackState, HitStunState e KnockoutState agora herdam de FighterStateBase, eliminando implementacoes vazias redundantes.
- **Metodo LockDown() no FighterController:** Criado metodo utilitario atomico (fighter.LockDown()) que trava a locomocao, desliga todas as hitboxes e limpa buffers de ataque em uma unica linha. Utilizado uniformemente na entrada de HitStunState e KnockoutState.

#### 4. Deduplicacao de Logica entre Player 1 e Player 2
- **Verificacao de Derrota:** Criado IsFighterDefeated(FighterController fighter) para unificar o teste de saude e estado de nocaute, simplificando GetDefeatedFighter().
- **Inscricao de Eventos:** Criado WireKnockoutEvents(FighterController fighter, bool subscribe) em GameFlowController, eliminando blocos repetidos de inscricao/desinscricao em SetupFight.
- **Restauracao de Animator:** Criado RestoreOriginalAnimator(FighterController fighter, RuntimeAnimatorController original) em ResetRound, garantindo que poses de vitoria/derrota sejam limpas de forma simetrica para ambos os combatentes.
- **Constantes de Posicionamento:** Posicoes e rotacoes de spawn foram substituidas por constantes estaticas imutaveis (DefaultP1Position, DefaultP2Position, DefaultP1Rotation, DefaultP2Rotation).

---

### Comparativo Consolidado de Performance (Baseline vs. Fases 1, 2 e 3)
Cenario: 300 frames de sparring ativo (Tiago vs CPU Isaac):

| Metrica | Antes (Baseline) | Fase 1 | Fase 2 | Fase 3 (Final) | Variacao Total vs Baseline |
|---|---|---|---|---|---|
| **Tempo Medio de Frame** | 3,89 ms | 2,73 ms | 2,74 ms | **2,83 ms** | **-27,2%** (mais rapido) |
| **FPS Medio** | 257,3 FPS | 366,0 FPS | 365,5 FPS | **353,6 FPS** | **+96,3 FPS (+37,4%)** |
| **Alocacao de GC por Frame** | 30.829,8 bytes (30,11 KB) | 24.963,7 bytes (24,38 KB) | 24.779,7 bytes (24,20 KB) | **24.768,0 bytes (24,19 KB)** | **-6.061 bytes/frame (-19,7%)** |
| **Scripts no Update (BehaviourUpdate)** | 0,155 ms | 0,037 ms | 0,033 ms | **0,036 ms** | **-76,8% de tempo de CPU!** |

### Validacao Funcional
- **Erros de Compilacao:** 0 erros no Unity MCP.
- **Harness de Testes:** Bateria de testes automatizados (AutoFightTester) executada com sucesso, mantendo 100% de paridade em todos os cenarios e telemetrias em relacao a baseline.

---

### âš™ï¸ Ajuste de Qualidade e Taxa de Quadros (Target Frame Rate e V-Sync)
- **ConfiguraÃ§Ã£o de V-Sync:** QualitySettings.vSyncCount = 1 aplicado em ProjectSettings/QualitySettings.asset (nÃ­veis Mobile e PC).
- **Target Frame Rate:** Adicionado targetFrameRate = 60 e enableVSync = true em GameFlowController.cs (com campos configurÃ¡veis no Inspector e aplicados no Awake()).
- **Resultado:** O jogo agora roda travado em **60 FPS cravados** (16,66 ms por frame), eliminando aquecimento desnecessÃ¡rio de GPU/CPU, oscilaÃ§Ãµes de frame pacing (stuttering) e garantindo a resposta padrÃ£o esperada de jogos de luta de precisÃ£o.

## 🎬 Catálogo Geral de Animações e Parâmetros (Mixamo)

Referência dos assets de animação disponíveis em `Assets/Animations/Mixamo/`.

### 1. Animações Ligadas ao Controlador (`BaseFighter.controller`)

| Arquivo | Uso Atual / Significado |
|---|---|
| `Fighter_Idle.anim` | Idle base do lutador em pose neutra. |
| `X Bot@Bouncing Fight Idle.fbx` | Idle de luta alternativo, com balanço contínuo. |
| `X Bot@Walking.fbx` | Caminhada / locomoção para frente. |
| `X Bot@Running Backward.fbx` | Recuo / locomoção para trás. |
| `X Bot@Standing To Crouched.fbx` | Transição de em pé para agachado (ao pressionar para baixo). |
| `X Bot@Crouched To Standing.fbx` | Transição de agachado para em pé (ao soltar direcional baixo). |
| `X Bot@Crouch Walk Forward.fbx` | Deslocamento agachado para frente. |
| `X Bot@Crouch Walk Back.fbx` | Deslocamento agachado para trás. |
| `X Bot@Crouching Idle.fbx` | Agachamento parado / postura agachada contínua. |
| `X Bot@Jumping1.fbx` | Pulo parado (`jumping1`), sem deslocamento horizontal. |
| `X Bot@Jump.fbx` | Pulo para trás. |
| `Pulo pra frente.fbx` | Pulo para frente. |
| `X Bot@Punching.fbx` | Soco principal (`Punch` / ataque primário). |
| `X Bot@Kicking.fbx` | Chute principal (`Attack2` / ataque secundário). |
| `X Bot@Hit To Body.fbx` | Reação ao receber golpe no corpo (`HitStun`). |
| `X Bot@Walking Turn 180.fbx` | Virada de 180 graus quando oponente troca de lado (`Turn180`). |

### 2. Ataques e Golpes Disponíveis para Expansão

| Arquivo | Possível Uso / Aplicação |
|---|---|
| `X Bot@Cross Punch.fbx` | Soco cruzado; alternativa para ataque primário ou golpe pesado. |
| `X Bot@Surprise Uppercut.fbx` | Uppercut surpresa; golpe vertical de curto alcance. |
| `X Bot@Surprise Uppercut (1).fbx` | Variação do uppercut / animação padrão de derrota no K.O. |
| `X Bot@Headbutt.fbx` | Cabeçada; ataque de curta distância. |
| `X Bot@Flying Kick.fbx` | Chute voador; ataque aéreo com avanço horizontal. |
| `X Bot@Kicking (1).fbx` | Variação do chute; comparar alcance e timing com `Kicking.fbx`. |
| `X Bot@Martelo 2.fbx` | Golpe de martelo; ataque descendente ou pesado. |
| `X Bot@Macaco Side.fbx` | Golpe lateral / acrobático estilo capoeira. |

### 3. Reações, Estados e Utilitários

| Arquivo | Possível Uso / Aplicação |
|---|---|
| `X Bot@Dying.fbx` | Queda e nocaute no chão após perda total de vida. |
| `X Bot@Getting Up.fbx` | Levantamento e recuperação após knockdown. |
| `X Bot@Reaction.fbx` | Reação de impacto genérica ou defesa. |
| `X Bot@Kneeling Down.fbx` | Postura de agachamento alternativa. |
| `X Bot@Jumping.fbx` | Pulo original alternativo. |
| `X Bot@Step Backward.fbx` | Passo curto para trás (backdash). |
| `X Bot@Walk Backwards.fbx` | Caminhada alternativa para trás. |
| `X Bot@Silly Dancing.fbx` | Provocação (taunt) ou menu. |

### 4. Parâmetros do Animator (`BaseFighter.controller`)
- `Punch` (*Trigger*): Dispara o golpe primário (soco).
- `Attack2` (*Trigger*): Dispara o golpe secundário (chute).
- `Hit` (*Trigger*): Aciona o estado de dano / reação a golpe.
- `Crouch` (*Bool*): Mantém o lutador na postura de agachamento.
- `CrouchDirection` (*Float*): Diferencia locomoção agachada para frente (+1) e para trás (-1).
- `Jump` (*Trigger*): Dispara o pulo.
- `JumpType` (*Int*): `0` para pulo parado, `1` para pulo recuando, `2` para pulo avançando.
- `Speed` (*Float*): Controla a velocidade de locomoção no Blend Tree da caminhada.

### 5. Diretrizes para Incorporação de Novas Animações
- **Root Motion:** Para golpes com deslocamento, validar se o clipe tem root motion e se o avanço deve ser persistido via script pelo `CharacterController`.
- **Janelas de Contato:** Ao configurar novos ataques, calibrar a janela de ativação da hitbox no `Hitbox.cs` para bater exatamente com a extensão máxima do membro.
- **Tempo de Salto:** Adequar a duração visual do clipe de pulo à física de gravidade e velocidade vertical do `FighterMovement.cs`.

## 2026-10-22 — Reparo de retargeting Humanoid (Unity MCP)

- Projeto confirmado pelo bridge MCP: Unity 6000.5.9f1, Teste antigravity, porta local 7890. Acesso pela API HTTP do plugin com.anklebreaker.unity-mcp 2.39.5; ferramentas MCP não expostas diretamente nesta sessão.
- Backup realizado ANTES de alterar assets: `Backups/RigRepair_20261022_181732`. Inclui os 16 FBXs Meshy e seus .meta, os 7 prefabs e .meta, Fighter_Idle.anim e .meta, FightingPrototypeSetup.cs e .meta e este registro original. Integridade em manifest-sha256.json (SHA-256).
- Diagnóstico: 38 FBXs Mixamo Humanoid válidos; 9 Meshy Humanoid válidos; 7 Meshy Generic/NoAvatar (6 Character_output e Running do Watchful Executive). audit-before.json contém caminhos, mapeamentos e clipes.
- Coluna Meshy confirmada: Hips > Spine02 (Spine) > Spine01 (Chest) > Spine (UpperChest). Avatars existentes com pose válida segundo AvatarSetupTool do Editor. A quantidade diferente de ossos não exige igualar os esqueletos.
- Fighter_Idle.anim: 182 curvas Transform, nenhuma curva Animator/Humanoid; incompatível como clipe universal entre esqueletos diferentes. Preservado integralmente no backup.
- Prefabs CasualSmilingMan, ManInBlack e VascoJacket contêm overrides manuais de rotação em ossos. Os Animators filhos estão desativados corretamente; não há dois Animators ativos disputando o rig.
- Baseline visual do idle Mixamo capturada em before-*.png, com cópias em cena de preview isolada. Não foi preciso Blender: nenhum problema estrutural de malha/rig foi demonstrado até aqui.
- Planejado: converter os 7 Generic para Humanoid/CreateFromThisModel com Avatar próprio; restaurar rotações de ossos sobrescritas nos 3 prefabs; tornar Fighter_Idle.anim Humanoid preservando seu GUID. Não executar o gerador FightingPrototypeSetup, pois ele apaga/recria assets.

### Alterações efetivamente aplicadas

- Sete .fbx.meta: animationType Generic (2) -> Humanoid (3); avatarSetup NoAvatar (0) -> CreateFromThisModel (1). Cada modelo gera seu próprio Avatar; nenhum Avatar Mixamo foi copiado para um esqueleto Meshy.
- Os seis Character_output corrigidos são Emerald Strength, Modern Gentleman poised, Shadow Sentinel, Studio Rocker Pose, The Watchful Executive e Vasco Jacket Avatar. O sétimo arquivo é Animation_Running_withSkin do The Watchful Executive.
- Três prefabs: restauradas somente 8 propriedades de rotação de ossos (CasualSmilingMan/LeftLeg: 2; ManInBlack/RightShoulder: 4; VascoJacket/RightUpLeg: 2). Materiais, controllers, hierarquias e referências foram preservados. A serialização adicional de campos padrão feita ao salvar pelo Editor foi omitida para manter o diff restrito às rotações pretendidas; reimportação solicitada pelo MCP.
- Fighter_Idle.anim agora usa uma cópia serializada do clipe Humanoid X Bot@Bouncing Fight Idle.fbx: 130 curvas, zero curvas Transform diretas. GUID preservado: 8f693952a6f913b46b09da7f8676b567. A animação anterior permanece no backup. Nenhum controller/scene/prefab possui referência a esse GUID atualmente; esta correção evita uso futuro de um idle específico de Meshy como universal.
- Nenhum arquivo foi apagado. Nenhum FBX foi regravado (SHA-256 dos 16 FBXs Meshy conferido contra o backup). Nenhuma malha, peso, osso ou script de gameplay foi alterado. Blender não foi necessário.

### Validação e evidências

- audit-after.json: todos os 54 modelos inspecionados com Humanoid e Avatar válido (38 Mixamo + 16 Meshy).
- validation-result.json: 23 modelos (7 personagens Mixamo + 16 Meshy), 39 clipes, 3 tempos por clipe = 2.691 amostras. prefab-validation-result.json: 7 prefabs, mesmos 39 clipes e 3 tempos = 819 amostras. Total: 3.510 amostras, zero falhas na validação final.
- Amostragem via PlayableGraph manual em cenas de preview isoladas, com AlwaysAnimate para impedir descarte por câmera e root motion desativado. Conferidos valores finitos, distância dos ossos relativa ao quadril e variação de comprimento. Maior variação observada: aproximadamente 5%, compatível com o ajuste de retargeting; limite de detecção usado: 15%. Isto não constitui validação visual de cada frame de cada clipe.
- A primeira versão da auditoria usou distância absoluta e sinalizou deslocamento de root motion como coordenada extrema; resultado preservado em validation-result-initial.json. O teste final mede distância relativa ao quadril. As primeiras capturas de modelos foram substituídas por capturas com AlwaysAnimate após detectar culling; os resultados finais estão nos JSONs citados.
- mesh-validation.json: BakeMesh dos 16 Meshy durante soco Mixamo, todos com vértices finitos. Capturas punch-*.png. Capturas before-*.png e after-*.png do idle dos prefabs e kick-*.png do chute. Inspeção visual de exemplos de idle e silhueta de soco; demais imagens disponíveis para revisão.
- Unity MCP compilation/errors: count 0, isCompiling false. Cena RuaUrbana mantida aberta e isDirty false. Não foi executada uma partida completa em Play Mode; a validação se concentrou nos Avatars, deformação e retargeting via Playables.
- Scripts de diagnóstico/reparo e respostas do MCP preservados no diretório de backup. FightingPrototypeSetup.cs não alterado nem executado. Referência de apoio para a auditoria de pose: https://github.com/Unity-Technologies/UnityCsReference/blob/master/Editor/Mono/Inspector/Avatar/AvatarSetupTool.cs.

### Como reverter

Com o Unity fora do Play Mode, copiar de `Backups/RigRepair_20261022_181732` para os mesmos caminhos do projeto os sete .fbx.meta alterados, os três prefabs e Fighter_Idle.anim, preservando seus .meta/GUIDs; solicitar AssetDatabase.Refresh no Unity. O manifest-sha256.json lista os originais. Não restaurar pastas inteiras por exclusão e não apagar os backups.
- Verificação final de diff: removidos apenas espaços ao fim de linha do YAML de Fighter_Idle.anim gerado pelo Unity; reimportação confirmou Humanoid e 130 curvas. git diff --check sem erros após normalização.

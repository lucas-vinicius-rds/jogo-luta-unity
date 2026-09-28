# 📜 Registro de Alterações e Correções - UnDFight

Documento de acompanhamento contínuo das correções de bugs, refatorações pontuais e novas funcionalidades implementadas no jogo **UnDFight** (Unity 6 / C#).

Este registro é atualizado a cada nova tarefa para garantir a rastreabilidade e a integridade da arquitetura do projeto.

---

## 📑 Índice de Ciclos
1. [Ciclo 1 (28/09/2026) - Correções de Input e Limpeza de APIs Obsoletas](#-ciclo-1-28092026---correções-de-input-e-limpeza-de-apis-obsoletas)
2. [Ciclo 2 (28/09/2026) - Correção de Hurtbox ao Agachar, Vínculo de Hitboxes a Ossos e Debug Gizmos](#-ciclo-2-28092026---correção-de-hurtbox-ao-agachar-vínculo-de-hitboxes-a-ossos-e-debug-gizmos)
3. [Ciclo 3 (28/09/2026) - Timing de HitStun no Hitstop e Transição Suave de Virada (Turn180)](#-ciclo-3-28092026---timing-de-hitstun-no-hitstop-e-transição-suave-de-virada-turn180)
4. [Ciclo 4 (28/09/2026) - Offsets de Hitbox no Espaço do Osso](#-ciclo-4-28092026---offsets-de-hitbox-no-espaço-do-osso)
5. [Ciclo 5 (28/09/2026) - Harness de Teste Automático de Combate (AutoFightTester - FASE 1)](#-ciclo-5-28092026---harness-de-teste-automático-de-combate-autofighttester---fase-1)

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

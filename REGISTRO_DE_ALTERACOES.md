# 📜 Registro de Alterações e Correções - UnDFight

Documento de acompanhamento contínuo das correções de bugs, refatorações pontuais e novas funcionalidades implementadas no jogo **UnDFight** (Unity 6 / C#).

Este registro é atualizado a cada nova tarefa para garantir a rastreabilidade e a integridade da arquitetura do projeto.

---

## 📑 Índice de Ciclos
1. [Ciclo 1 (28/09/2026) - Correções de Input e Limpeza de APIs Obsoletas](#-ciclo-1-28092026---correções-de-input-e-limpeza-de-apis-obsoletas)
2. [Ciclo 2 (28/09/2026) - Correção de Hurtbox ao Agachar, Vínculo de Hitboxes a Ossos e Debug Gizmos](#-ciclo-2-28092026---correção-de-hurtbox-ao-agachar-vínculo-de-hitboxes-a-ossos-e-debug-gizmos)
3. [Ciclo 3 (28/09/2026) - Timing de HitStun no Hitstop e Transição Suave de Virada (Turn180)](#-ciclo-3-28092026---timing-de-hitstun-no-hitstop-e-transição-suave-de-virada-turn180)
4. [Ciclo 4 (28/09/2026) - Offsets de Hitbox no Espaço do Osso](#-ciclo-4-28092026---offsets-de-hitbox-no-espaço-do-osso)

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

---

### 🛠 Alterações Realizadas

#### 1. Pausa do Temporizador de HitStun durante Hitstop (Bug A)
- **Problema:** Em `HitStunState.cs`, `elapsedTime += Time.deltaTime` era somado incondicionalmente a cada frame, consumindo a janela de atordoamento enquanto os lutadores estavam congelados no impacto.
- **Solução:**
  - Em [FighterController.cs](file:///D:/Programas%20SSD/Unity/Unity%20Projects/Teste%20antigravity/Assets/Scripts/FighterController.cs), exposta a propriedade booleana:
    ```csharp
    public bool IsInHitstop => hitstopCoroutine != null;
    ```
    Como o hitstop é gerido por `hitstopCoroutine = StartCoroutine(HitstopRoutine(duration))`, essa propriedade reflete exatamente o período de congelamento sem interferir no tempo global (`Time.timeScale`).
  - Em [HitStunState.cs](file:///D:/Programas%20SSD/Unity/Unity%20Projects/Teste%20antigravity/Assets/Scripts/HitStunState.cs), a contagem do tempo decorrido foi condicionada:
    ```csharp
    if (fighter == null || !fighter.IsInHitstop)
    {
        elapsedTime += Time.deltaTime;
    }
    ```
    Desta forma, todo o tempo de hitstop é preservado e a vítima cumpre integralmente a duração de `stunDuration` após o término do congelamento.

#### 2. Detecção de Cruzamento de Lado e Rotação Suave com Turn180 (Bug B)
- **Problema:** `FaceOpponent()` aplicava rotação instantânea (`transform.rotation = Quaternion.Euler(...)`) a cada frame, causando estalos visuais. Além disso, `TriggerTurn180()` não era acionado e o personagem podia girar no meio de um soco ou golpe.
- **Solução:**
  - Em [FighterController.cs](file:///D:/Programas%20SSD/Unity/Unity%20Projects/Teste%20antigravity/Assets/Scripts/FighterController.cs), criada a propriedade auxiliar:
    ```csharp
    public bool IsInAttackOrHitStun => CurrentState is AttackState || CurrentState is HitStunState || CurrentState is KnockoutState;
    ```
  - Em [FighterMovement.cs](file:///D:/Programas%20SSD/Unity/Unity%20Projects/Teste%20antigravity/Assets/Scripts/FighterMovement.cs):
    - Adicionado campo configurável `turnSpeed` (padrão `720°/s`, realizando a rotação de 180° em ~0.25s) e rastreamento de direção `lastFacingDirection`.
    - No método `FaceOpponent()`:
      1. Ignora virada se o lutador estiver no ar (`!characterController.isGrounded`).
      2. Ignora virada se estiver atacando, em hitstun ou nocauteado (`fighterController.IsInAttackOrHitStun`).
      3. Quando o oponente cruza para o outro lado (`desiredDirection != lastFacingDirection`), atualiza `lastFacingDirection` e invoca `fighterController.TriggerTurn180()` **uma única vez** (prevenindo loops contínuos de animação).
      4. Aplica rotação progressiva e fluida via `Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime)`.
    - No `ResetMotion()`, `lastFacingDirection` é sincronizado com a rotação atual.

---

### ⚠️ Nota Técnica sobre o Animator (BaseFighter.controller)

> [!IMPORTANT]
> **Existência do Estado vs Parâmetro Trigger no Animator:**
> - O **Estado** `Turn180` **EXISTE** na camada base do Animator Controller (`BaseFighter.controller`), configurado com a animação de virada e uma transição de saída automática por tempo (`ExitTime: 0.9`) de volta para `Locomotion`.
> - O **Parâmetro Trigger** com o nome `Turn180` **NÃO EXISTE** na lista de parâmetros do controller (apenas `Punch`, `Attack2`, `Hit`, `Jump`, etc.).
> - **Como foi resolvido corretamente:** Em conformidade com a arquitetura do projeto, `FighterController.TriggerTurn180()` utiliza `CrossFadeAnimation(Turn180AnimHash, 0.1f)` (`animator.CrossFadeInFixedTime`), acessando o estado diretamente pelo hash numérico. Dessa forma, a animação de virada é disparada com máxima eficiência sem gerar erros de parâmetro inexistente no Unity.

---

### 📂 Arquivos Afetados no Ciclo 3

| Arquivo | Tipo de Alteração | Descrição |
|---|---|---|
| `Assets/Scripts/FighterController.cs` | Modificado | Adicionadas propriedades públicas `IsInHitstop` e `IsInAttackOrHitStun`. |
| `Assets/Scripts/HitStunState.cs` | Modificado | Pausa do avanço de `elapsedTime` enquanto `fighter.IsInHitstop` for verdadeiro. |
| `Assets/Scripts/FighterMovement.cs` | Modificado | Rotação suave com `turnSpeed`, bloqueio de virada durante ataque/hitstun e acionamento único de `TriggerTurn180()` na troca de lado. |
| `REGISTRO_DE_ALTERACOES.md` | Modificado | Atualização do documento com o Ciclo 3 e documentação do Animator. |

---

### 🧪 Status de Compilação
- **dotnet build:** `0 Erros, 0 Avisos de código`.
- **Unity Editor MCP:** Compilação limpa (`0 erros`).

---

## Ciclo 4: 28/09/2026 - Offsets de Hitbox no Espaço do Osso

### Alterações realizadas
- A posição de projeto das hitboxes, criada no espaço do lutador, agora é convertida uma vez para o espaço local do osso em `Hitbox.BindBone()`. A sincronização usa esse offset convertido, em vez de substituir a posição pela origem do osso.
- O fallback de `HeadHitbox` foi corrigido para um ponto central à frente da cabeça, em vez de reutilizar o offset da mão esquerda.
- Os Gizmos agora mostram, para cada hitbox, o golpe configurado, o membro e o estado `ATIVA`/`inativa`.
- O prefab ativo do Player 2 (`EmeraldStrength`) foi alinhado: `BodyHurtbox.height = 2`, igual ao `CharacterController` e ao setup.

### Como testar no Unity
1. Abra `FightingPrototype`, habilite Gizmos e selecione P1 ou P2.
2. Execute Attack e Attack2 dos dois lados da arena; as esferas devem ficar deslocadas a partir do membro, não presas na origem do osso.
3. Cruze os lutadores para disparar Turn180 e repita Attack2; o deslocamento deve acompanhar a nova orientação.
4. Agache, levante e execute os ataques; a cápsula verde deve coincidir com o `CharacterController` do P2 e retornar à altura normal ao levantar.

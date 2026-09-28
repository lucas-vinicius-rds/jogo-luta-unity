# 📜 Registro de Alterações e Correções - UnDFight

Documento de acompanhamento contínuo das correções de bugs, refatorações pontuais e novas funcionalidades implementadas no jogo **UnDFight** (Unity 6 / C#).

Este registro é atualizado a cada nova tarefa para garantir a rastreabilidade e a integridade da arquitetura do projeto.

---

## 📑 Índice de Ciclos
1. [Ciclo 1 (28/09/2026) - Correções de Input e Limpeza de APIs Obsoletas](#-ciclo-1-28092026---correções-de-input-e-limpeza-de-apis-obsoletas)
2. [Ciclo 2 (28/09/2026) - Correção de Hurtbox ao Agachar, Vínculo de Hitboxes a Ossos e Debug Gizmos](#-ciclo-2-28092026---correção-de-hurtbox-ao-agachar-vínculo-de-hitboxes-a-ossos-e-debug-gizmos)

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

---

### 🛠 Alterações Realizadas

#### 1. Sincronização Dinâmica da Hurtbox ao Agachar (Bug A)
- **Problema:** Em `FighterMovement.cs`, `characterController.height` e `center` encolhiam para `62%` ao agachar, mas o `BodyHurtbox` mantinha sua altura fixa (`1.8f`) e centro inalterado, deixando o personagem vulnerável a golpes altos mesmo agachado.
- **Solução:**
  - Em [Hurtbox.cs](file:///D:/Programas%20SSD/Unity/Unity%20Projects/Teste%20antigravity/Assets/Scripts/Hurtbox.cs), implementado o método `SetBounds(float targetHeight, Vector3 targetCenter)` que recalcula a altura da cápsula e o deslocamento vertical local.
  - Em [FighterMovement.cs](file:///D:/Programas%20SSD/Unity/Unity%20Projects/Teste%20antigravity/Assets/Scripts/FighterMovement.cs), no método `HandleJumpAndCrouch`, a Hurtbox agora é redimensionada em tempo real com **exatamente os mesmos valores** aplicados ao `CharacterController` (`standingHeight * 0.62f` e `standingCenter.y * 0.62f`).
  - No `ResetMotion()`, tanto o `CharacterController` quanto a `Hurtbox` são restaurados para as dimensões em pé.
  - Em [FightingPrototypeSetup.cs](file:///D:/Programas%20SSD/Unity/Unity%20Projects/Teste%20antigravity/Assets/Editor/FightingPrototypeSetup.cs), o método `CreateHurtbox` foi alinhado para criar a cápsula com altura `2.0f` e raio `0.42f`, idêntico ao `CharacterController`.

#### 2. Vínculo de Hitboxes a Ossos Animados com LogWarning (Bug B)
- **Problema:** A flag `followAnimatedBone` vinha desligada por padrão (`false`) em `Hitbox.cs` e não era ativada em `FightingPrototypeSetup.cs`. Além disso, se o osso falhasse na busca do Humanoid, o script falhava em silêncio.
- **Solução:**
  - Em [Hitbox.cs](file:///D:/Programas%20SSD/Unity/Unity%20Projects/Teste%20antigravity/Assets/Scripts/Hitbox.cs), `followAnimatedBone` agora é `true` por padrão.
  - O método `BindBone()` tenta obter o Transform do osso via `Animator.GetBoneTransform(targetBone)`. Caso falhe (ou se o rig não for humanoid), executa uma busca na hierarquia (`FindBoneInHierarchy`) pelo nome padrão do membro (`RightHand`, `LeftFoot`, etc.).
  - Se mesmo assim o osso não for encontrado, emite um log claro:
    `Debug.LogWarning($"[Hitbox] O osso correspondente ao membro '{limbType}' não foi encontrado no modelo do lutador '{owner.gameObject.name}'. A hitbox permanecerá na posição relativa padrão.");`
  - Em [FightingPrototypeSetup.cs](file:///D:/Programas%20SSD/Unity/Unity%20Projects/Teste%20antigravity/Assets/Editor/FightingPrototypeSetup.cs), `CreateHitbox` agora serializa `followAnimatedBone = true` explicitamente.

#### 3. Modo de Debug Visual (Gizmos)
- **Hurtbox Gizmos ([Hurtbox.cs](file:///D:/Programas%20SSD/Unity/Unity%20Projects/Teste%20antigravity/Assets/Scripts/Hurtbox.cs)):**
  - Toggle `showDebugGizmos` inspecionável.
  - Desenha a cápsula completa na Scene View em verde aramado com polos e geratrizes cilíndricas, permitindo visualizar o encolhimento no agachamento.
- **Hitbox Gizmos ([Hitbox.cs](file:///D:/Programas%20SSD/Unity/Unity%20Projects/Teste%20antigravity/Assets/Scripts/Hitbox.cs)):**
  - Toggle `showDebugGizmos` inspecionável.
  - Quando **Inativa**: desenha aramado em laranja suave acompanhando o osso em cada pose/animação.
  - Quando **Ativa**: desenha volume preenchido vermelho translúcido com borda sólida durante os frames de impacto do golpe.

---

### 📂 Arquivos Afetados no Ciclo 2

| Arquivo | Tipo de Alteração | Descrição |
|---|---|---|
| `Assets/Scripts/Hurtbox.cs` | Modificado | Adicionado método `SetBounds()`, suporte a crouch dinâmico e Gizmos com fio de cápsula. |
| `Assets/Scripts/FighterMovement.cs` | Modificado | Cache da `BodyHurtbox`, sincronização com `targetHeight`/`targetCenter` no crouch e restauração no `ResetMotion()`. |
| `Assets/Scripts/Hitbox.cs` | Modificado | `followAnimatedBone = true` por padrão, fallback de hierarquia, `LogWarning` em osso ausente e Gizmos de estado ativo/inativo. |
| `Assets/Editor/FightingPrototypeSetup.cs` | Modificado | Dimensões de `CreateHurtbox` iguais ao `CharacterController` e ativação de `followAnimatedBone = true` no `CreateHitbox`. |

---

### 🧪 Status de Compilação
- **dotnet build:** `0 Erros, 0 Avisos CS0618`.
- **Unity Console:** Compilação limpa (`0 erros`).

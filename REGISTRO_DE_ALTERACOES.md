# 📜 Registro de Alterações e Correções - UnDFight

Documento de acompanhamento contínuo das correções de bugs, refatorações pontuais e novas funcionalidades implementadas no jogo **UnDFight** (Unity 6 / C#).

Este registro será atualizado a cada nova tarefa para garantir a rastreabilidade e a integridade da arquitetura do projeto.

---

## 📑 Índice de Ciclos
1. [Ciclo 1 (28/09/2026) - Correções de Input e Limpeza de APIs Obsoletas](#-ciclo-1-28092026---correções-de-input-e-limpeza-de-apis-obsoletas)

---

## 🥊 Ciclo 1: 28/09/2026 - Correções de Input e Limpeza de APIs Obsoletas

### 🎯 Objetivos do Ciclo
1. Eliminar perda de comandos de ataque pressionados durante o atordoamento (*HitStun*).
2. Separar esquemas de controle de Player 1 e Player 2, acabando com teclas hardcoded e interferência mútua no teclado.
3. Desfazer sobreposição de botões no gamepad (`buttonWest` disparava Soco e Ataque 2 simultaneamente).
4. Substituir APIs de busca de objetos depreciadas no Unity 6 (`CS0618`).
5. Garantir compilação com zero erros no editor de código e no Unity.

---

### 🛠 Alterações Realizadas

#### 1. Input Buffer durante HitStun (Bug A)
- **Problema:** Entradas de ataque feitas durante o estado de `HitStun` eram descartadas porque `ReadAttackCommand()` não rodava enquanto o lutador estivesse atordoado.
- **Solução:**
  - Criado buffer de entrada de curta duração (`hitStunInputBufferDuration = 0.15s`, serializado no Inspector de [FighterController.cs](file:///D:/Programas%20SSD/Unity/Unity%20Projects/Teste%20antigravity/Assets/Scripts/FighterController.cs)).
  - [HitStunState.cs](file:///D:/Programas%20SSD/Unity/Unity%20Projects/Teste%20antigravity/Assets/Scripts/HitStunState.cs) agora escuta e armazena comandos no buffer a cada frame do `Update` via `fighter.BufferAttackInput()`, mantendo o tempo integral do atordoamento sem permitir cancelá-lo.
  - Ao sair do atordoamento e entrar em [NeutralState.cs](file:///D:/Programas%20SSD/Unity/Unity%20Projects/Teste%20antigravity/Assets/Scripts/NeutralState.cs), se houver comando no buffer não expirado, o ataque é consumido e disparado imediatamente no primeiro frame livre.
  - O buffer é automaticamente limpo ao expirar, ao reiniciar o round ou caso o personagem seja nocauteado ([KnockoutState.cs](file:///D:/Programas%20SSD/Unity/Unity%20Projects/Teste%20antigravity/Assets/Scripts/KnockoutState.cs)).

#### 2. Separação de Controles P1 e P2 (Bug B)
- **Problema:** [FighterMovement.cs](file:///D:/Programas%20SSD/Unity/Unity%20Projects/Teste%20antigravity/Assets/Scripts/FighterMovement.cs) lia tanto `WASD` quanto as `Setas` de forma hardcoded, e [LocalPlayerTwoInput.cs](file:///D:/Programas%20SSD/Unity/Unity%20Projects/Teste%20antigravity/Assets/Scripts/LocalPlayerTwoInput.cs) usava `J` e `K` no P2 enquanto o P1 também escutava `J` e `K`.
- **Solução:**
  - Criada a classe serializável [FighterInputConfig.cs](file:///D:/Programas%20SSD/Unity/Unity%20Projects/Teste%20antigravity/Assets/Scripts/FighterInputConfig.cs) que centraliza e desacopla o mapeamento de teclas e controle por jogador.
  - Removidas todas as teclas hardcoded de `FighterMovement.cs`, `FighterController.cs` e `LocalPlayerTwoInput.cs`. Cada lutador agora consulta exclusivamente seu próprio `InputConfig`.
  - [LocalPlayerTwoInput.cs](file:///D:/Programas%20SSD/Unity/Unity%20Projects/Teste%20antigravity/Assets/Scripts/LocalPlayerTwoInput.cs) inicializa o lutador com o preset de Player 2 (`CreatePlayerTwo()`) e ativa `IsPlayerControlled = true`.

#### 3. Eliminação de Sobreposição no Gamepad (Bug C)
- **Problema:** Em `InitializeAttackInput()`, `runtimeAttackAction` registrava tanto `buttonSouth` quanto `buttonWest`, enquanto `runtimeAttack2Action` também registrava `buttonWest`.
- **Solução:**
  - `runtimeAttackAction` (Soco) agora utiliza exclusivamente `buttonSouth`.
  - `runtimeAttack2Action` (Ataque 2) agora utiliza exclusivamente `buttonWest`.

#### 4. Resolução de APIs Obsoletas Unity 6 (CS0618)
- Em [FightingPrototypeSetup.cs](file:///D:/Programas%20SSD/Unity/Unity%20Projects/Teste%20antigravity/Assets/Editor/FightingPrototypeSetup.cs) e [GameFlowController.cs](file:///D:/Programas%20SSD/Unity/Unity%20Projects/Teste%20antigravity/Assets/Scripts/GameFlowController.cs):
  - `FindObjectsByType<T>(FindObjectsSortMode.None)` ➔ `FindObjectsByType<T>()`.
  - `FindFirstObjectByType<T>()` ➔ `FindAnyObjectByType<T>()` onde a ordem não é relevante.
  - `FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None)` ➔ `FindObjectsByType<T>(FindObjectsInactive.Include)`.

---

### 🎮 Tabela Consolidada de Controles

| Ação | Player 1 (Teclado) | Player 1 (Gamepad 1) | Player 2 (Teclado) | Player 2 (Gamepad 2 / Alt) |
|---|---|---|---|---|
| **Mover Esquerda** | `A` | Analógico Esq. / D-Pad | `Seta Esquerda` | Analógico Esq. / D-Pad |
| **Mover Direita** | `D` | Analógico Esq. / D-Pad | `Seta Direita` | Analógico Esq. / D-Pad |
| **Pular** | `W` | Analógico Esq. (Cima) | `Seta Cima` | Analógico Esq. (Cima) |
| **Agachar** | `S` | Analógico Esq. (Baixo) | `Seta Baixo` | Analógico Esq. (Baixo) |
| **Soco (Punch)** | `F` *(ou `Espaço`)* | `buttonSouth` (Xbox: `A` / PS: `✕`) | `Keypad 1` *(ou `J`)* | `buttonSouth` (Xbox: `A` / PS: `✕`) |
| **Ataque 2 (Kick)** | `G` | `buttonWest` (Xbox: `X` / PS: `□`) | `Keypad 2` *(ou `K`)* | `buttonWest` (Xbox: `X` / PS: `□`) |

---

### 📂 Arquivos Afetados no Ciclo 1

| Arquivo | Tipo de Alteração | Descrição |
|---|---|---|
| `Assets/Scripts/FighterInputConfig.cs` | **Novo** | Configuração centralizada de mapeamento por jogador. |
| `Assets/Scripts/FighterMovement.cs` | Modificado | Leitura via `inputConfig`, remoção de teclas hardcoded. |
| `Assets/Scripts/FighterController.cs` | Modificado | Input buffer no HitStun, leitura via `inputConfig`, correção de bindings no gamepad. |
| `Assets/Scripts/LocalPlayerTwoInput.cs` | Modificado | Configuração automática do preset de P2 sem leituras legadas. |
| `Assets/Scripts/HitStunState.cs` | Modificado | Gravação contínua no buffer durante o atordoamento. |
| `Assets/Scripts/NeutralState.cs` | Modificado | Execução do ataque em buffer na transição de saída do HitStun. |
| `Assets/Scripts/KnockoutState.cs` | Modificado | Limpeza do buffer de ataque ao sofrer nocaute. |
| `Assets/Editor/FightingPrototypeSetup.cs` | Modificado | Atualização de métodos de busca para Unity 6 (`FindAnyObjectByType`). |
| `Assets/Scripts/GameFlowController.cs` | Modificado | Atualização de métodos de busca para Unity 6 (`FindAnyObjectByType`). |

---

### 🧪 Verificação e Status de Compilação
- **dotnet build:** `0 Erros, 0 Avisos CS0618`.
- **Unity Console:** Compilação limpa (`0 erros`).

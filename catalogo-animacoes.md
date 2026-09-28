# Catálogo de animações — UnDFight

Arquivo de referência das animações disponíveis em `Assets/Animations/Mixamo/`.
Os nomes abaixo são os nomes atuais dos arquivos; quando você renomear os assets,
atualize também esta tabela.

## Animações ligadas ao controlador atual

| Arquivo | Uso atual / significado |
|---|---|
| `Fighter_Idle.anim` | Idle base do lutador parado. |
| `X Bot@Bouncing Fight Idle.fbx` | Idle de luta alternativo, com balanço constante. |
| `X Bot@Walking.fbx` | Caminhada/locomoção para frente. |
| `X Bot@Running Backward.fbx` | Recuo / locomoção para trás. |
| `X Bot@Standing To Crouched.fbx` | Transição de em pé para agachado (ao pressionar S). |
| `X Bot@Crouched To Standing.fbx` | Transição de agachado para em pé (ao soltar S). |
| `X Bot@Crouch Walk Forward.fbx` | Deslocamento agachado para frente. |
| `X Bot@Crouch Walk Back.fbx` | Deslocamento agachado para trás. |
| `X Bot@Crouching Idle.fbx` | Agachamento parado / postura agachada contínua (ao segurar S). |
| `X Bot@Jumping1.fbx` | Pulo parado (`jumping1`), sem deslocamento horizontal. |
| `X Bot@Jump.fbx` | Pulo para trás. |
| `Pulo pra frente.fbx` | Pulo para frente. |
| `X Bot@Punching.fbx` | Soco principal atual (`Punch` / ataque primário). |
| `X Bot@Kicking.fbx` | Chute principal (`Attack2` / ataque secundário). |
| `X Bot@Hit To Body.fbx` | Reação ao receber golpe no corpo (`HitStun`). |

## Ataques e golpes disponíveis

| Arquivo | Possível uso |
|---|---|
| `X Bot@Cross Punch.fbx` | Soco cruzado; alternativa para ataque primário ou golpe forte. |
| `X Bot@Surprise Uppercut.fbx` | Uppercut surpresa; golpe vertical de curto alcance. |
| `X Bot@Surprise Uppercut (1).fbx` | Outra variação do uppercut surpresa; comparar no Unity antes de escolher. |
| `X Bot@Headbutt.fbx` | Cabeçada; ataque de curta distância. |
| `X Bot@Flying Kick.fbx` | Chute voador; ataque com avanço aéreo. |
| `X Bot@Kicking (1).fbx` | Variação do chute; comparar duração e avanço com `Kicking.fbx`. |
| `X Bot@Martelo 2.fbx` | Golpe de martelo; ataque descendente ou pesado. |
| `X Bot@Macaco Side.fbx` | Golpe lateral/acrobático; validar orientação e root motion. |

## Reações, estados e utilitários

| Arquivo | Possível uso |
|---|---|
| `X Bot@Dying.fbx` | Queda/morte após nocaute. |
| `X Bot@Getting Up.fbx` | Levantar após queda ou recuperação. |
| `X Bot@Reaction.fbx` | Reação genérica; pode servir para impacto leve ou defesa. |
| `X Bot@Walking Turn 180.fbx` | Virada de 180 graus durante caminhada. |
| `X Bot@Kneeling Down.fbx` | Postura de agachamento alternativa (anterior). |
| `X Bot@Jumping.fbx` | Pulo original parado alternativo (anterior). |
| `X Bot@Step Backward.fbx` | Passo para trás / recuo curto (disponível para troca). |
| `X Bot@Walk Backwards.fbx` | Caminhada alternativa para trás (disponível para troca). |
| `X Bot@Silly Dancing.fbx` | Animação de vitória, menu ou provocação. |

> Observação: o arquivo real se chama `X Bot@Getting Up.fbx`.

## Parâmetros usados no Animator

- `Punch`: dispara o soco principal.
- `Attack2`: dispara o ataque secundário/chute.
- `Hit`: entra na reação de dano.
- `Crouch`: mantém o lutador agachado.
- `CrouchDirection`: diferencia agachamento/deslocamento para frente e para trás.
- `Jump`: dispara um pulo.
- `JumpType`: `0` pulo parado, `1` pulo para trás, `2` pulo para frente.
- `Speed`: controla a locomoção/estado de caminhada.

## Notas para escolher animações futuras

- Para golpes com deslocamento, verificar se o clipe possui root motion e se o
  avanço deve ser persistido pelo código.
- Para ataques, comparar duração total, início do contato, fim do contato e
  retorno à pose neutra antes de substituir um golpe atual.
- Para pulos, comparar a duração visual com a gravidade e o tempo real no ar.
- Para variações de agachamento, confirmar se o clipe é pose parada ou caminhada.

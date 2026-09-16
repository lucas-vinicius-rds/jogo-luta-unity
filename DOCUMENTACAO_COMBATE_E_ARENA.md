# Combate, animação e arena

## Base comum de animação

Todos os personagens humanoides usam `Resources/Animations/FighterFoundation.controller` em runtime. Os estados são comuns, sem substituições semânticas:

- `Idle`: `X Bot@Bouncing Fight Idle` — postura neutra durante a luta.
- `Jump`: `X Bot@Jump`.
- `Attack`: `X Bot@Martelo 2` (soco).
- `Attack2`: `X Bot@Kicking` (chute).

O movimento físico é responsabilidade de `FighterMovement`; root motion dos golpes passa por `ApplyAttackRootMotion`, que respeita limites da arena e a separação entre lutadores. Não adicione uma animação de outro significado como fallback: estado sem clip deve permanecer no `Idle`.

## Janela de dano

`FighterController` centraliza a janela para todos os personagens. O relógio é o tempo normalizado do estado do Animator, não um cronômetro independente.

| Ação | Preparação e avanço | Janela ativa | Recuperação |
| --- | --- | --- | --- |
| Martelo 2 | 0.00–0.58 | 0.58–0.64 | 0.64–0.98 |
| Kicking | 0.00–0.54 | 0.54–0.61 | 0.61–0.98 |

A hitbox abre no pico de extensão e fecha antes do retorno. `AttackState` só volta ao neutro após `recoveryEndNormalized`; assim o golpe não pode ser visualmente cancelado logo depois do impacto. Caso um clip seja trocado, revisar estes três valores no prefab/controlador antes de alterar dano, alcance ou velocidade.

## Movimento e troca de lado

`FighterMovement` conserva o espaçamento mínimo no solo. No salto, esse bloqueio é removido apenas quando o personagem está acima da altura configurada em `jumpOverClearance`, permitindo ultrapassar o rival sem atravessá-lo ao nível do chão.

`FaceOpponentNow` é executado continuamente e antes de iniciar cada ataque. Depois de cruzar o eixo X do oponente, ambos viram para continuar se olhando; ataques, knockback e avanço usam `transform.forward`, portanto acompanham a nova direção.

## Seleção de personagem

O preview lateral e os cartões criam instâncias isoladas, sem colliders e sem scripts de combate. Os cartões da grade têm o Animator desligado e mostram exclusivamente a A-pose, sem animação automática. O preview lateral mantém o Animator no `Idle`, pois é a única visualização da seleção que pode permanecer animada; sua câmera enquadra o corpo sem espaço vazio abaixo dos pés.

## Arena e menu

- `ArenaEnvironmentBuilder` monta a arena em runtime e busca `Resources/Arena/DistanceBuilding.prefab` e `Resources/Arena/FeaturedTree.prefab` para os assets importados. Eles ficam ao fundo e não possuem colliders.
- A imagem `Assets/telaInicial.png` é o fundo exclusivo do menu. A arena não é usada como substituta do menu.
- `TekkenCamera` usa projeção ortográfica e enquadramento mais fechado, calculado a partir da distância horizontal entre lutadores.

## Checklist de regressão

1. Em luta, ambos começam em `Idle`; pressionar W usa `Jump`, Espaço usa `Martelo 2` e Ctrl esquerdo usa `Kicking`.
2. Dano só ocorre no pico visual de cada ataque e não durante a recuperação.
3. Pular sobre o rival inverte a orientação de ambos; um ataque em seguida sai para o novo lado.
4. Menu usa `telaInicial.png`; seleção mantém retratos alinhados e preview 3D centralizado.
5. Construção e árvore importadas aparecem ao fundo e não bloqueiam a luta.

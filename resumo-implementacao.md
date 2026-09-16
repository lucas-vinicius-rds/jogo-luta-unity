# Resumo da implementação — UnDFight

## Visão geral

O projeto foi evoluído de um protótipo de luta para um fluxo jogável de:

```text
Menu Principal → Versus → Modo de jogo → Seleção de personagens → VS → Luta → K.O.
```

O trabalho foi realizado no projeto Unity localizado em `jogo-luta-unity/`.

## Menu principal

- Nome do jogo: `UnDFight`.
- Opções principais:
  - `VERSUS`;
  - `SAIR`.
- Botões centralizados.
- Fundo usando `Assets/telaInicial.png`.
- Música usando `Assets/eltema.mp3`.
- Identidade visual baseada em azul escuro e branco.

## Menu Versus

- `PLAYER VS CPU`;
- `PLAYER VS PLAYER LOCAL`;
- `VOLTAR`.

## Seleção de personagens

Os sete personagens foram configurados com estes nomes:

| Nome original | Nome exibido |
|---|---|
| Casual Smilling Man | Tiago |
| Emerald Strength | Isaac |
| Man in Black | Lucas |
| Modern Gentleman | Enomoto |
| Shadow Sentinel | Jompi |
| Studio Rocker | Gabutas |
| Vasco Jacket Avatar | Ricardinho |

Recursos implementados:

- seleção de P1 e P2/CPU;
- confirmação separada para cada jogador;
- bloqueio de personagem duplicado;
- hover adaptativo pelo mouse;
- navegação pelas setas;
- confirmação por Enter ou clique;
- cancelamento por Esc;
- contorno ciano no personagem em foco;
- indicação verde-clara para personagem confirmado;
- P1 visualizado à esquerda;
- P2/CPU visualizado à direita;
- grade reorganizada para cards maiores;
- layout expandido para aproveitar melhor a tela.

## Previews dos personagens

Foram separados dois sistemas de preview:

### Preview lateral

- Usa modelos 3D renderizados em `RenderTexture`.
- Troca conforme o personagem passa a ser destacado.
- P1 aparece à esquerda.
- P2/CPU aparece à direita.
- Usa câmeras e camadas próprias para impedir que os modelos fiquem sobrepostos.
- Mantém o Animator ativo.
- Recebe zoom óptico ampliado.

### Retratos dos cards

- Cada personagem possui uma câmera e uma textura próprias.
- Os modelos são estáticos.
- O Animator é desativado.
- O enquadramento é direcionado para a cabeça/rosto.
- O nome fica abaixo da imagem.
- Os cards possuem dimensões maiores e contorno de seleção.

## Entrada na luta

- A luta começa com exatamente os personagens escolhidos.
- P1 é instanciado no lado esquerdo.
- P2/CPU é instanciado no lado direito.
- No modo CPU, o segundo personagem recebe `FighterSparringAI`.
- No modo local, o segundo personagem recebe `LocalPlayerTwoInput`.
- Os personagens são configurados como oponentes entre si.
- A câmera da arena recebe os dois lutadores instanciados.
- O fluxo VS acontece antes do controle da luta.

## Área de luta e combate

- Movimento reorganizado para estilo 2.5D:
  - frente e trás no eixo X;
  - pulo;
  - agachamento;
  - limite de profundidade no eixo Z.
- Personagens posicionados de lado para a câmera.
- Direção dos lutadores ajustada para se enfrentarem.
- Punch bugado substituído por `X Bot@Punching.fbx`.
- Root motion do ataque tratado pelo `FighterController`.
- Avanço do chute preserva a posição final da animação.
- Duração dos ataques passa a considerar a duração real do estado do Animator.
- Animações de agachamento adicionadas:
  - frente;
  - trás.
- Animações de pulo adicionadas:
  - `Jumping`: pulo parado;
  - `Jump`: pulo para trás;
  - `Pulo pra frente`: pulo para frente.

## Tela de K.O. e vitória

A tela de vitória é acionada pelo evento real de nocaute e aguarda a animação de queda antes de aparecer.

Ela exibe:

- `K.O.`;
- nome do vencedor;
- nome do derrotado;
- preview visual do vencedor;
- preview visual do derrotado;
- `JOGAR NOVAMENTE`;
- `ESCOLHER PERSONAGENS`;
- `VOLTAR AO MENU`.

As opções funcionam por mouse, setas e Enter.

### Animações de vitória

Os arquivos específicos por personagem estão em `Assets/Animations/Mixamo/`:

- `tigas.fbx`;
- `isaac.fbx`;
- `lusca.fbx`;
- `enomoto.fbx`;
- `jompis.fbx`;
- `gabutas.fbx`;
- `ricardin.fbx`.

O vencedor usa o arquivo correspondente ao personagem selecionado.

O derrotado usa:

```text
X Bot@Surprise Uppercut (1).fbx
```

O caminho dos arquivos foi corrigido para incluir a pasta `Mixamo`.

## Catálogo de animações

Foi criado um catálogo separado com a descrição das animações disponíveis:

- `catalogo-animacoes.md`.

Ele lista as animações de idle, locomoção, agachamento, pulo, ataques, reações e nocaute.

## Arquivos principais alterados

- `Assets/Scripts/GameFlowController.cs`;
- `Assets/Scripts/FighterMovement.cs`;
- `Assets/Scripts/FighterController.cs`;
- `Assets/Scripts/FighterSparringAI.cs`;
- `Assets/Scripts/AttackState.cs`;
- `Assets/Scripts/KnockoutState.cs`;
- `Assets/Scripts/TekkenCamera.cs`;
- `Assets/Scripts/FighterAppearance.cs`;
- `Assets/Editor/FightingPrototypeSetup.cs`;
- `Assets/Animator/Characters/BaseFighter.controller`;
- `ProjectSettings/EditorBuildSettings.asset`.

## Validações realizadas

- A compilação do Unity foi verificada várias vezes.
- O estado final verificado apresentou zero erros de compilação.
- Foram capturadas imagens do Game View para conferir a seleção, os previews e a tela de K.O.
- O problema de referências inválidas de prefab foi corrigido usando instanciação não-genérica segura.

## Observações

- Depois de alterações nos previews ou animações, é recomendável sair e entrar novamente no Play Mode para limpar objetos e RenderTextures da execução anterior.
- Os arquivos de animação por personagem estão em `Assets/Animations/Mixamo/`; esse caminho deve ser mantido ao adicionar novas animações.

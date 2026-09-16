# Guia de desenvolvimento — UnDFight

## Objetivo do projeto

UnDFight é um jogo de luta 2.5D. Cada mudança deve preservar três princípios: leitura clara do golpe, deslocamento físico coerente e interface legível em qualquer resolução suportada.

## Cenário demonstrativo

`ArenaEnvironmentBuilder` gera a arena `RainforestDockArena` em runtime com primitivas da Unity e materiais URP:

- palafita modular de madeira, com collider no piso;
- rio ao fundo, céu, nuvens e luz quente;
- cabanas, árvore central, palmeiras, cipós, folhagem e cordas;
- arena anterior desativada apenas em runtime. Os objetos de cena originais não são apagados.

Para trocar de mapa, crie outro builder ou prefab de ambiente. O chão jogável precisa manter o plano central em `z = 0`, espaço útil aproximado de `x -9` a `x 9` e collider contínuo.

## Animação autoral

Os sete prefabs mantêm Avatar humanoide, mas seus `Animator Controllers` legados foram removidos. Os FBX continuam no repositório como backup e não devem ser apagados.

`FighterFoundationControllerBuilder` gera `Assets/Resources/Animations/FighterFoundation.controller`. Ele contém apenas `Bouncing Fight Idle`, usado exclusivamente como a base de guarda: é a referência autorizada para a postura inicial, com pernas apoiadas e leitura de luta.

`ProceduralFighterAnimation` é adicionado quando a luta é criada e:

1. lê os ossos Humanoid do Avatar;
2. registra a pose de bind;
3. remove root motion e mantém somente o controller-base de idle;
4. sobrepõe poses próprias em `LateUpdate` para caminhada, agachamento, salto, soco, chute, dano e KO.

O golpe secundário (`Attack2`) é o chute autoral. Sua sequência é: preparação (0–24%), extensão e impacto (24–51%) e recuperação (51–82%). A janela de hitbox continua controlada por `FighterAttackTiming` no prefab; ao criar um novo golpe, ajuste a janela para coincidir com a extensão visual do membro.

Não reutilize clips ou poses para ações de significado diferente. Se uma animação ainda não existe, mantenha a pose neutra e documente a pendência.

## Física e sincronização

- `FighterMovement` é a única fonte de deslocamento horizontal, gravidade, salto e limites da arena.
- A animação procedural nunca desloca `transform.position`; ela só altera rotação dos ossos. Isso evita deslizamento e desincronização de root motion.
- `AttackState` bloqueia deslocamento durante ataque. O ponto final físico depende exclusivamente da física e do impulso configurado.
- Antes de mudar velocidade, gravidade ou duração, teste os sete personagens em ambos os lados da arena.

## Interface

Menus principais usam `ModernMenuPresenter` com UI Toolkit:

- `Assets/Resources/UI/ModernMenu.uxml`: raiz estrutural;
- `Assets/Resources/UI/ModernMenu.uss`: tipografia, espaçamento, cores e estados de botão;
- `ModernMenuPresenter.cs`: ligação entre botões e `GameFlowController`.

Evite texto fixo maior do que a largura do botão. Prefira containers flexíveis, `white-space: normal`, margens consistentes e fontes calculadas por resolução quando usar HUD IMGUI.

## Checklist para novas contribuições

1. Não remover assets existentes sem autorização explícita; desative ou substitua referências primeiro.
2. Registrar nova animação, hitbox e duração nesta documentação.
3. Verificar Game View em 1280×720, 1600×900 e 1920×1080.
4. Conferir Console sem erros antes de entregar.
5. Para conteúdo externo futuro, registrar origem e licença no repositório.

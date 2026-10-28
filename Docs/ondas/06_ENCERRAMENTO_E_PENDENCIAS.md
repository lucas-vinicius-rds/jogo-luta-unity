# Encerramento solicitado — 27/10/2026

Implementações interrompidas por solicitação do usuário. O Unity foi retirado do Play Mode; nenhuma exclusão de assets ou substituição adicional foi feita. Este arquivo é o ponto de retomada. Os relatórios usam UTC, portanto algumas execuções aparecem como 28/10.

## Entregas por responsável

Todos os agentes trabalharam no canvas Maestri. Configurações de lançamento conferidas pelo integrador: Cadencia e Prisma `gpt-6.1-sol / low`; Solario e Brasa `gpt-6-luna / medium`.

| Responsável | Entregas |
|---|---|
| Cadencia | `AuthoredFighterMotionBuilder.cs`, `AuthoredFighterGrounding.cs`, alterações em `FighterMovement.cs`, `FighterController.cs` e `AttackState.cs`. Gerador reversível de controller/clips Humanoid, seed completo de Fighter_Idle, calibração de RootT por SampleAnimation, apoio dos pés, seleção medida de poses ofensivas, recuperação e janelas de contato. Briefing: `01_ANIMACAO.md`. |
| Prisma | Home/modos em UI Toolkit; `ModernMenuPresenter.cs`, `ModernMenu.uss`, `SelectionPresenter.cs`, `Selection.uxml`, `Selection.uss`. Logo e fundos Resources, teclado/gamepad sem confirmação duplicada, foco preservado nas opções, dificuldade localizada e botões de volume. Briefing: `02_UI.md`. |
| Solario | `TekkenCamera.cs`: altura/foco 1.5x, tamanho ortográfico 0.6x com limites de enquadramento. `StageGraphicsUpgrade.cs`: audita/configura sombras da direcional existente sem criar outro sol ou mudar a paleta. Briefing: `03_CAMERA_GRAFICOS.md`. |
| Brasa | Quatro fundos imagegen 1672×941 e cópia idêntica da logo em `Assets/Resources/UI/Brand`. Pesquisa institucional distingue UnDF pública de UDF privada e documenta arquitetura fictícia, fontes e prompts em `04_IDENTIDADE_IMAGENS.md`. Investigação somente leitura confirmou que o grupo Spectators é público legítimo da cena antiga. |
| Codex / integrador | Compilação e execução real no Editor, correção de integração, `CombatPresentationRound.cs` e `CombatPresentationVerification.cs`, materiais com normal map discreto e SMAA, transição física de crouch, hitboxes ancoradas no membro, override de vitória reconhecendo Guard, correções de entrada P1/P2 e passagem aérea. Documentação e evidências em `05_INTEGRACAO_VALIDACAO.md` e `Docs/validation`. |

## Comportamento implementado

- Guarda neutra parte dos músculos do idle confiável; não usa a pose importada de corrida.
- Agachar fica imóvel, com collider/hurtbox acompanhando a transição de .28 s para baixo e .32 s para cima. Apoio IK usa ossos reais e orientação da sola, não eixos arbitrários do osso.
- Soco autoral Cross: .92 s, contato 39–43%; chute FrontKick: 1.18 s, contato 46–50%; recuperação completa. Root motion/lunge artificial removido desses ataques.
- Jump conserva altura 2.2 e airControl .9, com tempo do clip vinculado à trajetória e saída condicionada ao chão real. Caminhada frente/trás reaproveita os clips correspondentes a 1x, com aceleração/desaceleração; não é caminhada inteiramente criada do zero.
- Durante passagem suficientemente alta, apenas a colisão entre cápsulas dos lutadores é suspensa. Hurtboxes continuam habilitadas. Espaçamento terrestre não empurra o adversário embaixo; orientação acompanha a troca de lado sem tocar animação de giro terrestre em pleno salto.
- P2 com configuração própria não herda WASD do fallback de P1.
- UI preserva a lógica de fluxo, previews reais e interface híbrida existente. Não houve migração integral do HUD/IMGUI.
- Gráficos preservam o URP ativo e recursos já existentes; foram acrescentados ajustes discretos de superfície/antialiasing. Não foi criado novo pipeline nem feito trabalho de shaders fotorrealistas.

## Validação efetivamente concluída

1. Compilação C# runtime/editor passou por `Tools/Validate-CSharp.ps1`; o Editor também compilou e executou os scripts.
2. `Docs/validation/presentation/results.json`: **passed=true, 308 verificações**, execução iniciada em `2026-10-28T01:58:24Z`. Sete personagens; caminhar frente/trás; agachar imóvel e apoio vertical; salto vertical/frente/trás e pouso; passagem sobre oponente e orientação; soco na nova direção; 14 medições de extensão; **14 contatos reais com dano exatamente uma vez**, janelas e recuperação; Home/modos em 720p, 1080p e ultrawide; teclado/gamepad.
3. `Docs/validation/stage-results.json`: **passed=true, 153 verificações**, execução iniciada em `2026-10-28T01:56:32Z`. Seleção, cinco arenas, carregamento/limpeza, input dos dois jogadores, HUD, pausa, vitória e revanche.
4. Capturas reais foram inspecionadas: Home/modos, guarda, agachamento e picos de soco/chute, incluindo rigs diferentes. Os testes iniciais reprovados motivaram correções; não confundir seus logs antigos com o resultado vigente.
5. Uma repetição diagnóstica das arenas foi interrompida no encerramento solicitado. Arquivos `camera-*.txt` dessa repetição são **parciais**, não outro relatório completo aprovado.

## Pendências reais, em ordem de retomada

1. **Investigar capturas das arenas sem os lutadores visíveis.** A imagem `fight-neon-district.png` mostra cenário/placas sem os atores, apesar da regressão lógica passar. O dump diagnóstico registra câmera `(0,2.48,-8)`, tamanho 2.58 e P1 com centro projetado dentro da tela. Ainda não foi isolado se a causa é captura/renderização, culling, oclusão ou comportamento durante a luta. Não declarar aprovação visual completa das arenas. Adicionar assert de visibilidade real dos meshes/captura após descobrir a causa.
2. **Dedos/punhos:** os Avatars Meshy atuais não têm dedos mapeados. Curvas dos dedos estão preparadas, mas as mãos permanecem abertas nesses rigs. Exige revisar/completar rig e skinning; não foi feito.
3. **Polimento de animação:** não há foot locking por fase na caminhada, antecipação dedicada antes do salto ou clip específico de amortecimento no pouso. Ainda pode haver deslizamento e falta de transferência de peso. Animação procedural não equivale a motion capture nem recebeu aprovação artística do usuário.
4. **Qualidade visual contínua:** capturas estáticas e asserts não substituem revisão do movimento em vídeo, hitstop/hitstun e transições sob combinações prolongadas de inputs. Não foi concluído profiling gráfico de GPU/FPS nem teste em build/dispositivos físicos.
5. **Harness do Editor:** há assert do pacote Input System durante saída de Play Mode/restauração das configurações de teste. Os relatórios concluídos não registraram erros durante o jogo; investigar/limpar teardown antes de novas séries automatizadas. Também houve erros externos Unity Connect 403; não foram tratados como falha do combate.
6. **UI restante:** HUD, VS e vitória ainda mantêm partes da arquitetura antiga; a migração integral continua fora desta rodada. Os fundos são 1672×941, abaixo do alvo ideal 1920×1080.

## Retomada segura

Ler este arquivo e as ondas 1–5 antes de alterar código. Conferir o estado/salvamento do Editor. Não executar FightingPrototypeSetup depois do gerador, pois sobrescreve controllers/prefabs. Para regenerar: `AuthoredFighterMotionBuilder.Generate` → `DiagnoseSupport` → testes. O Generate já mede candidatos antes de salvar. Não apagar animações originais nem assets importados.

Quatro arquivos locais gerados pelo Unity na raiz de Assets (`DefaultVolumeProfile.asset` e `UniversalRenderPipelineGlobalSettings.asset`, com seus `.meta`) continuam preservados fora do commit: duplicam configurações e não são as referências ativas, que estão em `Assets/Settings`. A logo original fornecida na raiz foi incluída como fonte de referência, além da cópia Resources utilizada pelo jogo.

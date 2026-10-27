# Relatório da reconstrução e das arenas

## Entrega

Base `bugfix/test` incorporada, menu/HUD anteriores recuperados do stash e implementações novas reconstruídas. A seleção de personagens usa UI Toolkit com identidade grafite/vermelho/amarelo, fonte condensada e os previews já existentes. A seleção de arenas usa o catálogo central e o fluxo personagens → arena → VS → carregamento → luta. Foram criados cinco cenários procedurais e uma exportação de teste da RuaUrbana.

O código e os assets têm checkpoints locais no Git. Não foi feito push. O stash de segurança anterior continua preservado. Não foram usados MCPs nesta reconstrução; compilação, geração, renders e Play Mode foram executados pelo CLI do Unity.

## Problemas encontrados e tratados

1. A ausência de 519 arquivos versionados foi resolvida restaurando os caminhos ausentes do Git; arquivos novos sem commit precisaram ser reconstruídos. A causa da exclusão não foi comprovada.
2. O HUD recuperado referenciava uma propriedade de guarda inexistente na branch incorporada. A apresentação foi adaptada e a leitura de saúde separada em FightHudState, com eventos e descarte de inscrições.
3. A leitura IMGUI de seleção competiria com o protótipo. O caminho antigo fica como fallback e deixa de ler controles quando a nova view está ativa.
4. O teste de teclado precisava aguardar frames reais do PlayerLoop, não apenas ticks de EditorApplication.update. A espera de VS/vitória também foi alinhada ao relógio usado por WaitForSecondsRealtime.
5. Trocas repetidas expuseram lutadores retornando perto da origem apesar de input zerado. SetupFight agora reaplica o spawn pela rotina segura usada na revanche: desativa CharacterController durante reposicionamento, reinicia movimento/estado, reativa e sincroniza transforms físicos. Não foram alterados valores de velocidade, gravidade ou golpes.
6. AnimatorOverrideControllers criados para a vitória não tinham descarte explícito. Agora pertencem ao fluxo e são liberados junto da apresentação de vitória.
7. Normais opostas do telhado causavam faces negras; a malha foi corrigida. Letreiros, céu, preenchimento frontal e materiais foram revisados nos renders reais.
8. Foram removidos scrollbars padrão da nova seleção e o azul dos fundos dos previews. A grade mostra os sete retratos. O fundo original do menu principal permanece.

## Testes executados no Editor

Unity **6000.5.9f1**, URP **17.5.0**, Windows/D3D11, modo batch com renderização. Não foi usado `-nographics` para avaliar imagens.

O relatório [stage-results.json](validation/stage-results.json) contém a execução completa e os resultados individuais. A execução de estresse está em [stage-stress-results.json](validation/stage-stress-results.json).

Cobertura do fluxo completo:

- Catálogo, referências, contratos de piso/spawns e previews reais.
- Fundo original do menu e reutilização de RenderTextures dos personagens.
- Navegação e confirmação com teclado e gamepad virtual do Input System, seleção aleatória e desconexão.
- Voltar da arena sem perder as escolhas de personagens.
- Seleção e layout em **1920×1080, 1280×720, 2560×1440 e 2560×1080**.
- Arena selecionada igual à instanciada, rejeição de ID desconhecido e confirmação duplicada.
- Spawns corretos, personagens apoiados no piso, movimento de P1/P2 e entrada/recuperação de ataque na FSM.
- Pausa, eventos do HUD, saúde, revanche, KO e vitória nas cinco arenas.
- Uma instância de cenário, um AudioListener e uma fonte de ambiência por arena.
- Descarregamento, contagem estável de RenderTextures e liberação dos overrides de vitória.
- Restauração de RenderSettings e compatibilidade com a arena antiga sem StageDefinition selecionado.
- **18 trocas consecutivas**, percorrendo os seis cenários do catálogo e os sete personagens, verificando spawns, piso e ausência de duplicatas.

Não houve teste físico com dispositivos Xbox/PlayStation, nem teste de executável standalone ou campanha completa de balanceamento/hitboxes. O suporte de navegação foi exercitado por eventos reais do Input System em dispositivos virtuais. A câmera de combate e os números essenciais da mecânica foram preservados.

## Evidência visual

- [Seleção de personagens](validation/characters-1920x1080.png).
- [Seleção de arenas](validation/arenas-1920x1080.png).
- [Seleção ultrawide](validation/arenas-2560x1080.png).
- Previews reais de cada arena ficam em `Assets/Stages/*-preview.png`.
- Capturas de luta ficam em `Docs/validation/fight-*.png`. São renders da câmera 3D; não incluem a camada OnGUI do HUD.

## Desempenho e interpretação das medições

A tabela numérica desta execução fica em [MEDICOES_ARENAS.md](MEDICOES_ARENAS.md), extraída do JSON de teste.

- Renderers, materiais, triângulos e limites de partículas são contagens reais das instâncias geradas.
- Tempo de carga registrado mede **Instantiate + integração à cena + aplicação da ambientação**, com assets referenciados pelo catálogo já carregados. Não é benchmark de leitura fria do disco, streaming, compilação de shader ou duração completa da transição VS.
- CPU e GC são amostras de 30 frames reais do Editor/Play Mode, incluindo jogo, HUD, harness e limitação de frame. Não representam custo isolado de uma arena.
- Memória de texturas é global ao processo e inclui personagens, UI e recursos do catálogo.
- Os contadores de draw calls e triângulos renderizados não forneceram valores utilizáveis neste modo batch; `-1` significa indisponível. Não converter contagem de renderers em draw calls.
- GPU não foi medida. Não foi afirmada garantia de FPS, ausência de stutter ou desempenho em hardware alvo. A primeira instanciação pode exceder um orçamento de 16,7 ms; por isso há uma tela de carregamento antes da luta.

## Qualidade artística e pendências

A classificação individual e a lista detalhada de assets estão em [ETAPAS_2_A_8.md](ETAPAS_2_A_8.md).

Neon District, Underground e Ironworks são bases visualmente desenvolvidas, ainda procedurais. Skyline Rooftop e Ancient Temple permanecem blockouts funcionais com detalhamento. **Nenhuma arena está pronta para produção.** Não há equivalência de acabamento com os jogos comerciais citados como inspiração.

Prioridades seguintes: materiais PBR finais e decals; fachadas/skyline menos repetitivos; vegetação e relevo dedicados; equipamentos e áudio profissionais; revisão artística do enquadramento legado, que ainda pode mostrar uma faixa inferior abaixo do plano de combate. Esse enquadramento não foi alterado para cumprir a preservação da câmera nesta etapa.

As telas antigas continuam disponíveis. A próxima migração visual deve avaliar VS, vitória e HUD de forma independente, usando a separação de dados já introduzida, sem reconstruir o combate.

## Arquivos e manutenção

Inventário completo: [ARQUIVOS_DA_ENTREGA.md](ARQUIVOS_DA_ENTREGA.md).

Arquitetura, investigação e guia para adicionar arenas: [RECONSTRUCAO_E_ARQUITETURA.md](RECONSTRUCAO_E_ARQUITETURA.md). Operação das ferramentas e direções de arte: [ETAPAS_2_A_8.md](ETAPAS_2_A_8.md).

Não remover assets antigos ou executar rebuild dos assets gerados sobre alterações manuais sem um checkpoint. Não criar junctions para os arquivos originais em projetos temporários de validação.

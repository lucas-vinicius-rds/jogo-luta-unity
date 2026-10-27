# Reconstrução, UI e arenas

## Estado e recuperação

A base `origin/bugfix/test` foi incorporada no commit `9a5829f`. Em 26/10/2026 foram constatados 519 arquivos versionados ausentes e a ausência de implementações ainda não commitadas. Os 519 arquivos foram restaurados com `git restore --worktree`, limitado aos caminhos deletados. Não foi identificada a causa da exclusão. Não atribuir a exclusão ao usuário nem declarar recuperação de código que precisou ser reconstruído.

O stash `backup-local-before-bugfix-ui-integration-20261026` continua preservado. Dele foram recuperados ModernMenuPresenter, FightHud e recursos do menu. A reconstrução tem checkpoints locais no Git. Nenhuma implementação foi enviada ao remoto.

Validação usa **cópia física** em `%LOCALAPPDATA%/UnDFightValidation/Project`, feita com robocopy `/E /XJ`, sem `/MIR`, sem junctions e sem remover pastas. Nunca excluir recursivamente projetos de validação com vínculos para arquivos originais.

## Arquitetura encontrada

- Unity 6000.5.9f1, URP 17.5.0; Input System 1.20.0, uGUI 2.5.0, Timeline 1.8.12, Test Framework 1.7.0. `activeInputHandler` suporta entradas antiga e nova.
- Cenas existentes: FightingPrototype, RuaUrbana e SampleScene. GameFlowController é persistente e dividido em lógica, UI IMGUI e previews.
- Movimento em X com CharacterController, gravidade própria, profundidade Z limitada. Limites reais são lidos do componente FighterMovement do prefab, sem modificar velocidade, dano, root motion ou alcance.
- TekkenCamera configura câmera ortográfica, acompanha o ponto médio e ajusta enquadramento com a distância. A arquitetura de arenas preserva seu comportamento.
- HealthSystem publica OnHealthChanged, OnDamageTaken e OnKnockout. FightHudState gerencia inscrições e fornece dados independentes da apresentação IMGUI.
- FSM de combate, hitboxes/hurtboxes, configuração de input por jogador e AI continuam nos componentes existentes.
- Não há um sistema completo de rounds com temporizador implementado nessa base. Esta entrega não inventa um temporizador nem reescreve regras.
- Assets de cenário disponíveis: arena RuaUrbana com materiais/arquitetura procedural. Os FBXs existentes são predominantemente personagens e animações. As novas arenas usam módulos autorais; não baixam cenários nem usam imagens como substituto de geometria.

## Separação de UI

ModernMenuPresenter mantém o menu UI Toolkit existente (UXML, USS, C#). SelectionPresenter é uma view de personagens e arenas com estrutura e estilos separados em Selection.uxml/uss. GameFlowController.Presentation expõe comandos e dados; transações de partida não ficam nos callbacks de renderização.

RenderTextures e câmeras dos personagens são reutilizadas de GameFlowController.Previews. Miniaturas continuam estáticas; não são instanciados personagens adicionais pelo presenter. A UI antiga permanece como fallback se os recursos do protótipo não estiverem disponíveis.

MenuNavigationInput centraliza navegação de teclado, d-pad e analógico, confirmações por borda e repetição limitada. O caminho antigo de seleção não lê input quando o protótipo está ativo. Mouse parado não altera foco. Gamepads são enumerados a cada leitura, sem guardar referências desconectadas.

Fluxo: menu → modo → personagens P1/P2 → arena → VS → carregamento → luta → vitória. Voltar da arena preserva P1 e retorna à escolha P2. Rematch conserva arena e spawns. Confirmação de arena é bloqueada durante transição.

### Plano de migração

1. Manter o menu UI Toolkit e o fallback IMGUI enquanto a seleção é validada.
2. Consolidar componentes/tokens USS e dados de apresentação, sem alterar FSM.
3. Migrar VS/vitória após aprovar o protótipo e animações reais do editor.
4. Avaliar HUD: UI Toolkit para barras/texto; uGUI apenas se necessidades de máscaras/animações justificarem. FightHudState permite trocar o renderer sem trocar HealthSystem.
5. Só remover telas antigas depois de registrar validação de navegação, resolução e fluxo completo.

## Contrato de arena

StageDefinition: ID estável explícito, nome, descrição, preview real, prefab, áudio, disponibilidade, ordem e configurações ambientais. StageRegistry é o catálogo único; rejeita IDs duplicados, dados ausentes e prefabs inválidos. Ordem de lista nunca é identidade.

StageManager aceita seleção por ID, prepara a instância na cena de combate compartilhada, evita duplicação, desativa os roots antigos indicados em StageSceneBinding e restaura estado individual e RenderSettings ao descarregar. StageEnvironmentController controla apenas ambientação, piso e referências. A arena não muda atributos de combate.

Hierarquia padrão: GameplaySurface, BackgroundArchitecture, MidgroundEnvironment, ForegroundDetails, Lighting, Atmosphere, AmbientAnimation, Audio, SpawnPoints, CameraBoundaries, Optimization. Apenas o piso tem colisor sólido. Elementos decorativos não impedem saltos, ataques ou movimentação.

Validação verifica superfície nivelada cobrindo os limites, spawns ordenados dentro do contrato e no topo do piso, escala unitária e colisores decorativos indevidos. Runtime exige contrato compatível com FighterMovement. Limites continuam pertencendo à mecânica original.

## Adicionar uma arena

1. Criar prefab com StageEnvironmentController e os grupos acima. Separar decoração do piso sólido.
2. Copiar contrato de FighterMovement; definir spawns nivelados e orientados um para o outro.
3. Criar StageDefinition pelo menu Create/UnDFight/Stage. Definir ID estável que não será alterado após publicação.
4. Adicionar a referência ao StageRegistry. Configurar áudio, iluminação e disponibilidade.
5. Gerar preview com StageTools.CapturePreview ou ferramenta de build; o preview deve vir da arena real.
6. Validar o catálogo e entrar programaticamente com `flow.Stages.Select(id)` antes da luta, ou `flow.ConfirmStage(id)` na seleção de arenas.
7. Testar voltar, rematch, KO, troca de arena e restauração do cenário legado.

Ferramentas: Tools/UnDFight/Stages. APIs UnityEditor e AssetDatabase ficam somente em Assets/Editor. O gerador não é executado em runtime. Rebuild sobrescreve somente os próprios assets gerados; não usar esse comando sobre alterações artísticas manuais sem um checkpoint.

## Etapas 2 a 8 — critérios e evidências

| Entrega | Critério | Evidência exigida |
| --- | --- | --- |
| Seleção | Teclado, gamepad, mouse, aleatório, voltar, confirmação única | Teste no Editor e imagens em quatro proporções/resoluções |
| Neon District | Rua noturna densa com lojas, luzes, chuva e asfalto | Preview real revisado antes das próximas arenas |
| Skyline Rooftop | Cobertura técnica aberta com skyline e luz de fim de tarde | Preview real e teste de carga |
| Underground | Estação subterrânea fria, colunas e trilhos fora da luta | Preview real e contrato validado |
| Ancient Temple | Pátio, madeira, pedra, cobertura curva e vegetação | Preview real; classificar limitações procedurais |
| Ironworks | Estrutura pesada, vasos/fornos/vapor e iluminação quente | Preview real e contagem de recursos |
| Auditoria | Sem vazamentos, duplicatas ou regressões de fluxo | Relatório JSON/MD com resultados reais |

**Código escrito e compilação não equivalem a comportamento testado.** Resultados posteriores devem registrar explicitamente testes executados, falhas e limitações. Não classificar estes módulos procedurais como arte final de produção ou afirmar FPS/GPU sem medição.

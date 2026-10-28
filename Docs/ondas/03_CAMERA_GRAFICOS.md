# Solario — câmera e gráficos

Modelo obrigatório gpt-6-luna / medium. Ler coordenação, TekkenCamera, prefabs de arenas, definições, URP e cenas (somente leitura).

Câmera é ORTOGRÁFICA. Aplicar altura +50% e aproximação +40% como fatores de apresentação ajustáveis em cima da base existente: 1.5x altura e .6x distância/orthographicSize, preservando acompanhamento e ambos os lutadores no quadro. Considerar também foco/olhar para elevar o enquadramento; só reduzir Z não muda zoom ortográfico. Evitar cortar cabeças no salto, pés/HUD e faixa sob o solo, inclusive em 16:9/21:9. Se um limite de segurança limitar o zoom em posições extremas, documentar. NÃO mudar física para atender à câmera.

Melhorar legibilidade gráfica com sol, sombras suaves e materiais URP, com custo controlado. Auditar configuração existente; criar uma ferramenta Editor separada `StageGraphicsUpgrade` que modifica apenas assets necessários e mantém composição das arenas. Preferir iluminação por arena, luz de preenchimento, materiais/roughness e pós-processamento sutil quando suportado. Não ativar recursos caros indiscriminadamente, não inventar dados de GPU ou substituir todo o render pipeline. Os quatro assets URP locais não versionados devem ser preservados.

Propriedade exclusiva: TekkenCamera.cs, novos scripts Editor/runtime próprios de gráficos, parâmetros de iluminação em StageDefinition/StageEnvironmentController/StageManager SOMENTE se indispensável e avisando Codex antes. NÃO editar StageTools*, GameFlowController*, Fighter*, UI, cenas ou prefabs diretamente neste primeiro trabalho. Ferramenta será executada pelo integrador para gerar as mudanças de assets. Não iniciar Unity, não usar MCP, não commitar/push.

Entregar método Editor reproduzível, explicação das porcentagens e dos limites de enquadramento, arquivos, configurações e como validar. Atualizar este MD; relatar pelo `maestri ask "Codex" "resultado"`.

## Implementação Solario (gpt-6-luna / medium)

### Câmera

`Assets/Scripts/TekkenCamera.cs` agora expõe multiplicadores de apresentação serializados: altura e foco usam `1.5` (+50%) sobre os valores existentes da cena, e o tamanho ortográfico calculado usa `0.6` (redução de 40%, equivalente a aproximar 1/0.6 = 1.67x). A mudança não depende de Z para simular zoom.

O tamanho solicitado é protegido por um mínimo vertical de `1.92` unidades (3.2 × 0.6), pelo espaço vertical ocupado pelos `CharacterController.bounds` projetado na orientação da câmera e por um ajuste horizontal baseado na distância entre os lutadores, margem `1.12` e aspect ratio atual. O ajuste horizontal é relevante em 16:9 e em proporções mais estreitas; em 21:9 há mais largura disponível. Assim, o zoom efetivo pode ficar abaixo dos 40% quando os limites reais dos corpos ou o espaço horizontal exigir, principalmente durante saltos/próximo do limite da arena. Sem `CharacterController` a proteção vertical usa o mínimo fixo. A câmera continua ortográfica e preserva o seguimento e foco nos dois lutadores. Esses limites são salvaguardas matemáticas, não uma validação visual de cada resolução; o integrador ainda precisa conferir frames de jogo, HUD e os extremos do salto.

### Gráficos e ferramenta Editor

`Assets/Editor/StageGraphicsUpgrade.cs` adiciona os menus `Tools/UnDFight/Graphics/Audit Stage Lighting` e `Upgrade Stage Lighting`. O upgrade localiza `StageDefinition` em `Assets/Stages`, carrega cada prefab referenciado e configura a luz direcional já existente para sombra suave em resolução média. Preserva tipo, transform, cor, intensidade e força da sombra de cada arena. Se um prefab não tiver luz direcional, ele é relatado como ignorado; nenhuma luz extra é criada. O processo é idempotente e salva apenas prefabs com direcional. Não mexe em cenas, geometria, perfis URP, configurações globais ou materiais. O diálogo de conclusão só abre fora de batch mode.

Auditoria feita por leitura dos perfis: PC usa URP com sombras suaves, AO SSAO ativo e resolução principal de sombra 2048; Mobile suporta sombra principal em 1024, mas tem sombras suaves e sombras de luzes adicionais desativadas. O script não habilita recursos do pipeline. Portanto, no perfil Mobile, a sombra principal permanece no modo suportado pelo perfil. Os materiais de arena têm faixas de roughness/smoothness intencionais para concreto, vidro, metal e asfalto molhado; não foram homogeneizados. A ferramenta inclui uma auditoria de luzes e materiais para o integrador revisar antes/depois.

### Validação pendente do integrador

Não executei Unity nem apliquei a ferramenta em assets. O integrador pode executar a auditoria e o upgrade no Editor, revisar o diff dos prefabs e validar o enquadramento em 16:9/21:9, perto/longe, salto alto e HUD antes de aceitar os assets gerados. Não houve validação visual nesta etapa.

# Etapas 2 a 8 — UI e arenas

## Escopo preservado

A entrega acrescenta arenas selecionáveis à cena de combate compartilhada e um protótipo de seleção UI Toolkit. Não substitui a FSM, os golpes, as velocidades, a gravidade, as colisões entre personagens, a câmera TekkenCamera ou o sistema de vitória da branch bugfix/test. O menu mantém sua imagem original. VS, vitória e HUD ainda possuem renderização IMGUI; não foram removidos sem uma migração visual aprovada.

A nova seleção tem fundo grafite, títulos Barlow Condensed (OFL incluída), acentos vermelho/amarelo, foco visível e transições de opacidade. Aproveita câmeras e RenderTextures que o projeto já produzia. Informações de classe/estatísticas vêm dos metadados existentes; não são novos atributos de combate.

## Etapa 2 — seleção de arenas

`Selection.uxml` define estrutura; `Selection.uss`, aparência; `SelectionPresenter`, apresentação e navegação. `MenuNavigationInput` é o leitor compartilhado entre seleção de personagens e de arenas. A tela antiga não lê entrada quando o protótipo está ativo, e eventos nativos de submit/navigation são interceptados nessa view para evitar dois caminhos de confirmação.

- Lista à esquerda, preview real central, nome/descrição e ações na faixa inferior.
- Todos os StageDefinitions disponíveis são consumidos do catálogo, sem nomes de arenas duplicados em runtime.
- Teclado: setas navegam, Enter confirma, Escape volta, R escolhe aleatório.
- Gamepad: d-pad/analógico navegam; botão sul confirma (A/Cruz), leste volta (B/Círculo), norte aleatório (Y/Triângulo).
- Mouse: botões e foco por movimento real do ponteiro. Mouse parado não toma foco do gamepad.
- Desconexão não deixa referência fixa ao dispositivo. O próximo teclado/gamepad disponível continua sendo lido.
- Entrada de confirmação usa borda; GameFlow bloqueia a transação quando VS/carregamento já começaram.
- Voltar da arena mantém P1 e retorna ao passo de P2. Revanche conserva arena, personagens e referências iniciais.
- Os previews são arquivos renderizados dos próprios prefabs, não imagens promocionais substituindo o cenário.

Fluxo: menu → personagens → arena → VS → carregamento → luta → vitória. As dimensões de referência da seleção são 1920×1080; a escala acompanha a altura e amplia a composição em ultrawide. O relatório JSON registra verificações em 1920×1080, 1280×720, 2560×1440 e 2560×1080.

## Etapas 3 a 7 — direções e implementação

| ID estável | Direção | Recursos construídos |
| --- | --- | --- |
| neon-district | Rua comercial noturna | Fachadas com caixilharia, lojas, letreiros, toldos, ar-condicionado com grelhas, vending machines com produtos/pagamento, postes de sinalização, cabos com flecha, skyline, sarjetas, asfalto especular, chuva e vapor |
| skyline-rooftop | Cobertura técnica ao entardecer | Piso com juntas, parapeito, guarda-corpo, entrada de serviço, HVAC, rotores animados, bandeiras discretas, skyline em três profundidades e céu de fim de tarde |
| underground | Estação subterrânea fria | Paredes cerâmicas, cobertura, arcadas, pilares com bases/capitais/rebites, plataforma sinalizada, bancos, escadas, trilhos/dormentes fora do plano de luta e poeira |
| ancient-temple | Pátio tradicional e jardim | Lajes com juntas, fundação/escadaria, pilares e travessas de madeira, shoji, telhados curvos com linhas de telhas, lanternas, cedros com folhas pontudas, jardim e relevo ao fundo |
| ironworks | Siderúrgica e máquinas pesadas | Chapas/rebites, pórticos/contraventamentos, ponte rolante, vasos com cintas/flanges, tubulações, fornos com grelhas, galeria industrial, passarela/escada, vapor e faíscas |
| rua-urbana-test | Compatibilidade | Exportação da decoração da cena existente, sem câmeras, managers ou lutadores; usa os spawns da RuaUrbana original |

### Materiais e áudio

Materiais usam URP Lit para metal, madeira, concreto, cerâmica e vidro opaco estilizado. Texturas procedurais pequenas acrescentam agregado, juntas e veios; letreiros usam superfície luminosa Unlit. Não foram implementados SSR, reflexos planares ou normal maps detalhados. O aspecto molhado vem de smoothness e highlights das luzes, e não deve ser descrito como reflexão real do cenário.

Os céus externos são cubemaps procedurais próprios. Cenários internos têm geometria de fechamento. Iluminação tem uma luz com sombras por arena e luzes locais sem sombras, incluindo preenchimento frontal para legibilidade dos lutadores. Atmosfera usa partículas com máximos explícitos e texturas suaves pequenas. Não há física ou colisores nas partículas.

As faixas WAV são ambiências sintéticas autorais curtas (chuva/vento/ruído/hum), com loop e volume por definição. Não representam gravações profissionais de campo nem uma trilha musical final. O áudio do menu é silenciado na luta; o ambiente pertence à instância da arena e é limpo com ela.

### Otimização adequada a esta base

Malhas estáticas são combinadas por grupo/material durante authoring. Animações e partículas ficam fora dessa combinação. Rotores reutilizam uma malha compartilhada. O skyline usa geometria simplificada; não há LOD complexo nem bake de iluminação neste estágio. Somente o cenário selecionado é instanciado. Como o catálogo referencia prefabs diretamente, seus assets podem continuar carregados; não há promessa de streaming/Addressables ou descarregamento individual de texturas.

O piso sólido cobre o contrato real de movimento. A continuação visual é maior para evitar bordas laterais dentro do enquadramento; isso não amplia o limite de deslocamento do lutador. Elementos decorativos não ganham colisores sólidos. A câmera original permanece responsável por distância e enquadramento.

## Etapa 8 — revisão e limites de qualidade

### Classificação artística

| Arena | Classificação atual | Motivo e próximo acabamento |
| --- | --- | --- |
| Neon District | Visualmente desenvolvida, base procedural | Identidade noturna/comercial e camadas presentes; fachadas ainda repetitivas, interiores simplificados e sinalização provisória em alfabeto latino |
| Skyline Rooftop | Blockout funcional com detalhamento procedural | Equipamentos, céu e skyline contextualizam a cobertura; edifícios ainda têm silhuetas e fachadas muito repetidas para arte final |
| Underground | Visualmente desenvolvida, base procedural | Estrutura fechada, plataforma, materiais e sinalização; faltam desgaste localizado, mobiliário e elementos ferroviários finais |
| Ancient Temple | Blockout funcional com detalhamento procedural | Coberturas/estrutura/pátio funcionais; vegetação, rochas e montanhas ainda têm aparência simplificada e precisam de arte dedicada |
| Ironworks | Visualmente desenvolvida, base procedural | Maquinário detalhado e leitura industrial própria; falta acabamento de superfícies, maquinário secundário e iluminação artística final |

**Nenhuma arena está classificada como pronta para produção.** As cinco são modelos 3D editáveis reais; também continuam sendo bases autorais procedurais. Diferenciação e integração técnica não equivalem à qualidade final de SF6/Tekken 8.

### Auditoria visual registrada

- Corrigidas normais do telhado que produziam faces negras e ausência de material.
- Corrigidos letreiros escuros; adicionados céu e separação de luz ambiente por arena.
- Corrigida presença do scrollbar padrão da Unity na seleção; grade de personagens dimensionada para mostrar os sete retratos.
- Removido o azul dos fundos dos RenderTextures de seleção; mantido o fundo original do menu principal.
- O enquadramento da luta legado é ortográfico e pode mostrar uma faixa abaixo da superfície ao nível do horizonte. Ele foi preservado conforme o escopo arquitetural; a aprovação artística deve decidir um ajuste global de câmera antes de chamar a apresentação de final. Não mascarar isso como validação de enquadramento perfeito.
- Capturas `fight-*.png` são renders da câmera 3D e não incluem o OnGUI do HUD. O estado/eventos do HUD são verificados separadamente pelo teste.

### Assets que ainda melhorariam o resultado

1. Materiais PBR tileáveis (2K suficiente inicialmente), com normal/roughness/AO: asfalto molhado, concreto técnico, azulejo envelhecido, aço pintado/oxidado, madeira e telhas.
2. Fachadas comerciais japonesas e interiores rasos, anúncios fictícios e fonte CJK com licença de redistribuição, para substituir sinalização provisória.
3. Dois ou três edifícios de skyline com módulos variados; rooftop com chaminés, ventilação e cabos finalizados.
4. Kit ferroviário com banco, escada, plataforma, sinalização e desgaste coerente.
5. Árvores/arbustos/rochas com materiais finais e LODs, além de relevo mais natural para o templo.
6. Equipamentos industriais detalhados, válvulas, correntes, painéis e decals de desgaste.
7. Ambiências sonoras dedicadas e revisão de mixagem/loop por arena.

Nenhum download adicional de modelos de cenário foi feito. A fonte Barlow Condensed inclui sua licença OFL no repositório.

## Operação e manutenção

Menu `Tools/UnDFight/Stages`:

- Build All And Test Arena: gera/atualiza as cinco arenas e a exportação de teste.
- Rebuild Neon District / Build Remaining Arenas: etapas separadas de authoring.
- Export Existing Test Arena: atualiza somente a arena de compatibilidade.
- Create Stage Template In Scene: gera a hierarquia e contrato iniciais com Undo.
- Validate Registry: verifica catálogo e referências/contratos.
- Update Selected Preview: renderiza o StageDefinition selecionado.
- Verify Flow And UI: executa o teste real de Play Mode e grava o relatório.
- Verify Repeated Stage Changes: alterna os seis cenários e os sete personagens em 18 ciclos de verificação de spawn e limpeza.

Não executar rebuild sobre alterações artísticas manuais sem checkpoint: o gerador atualiza seus próprios prefabs/materiais/malhas. Para uma arena criada manualmente, use outro ID/asset e apenas as ferramentas de validação/preview.

O resultado vigente dos testes é [validation/stage-results.json](validation/stage-results.json). A compilação rápida pode ser repetida com `Tools/Validate-CSharp.ps1` depois que o Unity tiver gerado seus arquivos de compilação. O teste de Play Mode é a evidência de comportamento; a compilação sozinha não o substitui.

# Onda 5 — integração e validação (Codex)

## Contrato de integração

Todos os agentes foram criados no canvas Maestri. Os processos foram conferidos pelo integrador: Cadencia/Prisma com `gpt-6.1-sol` e `model_reasoning_effort=low`; Solario/Brasa com `gpt-6-luna` e `model_reasoning_effort=medium`. Não foram usados subagentes fora do canvas. Os briefings individuais registram entregas e limitações.

O integrador é o único a executar Unity e salvar checkpoints. Não excluir animações antigas, não misturar iluminação diurna em arenas noturnas, não apagar os quatro assets URP locais fora da entrega e não usar junctions. O Editor aberto foi consultado: cena FightingPrototype sem alterações não salvas e fora de Play Mode antes da preparação.

## Pipeline e responsabilidades

1. Compilar pelo `Tools/Validate-CSharp.ps1` e atualizar o Editor somente quando as edições dos agentes terminarem.
2. Executar `CombatPresentationRound.Prepare`: gerar os clips/controller autorais; aplicar prefabs; configurar sombras da direcional existente; normais de superfície leves nos materiais URP Lit; importação dos quatro fundos e logo; SMAA High e dithering nas câmeras das duas cenas de combate.
3. Executar `AuthoredFighterMotionBuilder.DiagnoseSupport` em todos os sete Avatars. Não iniciar aprovação visual se a posição dos pés falhar.
4. Executar `CombatPresentationVerification.Start` para Home/modos, teclas/gamepad, guarda/agachar/saltos/golpes nos sete personagens, captura e medição de extensão dos membros.
5. Repetir o fluxo modular de arenas com StageVerification para garantir que os novos controllers e a apresentação não quebrem vitória, revanche ou seleção.
6. Salvar relatórios e capturas em `Docs/validation/presentation`. Distinguir código escrito de comportamento realmente observado.

## Ajustes de integração além das entregas

- O zoom é ortográfico: `0.6 × orthographicSize`; altura e foco usam `1.5 ×` a base, com abertura adicional quando necessária para conter os lutadores/saltos. Não prometer 40% fixos quando a margem física exigir recuo.
- O salto mantém altura original 2.2 em vez dos 1.45 propostos inicialmente, para não perder a passagem sobre um oponente em pé. Air control .9 acompanha a menor velocidade de caminhada. FlightRate usa altura/gravidade reais.
- Altura e centro do CharacterController/hurtbox agora acompanham a transição de agachar (0.28 s) e levantar (0.32 s), preservando a base. Deslocamento horizontal fica bloqueado inclusive enquanto levanta.
- Hitboxes autorais usam centro anatômico da palma/antepé em vez de um offset desenhado para outra pose inicial. Extensões/tamanho originais permanecem para preservar tolerância de contato.
- A apresentação de vitória passa a reconhecer o clip neutro `Guard` como alvo de override, preservando animações de vitória após a troca de controller.
- A referência `Spectators` da cena antiga é um grupo legítimo de espectadores, não lutadores esquecidos. Somente a captura diagnóstica os oculta temporariamente para isolar os dois sujeitos; o asset/cenário não é apagado.

## Falhas reais encontradas na primeira validação

1. Ferramenta gráfica inicial usava API GetInstanceID removida na versão do projeto e faltava namespace de LightShadowResolution. Solario corrigiu e a compilação passou.
2. Guard/crouch somente com curvas de músculos omitiram RootT/RootQ: ator ficou parcialmente enterrado. Captura e teste de apoio reprovaram, em vez de aceitar só compilação.
3. A primeira tentativa de converter HumanPose.bodyPosition diretamente para RootT também falhou: no CasualSmilingMan o pé saiu de 0.0837 para ~0.403 na guarda e ~0.756 agachado. Convenções diferentes não são intercambiáveis.
4. A nova calibração usa os sete canais originais de Fighter_Idle, SampleAnimation real e derivada numérica do apoio em função de RootT.y; exige validação do clip final antes de salvar. O resultado efetivo deve ser consultado no relatório vigente, não inferido deste briefing.

A Home e os modos já foram observados em 720p/1080p/ultrawide. Ajustes de foco quente e persistência de foco de opções foram solicitados ao Prisma após as primeiras imagens. Os quatro fundos são imagens imagegen reais 1672×941, não placeholders; a logo foi copiada sem alterar seus bytes. Fontes institucionais e prompts estão na onda 4.

## Critérios finais

## Revisão após capturas — 27/28 de outubro

- O primeiro relatório aprovado ainda não verificava a direção anatômica do ataque. A inspeção das imagens e métricas encontrou chute para trás. O gerador agora compara candidatos por `SampleAnimation` no rig real antes de produzir os clips. A verificação runtime exige avanço proporcional ao comprimento do membro e contato próximo da extensão máxima, em todos os sete personagens.
- As capturas dos picos aguardam dois frames para atualizar a malha renderizada; medir ossos imediatamente após `Animator.Update(0)` não garantia que a imagem refletisse a mesma pose.
- O agachamento usa flexão de quadril para frente. A rotação IK dos pés usa direção dos dedos projetada no chão, porque os eixos dos ossos importados não são os eixos dos alvos IK. A captura Tiago-crouch passou a apresentar os dois pés apoiados.
- O teste real de cruzamento descobriu pouso sobre a cápsula do adversário e afastamento indevido de quem estava embaixo. Colisão entre cápsulas é suspensa quando a diferença vertical supera 60% da altura menor, mantida durante a passagem e restaurada após separação/pouso. Hitboxes/hurtboxes permanecem ativas. A distância mínima terrestre só se aplica em alturas próximas. A orientação acompanha a troca de lado no ar sem disparar o clip de giro terrestre.
- Entrada configurada de P2 não herda o fallback WASD de P1. Há teste com teclado virtual para essa independência.
- Limitação de asset: os sete Avatars Meshy não mapeiam dedos no Humanoid. As curvas de punho ficam preparadas para rigs completos, mas não fecham uma mão sem articulações de dedos mapeadas. Isso exige melhorar o rig; não é correto declarar esse detalhe visual resolvido.

Evidência vigente: `Docs/validation/presentation/results.json` e PNGs ao lado; regressão de arenas/fluxo em `Docs/validation/stage-results.json`. Consulte `passed`, `date` e `failure`: capturas e relatórios anteriores podem registrar falhas corrigidas posteriormente. Não interpretar a simples existência dos arquivos como aprovação.

Pés apoiados e cabeça acima do piso; sem deriva ao agachar; salto termina no chão e permite troca de lado; contato ofensivo segue a extensão visual e encerra antes do retorno; recuperação completa; câmeras contêm os dois corpos; menus com rótulos dentro dos botões e sem confirmação duplicada; recursos gráficos sem duplicar sóis/ambientes; relatórios e limitações explícitos. Não chamar animação procedural de motion capture ou afirmar fotorealismo.

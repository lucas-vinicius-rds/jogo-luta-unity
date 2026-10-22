# Teste visual de retargeting — 2026-10-22

Cena Assets/Scenes/_TesteRig.unity criada pelo Unity MCP, usada para captura e apagada ao terminar, incluindo seu .meta. RuaUrbana foi reaberta sem alterações pendentes. SHA-256 da cena e de seu .meta permaneceram idênticos (ruaurbana-verification.json).

## Limitação da comparação

Assets/Animations/Mixamo/enomoto.fbx contém 52 ossos Humanoid mapeados, mas ZERO Renderers/malhas. Os 38 FBXs Mixamo inspecionados estão sem malha. Portanto, enomoto aparece como esqueleto laranja desenhado com LineRenderers sobre os ossos reais animados; não se pode avaliar deformação de superfície dele. Não foi substituído por outro personagem.

O Meshy é Assets/MeshyImports/Man in Black_20260902_155135/Meshy_AI_Man_in_Black_biped_Animation_Running_withSkin.fbx, com 22 ossos Humanoid mapeados, usando nas cópias temporárias os materiais do prefab ManInBlack. Na Game View: Meshy à esquerda; esqueleto Mixamo à direita. Não foram editados FBXs, importadores, prefabs nem materiais originais.

## Método

Mesmo clipe Mixamo e mesmo tempo normalizado em ambos os Animators via PlayableGraph manual. Root motion desativado; sem scripts de combate e sem IK adicional. Capturas 1920x1080 da câmera da Game View e da câmera real da Scene View por métodos do bridge MCP (não são capturas dos painéis da interface do Editor). A Scene View usa um ângulo oblíquo para examinar o perfil e as articulações.

Capturas finais realizadas com AlwaysAnimate, SkinnedMeshRenderer.updateWhenOffscreen e forceMatrixRecalculationPerRender nas cópias temporárias. Isso foi necessário para evitar imagem de skinning desatualizada no mesmo tick do Editor. A primeira tentativa de Scene View também precisou corrigir a posição/clipping de sua câmera. Os PNGs finais substituem essas tentativas e foram inspecionados visualmente em poses representativas.

## Observações visuais nas amostras

| Região | Idle | Cross Punch | Walking |
|---|---|---|---|
| Ombros | Contorno acompanha a guarda, sem colapso evidente. | Leve achatamento na transição manga/axila quando o braço eleva/avança; sem torção grave ou ombro deslocado nas amostras. | Sem deformação anormal evidente. |
| Braços | Cotovelos ficam algo angulosos quando muito dobrados; leve perda de volume aparente. | Compressão/ângulo no cotovelo flexionado mais perceptível; braço do soco estende sem alongamento grosseiro. | Sem alongamento ou inversão evidente. |
| Quadril | Sem colapso aparente da cintura/virilha. | Acompanha a rotação do tronco, sem deslocamento grosseiro aparente. | Sem torção grave ou ruptura aparente. |
| Pernas | Joelhos acompanham a flexão da guarda sem inversão aparente. | Pés/pivô e flexão dos joelhos acompanham a pose; sem pernas quebradas aparentes. | Passada coerente; pregas/ângulos na região dos joelhos da calça, sem colapso grave aparente. |

As pequenas perdas de volume são observações de superfície; este teste não isola a causa entre topologia, pesos e pose de referência. Não foi comprovado erro estrutural no rig. Há uma limitação clara nas mãos: o Meshy não tem ossos de dedos mapeados e não reproduz integralmente o fechamento dos dedos do Mixamo; a mão fica relativamente aberta no soco.

O esqueleto de enomoto acompanha os três clipes sem ruptura visual nas poses amostradas. Isso não prova qualidade de skinning de uma malha Mixamo, pois essa malha não existe nesses arquivos. São amostras estáticas, não avaliação exaustiva de cada frame ou de uma partida completa.

## Capturas

15, 50 e 85 representam 15%, 50% e 85% da duração de cada clipe. No soco foram acrescentados 25%, 75% e 57,5%; o sufixo 57 corresponde a 57,5% (1,15 s), selecionado após varredura da extensão do cotovelo direito. Total: 12 poses e 24 PNGs.

| Animação | Fase | Game View | Scene View |
| X Bot@Bouncing Fight Idle | 15.0% (0.445 s) | [PNG](Bouncing_Fight_Idle_15_GameView.png) | [PNG](Bouncing_Fight_Idle_15_SceneView.png) |
| X Bot@Bouncing Fight Idle | 50.0% (1.483 s) | [PNG](Bouncing_Fight_Idle_50_GameView.png) | [PNG](Bouncing_Fight_Idle_50_SceneView.png) |
| X Bot@Bouncing Fight Idle | 85.0% (2.522 s) | [PNG](Bouncing_Fight_Idle_85_GameView.png) | [PNG](Bouncing_Fight_Idle_85_SceneView.png) |
| X Bot@Cross Punch | 15.0% (0.3 s) | [PNG](Cross_Punch_15_GameView.png) | [PNG](Cross_Punch_15_SceneView.png) |
| X Bot@Cross Punch | 50.0% (1 s) | [PNG](Cross_Punch_50_GameView.png) | [PNG](Cross_Punch_50_SceneView.png) |
| X Bot@Cross Punch | 85.0% (1.7 s) | [PNG](Cross_Punch_85_GameView.png) | [PNG](Cross_Punch_85_SceneView.png) |
| X Bot@Walking | 15.0% (0.153 s) | [PNG](Walking_15_GameView.png) | [PNG](Walking_15_SceneView.png) |
| X Bot@Walking | 50.0% (0.508 s) | [PNG](Walking_50_GameView.png) | [PNG](Walking_50_SceneView.png) |
| X Bot@Walking | 85.0% (0.864 s) | [PNG](Walking_85_GameView.png) | [PNG](Walking_85_SceneView.png) |
| X Bot@Cross Punch | 25.0% (0.5 s) | [PNG](Cross_Punch_25_GameView.png) | [PNG](Cross_Punch_25_SceneView.png) |
| X Bot@Cross Punch | 75.0% (1.5 s) | [PNG](Cross_Punch_75_GameView.png) | [PNG](Cross_Punch_75_SceneView.png) |
| X Bot@Cross Punch | 57.5% (1.15 s) | [PNG](Cross_Punch_57_GameView.png) | [PNG](Cross_Punch_57_SceneView.png) |

Backups prévios: RuaUrbana.unity.backup, RuaUrbana.unity.meta.backup e REGISTRO_DE_ALTERACOES.md.backup. Scripts e respostas do Unity MCP estão nesta pasta. Não houve necessidade de Blender; nenhuma correção de skinning foi aplicada nesta etapa.

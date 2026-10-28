# Cadencia — animação e movimento

Modelo obrigatório gpt-6.1-sol, reasoning low. Leia `Docs/ondas/00_COORDENACAO.md` e a skill unity-animation em `../.agents/skills/unity-animation/SKILL.md` (também existe no catálogo de skills). Examine rig humanoide, Animator, controller/overrides, FSM, root motion, timing, colisões e física ANTES de escolher implementação.

Objetivo: criar movimentos autorais plausíveis nos rigs existentes (preferir dentro da Unity, sem exigir Blender). Substituir movimento acelerado/truncado por preparação, transferência de peso e recuperação legíveis. Implementar base consistente: guarda estável, andar à frente e atrás, saltos vertical/frente/trás sincronizados com subida/ápice/aterrissagem reais, agachar apenas parado com pés apoiados e sustentar a pose até soltar. Quando agachado, NÃO deslocar na horizontal.

Rever soco e chute; criar se tecnicamente viável ou reaproveitar clipes corretos com retiming documentado. Não chamar pequena oscilação de osso de animação realista. Preferir clipes Humanoid autorais por músculos ou um sistema esquelético procedural com pose-base controlada, transições e IK, justificado e observável. Não reconstruir sobre pose de corrida. Evitar dupla aplicação de root motion e CharacterController, pés deslizando e snaps de retorno. Golpe ativo somente no pico de extensão, recuperação antes do próximo ataque; não manter hitbox ativa no retorno.

Propriedade exclusiva: FighterMovement.cs, FighterController.cs, AttackState.cs, NeutralState.cs, FighterStateBase.cs se necessário, novos scripts runtime/editor de movimento/animação, assets próprios `Assets/Animations/Authored`, controllers/overrides e prefabs Characters se necessários. NÃO editar GameFlowController*, TekkenCamera, UI, StageTools, cenas ou ferramentas de outros agentes. Não rodar Unity: entregar um método Editor estático para o integrador gerar/revisar assets. Não fazer commits/push.

Entregáveis: implementação, pipeline reproduzível para gerar movimentos, tabela de duração/contato por ação, instruções de validação, limitações reais. Citar arquivos e evitar alegar teste visual não executado. Reportar a Codex pelo Maestri ao concluir e avisar se precisar de uma pequena integração em GameFlowController. Não chamar outros agentes fora do canvas.

## Entrega Cadencia — 2026-10-27

Configuração gpt-6.1-sol / low confirmada pelo integrador via Win32_Process/cabeçalho. Skills aplicadas: maestri e unity-animation. Comunicação exclusivamente por Maestri após leitura inicial. Sem Unity, commit, push, importações externas ou edição de arquivos dos demais agentes.

### Inspeção e decisão

`FightingPrototypeSetup` cria Animator Humanoid na raiz com Avatar dos modelos Meshy e desativa Animator da malha. Os sete prefabs usam overrides próprios e CharacterController. A base usava caminhada 1.35x, retorno 1.15x e crouch locomotor; `CrossFadeAnimation` chamava Play, ignorando sua duração. AttackState adicionava impulso e root motion, e ApplyAttackRootMotion repetia Y já integrado no Update. FSM mantém Neutral/Attack/HitStun/Knockout; dano e knockback-base não foram alterados.

Escolha: clipes Humanoid autorais de músculos completos, zerando todos os músculos antes da pose de guarda (sem capturar corrida), transições suaves e IK de apoio para crouch. Locomoção reutiliza os clipes existentes Walking/Walk Backwards em 1x. Soco e chute agora têm preparação, rotação/chamber, pico de extensão e recuperação próprios. São movimentos paramétricos iniciais a revisar, sem alegação de realismo validado.

### Arquivos e execução reproduzível

- `Assets/Editor/AuthoredFighterMotionBuilder.cs`: método estático `AuthoredFighterMotionBuilder.Generate`, menu **UnDFight > Animation > Generate Authored Motions**. Gera/atualiza `Assets/Animations/Authored/*.anim` e cópia `AuthoredFighter.controller`; aplica controller, velocidades, timing e grounding aos prefabs de `Assets/Prefabs/Characters`. Executar **depois** de FightingPrototypeSetup, que recria os prefabs. Reexecução preserva GUIDs dos clipes gerados.
- `Assets/Scripts/AuthoredFighterGrounding.cs`: IK de pés plantados e abaixamento do corpo ao agachar; FlightRate calculado a partir da altura/gravidade reais.
- `FighterMovement.cs`: aceleração/desaceleração gradual; bloqueio horizontal integral de crouch (inclusive impulso enquanto crouch; hitstun libera postura), collider mantém base ao reduzir altura, Grounded alimentado pela colisão real, gravidade integrada uma só vez.
- `FighterController.cs`: CrossFadeInFixedTime real; leitura do próximo estado durante blend para duração/progresso do ataque.
- `AttackState.cs`: sem lunge ou root motion de ataque, velocidade limitada, janela não abre se um frame já passou do fim, recuperação completa antes de outro ataque.

Não há integração obrigatória em GameFlowController: ele guarda/restaura o controller original do prefab; apresentação de vitória continua separada. Grounding se aplica somente ao controller AuthoredFighter.

### Durações e contato após Generate

| Ação | Duração | Contato / apoio |
| --- | --- | --- |
| Guarda | 1,60 s loop | Pose estável, sem oscilação artificial |
| Frente / trás | Duração original Walking / Walk Backwards, 1x | Velocidades 1,55 / 1,20 unidades/s; aceleração 14 unidades/s² |
| Agachar / levantar | 0,28 / 0,32 s | Hold 1 s em loop constante; IK mantém pés, corpo desce 0,28 × humanScale |
| Salto vertical/frente/trás | 2 sqrt(2h / -g), ~0,762 s com h=1,45 e g=-20 | Pico em ~0,381 s; clip parametrizado por FlightRate; saída apenas ao Grounded real |
| Soco Cross | 0,92 s | Mão direita ativa 0,3588–0,3956 s (39–43%); pico 43%; recovery até 100% |
| Chute frontal | 1,18 s | Pé direito ativo 0,5428–0,5900 s (46–50%); pico 50%; recovery até 100% |

Nenhum clip antigo/controller/override é apagado. Para reverter visualmente, restaurar controllers originais nos prefabs (ou regenerar setup antigo); os scripts suportam ausência dos parâmetros novos. O gerador substitui a identidade dos golpes por um par autoral comum; não mantém variações antigas por personagem.

### Evidência e validação pendente do integrador

Inspeção textual de scripts, prefabs, controller e importadores; revisão do diff dos arquivos próprios. Compilação runtime/editor solicitada ao integrador, incluindo os dois arquivos novos. **Nenhuma execução Unity, captura real, teste visual ou compilação local foi realizada por Cadencia.** Os assets autorais serão materializados pela execução do método Editor pelo integrador.

Validar em todos os sete Avatars: curvas reconhecidas como Humanoid; guarda/joelhos/cotovelos plausíveis e sem interpenetração; S+A/S+D mantém X imóvel e pose sustentada; soltar S retorna suave; collider/hurtbox preservam base. Testar W, W+A, W+D nos dois lados e cruzamento, capturando saída/apex/aterrissagem e garantindo que não retorna a andar ainda no ar. Testar soco/chute com gizmos da hitbox nos frames imediatamente antes/durante/depois das janelas; nenhum dano no retorno, duplo hit ou cancelamento da recuperação. Repetir sob hitstop, hitstun, KO, reset e vitória.

Limitações reais: a formulação do salto usa trajetória balística em piso plano (sem antecipação pré-decolagem e sem clip dedicado de amortecimento no pouso); não há foot locking por fase na caminhada, então deslizamento precisa ser medido visualmente e velocidade ajustada ao stride; IK de crouch usa último apoio observado e não faz adaptação ao relevo; transferência de peso/contato dos golpes depende de revisão dos Avatars. Não apresentar estes movimentos como animação realista aprovada antes de observar frames reais. Se curvas de músculos/IK falharem na importação, manter controllers antigos e corrigir o gerador antes de aceitar.

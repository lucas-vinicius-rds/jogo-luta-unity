# Rodada de câmera, movimento, gráficos e identidade

Solicitação: câmera 50% mais alta e 40% mais próxima; movimentos mais humanos e menos acelerados/truncados; andar para frente/trás, salto vertical/frente/trás, agachar imóvel; rever golpes e sincronização; melhorar sol/sombras/shaders; refazer Home e seleção de modo; quatro fundos coerentes com logo e Universidade do Distrito Federal.

Referência: `Logotipo arcade UnDFight em chamas.png` na raiz do projeto. Base: df87d2a, Unity 6000.5.9f1 / URP 17.5 / Input System 1.20. Câmera atual ortográfica: aproximar requer alterar orthographicSize, não só Z.

Agentes SOMENTE via canvas Maestri, sem subagentes fora dele:

| Agente | Modelo / esforço | Propriedade |
| --- | --- | --- |
| Cadencia | gpt-6.1-sol / low | animação, movimento, timing, gerador de movimentos e relatório |
| Prisma | gpt-6.1-sol / low | Home/modos e integração visual dos quatro fundos nas views |
| Solario | gpt-6-luna / medium | TekkenCamera e ferramenta isolada de melhoria gráfica |
| Brasa | gpt-6-luna / medium | pesquisa institucional, direção e geração de quatro fundos + logo importada |
| Codex | integrador | integração, execução única do Unity, validação, checkpoints e documentação final |

Regras: propriedade exclusiva dos arquivos; não editar arquivos de outro agente. Não usar git reset/clean, apagar pastas, junctions, commits globais ou push. Não iniciar Unity/geradores em paralelo: apenas o integrador executa o Editor. Código pode ser compilado pelo CLI de validação se não disputar arquivos temporários; preferir solicitar compilação ao integrador. Não apagar animações antigas: substituir o uso de forma reversível. Não importar animações externas sem licença clara. Não fingir que um movimento foi validado visualmente antes de observar frames reais.

Cada agente atualiza seu briefing com mudanças/evidência/limitações e responde via `maestri ask "Codex" "resultado"`. Preservar os quatro assets locais de configuração URP ainda não versionados. Não alterar números de dano/balanceamento sem necessidade; sincronizar janelas com os novos movimentos. Manter todos os estados/controles existentes funcionais.

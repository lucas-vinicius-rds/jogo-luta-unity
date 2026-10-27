# Medições reais de arenas

Execução UTC: 2026-10-27T22:20:42.5654117Z. Unity 6000.5.9f1, Editor batch/D3D11.

Fluxo completo: **153 verificações aprovadas**. Estresse: **91 verificações aprovadas em 18 trocas**, percorrendo os seis cenários e os sete personagens.

| Arena | Renderers | Triângulos da arena | Materiais | Luzes / com sombras | Máx. partículas | Instanciar/aplicar (ms) | Main Thread médio (ms) | GC médio (KiB/frame) | Texturas globais (MiB) |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| neon-district | 20 | 33740 | 12 | 4 / 1 | 184 | 44.60 | 16.10 | 24.20 | 134.62 |
| skyline-rooftop | 16 | 38120 | 7 | 2 / 1 | 12 | 12.68 | 16.10 | 19.12 | 135.24 |
| underground | 11 | 18580 | 8 | 3 / 1 | 26 | 11.71 | 16.10 | 24.57 | 135.60 |
| ancient-temple | 12 | 71401 | 10 | 2 / 1 | 24 | 13.42 | 16.10 | 21.37 | 135.98 |
| ironworks | 12 | 20600 | 7 | 4 / 1 | 95 | 11.55 | 15.85 | 20.86 | 136.59 |

CPU/GC são amostras de 30 frames reais com o jogo, HUD, Editor e harness ativos, incluindo espera/limitação de frame. Não são custo isolado de cenário nem garantia de FPS. As texturas são o total global, incluindo UI, personagens e assets referenciados pelo catálogo.

O tempo de instanciação inclui Instantiate, integração à cena e configuração ambiental; os assets já estavam carregados. Não mede leitura fria, compilação inicial de shaders ou toda a tela VS. O primeiro mapa teve custo maior, compatível com aquecimento/JIT; o carregamento acontece antes de habilitar a luta.

Draw Calls Count e Triangles Count do Profiler não retornaram dados utilizáveis neste batch (-1 no JSON). Renderers não são draw calls. GPU indisponível; não foi fabricado resultado de GPU/FPS. Para orçamento de produção, medir um build standalone no hardware alvo e inspecionar o primeiro frame de cada arena.

Fontes: [stage-results.json](validation/stage-results.json) e [stage-stress-results.json](validation/stage-stress-results.json).

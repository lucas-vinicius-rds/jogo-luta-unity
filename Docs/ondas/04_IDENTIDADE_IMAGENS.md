# Brasa — imagens e identidade

Modelo/esforço efetivos confirmados pelo integrador via processo Win32 e cabeçalhos dos terminais: `gpt-6-luna / medium`.

## Direção e referências

A logo de referência foi inspecionada: letras inclinadas com recortes de energia, azul/ciano contra vermelho/laranja/amarelo, contorno/sombra escuros. Ela foi preservada por cópia byte a byte, sem redesenho. Os fundos gerados usam essa paleta e linguagem de diagonais como direção visual, sem inserir uma logo falsa, brasão, sinalização institucional ou personagens de terceiros.

O nome da solicitação foi interpretado como Universidade do Distrito Federal (UnDF), instituição pública do DF, e não como Centro Universitário UDF. Fontes oficiais usadas para orientar a ambientação:

- [Estatuto da UnDF — SINJ/DF](https://www.sinj.df.gov.br/sinj/Norma/1b514405969647f599028393a62e3b39/undf_res_3_2022.html): identifica a Universidade do Distrito Federal (UnDF) e sua sede em Brasília.
- [Agência Brasília — instalações do Campus Norte e planos para o BIOTIC](https://www.agenciabrasilia.df.gov.br/web/guest/topicos?delta=40&sort=createDate-&tag=Universidade+do+Distrito+Federal+Professor+Jorge+Amaury+Maia+Nunes+%28UnDF%29): registra o campus no Lago Norte e a instalação planejada no Parque Tecnológico de Brasília (BIOTIC), Granja do Torto.
- [Portal de Serviços Digitais da UnDF](https://portal.undf.edu.br/): confirma a presença institucional de mapas de campi, incluindo Ceilândia e Lago Norte.
- Referência visual local: `Logotipo arcade UnDFight em chamas.png` na raiz do projeto.

Inferência de direção: como o pedido é por fundos arcade e não por reprodução documental de uma unidade específica, os cenários são arenas universitárias fictícias inspiradas em concreto/vidro, espaços amplos e geometria brasiliense. Não representam um campus real nem afirmam retratar a arquitetura oficial da UnDF.

## Entregáveis

Arquivos copiados para `Assets/Resources/UI/Brand/`:

| Arquivo | Uso/composição | Dimensões | Origem |
| --- | --- | ---: | --- |
| `HomeBackground.png` | Arena universitária noturna; arquitetura e luz no lado direito, grande área escura à esquerda para logo/menu. | 1672×941 | Imagegen, gerado para este projeto |
| `ModeBackground.png` | Praça de confronto simétrica, iluminação ciano/azul à esquerda e quente à direita. | 1672×941 | Imagegen, gerado para este projeto |
| `CharacterBackground.png` | Arena coberta escura; centro e laterais com baixo contraste para os renders de personagem. | 1672×941 | Imagegen, gerado para este projeto |
| `StageBackground.png` | Vista ampla de campus/cidade à noite, com faixa inferior central calma para miniaturas reais de mapas. | 1672×941 | Imagegen, gerado para este projeto |
| `UnDFightLogo.png` | Cópia sem edição da logo original da raiz. | 1993×789 | `Logotipo arcade UnDFight em chamas.png` |

Os quatro fundos são aproximadamente 16:9 (geração retornou 1672×941); não foram redimensionados nem retocados. A logo copiada mantém o mesmo tamanho e bytes da fonte. Os PNGs foram inspecionados visualmente após geração. A resolução ficou abaixo do ideal de 1920×1080, mas mantém enquadramento widescreen adequado para os usos descritos. Não houve validação dentro do jogo; a legibilidade final, recortes e importação devem ser conferidos pelo integrador no Unity. Nenhum `.meta` foi criado manualmente; o importador do Unity pode gerá-los na execução autorizada do integrador.

## Prompts usados

Ferramenta: imagegen integrado, uma geração individual para cada cenário. Os prompts abaixo reproduzem os briefings enviados (em inglês):

### Home

> Use case: stylized-concept. Asset type: 16:9 fighting game main menu background, full bleed image. Create a cinematic fictional university arena at night in Brasília, inspired by modern public university campus architecture and the broad civic geometry of the capital: angular concrete and glass buildings, monumental open plaza, distant modernist skyline, subtle tropical landscaping. Composition: dramatic focal arena and architecture mostly on the right half, left half and upper center kept dark and uncluttered as generous negative space for an overlaid game logo and menu. Visual direction derived from the supplied UnDFight logo reference: energetic diagonal cuts and streaks of cyan/electric blue meeting red, orange and yellow flame light; black/navy shadows; high contrast arcade fighting game concept art, polished painterly 3D environment, cinematic volumetric lighting. A few small abstract energy sparks only, no people. Keep wide composition with safe negative space for ultrawide crops. No text, no logos, no signage, no badges or crests, no HUD, no UI controls, no watermark, no recognizable commercial game references. High detail 16:9 landscape background.

### Seleção de modo

> Use case: stylized-concept. Asset type: 16:9 fighting game mode selection background, full bleed image, same cohesive visual universe as an arcade fighting game set in Brasília. Scene: a fictional university plaza at night transformed into a versus arena, angular modernist campus and glass/concrete buildings, two broad opposing architectural wings framing an open central dueling space, distant Brasília civic skyline and subtle tropical silhouettes. Composition: strong left-right confrontation, cool cyan/electric blue illumination on left, warm red/orange/yellow flame illumination on right; center stays readable and uncluttered; darker upper bands provide space for overlaid mode title and UI. Match a polished cinematic concept-art environment with bold diagonal energy streaks, deep navy/black shadows, reflective stone and glass, dramatic but controlled lighting. No people, no text, no logos, no signs, no crests, no interface, no buttons, no watermark, no copyrighted game references. Keep framing safe for ultrawide crop; landscape 16:9.

### Seleção de personagens

> Use case: stylized-concept. Asset type: 16:9 fighting game character selection background, full bleed image behind selectable fighter renders. Scene: shadowy fictional university combat hall in Brasília, architectural blend of modernist concrete, glass and subtle tropical landscape visible through high windows, a restrained arena floor and distant campus lights. Composition: darkest value across both side thirds and middle foreground so full-body character renders remain readable; muted blue/cyan rim lights on the left, red/orange/gold rim lights at far right, a quiet central depth opening for UI focus. Cohesive with an arcade fighting game identity using sharp diagonal light cuts, deep navy and black shadows, cinematic polished concept art and restrained atmospheric haze. Keep background detail low where characters would appear; no characters or silhouettes. No text, logos, signage, crests, HUD, controls, watermark, or commercial game references. Wide 16:9 with safe ultrawide crop.

### Seleção de mapas

> Use case: stylized-concept. Asset type: 16:9 fighting game stage selection background, full bleed image behind actual stage thumbnail cards. Create a cinematic Brasília-inspired campus and city overlook at night: a university rooftop terrace or elevated plaza looking across a modernist civic skyline and a fictional campus district, with angular concrete, glass, tropical plants and distant illuminated roads. Composition: broad landscape vista, atmospheric depth, keep lower middle and central band relatively calm and dark so real stage thumbnails can overlay; recognizable architectural variety in the distance without a single dominating subject. Cohesive UnDFight arcade identity: deep navy shadows, cyan/electric blue streaks balanced by red/orange/yellow flame-like illumination on architectural edges, polished concept art, controlled cinematic lighting. Campus/city environment only; no combatants. No text, logo, signs, crests, interface, buttons, watermark, copyrighted game references. Maintain wide 16:9 framing with ultrawide crop safety.

## Limitações e validação

- Os fundos são imagens geradas, não fotografias de campi reais.
- Apesar dos pedidos 16:9, o gerador entregou 1672×941; não foi feito upscale.
- A inspeção visual foi feita nos PNGs gerados. Não houve montagem de contact sheet nem validação em resolução nativa no Unity, que cabe ao integrador.
- Assets entregues e importáveis em `Assets/Resources/UI/Brand/`; views/código de UI pertencem a outro agente e não foram editados.

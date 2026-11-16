# Relatório Pass D1 — Névoa e legibilidade

Branch `sprint-1/pass-d1-nevoa` (empilhada sobre o Pass A). Unity 6000.3.10f1, URP 17.3. Valores do Editor (Play Mode), não de build.

## 1. O que mudou (arquivo a arquivo)
- `Assets/testes shaders/Daynightcontroller.cs`: **convertido de Windows-1252 para UTF-8 como passo separado e explícito** (`iconv -f cp1252 -t utf-8`;
  corrige os acentos dos tooltips que o Unity lia errado; CRLF mantido) e depois editado. Continua o **único escritor** de `RenderSettings`
  (nenhum controlador novo; nome, namespace e pasta intactos). Campos antigos mantidos. Novos:
  - Névoa: `modoDaNevoa` (Linear/Exponential/ExponentialSquared), `fogInicio`, `fogFim`, `misturaDaNevoaComHorizonte`, `nevoaDessaturacao`, `nevoaClareamento`
    (cor da névoa = céu misturado com um "horizonte" dessaturado e, de dia, mais claro; `misturaDaNevoaComHorizonte = 0` reproduz o antigo).
  - Sol/lua: `elevacaoMinimaDoSol`, `duracaoDoCrepusculo`, `luaCor`, `luaIntensidade`, `luaElevacaoMinima`; ambiente: `ambienteDessaturacao`, `preenchimentoNoturno`.
  - Funções públicas/puras para teste: `Apply()` (era privada), `AvaliarLuz(hora, out anguloX, out intensidade, out cor)`, `FatorDeDia(hora)`, `CorDaNevoa(...)`.
  - Sol: X de `e` a `180-e` (e = 26°) só durante o dia, com crepúsculo de 2 h de jogo (a intensidade sobe de 0 antes do nascer e desce a 0 depois do pôr);
    de noite a **mesma Light** vira lua em arco (X de 25° a 155°), intensidade 0,35, cor fria. Na troca sol-lua as duas intensidades são 0 (a mudança de
    direção não aparece). Nunca mais ilumina por baixo.
  - Ambiente: Trilight como antes, dessaturado em 85 % em direção ao cinza de mesma luminância (o topo da ilha não fica "laranja chapado" no pôr do sol) e
    somado a `preenchimentoNoturno` x fator de noite (0 de dia).
  - Curva de intensidade padrão: sem a chave 0,22 (0,2 -> 1,0 em 43 min); chaves 0,2 / 1,0 / 1,4 / **1,5** (18h) / 0,2.
  - "Segunda Light?": **não foi necessária** (uma só Light: sol de dia, lua de noite).
- `Assets/Scenes/HexPrototipo.unity`: ver seção 3.
- `Assets/Tests/Editor/`: `DayNightControllerTests` (Logic, 13), `NevoaSceneTests` (SceneStatic, 4), `NevoaPlayModeTests` (PlayModeSmoke, 1) + `.meta`.
- `tools/visual-baseline/`: `legibilidade.py` (métrica), `run_baseline.py` (comando `pontos`, `LOOPIA_HORAS_EXTRAS`, `LOOPIA_MODO_ANTIGO`, `luz` por captura no manifest),
  `cs/pontos.cs`, `cs/measure_profundidade.cs`, `cs/antigo.cs`, `README.md`.
- `docs/art/pass-d1/**`: capturas, medidas e este relatório.

## 2. Aceite (números reais)
| Métrica | Alvo | Antes | Depois |
|---|---|---|---|
| razão topo/fundo, 12h | >= 1,25 | 1,28–1,32 (emulado; 3 seeds, 2 resoluções) | **2,48–2,85** |
| razão, 18h | >= 1,25 e matiz do topo != matiz da névoa | 0,82–0,87; topo 21° vs névoa 12° (seed 12345): laranja chapado | **1,80–2,12**; topo **70°** (verde-amarelado) vs névoa 12° (seed 12345) |
| razão, 22h | >= 2,0 | 0,83–0,89 | **2,85–3,23** |
| profundidade máxima das ilhas < fogInicio, 12 seeds | sim | — | **sim**: máximo 99,98 u (seed 4) < 105 (folga 5,0); seeds 12345/7/2024: 88,4 / 93,5 / 88,9 |
| sol abaixo do horizonte com intensidade > 0,05 (24 h, passo de 1 min) | nunca | ocorria (X de 180° a 360° após as 18h) | **nunca** (teste `Sol_NuncaIluminaPorBaixoDoHorizonte...`) |
| maior variação de intensidade por minuto de jogo | <= 0,02 | 0,2 -> 1,0 em 43 min (≈0,019/min em média, mais íngreme no meio da subida) | **0,01918** (18h56), incluindo a volta 24h -> 0h |
| batches, mesma sessão | ±5 % | 609 (antigo emulado) | **604** (−0,8 %); outra amostra nova: 611 (+0,3 %). Triângulos 897 k vs 892 k |
| Suíte | 100 % | 200/200 | **218/218** (200 + 18 novos) |
| Console | 0 erros | — | 0 erros |

Observações honestas:
- Os limites de legibilidade passam com folga, mas a razão sobe principalmente porque as ilhas deixaram de ser tingidas pela névoa e o **fundo próximo ao anel ficou escuro**
  (a maquete verde-escura, em profundidade parecida com a das ilhas, não recebe névoa). O visual pastel do meio-dia antigo se perdeu; a paleta fica para o Pass D2
  e a escala/composição do fundo para o Pass B. A razão alta às 12h (~2,6) vem em parte desse fundo escuro, não só do topo mais claro.
- O "antes" do aceite é o emulado na mesma sessão (`antigo.cs`). As capturas de `docs/art/pass-a/depois/` (resultado em `antes_legibilidade.json`, razão 0,85–1,30) mostram
  ilhas residuais de um mapa anterior (contadas como fundo pela métrica) e por isso não servem de geometria; os dois "antes" concordam (±0,03).
- Elevação mínima do sol de 26° e crepúsculo de 2 h são escolhas de legibilidade e do limite de 0,02/min; com 22° / 1,5 h o dusk ficou em 1,04–1,5 (abaixo do alvo em 3 de 6 casos testados).

## 3. Diff da cena (`git diff Assets/Scenes/HexPrototipo.unity`)
Só estas linhas (além de whitespace do serializador):
- `RenderSettings`: `m_FogMode 2 -> 1`, `m_FogColor` (0,775/0,721/0,709 -> 0,790/0,747/0,738), `m_LinearFogStart 0 -> 105`, `m_LinearFogEnd 300 -> 230`,
  `m_AmbientSkyColor/Equator/Ground` (derivadas pelo controlador em `timeOfDay = 14.8`).
- Sol (escrito pelo controlador em 14,8 h): `Light.m_Intensity 1,0849 -> 1,4450` e rotação da Directional.
- `DayNightController`: campos novos `elevacaoMinimaDoSol 26`, `duracaoDoCrepusculo 2`, `luaCor`, `luaIntensidade 0,35`, `luaElevacaoMinima 25`, `preenchimentoNoturno`,
  `ambienteDessaturacao 0,85`, `modoDaNevoa 1 (Linear)`, `fogInicio 105`, `fogFim 230`, `misturaDaNevoaComHorizonte 0,6`, `nevoaDessaturacao 0,25`, `nevoaClareamento 0,15`;
  `sunIntensityCurve` (sem a chave 0,22; 0,75 -> 1,5; pontas 0,2).
- Cor de fundo da câmera não mudou (mesmo `timeOfDay`). **Deriva revertida**: `Cenario background` e `Rio de fundo` (`[ExecuteAlways]`) tinham mudado posição/escala e o
  `PrefabInstance` guardava overrides novos; foram devolvidos aos valores do HEAD e a cena salva (na mesma chamada). `timeOfDay` (14,8) e `dayLengthInMinutes` (2) intactos.
- `ProjectSettings/TimeManager.asset` foi reserializado pelo Unity (formato do Fixed Timestep) durante as sessões; **revertido** com `git checkout`.

## 4. Névoa em câmera ortográfica (achados)
- O URP em ortográfica **se comporta como profundidade linear de câmera** (distância ao longo do forward): com névoa Linear magenta e `start = limiar`
  (`fog_ortografica_limiar90.jpg`, `..._130.jpg`) a fronteira é uma linha horizontal na tela, que sobe/desce com o limiar. Com limiar 90 as ilhas da seed 12345 (máximo 88,4)
  ficam sem névoa. `fog_ortografica_teste.json`: a transição acompanha o limiar (seed 7, limiar 110: sem névoa até 111, com névoa a partir de 122); há outliers (bandeira vermelha da maquete,
  amostragem no centro do bounds) que não dependem do limiar e não são névoa.
- Profundidades medidas (cantos dos bounds, 12 seeds, 1920x1080 e 2520x1080 idênticas; `profundidades.json`): ilhas 41,7–100,0 u (máximo por seed 88,4–100,0);
  `Cenario background` 10,7–248,4 u; rio 26,2–185,1 u; nuvens 56,7–139,2 u. Árvores e chão do fundo ficam **na mesma faixa de profundidade das ilhas**: só o que passa de ~105 u
  (alto da tela, mais longe) leva névoa; a perspectiva aérea aparece no alto da imagem, não ao redor do anel. A névoa por profundidade não separa fundo de ilha por plano.
- Nuvens (`CloudController`/`CamadaDeNuvens`/SoftCloud) leem cor do sol/névoa e seguiram coerentes nas capturas (coral no pôr do sol, azuladas à noite); nada alterado.

## 5. Legibilidade por captura (razão = L_topo/L_fundo; Michelson; matiz do topo vs névoa)
Antes (pass-a): `antes_legibilidade.json`; antes emulado: `antes_emulado_legibilidade.json`; depois: `depois_legibilidade.json`. Janelas de 5x5 px; fundo imediato = até 60 px do que cobre.

| Captura | Antes (pass-a) razao | Antes (emulado) razao | Depois: L_topo | L_fundo | razao | Michelson | matiz topo | matiz fog |
|---|---|---|---|---|---|---|---|---|
| s7_h12_1920x1080 | 1.27 | 1.29 | 0.5669 | 0.2212 | **2.56** | 0.439 | 103 | 193 |
| s7_h18_1920x1080 | 0.87 | 0.84 | 0.1581 | 0.0846 | **1.87** | 0.303 | 70 | 12 |
| s7_h22_1920x1080 | 0.88 | 0.86 | 0.0930 | 0.0316 | **2.94** | 0.493 | 147 | 300 |
| s7_h12_2520x1080 | 1.26 | 1.28 | 0.5794 | 0.2335 | **2.48** | 0.425 | 103 | 193 |
| s7_h18_2520x1080 | 0.85 | 0.82 | 0.1631 | 0.0906 | **1.80** | 0.286 | 70 | 12 |
| s7_h22_2520x1080 | 0.86 | 0.83 | 0.0952 | 0.0334 | **2.85** | 0.481 | 147 | 300 |
| s2024_h12_1920x1080 | 1.30 | 1.32 | 0.5737 | 0.2012 | **2.85** | 0.481 | 103 | 193 |
| s2024_h18_1920x1080 | 0.91 | 0.87 | 0.1608 | 0.0758 | **2.12** | 0.359 | 70 | 12 |
| s2024_h22_1920x1080 | 0.92 | 0.89 | 0.0943 | 0.0292 | **3.23** | 0.527 | 147 | 300 |
| s2024_h12_2520x1080 | 1.29 | 1.32 | 0.5843 | 0.2048 | **2.85** | 0.481 | 103 | 193 |
| s2024_h18_2520x1080 | 0.89 | 0.85 | 0.1646 | 0.0806 | **2.04** | 0.343 | 70 | 12 |
| s2024_h22_2520x1080 | 0.90 | 0.86 | 0.0961 | 0.0307 | **3.13** | 0.516 | 147 | 300 |
| s12345_h5_1920x1080 | - | 0.87 | 0.1460 | 0.0608 | **2.40** | 0.412 | 103 | 325 |
| s12345_h6_1920x1080 | - | 0.86 | 0.1866 | 0.0915 | **2.04** | 0.342 | 83 | 335 |
| s12345_h7_1920x1080 | - | 0.95 | 0.2430 | 0.1035 | **2.35** | 0.403 | 83 | 326 |
| s12345_h12_1920x1080 | 1.28 | 1.30 | 0.5811 | 0.2117 | **2.75** | 0.466 | 103 | 193 |
| s12345_h17_1920x1080 | - | 0.93 | 0.2345 | 0.0956 | **2.45** | 0.421 | 72 | 12 |
| s12345_h18_1920x1080 | 0.88 | 0.85 | 0.1637 | 0.0793 | **2.06** | 0.347 | 70 | 12 |
| s12345_h19_1920x1080 | - | 0.85 | 0.1199 | 0.0509 | **2.36** | 0.404 | 94 | 9 |
| s12345_h22_1920x1080 | 0.90 | 0.87 | 0.0958 | 0.0311 | **3.08** | 0.510 | 147 | 300 |
| s12345_h12_2520x1080 | 1.28 | 1.31 | 0.5911 | 0.2146 | **2.75** | 0.467 | 103 | 193 |
| s12345_h18_2520x1080 | 0.87 | 0.84 | 0.1671 | 0.0862 | **1.94** | 0.319 | 70 | 12 |
| s12345_h22_2520x1080 | 0.89 | 0.85 | 0.0973 | 0.0319 | **3.05** | 0.506 | 146 | 300 |

Escala de cinza (teste da Bible, anel identificável nas 3 horas): `docs/art/pass-d1/depois/cinza/` (12/18/22 e transições) e `antes/cinza/`.

## 6. Capturas
- `docs/art/pass-d1/depois/`: matriz 3 seeds x 12/18/22 x 1920x1080 e 2520x1080 + seed 12345 em 05, 06, 07, 17, 19 (1920x1080), `manifest.json` (fog/ambient/sol por captura), `_raw_lighting.json`, `_raw_stats.json`, `cinza/`.
- `docs/art/pass-d1/antes/`: o mesmo conjunto com o controlador antigo emulado (mesma sessão/geometria).
- `docs/art/pass-d1/pontos/`: pontos e máscaras de cobertura por seed x resolução. `docs/art/pass-d1/profundidades.json`.

## 7. Testes
- Novos (EditMode, 17 Logic/SceneStatic): job `1ba65314e3cd42fc84f3b93e4e415d88` = 17/17.
- Suíte completa EditMode (inclui PlayModeSmoke): job `85133a6055634be7a915a856f4f0ca1a` travou em 70/218 (rastreador, como no README) e foi destravado com `clear_stuck_test_job.json`;
  resultado pelo `TestResults.xml` (end-time 20:00:30Z, dentro da janela do job): **218 total / 218 passed / 0 failed**, incluindo os 5 PlayModeSmoke (4 antigos + `NevoaPlayModeTests`: seeds 12345/7/2024/4 em 16:9 e 21:9, máxima 99,98 < 105).
- Nenhum teste existente foi alterado ou enfraquecido; nenhum fixava valores de iluminação.

## 8. Save
`loopia_save.json`: o arquivo que encontrei ao começar tinha SHA-256 `c0a120f1…` (99 moedas), **não** `38433742…` (esse é o backup do revisor em `%TEMP%/loopia_save_reviewer.bak`, 82 moedas, de 16:07;
o arquivo foi alterado às 16:13 por uma execução anterior). Fiz backup do que encontrei e o **restaurei byte a byte** (`c0a120f1…` antes e depois). Para voltar ao `38433742…`, copie o `.bak` do revisor.

## 9. Pendente
- Valores do Editor; nada medido em build. HUD real em 16:9/21:9 não validado.
- Fundo próximo ao anel escuro (Pass B/D2: escala/composição e paleta). Perspectiva aérea limitada ao alto da tela pela profundidade ortográfica.
- `Daynightcontroller.cs` continua no namespace global e em `testes shaders/` (PR próprio).
- A variação máxima de intensidade (0,01918) está colada no limite de 0,02/min: mexer na curva ou no crepúsculo exige refazer o teste.

## 10. Rollback
`git revert` do commit do Pass D1 (script + cena + testes). Só da cena: abrir um mapa vazio, `git checkout -- Assets/Scenes/HexPrototipo.unity`, reabrir. Só do visual: no Inspector,
`modoDaNevoa = Exponential`, `misturaDaNevoaComHorizonte = 0`, `ambienteDessaturacao = 0`, `preenchimentoNoturno = preto` reproduzem a névoa/ambiente antigos (o sol/lua novos ficam).

## Revisao de qualidade (analista, 2026-11-14)
- **Reexecucao independente:** Logic+SceneStatic 213/213 (job `76caba5c7231466ab1c1d2b9409429ba`); PlayModeSmoke 5/5
  (`TestResults.xml` 2026-11-14 20:07:52Z) — total 218/218. Console 0 erros; cena sem alteracoes pendentes; save inalterado
  durante a reexecucao.
- **Codigo:** controlador continua dono unico de `RenderSettings`; uma so luz serve de sol e lua (nunca abaixo do horizonte);
  funcoes puras (`AvaliarLuz`, `FatorDeDia`, `CorDaNevoa`) cobertas por teste. Conversao cp1252 -> UTF-8 corrige os tooltips.
- **Diff da cena conferido:** RenderSettings (fog/ambient) e luz escritos pelo controlador em `timeOfDay` 14,8, campos novos do
  controlador e a curva de intensidade (chaves 0,22 e 0,85 removidas). Nada mais.
- **Capturas:** um anel por imagem, ilhas sem nevoa, fundo com perspectiva aerea na parte alta da tela.
- **Achado:** as capturas `docs/art/pass-a/depois/` (seeds 7 e 2024) mostram ilhas residuais de um mapa anterior. As
  medidas do Pass A nao sao afetadas (usam so as ilhas do `HexWorld`), e as imagens foram refeitas na branch do Pass A (commit `c9d248d`).
- **Ressalvas:** ao meio-dia o fundo perto do anel fica verde-escuro (sem nevoa, mesma profundidade das ilhas) — paleta e
  composicao ficam para B/D2. Passo maximo de intensidade 0,01918 por minuto de jogo, perto do limite 0,02.
- **Save:** o arquivo mudou entre 16:09 e 16:13 fora das execucoes revisadas (`38433742...` -> `c0a120f1...`); origem nao
  determinada. Copia do estado anterior: `%TEMP%/loopia_save_reviewer.bak`.

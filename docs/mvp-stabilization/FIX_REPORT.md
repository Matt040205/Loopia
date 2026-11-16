# Relatório de correções — Sprint de estabilização do MVP

Branch `fix/mvp-stabilization` (Draft PR #12 → `integration/mvp-20261117`). Cada item: estado inicial, reprodução,
causa, solução, commit, teste, resultado, captura (quando visual) e risco residual. Testes e totais em
`TEST_RESULTS.md`; partidas na build em `BUILD_VALIDATION.md`; o que ficou em aberto em `REMAINING_ISSUES.md`.

## LEG-1 · Elementos das ilhas minúsculos e pixelizados em telas grandes
- **Estado inicial:** captura do responsável (janela 1280×720 num monitor 1920×1080, anel de 36): topo da ilha ~52 px.
- **Reprodução:** Editor, mesma semente/anel em 1280×720 e 1920×1080 (`docs/art/sprint-4-legibilidade/antes/`).
- **Causa:** resolução gravada no registro (janela 1280×720, não redimensionável); anel de 36 casas nunca validado;
  nenhum anti-aliasing.
- **Solução:** zoom do jogador (roda + arraste), resolução nativa no build, HUD escalável acima de 1080p, MSAA 4×,
  anel de até 30. Detalhes em `docs/sprints/SPRINT-4.md`.
- **Commits:** `9d7a759`, `6871e25`, `dfb9dec`, `684f122`, `6ee7552`, `98480cb`, `0340291`.
- **Teste:** 26 testes novos; suíte 396/396 na época.
- **Resultado:** topo da ilha 109–125 px sem zoom (1080p, anel ≤ 30), até ~300 px com zoom.
- **Captura:** `docs/art/sprint-4-legibilidade/comparacao_antes_depois_zoom.jpg`, `recorte_msaa_1x_4x.png`.
- **Risco residual:** custo do MSAA em GPU fraca não medido (a medição no Editor derrubou a Unity, ver abaixo);
  `ResolucaoDaTela` só se comprova num build.

## P0-1 · Vitória, derrota e retorno ao acampamento na build
- **Estado inicial:** vitória e clique real no botão nunca comprovados fora do Editor.
- **Reprodução:** build de fallback com mouse real (`BUILD_VALIDATION.md` §1).
- **Causa:** nenhum defeito no jogo: o relatório do MVP usou clique por mensagem de janela, que o Input System
  ignora. Com mouse real o botão funciona.
- **Resultado:** derrota → resultado → botão → acampamento → save → reabertura comprovados na build de fallback;
  **vitória** → resultado → botão → acampamento → save (vitória registrada uma vez) → reabertura comprovados na build
  Release `7bf6492` com mouse real (`BUILD_VALIDATION.md` §2.2).
- **Achado lateral corrigido:** a tela de derrota mostrou 43 moedas e o save ganhou 44. Causa: na derrota o corpo do
  Lucca termina o trecho do NavMesh, entra na casa seguinte e coleta uma moeda depois do resumo; `GanharMoedas` não
  conferia se ele estava vivo. Solução: morto não coleta (`LuccaStatus.GanharMoedas`), teste Logic
  `Morto_NaoColetaMoedas`. Commit `31b7cdf`. A vitória na Release mostrou o mesmo defeito com o Lucca vivo (tela 27,
  save +28): `a7a5c56` encerra as recompensas na vitória e na derrota (`LuccaStatus.EncerrarRecompensas`, chamado pelo
  `LoopDirector` antes da tela de resultado); testes Logic e PlayModeSmoke.

## P0-2 · Save do jogador compartilhado por Editor, build e build de QA
- **Estado inicial:** um único `loopia_save.json`; qualquer Play no Editor ou build altera o progresso do responsável.
  Nesta sessão: capturas em Play (209 → 210), uma execução curta da build (209 → 214) e o processo do Editor que caiu,
  que gravou o save ao terminar (209 → 211). Todos restaurados para `3d27854e…`.
- **Proteção dos testes (existente, não reimplementada):** verificada com teste novo — com outra worktree no meio de
  uma suíte (trava com outro `dataPath` e processo vivo), `SaveIsolado.Backup()` recusa antes de tocar em arquivos e o
  backup desta suíte não muda; um segundo `Backup()` não sobrescreve o original. Commit `e009508`.
- **Solução para QA:** `-loopia-perfil <nome>` grava em `loopia_save_<nome>.json` (só letras, números, `-`, `_`).
  Sem o argumento, o arquivo de sempre: a versão final não muda de lugar. Commit `3bdd321`.
- **Teste:** 7 testes Logic com pastas temporárias (`SaveDoJogadorPerfilTests`), 2 de concorrência.
- **Risco residual:** o Play Mode do Editor continua gravando no save do Editor (agora em `DefaultCompany/Loopia`);
  validações no Editor precisam de backup/restauração (README das ferramentas atualizado).

## P0-3 · Nome do produto `GameJam13-06`
- **Estado inicial:** janela e pasta de dados com o nome do jam.
- **Causa da cautela:** `productName` compõe `Application.persistentDataPath`; renomear sem migração "zeraria" o save.
- **Solução:** `productName: Loopia` (empresa continua `DefaultCompany`). Na primeira abertura, se não há save na
  pasta nova e há em `...\DefaultCompany\GameJam13-06\`, ele é **copiado** (o original fica intacto; um save novo
  nunca é sobrescrito). Perfis de QA não migram. Commits `3bdd321`, `c9b6364`.
- **Teste:** Logic (instalação antiga, nova, save novo já existente, caminho antigo) e SceneStatic (nome/empresa).
- **Validação em build:** instalação antiga e nova — ver `BUILD_VALIDATION.md` (build nova).
- **Risco residual:** preferências de tela do registro também mudam de chave (`HKCU\Software\DefaultCompany\Loopia`),
  o que é desejado (a resolução gravada era a causa do LEG-1).

## P0-4 · Cenas legadas no build padrão
- **Estado inicial:** Menu, Testes, GameOver e Win habilitadas no `EditorBuildSettings`.
- **Solução:** desabilitadas pelo Editor (continuam no projeto e na lista). Commit `c9b6364`.
- **Teste:** SceneStatic `ConfiguracaoDeBuildTests` (só Acampamento e HexPrototipo, nessa ordem; legadas existem e
  estão fora).

## P0-5 · Project Settings e URP regravados pelo build
- **Estado inicial:** 6 arquivos sempre modificados depois de cada build.
- **Análise:** todos são reserialização automática coerente com a configuração aprovada: pré-filtragem de shader do
  `PC_RPAsset` (remove variantes de SSAO, já desligado no D2), runtime settings do URP, batching padrão do Standalone,
  `TimeManager` no formato racional do Unity 6 (mesmo 0,02 s), `_MainTex` do `Dressing_Vegetacao` espelhando o
  `_BaseMap`. `QualitySettings`: versão 5 e `antiAliasing` 4 (o URP espelha o MSAA 4×).
- **Solução:** versionados para encerrar o ruído recorrente; `.vscode/settings.json` (nome da solução da worktree)
  fica fora. Opaque Texture e SSAO continuam desligados (teste `RenderDoPCTests`). Commits `b0bbdf1`, `29a6059`.

## P1-1 · Faixa rosa na borda inferior
- **Estado inicial:** faixa ondulada rosa embaixo da tela no fim da tarde (relatório do MVP `build_02` e captura do
  responsável, ambos 720p com anel de 36).
- **Reprodução:** Editor, 4:3 às 18 h, semente 27 (anel 30): faixa salmão ondulada (`evidencias/faixa_rosa/antes_*`).
- **Causa (medida):** o chão do `FundoDePaisagem` era fixo em ±64 × ±52 u; a borda de baixo da tela cai no chão a
  ~2 × orthoSize da câmera. Em 40 sementes: com anel de até 30, 4:3 passava da borda em 11/40 (folga −2,9 u), 16:10
  ficava a 6,1 u; com anel de 36, 4:3 em 20/40. O relevo da borda (~3 u) come ~5 u e deixa a faixa ondulada. Além da
  borda aparece a cor de fundo da câmera, que o `DayNightController` pinta de rosa no fim da tarde.
- **Solução localizada:** `bordaDeChao = 24` estende só o terreno (e o rio) em volta do vale; árvores, pedras e
  lagoas continuam na área aprovada. Névoa, luz, câmera e cena intactas. Commit `1780146`.
- **Teste:** SceneStatic `FundoDePaisagemCoberturaTests` (24 anéis × 16:9/16:10/4:3/21:9 com os números da cena:
  folga ≥ 6 u; e a caracterização de que só o vale não cobria o 4:3).
- **Resultado:** faixa eliminada no 4:3; 16:9 (12/18/22 h) e 21:9 (18 h) sem mudança perceptível.
- **Captura:** `evidencias/faixa_rosa/comparacao_4x3_fim_da_tarde.jpg` e pares `antes_*`/`depois_*`.
- **Custo:** +6,7 mil triângulos (416 mil no fundo), geração em ~60 ms; sem draw call novo (mesma malha de terreno).
- **Risco residual:** telas mais altas que 4:3 (5:4) ou um anel muito maior voltariam a precisar de borda maior; o
  teste cobre as quatro proporções comuns.

## P1-2 · Painel de depuração IMGUI na apresentação
- **Estado inicial:** o painel mostrava sempre passos, XP, total de moedas do save, inimigos vivos e escala.
- **Inventário de painéis:** painel do `LoopDirector` (depuração + jogo); barra de cartas, descrição e pendentes
  (`CartasUI`, jogo); barra do chefe (jogo); dica do zoom (jogo, some no 1º uso); destaques do `HexPointer` (hexágono
  sob o mouse = jogo; prévia do caminho só com controle manual, desligado na cena).
- **Solução:** o painel de jogo mostra volta, PV, flechas, nível e moedas da jornada (5 linhas, painel mais baixo);
  os dados técnicos aparecem no Editor e em Development Build, e o F3 alterna em qualquer build. Commit `cf5e968`.
- **Teste:** Logic `PainelDoJogoTests` (4).
- **Risco residual:** a marca "Development Build" do Unity só some numa build Release (a de apresentação).

## P1-3 · Balanceamento do Morcego Ancião
- **Estado inicial:** sem carta de dano o chefe vence; na build de fallback nenhuma carta de dano saiu em 4 níveis.
- **Investigação (antes de mudar número):**
  - Baralho: 14 cartas sorteadas com probabilidade igual, 2 de dano (Torre, Área de Treino) e 2 de munição (Flechas,
    Oficina). ~4 níveis antes do chefe → P(nenhuma carta de dano) = (12/14)⁴ ≈ **54%**.
  - Mão inicial garantida: Mola, Coração, **Moedas**, Flechas. Moedas só dá moeda: nenhum efeito no confronto.
  - Chefe: 732 PV efetivos (500 × 1,1⁴), 21 flechas de 35 (20 com Torre, 16 com Torre + Área vizinha).
  - Fila de recompensas G2: funciona; com a mão parada os 4 níveis ficam pendentes (0 decisões novas).
  - Duração da jornada: ~2,5 min (anel 24–30), abaixo da meta de 5–7 min do GDD — decisão de design aberta, não mexida.
- **Medição** (`tools/balanceamento/medir_chefe.py`, Editor, timeScale 4, morcegos comuns ligados, mapa com semente
  fixa 1/2/27; spawn, Revoada e compra **sem** semente: cada linha é uma amostra, sem conclusão estatística forte):

  | Cenário | Vitórias | Jornada | Chefe | Flechas no chefe (+Revoada) | Sem flechas | Dano no chefe | Pendentes na chegada |
  |---|---|---|---|---|---|---|---|
  | 1 sem dano (Mola, Coração, Moedas, Flechas) | 0/3 | 156 s | 27,8 s | 15,3 (+6,3) | 10,4 s | 55 | 0–1 |
  | 2 Torre | 1/3 | 153 s | 24,2 s | 15,7 (+3,3) | 8,7 s | 50 | 0–1 |
  | 3 Flechas + Torre | 2/3 | 155 s | 26,8 s | 20,0 (+2,7) | 8,6 s | 41 | 1 |
  | 4 sobrevivência (Fonte, Santuário, Oficina) + Torre + Área vizinha | 2/3 | 151 s | 23,2 s | 15,0 (+5,0) | 6,9 s | 47 | 1 |
  | 5 mão parada (ninguém joga) | 0/3 | 149 s | 21,4 s | 10,0 (+5,0) | 9,0 s | 50 | 4 |

  Dados brutos: `evidencias/balanceamento/antes.json` (por volta: duração, flechas, dano, abates, PV).
- **Leitura:** o que decide é ter carta de dano e flechas no 1º minuto do confronto: as vitórias acontecem com 20–21
  flechas em ~25 s; o Lucca fica ~1/3 do confronto sem flechas e morre em 20–35 s. A vida do chefe não é o problema
  isolado, e com dano o resultado ainda não é garantido.
- **Solução (menor mudança, sem sistema novo):** Moedas → **Torre** na mão inicial garantida (Moedas continua no
  baralho). Toda jornada começa com uma ferramenta de dano que o jogador escolhe onde pôr (vizinhança com a Área de
  Treino rende +15%). Vida do chefe, escala ×1,1, munição e Revoada intocadas.
- **Depois** (`evidencias/balanceamento/depois.json`, mesmas sementes): mão inicial nova (Mola, Coração, Torre,
  Flechas) **2/3** — vitórias nas sementes 1 e 27 (20–21 flechas no chefe), derrota na 2 com o chefe a 48 PV. Contra
  0/3 do cenário 1 (mão antiga, sem dano). A semente 2 (anel de 30) perdeu em todos os cenários: o confronto continua
  exigindo preparo e não há vitória automática.
- **Commit:** `b8881e1` (cena pelo Editor + teste SceneStatic `MaoInicial_TemUmaCartaDeDano_MoedasContinuaNoBaralho`).
- **Erro de medição corrigido no caminho:** na 1ª rodada do cenário 4 as cartas de sobrevivência foram postas na
  casa da Torre e a substituíram (regra de troca do jogo); o driver passou a pular casas ocupadas e o cenário foi
  refeito. Os outros cenários foram conferidos (nenhuma casa repetida).
- **Risco residual:** amostra pequena e RNG sem semente; partida mais curta que a meta do GDD; a vitória na build
  com a mão nova ainda precisa de uma partida real (`BUILD_VALIDATION.md` §2.2).

## P2-1 · Mira do Mirante para alvos atrás
- **Antes:** o tronco gira no máximo 65°. No Mirante o Lucca atira em qualquer direção e, com o alvo atrás, o arco
  soltava **98–100°** fora do alvo (medido na soltura; a flecha segue o alvo assim mesmo).
- **Solução:** o que passa de 80% do limite do tronco gira o **corpo visual inteiro** (o objeto do Animator) só durante
  o tiro e fora do pulo: até 120° parado e até 30° correndo, proporcional à velocidade da locomoção. A raiz do
  NavMeshAgent não gira (o teste confere), o tronco continua ≤ 65° (sem torção de 180°) e o tiro reservado do Codex não
  mudou (`Mirante_TiroReservado_…` passa).
- **Primeira versão corrigida no caminho:** em `2b9ba2d` o corpo girava até 120° mesmo correndo. A medição mostrou as
  pernas correndo para um lado e o corpo indo para outro: deslize mediano de **4,4 u/s** nos tiros a mais de 52°,
  contra 1,8 sem o giro. `478751d` limitou o giro a 30° correndo.
- **Resultado:**
  - Mirante, Lucca parado, alvo atrás: erro na soltura **14–15°** (antes 98,5–100,5°; 4 execuções).
  - Correndo, alvo grande no miolo como o chefe (medição, n pequeno): de lado (52–100°) mediana **3°** (sem o giro, 14°;
    n 6/5); atrás (> 100°) mediana **29°**, máximo 55° (sem o giro, 40°, máximo 82°; n 8/8). Custo: deslize mediano
    nesses tiros 1,56 u/s contra 0,97 sem o giro (o giro sem limite dava 2,35).
- **Limite que fica:** correndo, alvo bem atrás (> ~95°) segue com erro grande. Resolver de vez exige clipes de corrida
  lateral/de costas (Blender) ou IK de braço, fora do escopo do P2.
- **Evidência:** `evidencias/pes_e_mira/mira_atras_antes_so_tronco.png` × `mira_atras_depois_corpo_vira.png` (mesmo
  cenário, um quadro depois da soltura); `apoio_tiro_por_giro_do_corpo.txt`.
- **Commits:** `2b9ba2d`, `478751d`. Testes: `LuccaMiraTests` (Logic), `Mirante_AlvoAtras_OArcoApontaParaOAlvoNaSoltura`
  (PlayModeSmoke).

## P2-2 · Deslizamento dos pés e contato do arco
- **Medição nova** (`ApoioDosPesMedicaoTests`, PlayModeMedicao, fora da suíte padrão): velocidade horizontal do pé que
  está no chão (o mais baixo, a menos de 3,5 cm da sola calibrada parado), com o corredor real andando, separada por
  contexto. O critério pega também quadros do pé em balanço rente ao chão: o P95 fica perto de 5 u/s até na reta, então
  a comparação é pela **mediana**. Não é a métrica da Sprint 2C.2 (só o meio do apoio de cada clipe, P95 2,39 u/s).
- **Diagnóstico (só locomoção, sem alvos):** reta 0,75 u/s; pouso 0,92; **curva até 0,3 s antes do pulo 3,05** (22% das
  amostras); curva longe de pulo 4,86 (4%). Os quadros antes do pulo mostram a causa: o caminho vira para o ponto do
  link e a raiz do NavMeshAgent fica até **~52°** fora da direção em que anda, convergindo em ~0,3 s. A corrida empurra
  os pés para a frente do corpo enquanto ele anda de lado. O `angularSpeed` do agente (360, 720 e 1440) não muda nada.
  A sincronia Blend Tree × clipes não é a causa (a reta fica em 0,75).
- **Solução (localizada, sem Blender):** o corpo visual vira para o deslocamento real da raiz (máximo 75°, suavização
  35 ms), também no pulo; teleporte não conta. Raiz, combate (`EstaAFrente`) e navegação não mudam.
- **Resultado (mesma métrica):** antes do pulo **3,05 → 0,76 u/s**; todas as amostras **1,27 → 0,86 u/s (−32%)**; pouso
  igual (~0,95). O visual alinha em ~50 ms (−25° → −1°, depois 3–5°) enquanto a raiz ainda está a ~50°. A curva longe
  de pulo (4% das amostras) piorou um pouco (4,86 → 5,61): o pé de apoio gira junto com o corpo. Limitar a velocidade do
  giro (600 e 400°/s) foi testado: piorou a mediana antes do pulo (1,58 e 1,73) sem ganho claro, e não ficou.
- **Contato do arco com a túnica/saia:** não mexido. Vem da mistura de tiro autorada no Blender (registrada na 2C.2) e
  só se corrige editando o clipe, com backup. As mudanças desta sprint não pioram: o giro do corpo tira carga do
  tronco, que continua ≤ 65°.
- **Evidência:** `evidencias/pes_e_mira/apoio_locomocao_marcha_0_e_75.txt` (resumo e quadros antes de dois pulos com o
  giro desligado e ligado).
- **Commit:** `c615e42`. Teste: `GiroDaMarcha_OlhaParaAVelocidade_ComLimite_EZeroDevagar` (Logic).

## P2-3 · Fim de partida sem pose
- **Antes:** o Lucca ficava em Idle na vitória e na derrota.
- **Solução (sem clipe novo, sem mexer no rig):** na vitória o Spring Jump toca no lugar e para com o braço erguido
  (0,78 do clipe); na derrota o Lucca afunda no agachamento do Jump e fica parado. Só o parâmetro `RitmoPulo` (velocidade
  desses estados); a raiz continua com o NavMeshAgent e a camada do arco vai a 0. A tela de resultado aparece com fade de
  0,35 s em tempo real, ativa e clicável desde o primeiro quadro; troca de cena e save inalterados.
- **Evidência:** `evidencias/fim_de_partida/poses_do_fim.png`. Teste: os PlayModeSmoke de vitória e derrota do chefe
  conferem a pose parada, o estado, `RitmoPulo` 0 e a camada do arco em 0.
- **Commit:** `295f7d0`.

## Incidente · Queda da Unity numa medição
Durante o LEG-1, uma medição de custo do MSAA com dezenas de `Camera.Render()` num único frame do Editor estourou a
memória D3D12 (`8007000e`) e fechou a Unity. Nada se perdeu; o save foi restaurado. Método descartado (registrado
na memória do agente e no `SPRINT-4.md`).

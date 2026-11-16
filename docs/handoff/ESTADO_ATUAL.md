# Estado atual — 2026-11-16

## Sprint de estabilização do MVP — **comece por aqui**
- Branch `fix/mvp-stabilization` (base `integration/mvp-20261117` @ `f49cee1` + os 7 commits de legibilidade),
  Draft PR #12 → `integration/mvp-20261117`. Nada na `main`. Documentos em `docs/mvp-stabilization/`
  (`ISSUE_AUDIT`, `FIX_REPORT`, `TEST_RESULTS`, `BUILD_VALIDATION`, `REMAINING_ISSUES`).
- Feito: perfil de save para QA (`-loopia-perfil <nome>`), produto **Loopia** com migração do save de `GameJam13-06`
  (copia, nunca apaga), cenas legadas fora do build, settings que o build regravava versionados, faixa rosa corrigida
  (borda de chão no `FundoDePaisagem`), painel de jogo enxuto (dados técnicos com F3 / Development), moeda pós-morte,
  mão inicial com Torre no lugar de Moedas (medido: 0/3 → 2/3 vitórias).
- Testes: **427/427** depois do Bloco C (Logic+SceneStatic 398, job `f9bde7ad…`; PlayModeSmoke 25, job `606df17d…`;
  PlayModeMedicao 4, job `9e92549d…`). Antes do Bloco C: 417/417.
- Builds mais novas (`6dd6206`, com o Bloco C): Release `Documents\Loopia_Builds\mvp-estabilizacao-6dd6206-release\`
  (apresentação) e Development `...-6dd6206-dev\` (QA). Na Release: **vitória e derrota com mouse real**, botão,
  acampamento, save de QA (+25 e +35, iguais às telas) e reabertura, 0 exceções (`BUILD_VALIDATION.md` §4.1). As
  `a7a5c56` e `7bf6492` ficam como versões anteriores; fallback `mvp-20261117-dev-55442a5` intacto.
- Validado na build com mouse real: **vitória** → resultado → botão → acampamento → save (1 vitória) → reabertura
  (Release `7bf6492`, perfil QA); derrota → retorno (fallback); instalação nova e antiga com migração (Release). A
  build final só teve abertura/fechamento (acesso do computer use negado); difere só pela correção da moeda.
- Save do responsável: em `...\DefaultCompany\Loopia\loopia_save.json` (migrado). **Atenção:** outras frentes
  (`loopia-cards`, `loopia-hud-preview`) gravaram nele e no antigo depois das validações (210 e 212 moedas); não
  restaurado sem o responsável decidir. Ao validar no Editor ou na build, use backup ou `-loopia-perfil qa`.
- **Bloco C (P2) feito** em 16/11: fim de partida com pose (`295f7d0`); mira do Mirante com o corpo visual girando o
  excesso além do tronco, 120° parado e 30° correndo (`2b9ba2d`, `478751d`; erro na soltura ~99° → ~15°); pés: o
  corpo visual olha para onde a raiz anda, a causa medida era a raiz do agente atrasada antes de cada pulo (`c615e42`;
  3,05 → 0,76 u/s antes do pulo, mediana). Arco × túnica e corrida com alvo bem atrás ficaram pendentes (Blender).
  Medição reprodutível: `ApoioDosPesMedicaoTests` (PlayModeMedicao); detalhes em `FIX_REPORT.md` P2-1/2/3.
- Ferramentas: `tools/balanceamento/medir_chefe.py` (cenários do chefe), `tools/build/construir.cs` (build pelo
  bridge). **Nunca** dezenas de `Camera.Render()` num `execute_code` (derrubou a Unity).

## Sprint 4, passo 1 — Legibilidade em telas grandes
- Branch `sprint-4/legibilidade-tela` (base `integration/mvp-20261117` @ `f49cee1`), worktree `loopia-mvp`.
  Registro completo, backlog da sprint (problemas do playtest + §9 do relatório do MVP) e medidas:
  [SPRINT-4.md](../sprints/SPRINT-4.md).
- Feito: zoom do jogador (roda + arraste, padrão = anel inteiro), nuvens presas ao mundo no zoom, resolução nativa no
  build (`ResolucaoDaTela`, só no player), HUD IMGUI escalando acima de 1080p, MSAA 4× no PC, janela redimensionável,
  anel de no máximo 30 casas (`raioMaximo` 5). Topo da ilha no caso do playtest: ~52 px → 109–125 px sem zoom.
- Testes: **396/396** (Logic + SceneStatic 371/371 job `eb1ff800…`; PlayModeSmoke + PlayModeMedicao 25/25,
  `TestResults.xml` 22:29Z). Console limpo (só o `LogAssert` esperado). Save restaurado (SHA `3d27854e…`).
- **Pendente:** custo do MSAA e `ResolucaoDaTela` só se validam num build novo. A Unity caiu numa medição minha de
  custo do MSAA (muitos `Camera.Render()` num frame, sem memória D3D12); nada se perdeu. Não repetir esse método.
- Arquivos regravados pelo build (`PC_RPAsset` pré-filtragem, `UniversalRenderPipelineGlobalSettings`,
  `GraphicsSettings`, batching em `ProjectSettings`, `TimeManager`) continuam fora dos commits; do `PC_RPAsset` e do
  `ProjectSettings` só entram `m_MSAA` e `resizableWindow`.

## Integração do MVP (base para o polimento) — **comece por aqui**
- Branch `integration/mvp-20261117`, worktree `C:\Users\Sitr3n\Documents\Github\loopia-mvp`. É a única base com tudo
  junto: G1 + arte (PR #8), Lucca do Codex (PR #10), G2 (PR #9), Mirante ligado ao combate e as 3 cartas G2 no baralho.
- Editor de trabalho: o Unity aberto em `loopia-mvp`. As worktrees `loopia`, `loopia-lucca`, `loopia-gameplay` e
  `loopia-g2` ficam como estão (histórico das frentes); não abrir Editors nelas para trabalho novo.
- Testes: 370/370 (Logic 309, SceneStatic 37, PlayModeSmoke 23, PlayModeMedicao 1). Build Windows de desenvolvimento
  (fallback): `C:\Users\Sitr3n\Documents\Loopia_Builds\mvp-20261117-dev-55442a5\Loopia.exe`.
- Relatório, partida manual, build e problemas conhecidos: [MVP_INTEGRATION_REPORT.md](../integration/MVP_INTEGRATION_REPORT.md).
- Suítes de teste: uma worktree por vez (trava `loopia_tests_save.lock` em `%TEMP%`); o save é o mesmo para Editor e build.

## Sprint 2C.2 — Locomoção na branch Lucca

- `feature/lucca-locomotion-2c2` parte da integração `89dc640` na worktree `loopia-lucca`.
- A cena usa Run como movimento principal e Walk para baixa velocidade. O prefab combina Idle/Walk/Run pela velocidade medida, mantém a mira da camada superior e sincroniza a aterrissagem ao NavMesh Link.
- Estado e reprodução em [LOCOMOTION_REWORK.md](../character-integration/LOCOMOTION_REWORK.md). A cena de gameplay continua sendo `Assets/Scenes/HexPrototipo.unity`.

## Integração de Lucca nesta branch

- `feature/lucca-character-integration` parte de `sprint-3/gameplay-cena` em `ee46799` (PR #8). A worktree separada é `loopia-lucca`.
- `HexPrototipo.unity` usa o prefab `LuccaVisual` com Avatar Humanoid, materiais URP, Animator sem Root Motion, arco equipado e os seis clipes da Sprint 2C. O GameObject lógico de Lucca continua com seus componentes e referências.
- `LuccaCombate` reserva o tiro e só cria o projétil na soltura visual ou pelo fallback. `HexPlayer` continua dono da trajetória e informa tipo/tempo de salto ao bridge.
- Reprodução, fontes, resultados e limitações em [docs/character-integration](../character-integration/UNITY_INTEGRATION.md). Registre novas execuções em [TEST_RESULTS.md](../character-integration/TEST_RESULTS.md).
- O diff foi preparado sobre o PR #8 para revisão. Consulte [KNOWN_ISSUES.md](../character-integration/KNOWN_ISSUES.md) para a passada em velocidade máxima e os limites de mira.

## Onde estava a pilha anterior
- Sprint / passo: Sprint 1 (visual) — **Pass D2 completo, menos build** (luz + render/materiais; aguardando revisão)
- Branch: `sprint-1/pass-d2-render`, empilhada: `sprint-0/preparacao-tecnica` → `sprint-1/auditoria-visual` →
  `sprint-1/pass-a-escala-camera` (PR #1) → `sprint-1/pass-d1-nevoa` (PR #2) → `sprint-1/pass-b-composicao` (PR #3) →
  `sprint-1/pass-c-dressing` (PR #4) → `sprint-1/pass-d2-luz` (PR #5) → `sprint-1/pass-d2-render` (PR do D2 render).
  Nada disso está na `main`.
- Status: aguardando revisão/merge do responsável; próximo passe só após aprovação

## O que foi feito no D2 render (sem build, pedido do responsável)
- `PC_RPAsset`: Opaque Texture desligada (nenhum shader da cena lê a cor da cena). `PC_Renderer`: SSAO desligado (efeito
  imperceptível; o prepass DepthNormals redesenhava a geometria). Feito pelo Editor; a Unity atualizou o asset do renderer
  (URP 17, versão 2 → 3).
- `MateriaisCompartilhados`: inimigos sem prefab e flechas reaproveitam um material por cor (antes: `new Material` por
  spawn/tiro, nunca destruído). Medido: 1 391 materiais estáveis em 140 s de combate acelerado.
- Evidências: suíte 263/263 (Logic+SceneStatic job `577ff505…`, PlayModeSmoke 7/7 13:51Z); legibilidade igual; batches
  531 → 287, triângulos 912k → 451k (Editor). Relatório: `docs/art/pass-d2-render/RELATORIO_PASS_D2_RENDER.md`.
- Único item do D2 ainda pendente: desempenho em build.

## O que foi feito no Pass D2 (escopo: só luz, pedido do responsável)
- `DayNightController.elevacaoMaximaDoSol` (padrão 90 = D1); cena com 58°: o sol não passa mais pelo zênite (volume e relevo
  leem de dia), nascer/pôr/noite idênticos ao D1. Diff de cena: 1 linha.
- Avaliados e rejeitados com números: sombras (+57 % batches, quase invisíveis sem chão), tonemapping Neutral (lava a cor) e
  ACES (noite quase preta). Relatório: `docs/art/pass-d2/RELATORIO_PASS_D2.md`.
- Evidências: suíte 259/259 (Logic+SceneStatic job `75d42646…`, PlayModeSmoke 7/7 13:38Z); contraste 12h 2,34–2,56
  (antes 2,59–2,81), 18h e 22h iguais; batches 527.
- Opaque Texture/SSAO e materiais por spawn: feitos no D2 render (acima); desempenho em build segue pendente.
- `Assets/_Recovery/0.unity` (sem versionar): cópia de recuperação da Unity, idêntica à `HexPrototipo` salva; pode apagar.

## O que foi feito no Pass C
- C1: `PlataformaDef.marcador`/`ocultarDecoracao`; `Plataforma` instancia o marcador como filho da ilha (sobe com o relevo,
  fora do bake). Marcadores das 7 cartas sem modelo em `Assets/Modelos/Marcadores/`, gerados por
  `Assets/Editor/MarcadoresDeCarta.cs` (menu Loopia/Pass C). Cartas do caminho: nada alto no centro nem do lado da câmera.
- C2/C3: segunda camada `VegetacaoBaixa` no `DecoracaoDeIlha` (sorteio próprio; decoração principal idêntica), configurada no
  perfil (`vegetacaoBaixa`, 3–8 por ilha, mistura ±60 %, teto 12 por ilha); static batch em runtime.
- Evidências: suíte 253/253 (Logic+SceneStatic job `19b19fd7…`, PlayModeSmoke 7/7 05:38Z); legibilidade igual ao Pass B;
  batches 421 → 530 (+26 %, ainda < 615 de antes do Pass B). Relatório: `docs/art/pass-c/RELATORIO_PASS_C.md`.
- Pendente do C: build; oclusão do Lucca real pelas moitas do lado da câmera (não medida).

## O que foi feito no Pass B
- Fundo novo `FundoDePaisagem` (`Assets/Scripts/Hex/FundoDePaisagem.cs`): vale em miniatura gerado ao carregar a cena
  (malhas em memória, determinístico, fora de `Mundo`, sem collider/sombra) com KayKit Forest Nature Pack (CC0) em
  `Assets/ThirdParty/KayKit_ForestNature_Free/`. Materiais/atlas em `Assets/Modelos/Paisagem/`, gerados por
  `Assets/Editor/MateriaisDaPaisagem.cs` (menu Loopia/Paisagem). Versão aprovada: prévia v6 do responsável.
- Cena (via Editor): `Fundo de paisagem` adicionado; removidos `Cenario background`, `Rio de fundo` e as sobras inativas
  (`QuadCloud`×6, `Plane`, `Cube`, `IlhaFlutuante`).
- `tools/visual-baseline/cs/pontos.cs`: `Fundo de paisagem` conta como fundo na métrica de legibilidade.
- `.gitignore`: `Assets/Previa/` (prévias locais v5/v6 com ~80 MB de malhas geradas) não é versionado.

## Evidências
- Suíte 230/230: Logic+SceneStatic 224/224 (job `023d3911…`), PlayModeSmoke 6/6 (`TestResults.xml` 05:01:57Z). 12 testes novos.
- Legibilidade (antes → depois): 12h 2,48–2,85 → 2,59–2,80; 18h 1,80–2,12 → 1,77–2,01; 22h 2,85–3,23 → 2,85–3,01;
  saturação do fundo 0,45–0,54 → 0,32–0,41.
- Editor (mesma sessão): batches 615 → 416; tris do frame 906k → 873k; shadow casters 4 → 0. Relatório:
  `docs/art/pass-b/RELATORIO_PASS_B.md`.

## Pendente / não verificado
- Build: tempo de geração no player, memória das malhas legíveis, fps. HUD em resolução real (faixa de baixo).
- Relevo do fundo não lê ao meio-dia (sol a 90°): decisão de luz para o D2.
- Scripts `CenarioContinuo`/`FundoContinuo`/`RioDeFundo` e prefab `Cenario background.prefab` (com o Volume latente)
  ficaram sem uso: remover em PR de limpeza.
- Save do jogador: mudança entre 16:09 e 16:13 de 2026-11-14 ainda não confirmada pelo responsável
  (`%TEMP%/loopia_save_reviewer.bak`).

## Fora do repositório (só na máquina do responsável)
- Pesquisa de licenças dos 12 pacotes, laboratório e relatórios das prévias: `C:\Users\Sitr3n\Documents\Loopia_AssetResearch\`.
- Prévias locais: `Assets/Previa/FundoKayKit/HexPrototipo_FundoKayKit_v5.unity` (v5 congelada, "mapa anterior") e `_v6.unity`.

## Decisões que dependem do humano
- Revisar/mergear a pilha (Sprint 0 → auditoria → PR #1 → #2 → #3 → #4 → #5 → D2 render)
- Desempenho em build (último item do D2) ou outro foco
- `.meta` de `ArvoreGrande.fbx`/`Morcegp.fbx` (dono dos assets)

## Como retomar
1. `git fetch && git checkout sprint-1/pass-d2-render`
2. Unity 6000.3.10f1 na `HexPrototipo`; antes de trocar de branch com o Editor aberto, abrir uma cena vazia.
3. Suíte (AGENTS.md §6); capturas e legibilidade: `tools/visual-baseline/README.md`.

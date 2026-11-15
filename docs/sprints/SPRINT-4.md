# Sprint 4 — Correções do playtest

**Objetivo:** corrigir o que o responsável encontrou jogando o build do MVP e os problemas conhecidos do
[relatório de integração](../integration/MVP_INTEGRATION_REPORT.md) (§9), um assunto por passo.

**Base:** `integration/mvp-20261117` @ `f49cee1`. **Branch do passo 1:** `sprint-4/legibilidade-tela`.

## Backlog

| # | Origem | Problema | Status |
|---|---|---|---|
| 1 | Responsável (playtest) | Em telas grandes os elementos das ilhas ficam minúsculos e pixelizados; difícil ver o que há nas ilhas | **feito no Editor**; build e custo do MSAA pendentes |
| 2 | Relatório MVP, P1 | Sem carta de dano o chefe vence; mão vazia por muito tempo quando os níveis demoram | não iniciado |
| 3 | Relatório MVP, P2 | Faixa rosa na borda inferior da tela no fim de tarde (cor do céu/névoa além do fundo) | não iniciado |
| 4 | Relatório MVP, P2 | Mira do tronco limitada a 65°: no Mirante o arco não aponta para o alvo atrás | não iniciado |
| 5 | Relatório MVP, P2 | Pés deslizando e arco encostando na roupa | não iniciado |
| 6 | Relatório MVP, P2 | Sem clipe de vitória/morte (Lucca fica em Idle no fim) | não iniciado |
| 7 | Relatório MVP, P2 | Painel de depuração IMGUI grande; produto ainda `GameJam13-06` (mudar troca o caminho do save) | não iniciado |
| 8 | Relatório MVP, infra | Editor e build usam o mesmo `loopia_save.json` | não iniciado |
| 9 | Relatório MVP, infra | `EditorBuildSettings` com cenas legadas habilitadas | não iniciado |

## Passo 1 — Legibilidade em telas grandes

### Diagnóstico (captura do responsável + registro do player)
Três causas que se multiplicam:
1. **Resolução.** O build abriu numa janela de 1280×720 num monitor 1920×1080 (registro
   `HKCU\Software\DefaultCompany\GameJam13-06`: modo janela, `Use Native = 0`). A Unity grava o último modo e ele
   vence o Player Settings; `resizableWindow: 0` impedia maximizar. Só 44% dos pixels do monitor.
2. **Anel de 36 casas.** O gerador sorteia raio 4–6 (24/30/36 casas) e a câmera sempre enquadra o anel inteiro.
   O Pass A validou 24 e 30; a partida do playtest tinha 36.
3. **Sem anti-aliasing.** `PC_RPAsset` com MSAA 1× e câmera sem FXAA/SMAA (já apontado em
   `docs/art/audit/RENDERING.md`). Cones, cogumelos e pedras de 3–6 px serrilham e piscam.

### Mudanças
| Assunto | Arquivos |
|---|---|
| Zoom do jogador: roda do mouse aproxima no ponto sob o cursor (até `menorMeiaAltura` = 7); botão direito ou do meio arrasta; afastado ao máximo = anel inteiro (padrão). A câmera só anda no próprio plano: névoa, sombra e o enquadramento do Pass A não mudam. Dica no canto inferior esquerdo até o primeiro uso | `HexIsoCamera.cs` |
| Nuvens presas ao mundo durante o zoom (medem-se pela vista do anel inteiro) | `CamadaDeNuvens.cs` |
| Resolução do build: tela cheia sem borda na resolução do monitor na primeira abertura; tela cheia abaixo da nativa sempre corrigida (inclusive após Alt+Enter); argumentos `-screen-*`/`-window-mode` respeitados. Criado sozinho antes da primeira cena (sem mudança de cena). Não roda no Editor | `ResolucaoDaTela.cs` (novo) |
| HUD IMGUI escala com a tela acima de 1080p (até 1080p nada muda) | `EscalaDaInterface.cs` (novo), `CartasUI.cs`, `LoopDirector.cs`, `BarraDeVidaDoChefe.cs` |
| MSAA 4× no PC (`m_MSAA: 1 → 4`) | `Assets/Settings/PC_RPAsset.asset` (pelo Editor) |
| Janela redimensionável (`resizableWindow: 0 → 1`) | `ProjectSettings/ProjectSettings.asset` (pelo Editor) |
| Anel de no máximo 30 casas (`raioMaximo` 6 → 5) e campos do zoom serializados | `Assets/Scenes/HexPrototipo.unity` (pelo Editor; diff de 7 linhas) |
| Testes | `LegibilidadeDaTelaTests.cs` (Logic, 22), `LegibilidadeDaTelaSceneTests.cs` (SceneStatic, 2), `LegibilidadeDaTelaPlayModeTests.cs` (PlayModeSmoke, 1), `RenderDoPCTests.cs` (+1, MSAA), `HexPrototipoSceneTests.cs` (pino 6 → 5) |

### Medidas (Editor, render da Main Camera, hora 12; `docs/art/sprint-4-legibilidade/`)
Topo da ilha = 3,97 u × pixels por unidade (Pass A: 127,4 px a 32,09 px/u).

| Caso | orthoSize | Topo da ilha |
|---|---|---|
| Playtest (build, janela 1280×720, anel 36 deformado) | ≈ 27 (estimado da captura) | ~52 px |
| Antes, anel 36 (semente 2), 1280×720 | 22,66 | 63 px |
| Antes, anel 36 (semente 2), 1920×1080 | 22,66 | 95 px |
| Antes, anel 30 (semente 5), 1920×1080 | 19,68 | 109 px |
| Antes, anel 24 (semente 1), 1920×1080 | 17,21 | 125 px |
| Depois, anéis de 24–30 sem zoom, 1920×1080 | igual ao antes (o enquadramento-base não mudou; pinado por `EnquadramentoDaCameraTests`) | 109–125 px |
| Depois, zoom máximo (meia-altura 7), 1920×1080 | 7 | ~306 px |

No caso do playtest, as três mudanças juntas levam o topo da ilha de ~52 px para 109–125 px sem zoom (resolução nativa
+ anel de até 30) e até ~300 px com zoom. Capturas: `antes/`, `depois/`, `comparacao_antes_depois_zoom.jpg`
(720p anel 36 × 1080p anel 30 × zoom de 4 passos) e `recorte_msaa_1x_4x.png` (mesma região com MSAA 1× e 4×,
ampliada 3×: bordas de cones, cogumelos e beira das ilhas sem degraus). Os metadados do "depois" (`capturas.json`)
se perderam na queda descrita abaixo; as imagens foram gravadas antes dela.

### Evidências
- Compilação no Editor (Unity 6000.3.10f1): 0 erros, 0 avisos. Antes disso, checagem fora do Editor com o Roslyn da
  Unity (defines de Editor e de player): 0 erros.
- Logic + SceneStatic: job `eb1ff800b25e45bf8d060b6d19f6ba60` — **371/371** (eram 346; +25).
- PlayModeSmoke + PlayModeMedicao: job `41af2a55578149d1840bc4bb6b74a21d`, `TestResults.xml` 22:29:03Z–22:29:38Z —
  **25/25** (eram 24; +1), inclui `Zoom_AproximaArrastaEVolta_NuvensFicamPresasAoMundo`. XML em
  `docs/art/sprint-4-legibilidade/evidencias/`.
- **Total 396/396** (370 + 26 novos). Nenhum teste removido ou ignorado.
- Console: só o erro esperado do `HexLoopGeneratorTests` (`LogAssert`, "Nenhum HexWorld encontrado na cena").
- Save do jogador: SHA `3d27854e…` antes e depois (as capturas em Play mudaram as moedas 209 → 210; restaurado da cópia).
- Cena: `HexPrototipo.unity` com 6 linhas novas (campos do zoom) e `raioMaximo: 6 → 5`; sem mudança de posição de câmera.

### Incidente: queda da Unity na medição de custo do MSAA
Depois das capturas, um script de medição renderizou a Main Camera 65 vezes por configuração em 12 configurações
(1080p e 4K, MSAA 1× e 4×) **dentro de um único frame** do Editor. A Unity só libera esses alvos no fim do frame:
`d3d12: Device failed error (8007000e)` (sem memória; a reserva de memória compartilhada passou ~8 GB do disponível)
e o Editor fechou (`Crash_2026-11-15_223206568`). Nada se perdeu: cena, assets e save estavam gravados. É um problema
do método de medição, não do jogo: em jogo é um render por frame (alvo com MSAA 4×: até ~100 MB em 1080p, ~400 MB em 4K).
**Não repetir** medição com muitos `Camera.Render()` no mesmo frame; medir um render por frame ou no build.

### Pendente
- **Custo do MSAA** (tempo de GPU em 1080p e 4K): **PENDENTE**, medir no build junto com o desempenho já pendente
  do D2. Em máquina fraca, a alternativa é MSAA 2×.
- **Build**: `ResolucaoDaTela` só roda no player; validar abrindo um build novo no monitor do playtest (deve abrir em
  tela cheia 1920×1080) e Alt+Enter ida e volta. **PENDENTE**.
- HUD acima de 1080p: coberto por teste da conta (`EscalaDaInterface.Fator`), não visto numa tela 1440p/4K real.

### Rollback
`git revert` dos commits do passo. A chave `loopia_resolucao_nativa_v1` em PlayerPrefs só marca a primeira
abertura; apagar não quebra nada.

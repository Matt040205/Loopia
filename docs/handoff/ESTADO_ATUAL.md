# Estado atual — 2026-11-15

## Onde estamos
- Sprint / passo: Sprint 1 (visual) — **Pass D2: só a luz** (concluído, aguardando revisão)
- Branch: `sprint-1/pass-d2-luz`, empilhada: `sprint-0/preparacao-tecnica` → `sprint-1/auditoria-visual` →
  `sprint-1/pass-a-escala-camera` (PR #1) → `sprint-1/pass-d1-nevoa` (PR #2) → `sprint-1/pass-b-composicao` (PR #3) →
  `sprint-1/pass-c-dressing` (PR #4) → `sprint-1/pass-d2-luz` (PR do D2 luz).
  Nada disso está na `main`.
- Status: aguardando revisão/merge do responsável; próximo passe só após aprovação

## O que foi feito no Pass D2 (escopo: só luz, pedido do responsável)
- `DayNightController.elevacaoMaximaDoSol` (padrão 90 = D1); cena com 58°: o sol não passa mais pelo zênite (volume e relevo
  leem de dia), nascer/pôr/noite idênticos ao D1. Diff de cena: 1 linha.
- Avaliados e rejeitados com números: sombras (+57 % batches, quase invisíveis sem chão), tonemapping Neutral (lava a cor) e
  ACES (noite quase preta). Relatório: `docs/art/pass-d2/RELATORIO_PASS_D2.md`.
- Evidências: suíte 259/259 (Logic+SceneStatic job `75d42646…`, PlayModeSmoke 7/7 13:38Z); contraste 12h 2,34–2,56
  (antes 2,59–2,81), 18h e 22h iguais; batches 527.
- Fora do escopo pedido (continuam pendentes do D2): Opaque Texture/SSAO, `new Material` por spawn, desempenho em build.
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
- Revisar/mergear a pilha (Sprint 0 → auditoria → PR #1 → PR #2 → PR #3 → PR #4 → D2 luz)
- Liberar o resto do D2 (Opaque Texture/SSAO, materiais por spawn, build) ou outro foco
- `.meta` de `ArvoreGrande.fbx`/`Morcegp.fbx` (dono dos assets)

## Como retomar
1. `git fetch && git checkout sprint-1/pass-d2-luz`
2. Unity 6000.3.10f1 na `HexPrototipo`; antes de trocar de branch com o Editor aberto, abrir uma cena vazia.
3. Suíte (AGENTS.md §6); capturas e legibilidade: `tools/visual-baseline/README.md`.

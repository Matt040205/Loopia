# Estado atual — 2026-11-14

## Onde estamos
- Sprint / passo: Sprint 1 (visual) — entrega 1: auditoria visual, Scene Bible e plano dos passes A–D (concluída)
- Branch: `sprint-1/auditoria-visual` (base: `sprint-0/preparacao-tecnica` @ `85c5276`, que ainda não foi mergeada na `main`)
- PR: não aberto
- Status: aguardando **aprovação do responsável** para implementar só o Pass A

## O que foi feito
- Auditoria visual consolidada com medidas reais do Editor — `docs/art/AUDITORIA_VISUAL.md`
- Scene Bible v0.1 (direção, escala em L/R, hierarquia, paleta, legibilidade, desempenho) — `docs/art/SCENE_BIBLE.md`
- Plano dos passes A–D, arquivos afetados, métricas, riscos/rollback — `docs/art/PLANO_PASSES.md`
- Relatórios especialistas — `docs/art/audit/{MEDIDAS,ENVIRONMENT,RENDERING}.md` (+ JSON brutos)
- Baseline reproduzível: 18 capturas + manifest — `docs/art/baseline/`; ferramenta — `tools/visual-baseline/`

## Evidências
- Capturas por `Camera.Render()` em Play Mode (seeds 12345/7/2024 × 12/18/22 h × 16:9/21:9), metadados no manifest
- Cena `HexPrototipo` não alterada (`isDirty=false`); nenhum arquivo em `Assets/` mudou
- Save do jogador restaurado byte a byte (SHA-256 `38433742…08ac`)
- Volume: só 1 na cena (verificado no Editor); o do prefab de fundo é latente

## Pendente / não verificado
- Captura de referência do responsável (não recebida)
- Lobo de 7,65 u: medido por bounds; confirmar com captura em jogo (item A0)
- Desempenho em build; HUD em 1920×1080 real (Game view do Editor está em 472×266)
- Origem dos 909 mil triângulos (Editor)

## Decisões que dependem do humano
- Aprovar Scene Bible (alvos de escala/legibilidade) e o Pass A
- Aprovar a ordem A → D1 (névoa) → B → C → D2
- `.meta` de `ArvoreGrande.fbx`/`Morcegp.fbx`: dono dos assets commitar antes de qualquer uso
- Merge da Sprint 0 na `main` (as branches estão empilhadas)

## Próximo passo sugerido
- **Pass A — Scale & Camera** em `sprint-1/pass-a-escala-camera`: `VisualDeIlha.cs`, `ArtScaleProfile.cs` (novo),
  `DecoracaoDeIlha.cs`, `HexIsoCamera.cs`, `Assets/Dados/Arte/EscalaDeArte.asset`, `Lobo.asset`/`Morcego.asset`,
  1 commit de cena via MCP, validador em `Assets/Editor/`. Aceite e testes: `docs/art/PLANO_PASSES.md` §Pass A.

## Como retomar
1. `git fetch && git checkout sprint-1/auditoria-visual`
2. Abrir o Unity 6000.3.10f1 na `HexPrototipo`.
3. Rodar a suíte (AGENTS.md §6) e, para comparar visual, `tools/visual-baseline/README.md`.

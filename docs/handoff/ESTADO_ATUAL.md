# Estado atual — 2026-11-14

## Onde estamos
- Sprint / passo: Sprint 1 (visual) — **Pass A: Scale & Camera** (concluído e revisado)
- Branch: `sprint-1/pass-a-escala-camera` (base: `sprint-1/auditoria-visual` → `sprint-0/preparacao-tecnica`; nada disso
  está na `main` ainda)
- PR: ver descrição em `docs/art/pass-a/RELATORIO_PASS_A.md`
- Status: aguardando revisão/merge do responsável; próximo passe só após aprovação

## O que foi feito
- `VisualDeIlha`: bounds de `SkinnedMeshRenderer` + `EixoDeMedida` (corrige lobo gigante)
- `ArtScaleProfile` (novo) + `Assets/Dados/Arte/EscalaDeArte.asset`: categorias da Scene Bible, tamanhos em L
- `DecoracaoDeIlha`: usa o perfil (sem perfil = comportamento antigo, provado por teste); árvores grandes só no lado
  longe da câmera e sem cobrir corredores
- `HexIsoCamera`: enquadra só pela malha hex das ilhas (arte não mexe no zoom)
- Cena (via Editor): `perfil` atribuído, `margem` 1.3 → 1.14, `distancia` 40 → 70
- Dados: Lobo `escala` 1.1 → 1.5, Morcego 0.38 → 0.6 (só visual)
- Validador `Loopia/Validar escala de arte`; 81 testes novos

## Evidências
- Suíte: 200/200 — revisor: job `8fd13927…` (196) + `TestResults.xml` 19:08:20Z (4)
- Aceite (1920×1080): Lucca 44/47/40 px (antes 33/35/31), topo da ilha ≥ 115 px, árvore grande 70–107 px, lobo 47 px
  (antes 312), morcego 30 px, oclusão 0/2160, ortho independente de decoração
- Capturas antes: `docs/art/baseline/`, `docs/art/pass-a/antes/`; depois: `docs/art/pass-a/depois/`
- Save restaurado (SHA-256 `38433742…`); cena sem alterações pendentes

## Pendente / não verificado
- HUD em 1920×1080 e 2520×1080 reais (validação manual do responsável)
- Desempenho em build; batches absolutos do Editor não são reproduzíveis entre sessões
- Validador: sinalizar modelo achatado com `EixoDeMedida.Altura`

## Decisões que dependem do humano
- Aprovar/mergear Sprint 0 → auditoria visual → Pass A (branches empilhadas)
- Liberar o próximo passe: **D1 (névoa/legibilidade)** pela ordem aprovada
- `.meta` de `ArvoreGrande.fbx`/`Morcegp.fbx` (dono dos assets)

## Próximo passo sugerido
- **Pass D1 — Névoa e legibilidade** (`docs/art/PLANO_PASSES.md`): `Daynightcontroller.cs` com parâmetros; névoa
  linear começando além da profundidade do anel (agora 51,5–85,1 u da câmera); noite legível; sol abaixo do horizonte.

## Como retomar
1. `git fetch && git checkout sprint-1/pass-a-escala-camera`
2. Unity 6000.3.10f1 na `HexPrototipo`; rodar a suíte (AGENTS.md §6) e comparar com as evidências.
3. Capturas: `tools/visual-baseline/README.md` (use `LOOPIA_CAPTURE_DIR` para não sobrescrever).

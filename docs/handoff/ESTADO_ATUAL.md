# Estado atual — 2026-11-14

## Onde estamos
- Sprint / passo: Sprint 0 — preparação técnica (concluída)
- Branch: `sprint-0/preparacao-tecnica` (base: `main` @ `0b9513e`)
- PR: não aberto (ver PRs propostos em `docs/sprints/SPRINT-0.md`)
- Status: concluído localmente; aguardando aprovação do humano para push/PR

## O que foi feito
- Contrato de trabalho para qualquer agente — `AGENTS.md`, `CLAUDE.md`
- Baseline do projeto — `docs/sprint-0/BASELINE.md`
- Auditoria estática validada contra o código — `docs/sprint-0/AUDITORIA_VALIDADA.md`
- Riscos, validação e rollback — `docs/sprint-0/RISCOS_VALIDACAO_ROLLBACK.md`
- Suíte de testes (119) — `Assets/Tests/Editor/`, relatório `docs/sprint-0/TEST_REPORT.md`
- Cliente do Unity MCP por TCP + procedimento para Play Mode — `tools/unity-mcp/`

## Evidências
- Testes: job `99ebcac91798424093a094f3bc38f4f6` Logic+SceneStatic 116/116; PlayModeSmoke 3/3
  (`TestResults.xml` 2026-11-14 17:28:31Z); job `2a662bca410a4ae48ca239a209f3513d` Logic 106/106
- Console: 0 erros
- Cenas tocadas: nenhuma (`HexPrototipo` aberta, não suja)
- Save do jogador restaurado: sim (SHA-256 `e67b637d…91e37e6` antes e depois)

## Pendente / não verificado
- Build, desempenho, 16:9 vs 21:9 — nada medido
- 4 voltas reais em sequência (boss testado via reflexão em `LoopDirector.FechouUmaVolta`)
- Sem testes: UI de cartas/arraste, câmera, inimigos, efeitos de plataforma, cena Acampamento

## Riscos abertos e decisões que dependem do humano
- `.meta` de `ArvoreGrande.fbx`, `Morcegp.fbx` e 2 PNGs (commit `0b9513e`) não estão no git: o dono
  dos assets precisa commitar um único `.meta` antes de qualquer prefab/cena usá-los
- `com.coplaydev.unity-mcp` em `#main` sem pin (lock em `30d2207…`)
- Introduzir asmdef de gameplay exige PR próprio (UnityEvents citam `Assembly-CSharp`)

## Próximo passo sugerido
- **Sprint 1 / P0.1 — normalização de escala** (planejar antes de implementar):
  `VisualDeIlha.cs` (bounds com `SkinnedMeshRenderer`), `DecoracaoDeIlha.cs` (escala por categoria),
  novo `ArtScaleProfile` (ScriptableObject, arquivo próprio), `HexTile.cs` (registrar renderers após
  decoração), `HexIsoCamera.cs` (bounds só da malha das ilhas), validador em `Assets/Editor/`.
  Aceite: árvore ≠ arbusto/pedra; Lucca proporcional; decoração não altera NavMesh nem zoom.
  Testes novos: escala por categoria, bounds com skinned mesh, câmera estável ao trocar decoração.

## Como retomar
1. `git fetch && git checkout sprint-0/preparacao-tecnica`
2. Abrir o projeto no Unity 6000.3.10f1; aguardar import.
3. Rodar a suíte (AGENTS.md §6; Play Mode: `tools/unity-mcp/README.md`) e comparar com as evidências acima.

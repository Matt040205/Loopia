# Sprint 0 — Preparação técnica

**Objetivo:** registrar baseline, mapear sistemas ativos, criar testes que preservem o comportamento
atual, mapear riscos e documentar validação/rollback. Sem refatorar gameplay, sem tocar
materiais/shaders/`HexPrototipo.unity`, sem dependências novas.

**Branch:** `sprint-0/preparacao-tecnica` (base `main` @ `0b9513e`). **Status:** concluída localmente,
aguardando push/PR.

## Entregas
| Prioridade da sprint | Entrega |
|---|---|
| 1. Baseline, commit, dependências | `docs/sprint-0/BASELINE.md` |
| 2. Sistemas ativos em `Scripts/Hex` | `BASELINE.md` §4–5, `AUDITORIA_VALIDADA.md`, `AGENTS.md` §3 |
| 3. Testes Edit/Play Mode (geração, navegação, cartas, voltas) | `Assets/Tests/Editor/` (119 testes), `docs/sprint-0/TEST_REPORT.md` |
| 4. Riscos de regressão nas cenas | `docs/sprint-0/RISCOS_VALIDACAO_ROLLBACK.md` §1 |
| 5. Validação e rollback | `RISCOS_VALIDACAO_ROLLBACK.md` §2–3 |
| Handoff entre agentes | `AGENTS.md`, `CLAUDE.md`, `docs/handoff/`, `tools/unity-mcp/` |

## PRs propostos (arquivos disjuntos, podem entrar em qualquer ordem)
1. **docs: contrato de agentes, baseline e auditoria validada** — `AGENTS.md`, `CLAUDE.md`,
   `docs/handoff/*`, `docs/sprints/*`, `docs/sprint-0/{BASELINE,AUDITORIA_VALIDADA,RISCOS_VALIDACAO_ROLLBACK}.md`,
   `tools/unity-mcp/*`. Risco zero para o jogo.
2. **test: suíte base de caracterização** — `Assets/Tests/**` (+ `.meta`), `docs/sprint-0/TEST_REPORT.md`.
   Compila só no `Assembly-CSharp-Editor`; não entra na build.

## Evidências
Ver `TEST_REPORT.md` §4: 119/119 em duas execuções do implementador e reexecução independente do
revisor (116/116 Logic+SceneStatic, 3/3 PlayModeSmoke). Save restaurado (hash idêntico).

## Execução (para auditoria do processo)
- Testes: implementados por subagente Sonnet; inventário da baseline por subagente Haiku; contrato,
  auditoria validada, riscos e revisão/reexecução pelo Claude Opus (analista de qualidade).
- Desvio aceito: resultados de Play Mode vêm do `TestResults.xml` (limitação do rastreador do MCP).

## Pendente
Build, desempenho, 21:9, corrida real de 4 voltas, UI/câmera/inimigos sem testes (lista completa em
`TEST_REPORT.md` §7). `.meta` dos modelos do commit `0b9513e` continuam fora do git (decisão do dono).

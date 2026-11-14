# Estado atual — 2026-11-14

## Onde estamos
- Sprint / passo: Sprint 1 (visual) — **Pass D1: névoa e legibilidade** (concluído e revisado)
- Branch: `sprint-1/pass-d1-nevoa`, empilhada: `sprint-0/preparacao-tecnica` → `sprint-1/auditoria-visual` →
  `sprint-1/pass-a-escala-camera` (PR #1) → `sprint-1/pass-d1-nevoa` (PR #2). Nada disso está na `main`.
- Status: aguardando revisão/merge do responsável; próximo passe só após aprovação

## O que foi feito no D1
- `Assets/testes shaders/Daynightcontroller.cs` (UTF-8 agora): névoa linear 105→230 u (ilhas a ≤ 100 u da câmera ficam
  limpas), cor de horizonte, elevação mínima do sol 26°, crepúsculo de 2 h, luar e preenchimento noturno no ambiente,
  dessaturação do ambiente. Continua sendo o único escritor de `RenderSettings`.
- Cena (via Editor): campos novos, curva de intensidade, RenderSettings/luz escritos pelo controlador.
- `tools/visual-baseline/legibilidade.py`: contraste topo da ilha × fundo imediato por captura.
- `tools/unity-mcp/umcp.py`: porta do bridge lida de `~/.unity-mcp` (muda quando o Unity reabre).
- Pass A: capturas "depois" refeitas em sessão limpa (as antigas tinham ilhas residuais; medidas não mudaram).

## Evidências
- Suíte 218/218 — revisor: job `76caba5c…` (213) + `TestResults.xml` 20:07:52Z (5); após rebase, job `a70dde67…` 213/213
- Contraste topo/fundo (antes → depois): 12h 1,28–1,32 → 2,48–2,85; 18h 0,82–0,87 → 1,80–2,12; 22h 0,83–0,89 → 2,85–3,23
- Sol nunca abaixo do horizonte com intensidade > 0,05; passo máximo 0,0192/min de jogo; batches −0,8 % (mesma sessão)

## Pendente / não verificado
- HUD em 1920×1080/2520×1080 reais (manual); desempenho em build
- Fundo perto do anel fica verde-escuro ao meio-dia (sem névoa): paleta/composição → Pass B e D2
- Save do jogador mudou entre 16:09 e 16:13 fora das execuções revisadas (`38433742…` → `c0a120f1…`, 82 → 99 moedas);
  cópia do estado anterior em `%TEMP%/loopia_save_reviewer.bak`. Confirmar com o responsável se foi uma partida dele.

## Decisões que dependem do humano
- Revisar/mergear a pilha (Sprint 0 → auditoria → PR #1 → PR #2)
- Liberar o próximo passe: **B (composição: fundo e rio)**, depois C e D2
- `.meta` de `ArvoreGrande.fbx`/`Morcegp.fbx` (dono dos assets)

## Próximo passo sugerido
- **Pass B — Scene Composition** (`docs/art/PLANO_PASSES.md`): `CenarioContinuo` com escala de mundo fixa ancorada no anel
  (árvores do fundo menores na tela que as das ilhas), rio fora da projeção do anel ou bem abaixo e esmaecido, limpeza de
  sobras inativas e do Volume latente do prefab de fundo.

## Como retomar
1. `git fetch && git checkout sprint-1/pass-d1-nevoa`
2. Unity 6000.3.10f1 na `HexPrototipo`; antes de trocar de branch com o Editor aberto, abrir uma cena vazia.
3. Suíte (AGENTS.md §6); capturas e legibilidade: `tools/visual-baseline/README.md`.

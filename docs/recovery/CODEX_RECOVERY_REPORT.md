# Resgate do trabalho do Codex (personagem Lucca) — 2026-11-15

Executado pelo Claude Code (frente de gameplay), sem integração geral, sem mudança de cena e sem alterar worktrees de
outros agentes. Ordem seguida: inventariar → preservar → comparar → publicar → relatar.

## Resumo
- **Nada estava só no working tree.** Todo o trabalho Unity do Codex estava em **6 commits locais nunca publicados**
  na branch `feature/lucca-locomotion-2c2` (worktree `loopia-lucca`), sem alterações pendentes nem assets sujos no Editor.
- As **fontes de produção** (Blender, FBX de ação, relatórios e telemetria) existiam **só fora do repositório**, em
  `C:\Users\Sitr3n\Documents\Codex\2026-11-14\ess`.
- A branch `recovery/codex-character-20261115` = os 6 commits do Codex **intactos** (mesmos SHAs) + fontes de arte em
  `ArtSource/Lucca/` + este relatório. Validada num Editor isolado: **319/319 testes**.
- Merge com `sprint-3/gameplay-cena` e com a G2 (`feature/gameplay-g2`): **sem conflito textual** (dry-run).

## 1. Inventário
### Worktrees (todas sem conflito, merge, rebase, cherry-pick ou bisect em andamento; nada staged)
| Worktree | Branch | HEAD | Estado local | Modif. / untracked | Commits não publicados | Responsável provável |
|---|---|---|---|---|---|---|
| `C:\Users\Sitr3n\Documents\Github\loopia` | `sprint-1/pass-d2-render` | `ea12ae7` | limpo | 0 / 0 | 2: `08d4a78` (.meta de `ArvoreGrande`/`Morcegp`), `ea12ae7` (`Assets/_Recovery/0.unity`, cópia de recuperação da Unity) | frente de ambiente/arte |
| `C:\Users\Sitr3n\Documents\Github\loopia-g2` | `feature/gameplay-g2` → `recovery/codex-character-20261115` (usada para este resgate) | `c125a9a` (G2, publicada) | só ruído do Unity | 3 / 4 | 0 (G2 está no PR #9) | Claude (gameplay G2) |
| `C:\Users\Sitr3n\Documents\Github\loopia-gameplay` | `sprint-3/gameplay-cena` | `0d21629` | só ruído do Unity; Editor fechado | 5 / 4 | 0 (PR #8) | conta anterior do Claude (G1) |
| `C:\Users\Sitr3n\Documents\Github\loopia-lucca` | `feature/lucca-locomotion-2c2` | `cd8e5aa` | só ruído do Unity | 2 / 4 | **6** (sem upstream) | **Codex (personagem)** |
"Ruído do Unity" = `.vscode/settings.json`, `Dressing_Vegetacao.mat` (ganha `_MainTex` ao importar),
`ProjectSettings/{TimeManager,ShaderGraphSettings,EditorBuildSettings}.asset` (formato/fim de linha) e os 4 `.meta`
untracked de `ArvoreGrande`/`Morcegp` (já commitados localmente em `08d4a78`). Nada disso é do Codex; nada foi publicado.

Outras branches locais sem worktree: `feature/lucca-character-integration` (`89dc640`, ancestral de `cd8e5aa`, também
não publicada) e `prototipo-hex` (3 commits locais de 2026-10-07 — nuvens/cenário do protótipo antigo, não são do
Codex; ficaram preservados no bundle, **não** publicados aqui).

### Editors Unity
| PID | `-projectPath` | Porta MCP | Agente | Cena aberta | Suja? |
|---|---|---|---|---|---|
| 35356 | `...\Github\loopia` | 6401 | frente de ambiente | `HexPrototipo` | não |
| 41608 | `...\Github\loopia-lucca` (log em `Documents\Codex\2026-11-14\ess\work\integration-unity.log`) | 6400 | **Codex** | `HexPrototipo` | não |
| 2352 | `...\Github\loopia-g2` | 6402 | Claude (G2 / este resgate) | cena vazia (aberta antes do checkout) | não |
Assets marcados como alterados em memória (consulta só leitura): nos Editors 6400/6401 só TMP, `WaterLowPoly.shadergraph`
e `Dressing_Vegetacao.mat` (reimportação do Unity). **Nenhum asset da Lucca com alteração não salva.** Nenhum Editor foi
fechado ou alterado.

## 2. Recuperação
### Commits do Codex (todos em `feature/lucca-locomotion-2c2`, base `ee46799` = PR #8 antes do último commit de docs)
| Commit | Grupo | Conteúdo |
|---|---|---|
| `fa4fb87` | Assets e FBX | `Assets/Modelos/Lucca/`: `Loopia-Game-Ready-2B.2.fbx` (rig Humanoid), `Flecha.fbx`, 5 materiais, 2 texturas, `Lucca.controller`, `VisualFlecha.prefab`, clipes Idle/Walk/Jump/Spring Jump/Bow Ready/Bow Shot, `Loopia_BowUpper.mask` |
| `9eff525` | Prefab, cena, scripts | `LuccaVisual.prefab`, `HexPrototipo.unity` (+68/−), `LuccaAnimationBridge.cs` (novo), `LuccaCombate.cs` (+83/−), `HexPlayer.cs` (+4: `AoIniciarPulo`), `Projetil.cs` (visual da flecha opcional) |
| `45c8bb3` | Testes | `LuccaIntegracaoTests.cs` (novo), `MarcadoresPlayModeTests.cs` (+4) |
| `89dc640` | Docs | `docs/character-integration/*` (6), `docs/handoff/ESTADO_ATUAL.md` |
| `da0ad72` | Animações 2C.2 | Walk/Jump/Spring Jump revisados, `Run.anim` novo, `Lucca.controller` (Blend Tree 1D `MoveSpeed`), prefab, bridge |
| `cd8e5aa` | Testes e docs 2C.2 | `LuccaIntegracaoTests` (+39), `LOCOMOTION_REWORK.md`, `KNOWN_ISSUES.md`, `ANIMATION_CONTRACT.md`, `ESTADO_ATUAL.md` |
Os dois bundles git que o próprio Codex deixou (`outputs/Loopia-Integracao-Lucca/loopia-lucca.bundle` →
`89dc640`, `outputs/Loopia-Sprint-2C.2/Loopia-Locomocao-2C.2.bundle` → `cd8e5aa`) foram verificados e batem com as
branches locais.

`Assets/TempLucca2C2/` (vista untracked no início do dia) **não existe mais**: era a pasta temporária de importação
dos FBX da 2C.2 (62 menções no log do Editor do Codex); o resultado (`.anim`) está em `da0ad72`. Os FBX de origem estão
em `ArtSource/Lucca/Sprint-2C.2/FBX/`.

### O que estava onde
| Material | Antes do resgate | Agora |
|---|---|---|
| Assets, prefabs, controller, clipes, scripts, testes, docs | commitado **só localmente** | publicado (commits originais, mesmos SHAs) |
| `.blend` 2C (`4a43045d…`) e 2C.2 (`e30bb109…`), FBX de ação, relatórios e telemetria | **só fora do repo** | publicado em `ArtSource/Lucca/` (commit `9a70328`), fora de `Assets` |
| Vídeos (`*.mp4`, ~13 MB), capturas PNG, bundles, `.blend` de sprints antigas (v01, 2B.1, 2B.2), projeto `UnityRigQA`, scripts Python do Codex | só fora do repo | **não publicados** (ver §4); preservados na cópia de segurança com hashes |
| Alterações não commitadas do Codex | nenhuma | — |

## 3. Preservação (feita antes de qualquer mudança)
Pasta `C:\Users\Sitr3n\Documents\Loopia_Recovery_20261115\` (fora dos repositórios):
- `git/loopia-all-refs-20261115.bundle` — `git bundle --all` (68 MB; todas as branches locais, inclusive
  `prototipo-hex` e os 2 commits locais de `sprint-1/pass-d2-render`) + `refs.txt`.
- `codex-workspace/2026-11-14/` — cópia integral do workspace do Codex (546 MB; só `UnityRigQA/Library` excluída,
  regenerável). Conferida: **6.906 arquivos, todos com SHA-256 igual ao original**.
- `manifests/codex-workspace-original.sha256` e `manifests/lucca-worktree-arquivos-do-codex.sha256` (60 arquivos).
- `worktree-diffs/` — diff dos arquivos rastreados, `status --porcelain=v2` e cópia dos untracked de cada worktree.
Os originais não foram alterados. Nenhum `reset`, `clean`, rebase, `stash` ou `add -A`.

Hashes-chave (conferem com `docs/character-integration/ASSET_AUDIT.md` e com o pacote em
`Downloads\Loopia - Progresso de Rigging e Animacao\Sprint 2C`, 46 arquivos):
| Arquivo | SHA-256 |
|---|---|
| `Loopia-Animacao-2C.blend` | `4a43045d1fefd38b43687e36ff6a3afc761cafafb6a289d1a27d1451996b6212` |
| `Loopia-Locomocao-2C.2.blend` | `e30bb109c6ee2eda44d34ef124f939b922b38b8f1bfe23d30371467262a7f460` |
| `Loopia-Game-Ready-2B.2.fbx` | `d698ff7103980f389617ad616b252d5c791a87df5cab1ac4832cfe8afe9c0640` |
Obs.: os arquivos texto de `ArtSource` (md/json/csv/xml) passam pela normalização de fim de linha do Git
(`core.autocrlf=true`); os hashes acima e os do manifesto valem para os binários e para os originais.

## 4. Não publicado (e por quê)
| Item | Onde está | Motivo |
|---|---|---|
| Vídeos `Ciclo-continuo-Unity.mp4`, `Volta-real-Unity.mp4`, `Lucca-Gameplay-Unity.mp4`, `videos/*.mp4` | `Documents\Codex\2026-11-14\ess\outputs\…` + cópia | mídia de evidência, sem uso no build; manter o repo enxuto |
| Capturas PNG (Blender/Unity) | idem | idem |
| `.blend` históricos (`Loopia-Personagem-v01`, Sprint 2B.1/2B.2, `work/*.blend`) e `Downloads\…\Loopia-Art-Master-v01.blend` | idem / Downloads | versões anteriores; a fonte vigente (2C + 2C.2) foi publicada |
| `work/UnityRigQA` (projeto Unity de QA, 114 MB de Assets) | `ess\work\UnityRigQA` + cópia | laboratório do Codex, não é produção |
| Scripts Python/Blender do Codex (`work/*.py`), logs | `ess\work` + cópia | ferramentas de trabalho; conferir com o responsável antes de versionar |
| 2 commits locais de `sprint-1/pass-d2-render`, 3 de `prototipo-hex` | branches locais + bundle | não são do Codex; decisão do dono da frente |
Nenhum arquivo passa de 13 MB no Git (maior: `Idle.anim`); sem necessidade de Git LFS. O repositório não foi reconfigurado.

## 5. Integridade
- **Scripts modificados pelo Codex:** `LuccaCombate.cs`, `HexPlayer.cs`, `Projetil.cs`; novo `LuccaAnimationBridge.cs`.
- **Assets novos:** 60 arquivos em `Assets/Modelos/Lucca` + testes/docs. **Todo arquivo novo tem `.meta`**, inclusive
  pastas. Nenhum `m_Script: {fileID: 0}`.
- **Referências:** todos os GUIDs citados por `LuccaVisual.prefab` (6), `VisualFlecha.prefab` (4), `Lucca.controller` (8),
  materiais e `HexPrototipo.unity` (94) resolvem para `.meta` do próprio commit ou para pacotes. Nenhuma referência
  quebrada.
- **No Editor isolado (branch de recuperação):** Avatar válido e Humanoid; `Lucca.controller` com 2 camadas; clipes
  Idle 3,200 s, Walk 0,867 s, Run 0,500 s, Jump 0,350 s, Spring Jump 0,700 s, Bow Ready 1 frame, Bow Shot 0,650 s com
  1 evento (`OnBowRelease`) — igual ao `ANIMATION_CONTRACT.md`.
- **Blender:** `.blend` 2C (rig 57 ossos, ações 2C) e 2C.2 (locomoção revisada) publicados; não foram abertos no
  Blender nesta auditoria (só hash e auditoria do Codex).
- **Dependências externas:** Blender 5.2.0 LTS para editar as fontes; nenhum pacote Unity novo.

## 6. Conflitos potenciais
Dry-run com `git merge-tree --write-tree` (nada foi mergeado):
| Contra | Resultado |
|---|---|
| `sprint-3/gameplay-cena` (`0d21629`, PR #8) | **limpo** (árvore `70c79f5…`). `0d21629` só mexe em docs de G1 |
| `feature/gameplay-g2` (`c125a9a`, PR #9) | **limpo** (árvore `cacf778…`). Nenhum arquivo é tocado pelos dois lados |

Conflitos **funcionais** a revisar na integração:
1. **`LuccaCombate.cs`** — o Codex reescreveu `Update` (tiro reservado → soltura no evento da animação), criou
   `PodeAgir`, `AlvoDaPreparacao`, `SoltarTiro`, `CancelarTiro`, `PodeDispararEm`. `AlvoAtual` e `AoDisparar` foram
   preservados; `AoDisparar` agora sai **depois** de criar a flecha e gastar munição. A G2-C (Mirante) **ainda não**
   mexe nesse arquivo; o plano dela (`docs/gameplay/g2/G2_HANDOFF.md`) já mira esta versão (trocar `alcance` em
   `EscolherAlvo`/`PodeDispararEm` e consultar o Mirante em `EstaAFrente`).
2. **`Projetil.Disparar`** — novo parâmetro opcional `visual` e flecha como filho visual (sem collider). Compatível com
   chamadas antigas; único chamador é o `LuccaCombate`.
3. **`HexPlayer.cs`** — só o evento novo `AoIniciarPulo`. Sem impacto em G1/G2.
4. **`HexPrototipo.unity`** — o Codex mudou a cena (prefab visual, objetos `Corpo`/`Frente` desativados). O PR #8 também
   a altera (já incluído na base do Codex) e a G2 tem um passo de cena pendente (cartas no baralho). Pela regra do
   AGENTS §4.1, só um PR de cena aberto por vez: integrar a cena do Codex **antes** do passo de cena da G2.
5. **`docs/handoff/ESTADO_ATUAL.md`** — editado pelo Codex; a G2 não o tocou. Sem conflito.
6. **Testes de Play que leem a munição/tiro** (`EncontroDoChefePlayModeTests`, `PartidaRealPlayModeTests`) passam na
   branch do Codex; a combinação com a G2 (teto `FlechasMaximoTotal`, testes `G2PlayModeTests`) **não foi executada**
   no Editor — só o merge textual foi verificado.

## 7. Validação (real)
Editor Unity 6000.3.10f1 isolado (PID 2352, worktree `loopia-g2`, branch `recovery/codex-character-20261115` em
`9a70328`; o commit deste relatório não muda código). Nenhum outro Editor rodava testes durante a execução
(sem backup de save em `%TEMP%` antes de cada suíte).
| Categoria | Job / fonte | Total | Passou | Falhou |
|---|---|---|---|---|
| Logic | `37f304b050f64dc7b9a2b7d4ff6325c5` | 265 | 265 | 0 |
| SceneStatic | `448db84404044322a691b3f75671032d` | 36 | 36 | 0 |
| PlayModeSmoke | `TestResults.xml` 18:04:43Z–18:05:26Z (job `d759417b…`, assembly da `loopia-g2`) | 17 | 17 | 0 |
| PlayModeMedicao | `TestResults.xml` 18:05:50Z–18:06:30Z (job `02bfb03e…`) | 1 | 1 | 0 |
**319/319.** Inclui os 4 testes do Codex (`LuccaIntegracaoTests`: locomoção pela velocidade real e retomada após
pulo, Mola real com Spring Jump, fallback sem Animator e sem flechas, soltura única no socket com evento ausente).
Medição `flechas+torre`: vitória, 4 voltas de 33,9–34,0 s, chefe 21,6 s, 21 flechas no chefe.
Console: 0 erros (só o erro esperado do `HexLoopGeneratorTests` durante o Logic). `GuardaDaSuite` aprovada; save do
jogador com SHA-256 `3d27854e…551efb` antes e depois.

**Não validado aqui:** partida manual/visual com a Lucca (deslizamento dos pés, mira lateral, contatos do arco —
ver `KNOWN_ISSUES.md` do Codex), build, abertura dos `.blend` no Blender, combinação com a G2 no Editor.

## 8. Entrega
| Item | Valor |
|---|---|
| Branch | `recovery/codex-character-20261115` |
| Base do PR | `sprint-3/gameplay-cena` (PR #8) |
| Commits | 6 do Codex (`fa4fb87`…`cd8e5aa`, inalterados) + `9a70328` (ArtSource) + este relatório |
| Merge | **nenhum**. Integração geral só depois da revisão externa |

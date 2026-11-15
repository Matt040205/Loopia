# Integração do MVP — 2026-11-15

Branch `integration/mvp-20261117`, worktree `C:\Users\Sitr3n\Documents\Github\loopia-mvp`, Editor Unity exclusivo desta
worktree. Nada foi mergeado na `main` nem nas branches de origem.

## 1. Origem
| Fonte | Branch | HEAD usado |
|---|---|---|
| PR #10 — resgate do Codex (Lucca) | `recovery/codex-character-20261115` | `9362e9f` (base da branch) |
| PR #9 — G2 | `feature/gameplay-g2` | `c125a9a` (merge `--no-ff`) |
| PR #8 — G1 + pilha de arte | `sprint-3/gameplay-cena` | `0d21629` (já na ancestralidade dos dois) |
Antes de começar: as 4 worktrees sem nada staged nem operação pendente; os 3 Editors abertos sem cena suja e sem asset
relevante alterado em memória (só reimportação de TMP, `WaterLowPoly.shadergraph`, `Dressing_Vegetacao.mat`). Cópia do
resgate preservada em `Documents\Loopia_Recovery_20261115` (não tocada).

## 2. Commits desta integração
| Commit | O quê |
|---|---|
| `1d5407d` | merge da G2 sobre o resgate. **Sem conflito**: nenhum arquivo tocado pelos dois lados |
| `eadf560` | `TestSupport.SaveIsolado`: backup do save com id do projeto no nome + trava comum `loopia_tests_save.lock` (projeto, PID, 30 min); a 2ª suíte falha antes de tocar no save; backup no formato antigo em `%TEMP%` também bloqueia |
| `e01223d` | Mirante ligado ao `LuccaCombate` do Codex + `MirantePlayModeTests` (4) |
| `275ad7c` | cena: Santuário, Fonte Restauradora e Oficina no `MaoDeCartas.baralho` (pelo Editor) |
| `55442a5` | `G2SceneTests` (SceneStatic): as 3 cartas no baralho, as 11 antigas preservadas, sem repetição |
| (este) | relatório, capturas, evidências e handoff |

## 3. Conferência do código combinado (antes de editar a cena)
- Idênticos ao PR #10: `LuccaCombate.cs`, `HexPlayer.cs`, `Projetil.cs`, `LuccaAnimationBridge.cs`, `HexPrototipo.unity`,
  `Lucca.controller`. Idênticos ao PR #9: `MaoDeCartas.cs`, `LuccaStatus.cs`, `LoopDirector.cs`, `MiranteDoLucca.cs`,
  `ModificadoresDoLucca.cs`, `EncontroDoChefePlayModeTests.cs`.
- Compilação no Editor sem erros; tipos presentes: `LuccaAnimationBridge`, `MiranteDoLucca`, `RegrasDoMirante`,
  `ModificadoresDoLucca`, `EncontroDoChefe`, `CicloDaPartida`; `LuccaCombate.SoltarTiro` (disparo sincronizado),
  `HexPlayer.AoIniciarPulo`, `LuccaStatus.FlechasMaximoTotal`, `MaoDeCartas.Pendentes`.

## 4. Mirante no combate (mudança mínima)
`LuccaCombate`: campo opcional `mirante` (vazio → `AddComponent` em tempo de execução, sem editar a cena), `NoMirante`,
`AlcanceAtual`. `AlcanceAtual` substitui `alcance` em `EscolherAlvo` **e** em `PodeDispararEm` — que o combate chama de
novo no `Update` (reserva) e na soltura (`SoltarTiro → Disparar`). `EstaAFrente` consulta
`RegrasDoMirante.IgnoraDirecao(noMirante, tiroEmQualquerDirecao, alvoDeQualquerAngulo)`: a regra de alvo grande do chefe
continua igual. `Update` chama `mirante.SincronizarCom(player.Coord)` antes de mirar (cobre o teleporte de
`HexPlayer.ColocarEm`, que não dispara `AoEntrarNoHexagono`). Nada mudou na reserva, no evento `OnBowRelease`, no
cancelamento nem na cadência.
Testes (`MirantePlayModeTests`, cena real, Mola + Ilha alta): entrada/saída (alcance 4 ↔ 3); alvo **atrás** a 4 casas
escolhido e atingido só no Mirante; tiro reservado no Mirante **cancelado** ao sair antes da soltura (munição intacta);
alvo grande escolhido com e sem Mirante.
Limite visual: a mira do tronco do Codex vai até 65° de yaw; no Mirante a flecha (teleguiada) acerta alvos atrás, mas o
arco não aponta para trás (P2).

## 5. Cena
Pelo Editor (SerializedObject + Undo + SaveScene), objeto `Cartas`: baralho 11 → 14 (GUIDs `5bcfc580…`, `105e39fc…`,
`b8f02e8d…`). O Unity serializou os campos novos com o padrão: `LuccaCombate.mirante` vazio,
`MaoDeCartas.limiteDeAlertaDePendentes: 4`, `MaoDeCartas.diretor` vazio. Diff da cena: +6 linhas; prefab da Lucca e
encontro do chefe intactos. **Atenção:** o PR #8 e o PR #10 também alteram esta cena; este PR deve entrar depois deles.

## 6. Testes (Editor exclusivo; nenhum outro Editor rodando testes)
| Categoria | Job / fonte | Total | Passou |
|---|---|---|---|
| Logic | `6659d39b545540c39fa80e9e69cbecd3` | 309 | 309 |
| SceneStatic | `510a529eaffc462d92238ae6967088ee` | 37 | 37 |
| PlayModeSmoke | XML 18:33:42Z–18:34:32Z (job `6e1648e1…`) | 23 | 23 |
| PlayModeMedicao | XML 18:34:48Z–18:35:30Z (job `c997ec94…`) | 1 | 1 |
**Total 370** = 360 da G2 + 5 do Codex que a G2 não tinha (1 SceneStatic + 4 PlayModeSmoke) + 5 novos da integração
(4 Mirante + 1 SceneStatic). Nenhum teste removido, ignorado ou inconclusivo. Console: só o erro esperado de
`HexLoopGeneratorTests` (`LogAssert`). Save: backup próprio antes (SHA `3d27854e…551efb`), a guarda da suíte conferiu o
mesmo hash depois; trava e backup por projeto apagados no fim. XMLs em `evidencias/`.

## 7. Partida manual (Editor, Play Mode na `HexPrototipo`, capturas em `capturas/editor_*`)
Cartas jogadas pelo mesmo caminho da UI (`MapaDePlataformas.Colocar` + `MaoDeCartas.Gastar`) por script MCP, não por
arraste do mouse. O arraste real foi visto funcionando na build (ver §8).
| # | Item | Resultado |
|---|---|---|
| 1 | Lucca com o modelo novo | ok (`editor_02_lucca_perto`, `VisualRoot` ativo) |
| 2 | Run | ok (estado `Locomotion` a 3,5 u/s) |
| 3 | Jump e Spring Jump | ok (estados `Jump`/`Spring Jump` observados; `editor_04_spring_jump`) |
| 4 | Arco equipado | ok (em todas as capturas) |
| 5 | Flecha sai na animação | ok (pose de disparo com flecha na corda, `editor_08_lucca_chefe`; origem no socket coberta por teste do Codex) |
| 6 | Sem disparo duplicado | ok: 7 eventos `AoDisparar` = 7 flechas gastas, janela sem recarga |
| 7 | Cartas G2 sorteadas | ok: Oficina, Fonte e Santuário saíram do baralho da cena por pendentes |
| 8 | Indicador de pendentes | ok (`+1 pendente`, `editor_03_jogo`; `+4 pendentes` na build) |
| 9 | Bônus | Santuário 50 → 53/53; Oficina 15 → 17 (atual +2); Fonte `coracao+2` no agregador; troca Oficina → Torre removeu o +2 sem resíduo. Cura +7 no coração: coberta por testes, não isolada na partida |
| 10 | Mirante | ok: Spring Jump → `MIRANTE alc=4`, volta a 3 ao sair |
| 11 | 4 voltas | ok (duas partidas) |
| 12 | Morcego Ancião | ok (`editor_07_chefe`, barra 557/732) |
| 13 | Vitória | ok, 2ª partida: "O Lucca derrotou o chefe depois de 4 voltas!" (`editor_11_fim2`). **Ajuda de teste:** Torre entregue com `DarCarta` porque a mão não sorteou carta de dano |
| 14 | Derrota | ok, 1ª partida (sem dano extra): "O Lucca caiu diante do chefe." (`editor_09_fim`) |
| 15 | Volta ao acampamento | ok nas duas; vitória persistida (vitórias 1 → 2), moedas acumuladas |
Console no fim: 0 erros, 0 avisos. Save restaurado ao hash anterior depois da partida.

## 8. Build Windows x64 (Development)
| Item | Valor |
|---|---|
| Build principal (fallback) | `C:\Users\Sitr3n\Documents\Loopia_Builds\mvp-20261117-dev-55442a5\Loopia.exe` — 203 MB, 0 erros, 0 avisos, 2 min 20 s |
| Cenas | `Acampamento` (0) e `HexPrototipo` (1), passadas no `BuildPlayerOptions`; `EditorBuildSettings` **não** foi alterado (ainda lista Menu/Testes/GameOver/Win legadas) |
| Build de QA | `...\mvp-20261117-dev-55442a5-qa-partida-direta\Loopia.exe` — mesma versão, `HexPrototipo` como cena 0, só para rodar a partida sem clique |
| Commit | `55442a5` (o commit deste relatório não muda código) |

Verificado na build principal: abre fora do Editor no Acampamento lendo o save (`build2_01`); partida com HUD, cartas,
`+1 pendente` e arraste/soltura de cartas funcionando (`build_01`, `build_02`: uma pessoa colocou Mola e Flechas
durante a execução); ao fechar, o save foi gravado (moedas 215 → 222). Fechamento limpo, sem crash.
Na build de QA (sem interação): 4 voltas → chefe (alcance extra 3) → derrota → tela final, em 193 s; **0 exceções**
(`evidencias/build_qa_player.log`, `buildqa_*`). Materiais/shaders, prefab da Lucca e Animator carregados (Lucca e ilhas
renderizadas; nenhum magenta de shader faltando).
Não verificado na build: vitória (precisa de cartas de dano colocadas à mão) e o botão "Voltar ao acampamento" (o clique
por mensagem de janela não chega ao Input System; não injetei entrada real no mouse do usuário).

## 9. Problemas conhecidos
| Prioridade | Problema |
|---|---|
| P1 (balanceamento, não bloqueia) | Sem carta de dano, o chefe vence (as duas partidas sem Torre terminaram em derrota). Mão vazia por muito tempo quando os níveis demoram |
| P2 | Faixa rosa na borda inferior da tela na build, fim de tarde, 16:9 (`build_02`): cor do céu/névoa além do fundo de paisagem. Frente gráfica |
| P2 | Mira do tronco limitada a 65°: no Mirante o tiro para trás acerta, mas o arco não aponta para o alvo |
| P2 | Deslizamento dos pés e contato do arco com a roupa (`docs/character-integration/KNOWN_ISSUES.md`) |
| P2 | Sem clipe de vitória/morte (Lucca fica em Idle no fim) |
| P2 | Painel de depuração IMGUI grande; nome da janela/produto ainda `GameJam13-06` (muda o caminho do save se alterado) |
| Infra | Editor e build usam o **mesmo** `loopia_save.json`; jogar a build altera o save do jogador |
| Infra | `EditorBuildSettings` com cenas legadas habilitadas; um "Build" pelo menu incluiria Menu/Testes/GameOver/Win |

## 10. Rollback
- Branch: não há merge em lugar nenhum; fechar o PR basta. Para desfazer partes: `git revert 55442a5 275ad7c e01223d
  eadf560` (ordem inversa), e `git revert -m 1 1d5407d` para tirar a G2.
- Build: a pasta `mvp-20261117-dev-55442a5` é o fallback; não apagar.
- Save do jogador: cópias em `Documents\Loopia_Recovery_20261115\save\`.

# G2 — plano de implementação

Frente de gameplay. Referências: `GDD_V2.md` §3 (G2), `IMPLEMENTATION_PLAN.md` §G2, `RELATORIO_G1.md`.

## Auditoria inicial (2026-11-15)
| Item | Estado |
|---|---|
| Base | `origin/sprint-3/gameplay-cena` @ `0d21629` (G1 aprovada; contém a pilha de arte #1–#6 até `56c6bf1` + G1 + cena integrada) |
| Branch / worktree | `feature/gameplay-g2` em `C:\Users\Sitr3n\Documents\Github\loopia-g2` (nova; a worktree `loopia-gameplay` da conta anterior não é usada) |
| PRs abertos | #1–#6 (arte), #7 (G0+G1), #8 (cena G1). Nenhum mergeado na `main` |
| Divergência | `sprint-1/pass-d2-render` local tem 2 commits a mais que a base (`08d4a78` `.meta` de `ArvoreGrande`/`Morcegp`, `ea12ae7` `Assets/_Recovery`), fora do PR #6 publicado. Não pertencem a G2; não reaplicados |
| Frente de personagem | `feature/lucca-locomotion-2c2` (worktree `loopia-lucca`, Editor próprio aberto, **não publicada**) altera `LuccaCombate.cs` (+83/−), `HexPlayer.cs`, `Projetil.cs` e `HexPrototipo.unity`, e tem mudanças locais não commitadas |
| Unity | Editor da pasta principal (frente de ambiente) e da `loopia-lucca` abertos. G2 usa um **terceiro Editor**, só na `loopia-g2` |

## Riscos de conflito e decisões
| Risco | Decisão |
|---|---|
| `LuccaCombate.cs` em edição pela frente de personagem | G2 **não toca** `LuccaCombate`. Dano continua lido de `MapaDePlataformas.BonusDeDanoTotal` (API preservada). O Mirante sai como componente isolado + plano de integração (G2-C) |
| `HexPrototipo.unity` já mexida pelo PR #8 (aberto) e pela frente de personagem | G2 **não edita cena** (AGENTS §4.1: nunca dois PRs abertos na mesma cena). As 3 cartas novas entram como assets; incluí-las no `baralho` da `MaoDeCartas` é um passo de cena documentado em `G2_HANDOFF.md`. A validação em partida real usa a cena carregada, com as cartas colocadas em tempo de execução, sem salvar |
| Campos novos em componentes da cena | só com padrão seguro (fallback por código), para a cena antiga continuar válida sem reserialização |

## Fases e commits
1. **G2-A fila de recompensas** — `MaoDeCartas` (fila de direitos, entrega por evento), `CartasUI` (indicador `+N pendentes`), testes; o teste de caracterização da mão cheia é atualizado de propósito. Design: `REWARD_QUEUE_DESIGN.md`.
2. **G2-B agregador** — `ModificadoresDoLucca` (struct imutável), ganchos `BonusDeFlechas`/`BonusDeCuraPorCoracao` em `PlataformaDef`, `MapaDePlataformas.Recalcular` soma o struct, `LuccaStatus.DefinirModificadores`/`FlechasMaximoTotal`/`CurarComCoracao`, recargas (`RestaurarVidaEMunicao`, acampamento no chefe) no teto efetivo. Design: `MODIFIER_SYSTEM.md`.
3. **G2-B cartas** — Santuário, Fonte Restauradora, Oficina de Arqueiro como `PlataformaSimplesDef` (sem subclasse nova, sem switch por nome), assets criados pelo Editor com `.meta`.
4. **G2-C Mirante** — `RegrasDoMirante` (puro) + `MiranteDoLucca` (componente, eventos), testes; integração em `LuccaCombate` só quando a frente de personagem publicar.
5. **Testes/docs** — suíte completa no Editor, `G2_TEST_REPORT.md`, `G2_HANDOFF.md`.

## Arquivos previstos
`Assets/Scripts/Hex/`: `MaoDeCartas.cs`, `CartasUI.cs`, `ModificadoresDoLucca.cs` (novo), `PlataformaDef.cs`, `PlataformaSimplesDef.cs`,
`PlataformaRecompensaDef.cs`, `PlataformaRelevoDef.cs`, `MapaDePlataformas.cs`, `LuccaStatus.cs`, `LoopDirector.cs` (recarga no chefe e HUD
usam o teto efetivo), `RegrasDoMirante.cs` (novo), `MiranteDoLucca.cs` (novo).
`Assets/Dados/Plataformas/`: `Santuario.asset`, `FonteRestauradora.asset`, `OficinaDeArqueiro.asset` (novos).
`Assets/Tests/Editor/`: `MaoDeCartasTests.cs`, `FilaDeRecompensasTests.cs`, `ModificadoresTests.cs`, `CartasDeSobrevivenciaTests.cs`,
`MiranteTests.cs`, `G2PlayModeTests.cs`, ajuste em `EncontroDoChefePlayModeTests.cs` (teto efetivo).
Não tocados: cenas, `LuccaCombate.cs`, `HexPlayer.cs`, `Projetil.cs`, materiais, shaders, URP, câmera, luz.

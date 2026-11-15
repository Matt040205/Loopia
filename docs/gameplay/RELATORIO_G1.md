# Relatório G1 — ciclo da partida, chefe, vitória e derrota

Handoff da frente de gameplay (não substitui `docs/handoff/ESTADO_ATUAL.md`, que é coordenado entre as frentes).

## Status: **G1 NÃO concluída** — implementada, compilada fora do Editor, sem execução no Editor
G0, GDD v2 e arquitetura aprovados pelo responsável (2026-11-15). G1 só conclui com a suíte rodando no Editor e a
validação em Play Mode (GDD_V2 §6).

## Onde está
- Worktree `C:\Users\Sitr3n\Documents\Github\loopia-gameplay`, branch `sprint-3/gameplay-loop`, PR #7 (rascunho,
  base `sprint-0/preparacao-tecnica`). Commit-base: `85c5276`.
- Nenhuma cena, prefab, material ou asset serializado alterado. Só scripts, testes e `docs/gameplay/`.

## O que foi feito
| Entrega | Arquivos |
|---|---|
| Auditoria G0 e planejamento | `GAMEPLAY_AUDIT.md`, `BALANCE_BASELINE.md`, `GDD_V2.md`, `IMPLEMENTATION_PLAN.md`, `APRESENTACAO_DO_CHEFE.md` |
| Máquina de estados pura | `EstadoDaPartida.cs`, `CicloDaPartida.cs` |
| Regras do chefe | `RegrasDoChefe.cs` (alcance extra), `VarianteDoChefe.cs` (morcego ampliado, sem asset) |
| Encontro | `EncontroDoChefe.cs` (nasce 1×, alvo grande, Revoada), `BarraDeVidaDoChefe.cs`, `IndicadorDePerigoDoChefe.cs` |
| Fluxo | `LoopDirector` (estados, congela voltas, recarga de flechas no chefe, vitória, `aoVencer`), `HexLoopRunner` (`contarVoltas`, `AoPassarPeloAcampamento`), `InimigoSpawner` (`pausarSpawnComum`, `CongelarTodos`, `NascerNoAnel`, `AoInimigoDerrotado`), `DadosDoJogador` (`vitorias`, `TentarLer`), `TelaDeMorte` (`MostrarVitoria`) |
| Cartas bloqueadas no fim | `MapaDePlataformas.PodeColocar` ("a jornada terminou"), `CartasUI` (barra some) |
| Alvo grande + gancho de mira | `Inimigo` (`TornarAlvoGrande`), `LuccaCombate` (regra de alvo, `AlvoAtual`, `AoDisparar`, para no fim) |
| Medição | `MetricasDaPartida.cs`, `MedidorDaPartida.cs` |

Motivo para tocar `Inimigo.cs`/`LuccaCombate.cs`: o chefe precisa ser alvo do tiro automático; a frente de personagem
precisa de um dado de leitura para orientar o arco. Inimigos comuns: alcance extra 0, comportamento igual.
`HexPlayer.cs`, rig e animações: não tocados.

## Testes escritos
| Arquivo | Categoria | Testes | Cobre |
|---|---|---|---|
| `CicloDaPartidaTests` | Logic | 10 | transições, terminais exclusivos e únicos, tabela completa |
| `RegrasDoChefeTests` | Logic | 16 | chefe alcançável de todo o anel (11 sementes), sem extra fica fora, variante sem alterar o asset |
| `DadosDoJogadorFormatoTests` | Logic | 7 | save antigo sem `vitorias`, novo, negativos, ilegível |
| `ColocacaoNoFimDaJornadaTests` | Logic | 4 | cartas aceitas correndo/no chefe, recusadas na vitória/derrota |
| `MetricasDaPartidaTests` | Logic | 3 | contagem por volta e do chefe, impasse, término único |
| `EncontroDoChefePlayModeTests` | PlayModeSmoke | 5 | 4 voltas pelo corredor → 1 confronto, 5ª não conta, chefe 1×, golpe via UnityEvent, flechas reais ferem o chefe, vitória 1× + save + volta ao acampamento, derrota no chefe, Revoada + indicador, cartas no fim |
| `PartidaRealPlayModeTests` | PlayModeSmoke (1) + PlayModeMedicao (1) | 2 | 4 voltas **reais** no NavMesh → chefe → combate automático até o fim; métricas |
Total novo: 40 Logic + 6 PlayModeSmoke + 1 PlayModeMedicao. Suíte anterior: 119.

## Evidências
| Verificação | Resultado | Fonte |
|---|---|---|
| Compilação de `Scripts/Hex` + `testes shaders` | 0 erros, 0 avisos nos arquivos tocados | Roslyn do Unity 6000.3.10f1 fora do Editor. **Não é** a compilação do Editor |
| Compilação de `Assets/Tests/Editor` | 0 erros | idem |
| Merge com a pilha de arte (`56c6bf1`) | sem conflito; árvore mesclada compila | `git merge-tree` + mesma compilação |
| Suíte no Editor (Logic, SceneStatic, PlayModeSmoke) | **PENDENTE** | Editor ocupado por outra frente; MCP não usado |
| Console do Editor | **PENDENTE** | idem |
| 4 voltas reais, chefe, combate, vitória, derrota em Play Mode | **PENDENTE** | não declarar funcionamento antes de rodar |
| Métricas do chefe (duração, flechas, recargas, dano, Revoada, impasse, cartas) | **PENDENTE** | `PartidaRealPlayModeTests` |

## Correção de processo
Os 4 arquivos de teste do primeiro commit entraram **sem `.meta`** (contra AGENTS.md §4.2); os `.meta` foram
adicionados no commit seguinte, antes de qualquer import pela Unity, então não há GUID divergente em nenhuma máquina.

## Pendências e riscos
1. Rodar a suíte e os testes Play (`PartidaRealPlayModeTests` leva alguns minutos: 4 voltas reais a timeScale 4).
2. Integração de cena: base a decidir (`IMPLEMENTATION_PLAN.md` §G1.8; recomendado: empilhar sobre a pilha de arte).
3. Na integração, atualizar de propósito `HexPrototipoPlayModeTests.Quatro_Voltas_LiberamOBossUmaUnicaVez`.
4. Números do chefe: hipóteses até as medições.
5. `IndicadorDePerigoDoChefe` usa `Shader.Find("Universal Render Pipeline/Unlit")` com fallback; conferir em build.
6. Testes Play compartilham o `loopia_save.json` e o backup em `%TEMP%` com qualquer outro Editor do projeto aberto
   ao mesmo tempo: rodar só com o outro Editor parado (sem Play Mode nem testes).

## Como testar
1. Editor exclusivo, aberto no projeto desta branch (worktree) ou da branch de integração.
2. Test Runner → EditMode: `Logic`, `SceneStatic` (resultado no `get_test_job`), depois `PlayModeSmoke` e
   `PlayModeMedicao` (resultado no `TestResults.xml`; ver `tools/unity-mcp/README.md`).
3. Esperado: 119 antigos + 47 novos. Avisos esperados no console: "Sem EncontroDoChefe na cena" (teste antigo das
   4 voltas), resumos `[Metricas]`.

## Checklist do PR
- [x] Escopo: só scripts de gameplay listados, testes e `docs/gameplay/`; `git diff --stat` revisado
- [x] `.meta` de todo arquivo novo incluído (GUIDs únicos); nenhum `.asset` novo
- [ ] Testes: **PENDENTE** (Editor); compilação verificada fora do Editor
- [ ] Console: **PENDENTE**
- [x] Cenas tocadas: nenhuma
- [ ] Captura antes/depois: na integração de cena
- [x] Rollback: `git revert` dos commits do G1 (sem listeners de cena órfãos)

## Próximo passo
Validar G1 no Editor e integrar a cena. G2 só depois da aprovação do responsável: 1º fila de recompensas, depois as
três cartas, depois Mirante.

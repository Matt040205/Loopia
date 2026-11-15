# Resultados de testes — Sprint de estabilização do MVP

Unity 6000.3.10f1, Editor da worktree `loopia-mvp`, MCP `run_tests` + `get_test_job`. Os testes de Play Mode
(PlayModeSmoke/PlayModeMedicao) são lidos no `TestResults.xml` do Test Framework (o rastreador do MCP perde o fim);
desde a renomeação do produto o XML fica em `%USERPROFILE%\AppData\LocalLow\DefaultCompany\Loopia\`.

## Linha de base
| Origem | Logic | SceneStatic | PlayModeSmoke | PlayModeMedicao | Total |
|---|---|---|---|---|---|
| Integração do MVP (`f49cee1`) | 309 | 37 | 23 | 1 | **370** |
| Depois da legibilidade (`0340291`) | 331 | 40 | 24 | 1 | **396** |

## Execuções desta sprint (parciais, por item)
| Quando / item | Job ou XML | Resultado |
|---|---|---|
| P0-2/P0-3/P0-4: Logic + SceneStatic | job `ba05871102ae4cb2b91ecbb020e57cc4` | 381/381 |
| P0-2: trava entre worktrees | job `27194c902f5d452ba5a07a702b95c8b1` (XML) | 2/2 (a 1ª versão deu Inconclusive por não achar o processo `explorer` no Mono; corrigida para usar o PID do Editor) |
| P1-1: testes do fundo (inclui 1 PlayModeSmoke) | job `198c93c929554974ab98770c5bbb4609` (XML) | 14/14 |
| P1-2: painel | job `e073db6929d845e69781b7421bc741f5` (XML) | 4/4 |

## Integridade do save durante os testes
- Antes de cada bloco: SHA-256 do save antigo (`...\GameJam13-06\loopia_save.json`) = `3d27854e…551efb`, conferido
  depois de cada execução: inalterado.
- A pasta nova (`...\DefaultCompany\Loopia\`) começou sem save; Play Mode do Editor (capturas, medições) cria um,
  que foi apagado depois de cada sessão para a build nova fazer a migração de verdade.
- Nenhuma trava (`loopia_tests_save.lock`) nem backup (`loopia_tests_save_<id>.bak`) sobrou em `%TEMP%`.

## Suíte completa final (código de `31b7cdf`/`b8881e1`, antes das builds)
| Categoria | Job ou XML | Total | Passou | Falhou |
|---|---|---|---|---|
| Logic + SceneStatic | job `61a5074535bb45b0a44118f0218e6071` | 391 | 391 | 0 |
| PlayModeSmoke + PlayModeMedicao | job `e328d64f8b2a498c95ecaad877e50bef`, XML 23:49:20Z–23:49:57Z (`evidencias/TestResults_playmode_20261115T234920Z.xml`) | 25 | 25 | 0 |
| **Total** | | **416** | **416** | **0** |

Linha de base 370 → 396 (legibilidade) → **416** (+20 nesta sprint): `SaveDoJogadorPerfilTests` 7,
`ConfiguracaoDeBuildTests` 3, `SaveIsoladoConcorrenciaTests` 2, `FundoDePaisagemCoberturaTests` 2, `PainelDoJogoTests`
4, `LuccaStatusTests.Morto_NaoColetaMoedas` 1, `HexPrototipoSceneTests.MaoInicial_…` 1. Nenhum teste removido ou
ignorado. Console no fim: 0 erros (só o aviso informativo do MCP ao limpar o job de Play Mode).

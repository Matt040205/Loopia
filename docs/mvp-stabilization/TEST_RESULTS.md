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

## Suíte antes das builds `7bf6492` (código de `31b7cdf`/`b8881e1`)
| Categoria | Job ou XML | Total | Passou | Falhou |
|---|---|---|---|---|
| Logic + SceneStatic | job `61a5074535bb45b0a44118f0218e6071` | 391 | 391 | 0 |
| PlayModeSmoke + PlayModeMedicao | job `e328d64f8b2a498c95ecaad877e50bef`, XML 23:49:20Z–23:49:57Z (`evidencias/TestResults_playmode_20261115T234920Z.xml`) | 25 | 25 | 0 |
| **Total** | | **416** | **416** | **0** |

Linha de base 370 → 396 (legibilidade) → **416** (+20 nesta sprint): `SaveDoJogadorPerfilTests` 7,
`ConfiguracaoDeBuildTests` 3, `SaveIsoladoConcorrenciaTests` 2, `FundoDePaisagemCoberturaTests` 2, `PainelDoJogoTests`
4, `LuccaStatusTests.Morto_NaoColetaMoedas` 1, `HexPrototipoSceneTests.MaoInicial_…` 1. Nenhum teste removido ou
ignorado. Console no fim: 0 erros (só o aviso informativo do MCP ao limpar o job de Play Mode).

## Suíte final (código de `a7a5c56`, antes das builds finais)
| Categoria | Job ou XML | Total | Passou | Falhou |
|---|---|---|---|---|
| Logic + SceneStatic | job `67d7335ae02149249e2e2bf6027e0e88` | 392 | 392 | 0 |
| PlayModeSmoke + PlayModeMedicao | job `ad60b6d226bd41e39af5c475e62ca368`, XML 03:33:54Z–03:34:29Z (`evidencias/TestResults_playmode_final.xml`) | 25 | 25 | 0 |
| **Total** | | **417** | **417** | **0** |

+1 em relação a 416: `LuccaStatusTests.JornadaEncerrada_VivoTambemNaoColetaMoedas`; o PlayModeSmoke da vitória
ganhou a conferência de moeda depois do fim. Save do jogador com o mesmo hash antes e depois da suíte; nenhuma
trava ou backup sobrando em `%TEMP%`.

## Bloco C (P2-1, P2-2, P2-3) — código de `c615e42`
| Categoria | Job ou XML | Total | Passou | Falhou |
|---|---|---|---|---|
| Logic + SceneStatic | job `f9bde7adb9414c7590dfc1cb7d943204` | 398 | 398 | 0 |
| PlayModeSmoke | job `606df17d2ea84846a51a5cf88eb30272`, XML 05:11:35Z (`evidencias/TestResults_playmodesmoke_blocoC.xml`) | 25 | 25 | 0 |
| PlayModeMedicao | job `9e92549dd95948de87d1d6e0e821673a`, XML 05:17:15Z (`evidencias/TestResults_playmodemedicao_blocoC.xml`) | 4 | 4 | 0 |
| **Total** | | **427** | **427** | **0** |

Base 417 → **427**: +6 Logic (`LuccaMiraTests`: divisão tronco/corpo, limite correndo, giro da marcha, erro horizontal), +1 PlayModeSmoke
(`Mirante_AlvoAtras_OArcoApontaParaOAlvoNaSoltura`), +3 PlayModeMedicao (`ApoioDosPesMedicaoTests`: deslize por
contexto, só locomoção com a marcha ligada/desligada, captura antes/depois da mira). O PlayModeSmoke da vitória e da
derrota do chefe passou a conferir a pose do fim (P2-3). Nenhum teste removido ou ignorado. Save do jogador com o
mesmo hash (`76e429e4…`) antes e depois de cada execução.

Execuções parciais no caminho (todas com o save conferido):
| Item | Job | Resultado |
|---|---|---|
| P2-3: grupo do chefe | XML da sessão | 5/5 |
| P2-1: mira, Mirante, integração do Lucca | job `08e0da7836074aefb90e61c6f6a5d1d6` | 14/14 |
| P2-1/P2-2: mira, integração, Mirante, chefe | job `78aca0a695b34d0589bdeeefb7446736` | 21/21 |

# G2 — relatório de testes

Editor Unity 6000.3.10f1 aberto **só** na worktree `C:\Users\Sitr3n\Documents\Github\loopia-g2` (bridge MCP próprio,
porta 6402). Os Editors das outras frentes (pasta principal e `loopia-lucca`) não foram usados. Data: 2026-11-15.

## Baseline (antes de qualquer mudança, base `0d21629`)
| Categoria | Job / fonte | Total | Passou | Falhou |
|---|---|---|---|---|
| Logic | `77ad89c888e04a39afa9f4b92cef5f83` | 265 | 265 | 0 |
| SceneStatic | `536fef30772d4a0d853f2ffc9d8cd4dc` | 35 | 35 | 0 |
| PlayModeSmoke | `TestResults.xml` 17:28:39Z–17:29:30Z | 13 | 13 | 0 |
Igual ao baseline reportado na G1 (265/35/13). PlayModeMedicao não foi repetido no baseline (rodado no fim, abaixo).

**Incidente no baseline (save):** os 13 testes passaram, mas a guarda da suíte (`GuardaDaSuite`) acusou "save não
restaurado": o arquivo de backup dos testes fica em `%TEMP%\loopia_tests_save.bak`, **compartilhado entre os três
Editors abertos**, e outra frente rodou testes ao mesmo tempo e apagou o backup. O save real ficou com
`moedas 174` (antes `136`). Restaurei manualmente a cópia feita antes da execução (SHA-256 `f672c88b…f34006a`,
conferido). Depois disso o save mudou de novo por fora desta frente (17:38:10Z, `moedas 209, vitorias 1`,
SHA `3d27854e…551efb`); as execuções seguintes de G2 preservaram esse estado (hash igual antes/depois de cada
suíte). Risco registrado em `G2_HANDOFF.md`.

## Final (ponta `843b50e` + ajuste de avisos nos testes da fila)
| Categoria | Job / fonte | Total | Passou | Falhou |
|---|---|---|---|---|
| Logic | `e878f2a998e34e98bbfeeae392dbd3af` | 309 | 309 | 0 |
| SceneStatic | `2fafcd69e6464c2daacc3bfbcfa5a09e` | 35 | 35 | 0 |
| PlayModeSmoke | `TestResults.xml` 17:50:16Z–17:51:05Z (job `3d172ff5…`) | 15 | 15 | 0 |
| PlayModeMedicao | `TestResults.xml` 17:51:23Z–17:52:04Z (job `ee1160d1…`) | 1 | 1 | 0 |
**Total: 360** (314 da G1 + 46 novos). `GuardaDaSuite` aprovada nas duas execuções de Play: SHA-256 do save
`3d27854e…551efb` antes e depois. Nenhum teste ignorado ou inconclusivo.

**Console:** 0 erros depois das suítes. Durante o Logic aparece 1 erro esperado de teste antigo
(`HexLoopGeneratorTests`, `LogAssert.Expect("Nenhum HexWorld encontrado")`) e avisos esperados (`MapaDePlataformas`
"Nao da para colocar", `FundoDePaisagem` sem modelos em cena vazia, e o aviso do limite suave da fila, que tem
`LogAssert` próprio). Nenhum erro novo.

## Testes novos (46)
| Arquivo | Categoria | Qtd | Cobre |
|---|---|---|---|
| `FilaDeRecompensasTests` | Logic | 16 | entrega imediata com espaço; mão cheia + vários níveis; `cartasPorNivel`; gastar uma → entra uma (sem duplicar); ordem FIFO; nenhum nível perdido; baralho vazio; carta indisponível espera liberar (gatilho do mapa); sorteio só entre disponíveis; eventos `AoMudarPendentes`/`AoMudar` uma vez por mudança; reentrada sem duplicar; vitória/derrota (diretor próprio e do mapa) e Lucca morto bloqueiam entrega sem perder direito; limite suave só avisa; `Comprar` direto não cria pendente |
| `ModificadoresTests` | Logic | 12 | soma/igualdade do struct; ganchos com padrão 0; PV e flechas sobem/descem com o teto; recargas (carta Flechas, volta, acampamento no chefe) no teto efetivo; morto só muda o teto; negativos e notificação; atalho `DefinirBonusDePvMaximo`; `CurarComCoracao` (só coração, não ressuscita); `MapaDePlataformas.Recalcular` soma tudo e preserva `BonusDePvTotal`/`BonusDeDanoTotal`; Área de Treino + Torre = 31 % como antes |
| `CartasDeSobrevivenciaTests` | Logic | 7 | assets reais: valores, fora do caminho, sem pré-requisito; assets antigos sem bônus novos; coração 5 → 7 → 9; Fonte não afeta Flechas/moedas/restauro; não ressuscita; Oficina 15/17/19 e teto da carta Flechas; Santuário + Campo Pacífico |
| `MiranteTests` | Logic | 9 | regras puras (alcance, direção, ilha levantada); coexistência com o alcance extra do chefe (anel gerado, semente 12345); componente fora/dentro/saindo da ilha alta, evento só na troca; valores vindos do asset; asset `IlhaAlta` com +1 e exigindo Mola |
| `G2PlayModeTests` | PlayModeSmoke | 2 | **cena real**: mão inicial cheia + nível 1 → pendente, jogar Mola pela via da UI → a pendente entra, derrota bloqueia; Santuário/Oficina/Fonte colocados, coração coletado (+7), Oficina substituída por Torre, Santuário/Fonte removidos sem bônus fantasma, 4 voltas reais até o chefe e recarga no acampamento até 17 |
Atualizado de propósito: `MaoDeCartasTests.Characterization_MaoCheia_ComprarFalhaEACartaDoLevelUpESimplesmentePerdida`
→ `MaoCheia_ComprarFalha_MasACartaDoLevelUpFicaPendenteEEntraAoAbrirEspaco` (comportamento novo, mesmas pré-condições).
`EncontroDoChefePlayModeTests` passou a comparar a recarga do chefe com `FlechasMaximoTotal` (igual ao base sem Oficina).

## Compatibilidade com G1 (pela suíte)
4 voltas → chefe uma vez, 5ª passagem não conta, Revoada, recarga de flechas no chefe, vitória/derrota exclusivas,
save de vitórias e volta ao acampamento, cartas bloqueadas no fim: todos os testes de G1 continuam verdes
(`EncontroDoChefePlayModeTests`, `HexPrototipoPlayModeTests`, `ColocacaoNoFimDaJornadaTests`, `PartidaRealPlayModeTests`).
Medição (`flechas+torre`, anel 30): vitória, 4 voltas de 33,9–34,2 s, chefe 23,6 s, 20 flechas no chefe — mesma
faixa da G1 (`BALANCE_BASELINE.md` §8.2). As cartas novas não estão no baralho da cena, então não entram nessa medição.

## Não verificado / pendente
- **Partida manual com as cartas novas sorteadas pela mão:** depende de pôr as 3 cartas no `baralho` da cena (passo
  de cena bloqueado, `G2_HANDOFF.md`). A cena real foi exercitada pelos testes de Play com as cartas colocadas em
  tempo de execução.
- **Indicador `+N pendentes`:** desenhado em Play (o teste passa frames com pendentes e sem exceção), mas **sem
  captura de tela** nesta sprint.
- **Mirante em combate:** só regra/componente; o `LuccaCombate` ainda não consulta o Mirante.
- Build: não feito (fora do escopo de G2).

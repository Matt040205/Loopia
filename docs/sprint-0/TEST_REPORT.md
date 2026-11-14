# Sprint 0 - Relatorio da suite de testes automatizados

Branch: `sprint-0/preparacao-tecnica`. Unity 6000.3.10f1, URP 17.3, AI Navigation 2.0.6, Test Framework 1.4.6 no
manifest (1.6.0 resolvido no `packages-lock.json`).
Escopo: caracterizar o comportamento ATUAL do gameplay ativo em `Assets/Scripts/Hex/` (namespace `Loopia.Hex`).
Os testes protegem o que ja existe; nenhum arquivo de gameplay, cena, dado ou configuracao foi alterado.

## 1. Onde ficam os testes e por que nao ha asmdef

Todos os testes estao em `Assets/Tests/Editor/` e rodam no assembly predefinido `Assembly-CSharp-Editor`
(qualquer pasta chamada `Editor` sem asmdef). Esse assembly ja referencia nunit e o `Assembly-CSharp`.

Nao foi criado nenhum `.asmdef` de proposito: as cenas guardam listeners de UnityEvent com
`m_TargetAssemblyTypeName: Loopia.Hex.X, Assembly-CSharp` (4 ocorrencias). Mover os scripts de gameplay para um
assembly novo quebraria esses listeners, risco que nao vale a pena na Sprint 0.

Consequencia: os testes "Play Mode" sao `[UnityTest]` do assembly de Editor que ligam o Play Mode de dentro do teste
(`yield return new EnterPlayMode()` / `ExitPlayMode()`), e nao testes do assembly de Play Mode.

## 2. Como rodar

Test Runner do Editor: `Window > General > Test Runner`, aba **EditMode** (todos os testes aparecem la). Filtre por
categoria (`Logic`, `SceneStatic`, `PlayModeSmoke`) ou rode tudo. Os `PlayModeSmoke` trocam para Play Mode sozinhos
(a cena HexPrototipo e aberta, jogada e a cena original e reaberta depois).

Via MCP (Unity MCP bridge):

```
run_tests {"mode":"EditMode"}                                  # tudo
run_tests {"mode":"EditMode","categoryNames":["Logic"]}        # so logica pura
run_tests {"mode":"EditMode","categoryNames":["SceneStatic"]}  # abre a cena, sem Play
run_tests {"mode":"EditMode","categoryNames":["PlayModeSmoke"]}
get_test_job {"job_id":"<id>","include_details":true}
```

LIMITACAO CONHECIDA DO RUNNER MCP: testes que entram em Play Mode recarregam o dominio (o projeto usa Enter Play Mode
Options com reload de dominio ligado). O rastreador de jobs do MCP perde o fim da execucao e o `get_test_job` fica em
`running` com `completed: 0` para sempre; alem disso ele bloqueia novos `run_tests`/`refresh_unity` com `tests_running`.
Para esses testes, a evidencia vem do `TestResults.xml` que o proprio Unity Test Framework grava em
`%USERPROFILE%\AppData\LocalLow\DefaultCompany\GameJam13-06\TestResults.xml`. Para destravar o rastreador do MCP:
chamar `TestJobManager.ClearStuckJob()` por reflexao (foi o que se fez). Categorias `Logic` e `SceneStatic` nao tem
esse problema e foram verificadas pelo `get_test_job`. Comando pronto para destravar e procedimento completo:
`tools/unity-mcp/README.md` (secao "Testes de Play Mode e o rastreador de jobs").

## 3. Inventario (119 testes)

| Categoria | Arquivo | Testes | O que protege |
|---|---|---|---|
| Logic | `HexCoordTests.cs` | 14 | Distance simetrica/triangular, 6 vizinhos distintos a distancia 1, `Neighbor` mod 6, 6N casas no anel de raio N, direcoes antihorarias a 0/60/.../300 graus (base do `PosicaoLivre`), round-trip `ToWorld`/`ToHex` (size 3 e 1, grade de -12 a 12), pontos dentro do hexagono caem na mesma casa, medidas de `HexLayout`, `Round`, operadores |
| Logic | `HexLoopPathTests.cs` | 6 | Indexador circular com negativos e acima de Count, `Normalizar`, `TryIndiceDe`/`Contem`, `TryProxima` nos dois sentidos, `Definir` |
| Logic | `HexLoopGeneratorTests.cs` | 54 | 11 sementes (1..10 e 12345 da cena) x 4 propriedades: ciclo valido e >= 6 casas; miolo >= `mioloMinimo`, disjunto do anel, tem casa com 6 vizinhas no miolo e `CentroDoMiolo` dentro dele; `TotalDeIlhas == Loop.Count`, um unico Acampamento em `Loop[0]` e `mundo.Acampamento == Loop[0]`, todo miolo bloqueado e sem ilha; determinismo (mesma semente, mesma sequencia e mesmo miolo). Mais: regerar no mesmo gerador, falha sem `HexWorld` (log de erro esperado) e 8 casos de `CicloEhValido` (valido, nulo, < 6, repetida, nao vizinhas, nao fecha, atalho com 3 vizinhas, anel com centro) |
| Logic | `DecoracaoTests.cs` | 5 | `DecoracaoDeIlha.PosicaoLivre` com os valores da cena (raio 2.1, corredor 0.4): centro reservado, seis corredores rejeitados, perto do corredor rejeitado, ponto entre corredores a 0.8 do raio aceito nos 6 setores, borda e pegada grande rejeitadas. Os calculos estao nos comentarios do teste |
| Logic | `PlacementRulesTests.cs` | 11 | `MapaDePlataformas.PodeColocar` (mapa montado sem Start, `player` null): carta nula, acampamento nunca substituivel, miolo rejeita NoCaminho/ForaDoCaminho/Qualquer, NoCaminho (nao fora, sim no anel), ForaDoCaminho (nao no anel, sim fora), Qualquer, Ilha Alta sem mola indisponivel com motivo, mola antes do relevo no percurso aceita e depois rejeita, `podeSerSubstituida=false` bloqueia, ilha `Construida` criada fora do anel. Todo motivo de rejeicao e nao vazio |
| Logic | `MaoDeCartasTests.cs` | 10 | 4 espacos vazios, `DarCarta` no primeiro livre (reaproveita buracos), mao cheia, carta indisponivel, `AoMudar`, `Comprar` sem baralho/so indisponiveis, `Comprar` preenche; ver secao 6 sobre o teste de caracterizacao da mao cheia |
| Logic | `LuccaStatusTests.cs` | 6 | PV/flechas/nivel iniciais, morte unica e sem ressurreicao, cura limitada, bonus de PV, flechas, XP com varios niveis. Nao toca em moedas (gravariam o save) |
| SceneStatic | `HexPrototipoSceneTests.cs` | 10 | Cena abre sem ficar suja; exatamente um de cada: HexWorld, HexLoopGenerator, NavegacaoDoMundo, HexLoopRunner, LoopDirector, MapaDePlataformas, MaoDeCartas, HexPlayer; NavMeshSurface no objeto do HexWorld; valores serializados (tamanho 3, folga 0.3, espessura 0.6, raio 4..6, 400 deformacoes, miolo 9, sem semente fixa, semente 12345, 4 voltas, +10%, 4 espacos); `LoopDirector.aoGameOver` com listener nao nulo e metodo existente (`TelaDeMorte.Mostrar`); varredura de todos os UnityEvents dos componentes `Loopia.Hex` e de todos os `m_Calls` serializados da cena (3 chamadas); nenhum script faltando em nenhum objeto; os 11 assets de `Assets/Dados/Plataformas` carregam como `PlataformaDef`; baralho sem nulos |
| PlayModeSmoke | `HexPrototipoPlayModeTests.cs` | 3 | (1) mapa valido, NavMesh a menos de 2 m do acampamento, um `NavMeshLink` por casa do anel, Lucca nasce no acampamento e no NavMesh, `PassosNaVoltaAtual` sobe (Lucca anda), fecha ao menos 1 volta de verdade (timeScale 4), `VoltasCompletas` do diretor igual ao do corredor, multiplicador = 1.10^voltas (1.10 apos a 1a). (2) 4 voltas (disparadas por reflexao no `FechouUmaVolta` privado) liberam o Boss e `AoLiberarBoss` dispara exatamente uma vez (5a volta nao repete), multiplicador 1.1^n, vida restaurada. (3) morte: `GameOver`, `AoPerder` 1x, corredor para, `TelaDeMorte.painel` aparece via UnityEvent `aoGameOver` |

Qualquer `Debug.LogError` durante o Play Mode falha o teste (comportamento padrao do UTF, mantido).

### Isolamento e seguranca
- Nenhuma cena e salva. Testes de Logic usam `EditorSceneManager.NewScene(EmptyScene)`, os de cena abrem
  `HexPrototipo` em modo Single; no fim (`OneTimeTearDown` e `[SetUpFixture]`) a cena original e reaberta.
  A cena original fica registrada em `SessionState`, que sobrevive ao reload de dominio.
- Save do jogador: `SaveIsolado` copia `loopia_save.json` para a pasta temporaria antes (o backup fica em disco e nao
  em memoria, porque o Play Mode recarrega o dominio) e restaura depois de cada teste de Play Mode e no fim da suite;
  o `[SetUpFixture]` ainda compara os hashes e falha a suite se divergirem.
- Play Mode: `Application.runInBackground = true` logo ao entrar; `Time.timeScale = 4` apenas durante a corrida;
  todas as esperas tem timeout em tempo real (30 s para andar, 90 s para a volta); `timeScale` volta a 1 no teardown.
- Os testes que usam variaveis locais depois de `EnterPlayMode` rodam o corpo em um metodo separado: o reload de
  dominio descarta o estado do iterador (inclusive closures), o que causou `NullReferenceException` na primeira versao.

## 4. Resultados reais

Cada execucao abaixo foi feita de verdade no Editor aberto.

| Execucao | Job id | Total | Passou | Falhou | Pulou | Duracao | Fonte |
|---|---|---|---|---|---|---|---|
| Logic #1 | `0024bddf74574700bce4131d7764428b` | 106 | 106 | 0 | 0 | 1.75 s | `get_test_job` |
| SceneStatic #1 | `8db00705327946e095448e4ba6ff5764` | 10 | 10 | 0 | 0 | 1.13 s | `get_test_job` |
| Logic #2 (ordem invertida) | `30dffebe887446199690f4f23c55458b` | 106 | 106 | 0 | 0 | 0.57 s | `get_test_job` |
| SceneStatic #2 (ordem invertida) | `62ad95a3987d4b43989163bc023a4744` | 10 | 10 | 0 | 0 | 0.99 s | `get_test_job` |
| PlayModeSmoke - 1a tentativa | `94a9d2db8dc7470ba66f3956a7d8338e` | 3 | 0 | 3 | 0 | - | `TestResults.xml`; bug no PROPRIO teste (NRE por perda de closures no reload de dominio), corrigido |
| PlayModeSmoke #1 | `8656360677da4334955bb0377d6995a2` | 3 | 3 | 0 | 0 | 18.7 s + 7.1 s + 6.8 s | `TestResults.xml` (tracker MCP travado) |
| PlayModeSmoke #2 (Play antes de Logic/SceneStatic) | `9312a5d0490146cd91cf911ab78e2b1a` | 3 | 3 | 0 | 0 | 15.3 s + 7.4 s + 7.3 s | `TestResults.xml` |
| Suite completa #1 | `b4257c28098e49b5a35075e71645411f` | 119 | 119 | 0 | 0 | ~38 s de parede | `TestResults.xml` (`result="Passed"`) |
| Suite completa #2 | `2b64a1e21bf94a298645ce0a8c91c843` | 119 | 119 | 0 | 0 | ~38 s de parede (teste da volta 17.3 s) | `TestResults.xml` (`result="Passed"`) |

Nas execucoes Play o Lucca nao morreu e fechou a 1a volta de verdade em ~15-19 s reais com timeScale 4; nenhum
`Inconclusive` foi disparado. Nenhum teste foi marcado `KnownIssue`/`Ignore`; nenhuma assercao foi enfraquecida.
Nao houve dependencia de ordem: Logic/SceneStatic/Play foram rodados em ordens diferentes e a suite inteira duas vezes.

Apos tudo: cena ativa `Assets/Scenes/HexPrototipo.unity`, `isDirty=False`, `timeScale=1`, fora do Play Mode,
sem erros no console; `git diff --stat` vazio para arquivos rastreados.

### Verificacao independente (revisor, 2026-11-14)
Reexecucao feita pelo revisor depois da entrega, no mesmo Editor:
| Execucao | Job id | Resultado | Fonte |
|---|---|---|---|
| Logic + SceneStatic | `99ebcac91798424093a094f3bc38f4f6` | 116 / 116 / 0, 2.08 s | `get_test_job` |
| PlayModeSmoke | `7fbc382dd13147c8b2d1ed08b5f3358d` | 3 / 3 / 0 (13.9 s, 6.7 s, 7.5 s) | `TestResults.xml` 17:28:31Z |
| Logic (apos destravar o rastreador) | `2a662bca410a4ae48ca239a209f3513d` | 106 / 106 / 0, 1.75 s | `get_test_job` |
Depois: cena `HexPrototipo` sem alteracoes (`isDirty=false`), console sem erros, `git diff --stat` vazio, hash do save
inalterado (`e67b637d...91e37e6`).

### Hash do save (`loopia_save.json`, SHA-256)
- Antes da suite (medido por mim antes de qualquer teste): `e67b637de08b6e56b309f694835ccc9f86060131f69020d8a46ed389691e37e6`
- Depois da suite (apos todas as execucoes, incluindo mortes e voltas em Play Mode): `e67b637de08b6e56b309f694835ccc9f86060131f69020d8a46ed389691e37e6`
- O `[SetUpFixture]` tambem registra no console `backup=... depois=...` (iguais) ao fim de cada execucao.

## 5. Achados

- Nenhum bug de gameplay novo apareceu; nada precisou de `KnownIssue`.
- Informativo (nao e falha): sete cartas em `Assets/Dados/Plataformas` estao com `modelo` nulo:
  AreaDeTreino, Basica, Cabana, CampoPacifico, Floresta, IlhaAlta, Torre (funcionam, so nao mostram item no centro).
  Os 11 assets carregam como `PlataformaDef` (o defeito historico `m_Script: {fileID: 0}` nao esta presente).
- Todos os persistent calls de UnityEvent da cena (3) tem alvo valido; `LoopDirector.aoGameOver` chama `TelaDeMorte.Mostrar`.

## 6. Caracterizacao da auditoria (nao e correcao)

`MaoDeCartasTests.Characterization_MaoCheia_ComprarFalhaEACartaDoLevelUpESimplesmentePerdida` documenta o achado
"a recompensa de level-up some quando a mao esta cheia": com 4 cartas, `Comprar()` e o caminho do level-up
(`SubiuDeNivel`) retornam sem efeito, `AoMudar` nao dispara e liberar um espaco depois nao devolve a carta. O teste
protege o comportamento atual; se a correcao (fila de recompensas) for feita depois, ele deve ser atualizado de
proposito.

## 7. PENDENTE (nao executado ou nao coberto)

- Builds (Windows/outras plataformas) e teste em build: nada disto foi feito.
- Desempenho (FPS, GC, tempo de bake do NavMesh, memoria) e proporcao 21:9.
- Corrida REAL de 4 voltas: so a 1a volta e real. A liberacao do Boss na 4a volta e verificada chamando por reflexao
  o metodo privado `LoopDirector.FechouUmaVolta` (4 vezes, mais uma 5a para provar que nao repete). Isso testa a regra do
  diretor, nao a integracao corredor -> diretor por 4 voltas seguidas, nem a sobrevivencia do Lucca.
- Resultado do teste de 1 volta depende de o Lucca sobreviver aos inimigos (aleatorio); se morrer, o teste termina
  `Inconclusive` (nao passa em silencio) e e preciso repetir.
- Boss em si, inimigos, projeteis, HUD/cartas (UI de arraste), `HexPlacementGrid`, `HexPointer`, camera, nuvens,
  cenarios de fundo, e as cenas Acampamento/Menu/Testes: sem testes.
- Substituicao real de carta (`Colocar` sobre carta existente chama `Remover`/`Destroy`, so valido em Play Mode):
  apenas a regra (`PodeColocar`) e testada em Edit Mode; os efeitos de plataforma (cura, moedas, lobos) nao sao testados.
- `Colocar` em Edit Mode depende da injecao por reflexao do campo privado `_raiz` (nome do campo e acoplamento do
  teste; se for renomeado, o teste falha com mensagem clara).
- Testes de `MaoDeCartas` e `LuccaStatus` chamam o `Awake` privado por reflexao (mesma observacao).
- Rastreador do MCP nao acompanha testes que recarregam o dominio (ver secao 2); resultados de Play vem do XML do UTF.
- Backup do save fica em `%TEMP%/loopia_tests_save.bak` (ou marca `loopia_tests_save.absent`) e so e apagado no fim
  da suite. Se uma execucao for abortada (crash do Editor), o backup antigo permanece e a proxima execucao restaura
  ELE: progresso jogado manualmente entre as duas execucoes seria sobrescrito. Apos um crash durante testes, conferir
  e apagar esses dois arquivos antes de jogar.
- Pequeno vazamento de `Mesh`/`Material` por teste de Edit Mode e evitado (destruidos no teardown), mas nao ha
  verificacao automatica de vazamentos.

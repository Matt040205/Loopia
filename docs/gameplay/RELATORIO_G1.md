# Relatório G1 — ciclo da partida, chefe, vitória e derrota

Handoff da frente de gameplay (não substitui `docs/handoff/ESTADO_ATUAL.md`, que é coordenado entre as frentes).

## Onde está
- Worktree `C:\Users\Sitr3n\Documents\Github\loopia-gameplay`, branch `sprint-3/gameplay-loop`.
- Commit-base: `85c5276` (`sprint-0/preparacao-tecnica`). Independente da pilha de arte (`sprint-1/*`).
- Nenhuma cena, prefab, material ou asset serializado foi alterado. Só scripts, testes e `docs/gameplay/`.

## O que foi feito
| Entrega | Arquivos |
|---|---|
| Auditoria G0 | `GAMEPLAY_AUDIT.md`, `BALANCE_BASELINE.md`, `GDD_V2.md`, `IMPLEMENTATION_PLAN.md` |
| Máquina de estados pura | `EstadoDaPartida.cs`, `CicloDaPartida.cs` |
| Regras do chefe | `RegrasDoChefe.cs` (alcance extra), `VarianteDoChefe.cs` (morcego ampliado, sem asset) |
| Encontro | `EncontroDoChefe.cs` (nasce 1×, alvo grande, Revoada), `BarraDeVidaDoChefe.cs`, `IndicadorDePerigoDoChefe.cs` |
| Integração no fluxo | `LoopDirector` (estados, congela voltas, recarga de flechas no chefe, vitória, `aoVencer`), `HexLoopRunner` (`contarVoltas`, `AoPassarPeloAcampamento`), `InimigoSpawner` (`pausarSpawnComum`, `CongelarTodos`, `NascerNoAnel`), `Inimigo` + `LuccaCombate` (alvo grande), `DadosDoJogador` (`vitorias`, `TentarLer`), `TelaDeMorte` (`MostrarVitoria`, texto da derrota no chefe) |
| Testes | `CicloDaPartidaTests` (Logic, 10), `RegrasDoChefeTests` (Logic, 16 com 11 sementes), `DadosDoJogadorFormatoTests` (Logic, 7), `EncontroDoChefePlayModeTests` (PlayModeSmoke, 5) |

Motivo para tocar `Inimigo.cs`/`LuccaCombate.cs` (arquivos de integração com animação/visual): o chefe precisa ser alvo
do tiro automático. Mudança mínima: propriedades novas no `Inimigo` e a regra de alcance/ângulo em `EscolherAlvo` e
`EstaAFrente`. Inimigos comuns têm alcance extra 0 → mesmo comportamento de antes. `HexPlayer.cs` não foi tocado.

## Evidências
| Verificação | Resultado | Fonte |
|---|---|---|
| Compilação dos scripts de gameplay (`Scripts/Hex` + `testes shaders`) | **0 erros, 0 avisos nos arquivos tocados** | Roslyn do próprio Unity 6000.3.10f1 (`Editor/Data/DotNetSdkRoslyn/csc.dll`) fora do Editor, contra as DLLs da Unity e dos pacotes. **Não é** a compilação do Editor |
| Compilação de `Assets/Tests/Editor` (suíte antiga + nova) | **0 erros** | idem |
| Merge com a pilha de arte (`origin/sprint-1/pass-d2-render` @ `56c6bf1`) | **sem conflito**; árvore mesclada compila (jogo, testes e `Assets/Editor`) com 0 erros | `git merge-tree --write-tree` + mesma compilação fora do Editor |
| Suíte Logic + SceneStatic + PlayModeSmoke no Editor | **PENDENTE** | o Editor aberto (pasta principal, branch de arte) pertence a outra frente; esta frente não usou o MCP |
| Console do Editor | **PENDENTE** | idem |
| Chefe em Play Mode | **PENDENTE** | não declarar funcionamento antes de rodar `EncontroDoChefePlayModeTests` e uma partida manual |

## Pendências e riscos
1. Rodar a suíte inteira (ver §Como testar). Os 5 testes Play novos montam o encontro em tempo de execução.
2. Integração de cena (PR exclusivo no Editor): `IMPLEMENTATION_PLAN.md` §G1.8. Até lá, a cena se comporta como antes
   (modo antigo: 4 voltas só disparam `AoLiberarBoss`; um aviso no console diz que não há `EncontroDoChefe`).
3. Na integração, atualizar de propósito `HexPrototipoPlayModeTests.Quatro_Voltas_LiberamOBossUmaUnicaVez` (com o
   encontro na cena a 5ª volta deixa de contar).
4. Números do chefe são hipóteses (`BALANCE_BASELINE.md` §7). Calibrar em G4.
5. `CartasUI` continua aceitando cartas depois da vitória (só bloqueia com o Lucca morto). Inofensivo; corrigir junto
   da integração de cena ou em G2.
6. `IndicadorDePerigoDoChefe` usa `Shader.Find("Universal Render Pipeline/Unlit")` com fallback para Lit/Standard;
   conferir em build que o Unlit não é removido (senão cai no Lit, que o projeto já usa).
7. Merge com a pilha de arte: `Inimigo.cs` muda em regiões diferentes (`MontarVisual` lá, propriedades aqui).

## Como testar
1. Com o Editor livre: abrir o projeto da worktree (ou fazer merge desta branch na branch de integração escolhida).
2. Test Runner → EditMode → categorias `Logic`, `SceneStatic` (via `get_test_job`), depois `PlayModeSmoke` (resultado no
   `TestResults.xml`; ver `tools/unity-mcp/README.md`).
3. Esperado: 119 antigos + 33 Logic novos + 5 PlayModeSmoke novos. Console sem erros (avisos esperados:
   "Sem EncontroDoChefe na cena" no teste antigo das 4 voltas).

## Checklist do PR
- [x] Escopo: só scripts de gameplay listados, testes e `docs/gameplay/`; `git diff --stat` revisado
- [x] `.meta` de todo arquivo novo incluído (GUIDs novos, únicos no projeto); nenhum asset `.asset` novo
- [ ] Testes: **PENDENTE** (Editor ocupado por outra frente); compilação verificada fora do Editor
- [ ] Console: **PENDENTE**
- [x] Cenas tocadas: nenhuma
- [x] Captura antes/depois: não se aplica (sem mudança visual na cena atual)
- [x] Rollback: `git revert` dos commits do G1 (sem listeners de cena para órfãos)

## Próxima sprint recomendada
1. Rodar a suíte e integrar a cena (§G1.8) — sem isso o P0 não está entregue de ponta a ponta.
2. G2: fila de recompensas do level-up (a mão inicial já começa cheia, então a carta do nível 1 se perde) e as três
   cartas de sobrevivência sobre um agregador de modificadores.
3. G4 cedo: RNG com semente, para que os números do chefe deixem de ser hipótese.

# G2 — handoff

## Status
**G2-A e G2-B implementadas e testadas no Editor; G2-C entregue isolada (sem integração no combate).**
Aguardando revisão do responsável. Nada mergeado.

| Item | Onde |
|---|---|
| Branch | `feature/gameplay-g2` (worktree `C:\Users\Sitr3n\Documents\Github\loopia-g2`) |
| Base | `origin/sprint-3/gameplay-cena` @ `0d21629` (PR #8) — o PR de G2 vai empilhado sobre o #8 |
| Commits | `765a942` fila · `59d43eb` agregador · `83f738f` cartas · `843b50e` Mirante · docs/testes |
| Testes | Logic 309, SceneStatic 35, PlayModeSmoke 15, PlayModeMedicao 1 — todos verdes (`G2_TEST_REPORT.md`) |
| Editor | Unity aberto na `loopia-g2` (porta MCP 6402). Pode ser fechado; não há cena suja |

## O que mudou (arquivos)
| Arquivo | Mudança |
|---|---|
| `Hex/MaoDeCartas.cs` | fila de direitos pendentes, `Pendentes`, `AcimaDoLimite`, `AoMudarPendentes`, `AoEntregarPendente`, `TentarEntregarPendentes`, campos `limiteDeAlertaDePendentes` (4) e `diretor` (opcional) |
| `Hex/CartasUI.cs` | rótulo `+N pendentes` acima do canto direito da barra (texto em cache) |
| `Hex/ModificadoresDoLucca.cs` | novo: struct imutável |
| `Hex/PlataformaDef.cs` | ganchos `BonusDeFlechas`, `BonusDeCuraPorCoracao`, `Modificadores(p)` |
| `Hex/PlataformaSimplesDef.cs` | campos `bonusDeFlechas`, `bonusDeCuraPorCoracao` |
| `Hex/MapaDePlataformas.cs` | `Recalcular` soma o struct; `Modificadores`; mantém `BonusDePvTotal`/`BonusDeDanoTotal` |
| `Hex/LuccaStatus.cs` | `DefinirModificadores`, `Modificadores`, `FlechasMaximoTotal`, `BonusDeFlechasMaximo`, `BonusDeCuraPorCoracao`, `CurarComCoracao`; recargas no teto efetivo; `DefinirBonusDePvMaximo` virou atalho |
| `Hex/PlataformaRecompensaDef.cs` | coração usa `CurarComCoracao` |
| `Hex/LoopDirector.cs` | recarga do acampamento no chefe e HUD usam `FlechasMaximoTotal` (2 linhas) |
| `Hex/PlataformaRelevoDef.cs` | campos do Mirante (`ehMirante`, `bonusDeAlcance` 1, `tiroEmQualquerDirecao`) |
| `Hex/RegrasDoMirante.cs`, `Hex/MiranteDoLucca.cs` | novos (G2-C) |
| `Dados/Plataformas/{Santuario,FonteRestauradora,OficinaDeArqueiro}.asset` | novos, com `.meta` |
| `Tests/Editor/…` | 5 arquivos novos de teste + `CartaDeTesteControlavel`; `MaoDeCartasTests` e `EncontroDoChefePlayModeTests` ajustados de propósito |
**Não tocados:** cenas, prefabs, materiais, shaders, URP, câmera, luz, `LuccaCombate.cs`, `HexPlayer.cs`, `Projetil.cs`,
`docs/handoff/ESTADO_ATUAL.md` (a frente de personagem também o edita; ver "Riscos").
Arquivos que o Unity mexeu por fora e **não** foram commitados: `.vscode/settings.json`,
`Dressing_Vegetacao.mat`, `ProjectSettings/TimeManager.asset`, `.meta` de `ArvoreGrande`/`Morcegp` (mesma lista do G1).

## Decisões
1. **Fila guarda direitos, não cartas**; a carta é sorteada na entrega entre as disponíveis (sem RNG novo; semente é G4).
2. **Sem teto rígido** na fila: o "limite configurável" do GDD virou limite **suave** (aviso). Divergência registrada
   em `REWARD_QUEUE_DESIGN.md`; um teto rígido obrigaria descartar.
3. **Vitória/derrota/Lucca morto:** a fila não entrega, mas conta o direito.
4. **Flechas seguem a regra do PV:** teto sobe → atual sobe a diferença; teto desce → só limita.
5. **Cartas novas fora do caminho**, substituíveis, sem pré-requisito, sem marcador próprio (cor da ilha).
6. **Mirante** só numa Ilha alta **levantada** (a carta continua exigindo Mola); +1 casa e qualquer direção, por
   asset. Regra do chefe independente.

## Pendências (em ordem)
1. **Pôr as 3 cartas no baralho da cena** — passo de cena, pelo Editor: em `HexPrototipo`, objeto `Cartas`,
   `MaoDeCartas.baralho` += `Santuario`, `FonteRestauradora`, `OficinaDeArqueiro`. **Bloqueado** (AGENTS §4.1): o PR #8
   (aberto) e a frente de personagem (`feature/lucca-locomotion-2c2`, não publicada) mexem na mesma cena. Fazer num
   PR de cena isolado depois que o #8 entrar e a frente de personagem publicar a sua. Atualizar o SceneStatic que
   conta o baralho, se houver, e jogar uma partida manual com as cartas.
2. **Integrar o Mirante no `LuccaCombate`** (ver abaixo) quando a frente de personagem publicar a versão estável.
3. Captura do indicador `+N pendentes` e das ilhas das cartas novas (marcadores ficam com a frente gráfica).
4. Medir o Mirante com `MetricasDaPartida` (flechas que acertam / abates por volta, com e sem Mirante), como pede o GDD.

## Plano de integração do Mirante (mudança mínima no `LuccaCombate`)
Contra a versão da frente de personagem (`feature/lucca-locomotion-2c2`, que já reescreveu `Update`/`Disparar` e
criou `PodeDispararEm`):
```csharp
// campo opcional + fallback, como o resto do arquivo
public MiranteDoLucca mirante;            // Start: if (mirante == null) mirante = GetComponent<MiranteDoLucca>();
                                          //        if (mirante == null) mirante = gameObject.AddComponent<MiranteDoLucca>();
int AlcanceAtual => mirante != null ? mirante.AlcanceDoTiro(alcance) : alcance;

// EscolherAlvo e PodeDispararEm: trocar `alcance` por `AlcanceAtual` em RegrasDoChefe.DentroDoAlcance(...)
// EstaAFrente: if (RegrasDoMirante.IgnoraDirecao(mirante != null && mirante.Ativo,
//                    mirante != null && mirante.TiroEmQualquerDirecao, inimigo.AlvoDeQualquerAngulo)) return true;
```
- `AddComponent` em tempo de execução evita editar a cena. `AlvoAtual`, `AoDisparar`, dano e munição não mudam.
- O alcance extra do chefe é calculado pelo `EncontroDoChefe` sobre o `alcance` base: no Mirante o chefe continua
  alcançável (teste `Regra_CoexisteComOChefe_*`).
- Testes a acrescentar na integração: Play com Lucca parado numa Ilha alta (Mola antes), morcego a 4 casas de lado →
  é escolhido; fora da ilha → não é.

## Riscos de integração
| Risco | Mitigação |
|---|---|
| Merge com `LuccaCombate` da frente de personagem | G2 não tocou o arquivo; integração do Mirante é um passo separado com o plano acima |
| `HexPrototipo.unity` em 3 linhas (PR #8, personagem, cartas) | G2 não editou cena; o baralho entra em PR de cena próprio, depois |
| Save e `TestResults.xml` **compartilhados entre Editors** (`%TEMP%\loopia_tests_save.bak`, `persistentDataPath`) | rodar suítes de Play de uma frente por vez; conferir o hash do save antes/depois (ver incidente em `G2_TEST_REPORT.md`). Correção sugerida (fora de G2): backup por projeto (`Application.dataPath` no nome) |
| `MaoDeCartas` com campos novos serializados | padrões seguros (limite 4, diretor vazio → usa o do mapa); a cena não precisa ser reserializada |
| `IlhaAlta.asset` com campos novos | lidos com o padrão do código (+1, qualquer direção) até o asset ser salvo de novo |

## Futuro (G3/G4)
- G3 pode reaproveitar `ModificadoresDoLucca` como saída dos status (somar ao total do mapa antes de aplicar).
- G4: semente no sorteio da fila; decidir se o excedente da fila vira outra recompensa (explícita); tetos por carta
  (ex.: no máx. N Fontes) se as medições pedirem; métrica de colocações por volta (GDD §4).

## Rollback
`git revert` dos commits em ordem inversa (`843b50e`, `83f738f`, `59d43eb`, `765a942`). Nenhuma cena muda, então
não há listeners órfãos; os 3 assets novos não são referenciados por cena.

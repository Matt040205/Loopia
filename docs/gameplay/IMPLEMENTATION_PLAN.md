# Plano de implementação — gameplay

Branch `sprint-3/gameplay-loop` (worktree `../loopia-gameplay`), base `85c5276` (`sprint-0/preparacao-tecnica`).
Um PR por sprint. Só scripts, testes e documentação até a integração exclusiva no Editor (§G1.8).

---
## G1 — Ciclo da partida, chefe, vitória e derrota (P0, esta entrega)

### G1.1 Arquivos
| Arquivo | Tipo | O quê |
|---|---|---|
| `Hex/EstadoDaPartida.cs` | novo | enum `Preparando, Correndo, Chefe, Vitoria, Derrota` |
| `Hex/CicloDaPartida.cs` | novo | máquina de estados pura (C#, sem Unity): transições válidas, terminais, evento de mudança |
| `Hex/RegrasDoChefe.cs` | novo | regras puras: alcance extra para o chefe cobrir o anel inteiro |
| `Hex/VarianteDoChefe.cs` | novo | parâmetros serializáveis da variante ampliada + `Montar(InimigoDef base)` |
| `Hex/EncontroDoChefe.cs` | novo | nasce o chefe uma vez, aplica a regra de alvo grande, Revoada, eventos |
| `Hex/BarraDeVidaDoChefe.cs` | novo | barra de vida IMGUI (mesma tecnologia da HUD atual) |
| `Hex/IndicadorDePerigoDoChefe.cs` | novo | aviso visual da Revoada (só apresentação, ouve eventos) |
| `Hex/LoopDirector.cs` | alterado | dono do estado; transições; congela a contagem; recarga de flechas no chefe; vitória |
| `Hex/HexLoopRunner.cs` | alterado | `contarVoltas` + evento `AoPassarPeloAcampamento` |
| `Hex/InimigoSpawner.cs` | alterado | `pausarSpawnComum`, `CongelarTodos()`, `NascerNoAnel(def)` públicos |
| `Hex/Inimigo.cs` | alterado (integração) | `AlcanceExtraComoAlvo`, `AlvoDeQualquerAngulo`, `TornarAlvoGrande(int)` |
| `Hex/LuccaCombate.cs` | alterado (integração) | a regra de alcance/ângulo consulta o alvo grande |
| `Hex/DadosDoJogador.cs` | alterado | campo `vitorias` (compatível com saves antigos), `RegistrarVitoria()`, `TentarLer(json)` |
| `Hex/TelaDeMorte.cs` | alterado | `MostrarVitoria()`; texto correto para derrota no chefe |
| `Tests/Editor/CicloDaPartidaTests.cs` | novo | Logic |
| `Tests/Editor/RegrasDoChefeTests.cs` | novo | Logic (inclui 11 sementes de mapa) |
| `Tests/Editor/DadosDoJogadorFormatoTests.cs` | novo | Logic (JSON antigo/novo/corrompido, sem tocar no save real) |
| `Tests/Editor/EncontroDoChefePlayModeTests.cs` | novo | PlayModeSmoke (monta o encontro em tempo de execução, sem salvar cena) |
Motivo para tocar `Inimigo.cs` e `LuccaCombate.cs`: o chefe precisa ser alvo do tiro automático; a regra de alvo vive
no `LuccaCombate`. Mudança só em `EscolherAlvo`/`EstaAFrente` e propriedades novas no `Inimigo`; inimigos comuns
ficam com alcance extra 0 e o comportamento antigo. A pilha de arte muda `Inimigo.MontarVisual` (outra região).

### G1.2 API e eventos
- `LoopDirector`: `Estado`, `Terminou`, `DerrotaNoChefe`, `event Action<EstadoDaPartida,EstadoDaPartida> AoMudarEstado`,
  `event Action AoVencer`, `UnityEvent aoVencer` (Inspector), campo `encontro`, `recarregarFlechasNoChefe`.
  Mantidos: `VoltasCompletas`, `MultiplicadorDosInimigos`, `BossLiberado`, `GameOver` (agora `Estado == Derrota`),
  `AoCompletarVolta`, `AoLiberarBoss`, `AoPerder`, `aoGameOver`.
- `HexLoopRunner`: `bool contarVoltas = true`, `event Action AoPassarPeloAcampamento` (toda entrada no acampamento,
  contando ou não). `ReiniciarContagem` religa a contagem.
- `EncontroDoChefe`: `PodeIniciar(out motivo)`, `Iniciar()`, `Chefe`, `Iniciado`, eventos C# `AoChefeNascer(Inimigo)`,
  `AoAvisarEspecial(float)`, `AoUsarEspecial`, `AoChefeDerrotado`; UnityEvents `aoChefeNascer`, `aoChefeDerrotado`.

### G1.3 Fluxo
1. `LoopDirector.Start` inscreve e passa `Preparando → Correndo`.
2. Cada `AoCompletarVolta` em `Correndo`: igual a hoje (restaura, ×1,1, recorde).
3. Na volta 4: `BossLiberado`, `AoLiberarBoss` (como hoje). Se existir `EncontroDoChefe` e `PodeIniciar`:
   `Correndo → Chefe`, `corredor.contarVoltas = false`, spawn comum pausado, `encontro.Iniciar()`.
   Sem encontro na cena: **modo antigo** (aviso no console, a contagem continua) — evita prender a partida num
   estado de chefe sem chefe enquanto a cena não é integrada.
4. Em `Chefe`, `AoPassarPeloAcampamento` recarrega flechas (configurável). Voltas, escala e cartas de recompensa não.
5. Chefe morre → `Chefe → Vitoria` (uma vez): corredor para, inimigos congelam, save (`vitorias`+1), `aoVencer`.
6. Lucca morre em `Preparando/Correndo/Chefe` → `Derrota` (uma vez), como hoje. Estados terminais ignoram o resto.

### G1.4 Dependências
Nenhum pacote novo. Usa `InimigoSpawner.Nascer` (UnityEvent `aoAtacar` → `aoInimigoAtacar` → `LuccaStatus.ReceberDano`
preservado), `HexLoopGenerator.CentroDoMiolo`, `LuccaCombate.alcance`, o `InimigoDef` do morcego.

### G1.5 Riscos
| Risco | Mitigação |
|---|---|
| Cena sem o encontro → nada muda (modo antigo) | intencional até a integração; testes Play montam o encontro em tempo de execução |
| Ao integrar, o teste `Quatro_Voltas_LiberamOBossUmaUnicaVez` (que espera a 5ª volta contada) passa a falhar | atualizar o teste **de propósito** no PR de integração (a 5ª volta não conta mais) |
| Chefe inalcançável em algum anel | alcance extra calculado do anel real; teste com 11 sementes |
| Morcegos da Revoada roubam todas as flechas | recarga de flechas no acampamento durante o chefe; números em G4 |
| Merge com a pilha de arte em `Inimigo.cs` | regiões diferentes; conferir no merge e rodar `MateriaisCompartilhadosTests` |
| Material do aviso de perigo | um material estático criado uma vez, sem `renderer.material` |

### G1.6 Testes
Logic: transições válidas/ inválidas, terminais exclusivos e únicos; alcance extra cobre todo o anel (11 sementes) e é
0 quando já cabe; variante do chefe deriva do morcego sem alterar o asset base; JSON antigo sem `vitorias` carrega 0,
JSON novo faz ida e volta, corrompido falha sem exceção.
PlayModeSmoke (corredor real → diretor, via `AoEntrarNoHexagono` do corredor): 4 voltas iniciam exatamente um
encontro; a 5ª passagem não conta; o chefe nasce uma vez; flechas reais do Lucca causam dano ao chefe (alvo escolhido
pelo `LuccaCombate`); morte do chefe → vitória 1×; morte do Lucca depois da vitória não vira derrota; morte do Lucca no
chefe → derrota e o chefe morrendo depois não vira vitória; vitória registra no save e o retorno ao acampamento
preserva moedas/recorde/vitórias; tela de resultado aparece pelo UnityEvent `aoVencer`.

### G1.7 Critérios de aceite
Todos os testes acima e a suíte anterior passam no Editor (job id + XML); console sem erros novos; nenhuma cena,
prefab ou asset alterado neste PR.

### G1.8 Integração pendente (PR de cena, exclusivo no Editor)
1. Em `HexPrototipo`, criar o objeto `Encontro do chefe` com `EncontroDoChefe`, `BarraDeVidaDoChefe` e
   `IndicadorDePerigoDoChefe`. Referências podem ficar vazias (fallback por `FindFirstObjectByType`).
2. `LoopDirector.encontro` → esse objeto (opcional, por clareza no Inspector).
3. `LoopDirector.aoVencer` → `TelaDeMorte.MostrarVitoria` (persistent call, como o `aoGameOver`).
4. Opcional: criar `Assets/Dados/Chefe.asset` (`InimigoDef`) e pôr em `EncontroDoChefe.chefe`; vazio = variante do
   morcego montada em tempo de execução.
5. Atualizar `HexPrototipoPlayModeTests.Quatro_Voltas_LiberamOBossUmaUnicaVez` e o `SceneStatic` (novo listener
   esperado em `aoVencer`).
6. Rodar a suíte e jogar uma partida até a vitória e outra até a derrota, com capturas.

### G1.9 Rollback
`git revert` dos commits de G1. Como a cena não muda neste PR, reverter não deixa listeners órfãos. Depois da
integração, reverter o PR de cena antes do de código.

---
## G2 — Cartas de sobrevivência e fila de recompensas (P1, depois da integração de G1)
- `ModificadoresDoLucca` (struct imutável somada pelo `MapaDePlataformas.Recalcular`): PV máx., dano, flechas máx.,
  cura extra por coração. `PlataformaDef` ganha ganchos virtuais `BonusDeFlechas`/`BonusDeCuraPorCoracao` (padrão 0):
  sem switch por tipo.
- `LuccaStatus`: `DefinirModificadores` (substitui `DefinirBonusDePvMaximo` mantendo-o como atalho), `FlechasMaximoTotal`.
- `PlataformaRecompensaDef.AoPlayerPassar`: cura = `cura + bônus` só quando `cura > 0`.
- 3 assets novos (Santuário, Fonte, Oficina) com `.meta`, criados pelo Editor no PR de integração.
- `MaoDeCartas`: fila `_pendentes` (máx. configurável), drenada em `Gastar`/`DarCarta`; evento `AoMudarPendentes`;
  CartasUI mostra o número. Atualizar o teste de caracterização (renomear para comportamento novo).
- Ilha Alta "Mirante" (hipótese): `LuccaCombate` lê `BonusDeAlcance` da casa atual via `MapaDePlataformas`.
- Testes: Logic para cada gancho e para a fila; Play para colocar/remover e ver o PV/flechas mudarem.

## G3 — Status effects (P1)
- `StatusEffectDefinition` (SO), `ActiveStatusEffect` (classe), `StatusEffectController` (componente por alvo,
  `[DisallowMultipleComponent]`), `StatModifier`; política de acúmulo como enum (`Renovar`, `AcumularAte`, `Ignorar`).
- Dano por pulso via interface `IRecebeDano` implementada por `LuccaStatus` e `Inimigo` (sem switch).
- Testes Logic com relógio injetado (`Func<float>`), sem esperar tempo real.

## G4 — Balanceamento (P1)
- RNG injetável (`IRandom`/`System.Random` com semente) em `InimigoSpawner` e `MaoDeCartas`.
- Coletor de métricas de partida (componente só de teste) e cenários fixos; relatório em `BALANCE_BASELINE.md`.

# G2-B — agregador de modificadores e cartas de sobrevivência

## `ModificadoresDoLucca`
`readonly struct` em `Assets/Scripts/Hex/ModificadoresDoLucca.cs`. Quatro campos, soma explícita (`+` e `Somar`),
`Zero`, igualdade por valor. Não é framework de atributos: é o resumo do que as plataformas do mapa somam agora.

| Campo | Unidade | Quem lê |
|---|---|---|
| `PvMaximo` | PV inteiros | `LuccaStatus.PvMaximoTotal` |
| `Dano` | fração (0,08 = +8 %) | `MapaDePlataformas.BonusDeDanoTotal` → `LuccaCombate.DanoDoTiro` (inalterado) |
| `FlechasMaximo` | flechas inteiras | `LuccaStatus.FlechasMaximoTotal` |
| `CuraPorCoracao` | PV extra por coração coletado | `LuccaStatus.CurarComCoracao` |

## Fluxo (uma fonte, sem cálculo paralelo)
```
PlataformaDef.Modificadores(p)  ← compõe os ganchos virtuais BonusDePv / BonusDeDano / BonusDeFlechas / BonusDeCuraPorCoracao
        │ (somados por plataforma)
MapaDePlataformas.Recalcular()  → Modificadores (total)  → BonusDePvTotal, BonusDeDanoTotal (mantidos, derivados)
        │
LuccaStatus.DefinirModificadores(total)
```
`Recalcular` continua rodando em toda colocação/remoção (e em `LimparTudo`), então substituir ou remover uma
plataforma tira o bônus na hora: não existe estado acumulado fora da soma. Plataformas de vizinhança (Cabana,
Área de Treino) continuam iguais: só sobrescrevem `BonusDePv`/`BonusDeDano`.

`PlataformaDef` ganhou `BonusDeFlechas` e `BonusDeCuraPorCoracao` (virtuais, padrão 0). `PlataformaSimplesDef`
ganhou os campos `bonusDeFlechas` e `bonusDeCuraPorCoracao` (padrão 0, então os assets antigos não mudam).
Nenhum switch por tipo ou nome de carta.

## `LuccaStatus`
| Membro | Regra |
|---|---|
| `DefinirModificadores(m)` | valores negativos viram 0; dispara `AoMudar` só se algo mudou |
| `DefinirBonusDePvMaximo(int)` | mantido como atalho: troca só o PV do conjunto atual |
| `BonusDePvMaximo`, `PvMaximoTotal` | mantidos |
| `BonusDeFlechasMaximo`, `FlechasMaximoTotal` | novos: `flechasMaximo + bônus` |
| `BonusDeCuraPorCoracao` | novo |
| `CurarComCoracao(cura)` | `cura > 0` → `Curar(cura + bônus)`; não ressuscita (o `Curar` já guarda) |

**PV (semântica de G1 preservada):** o teto sobe → o PV atual sobe a mesma diferença (se vivo). O teto desce → o PV
atual é limitado ao novo teto; nada de cura ou dano extra.
**Flechas (mesma semântica):** o teto sobe → as flechas atuais sobem a mesma diferença (se vivo), para a Oficina
mudar o HUD na hora (critério "impacto em ≤ 1 volta"). O teto desce → as flechas atuais são limitadas ao novo teto.
Morto: só o teto muda.

### Locais que usavam `flechasMaximo` direto (auditados)
| Local | Agora |
|---|---|
| `LuccaStatus.Awake` | `FlechasMaximoTotal` (igual ao base: o mapa ainda não somou nada) |
| `LuccaStatus.RestaurarVidaEMunicao` (fim de volta) | recarrega até `FlechasMaximoTotal` |
| `LuccaStatus.GanharFlechas` (carta Flechas) | limitado a `FlechasMaximoTotal` |
| `LoopDirector.PassouPeloAcampamento` (recarga no chefe) | `GanharFlechas(FlechasMaximoTotal)` |
| `LoopDirector.OnGUI` (HUD) | mostra `FlechasMaximoTotal` |
| `EncontroDoChefePlayModeTests` | compara com `FlechasMaximoTotal` |
`LuccaCombate` não lê o teto (só `FlechasAtuais`/`GastarFlecha`): **não foi tocado**.

## Cartas novas (hipóteses de teste, como no GDD v2)
Todas `PlataformaSimplesDef`, `onde = ForaDoCaminho` (construções, como Campo Pacífico e Torre: o GDD v2 compara o
Santuário ao Campo Pacífico; nenhuma é coletável no caminho), substituíveis, sem marcador novo (arte é da frente
gráfica; a ilha fica com a cor da carta).

| Carta | Asset | Efeito | Observações |
|---|---|---|---|
| Santuário | `Santuario.asset` | `bonusDePv = 3` | igual ao Campo Pacífico, outro valor; soma com Cabana/Campo |
| Fonte Restauradora | `FonteRestauradora.asset` | `bonusDeCuraPorCoracao = 2` | só corações (`PlataformaRecompensaDef` com `cura > 0`); 5 → 7 com uma Fonte, 9 com duas (soma); não afeta a recarga do acampamento nem `Curar` genérico; não ressuscita |
| Oficina de Arqueiro | `OficinaDeArqueiro.asset` | `bonusDeFlechas = 2` | 15 → 17 → 19; vale na recarga de volta, na carta Flechas e na recarga do acampamento no chefe; não muda dano nem cadência |

Múltiplas cópias somam (regra do agregador). Não há teto por carta nesta sprint (G4 decide se precisa).

## Integração na cena (pendente, ver `G2_HANDOFF.md`)
As cartas só aparecem numa partida normal quando entrarem no `baralho` da `MaoDeCartas` da `HexPrototipo` — passo de
cena, bloqueado enquanto o PR #8 e a frente de personagem mexem na mesma cena (AGENTS §4.1).

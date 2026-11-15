# Auditoria de gameplay (Sprint G0)

Frente: Gameplay Engineering. Branch `sprint-3/gameplay-loop`, worktree `../loopia-gameplay`.
Commit-base: `85c5276` (`sprint-0/preparacao-tecnica`, suíte de caracterização com 119 testes).
Data: 2026-11-15. Método: leitura do código em `Assets/Scripts/Hex/`, dos assets em `Assets/Dados/`, dos valores
serializados em `Assets/Scenes/HexPrototipo.unity` (só leitura do YAML) e dos testes em `Assets/Tests/Editor/`.
**Nada desta auditoria foi medido em Play Mode.** O Editor estava aberto na pasta principal (branch de arte
`sprint-1/pass-d2-render`) e não foi usado por esta frente.

## 0. Escolha da base
- `main` (`0b9513e`) não tem a Sprint 0 nem os passes de arte.
- A pilha de arte (`sprint-1/*`, 8 PRs empilhados, nenhum na `main`) mexe em `Inimigo.cs` (material do corpo),
  `Plataforma.cs`, `PlataformaDef.cs` e `Projetil.cs`, e nunca em `LoopDirector`, `HexLoopRunner`, `LuccaStatus`,
  `LuccaCombate`, `InimigoSpawner`, `MaoDeCartas`, `MapaDePlataformas`, `DadosDoJogador` nem `TelaDeMorte`.
- Por isso a base é a `sprint-0/preparacao-tecnica`, como recomendado: o PR de gameplay não depende da pilha de arte,
  e as duas linhas se juntam por merge de 3 vias sem tocar nas mesmas linhas (o `Inimigo.cs` muda em regiões
  diferentes; ver §6).
- As duas cópias locais extras da branch de arte (`ea12ae7` `_Recovery`, `08d4a78` `.meta` de FBX) não entram aqui.

## 1. Legenda
| Rótulo | Significado |
|---|---|
| **IT** | Implementado e coberto por teste automatizado existente |
| **I** | Implementado, sem teste (comportamento lido no código) |
| **P** | Implementado parcialmente |
| **A** | Ausente |
| **PD** | Problema de design (funciona como escrito, mas prejudica decisões/ritmo) |
| **PT** | Problema técnico (bug, risco de regressão, acoplamento) |
| **IN** | Ideia nova, **não aprovada** (só registrada; está no GDD v2 como proposta) |

## 2. Sistemas
### 2.1 Fluxo da partida
| Item | Rótulo | Evidência |
|---|---|---|
| Acampamento (build 0) → `HexPrototipo` (build 1) por `TelaDoAcampamento.NovaJornada` | I | `TelaDoAcampamento.cs:49` |
| Mapa no `Awake` do gerador (ordem -150), bake (ordem -100), Lucca anda sozinho | IT | `HexPrototipoPlayModeTests` (1) |
| Estados da partida | **A** | Não há máquina de estados: só `bool BossLiberado` e `bool GameOver` em `LoopDirector`. Não existe "preparando", "chefe" nem "vitória". |
| Retorno ao acampamento | P | Só pela tela de morte (`TelaDeMorte.VoltarAoAcampamento`). Não há caminho de vitória. |

### 2.2 Contagem de voltas
| Item | Rótulo | Evidência |
|---|---|---|
| Volta fecha ao **entrar** de novo no acampamento | IT | `HexLoopRunner.AoEntrarNoHexagono`; 1 volta real no teste Play (1) |
| `LoopDirector.VoltasCompletas` acompanha o corredor | IT | teste Play (1) |
| Fechar volta restaura PV e flechas (não ressuscita) | IT | `LuccaStatus.RestaurarVidaEMunicao`; testes Logic e Play (2) |
| Recorde de voltas salvo a cada volta e na morte | I | `LoopDirector.FechouUmaVolta`/`LuccaMorreu` |
| 4 voltas seguidas reais | **A (teste)** | o teste Play (2) chama `FechouUmaVolta` por reflexão; integração corredor→diretor por 4 voltas não é testada |
| A contagem **continua** depois da 4ª volta (5ª, 6ª...) | PT | não há parada; ver 2.7 |

### 2.3 Spawn de inimigos
| Item | Rótulo | Evidência |
|---|---|---|
| Morcego a cada 5 s, até 6 vivos, numa casa do anel a ≥ 3 casas do Lucca e sem outro inimigo | I | `InimigoSpawner.Update`/`NascerEmCasaSorteada`; valores da cena |
| Lobo só pela carta Floresta (1 por casa, reposto a cada volta, não acumula) | I | `PlataformaFlorestaDef.Povoar`; `nascerLobosPorTempo=0` na cena |
| Ataques via `Inimigo.aoAtacar` → `InimigoSpawner.aoInimigoAtacar` → `LuccaStatus.ReceberDano` (UnityEvent no Inspector) | IT (fio) / I (efeito) | a cena tem o persistent call (teste SceneStatic); o dano em si não é testado |
| Na morte do Lucca: spawner para e congela os vivos | I | `InimigoSpawner.Congelar` (privado, só pelo PV) |
| API para pausar/retomar o spawn por outro motivo (chefe, vitória) | **A** | não existe |
| Sorteios com `UnityEngine.Random` global, sem semente | PT | impede reproduzir uma partida para balanceamento (§2.4) |

### 2.4 Progressão de dificuldade
| Item | Rótulo | Evidência |
|---|---|---|
| Multiplicador composto `1,10^voltas` em PV, ATK e XP | IT | testes Play (1) e (2) |
| Aplicado **só no nascimento**: inimigos já vivos não ficam mais fortes | I | `Inimigo.Nascer` |
| Única alavanca de dificuldade é o multiplicador; quantidade (6), intervalo (5 s) e alcance não mudam por volta | PD | `InimigoSpawner`. Na 4ª volta o multiplicador é só ×1,331 (no chefe, ×1,4641) |
| Pontos de corte de flechas (cálculo, não medição): morcego 50/55/61/67/73 PV nas voltas 0–4; com 35 de dano são 2 flechas até a volta 3 e **3 flechas a partir de 73 PV**; com uma Torre (38 de dano) volta a 2 | PD (bom) | `BALANCE_BASELINE.md` §3. É o tipo de decisão que as cartas deveriam provocar; hoje é acidental |

### 2.5 Balanceamento inicial (valores conferidos no código/asset/cena)
Lucca 50 PV, 35 ATK, 15 flechas, tiro a cada 0,8 s, alcance 3 casas; morcego 50 PV / 5 ATK / 50 XP / 1,6 s /
alcance 2; lobo 100 PV / 15 ATK / 100 XP / 2,2 s / mesma casa; XP 100 no 1º nível, ×1,75 por nível.
Batem com o pedido (50/35/15, 50/5, 100/15, ~10 %). Detalhe e derivados: `BALANCE_BASELINE.md`.

### 2.6 Economia de flechas
| Item | Rótulo | Evidência |
|---|---|---|
| 1 flecha por tiro; sem flecha ele segue andando e o intervalo não reinicia | I | `LuccaCombate.Update` |
| Recarga total ao fechar a volta; carta Flechas +10 por passagem, recarrega a cada volta; teto `flechasMaximo` (15) | I | `GanharFlechas` limita ao máximo |
| A carta Flechas (+10) com teto 15 só vale se o Lucca já gastou ≥ 10 | PD | o teto corta a recompensa; reforça a proposta "Oficina de Arqueiro" (+máx.) |
| Flechas em voo depois da morte do Lucca ainda causam dano e dão XP | PT (menor) | `Projetil` não olha o estado da partida; `GanharXp` não checa `EstaVivo` |

### 2.7 Cartas disponíveis
11 cartas (`Assets/Dados/Plataformas`), sorteio **uniforme** do baralho (as 11, incluindo Básica e Ilha Alta).
| Item | Rótulo | Evidência |
|---|---|---|
| Regras de colocação e motivos | IT | `PlacementRulesTests` (11) |
| Efeitos (cura, flechas, moedas, lobos, bônus de PV/dano, relevo) | I | só a regra é testada; os efeitos não |
| Cartas não custam nada; o único limite é a mão de 4 | PD | o ritmo vem só do XP |
| Básica no baralho uniforme (efeito nulo) | PD | carta morta em 1/11 das compras |
| **Ilha Alta**: exige Mola, eleva 2,2 u e rebake do NavMesh, **sem recompensa** | PD | `PlataformaRelevoDef` só tem `altura`; aparece no baralho **e** como `cartaAoLiberar` |

### 2.8 Recompensas por nível
| Item | Rótulo | Evidência |
|---|---|---|
| 1 carta por nível (`cartasPorNivel=1`) | IT | `MaoDeCartasTests.SubiuDeNivel_ComEspaco_*` |
| Mão cheia descarta a carta do level-up | IT (caracterização) | `Characterization_MaoCheia_*` |
| **A mão inicial já começa cheia**: `cartasIniciais=4` com 4 garantidas (Mola, Coração, Moedas, Flechas). Quem não jogar carta antes do 1º nível (100 XP = 2 morcegos) perde a recompensa | PD (grave) | valores da cena. Agrava o achado acima |
| `cartaAoLiberar` (Ilha Alta) é tentada em todo `Update` | PT (menor) | polling barato, mas acopla a mão ao estado do mapa |

### 2.9 Condições de vitória e derrota
| Item | Rótulo | Evidência |
|---|---|---|
| Derrota: PV 0 → `GameOver`, `AoPerder` 1×, corredor para, `aoGameOver` → `TelaDeMorte.Mostrar` | IT | teste Play (3) |
| Boss: 4 voltas disparam `AoLiberarBoss` uma vez | IT | teste Play (2) |
| `AoLiberarBoss` não tem assinante; nenhum chefe nasce | **A** | `grep` em `Scripts/Hex` |
| Vitória | **A** | não existe estado, evento, tela nem save |
| `Boss.prefab` legado | incompatível | `Assets/Modelos/PreFabs/Boss.prefab` usa `InimigoBase` (namespace global, legado) e só malhas primitivas; não serve ao `Loopia.Hex` |
| Chefe no centro do miolo seria alcançável pelo tiro? | **Não** (analítico) | `LuccaCombate` usa `HexCoord.Distance(player, inimigo.Casa) ≤ 3`. Num anel perfeito de raio R (4–6), toda casa do anel fica a R do centro; as deformações movem casas em ±1. Logo, com alcance 3, o centro fica fora do alcance em quase todo o anel. Além disso `somenteAFrente` usa produto escalar com o `forward`: andando em volta do centro, o chefe fica de lado (produto ≈ 0) e o tiro piscaria |

### 2.10 Persistência entre partidas
| Item | Rótulo | Evidência |
|---|---|---|
| Singleton `DadosDoJogador`, JSON em `persistentDataPath/loopia_save.json`, só `moedas` e `recordeDeVoltas` | I | `DadosDoJogador.Save` |
| Save ilegível volta ao padrão sem travar | I | `Carregar` (try/catch) |
| Sem `saveVersion`; campos novos são compatíveis porque `JsonUtility` deixa o padrão (0) para chaves ausentes | I | comportamento do `JsonUtility` |
| Moedas gravadas a cada coleta (1 escrita de arquivo por moeda) | PT (menor) | `AdicionarMoedas` → `Salvar` |
| Isolamento do save nos testes (backup/restauração + hash) | IT | `TestSupport.SaveIsolado`, `GuardaDaSuite` |

### 2.11 Alcance e combate automático
| Item | Rótulo | Evidência |
|---|---|---|
| Alvo: inimigo vivo mais próximo (em casas) a ≤ 3, à frente (produto escalar > 0) ou na mesma casa | I | `LuccaCombate.EscolherAlvo` |
| Dano = ATK × (1 + bônus das plataformas), arredondado | I | `DanoDoTiro` |
| Flecha teleguiada; some se o alvo morrer; vida máxima 5 s | I | `Projetil` |
| Morcego: mergulho só acerta se chegar a 0,45 u do peito antes de 1,2 s | I | `Inimigo.AtualizarMergulho` |
| Inimigos de lado/atrás são ignorados enquanto atacam | PD | o morcego mergulha de trás e não é alvo; coerência visual fraca |
| Desempate por distância em **casas** (inteiro): o primeiro da lista vence; não considera vida nem ameaça | PD (menor) | |

## 3. Problemas técnicos priorizados
1. **Sem estado de partida e sem fim** (P0): após 4 voltas o jogo segue indefinidamente. → G1.
2. **Chefe inalcançável** pela regra atual de alvo (P0): exige regra de alvo para inimigos grandes. → G1.
3. **Recompensa de level-up perdida** + mão inicial cheia (P1). → G2 (fila de recompensas).
4. Spawner sem pausa explícita (P0, necessário ao chefe/vitória). → G1.
5. Sem RNG com semente para spawn e compra (P1; bloqueia simulação reprodutível). → G4.
6. Flechas em voo e XP após a morte (menor). → G1 trata só o que afeta vitória/derrota (estado terminal ignora eventos).

## 4. Problemas de design priorizados
1. Mão inicial cheia anula a recompensa do 1º nível.
2. Dificuldade = só multiplicador; pressão real cresce pouco em 4 voltas.
3. Ilha Alta sem recompensa.
4. Teto de flechas corta a carta Flechas.
5. Básica como carta sorteável.
6. Alvo só à frente ignora ameaças reais.

## 5. Ideias registradas, não aprovadas (IN)
Ver `GDD_V2.md` §4: Santuário/Fonte/Oficina (G2), Ilha Alta como mirante, status effects (G3), dia/noite e estações
(depende do Environment Director), árvore permanente, flechas especiais e equipamentos.

## 6. Conflitos com as outras frentes
| Arquivo | Frente que também mexe | Situação |
|---|---|---|
| `Inimigo.cs` | Arte (pilha `sprint-1`: material compartilhado em `MontarVisual`) | G1 acrescenta propriedades de alvo; região diferente → merge limpo esperado. Verificar no merge |
| `LuccaCombate.cs` | Personagem (possível gatilho de animação de tiro) | G1 muda só `EscolherAlvo`/`EstaAFrente` (regra de alvo). Motivo registrado no PR |
| `HexPlayer.cs` | Personagem/navegação | **não tocado** |
| `VisualDeIlha`, `DecoracaoDeIlha`, `HexTile`, `HexIsoCamera`, `CenarioContinuo`, `FundoContinuo`, `RioDeFundo`, luz | Ambiente | **não tocados** |
| `HexPrototipo.unity` | Ambiente (passes de arte) | **não tocada**; integração do chefe agendada (`IMPLEMENTATION_PLAN.md` §G1.8) |

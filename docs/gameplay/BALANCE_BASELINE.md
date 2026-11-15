# Balanceamento — linha de base (Sprint G0)

Base `85c5276`. **Três tipos de número, sempre rotulados:**
- **[valor]**: lido no código, no asset ou no YAML da cena (conferido).
- **[cálculo]**: aritmética sobre os valores, sem rodar o jogo (reproduzível à mão).
- **[observado]**: medido em Play Mode por alguém, com fonte. Hoje há só um.
Nada aqui é resultado empírico de balanceamento. Estimativas de dano recebido e de duração **não** foram medidas.

## 1. Valores atuais
| Parâmetro | Valor | Fonte |
|---|---|---|
| Lucca PV / ATK / flechas | 50 / 35 / 15 | [valor] cena (`LuccaStatus`), igual ao default do script |
| XP do 1º nível / crescimento | 100 / ×1,75 | [valor] cena |
| Tiro: intervalo / alcance / só à frente | 0,8 s / 3 casas / sim | [valor] cena (`LuccaCombate`) |
| Velocidade do Lucca / pulo | 3,5 u/s / 0,35 s | [valor] cena (`HexPlayer`) |
| Distância entre centros de casas | 5,196 u (√3 × 3) | [cálculo] `HexLayout`, tamanho 3 |
| Anel | 24–36 casas (raio 4–6) | [valor] gerador |
| Morcego PV / ATK / XP / intervalo / alcance | 50 / 5 / 50 / 1,6 s / 2 casas | [valor] `Assets/Dados/Morcego.asset` |
| Mergulho do morcego | 9 u/s, 1,2 s, acerto a 0,45 u | [valor] asset |
| Lobo PV / ATK / XP / intervalo | 100 / 15 / 100 / 2,2 s, mesma casa | [valor] `Assets/Dados/Lobo.asset` |
| Spawn de morcego | 5 s, máx. 6 vivos, ≥ 3 casas do Lucca | [valor] cena (`InimigoSpawner`) |
| Crescimento por volta | ×1,10 composto em PV/ATK/XP, só no nascimento | [valor] cena + código |
| Voltas até o chefe | 4 | [valor] cena |
| Mão | 4 espaços, 4 cartas iniciais garantidas (Mola, Coração, Moedas, Flechas), 1 carta por nível | [valor] cena (`MaoDeCartas`) |
| Coração / Flechas / Moedas (cartas) | +5 PV / +10 flechas (teto 15) / +3 moedas, recarregam por volta | [valor] assets |
| Campo Pacífico / Cabana | +2 PV / +2 PV +2 por Campo vizinho | [valor] assets |
| Torre / Área de Treino | +8 % dano / +8 % +15 % por Torre vizinha | [valor] assets |
| Moeda no anel | 20 % por ilha, por volta | [valor] cena (`GeradorDeMoedas`) |
| Duração da 1ª volta | ~15–19 s reais com timeScale 4 (≈ 60–76 s de jogo) | [observado] `docs/sprint-0/TEST_REPORT.md` §4, 4 execuções, inclui o início da cena |
| Duração da volta (G1) | 27,1 s (anel de 24) a 34,0 s (anel de 30) de jogo, ≈ 1,13 s por casa | [observado] `MetricasDaPartida`, 3 partidas, 12 voltas (§8.2). Substitui a estimativa acima para as contas novas |

## 2. Escala dos inimigos por volta [cálculo]
`round(base × 1,1^k)`, `Mathf.RoundToInt`, k = voltas fechadas quando o inimigo nasce.
| k | mult. | Morcego PV/ATK/XP | Lobo PV/ATK/XP |
|---|---|---|---|
| 0 | 1,000 | 50 / 5 / 50 | 100 / 15 / 100 |
| 1 | 1,100 | 55 / 6 / 55 | 110 / 17 / 110 |
| 2 | 1,210 | 61 / 6 / 61 | 121 / 18 / 121 |
| 3 | 1,331 | 67 / 7 / 67 | 133 / 20 / 133 |
| 4 (chefe) | 1,4641 | 73 / 7 / 73 | 146 / 22 / 146 |
(Os valores de borda ,5 dependem do erro de ponto flutuante de `1.1f`; conferir no teste de G4.)

## 3. Pontos de corte de flechas [cálculo]
Dano por flecha: `round(35 × (1 + bônus))`. Sem bônus 35; 1 Torre ou 1 Área isolada 38; Torre+Área isoladas 41;
Área vizinha de 1 Torre + essa Torre (8+15+8 = 31 %) 46.
| Flechas para matar | 35 | 38 | 41 | 46 |
|---|---|---|---|---|
| Morcego k=0..3 (50–67) | 2 | 2 | 2 | 2 |
| Morcego k=4 (73) | **3** | 2 | 2 | 2 |
| Lobo k=0 (100) | 3 | 3 | 3 | 3 |
| Lobo k=1..3 (110–133) | 4 | 3–4 | 3–4 | 3 |
| Lobo k=4 (146) | 5 | 4 | 4 | 4 |
Leitura: hoje o dano extra quase não muda nada até a 4ª volta; os cortes aparecem com o lobo e no chefe. As cartas de
dano viram decisão real quando a vida dos inimigos cruza múltiplos de 35, e não com +8 % "invisível".

## 4. XP e cartas [cálculo]
Custo por nível: 100, 175, 306, 536, 938 (acumulado 100, 275, 581, 1117, 2055).
Morcegos para o nível (só morcegos, XP da volta): nível 1 = 2 (volta 0); nível 2 ≈ +3–4; nível 3 ≈ +5.
Como a mão começa cheia, a carta do nível 1 só chega se o jogador já tiver jogado uma carta (ver auditoria §2.8).

## 5. Economia de flechas por volta [cálculo + hipótese]
- Entrada: 15 por volta (recarga no acampamento) + 10 por carta Flechas no caminho (útil só se ≥ 10 já gastas).
- Demanda potencial: ~12–15 tentativas de spawn por volta de ~60 s, até 6 morcegos vivos, 2 flechas cada → até
  24–30 flechas para limpar tudo. **Hipótese a medir:** a munição, e não a vida, é o recurso que limita o combate
  antes do chefe. Ainda **não** se sabe quantos morcegos o Lucca de fato encontra no alcance por volta.

## 6. Dano recebido [não medido]
O morcego só acerta se o mergulho alcançar o Lucca (que nunca para). A taxa de acerto real é desconhecida; por isso
não há estimativa honesta de dano por volta. Medir em G4 (§8).

## 7. Chefe proposto em G1 (hipóteses, não calibradas)
Variante do morcego, montada em tempo de execução a partir de `Morcego.asset` pelo `EncontroDoChefe` (sem asset novo):
| Parâmetro | Multiplicador sobre o morcego | Base | Efetivo no chefe (×1,4641) [cálculo] |
|---|---|---|---|
| PV | ×10 | 500 | 732 |
| ATK (mergulho) | ×1,2 | 6 | 9 |
| XP | ×4 | 200 | 293 |
| Escala | ×3 | 1,14 | — |
| Intervalo de ataque | 4 s (fixo) | | |
| Mergulho | 14 u/s, até 3,5 s, acerto a 0,7 u, alcance ilimitado | | |
| Especial "Revoada" | a cada 14 s, aviso de 2 s, chama 2 morcegos comuns no anel | | |
[cálculo] Flechas para matar o chefe: 21 com 35 de dano, 20 com 38, 18 com 41, 16 com 46 — sempre acima das 15 da
recarga. A intenção é que o jogador precise de **uma** fonte extra (carta Flechas no caminho ou dano) para vencer na
primeira passada, e que a Revoada dispute flechas. Durante o chefe, passar pelo acampamento recarrega só as flechas
(sem vida, sem volta nova): não há impasse por falta de munição. **Tudo isso é hipótese até G4.**

## 8. Plano de medição
### 8.1 Instrumento (pronto, G1)
`MetricasDaPartida` (contagem pura, testada em Logic) + `MedidorDaPartida` (componente opcional que ouve eventos que
já existem). Mede o que o responsável pediu para o chefe:
| Pedido | Campo |
|---|---|
| Duração do confronto | `duracaoDoChefe` (segundos de jogo) |
| Flechas consumidas | `flechasNoChefe`, `flechasEmOutrosNoChefe` (as da Revoada) |
| Recargas necessárias | `recargasNoChefe` (passagens pelo acampamento no chefe) |
| Dano recebido | `danoRecebidoNoChefe`, `pvNoInicioDoChefe` |
| Frequência da Revoada | `revoadas`, `morcegosDaRevoada` ÷ `duracaoDoChefe` |
| Impasse | `tempoSemFlechasNoChefe`, `maiorTrechoSemFlechasNoChefe` (`PossivelImpasse(limite)`) |
| Impacto das cartas de dano/munição | comparar os campos acima entre cenários |
Por volta: `duracao`, `flechasDisparadas`, `danoRecebido`, `inimigosDerrotados`, `pvAoFechar`.

### 8.2 Medições do G1 [observado] — 2026-11-15, Editor da worktree, branch `sprint-3/gameplay-cena`
Segundos **de jogo** (timeScale 4). Cada linha = **uma** partida num mapa sorteado: amostras, não conclusões.
Valores do chefe = hipóteses de §7 (732 PV efetivos, 9 de dano por mergulho, Revoada a cada 14 s).

**Com `MedidorDaPartida` (testes `PartidaRealPlayModeTests`; morcegos comuns desligados, só a Revoada):**
| # | Cartas | Anel | Volta | Chefe | Resultado | Flechas chefe / outros | Recargas | Dano recebido | Revoadas (morcegos) | Sem flechas (maior trecho) |
|---|---|---|---|---|---|---|---|---|---|---|
| 1 | nenhuma | 24 | 27,1 s | 28,8 s | **Derrota** | 15 / 3 | 1 | 50 (de 50) | 2 (4) | 15,8 s (15,8 s) |
| 2 | nenhuma | 30 | 33,9 s | 37,8 s | **Derrota** | 16 / 4 | 1 | 50 (de 50) | 2 (4) | 22,6 s (22,6 s) |
| 3 | Flechas + Torre | 30 | 34,0 s | 22,1 s | **Vitória** | 20 / 2 | 0 | 34 | 1 (2) | 5,7 s (5,7 s) |

**Partidas manuais pelo Editor (morcegos comuns LIGADOS, estado lido a cada 0,5 s; sem medidor):**
| # | Cartas | Anel | Lucca ao chegar no chefe | Chefe | Resultado |
|---|---|---|---|---|---|
| 4 | Flechas + Torre | — | 50 PV, 14 flechas | ~24 s | **Derrota com o chefe a 10/732** |
| 5 | Flechas + Torre | 24 | 50 PV, 13 flechas | ~24 s | **Derrota com o chefe a 10/732** |
| 6 | Flechas + Coração + Torre + Área de Treino (46 de dano) | 30 | 50 PV, 12 flechas | ~20 s | **Vitória**, 30 PV no fim |
| 7 | idem 6 | 24 | — | — | **Vitória**, 37 PV no fim |

### 8.3 Leitura das medições (hipóteses para G4, nenhum valor foi alterado)
1. **Sem cartas, o chefe vence (2/2).** O 1º ciclo de munição (15 flechas × 35 = 525) não basta para 732 PV, e o Lucca
   morre antes de a recarga do acampamento fazer diferença: ficou sem flechas por 15,8–22,6 s (55–60 % do confronto).
   Não houve impasse infinito; o risco real é a "espiral" sem munição, não o impasse.
2. **Ponto de corte do teto de flechas.** Com Torre (38 de dano) são 20 flechas; nas partidas 4 e 5 o Lucca acertou 19
   e o chefe ficou com 10 PV (732 − 19 × 38). O teto de 15 limita o que a carta Flechas soma no meio da luta. Pequenas
   diferenças (1 flecha, +3 % de dano) decidem o final. Isso favorece a Oficina de Arqueiro (+2 flechas máx., G2) como
   decisão legível — **mas o número final do chefe só deve mudar depois de mais amostras com semente fixa.**
3. **Cartas de dano e munição mudam o resultado.** Sem cartas 0/2; Flechas+Torre 1/3; Flechas+Coração+Torre+Área 2/2.
   É o efeito desejado ("preparar para o chefe"), mas a faixa do meio está apertada demais.
4. **Morcegos comuns no chefe.** O spawn pausa, mas quem já existia continua: o Lucca chega com 12–14 flechas (não 15)
   e esses morcegos ainda disputam flechas e causam dano.
5. **Revoada** dispara no intervalo configurado (2 em ~29–38 s, 1 em 22 s).
6. **Duração da partida.** Volta de 27–34 s (≈ 1,13 s por casa) e jornada inteira de 2,3–2,9 min — bem abaixo da meta
   de 5–7 min do GDD v2 §4 (que partia da estimativa antiga de 60–76 s por volta). Questão de design aberta para o
   responsável: aceitar partidas curtas ou alongar (mais voltas, anel maior, Lucca mais lento) — não decidido aqui.

Os resumos completos saem no console (`[Metricas]`) e em `%TEMP%/loopia_metricas_<cenario>.txt`.

### 8.4 Próximos passos (G4)
1. RNG com semente para spawn e compra (`System.Random` injetável) — pré-requisito de qualquer número reproduzível.
2. Teste Play com semente fixa do mapa e do RNG, timeScale 4, registrando por volta: flechas gastas, flechas que
   sobraram, dano recebido por fonte, inimigos mortos, XP, nível, PV ao fechar a volta, duração.
3. Cenários: (a) nenhuma carta jogada; (b) só as 4 iniciais; (c) Flechas + Torre; (d) Coração + Cabana. 5 sementes cada.
4. Para o chefe: as mesmas métricas no encontro + tempo até a vitória/derrota.
5. Limitações conhecidas: o Play Mode depende do Editor livre; sem build não há números de desempenho.

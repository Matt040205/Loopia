# Loopia — GDD v2 (gameplay)

Documento da frente de gameplay. Complementa o GDD original do time, sem substituí-lo.
**Aprovado pelo responsável em 2026-11-15**, com as ressalvas registradas em §6. Níveis de compromisso:
- **[OBRIGATÓRIO]**: P0 — máquina de estados, chefe, vitória, derrota e integração. Implementado (G1); validação
  em Play Mode e integração de cena pendentes.
- **[P1 aprovado]**: planejamento e implementação incremental — fila de recompensas, Santuário/Fonte/Oficina,
  Mirante e infraestrutura inicial de sangramento/veneno. Valores são hipóteses de teste.
- **[P1 condicionado]**: só depois de avaliação — balanceamento definitivo, progressão permanente, clima com efeito
  mecânico, estações.
- **[FORA DE ESCOPO]**: inventário completo, equipamentos complexos, múltiplas variantes de chefe, fadiga por
  quantidade de inimigos mortos.
- **[PROPOSTA]**: ideia registrada, **não aprovada**. Não implementar sem decisão do responsável.

## 1. Identidade (inalterada)
Roguelite estratégico de construção de ilhas hexagonais. Lucca anda sozinho pelo anel; o jogador age só por cartas e
modificações no mundo. Pilares: construção estratégica, risco e recompensa, preparação para o confronto final.
Fora de escopo: controle direto, mundo aberto, combate de ação.

## 2. [OBRIGATÓRIO] Ciclo da partida
```
Preparando ──(cena pronta)──► Correndo ──(4ª volta fechada)──► Chefe ──(chefe morre)──► Vitória
     │                           │                                │
     └──────────(Lucca morre)────┴──────────(Lucca morre)─────────┴──► Derrota
```
- **Preparando**: cena montando (mapa, bake). Hoje dura até o `Start` do diretor; é o gancho para uma futura fase
  de colocação antes da partida.
- **Correndo**: o loop de hoje. Cada volta fechada restaura PV e flechas e fortalece os inimigos (×1,1).
- **Chefe**: na 4ª volta (depois da recompensa da volta), o chefe nasce **uma vez** no centro do miolo.
  - A contagem de voltas para: passar pelo acampamento não fecha volta, não fortalece ninguém, não recarrega cartas.
  - O acampamento ainda recarrega **flechas** (não vida): evita impasse sem munição e mantém a vida como recurso.
  - O spawn comum de morcegos pausa; quem já existe continua.
  - O chefe é alvo do tiro automático de qualquer casa do anel (regra de alvo grande, §2.1).
- **Vitória**: o chefe morre. Lucca para, inimigos congelam, o save registra a vitória e o recorde; tela de resultado
  com "voltar ao acampamento".
- **Derrota**: PV 0 em qualquer estado não terminal. Igual a hoje (tela de fim de jornada).
- Vitória e Derrota são terminais e mutuamente exclusivas: o primeiro que acontecer vale, o outro é ignorado.

### 2.1 Chefe: "Morcego Ancião"
Variante do morcego (sem modelo novo): maior, vermelho escuro, voa mais alto sobre o centro do miolo.
- Ataque básico: o mergulho do morcego, com alcance ilimitado e mais rápido; volta ao poleiro depois.
- Ataque especial **Revoada**: aviso visual de perigo (anel vermelho pulsando sob o chefe) por 2 s, depois chama
  2 morcegos comuns no anel. Eles disputam as flechas do Lucca (o tiro escolhe o mais próximo): o especial ataca a
  economia de munição, que é a decisão estratégica do jogador.
- Barra de vida própria no topo da tela.
- Regra de alvo grande: o chefe declara alcance extra = (maior distância do anel ao centro − alcance do tiro) e
  pode ser atingido de qualquer ângulo. A prioridade continua pela distância real: morcegos colados vêm primeiro.
- Números iniciais: `BALANCE_BASELINE.md` §7 (hipóteses, aprovados só para prototipagem).
- Cartas continuam valendo durante o chefe (preparo em tempo real); na vitória/derrota nenhuma carta é aceita
  (`MapaDePlataformas.PodeColocar` → "a jornada terminou") e a barra de cartas some.
- Leitura visual do tiro no chefe: plano com a frente de personagem em `APRESENTACAO_DO_CHEFE.md`.

## 3. [P1 aprovado] Próximas sprints
Só começam depois de G1 validada em Play Mode e aprovada pelo responsável.

### G2 — fila de recompensas primeiro, depois cartas de sobrevivência
Ordem obrigatória: (1) fila de recompensas pendentes com política de capacidade explícita, atualizando de propósito o
teste de caracterização da mão cheia; (2) só então Santuário, Fonte Restauradora e Oficina de Arqueiro.

| Carta | Efeito (hipótese) | Gancho existente |
|---|---|---|
| Santuário | +3 PV máximo | `BonusDePv` (igual Campo Pacífico) |
| Fonte Restauradora | +2 PV por coração coletado | novo modificador "cura recebida de coração" lido em `PlataformaRecompensaDef` |
| Oficina de Arqueiro | +2 flechas máximas | novo `BonusDeFlechas` (hoje não existe) |
- **Agregador de modificadores** (`ModificadoresDoLucca`): soma de PV máx., dano, flechas máx., cura por coração,
  recalculado pelo `MapaDePlataformas.Recalcular` como hoje; o mesmo agregador servirá a equipamentos e status.
- **Fila de recompensas**: a carta do level-up que não cabe vai para uma fila (limite configurável) e entra quando
  abrir espaço; a UI mostra "+N pendentes". O teste de caracterização da mão cheia será **atualizado de propósito**.
- **Ilha Alta → Mirante** (aprovado para prototipagem): enquanto o Lucca está numa ilha alta, +1 casa de alcance e
  o tiro atinge em qualquer direção. Pergunta a testar: a vantagem compensa exigir uma Mola antes e ocupar duas casas
  do circuito? Medir com `MetricasDaPartida` (flechas que acertam / abates por volta, com e sem Mirante). O rebake
  só acontece ao colocar/remover (já é assim); nada de rebake novo.

### G3 — efeitos de status
`StatusEffectDefinition` (SO: duração, intervalo, dano por pulso, política de acúmulo, renovação),
`StatusEffectController` (um por alvo, único que avança os efeitos), `ActiveStatusEffect` (estado de execução),
`StatModifier`. Sangramento: pulsos rápidos e curtos, acumula intensidade até N, renovar não estende além do teto.
Veneno: pulsos lentos e longos, não acumula intensidade, reaplicar renova a duração. Sem dano após a morte, eventos
`AoAplicar/AoPulsar/AoRemover` para o VFX futuro. Cansaço: **não** implementar (pune o que o jogador não controla);
alternativas em §5.

### G4 — dificuldade e progressão
RNG com semente, cenários fixos e métricas (`BALANCE_BASELINE.md` §8). Alavancas a testar antes de mexer no
multiplicador: quantidade máxima e intervalo de morcegos por volta, XP por volta, cura disponível.
Progressão permanente: só desenho (§5), compatível com saves antigos (campos novos com padrão 0).

## 4. Critérios de game design (metas para orientar testes — nenhum número aqui foi medido)
| Critério | Meta (hipótese) | Como medir / observar |
|---|---|---|
| Duração-alvo da partida | 5–7 min até o fim do chefe (4 voltas de ~60–75 s + confronto de 1–2 min) | `MetricasDaPartida.duracaoTotal`. **Medido no G1: jornada de 2,3–2,9 min** (volta de 27–34 s) — abaixo da meta; decisão aberta (`BALANCE_BASELINE.md` §8.3) |
| Decisões por volta | ≥ 2 decisões reais por volta (colocar/substituir/guardar uma carta), não só "jogar a carta que chegou" | contar colocações por volta (métrica nova em G2); playtest: o jogador hesita entre opções? |
| Percepção do impacto das cartas | cada carta muda algo visível em ≤ 1 volta: número no HUD (PV/flechas/dano) ou comportamento (flechas por morcego, alcance) | pontos de corte de `BALANCE_BASELINE.md` §3; playtest: o jogador explica o que a carta fez |
| Preparação para o chefe | as 4 voltas constroem munição, dano e sustento; sem uma fonte extra (Flechas no caminho ou dano) o 1º ciclo de munição não basta (21 flechas para 15 de recarga, cálculo) | cenários "sem cartas" × "Flechas+Torre" (`PartidaRealPlayModeTests`): duração do chefe, recargas, dano recebido |
| Evitar "esperar o circuito" | sempre há algo a decidir: carta na mão, substituição de Básica, reação à Revoada no chefe; cartas valem durante o confronto | playtest: tempo sem nenhuma carta jogável na mão; fila de recompensas (G2) reduz mãos travadas |
| Sem impasse | nenhum trecho > 1 volta com o chefe vivo e 0 flechas | `maiorTrechoSemFlechasNoChefe` vs duração da volta |

Regra: dificuldade sobe por pressão (quantidade, frequência, Revoada, economia de flechas), **não** por multiplicar
atributos. Mudança de número só com medição antes/depois registrada.

## 5. [PROPOSTA] Não aprovado
- **Árvore permanente** (moedas do save): Sobrevivência (+PV inicial, coração +1), Combate (+dano, +flecha inicial),
  Construção (+1 carta inicial, 1ª carta do nível garantida do tipo escolhido). Três nós por ramo, custo crescente.
- **Dia/noite**: noite com +25 % de frequência de morcegos e +25 % de XP; nada de precisão/alcance/crítico juntos.
  Depende de uma interface de leitura do Environment Director (`IEstadoDoAmbiente { bool EhNoite; Estacao Estacao; }`),
  só leitura, sem o gameplay conhecer o `DayNightController`.
- **Estações**: primavera (mais coração), verão (mais moeda, mais morcego), outono (mais inimigo e XP), inverno
  (menos recarga, flechas +dano). Só depois da interface estável do ambiente.
- **Equipamentos**: começar por flechas especiais (comum, sangramento, veneno) usando G3; inventário só se o loop
  pedir. Arcos, armaduras, botas, crítico, bloqueio, resistências e peso ficam fora.
- **Alternativas ao cansaço**: (a) fadiga por volta sem descanso numa carta "Cabana"; (b) penalidade só quando sem
  flechas; (c) nenhuma.
- **Alvo do tiro**: considerar inimigos em mergulho como alvo mesmo de lado (coerência visual).

## 6. Decisões do responsável (2026-11-15)
- G0, GDD v2 e arquitetura de G1 aprovados. Morcego Ancião e Revoada aprovados para prototipagem.
- G1 só conclui com a suíte rodando no Editor e validação em Play Mode (4 voltas reais, chefe, combate, vitória,
  derrota, 5ª volta bloqueada, cartas bloqueadas no fim, vitórias persistidas, volta ao acampamento). Compilação fora
  do Editor não conta.
- Valores do chefe seguem hipóteses até medir: duração, flechas, recargas, dano recebido, frequência da Revoada,
  impasse e impacto das cartas de dano/munição.
- Orientação visual do arco/tronco: planejar e coordenar com a frente de personagem; não tocar rig nem animações.
- G2 não começa antes de G1 validada e aprovada.
- **2026-11-15: G1 aprovada pelo responsável** após a validação no Editor (`RELATORIO_G1.md`). G2 liberada; 1ª tarefa: fila
  de recompensas pendentes.

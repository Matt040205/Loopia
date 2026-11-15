# Loopia — GDD v2 (gameplay)

Documento da frente de gameplay. Complementa o GDD original do time, sem substituí-lo. Três níveis de compromisso:
- **[OBRIGATÓRIO]**: P0, define a partida completa. Implementado nesta entrega (G1), com integração de cena pendente.
- **[P1 aprovado]**: escopo das sprints G2–G4 descrito no pedido de trabalho. Valores são hipóteses de teste.
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
- Números iniciais: `BALANCE_BASELINE.md` §7 (hipóteses).

## 3. [P1 aprovado] Próximas sprints
### G2 — cartas de sobrevivência e fila de recompensas
| Carta | Efeito (hipótese) | Gancho existente |
|---|---|---|
| Santuário | +3 PV máximo | `BonusDePv` (igual Campo Pacífico) |
| Fonte Restauradora | +2 PV por coração coletado | novo modificador "cura recebida de coração" lido em `PlataformaRecompensaDef` |
| Oficina de Arqueiro | +2 flechas máximas | novo `BonusDeFlechas` (hoje não existe) |
- **Agregador de modificadores** (`ModificadoresDoLucca`): soma de PV máx., dano, flechas máx., cura por coração,
  recalculado pelo `MapaDePlataformas.Recalcular` como hoje; o mesmo agregador servirá a equipamentos e status.
- **Fila de recompensas**: a carta do level-up que não cabe vai para uma fila (limite configurável) e entra quando
  abrir espaço; a UI mostra "+N pendentes". O teste de caracterização da mão cheia será **atualizado de propósito**.
- **Ilha Alta**: [hipótese] "Mirante" — enquanto o Lucca está numa ilha alta, +1 casa de alcance e o tiro ignora a
  regra "só à frente". Risco: exige mola antes, ocupa uma casa do caminho e o rebake só acontece ao colocar/remover
  (já é assim). Não muda topologia além do que já muda.

### G3 — efeitos de status
`StatusEffectDefinition` (SO: duração, intervalo, dano por pulso, política de acúmulo, renovação),
`StatusEffectController` (um por alvo, único que avança os efeitos), `ActiveStatusEffect` (estado de execução),
`StatModifier`. Sangramento: pulsos rápidos e curtos, acumula intensidade até N, renovar não estende além do teto.
Veneno: pulsos lentos e longos, não acumula intensidade, reaplicar renova a duração. Sem dano após a morte, eventos
`AoAplicar/AoPulsar/AoRemover` para o VFX futuro. Cansaço: **não** implementar (pune o que o jogador não controla);
alternativas em §4.

### G4 — dificuldade e progressão
RNG com semente, cenários fixos e métricas (`BALANCE_BASELINE.md` §8). Alavancas a testar antes de mexer no
multiplicador: quantidade máxima e intervalo de morcegos por volta, XP por volta, cura disponível.
Progressão permanente: só desenho (§4), compatível com saves antigos (campos novos com padrão 0).

## 4. [PROPOSTA] Não aprovado
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

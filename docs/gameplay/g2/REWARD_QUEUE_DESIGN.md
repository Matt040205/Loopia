# G2-A — fila de recompensas pendentes

## Problema (G1)
A mão tem 4 espaços e começa cheia (Mola, Coração, Moedas, Flechas). O 1º nível chega com 2 morcegos (100 XP).
`MaoDeCartas.SubiuDeNivel → Comprar()` devolvia `false` com a mão cheia e a carta do nível sumia sem aviso
(`GAMEPLAY_AUDIT.md` §2.8, teste `Characterization_MaoCheia_*`).

## Regra nova
Cada carta de nível vira um **direito de compra** (`cartasPorNivel` direitos por nível) que entra no fim de uma fila
FIFO. A fila é drenada sempre que pode haver entrega:

| Gatilho | Por quê |
|---|---|
| `SubiuDeNivel` (logo depois de enfileirar) | com espaço livre, a carta chega na hora, como antes |
| `Gastar(espaco)` | a carta jogada abre o espaço; a pendente mais antiga ocupa o primeiro espaço livre |
| `MapaDePlataformas.AoMudar` | uma carta pode ter ficado disponível (ex.: Mola colocada libera a Ilha alta) |

Nada é verificado por frame (sem polling novo). Uma guarda de reentrância (`_entregando`) impede que uma entrega
dispare outra drenagem aninhada.

### Direito × carta sorteada
A fila guarda **direitos** (o nível que os gerou), não cartas. A carta é sorteada **no momento da entrega**, com o
mesmo sorteio uniforme de `Comprar()` sobre as cartas `DisponivelParaCompra(mapa)`. Assim:
- nenhuma carta indisponível (Ilha alta sem Mola) é entregue, mesmo que tenha sido "sorteável" quando o nível subiu;
- não há RNG novo (o sorteio continua o de hoje; semente fica para G4).

Sem carta disponível (baralho vazio ou só cartas bloqueadas), o direito **fica na fila** e é tentado de novo no
próximo gatilho. Nunca é consumido sem entregar uma carta.

### Política de capacidade
- Os direitos **não têm teto rígido**: um teto obrigaria a descartar, que é exatamente o que G2 proíbe. Um contador
  inteiro não tem custo relevante (uma partida gera ~5–6 níveis, `BALANCE_BASELINE.md` §4).
- `limiteDeAlertaDePendentes` (padrão 4, configurável no Inspector) é um **limite suave**: ao ultrapassá-lo a fila
  registra um aviso no console (uma vez por ultrapassagem) e sinaliza `AcimaDoLimite`, para o balanceamento
  enxergar mãos travadas. Nada é descartado. **Divergência registrada:** o GDD v2 fala em "limite configurável";
  aqui ele é suave porque um limite rígido contradiz "nenhuma recompensa perdida". Revisitar em G4 se o
  balanceamento pedir conversão do excedente (ex.: moedas) — mudança explícita, nunca silenciosa.

### Estados terminais e morte
- Com a jornada encerrada (`LoopDirector.Terminou`, via `mapa.diretor` ou campo `diretor`) ou o Lucca morto, a fila
  **não entrega** (a barra de cartas some e o mapa recusa cartas). Direitos de níveis ganhos nesse instante (XP do
  chefe) continuam contados, sem mudar a mão nem o estado da partida.
- Durante o chefe a fila entrega normalmente (cartas continuam valendo no confronto).

### Prioridade com `cartaAoLiberar`
A oferta única da Ilha alta (`cartaAoLiberar`) continua no `Update` existente. Ao abrir um espaço, a pendente entra
na hora (em `Gastar`); a Ilha alta entra no próximo espaço livre. Comportamento de G1 inalterado quando não há
pendentes.

## API
| Membro | O quê |
|---|---|
| `int Pendentes` | direitos aguardando espaço/carta |
| `bool AcimaDoLimite` | `Pendentes > limiteDeAlertaDePendentes` |
| `event Action AoMudarPendentes` | toda vez que `Pendentes` muda (entra ou sai direito) |
| `event Action<int, PlataformaDef> AoEntregarPendente` | (nível de origem, carta entregue), na ordem FIFO |
| `int limiteDeAlertaDePendentes` | limite suave (Inspector) |
| `LoopDirector diretor` | opcional; vazio usa `mapa.diretor` |
Mantidos: `Comprar()` (sorteio imediato; usado na mão inicial), `DarCarta`, `Gastar`, `AoMudar`, `EspacosLivres`.

## UI
`CartasUI` desenha `+N pendentes` à direita da barra de cartas, só quando `N > 0` e a jornada não acabou. É um rótulo
IMGUI (mesma tecnologia da barra), fora da área clicável das cartas; não existe carta arrastável para uma pendente.
A barra do chefe fica no topo da tela, longe do indicador.

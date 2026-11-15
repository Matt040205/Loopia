# Pass D2 — Luz (2026-11-15)

Branch `sprint-1/pass-d2-luz`, empilhada sobre `sprint-1/pass-c-dressing` (PR #4).
**Escopo pedido pelo responsável: só a luz** ("pode ir apenas para a luz").
Ficaram para depois os outros itens do D2 no plano: Opaque Texture/SSAO, `new Material` por spawn em `Inimigo`/`Projetil` e a baseline de desempenho em build.

## Problema
- O `DayNightController` levava o sol de uma borda do horizonte à outra **passando pelo zênite**: ao meio-dia a luz vinha a 90°.
- Com luz de cima, as laterais das rochas das ilhas e o relevo do fundo ficavam chapados, e o topo das ilhas estourava em verde-limão.
- Pendência registrada no Pass B: "o relevo não lê ao meio-dia".

## O que mudou
- **`DayNightController.elevacaoMaximaDoSol`** (padrão 90, que reproduz o D1). Abaixo de 90:
  - o sol sobe da elevação mínima até a máxima e volta, numa curva seno;
  - o azimute varre os mesmos 180° do arco antigo (`sunAzimuth` → `sunAzimuth + 180`);
  - **no nascer e no pôr a direção é exatamente a mesma do D1**, então crepúsculo, 18h e noite não mudam.
- O controlador continua o único dono da luz. Há uma sobrecarga nova de `AvaliarLuz` que devolve também o azimute, e a assinatura antiga foi mantida.
- **Cena:** `elevacaoMaximaDoSol: 58`, aplicado pelo Editor. O diff da cena é **uma linha**.
- A lua não mudou. Ainda passa pelo zênite à meia-noite, mas as horas medidas (22h) não são afetadas.

## Avaliação (seed 12345, 1920×1080; variantes aplicadas em memória no mesmo Play)

| Variante | Contraste topo/fundo 12h / 18h / 22h | Batches | Shadow casters | Decisão |
|---|---|---|---|---|
| A: atual (sol pelo zênite) | 2,83 / 2,03 / 3,01 | 528 | 0 | — |
| **B: sol com teto de 58°** | **2,59 / 2,03 / 3,01** | 525 | 0 | **adotada** |
| C: B + sombras (distância 120, cobrindo as ilhas) | 2,65 / 1,94 / 3,03 | **826 (+57 %)** | 784 | rejeitada |
| D: C + tonemapping Neutral | 2,25 / 1,91 / 2,95 | 828 | 786 | rejeitada |
| E: C + tonemapping ACES | 3,36 / 2,22 / 4,60 | 823 | 784 | rejeitada |

- **Sombras:** a câmera fica a 70 u e a distância de sombra do URP é de 50 u, então hoje nada projeta sombra. Ligar custou +57 % de batches, e as sombras quase não aparecem: as ilhas flutuam sem chão para recebê-las, e as árvores e marcadores são pequenos. Não vale o custo agora.
- **Neutral:** lava a cor. Contraste ao meio-dia de 2,83 para 2,25, e a saturação do topo cai de 0,57 para 0,47.
- **ACES:** aumenta o contraste escurecendo tudo. À noite o fundo chega a 0,010 de luminância (quase preto) e o topo a 0,044, contra a regra "nada de silhueta preta" da Bible.
- Folhas da avaliação em `avaliacao/variantes_h12/18/22.jpg`. Números em `avaliacao/stats_variantes.json` e `avaliacao/legibilidade_*.json`.

## Aceite com a versão final (matriz do D1 + horas de transição)
**Legibilidade:** contraste de luminância entre o topo das ilhas e o fundo imediato.

| Hora | Mínimo na Bible | Antes (Pass C) | **Depois** |
|---|---|---|---|
| 12h | ≥ 1,25 | 2,59–2,81 | **2,34–2,56** |
| 18h | ≥ 1,25 | 1,77–2,01 | **1,77–2,01** (igual: no pôr do sol a direção não muda) |
| 22h | ≥ 2,0 | 2,85–3,02 | **2,84–3,01** |

- **Horas de transição** (seed 12345): 5h 2,04 · 6h 1,81 · 7h 2,02 · 17h 2,10 · 19h 2,02. Todas acima do mínimo.
- **Meio-dia:** o topo das ilhas ficou menos estourado (L 0,57–0,59 → 0,49–0,51). O contraste cai cerca de 10 %, mas continua quase o dobro do mínimo. A separação de valor da Bible (≥ 25 %) se mantém.
- **Efeito visual:** `sol_antes_depois.jpg` mostra 9h, 12h e 15h. As laterais das rochas passam a ter faces iluminadas e faces na sombra, e as ilhas ganham volume.
- **Custo:** batches 527 no Editor, igual ao Pass C (530). A mudança só gira a luz.
- **Arquivos:** `depois/` (matriz e horas extras), `depois/cinza/`, `depois_legibilidade.json`, `stats_depois/`, `horas/` (9h, 12h e 15h, antes e depois, na mesma sessão).

## Testes
- **Novos:** 6 testes.
  - **Logic** (`SolElevacaoMaximaTests`):
    - o padrão continua 90;
    - de dia a elevação fica entre a mínima e a máxima, e chega à máxima ao meio-dia;
    - o nascer e o pôr têm a mesma direção do D1;
    - a direção gira no máximo 1° por minuto de jogo, fora a troca sol/lua.
  - **SceneStatic** (`LuzSceneTests`):
    - o sol da cena tem teto entre 45° e 70°;
    - nenhum Volume liga tonemapping;
    - a distância de sombra não alcança as ilhas (≤ 70 u).
- **Suíte: 259/259.**
  - Logic + SceneStatic: 252/252, job `75d42646395b4c13bc0ecb00a5eea45f`.
  - PlayModeSmoke: 7/7, `TestResults.xml` 2026-11-15 13:38Z, 6–16 s por caso.
  - **Nenhum teste existente foi alterado**: o `MeioDia_SolAlto...` do D1 continua valendo para o padrão de 90.
- **Console:** 0 erros.

## Pendente
- Itens do D2 fora deste escopo: Opaque Texture/SSAO, materiais por spawn e desempenho em build.
- A lua também passa pelo zênite perto da meia-noite. Dá para aplicar o mesmo teto se a noite precisar de volume.
- **Sombras:** só fazem sentido com chão para recebê-las, como uma sombra projetada falsa sob as ilhas, ou com distância seletiva. É uma decisão de arte e de custo.
- **Rollback:** `git revert`, ou `elevacaoMaximaDoSol = 90` na cena, que volta ao D1 sem código.

# Pass C — Dressing das ilhas (2026-11-15)

Branch `sprint-1/pass-c-dressing`, empilhada sobre `sprint-1/pass-b-composicao` (PR #3). Liberado pelo responsável ("Sim, iniciar Pass C").
Sem mudança de cena, câmera, luz, NavMesh ou regra de jogo: só arte das cartas e das ilhas, mais os dados (cartas e perfil).

## C1 — Marcadores das 7 cartas sem modelo
**Antes:** Básica, Floresta, Ilha alta, Torre, Cabana, Campo Pacífico e Área de Treino só tingiam a ilha; nada indicava o tipo da carta.

**Como funciona:**
- `PlataformaDef` ganhou `marcador` (prefab em escala de mundo) e `ocultarDecoracao`.
- `Plataforma.Montar` instancia o marcador como **filho da ilha**: acompanha a altura quando a "Ilha alta" sobe, fica alinhado ao mundo e passa por `VisualDeIlha.PrepararArte` (fora do bake do NavMesh, sem collider).
- O marcador sai junto com a carta, e a decoração escondida volta.
- O item flutuante (`modelo`) não mudou.

**Regra de composição** (Scene Bible; a câmera olha para +X+Z):
- **Cartas do caminho** (o Lucca anda nelas): no centro só peças de até 0,12 u; tudo acima de 0,6 u fica nos vértices do lado longe da câmera.
  - **Básica:** laje escura e seis pedras nos vértices.
  - **Floresta:** bosque KayKit no lado longe e só coisa baixa do lado da câmera, para o lobo continuar visível. Esconde a decoração da ilha.
  - **Ilha alta:** mojão de pedras e bandeirola na cor da carta, no lado longe.
- **Cartas fora do caminho:** "construção" no centro, até 2,4 u.
  - **Torre:** `Torre.obj`, que já existia, com 2,0 u, mais bandeira.
  - **Cabana:** `BarracaArmada`, que já existia, mais caixote, lenha e bandeirola.
  - **Campo Pacífico:** leiras com brotos e espantalho. Esconde a decoração, que cobria as leiras.
  - **Área de Treino:** dois alvos virados para a câmera, boneco de palha e feno.
- Pano e anéis na **cor da carta**, que é cor de gameplay. O resto em madeira, pedra e palha.
- Tudo é gerado de forma reproduzível por `Assets/Editor/MarcadoresDeCarta.cs` (menu `Loopia/Pass C/...`). Prefabs e materiais ficam em `Assets/Modelos/Marcadores/`.

**Revisão durante o passe:**
- A primeira laje da Básica era branca, o ponto mais claro sob o Lucca. Foi escurecida.
- O mojão e a bandeira da Ilha alta eram pequenos demais e foram aumentados.
- No Campo, um cone da decoração cobria as leiras: o Campo passou a esconder a decoração.
- Os testes novos pegaram mais três: uma copa sobrando da ilha, o feno afundando no topo e um arbusto da Floresta num vértice do lado da câmera. Eu tinha errado a ordem dos vértices. Os três foram corrigidos nos marcadores, sem afrouxar o teste.

## C2/C3 — Vegetação baixa e variação por ilha
- **Segunda camada** do `DecoracaoDeIlha` (`VegetacaoBaixa`), com sorteio próprio.
  - A decoração principal **não mudou**: os valores congelados do Pass A continuam passando.
  - Usa as mesmas regras de corredor, centro e borda, não sobrepõe a primeira camada, e não usa collider nem bake.
- **Perfil** (`EscalaDeArte.asset`): 3 moitas KayKit (Arbusto, 0,3–0,6 L) e 2 tufos (Capim, 0,1–0,25 L: capim, e capim com flores creme e amarelas).
  - **C3:** de 3 a 8 peças por ilha, variação da mistura de ±60 %, sorteados por coordenada.
  - O teto de 12 objetos por ilha (4 da decoração principal + 8) é coberto por teste.
- **Revisão durante o passe:**
  - Na primeira versão, as moitas no atlas original **sumiam** no verde-limão do topo, e só 6 de 83 peças eram arbustos, porque a pegada cheia não cabia entre os corredores.
  - Correção: material próprio (`Dressing_Vegetacao`, verde mais fundo) e fração de pegada de 0,5 para Arbusto, como o Pass A fez com as árvores.
  - Resultado na seed 12345: 98 peças, 42 delas arbustos.
- **Custo:**
  - As flores eram esferas da Unity (768 tris cada) e viraram cubos de 12 tris.
  - Em jogo, a camada de cada ilha vira um **static batch com raiz móvel** (a ilha pode subir). Sem isso, foi medido 421 → 1 120 batches.

## Aceite (matriz do D1)
**Legibilidade:** contraste de luminância entre o topo das ilhas e o fundo imediato, sem cartas, só a vegetação.

| Hora | Antes (Pass B) | **Depois** |
|---|---|---|
| 12h | 2,59–2,80 | **2,59–2,81** |
| 18h | 1,77–2,01 | **1,77–2,01** |
| 22h | 2,85–3,01 | **2,85–3,02** |

- Os pontos de topo visíveis por captura caíram de 131–178 para 119–165, porque agora as moitas cobrem alguns pontos da amostra. Nenhuma ilha ficou sem ponto visível.

**Custo:** UnityStats do Editor, mesma sessão, seed 12345, 12h, 1920×1080. "Antes" é o mesmo build com a camada desligada (`vegetacaoMaxima = 0`), equivalente ao Pass B porque a decoração principal é idêntica. São valores do Editor, não de build.

| | Antes | Depois | Δ |
|---|---|---|---|
| Batches | 421 | 530 | +26 % (ainda abaixo dos 615 de antes do Pass B) |
| SetPass | 64 | 67 | +3 |
| Triângulos | 878 967 | 905 726 | +3 % |
| Frametime do Editor | 3,51 ms | 3,68 ms | indicativo |

**Arquivos:**
- `vegetacao_antes_depois.jpg`: mesma ilha sem e com vegetação, 3 closes.
- `cartas_h12.jpg` e `cartas_h22.jpg`: as 7 cartas em close mais a escala de jogo.
- `cartas/`: closes nas 3 horas e `cartas_s12345.json` com as casas usadas.
- `antes_depois_h*.jpg`, `depois/` (matriz), `depois/cinza/`, `depois_legibilidade.json`, `stats_*`.

## Testes
- **Novos:** 23 testes.
  - **Logic:**
    - `MarcadoresDeCartaTests` (15): toda carta tem marcador; o marcador é arte pura, cabe na ilha e fica dentro de 2,5 L; nas cartas do caminho, nada alto no centro e nada acima de 0,6 do lado da câmera; só Floresta e Campo escondem a decoração.
    - `VegetacaoBaixaTests` (7): o perfil está configurado; a camada respeita faixas, corredores, sobreposição e o teto de 12, e varia por ilha; é determinística; `SortearDe` preserva o sorteio original.
  - **PlayModeSmoke:** `MarcadoresPlayModeTests` (1). O marcador é filho da ilha e sobe com a Ilha alta; a Floresta esconde a decoração e a devolve; o NavMesh não sobe no marcador.
- **Suíte: 253/253.**
  - Logic + SceneStatic: 246/246, job `19b19fd7c5194497b91cb47b563a91f1`.
  - PlayModeSmoke: 7/7, `TestResults.xml` 2026-11-15 05:38Z, de 9 a 19 s por caso.
  - Nenhum teste existente foi alterado.
- **Console:** 0 erros.

## Pendente / riscos
- **Não verificado:** desempenho em **build** (o static batch em runtime precisa de malhas legíveis: KayKit tem Read/Write e os primitivos da Unity são legíveis) e HUD em resolução real.
- **Moitas do lado da câmera:** podem cobrir as pernas do Lucca perto da borda. Ficam fora dos corredores e com no máximo 0,6 u, contra 1,0 u do Lucca. Não medi a oclusão do Lucca andando: os testes cobrem regras de posição, não o personagem real.
- **Cor:** o verde da vegetação é mais saturado que o das árvores do Mateus. Se a família de árvores mudar, revisar o atlas `Dressing_Vegetacao`.
- **Ferramentas novas:** `tools/visual-baseline/cartas.py`, `cs/cartas.cs` e `cs/close.cs` colocam as 7 cartas e fazem closes. Reaproveitáveis no D2.
- **Rollback:** `git revert` dos commits. Ou, sem código: zerar `vegetacaoMaxima` no perfil, que desliga a camada, e/ou apagar o `marcador` das cartas, que volta ao comportamento anterior.

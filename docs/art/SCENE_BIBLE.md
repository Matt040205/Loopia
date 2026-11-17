# Scene Bible — Loopia (v0.1, inicial)

Documento de referência para qualquer mudança visual na `HexPrototipo`. Valores numéricos são **alvos propostos**
derivados da auditoria (`AUDITORIA_VISUAL.md`); só viram regra depois de validados em captura e aprovados pelo
responsável.

## 1. Direção visual
**Fantasia ilustrada em miniatura:** um arquipélago flutuante que parece uma maquete pintada vista de cima. O jogador lê
o tabuleiro primeiro e a paisagem depois.

Princípios traduzidos das referências (sem copiar assets/shaders):
- **Leitura isométrica (à la Tunic):** silhuetas simples e chapadas, uma forma dominante por objeto, contraste de
  valor entre o plano jogável e o resto. O topo das ilhas é o elemento mais claro e mais saturado da tela.
- **Atmosfera delicada (à la Sky):** névoa e luz criam profundidade **atrás** do tabuleiro; nunca velam o tabuleiro.
  Gradiente de céu + perspectiva aérea na paisagem distante; luz de borda suave nas ilhas ao entardecer.
- **Coesão de materiais (à la Genshin):** uma família de materiais toon/lit compartilhados com paleta limitada;
  VFX na mesma linguagem de cor (formas arredondadas, gradientes, sem ruído fotográfico).

## 2. Escala de referência
Unidade base **L = altura do Lucca = 1,0 u**. Ilha: grade 3 u (exigência de design), raio visual **R = 2,1 u**,
diâmetro 4,2 u (≈ 4,2 L).

| Categoria | Altura alvo (L) | Pegada máx. | Onde | Hoje |
|---|---|---|---|---|
| Lucca | 1,0 | 0,5 L | caminho | 1,0 ✔ |
| Árvore grande | 1,8–2,4 | 0,9 L | vértices **do lado oposto à câmera** | 0,28–0,65 ✘ |
| Árvore pequena | 1,1–1,5 | 0,7 L | qualquer vértice | — |
| Arbusto / vegetação baixa | 0,3–0,6 | 0,6 L | bordas, entre vértices | inexistente |
| Capim / flores | 0,1–0,25 | 0,3 L | tufos na borda, nunca no corredor | inexistente |
| Pedra | 0,3–0,8 | 0,8 L | vértices | 0,28–0,65 |
| Cogumelo | 0,2–0,45 | 0,3 L | vértices, em grupos | 0,28–0,65 |
| Tronco / cepo | 0,25–0,5 (comp. ≤ 1,0) | 1,0 L | vértices | 0,28–0,65 |
| Item de carta (flutuante) | 0,8–1,0 | 0,9 L | centro da ilha | 0,8–0,95 ✔ |
| Construção de carta (torre, cabana) | 1,5–2,5 | 0,6 R | centro da ilha | inexistente |
| Morcego | 0,8–0,9 de envergadura (Sprint 5B: 0,85; antes 0,5–0,7 para a esfera) | — | voo ~1,1 u acima do topo | 0,85 ✔ |
| Lobo | 0,8–1,1 (comp. ≤ 1,6) | — | ilha | 7,65 ✘ (bug) |
| Paisagem de fundo | parecer **menor** que as árvores das ilhas na tela (perspectiva aérea) | — | ≥ 7 u abaixo das ilhas | ≈ 10× maior ✘ |

Regras de escala:
1. Tamanhos de arte são **relativos** (L ou R), definidos em um único perfil (`ArtScaleProfile`), nunca espalhados.
2. Normaliza pela **altura** para objetos verticais e pelo **comprimento** para deitados (tronco), não pelo maior eixo.
3. Mudança de arte nunca muda o mundo físico (grade, colliders, NavMesh, agente do Lucca).
4. Modelos com `SkinnedMeshRenderer` seguem o mesmo contrato: pivô nos pés, +Z para frente, altura conforme a tabela.

## 3. Hierarquia visual (ordem de leitura)
1. **Lucca, inimigos e itens de carta:** maior contraste local, contorno/realce permitido só aqui.
2. **Topo das ilhas e cartas colocadas:** plano mais claro da cena; cor de carta legível.
3. **Grade de colocação e marcadores** (só durante o arraste).
4. **Dressing das ilhas** (árvores, pedras, vegetação): saturação média, não compete com o item 1.
5. **Paisagem, rio e nuvens:** menor contraste, mais frios ou mais claros (névoa), sempre abaixo das ilhas.

## 4. Paleta ambiental (ponto de partida; ajustar no Pass D)
| Hora | Céu/fog (fundo) | Ilhas (topo) | Regra |
|---|---|---|---|
| Meio-dia | azul-ciano claro (≈ hoje 0,62/0,85/0,91) | verde-folha saturado, mais escuro que o céu | separação de valor ≥ 25 % entre topo da ilha e fundo imediato |
| Entardecer | pêssego/rosa | verde quente, **não** laranja chapado | ilha mantém matiz própria; o fundo leva a cor do pôr do sol |
| Noite | azul-violeta profundo | verde-azulado escuro + luz de lua fria | topo das ilhas com luminância ≥ 2× o fundo; nada de silhueta preta |

Cores de gameplay (cartas, dano, cura) ficam **fora** da paleta ambiental e não podem ser repetidas no dressing.

## 5. Critérios de legibilidade (medidos em 1920×1080, anel de raio 4–6)
- Lucca ≥ **40 px** de altura (hoje 33); morcego ≥ 20 px; lobo 30–50 px.
- Topo da ilha ≥ **120 px** de largura (hoje ≈ 95).
- Árvore grande visível como árvore: ≥ 45 px; nunca cobre mais que 20 % do topo da ilha vizinha nem o Lucca no caminho.
- Anel inteiro + 1 ilha de margem visível em 16:9 **e** 21:9, sem cortar sob a HUD de cartas.
- Mesma seed, mesma hora, trocando só decoração: ortho idêntico (variação 0 %).
- Mesma composição em 12h/18h/22h: o anel continua identificável em escala de cinza (teste: dessaturar a captura).

## 6. Limites de desempenho (propostas — validar com baseline de build)
- Alvo: 60 fps em 1080p em GPU de entrada/média de PC; Editor não serve como prova.
- Dressing por ilha: ≤ 12 objetos, materiais compartilhados (sem `renderer.material`, sem `new Material` por objeto).
- Variação de decoração: nenhuma instância de material nova; GPU instancing só depois de medir.
- Orçamento inicial a confirmar em build: batches da cena ≤ 400, triângulos ≤ 600 mil (Editor hoje: 625 / 909 mil).
- Nenhum efeito screen-space novo sem perfil antes/depois; nuvens volumétricas não ganham passos.

## 7. Referências e exemplos
- Capturas atuais (baseline): `docs/art/baseline/s12345_h12_1920x1080.jpg` (névoa uniforme, fundo gigante),
  `s12345_h18_1920x1080.jpg` (monocromático), `s12345_h22_2520x1080.jpg` (silhuetas).
- Referências externas: Tunic (composição isométrica), Sky: Children of the Light (névoa e luz), Genshin Impact (coesão
  de materiais) — usadas só como princípios. Montar moodboard com prints do responsável em `docs/art/referencias/`
  (pendente; não versionar imagens de terceiros sem licença).

# Auditoria visual — HexPrototipo (estado atual)

Data: 2026-11-14 · Branch: `sprint-1/auditoria-visual` (base: `sprint-0/preparacao-tecnica`, sem mudanças de jogo).
Unity 6000.3.10f1 · URP 17.3 · Unity MCP `com.coplaydev.unity-mcp` **10.2.0** (lock `30d2207`, bridge TCP 6400 funcionando).

Fontes detalhadas: [`audit/MEDIDAS.md`](audit/MEDIDAS.md) (medido no Editor), [`audit/ENVIRONMENT.md`](audit/ENVIRONMENT.md) e
[`audit/RENDERING.md`](audit/RENDERING.md) (análise estática), [`baseline/`](baseline/) (capturas). Toda medida aqui foi lida
do Editor em Play Mode, **não** de build. Valores marcados "(calc.)" são contas a partir de medidas.

## 1. Baseline reproduzível
- Ferramenta: `tools/visual-baseline/run_baseline.py` (comandos `play`, `capture`, `measure`, `stats`, `stop`), C# executado
  pelo MCP em Play Mode. Câmera principal → RenderTexture no tamanho exato → JPG 85. Por captura: seed fixa + `Gerar()`,
  `timeOfDay` forçado com avanço desligado, aspecto forçado + `HexIsoCamera.Enquadrar()`, `timeScale = 0` (sem inimigos).
  Metadados (câmera, ortho, hora, seed, resolução) em `baseline/manifest.json`.
- Matriz: seeds **12345, 7, 2024** × horas **12 / 18 / 22** × **1920×1080 (16:9)** e **2520×1080 (21:9)** = 18 imagens.
- Limitação: HUD (Canvas overlay) e IMGUI não aparecem no render da câmera. Há um print da Game view com HUD
  (`gameview_hud_s12345_h12.png`, só 472×266 — tamanho do painel do Editor).
- **Pendente:** captura de referência do responsável (não recebida); baseline de desempenho em build.

## 2. Hierarquia relevante e quem controla escala/posição
| Objeto (raiz) | Script | Controla | Observação |
|---|---|---|---|
| Main Camera | `HexIsoCamera` | ortho, posição, zoom (a cada 0,25 s) | `margem` 1.3 na cena; enquadra **todos** os renderers filhos das ilhas |
| Mundo | `HexWorld` | ilhas: grade 3 u, folga 0.3, `modeloDaIlha` via `VisualDeIlha.Criar` (diâmetro 4,2) | grade 3 é exigência de design (`Exigencias_Ze_Validacao.md` item 1) |
| Mundo | `DecoracaoDeIlha` | 6 modelos, até 4 por ilha, **tamanho absoluto 0,28–0,65 u pelo maior eixo** | sem categoria; independe de `tamanhoDoHexagono` |
| Mundo / cartas | `Plataforma` + `PlataformaDef.tamanhoDoModelo` | item da carta, **absoluto 0,8–0,95 u** | 7 de 11 cartas sem modelo |
| Inimigos | `Inimigo.MontarVisual` + `InimigoDef.escala` | Lobo 1,1 (prefab skinned), Morcego 0,38 (esfera) | `VisualDeIlha` ignora `SkinnedMeshRenderer` |
| Lucca | primitivas `Corpo` + `Frente` | 0,68 × 1,0 × 0,76 u | `Lucca.prefab` (skinned) não usado; modelo novo em produção por outro agente |
| Cenario background | `CenarioContinuo` (margem 1.5) | maquete 180×32×180 u **reescalada pela projeção da câmera** | sem referência ao `HexWorld` |
| Rio de fundo | `FundoContinuo` (`RioDeFundo`) | copia o transform da maquete | superfície em y ≈ −20 |
| Camada de nuvens | `CamadaDeNuvens` | 18 nuvens ancoradas na câmera, y −8,6…−6,7 | shader `SoftCloud` (24 passos) |
| DayNightController | `Daynightcontroller` (global, `testes shaders/`) | sol, ambient, fog, cor de fundo, todo frame | ciclo de 2 min reais |
| Global Volume | `VolumeCena` | Bloom, Vignette, SMH, ColorAdjustments | único Volume na cena (verificado); o prefab de fundo tem outro no arquivo, fora da instância |

Origem do problema de escala: o commit `b010aa0` (2026-10-07) mudou a grade de **1 → 3** e manteve decorações, itens de
carta e inimigos em tamanhos absolutos pensados para a grade 1.

## 3. Mundo × tela (seed 12345, 1920×1080, ortho 22,48 ≈ 24 px/u)
| Elemento | Mundo (L×A×P, u) | Tela (L×A, px) | Leitura |
|---|---|---|---|
| Ilha — topo | Ø 4,2 (malha hex 3,64 × 0,6 × 4,2) | ≈ 95 × 50 | pequena para o papel de "tabuleiro" |
| Ilha — modelo inteiro (com base pendurada) | 3,99 × 3,45 × 4,2 | ≈ 139 × 141 | a base define o enquadramento |
| Lucca | 0,68 × 1,0 × 0,76 | **24,5 × 33,1** | herói ocupa ~3 % da altura da tela |
| Morcego | 0,38 esfera | 12,9 × 14,4 | quase ilegível |
| Lobo | **3,86 × 7,65 × 14,1** | **≈ 305 × 312** | anômalo: 7,6× o Lucca (bounds do skinned medidos; confirmar com captura) |
| Árvore (decoração) | altura 0,28–0,65 | 9,6 / 13,5 / 20,1 (mín/med/máx) | **menor que o Lucca** |
| Cogumelos / pedra | idem | 10–21 | indistinguíveis da árvore |
| Cepo / tronco | idem | 6,8–14,2 | ruído visual |
| Itens de carta (Coração, Flechas, Moedas, Mola) | ~0,9 | 14–29 × 26–34 | ≈ altura do Lucca |
| Árvores da maquete de fundo | altura típica **5,94** | (calc.) ≈ 120–140 | **≈ 10× a árvore da ilha**, 6× o Lucca |
| Torres / moinho do fundo | 7,9–9,3 | (calc.) ≈ 165–195 | maiores que uma ilha |

Seeds 7 e 2024 dão 22,2 e 25,1 px/u: o zoom varia ±8 % com o formato do anel.

## 4. Sintomas relatados → causa verificada
| Sintoma | Causa (evidência) | Confiança |
|---|---|---|
| Decorações minúsculas | tamanho absoluto 0,28–0,65 u (`DecoracaoDeIlha.cs:27`) numa ilha de 4,2 u; medido 7–21 px | alta (medido) |
| Escalas incoerentes | cada caminho visual tem um número absoluto próprio (decoração, `tamanhoDoModelo`, `InimigoDef.escala`) e nenhum é relativo à ilha ou ao Lucca; lobo inflado pelo bug de bounds de skinned | alta |
| Fundo desconexo | `CenarioContinuo` reescala a maquete para cobrir a tela: árvores de fundo ≈ 6 u contra 0,65 u na ilha — a perspectiva aérea fica invertida (o longe parece maior). Topo da maquete 7,4 u abaixo das ilhas, sem relação com o anel | alta |
| Pouca vegetação baixa | não existe asset de capim/arbusto; só 6 slots nos vértices da ilha; centro e corredores reservados (correto) | alta |
| Rio desconectado | malha fixa da maquete, 16,5 u abaixo das ilhas; o anel passa por cima (vértice a 0,3 u de um centro de ilha) sem margem, foz ou ponte visual | alta |
| Névoa/luz prejudicam a leitura | fog exponencial 0,01 na cor do céu aplicado a tudo; com câmera ortográfica as ilhas (profundidade 49–83) e o fundo recebem quase a mesma névoa → contraste figura/fundo baixo (ver capturas 12h); 18h monocromático; 22h com sol 0,158 deixa as ilhas em silhueta | alta (visual) / média (fórmula do fog em ortho) |
| Bounds da decoração no enquadramento | **refutado**: removendo decorações o ortho não muda (0,00 % nas 3 seeds). Quem domina é a base pendurada do modelo da ilha (só a malha hex daria ortho ≈ 15 % menor) | alta (medido) |

Achados adicionais: sol "por baixo" depois das 18h e subida brusca de intensidade 5h17–6h (`RENDERING.md` §3.4); sem
tonemapping com HDR + Bloom; opaque texture ligada sem consumidor; `new Material` por spawn em `Inimigo`/`Projetil`;
estatísticas do Editor (indicativas): 625 batches, 66 SetPass, **909 mil triângulos** — origem ainda não isolada.

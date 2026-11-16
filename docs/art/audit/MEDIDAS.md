# Medidas do HexPrototipo (baseline visual)

Fonte: Unity Editor 6000.3.10f1, Play Mode, cena `Assets/Scenes/HexPrototipo.unity`. **Valores do Editor, nao de build.** Dados completos em `medidas.json`; reproducao em `tools/visual-baseline/`. Unidade de mundo = 1 m. px = pixels em 1920x1080 com a camera enquadrada.

## 1. Hierarquia da cena (Edit Mode, 28 raizes)

| Raiz | Ativo | Filhos | Componentes |
|---|---|---|---|
| Global Volume | sim | 0 | Volume |
| Mundo | sim | 0 | HexWorld, HexLoopGenerator, NavMeshSurface, NavegacaoDoMundo, DecoracaoDeIlha |
| Sun | sim | 0 | Light, UniversalAdditionalLightData |
| Lucca | sim | 2 | HexPlayer, LuccaStatus, HexLoopRunner, LuccaCombate, NavMeshAgent |
| Main Camera | sim | 0 | Camera, UniversalAdditionalCameraData, HexIsoCamera |
| Controles | sim | 0 | HexPointer, HexPlacementGrid |
| Diretor do Loop | sim | 0 | LoopDirector |
| Inimigos | sim | 0 | InimigoSpawner |
| Cartas | sim | 0 | MapaDePlataformas, MaoDeCartas, CartasUI |
| Cube | nao | 0 | MeshFilter, MeshRenderer, BoxCollider |
| LightProbeGroup | sim | 0 | LightProbeGroup |
| DayNightController | sim | 0 | DayNightController |
| Plane | nao | 0 | MeshFilter, MeshRenderer, MeshCollider |
| Gerador de Moedas | sim | 0 | GeradorDeMoedas |
| HUD | sim | 1 | Canvas, CanvasScaler, GraphicRaycaster, TelaDeMorte |
| EventSystem | sim | 0 | EventSystem, InputSystemUIInputModule |
| DadosDoJogador | sim | 0 | DadosDoJogador |
| QuadCloud | nao | 0 | MeshFilter, MeshRenderer, MeshCollider, CloudController |
| QuadCloud (1) | nao | 0 | MeshFilter, MeshRenderer, MeshCollider, CloudController |
| QuadCloud (2) | nao | 0 | MeshFilter, MeshRenderer, MeshCollider, CloudController |
| QuadCloud (3) | nao | 0 | MeshFilter, MeshRenderer, MeshCollider, CloudController |
| QuadCloud (4) | nao | 0 | MeshFilter, MeshRenderer, MeshCollider, CloudController |
| QuadCloud (5) | nao | 0 | MeshFilter, MeshRenderer, MeshCollider, CloudController |
| IlhaFlutuante | nao | 0 | MeshFilter, MeshRenderer |
| SmoothSmoke | nao | 0 | VisualEffect, VFXRenderer |
| Cenario background | sim | 61 | CenarioContinuo |
| Rio de fundo | sim | 0 | MeshFilter, MeshRenderer, MeshCollider, FundoContinuo, RioDeFundo |
| Camada de nuvens | sim | 18 | CamadaDeNuvens |

Observacoes: Lucca e primitivo (2 filhos `Corpo`+`Frente`, ver 4); raizes inativas (Cube, Plane, QuadCloud x6, IlhaFlutuante, SmoothSmoke) nao renderizam. As ilhas, decoracoes, NavMesh e inimigos sao gerados em runtime (nao existem na hierarquia de Edit Mode).

## 2. Modelos brutos (sem escala; Edit Mode, lidos dos assets)

`VisualDeIlha.Criar` normaliza por bounds de **MeshFilter apenas** e reseta a escala local da raiz do modelo para 1 antes de medir.

| Asset | MeshFilter | SkinnedMR | Escala da raiz | Bounds cru (x,y,z) | min.y | Pivo vertical | Tris |
|---|---|---|---|---|---|---|---|
| HexPrototype/IlhaFlutuante.prefab | sim | nao | 1.0 | 4.826 x 4.177 x 5.081 | 0.0 | base (min.y ~ 0) | 498 |
| HexPrototype/Arvore.prefab | sim | nao | 1.0 | 1.193 x 2.42 x 1.211 | 0.0 | base (min.y ~ 0) | 310 |
| HexPrototype/Pedra.prefab | sim | nao | 1.0 | 0.332 x 0.241 x 0.384 | -0.103 | outro | 48 |
| HexPrototype/CogumeloMarrom.prefab | sim | nao | 1.0 | 0.139 x 0.115 x 0.146 | -0.0 | base (min.y ~ 0) | 316 |
| HexPrototype/CogumeloVermelho.prefab | sim | nao | 1.0 | 0.192 x 0.132 x 0.202 | -0.0 | base (min.y ~ 0) | 728 |
| HexPrototype/CepoDeMadeira.prefab | sim | nao | 1.0 | 0.44 x 0.128 x 0.463 | -0.0 | base (min.y ~ 0) | 136 |
| HexPrototype/TroncoDeMadeira.prefab | sim | nao | 1.0 | 0.429 x 0.313 x 1.111 | -0.143 | outro | 328 |
| HexPrototype/Coracao.prefab | sim | nao | 1.0 | 0.96 x 0.867 x 0.2 | -0.51 | outro | 256 |
| HexPrototype/Flecha.prefab | sim | nao | 1.0 | 0.786 x 1.269 x 1.22 | -0.245 | outro | 974 |
| HexPrototype/Moeda.prefab | sim | nao | 1.0 | 0.673 x 0.869 x 0.122 | -0.06 | outro | 1436 |
| HexPrototype/Mola.prefab | sim | nao | 1.0 | 0.8 x 0.88 x 0.8 | 0.01 | base (min.y ~ 0) | 2208 |
| HexPrototype/BarracaArmada.prefab | sim | nao | 1.0 | 1.894 x 0.973 x 1.936 | 0.0 | base (min.y ~ 0) | 5536 |
| MateusModelos/ArvoreGrande.fbx | sim | nao | 100.0 | 0.019 x 0.018 x 0.023 | -0.008 | outro | 6030 |
| MateusModelos/Arvore.fbx | sim | nao | 1.0 | 1.193 x 2.42 x 1.211 | 0.0 | base (min.y ~ 0) | 310 |
| MateusModelos/Morcegp.fbx | sim | nao | 100.0 | 0.006 x 0.01 x 0.008 | -0.005 | centro (center.y ~ 0) | 612 |
| Lucca.prefab | sim | sim | 1.0 | 4.39 x 2.349 x 3.903 | -0.719 | outro | 3870 |
| MateusModelos/IlhaFlutuante.fbx | sim | nao | 1.0 | 4.826 x 4.177 x 5.081 | 0.0 | base (min.y ~ 0) | 498 |

Notas: para `ArvoreGrande.fbx` e `Morcegp.fbx` a raiz tem escala 100 e o bounds acima e o da malha sem a escala da raiz; com escala 100 o tamanho seria ~100x maior (ArvoreGrande ~1.9 x 1.8 x 2.3). `VisualDeIlha` ignora essa escala (reseta para 1), entao so a normalizacao final importa. `Lucca.prefab` tem SkinnedMeshRenderer (bounds de localBounds), mas **nao e usado** na cena (Lucca da cena e capsula). `Pedra`, `TroncoDeMadeira`, `Coracao`, `Flecha`, `Moeda` tem pivo no centro/fora da base ('outro'); `VisualDeIlha` recentra em XZ e poe a base em y=0.

## 3.1 Seed 12345

- Ilhas: 24; orthoSize 1080p = 22.484; escala da tela = 24.02 px por unidade de mundo (ortografica).
- Modelo da ilha (mundo): 3.989 x 3.453 x 4.2; topo do modelo relativo ao tile (min/max) = 0.0 / 0.0; topo do collider relativo ao tile = 0.0.
- Hex mesh (renderer desligado) bounds 3.637 x 0.6 x 4.2; MeshCollider bounds 3.637 x 0.6 x 4.2.
- **Ilha em px (1080p)**: bounds do modelo inteiro (com a parte pendurada) 139.1 x 141.4 px (mediana, AABB de tela); superficie do topo 95.4 x 50.3 px (mediana).
- Alturas Altura dos tiles: min 0.0, max 0.0.

**Decoracoes** (85 instancias; `tamanho` = maior eixo local apos normalizacao; px = AABB de tela dos bounds de mundo / OBB = caixa orientada)

| Modelo | N | tamanho min/med/max | altura mundo min/med/max | px alt AABB min/med/max | px larg AABB med | px alt OBB med |
|---|---|---|---|---|---|---|
| Arvore | 15 | 0.302 / 0.413 / 0.634 | 0.302 / 0.413 / 0.634 | 9.6 / 13.5 / 20.1 | 9.5 | 11.6 |
| CepoDeMadeira | 18 | 0.293 / 0.398 / 0.556 | 0.081 / 0.11 / 0.154 | 7.2 / 10.8 / 14.2 | 17.1 | 8.3 |
| CogumeloMarrom | 16 | 0.308 / 0.391 / 0.551 | 0.243 / 0.308 / 0.434 | 11.1 / 14.7 / 21.0 | 16.8 | 12.8 |
| CogumeloVermelho | 12 | 0.294 / 0.443 / 0.553 | 0.192 / 0.289 / 0.361 | 10.5 / 15.3 / 20.4 | 19.2 | 12.7 |
| Pedra | 12 | 0.319 / 0.433 / 0.584 | 0.201 / 0.273 / 0.368 | 10.6 / 14.6 / 19.8 | 18.1 | 11.9 |
| TroncoDeMadeira | 12 | 0.301 / 0.476 / 0.648 | 0.085 / 0.134 / 0.183 | 6.8 / 10.1 / 13.6 | 14.6 | 7.9 |

**Lucca** (HexPlayer): bounds de mundo 0.683 x 1.0 x 0.763 (Corpo MeshRenderer cilindro/capsula + Frente), base em y=0.0 do pivo, **24.5 x 33.1 px (L x A)** em 1080p. Agente NavMesh raio 0.2, altura 1.0.
- Inimigo Morcego (spawnado temporariamente via `InimigoSpawner.Nascer`, escala def 0.38, prefab -, usa SkinnedMR: False): bounds 0.38 x 0.38 x 0.38, **12.9 x 14.4 px**, altura acima do topo do tile 1.1.
- Inimigo Lobo (spawnado temporariamente via `InimigoSpawner.Nascer`, escala def 1.1, prefab Lobo (1), usa SkinnedMR: True): bounds 3.858 x 7.648 x 14.126, **305.4 x 311.8 px**, altura acima do topo do tile 0.033.
- Cartas com `modelo` (aplicadas como `Plataforma.Montar`, instancia temporaria sobre o acampamento; nada colocado de fato):
  - Coracao (prefab Coracao, tamanhoDoModelo 0.9, altura 0.35): bounds 0.9 x 0.813 x 0.188, 18.5 x 26.1 px.
  - Flechas (prefab Flecha, tamanhoDoModelo 0.9, altura 0.35): bounds 0.558 x 0.9 x 0.865, 24.2 x 30.8 px.
  - Moedas (prefab Moeda, tamanhoDoModelo 0.9, altura 0.35): bounds 0.698 x 0.9 x 0.126, 14.0 x 25.7 px.
  - Mola (prefab Mola, tamanhoDoModelo 0.95, altura 0.03): bounds 0.864 x 0.95 x 0.864, 29.3 x 34.4 px.

## 3.2 Seed 7

- Ilhas: 30; orthoSize 1080p = 24.335; escala da tela = 22.19 px por unidade de mundo (ortografica).
- Modelo da ilha (mundo): 3.989 x 3.453 x 4.2; topo do modelo relativo ao tile (min/max) = 0.0 / 0.0; topo do collider relativo ao tile = 0.0.
- Hex mesh (renderer desligado) bounds 3.637 x 0.6 x 4.2; MeshCollider bounds 3.637 x 0.6 x 4.2.
- **Ilha em px (1080p)**: bounds do modelo inteiro (com a parte pendurada) 128.5 x 130.6 px (mediana, AABB de tela); superficie do topo 88.1 x 46.5 px (mediana).
- Alturas Altura dos tiles: min 0.0, max 0.0.

**Decoracoes** (104 instancias; `tamanho` = maior eixo local apos normalizacao; px = AABB de tela dos bounds de mundo / OBB = caixa orientada)

| Modelo | N | tamanho min/med/max | altura mundo min/med/max | px alt AABB min/med/max | px larg AABB med | px alt OBB med |
|---|---|---|---|---|---|---|
| Arvore | 16 | 0.3 / 0.406 / 0.65 | 0.3 / 0.406 / 0.65 | 8.4 / 12.0 / 18.2 | 8.4 | 10.4 |
| CepoDeMadeira | 23 | 0.293 / 0.449 / 0.541 | 0.081 / 0.124 / 0.15 | 6.7 / 11.0 / 14.3 | 17.1 | 8.0 |
| CogumeloMarrom | 19 | 0.287 / 0.424 / 0.572 | 0.226 / 0.334 / 0.45 | 9.4 / 14.3 / 19.5 | 16.1 | 12.2 |
| CogumeloVermelho | 15 | 0.282 / 0.421 / 0.546 | 0.184 / 0.275 / 0.356 | 9.6 / 13.5 / 18.2 | 17.1 | 11.5 |
| Pedra | 13 | 0.295 / 0.405 / 0.584 | 0.186 / 0.255 / 0.368 | 8.7 / 13.2 / 19.1 | 15.9 | 9.7 |
| TroncoDeMadeira | 18 | 0.282 / 0.475 / 0.616 | 0.079 / 0.134 / 0.173 | 5.7 / 8.8 / 12.6 | 12.1 | 6.9 |

**Lucca** (HexPlayer): bounds de mundo 0.683 x 1.0 x 0.763 (Corpo MeshRenderer cilindro/capsula + Frente), base em y=0.0 do pivo, **22.7 x 30.6 px (L x A)** em 1080p. Agente NavMesh raio 0.2, altura 1.0.
- Inimigo Morcego (spawnado temporariamente via `InimigoSpawner.Nascer`, escala def 0.38, prefab -, usa SkinnedMR: False): bounds 0.38 x 0.38 x 0.38, **11.9 x 13.3 px**, altura acima do topo do tile 1.1.
- Inimigo Lobo (spawnado temporariamente via `InimigoSpawner.Nascer`, escala def 1.1, prefab Lobo (1), usa SkinnedMR: True): bounds 3.858 x 7.648 x 14.126, **282.2 x 288.1 px**, altura acima do topo do tile 0.033.
- Cartas com `modelo` (aplicadas como `Plataforma.Montar`, instancia temporaria sobre o acampamento; nada colocado de fato):
  - Coracao (prefab Coracao, tamanhoDoModelo 0.9, altura 0.35): bounds 0.9 x 0.813 x 0.188, 17.1 x 24.2 px.
  - Flechas (prefab Flecha, tamanhoDoModelo 0.9, altura 0.35): bounds 0.558 x 0.9 x 0.865, 22.3 x 28.5 px.
  - Moedas (prefab Moeda, tamanhoDoModelo 0.9, altura 0.35): bounds 0.698 x 0.9 x 0.126, 12.9 x 23.8 px.
  - Mola (prefab Mola, tamanhoDoModelo 0.95, altura 0.03): bounds 0.864 x 0.95 x 0.864, 27.1 x 31.8 px.

## 3.3 Seed 2024

- Ilhas: 24; orthoSize 1080p = 21.501; escala da tela = 25.12 px por unidade de mundo (ortografica).
- Modelo da ilha (mundo): 3.989 x 3.453 x 4.2; topo do modelo relativo ao tile (min/max) = 0.0 / 0.0; topo do collider relativo ao tile = 0.0.
- Hex mesh (renderer desligado) bounds 3.637 x 0.6 x 4.2; MeshCollider bounds 3.637 x 0.6 x 4.2.
- **Ilha em px (1080p)**: bounds do modelo inteiro (com a parte pendurada) 145.4 x 147.8 px (mediana, AABB de tela); superficie do topo 99.7 x 52.6 px (mediana).
- Alturas Altura dos tiles: min 0.0, max 0.0.

**Decoracoes** (86 instancias; `tamanho` = maior eixo local apos normalizacao; px = AABB de tela dos bounds de mundo / OBB = caixa orientada)

| Modelo | N | tamanho min/med/max | altura mundo min/med/max | px alt AABB min/med/max | px larg AABB med | px alt OBB med |
|---|---|---|---|---|---|---|
| Arvore | 20 | 0.291 / 0.453 / 0.646 | 0.291 / 0.453 / 0.646 | 9.6 / 14.9 / 21.9 | 9.8 | 13.3 |
| CepoDeMadeira | 18 | 0.291 / 0.449 / 0.556 | 0.081 / 0.124 / 0.154 | 6.9 / 12.4 / 15.8 | 19.5 | 9.4 |
| CogumeloMarrom | 11 | 0.287 / 0.421 / 0.513 | 0.226 / 0.332 / 0.404 | 10.7 / 16.6 / 20.3 | 17.8 | 13.9 |
| CogumeloVermelho | 13 | 0.308 / 0.417 / 0.553 | 0.201 / 0.272 / 0.361 | 11.0 / 15.3 / 21.3 | 19.4 | 12.9 |
| Pedra | 11 | 0.323 / 0.502 / 0.584 | 0.203 / 0.316 / 0.368 | 10.4 / 16.1 / 20.7 | 18.3 | 13.6 |
| TroncoDeMadeira | 13 | 0.281 / 0.441 / 0.648 | 0.079 / 0.124 / 0.183 | 6.3 / 9.8 / 14.3 | 14.1 | 7.3 |

**Lucca** (HexPlayer): bounds de mundo 0.683 x 1.0 x 0.763 (Corpo MeshRenderer cilindro/capsula + Frente), base em y=0.0 do pivo, **25.7 x 34.6 px (L x A)** em 1080p. Agente NavMesh raio 0.2, altura 1.0.
- Inimigo Morcego (spawnado temporariamente via `InimigoSpawner.Nascer`, escala def 0.38, prefab -, usa SkinnedMR: False): bounds 0.38 x 0.38 x 0.38, **13.5 x 15.0 px**, altura acima do topo do tile 1.1.
- Inimigo Lobo (spawnado temporariamente via `InimigoSpawner.Nascer`, escala def 1.1, prefab Lobo (1), usa SkinnedMR: True): bounds 3.858 x 7.648 x 14.126, **319.4 x 326.0 px**, altura acima do topo do tile 0.033.
- Cartas com `modelo` (aplicadas como `Plataforma.Montar`, instancia temporaria sobre o acampamento; nada colocado de fato):
  - Coracao (prefab Coracao, tamanhoDoModelo 0.9, altura 0.35): bounds 0.9 x 0.813 x 0.188, 19.3 x 27.3 px.
  - Flechas (prefab Flecha, tamanhoDoModelo 0.9, altura 0.35): bounds 0.558 x 0.9 x 0.865, 25.3 x 32.2 px.
  - Moedas (prefab Moeda, tamanhoDoModelo 0.9, altura 0.35): bounds 0.698 x 0.9 x 0.126, 14.6 x 26.9 px.
  - Mola (prefab Mola, tamanhoDoModelo 0.95, altura 0.03): bounds 0.864 x 0.95 x 0.864, 30.7 x 36.0 px.

## 4. Camera: orthoSize com e sem decoracoes (formula de `HexIsoCamera.MedirIlhas` replicada)

A = todos os renderers filhos das ilhas (formula real; confere com o orthoSize do Editor), B = sem `Decoracoes`, C = so modelo da ilha, D = so MeshRenderer do hexagono (desligado), E = so decoracoes. margem=1.3 (cena), folga=tamanhoDoHexagono=3.

| Seed | Aspecto | orthoSize Editor | A | B (sem deco) | delta A vs B | C | D | E (so deco) | Limitado por |
|---|---|---|---|---|---|---|---|---|---|
| 12345 | 1920x1080 | 22.484 | 22.484 | 22.484 | 0.0% | 22.484 | 19.191 | 17.936 | altura(Y) |
| 12345 | 2520x1080 | 22.484 | 22.484 | 22.484 | 0.0% | 22.484 | 19.191 | 17.936 | altura(Y) |
| 7 | 1920x1080 | 24.335 | 24.335 | 24.335 | 0.0% | 24.335 | 21.164 | 20.66 | altura(Y) |
| 7 | 2520x1080 | 24.335 | 24.335 | 24.335 | 0.0% | 24.335 | 21.041 | 19.677 | altura(Y) |
| 2024 | 1920x1080 | 21.501 | 21.501 | 21.501 | 0.0% | 21.501 | 18.207 | 16.574 | altura(Y) |
| 2024 | 2520x1080 | 21.501 | 21.501 | 21.501 | 0.0% | 21.501 | 18.207 | 16.574 | altura(Y) |

Leitura numerica: nas tres seeds os bounds das decoracoes nao alteram o enquadramento (delta 0%) porque o modelo da ilha (com a parte pendurada, ~3.45 m abaixo do topo) ja define os extremos. Considerar so o mesh do hexagono (D) daria orthoSize menor, mostrando quanto o modelo da ilha pesa. Em 21:9 o orthoSize e igual ao de 16:9 (limitado pela altura), so a largura visivel cresce.

## 5. Fundo / ambiente (seed 12345, 1920x1080)

- Anel de ilhas: centro [-0.32, 0.0, -0.19], raio max centro-a-centro XZ 21.11, Y das ilhas de -3.45 (base pendurada) a 0.63 (topo, decoracoes); profundidade na camera (z camera-espaco) dos renderers de 49.0 a 83.0.
- **Cenario background (raiz)**: posicao [19.55, -29.21, 27.1], escala mundo [7.88, 7.88, 6.45], bounds [180.2, 32.0, 180.3] (min [-54.6, -42.9, -54.6], max [125.5, -10.9, 125.7]), topo Y -10.87 = 7.41 m abaixo da base das ilhas, profundidade camera-z do centro 124.7 (anel 49.0 a 83.0), distancia XZ ao centro do anel 50.6.
- **Rio de fundo**: posicao [19.55, -29.21, 27.1], escala mundo [7.88, 7.88, 6.45], bounds [66.1, 11.3, 179.6] (min [-32.5, -31.2, -53.9], max [33.6, -19.9, 125.7]), topo Y -19.93 = 16.47 m abaixo da base das ilhas, profundidade camera-z do centro 102.9 (anel 49.0 a 83.0), distancia XZ ao centro do anel 36.1.
- `CenarioContinuo.margem` = 1.5; `agua` nao atribuida (o rio tem seu proprio `FundoContinuo`).

Filhos do cenario por tipo (61 filhos): N, altura de mundo tipica, Y max, camera-z min/max (comparar com anel 49.0..83.0), quantos tem centro de tela no viewport 1920x1080:

| Tipo | N | altura bounds med | yMax min/max | cam-z min/max | mais perto da camera que o anel | no viewport |
|---|---|---|---|---|---|---|
| Arvore | 53 | 5.94 | -24.6 / -13.1 | 39.7 / 180.1 | 5 | 22 |
| Pedras | 1 | 18.69 | -10.9 / -10.9 | 116.8 / 116.8 | 0 | 1 |
| Plane.001 | 1 | 26.4 | -16.5 / -16.5 | 126.3 / 126.3 | 0 | 1 |
| Torre | 2 | 9.25 | -18.3 / -15.8 | 89.3 / 144.1 | 0 | 1 |
| Torre2 | 2 | 8.63 | -19.6 / -15.2 | 55.1 / 125.5 | 0 | 1 |
| banderas | 1 | 8.88 | -17.5 / -17.5 | 125.0 / 125.0 | 0 | 1 |
| moinho | 1 | 7.89 | -20.8 / -20.8 | 140.1 / 140.1 | 0 | 1 |

Nenhum filho do cenario tem yMax acima de -3.45 (base das ilhas)?  sim (maior yMax do cenario: -10.87).

**Rio** (seeds): superficie Y e vertice mais perto de um centro de ilha (distancia XZ):
- seed 12345: superficie Y -20.0 (base das ilhas -3.45 => rio 16.54 m abaixo), vertice mais perto a 0.31 m em XZ do centro da ilha `Caminho (2, -3)` (raio visual da ilha 2.1), 1677 vertices; ilhas dentro do AABB XZ do rio: 24.
- seed 7: superficie Y -20.75 (base das ilhas -3.45 => rio 17.3 m abaixo), vertice mais perto a 0.28 m em XZ do centro da ilha `Caminho (4, 1)` (raio visual da ilha 2.1), 1677 vertices; ilhas dentro do AABB XZ do rio: 30.
- seed 2024: superficie Y -19.6 (base das ilhas -3.45 => rio 16.15 m abaixo), vertice mais perto a 0.45 m em XZ do centro da ilha `Caminho (1, -4)` (raio visual da ilha 2.1), 1677 vertices; ilhas dentro do AABB XZ do rio: 24.

**Nuvens** (CamadaDeNuvens, 18): Y de -8.64 a -6.67 (base das ilhas -3.45; folga 3.22 m abaixo das ilhas), camera-z 59.7 a 125.9; 11 atras do anel em profundidade, 0 na frente.

O fundo e reposicionado/reescalado todo frame (`CenarioContinuo`, `FundoContinuo`, nuvens ancoradas na camera): posicoes acima valem para este enquadramento/aspecto (os arquivos de 21:9 estao em `medidas.json` > `fundo_2520x1080`).

## 6. Iluminacao por hora (seed 12345; DayNightController, sunrise 6 / sunset 18)

| Hora | Sol rot (euler) | Sol cor RGB | Intens. | Ambient (modo) sky / equator / ground | Fog (modo, dens., cor) | Camera clear / bg |
|---|---|---|---|---|---|---|
| 12.0 (noon) | [90.0, 330.0, 0.0] | (0.95, 0.96, 1.0) | 1.4 | Trilight: (0.62, 0.85, 0.91) / (0.72, 0.88, 0.94) / (0.31, 0.42, 0.45) | Exponential, 0.01, (0.62, 0.85, 0.91) | SolidColor / (0.62, 0.85, 0.91) |
| 18.0 (dusk) | [-0.0, 150.0, 180.0] | (1.0, 0.66, 0.55) | 0.7 | Trilight: (0.95, 0.58, 0.48) / (0.96, 0.6, 0.5) / (0.47, 0.29, 0.24) | Exponential, 0.01, (0.95, 0.58, 0.48) | SolidColor / (0.95, 0.58, 0.48) |
| 22.0 (night) | [300.0, 150.0, 180.0] | (0.53, 0.46, 0.55) | 0.158 | Trilight: (0.43, 0.34, 0.43) / (0.46, 0.38, 0.46) / (0.21, 0.17, 0.21) | Exponential, 0.01, (0.43, 0.34, 0.43) | SolidColor / (0.43, 0.34, 0.43) |

Sombras do sol: Soft, forca 1.0. Camera URP: post-processing True, antialiasing None, HDR True; skybox nenhum (SolidColor). Fog mode Exponential: start/end nao usados (valores 0.0 / 300.0). O fog tem a cor do ceu/ambient em todas as horas (gerado por `DayNightController`).

Volume: `Global Volume` (global=True, peso 1.0), perfil `VolumeCena`. Overrides ativos:
- Bloom (ativo=True): {'threshold': '1', 'intensity': '0.6', 'scatter': '0.6'}
- ColorAdjustments (ativo=True): {'postExposure': '0.1', 'contrast': '12', 'saturation': '20'}
- ShadowsMidtonesHighlights (ativo=True): {'shadows': '(0.84, 0.87, 1.00, 0.00)', 'highlights': '(1.00, 0.99, 0.84, 0.00)'}
- Vignette (ativo=True): {'intensity': '0.2', 'smoothness': '0.45'}

A mesma configuracao de Volume vale para todas as horas (o ciclo so altera sol, ambient, fog e cor de fundo).

## 7. Estatisticas de render (EDITOR, nao build)

Seed 12345, 12h, Main Camera desenhando em RenderTexture 1920x1080 (`UnityStats.screenRes` reporta 472x266, o tamanho da Game view); mediana de 12 amostras (0.5 s) de `UnityEngine.UnityStats`: batches 625, setPassCalls 66, drawCalls 626, triangulos 908,771, vertices 1,014,793, shadowCasters 12.
Tempo medio de frame do Editor: 4.29 ms (232.9 fps) sobre 1418 frames em 6.09 s (frames contados por `Time.frameCount`, inclui overhead do Editor, timeScale 0; **nao** e desempenho de build).
Incerteza: `UnityStats` reflete o ultimo frame renderizado pelo Editor; como a Game view e a RenderTexture podem ser renderizadas no mesmo frame, os totais podem somar mais de uma passada de camera.

## 8. Limitacoes / nao medido

- Capturas por `Camera.Render()`: UI Screen Space Overlay (HUD) e IMGUI nao entram; um screenshot da Game view (472x266, tamanho do painel do Editor) com HUD esta em `docs/art/baseline/gameview_hud_s12345_h12.png`.
- Inimigos nao existem com `timeScale=0` logo apos gerar o mapa; foram spawnados temporariamente (Morcego e Lobo) so para medir. O Lobo usa `Lobo (1).fbx` (SkinnedMeshRenderer apenas): `VisualDeIlha` mede so MeshFilter, entao a escala resultante e anormal (bounds 3.86 x 7.65 x 14.13 m) - valor real lido do Editor, nao interpretado.
- Modelos `Arvore`/`Morcego` novos (Mateus) so medidos como assets; nenhum usado na cena.
- Medidas de px usam `Camera.WorldToScreenPoint` com a camera em RenderTexture 1920x1080 e aspecto forcado; bounds de renderers desabilitados (hex mesh) nao entram nos px.

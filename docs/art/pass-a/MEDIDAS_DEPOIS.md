# Medidas do HexPrototipo (depois do Pass A)

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

- Ilhas: 24; orthoSize 1080p = 16.829; escala da tela = 32.09 px por unidade de mundo (ortografica).
- Modelo da ilha (mundo): 3.989 x 3.453 x 4.2; topo do modelo relativo ao tile (min/max) = 0.0 / 0.0; topo do collider relativo ao tile = 0.0.
- Hex mesh (renderer desligado) bounds 3.637 x 0.6 x 4.2; MeshCollider bounds 3.637 x 0.6 x 4.2.
- **Ilha em px (1080p)**: bounds do modelo inteiro (com a parte pendurada) 185.8 x 188.9 px (mediana, AABB de tela); superficie do topo 127.4 x 67.3 px (mediana).
- Alturas Altura dos tiles: min 0.0, max 0.0.

**Decoracoes** (79 instancias; `tamanho` = maior eixo local apos normalizacao; px = AABB de tela dos bounds de mundo / OBB = caixa orientada)

| Modelo | N | tamanho min/med/max | altura mundo min/med/max | px alt AABB min/med/max | px larg AABB med | px alt OBB med |
|---|---|---|---|---|---|---|
| Arvore | 30 | 1.137 / 1.308 / 2.296 | 1.137 / 1.308 / 2.296 | 48.6 / 56.3 / 99.6 | 39.0 | 49.5 |
| CepoDeMadeira | 12 | 0.311 / 0.385 / 0.493 | 0.086 / 0.107 / 0.137 | 10.2 / 14.1 / 17.0 | 21.9 | 9.9 |
| CogumeloMarrom | 16 | 0.268 / 0.444 / 0.563 | 0.211 / 0.35 / 0.443 | 13.3 / 22.4 / 28.7 | 25.4 | 18.7 |
| CogumeloVermelho | 7 | 0.31 / 0.393 / 0.527 | 0.202 / 0.257 / 0.344 | 12.6 / 19.4 / 24.1 | 24.6 | 13.5 |
| Pedra | 9 | 0.338 / 0.43 / 0.492 | 0.213 / 0.27 / 0.31 | 14.5 / 17.2 / 23.1 | 19.4 | 16.6 |
| TroncoDeMadeira | 5 | 0.524 / 0.583 / 0.709 | 0.147 / 0.164 / 0.2 | 15.2 / 15.9 / 20.2 | 23.5 | 13.1 |

**Lucca** (HexPlayer): bounds de mundo 0.759 x 1.0 x 0.691 (Corpo MeshRenderer cilindro/capsula + Frente), base em y=0.0 do pivo, **32.9 x 44.2 px (L x A)** em 1080p. Agente NavMesh raio 0.2, altura 1.0.
- Inimigo Morcego (spawnado temporariamente via `InimigoSpawner.Nascer`, escala def 0.6, prefab -, usa SkinnedMR: False): bounds 0.6 x 0.6 x 0.6, **27.2 x 30.3 px**, altura acima do topo do tile 1.1.
- Inimigo Lobo (spawnado temporariamente via `InimigoSpawner.Nascer`, escala def 1.5, prefab Lobo (1), usa SkinnedMR: True): bounds 0.433 x 0.859 x 1.586, **45.8 x 46.8 px**, altura acima do topo do tile 0.033.
- Cartas com `modelo` (aplicadas como `Plataforma.Montar`, instancia temporaria sobre o acampamento; nada colocado de fato):
  - Coracao (prefab Coracao, tamanhoDoModelo 0.9, altura 0.35): bounds 0.9 x 0.813 x 0.188, 24.7 x 34.9 px.
  - Flechas (prefab Flecha, tamanhoDoModelo 0.9, altura 0.35): bounds 0.558 x 0.9 x 0.865, 32.3 x 41.2 px.
  - Moedas (prefab Moeda, tamanhoDoModelo 0.9, altura 0.35): bounds 0.698 x 0.9 x 0.126, 18.7 x 34.4 px.
  - Mola (prefab Mola, tamanhoDoModelo 0.95, altura 0.03): bounds 0.864 x 0.95 x 0.864, 39.2 x 46.0 px.

## 3.2 Seed 7

- Ilhas: 30; orthoSize 1080p = 18.559; escala da tela = 29.1 px por unidade de mundo (ortografica).
- Modelo da ilha (mundo): 3.989 x 3.453 x 4.2; topo do modelo relativo ao tile (min/max) = 0.0 / 0.0; topo do collider relativo ao tile = 0.0.
- Hex mesh (renderer desligado) bounds 3.637 x 0.6 x 4.2; MeshCollider bounds 3.637 x 0.6 x 4.2.
- **Ilha em px (1080p)**: bounds do modelo inteiro (com a parte pendurada) 168.5 x 171.3 px (mediana, AABB de tela); superficie do topo 115.6 x 61.0 px (mediana).
- Alturas Altura dos tiles: min 0.0, max 0.0.

**Decoracoes** (98 instancias; `tamanho` = maior eixo local apos normalizacao; px = AABB de tela dos bounds de mundo / OBB = caixa orientada)

| Modelo | N | tamanho min/med/max | altura mundo min/med/max | px alt AABB min/med/max | px larg AABB med | px alt OBB med |
|---|---|---|---|---|---|---|
| Arvore | 32 | 1.111 / 1.326 / 2.24 | 1.111 / 1.326 / 2.24 | 41.9 / 51.2 / 86.9 | 35.9 | 44.9 |
| CepoDeMadeira | 12 | 0.323 / 0.407 / 0.486 | 0.089 / 0.113 / 0.135 | 9.3 / 14.2 / 15.8 | 22.7 | 9.3 |
| CogumeloMarrom | 11 | 0.278 / 0.428 / 0.514 | 0.219 / 0.337 / 0.405 | 12.5 / 19.3 / 24.4 | 21.6 | 16.7 |
| CogumeloVermelho | 13 | 0.31 / 0.448 / 0.563 | 0.202 / 0.292 / 0.368 | 11.4 / 18.7 / 23.1 | 22.2 | 16.0 |
| Pedra | 20 | 0.309 / 0.451 / 0.548 | 0.195 / 0.284 / 0.345 | 11.8 / 17.2 / 22.9 | 19.6 | 14.9 |
| TroncoDeMadeira | 10 | 0.502 / 0.577 / 0.686 | 0.141 / 0.163 / 0.193 | 13.3 / 14.4 / 17.9 | 21.3 | 11.9 |

**Lucca** (HexPlayer): bounds de mundo 0.759 x 1.0 x 0.691 (Corpo MeshRenderer cilindro/capsula + Frente), base em y=0.0 do pivo, **29.8 x 40.1 px (L x A)** em 1080p. Agente NavMesh raio 0.2, altura 1.0.
- Inimigo Morcego (spawnado temporariamente via `InimigoSpawner.Nascer`, escala def 0.6, prefab -, usa SkinnedMR: False): bounds 0.6 x 0.6 x 0.6, **24.7 x 27.5 px**, altura acima do topo do tile 1.1.
- Inimigo Lobo (spawnado temporariamente via `InimigoSpawner.Nascer`, escala def 1.5, prefab Lobo (1), usa SkinnedMR: True): bounds 0.433 x 0.859 x 1.586, **41.5 x 42.4 px**, altura acima do topo do tile 0.033.
- Cartas com `modelo` (aplicadas como `Plataforma.Montar`, instancia temporaria sobre o acampamento; nada colocado de fato):
  - Coracao (prefab Coracao, tamanhoDoModelo 0.9, altura 0.35): bounds 0.9 x 0.813 x 0.188, 22.4 x 31.7 px.
  - Flechas (prefab Flecha, tamanhoDoModelo 0.9, altura 0.35): bounds 0.558 x 0.9 x 0.865, 29.3 x 37.3 px.
  - Moedas (prefab Moeda, tamanhoDoModelo 0.9, altura 0.35): bounds 0.698 x 0.9 x 0.126, 17.0 x 31.2 px.
  - Mola (prefab Mola, tamanhoDoModelo 0.95, altura 0.03): bounds 0.864 x 0.95 x 0.864, 35.5 x 41.7 px.

## 3.3 Seed 2024

- Ilhas: 24; orthoSize 1080p = 15.966; escala da tela = 33.82 px por unidade de mundo (ortografica).
- Modelo da ilha (mundo): 3.989 x 3.453 x 4.2; topo do modelo relativo ao tile (min/max) = 0.0 / 0.0; topo do collider relativo ao tile = 0.0.
- Hex mesh (renderer desligado) bounds 3.637 x 0.6 x 4.2; MeshCollider bounds 3.637 x 0.6 x 4.2.
- **Ilha em px (1080p)**: bounds do modelo inteiro (com a parte pendurada) 195.8 x 199.1 px (mediana, AABB de tela); superficie do topo 134.3 x 70.9 px (mediana).
- Alturas Altura dos tiles: min 0.0, max 0.0.

**Decoracoes** (80 instancias; `tamanho` = maior eixo local apos normalizacao; px = AABB de tela dos bounds de mundo / OBB = caixa orientada)

| Modelo | N | tamanho min/med/max | altura mundo min/med/max | px alt AABB min/med/max | px larg AABB med | px alt OBB med |
|---|---|---|---|---|---|---|
| Arvore | 33 | 1.112 / 1.365 / 2.346 | 1.112 / 1.365 / 2.346 | 50.8 / 60.9 / 107.1 | 41.6 | 55.4 |
| CepoDeMadeira | 12 | 0.311 / 0.402 / 0.493 | 0.086 / 0.112 / 0.137 | 10.8 / 14.5 / 18.0 | 21.8 | 11.6 |
| CogumeloMarrom | 14 | 0.268 / 0.444 / 0.549 | 0.211 / 0.349 / 0.433 | 14.0 / 23.8 / 29.4 | 26.4 | 19.2 |
| CogumeloVermelho | 11 | 0.308 / 0.501 / 0.563 | 0.201 / 0.327 / 0.368 | 14.2 / 24.0 / 26.9 | 28.9 | 20.7 |
| Pedra | 4 | 0.309 / 0.458 / 0.477 | 0.195 / 0.288 / 0.3 | 13.7 / 20.6 / 23.8 | 24.3 | 17.1 |
| TroncoDeMadeira | 6 | 0.502 / 0.577 / 0.709 | 0.141 / 0.162 / 0.2 | 15.5 / 16.9 / 21.3 | 24.6 | 13.6 |

**Lucca** (HexPlayer): bounds de mundo 0.759 x 1.0 x 0.691 (Corpo MeshRenderer cilindro/capsula + Frente), base em y=0.0 do pivo, **34.7 x 46.6 px (L x A)** em 1080p. Agente NavMesh raio 0.2, altura 1.0.
- Inimigo Morcego (spawnado temporariamente via `InimigoSpawner.Nascer`, escala def 0.6, prefab -, usa SkinnedMR: False): bounds 0.6 x 0.6 x 0.6, **28.7 x 31.9 px**, altura acima do topo do tile 1.1.
- Inimigo Lobo (spawnado temporariamente via `InimigoSpawner.Nascer`, escala def 1.5, prefab Lobo (1), usa SkinnedMR: True): bounds 0.433 x 0.859 x 1.586, **48.3 x 49.3 px**, altura acima do topo do tile 0.033.
- Cartas com `modelo` (aplicadas como `Plataforma.Montar`, instancia temporaria sobre o acampamento; nada colocado de fato):
  - Coracao (prefab Coracao, tamanhoDoModelo 0.9, altura 0.35): bounds 0.9 x 0.813 x 0.188, 26.0 x 36.8 px.
  - Flechas (prefab Flecha, tamanhoDoModelo 0.9, altura 0.35): bounds 0.558 x 0.9 x 0.865, 34.0 x 43.4 px.
  - Moedas (prefab Moeda, tamanhoDoModelo 0.9, altura 0.35): bounds 0.698 x 0.9 x 0.126, 19.7 x 36.2 px.
  - Mola (prefab Mola, tamanhoDoModelo 0.95, altura 0.03): bounds 0.864 x 0.95 x 0.864, 41.3 x 48.5 px.

## 4. Camera: orthoSize com e sem decoracoes (formula de `HexIsoCamera.MedirIlhas` replicada)

Populacoes de renderers: A = todos os renderers filhos das ilhas (formula ANTIGA), B = sem `Decoracoes`, C = so modelo da ilha, **D = so a malha do hexagono (formula REAL desde o Pass A; confere com o orthoSize do Editor)**, E = so decoracoes. margem=1.14 (cena), folga=tamanhoDoHexagono=3. A coluna 'orthoSize Editor' e o valor real; so D deve bater com ele.

| Seed | Aspecto | orthoSize Editor | A | B (sem deco) | delta A vs B | C | D | E (so deco) | Limitado por |
|---|---|---|---|---|---|---|---|---|---|
| 12345 | 1920x1080 | 16.829 | 19.717 | 19.717 | 0.0% | 19.717 | 16.829 | 16.039 | altura(Y) |
| 12345 | 2520x1080 | 16.829 | 19.717 | 19.717 | 0.0% | 19.717 | 16.829 | 16.039 | altura(Y) |
| 7 | 1920x1080 | 18.559 | 21.339 | 21.339 | 0.0% | 21.339 | 18.559 | 18.823 | altura(Y) |
| 7 | 2520x1080 | 18.452 | 21.339 | 21.339 | 0.0% | 21.339 | 18.452 | 18.823 | altura(Y) |
| 2024 | 1920x1080 | 15.966 | 18.854 | 18.854 | 0.0% | 18.854 | 15.966 | 16.135 | altura(Y) |
| 2024 | 2520x1080 | 15.966 | 18.854 | 18.854 | 0.0% | 18.854 | 15.966 | 16.135 | altura(Y) |

Leitura numerica: o orthoSize real (coluna Editor) e igual a D em todas as linhas: o enquadramento depende so da malha do hexagono, nao do modelo da ilha, das decoracoes nem de plataformas. A/B/C/E ficam como referencia do que a formula antiga mediria. Em algumas seeds o 21:9 tem orthoSize menor que o 16:9 (o anel passa a ser limitado pela largura no 16:9).

## 5. Fundo / ambiente (seed 12345, 1920x1080)

- Anel de ilhas: centro [-0.32, 0.0, -0.19], raio max centro-a-centro XZ 21.11, Y das ilhas de -3.45 (base pendurada) a 2.3 (topo, decoracoes); profundidade na camera (z camera-espaco) dos renderers de 51.5 a 85.1.
- **Cenario background (raiz)**: posicao [17.9, -24.6, 23.59], escala mundo [5.9, 5.9, 4.83], bounds [134.9, 24.0, 134.9] (min [-37.6, -34.8, -37.5], max [97.2, -10.9, 97.4]), topo Y -10.87 = 7.41 m abaixo da base das ilhas, profundidade camera-z do centro 118.3 (anel 51.5 a 85.1), distancia XZ ao centro do anel 42.6.
- **Rio de fundo**: posicao [17.9, -24.6, 23.59], escala mundo [5.9, 5.9, 4.83], bounds [49.5, 8.5, 134.4] (min [-21.1, -26.1, -37.0], max [28.4, -17.6, 97.4]), topo Y -17.63 = 14.18 m abaixo da base das ilhas, profundidade camera-z do centro 102.0 (anel 51.5 a 85.1), distancia XZ ao centro do anel 30.6.
- `CenarioContinuo.margem` = 1.5; `agua` nao atribuida (o rio tem seu proprio `FundoContinuo`).

Filhos do cenario por tipo (61 filhos): N, altura de mundo tipica, Y max, camera-z min/max (comparar com anel 51.5..85.1), quantos tem centro de tela no viewport 1920x1080:

| Tipo | N | altura bounds med | yMax min/max | cam-z min/max | mais perto da camera que o anel | no viewport |
|---|---|---|---|---|---|---|
| Arvore | 53 | 4.44 | -21.2 / -12.5 | 54.7 / 159.8 | 0 | 22 |
| Pedras | 1 | 13.99 | -10.9 / -10.9 | 112.4 / 112.4 | 0 | 1 |
| Plane.001 | 1 | 19.76 | -15.1 / -15.1 | 119.5 / 119.5 | 0 | 1 |
| Torre | 2 | 6.92 | -16.4 / -14.6 | 91.8 / 132.8 | 0 | 1 |
| Torre2 | 2 | 6.46 | -17.4 / -14.1 | 66.2 / 118.9 | 0 | 1 |
| banderas | 1 | 6.65 | -15.8 / -15.8 | 118.6 / 118.6 | 0 | 1 |
| moinho | 1 | 5.9 | -18.3 / -18.3 | 129.9 / 129.9 | 0 | 1 |

Nenhum filho do cenario tem yMax acima de -3.45 (base das ilhas)?  sim (maior yMax do cenario: -10.87).

**Rio** (seeds): superficie Y e vertice mais perto de um centro de ilha (distancia XZ):
- seed 12345: superficie Y -17.7 (base das ilhas -3.45 => rio 14.25 m abaixo), vertice mais perto a 0.14 m em XZ do centro da ilha `Caminho (0, -4)` (raio visual da ilha 2.1), 1677 vertices; ilhas dentro do AABB XZ do rio: 24.
- seed 7: superficie Y -18.4 (base das ilhas -3.45 => rio 14.95 m abaixo), vertice mais perto a 0.2 m em XZ do centro da ilha `Caminho (1, -4)` (raio visual da ilha 2.1), 1677 vertices; ilhas dentro do AABB XZ do rio: 30.
- seed 2024: superficie Y -17.35 (base das ilhas -3.45 => rio 13.9 m abaixo), vertice mais perto a 0.2 m em XZ do centro da ilha `Caminho (1, -4)` (raio visual da ilha 2.1), 1677 vertices; ilhas dentro do AABB XZ do rio: 23.

**Nuvens** (CamadaDeNuvens, 18): Y de -7.93 a -5.94 (base das ilhas -3.45; folga 2.49 m abaixo das ilhas), camera-z 61.4 a 110.9; 7 atras do anel em profundidade, 0 na frente.

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

Seed 12345, 12h, Main Camera desenhando em RenderTexture 1920x1080 (`UnityStats.screenRes` reporta 472x266, o tamanho da Game view); mediana de 12 amostras (0.5 s) de `UnityEngine.UnityStats`: batches 1244, setPassCalls 71, drawCalls 1248, triangulos 1,116,934, vertices 1,401,951, shadowCasters 58.
Tempo medio de frame do Editor: 4.06 ms (246.3 fps) sobre 1495 frames em 6.07 s (frames contados por `Time.frameCount`, inclui overhead do Editor, timeScale 0; **nao** e desempenho de build).
Incerteza: `UnityStats` reflete o ultimo frame renderizado pelo Editor; como a Game view e a RenderTexture podem ser renderizadas no mesmo frame, os totais podem somar mais de uma passada de camera.

## 8. Limitacoes / nao medido

- Capturas por `Camera.Render()`: UI Screen Space Overlay (HUD) e IMGUI nao entram; validar o anel sob a mao de cartas com o HUD real em 1920x1080 e 2520x1080 e manual (pendente).
- Inimigos nao existem com `timeScale=0` logo apos gerar o mapa; foram spawnados temporariamente (Morcego e Lobo) so para medir. O Lobo usa `Lobo (1).fbx` (SkinnedMeshRenderer apenas): desde o Pass A `VisualDeIlha` considera `SkinnedMeshRenderer` (bounds de `sharedMesh`), mas os px abaixo usam os bounds de renderer (`SkinnedMeshRenderer.bounds`, que seguem a caixa local do asset e podem diferir um pouco da malha). Valores lidos do Editor, nao interpretados.
- Modelos `ArvoreGrande`/`Morcego` novos (Mateus) nao sao usados (aguardam .meta do dono); a 'ArvoreGrande' do perfil e o prefab `Arvore` em escala 1.8 a 2.4.
- Medidas de px usam `Camera.WorldToScreenPoint` com a camera em RenderTexture 1920x1080 e aspecto forcado; bounds de renderers desabilitados (hex mesh) nao entram nos px.
- Secao 2 (modelos brutos), secao 1 (hierarquia) e as constantes de iluminacao vem de `docs/art/audit/` (medidas em Edit Mode, nao alteradas pelo Pass A); a secao 6 e remedida.

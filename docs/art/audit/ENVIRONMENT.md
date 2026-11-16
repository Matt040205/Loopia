# Auditoria de composicao do ambiente (somente leitura)

Escopo: codigo em `Assets/Scripts/Hex/` e `Assets/testes shaders/`, dados em `Assets/Dados/`, cena `Assets/Scenes/HexPrototipo.unity` (YAML) e `Assets/Cenario background.prefab`. Nada foi aberto no Editor, nada foi medido em runtime. Onde falta dado do Editor esta escrito "nao verificado". Numeros de linha de cena apontam para a linha `m_EditorClassIdentifier` do componente (ou para o inicio do documento YAML, quando e GameObject/Transform).

Convencao de unidades: 1 unidade = 1 metro de mundo. `HexWorld.tamanhoDoHexagono = 3`, `folgaEntreHexagonos = 0.3` (cena:3752+), logo o raio visual de cada ilha e `3 * (1 - 0.3) = 2.1` e o diametro de referencia do modelo da ilha e `2.1 * 2 = 4.2` (`HexWorld.cs:125-126`). A distancia centro-a-centro entre ilhas vizinhas e `sqrt(3) * 3 = 5.196`.

---

## 1. Sistemas de fundo/ambiente

### 1.1 Visao geral (cena `HexPrototipo`)

Raizes relevantes (extraidas do YAML): `Main Camera` (HexIsoCamera), `Mundo` (HexWorld, HexLoopGenerator, NavegacaoDoMundo, DecoracaoDeIlha), `Lucca`, `Inimigos`, `Cartas`, `Controles`, `Gerador de Moedas`, `Diretor do Loop`, `DayNightController`, `Sun`, `Global Volume`, `Camada de nuvens`, `Rio de fundo`, instancia do prefab `Cenario background` (cena:990), `HUD`, `EventSystem`, `DadosDoJogador`, `LightProbeGroup`, `SmoothSmoke`. Itens desativados no YAML (`m_IsActive: 0`): `Plane`, `Cube`, `IlhaFlutuante` (instancia do FBX em y=-3.1) e as 6 `QuadCloud*` com `CloudController` manual. Sao sobras, nao participam.

### 1.2 Camera e enquadramento (determina a escala aparente de tudo)

- `HexIsoCamera` (cena:3490): ortografica, inclinacao 30, giro 45, `enquadrarAutomaticamente = 1`, `margem = 1.3`, `distancia = 40` (`HexIsoCamera.cs:66-101`).
- O enquadramento mede `Renderer.bounds` de TODOS os renderers filhos das ilhas (`HexIsoCamera.cs:124-137`), ou seja, inclui decoracao e modelo da ilha; soma uma casa de folga (`tamanhoDoHexagono`) e multiplica por `margem` (`:140-146`). Roda a cada 0.25 s (`:46`).
- Consequencia: `orthographicSize` cresce com o raio do anel. `HexLoopGenerator` sorteia raio entre 4 e 6 (`raioMinimo/raioMaximo`, cena:3735-3736, `HexLoopGenerator.cs:95`). Estimativa calculada (nao medida): raio 4 produz extensao de ~21 unidades do centro ate a ponta; com tilt 30 e margem 1.3 o `orthographicSize` fica na casa de ~19-28 unidades (raio 4 a 6). A altura visivel e `2 * orthographicSize`, ou seja ~38-56 unidades em 1080 px, algo como 19-28 px por unidade. Lucca (1.0 de altura) ocupa algo como 20-25 px de altura na tela; uma decoracao de 0.65 ocupa ~12-18 px. **Estimativa, confirmar no Editor.**
- Camera real fica a `max(distancia, meiaAltura*3)` do centro (`:85`), ou seja ~57-84 unidades (o valor 40/20/-24 salvo na cena e sobrescrito em runtime).

### 1.3 `CenarioContinuo` + prefab `Cenario background` (a "maquete" de fundo)

Arquivos: `Assets/Scripts/Hex/CenarioContinuo.cs` (GUID `f9726c02...`), instancia na cena em `HexPrototipo.unity:1456` (componente adicionado na instancia do prefab).

Valores serializados (cena:1456-1466): `camera3d` = Main Camera, `agua = null`, `margem = 1.5`, `_proporcao = (1, 1, 0.819)`, `_teto = -10.866`, `_referenciaSalva = 1`. Transform da raiz do prefab na cena: posicao `(14.77, -14.95, 16.42)`, escala `(1.7526, 1.7526, 1.4352)` (cena:1432-1437, YAML do override).

Comportamento (`CenarioContinuo.cs:18-57`):
- A cada `LateUpdate` projeta os 4 cantos da viewport no plano do cenario, calcula `fator = max(largura_visivel/(boundsX*prop.x), profund_visivel/(boundsZ*prop.z)) * margem` e seta `localScale = _proporcao * fator` (`:36-40`). Ou seja: **a maquete e reescalada para cobrir 1.5x a area visivel da camera**, independente de tamanho do mapa, resolucao ou aspecto.
- Mantem o topo da maquete fixo em `y = _teto = -10.87` (`:42`) e recentraliza em XZ para o centro da tela (`:44-49`), ou seja, segue a CAMERA/enquadramento (que por sua vez segue o centro das ilhas), nao o centro do mundo em si.
- `agua = null`: o ramo que ajustaria o plano de agua (`:50-56`) nunca roda.
- Adapta ao raio do anel? Indiretamente: como o `orthographicSize` muda com o anel (1.2), a escala da maquete muda junto. **Os objetos da maquete (arvores, moinho, torres) mudam de tamanho no mundo quando o anel cresce/encolhe.** O mundo jogavel nao muda de escala; a maquete sim. Isso e a raiz da desconexao de escala.

Conteudo do prefab (`Assets/Cenario background.prefab`, 7067 linhas, 20 GameObjects + 65 instancias de prefab aninhadas):
- 58 instancias de `Modelos/Cenário/Arvore (1).fbx` (GUID `af4ef7af...`, 986 referencias ao GUID no prefab), 2x `Torre.obj`, 2x `Torre2.obj`, 1x `banderas.fbx`, 1x `moinho.fbx`, 1x `Plano (1).fbx` (terreno) (contagem de `m_SourcePrefab` por GUID no prefab).
- Grupo `Pedras` (Transform em `(0.42,-0.69,1.33)`, escala `(0.604,0.604,0.738)`, prefab:1722-1732) com 13 `Plane.001` com escalas locais 2.3 a 18.4 (`prefab:955-1650`), mesh `Pedra.fbx`.
- `Lago` (mesh builtin Plane 10209, prefab:567-595), `Circle (1)`, `Circle (2)`, `Plane (1)` com escala 100 filhas do fbx de terreno (prefab:30, 400, 1144).
- `Post Processing` (prefab:1398-1429): um **segundo Volume global** (`m_IsGlobal: 1`, `weight 1`, `priority 0`) apontando para `Assets/Scenes/Testes/Post Processing Profile.asset`. **[Revisor, verificado no Editor em 2026-11-14: a instancia do prefab na cena NAO contem esse Volume — `FindObjectsByType<Volume>` (incluindo inativos) retorna so o `Global Volume` da cena. O Volume existe apenas no arquivo do prefab: risco latente se o prefab for reinstanciado.]**
- Materiais (shader `933532a4...`, que pelo GUID e o URP/Lit, nao verificado no Editor): `Terreno.mat`, `Material Pedra.mat`, `MaterialMoinho.mat`, `Torre_Material.mat`, etc.
- O root do prefab tem escala nativa `(1.654, 1.654, 1.355)` e posicao `(446.96, -369.74, 450.24)` (prefab:209-210). Na cena foi sobrescrito.

Relacao com o mundo jogavel: **nenhuma programatica**. Nao ha referencia a `HexWorld`; so a camera. Fica ~11 unidades abaixo do plano das ilhas (`_teto=-10.87` vs. ilhas em y=0). Em projecao ortografica 30/45 a maquete aparece "abaixo" do anel, deslocada na tela, reenquadrada artificialmente por `CenarioContinuo.cs:44-49`. Nao ha sombra, contato, silhueta de costa, nem cor/paleta compartilhada com as ilhas.

### 1.4 `FundoContinuo`

`FundoContinuo.cs`: escala um mesh plano ate cobrir a projecao da camera * `margem 1.2`. Na cena ele esta no GO `Rio de fundo` (cena:383) **com `m_Enabled: 0`** (cena:~376), logo inativo. E codigo morto na cena atual. (`CenarioContinuo.agua` tambem e nulo.)

### 1.5 `RioDeFundo`

`RioDeFundo.cs` + GO `Rio de fundo` (cena:257, componente cena:397). Valores: `cenario` = Transform da instancia do prefab (cena:689246115), `alturaDasOndas = 0.07`, `profundidadeDaCor = 0.6`.

- Copia posicao, rotacao e `lossyScale` da maquete a cada `LateUpdate` (`RioDeFundo.cs:143-144`). O rio e uma malha (`Assets/Modelos/HexPrototype/RioFundo/RioFundo.asset`, 1677 vertices, AABB centro `(-2.41, 0.46, 1.37)`, extensao `(4.19, 0.71, 13.92)` em espaco local do cenario; cena:300-350) com material `Water/Shader Graphs_WaterLowPoly 1.mat` (shader graph `WaterLowPoly`). Dimensao em mundo ~ `8.4*1.75 x 27.8*1.44` = ~14.7 x 40 unidades, e muda com a escala da maquete.
- O leito: a cena sobrescreve o mesh de um objeto do prefab com `CenarioLeitoRio.asset` (cena:1346-1348); extensao dessa malha e ~0.001 em unidades locais (AABB `m_Extent (0.001,0.001,0.0002)`), ou seja, so faz sentido sob a escala 100 do FBX de origem (nao verificado qual GO e alvo).
- Ondas em unidades de mundo (`alturaDasOndas / escalaY`) e cor por profundidade: independentes da camera. Mas o rio nao tem relacao com a geografia do anel: e um recorte fixo no espaco local da maquete, sem margens/margens-de-pedra no jogo, e transita com a escala da maquete.

### 1.6 `CamadaDeNuvens` (+ `CloudController`)

`CamadaDeNuvens.cs` (cena:2370) e `testes shaders/CloudController.cs`.
- Valores: `quantidade 18`, `semente 751`, `tamanho (0.13, 0.21)` como **fracao da altura visivel da camera**, `distanciaDasIlhas 0.85`, `vento (1,0.18)`, `velocidade 0.018`, `opacidade 0.8`; material `SoftCloud.mat`; quad builtin.
- Posicao: distribuidas em grade UV sobre a projecao dos 4 cantos da viewport num plano horizontal a `y = menorY_das_ilhas - 0.85 - metadeAltura - camada(0..1.1)` (`CamadaDeNuvens.cs:78-89`). `menorY_das_ilhas` e medido a cada 0.5 s em todos os `MeshRenderer` das ilhas (`:54-61`). Seguem a CAMERA (cobertura de tela), nao o centro do mundo; tamanho proporcional a `orthographicSize` (`:64-72`), portanto crescem com o anel (mesmo problema de escala relativa do 1.3).
- Ficam logo abaixo da base das ilhas, entre o anel e a maquete. Material/shader customizado (`SoftCloud`), billboard com densidade volumetrica sincronizada ao `DayNightController`.

### 1.7 Luz, neblina e pos-processamento

- `DayNightController` (cena:1898; `testes shaders/Daynightcontroller.cs`): `timeOfDay 14.8`, `dayLengthInMinutes 2`, `autoAdvanceTime 1` (o dia passa em 2 minutos reais), `syncFog 1`, `syncAmbientLight 1`, `fogDensity 0.01` (cena:2036-2038). Escreve todo frame `RenderSettings.fog = true`, `fogMode = Exponential`, `fogColor = skyColor`, ambiente Trilight (`:122-137`). Tambem sobrescreve `camera.backgroundColor` com a cor do ceu (camera em Solid Color, `m_ClearFlags: 2`, cena:3437).
- Fog na cena: `m_FogMode: 2` (Exponential), `m_FogDensity 0.01` (cena:17-20). Distancia de camera 57-84 (1.2) implica, se a formula de fog do URP for linear em profundidade, `exp(-0.01*57) = 0.57` a `exp(-0.01*84) = 0.43` de visibilidade, isto e ~43-57% da cor da superficie substituida pela cor do ceu, ja nas ilhas. A maquete, mais distante, recebe mais. **Em camera ortografica o calculo de fog do URP pode divergir** (usa clip z); precisa conferir no Editor desligando `syncFog`.
- `Sun` (cena:401+): direcional, cor (0.97, 0.82, 0.79), intensidade 1.085 (sobrescrita pelo DayNight a cada frame com curva 0.1..1.4), sombras tipo 2 (soft). Cor e intensidade variam ao longo do ciclo de 2 min.
- Volumes globais: `Global Volume` da cena (cena:3062, perfil `Assets/Settings/VolumeCena.asset`: Bloom, Vignette, ShadowsMidtonesHighlights, ColorAdjustments com `contrast 12`, `saturation 20`, `postExposure 0.1`) E o `Post Processing` dentro do prefab de fundo (perfil `Assets/Scenes/Testes/Post Processing Profile.asset`: **DepthOfField modo 2 (Bokeh)**, Vignette `0.338`, LensDistortion `-0.09`, Bloom `threshold 1.1 intensity 1 scatter 0.09`). Ambos `priority 0`, `weight 1` **no arquivo**; na cena so o primeiro existe (ver nota do revisor acima), entao nao ha mistura hoje. O DoF Bokeh com parametros nao sobrescritos (so `mode` esta override) e uma fonte plausivel de desfoque em tudo o que esta longe do foco padrao (nao verificado, DoF em camera ortografica).

---

## 2. Ilhas e decoracao

### 2.1 Criacao da ilha (`HexWorld.CriarIlha`, `HexWorld.cs:105-138`)
- GO "Ilha" com `MeshFilter/MeshRenderer/MeshCollider` usando uma malha hexagonal gerada (`HexMesh.Create`, espessura `0.6`; o topo em Y=0 e a superficie caminhavel/NavMesh, `HexMesh.cs`).
- Se `modeloDaIlha != null` (na cena: `IlhaFlutuante.prefab`, GUID `268d888a...`, cena:3752+), instancia o modelo via `VisualDeIlha.Criar(modelo, ilha, 4.2, topo: true)` e **desliga o MeshRenderer** da malha gerada (`:123-128`). O collider/NavMesh continua sendo a malha de espessura 0.6, nao a arte.
- `VisualDeIlha.Criar` (`VisualDeIlha.cs:11-31`): instancia o prefab, mede bounds por `MeshFilter` (`LimitesLocais`, `:33-51`), escala de modo que a MAIOR dimensao (em `topo`, o maior de X e Z; fora de `topo`, o maior entre X, Y e Z) vire `tamanho`, alinha o centro XZ e coloca o `min.y` (ou `max.y` em modo topo) em 0, adiciona `NavMeshModifier.ignoreFromBuild` e desliga colliders.
  - Limitacao: so enxerga `MeshFilter`. **Modelos com `SkinnedMeshRenderer` resultam em bounds vazios e escala `tamanho/0.001`** (`:19-21`, risco ja anotado em `docs/handoff/ESTADO_ATUAL.md`).
  - Ilha em modo `topo` fixa a largura, nao a altura: a espessura do modelo (altura visual da "ilha flutuante") e a do FBX, escalada junto. A altura visual real do modelo nao foi medida (nao verificado; depende do FBX `IlhaFlutuante.fbx`).
- A cor da ilha: `HexTile.Refresh` aplica `MaterialPropertyBlock` no renderer da malha e, nos renderers filhos (`_arte`, capturados em `Initialize` ANTES da decoracao), usa branco quando nao ha carta/hover (mantem a textura) ou a cor da carta/hover (`HexTile.cs` linhas ~114-123, 40-125 no recorte). Quando uma carta e colocada `ilha.DefinirCor(def.cor)` (`MapaDePlataformas.cs:220`) tinge a ilha inteira (multiplicando a textura). Decoracao e modelos de carta nascem depois e **nao** sao tingidos.

### 2.2 `DecoracaoDeIlha` (`DecoracaoDeIlha.cs`, cena:3837)

Valores serializados: `quantidade 4`, `larguraDoCorredor 0.4`, `semente 173`; `modelos[6]` (cena:3837+), resolvidos pelos GUIDs:

| # | GUID | Prefab |
|---|---|---|
| 0 | `7c2a7dfe...` | `Modelos/HexPrototype/Arvore.prefab` |
| 1 | `cd2744e0...` | `Modelos/HexPrototype/Pedra.prefab` |
| 2 | `4c20ea50...` | `Modelos/HexPrototype/CogumeloMarrom.prefab` |
| 3 | `d3ff6d47...` | `Modelos/HexPrototype/CogumeloVermelho.prefab` |
| 4 | `e8917b96...` | `Modelos/HexPrototype/CepoDeMadeira.prefab` |
| 5 | `524c2fcf...` | `Modelos/HexPrototype/TroncoDeMadeira.prefab` |

Como decide o tamanho: `tamanho = Lerp(0.28, 0.65, random)` (`DecoracaoDeIlha.cs:45`), **igual para todos os modelos, sem noção de categoria**; e passado a `VisualDeIlha.Criar(..., topo: false)`, ou seja a MAIOR dimensao de qualquer modelo (altura incluida) vira 0.28-0.65. Resultado: uma arvore (alta) e um cogumelo (baixo) recebem a mesma faixa de tamanho maximo; uma arvore de 0.65 e menor que o Lucca (1.0 de altura) e vale ~15% do diametro da ilha (4.2).

Posicao e reserva de corredores/centro (`:50-58, 72-84`):
- Angulos: so `k*60 +- 6` graus a partir de `+Z`, o que corresponde aos 6 VERTICES do hexagono pointy-top (`HexLayout.Corner = 60*i - 30`, `HexLayout.cs:47-51`), nunca as arestas, onde ficam os acessos aos vizinhos.
- Distancia ao centro: `raio * Lerp(0.72, 0.82)` = 1.51-1.72 (raio visual 2.1).
- `PosicaoLivre` rejeita se `|p| - pegada < 0.48*raio` (reserva o centro: ~1.0 de raio livre), se passa de 0.99 do apotema para qualquer vizinho, ou se esta a menos de `larguraDoCorredor + pegada` de qualquer segmento centro-vizinho (`:72-84`). Com os numeros acima, so objetos com pegada ~<0.4 passam ao lado do corredor a 30 graus; ate 60 tentativas por objeto (`:51-66`); falhou, o GO e destruido silenciosamente.
- Teto pratico: 6 slots (6 vertices) por ilha, com `quantidade` configuravel 1-8 (`[Range(1,8)]`); 4 por padrao. `semente` + hash da coordenada (determinístico por ilha).
- Cada `CriarIlha` chama `Decorar` (inclusive ilhas construidas por carta), `HexWorld.cs:134`.
- Fica fora de `ilha.Altura` (HexTile) no sentido de que ilha alta (Relevo) move a ilha inteira (filhos incluidos).

De onde pode vir vegetacao baixa hoje: so `CogumeloMarrom` e `CogumeloVermelho` (2 de 6 modelos, ~33% das posicoes), `CepoDeMadeira`/`TroncoDeMadeira` (33%), `Pedra` e `Arvore` (1/6 cada). **Nao ha capim, arbusto, flor nem moita.** Nao ha categoria nem peso por tipo; sorteio uniforme `random.Next(modelos.Length)` (`:43`).

### 2.3 `HexMesh`
Gera hexagono pointy-top (7 vertices topo + paredes) com raio `tamanhoDoHexagono*(1-folga)` = 2.1 e espessura 0.6; superficie do NavMesh e dos raios do mouse (`HexWorld.TryCasaNoRaio` usa o MeshCollider). A arte do modelo (4.2 de largura maxima) e independente: nao e ela que da forma ao NavMesh.

---

## 3. Estruturas/plataformas (cartas)

- `Plataforma.Montar` (`Plataforma.cs:35-48`): `VisualDeIlha.Criar(def.modelo, transform, def.tamanhoDoModelo)` (modo nao-topo, maior dimensao = `tamanhoDoModelo`), posicionado em `y = def.alturaDoModelo`; `ItemGiratorio` (rotacao 120 graus/s + balanco 0.08) se `girarModelo`. O GO `Plataforma` nasce sob `MapaDePlataformas._raiz`, **nao** sob a ilha (`MapaDePlataformas.cs:222-227`), portanto nao entra em `HexTile._arte` (sem tint) nem em `ilha.GetComponentsInChildren<Renderer>()` (nao conta no zoom).
- Valores padrao em `PlataformaDef`: `tamanhoDoModelo 0.8`, `alturaDoModelo 0.25`, `girarModelo false` (`PlataformaDef.cs:43-47`). Ou seja, mesmo categoria "estrutura" usa o mesmo mecanismo da decoracao: bounding box maxima -> 0.8-0.95.
- Cartas em `Assets/Dados/Plataformas/*.asset` (11):

| Carta | Tem `modelo`? | tamanho / altura | gira | Cor da ilha (`cor`) |
|---|---|---|---|---|
| Basica | nao (campo ausente no YAML) | padrao 0.8/0.25 | nao | (0.62,0.78,0.42) |
| Torre | nao | padrao | nao | (0.58,0.60,0.68) |
| Cabana | nao | padrao | nao | (0.72,0.50,0.32) |
| Floresta | nao | padrao | nao | (0.16,0.42,0.22) |
| Campo Pacifico | nao | padrao | nao | (0.55,0.85,0.50) |
| Area de Treino | nao | padrao | nao | (0.85,0.62,0.45) |
| Ilha alta | `modelo: {fileID: 0}` (vazio) | 0.8/0.25 | nao | (0.72,0.50,0.95) |
| Coracao | `Coracao.prefab` | 0.9/0.35 | sim | (0.90,0.32,0.40) |
| Flechas | `Flecha.prefab` | 0.9/0.35 | sim | (0.95,0.82,0.35) |
| Moedas | `Moeda.prefab` | 0.9/0.35 | sim | (0.98,0.72,0.20) |
| Mola | `Mola.prefab` | 0.95/0.03 | nao | (0.30,0.80,0.95) |

  Das 11 cartas, **7 nao tem modelo** (Basica, Torre, Cabana, Floresta, Campo Pacifico, Area de Treino, Ilha alta): so a cor da ilha as diferencia. Existe `BarracaArmada.prefab` (nao usado por nenhuma carta) e `Torre.obj`/`Torre2.obj` (usados so na maquete) como candidatos. Cartas com modelo sao "itens" flutuantes e giratorios de ~0.9 (itens/powerups), nao construcoes.
- Moedas no chao (`GeradorDeMoedas.cs:149-152`, cena:2082+): `modeloDaMoeda = Moeda.prefab` (GUID `accdf55a...`), tamanho fixo `0.5`, `alturaDaMoeda 0.5`, `chancePorIlha 0.2`. Fallback: cilindro `0.32 x 0.03`.
- Coloracao: `ilha.DefinirCor(def.cor)` tinge a malha da ilha inteira (`MapaDePlataformas.cs:220`; multiplicativo sobre a textura das filhas via `HexTile.Refresh`). Floresta, por exemplo, escurece a ilha para verde (0.16,0.42,0.22) e gera lobo, sem modelo proprio. Mola e Relevo (`PlataformaRelevoDef.altura 2.2`) alteram `ilha.Altura` (`MapaDePlataformas.cs:311-314`).

---

## 4. Personagem e inimigos (estado atual + contrato de escala)

### 4.1 Estado atual
- **Lucca** (cena:122): NavMeshAgent com `radius 0.2`, `height 1`, `baseOffset 0`, `speed 3.5` (cena:235-256). A "arte" e provisoria: filho `Corpo` com mesh builtin Capsule (id 10208) em `localScale 0.5`, `y 0.5` (cena:2109) e filho `Frente` com Cube (10202) `scale (0.16,0.16,0.28)`, `y 0.64, z 0.3` (cena:671) para indicar a frente. **Altura total ~1.0, largura 0.5.** `HexPlayer` nao instancia visual; a arte e filha na cena. `LuccaCombate.alturaDoTiro = 0.6` (`LuccaCombate.cs:35`, origem do projetil em `transform.position + up*0.6`), `Projetil` escala `(0.08,0.08,0.34)` (`Projetil.cs:29`).
- Ha `Modelos/Lucca.prefab` (usa `PersonagemJam.fbx`, com Animator), mas e referenciado apenas por `Scenes/Testes.unity`, nao pela `HexPrototipo`.
- **Inimigos** (`Inimigo.MontarVisual`, `Inimigo.cs:271-307`): com `def.prefab` usa `VisualDeIlha.Criar(def.prefab, transform, def.escala)`; sem prefab gera esfera (se `voa`) ou cubo (chao) de `Vector3.one * escala`, material URP/Lit com `def.cor`.
  - `Morcego.asset`: `prefab: {fileID: 0}` (nulo), `escala 0.38`, `voa 1`, `alturaDeVoo 1.1` (esfera roxa de 0.38). Existe `MateusModelos/Morcegp.fbx` (nome com typo, sem skin pelo grep binario, textura `Morcego.png`) **nao referenciado**.
  - `Lobo.asset`: `prefab = Lobo (1).fbx` (GUID `02a84f49...`), `escala 1.1` (maior dimensao do modelo = 1.1, ou seja maior que o Lucca). O FBX contem marcadores de skin/deformer (33 ocorrencias binarias): se o prefab tem `SkinnedMeshRenderer`, `VisualDeIlha.LimitesLocais` nao o ve (2.1). **Nao verificado no Editor** se o Lobo aparece corretamente.
  - Agente de lobo: `radius 0.18` (`Inimigo.cs`, trecho depois do `AddComponent<NavMeshAgent>`).
- `InimigoSpawner` (cena:3989): morcego max 6 / 5 s, lobo max 2, `distanciaMinimaDoPlayer 3`.

### 4.2 Contrato de escala para qualquer modelo novo de personagem/inimigo (sem integrar)
Para entrar sem surpresas no pipeline atual:
1. Pivô nos pes (centro da base em Y=0), eixo +Z = frente (HexPlayer gira o transform para a direcao de movimento; `LuccaCombate` usa `transform.forward`).
2. Altura total ~1.0 (alinhada com `NavMeshAgent.height 1`, `radius 0.2`, `alturaDoTiro 0.6`, origem das flechas e `AlturaDoPeito` dos inimigos). Largura (diametro) ~0.4-0.5.
3. Malha estatica OU garantir que `VisualDeIlha.LimitesLocais` consiga medi-la. Hoje so lê `MeshFilter`: um `SkinnedMeshRenderer` sem `MeshFilter` produz escala `tamanho/0.001`. Isso vale para Lucca (se virar skinned), Lobo e Morcego.
4. `VisualDeIlha` usa a MAIOR dimensao (X, Y ou Z): modelo com braco/asa/cauda esticada vai encolher o corpo. Quem quiser "altura = 1" precisa de bounds sem apendices ou de normalizacao por altura (hoje inexistente).
5. Sem colliders ativos e sem `NavMeshModifier` proprio (o helper ja desliga/ignora); um unico renderer principal facilita tingir por `MaterialPropertyBlock`.
6. Referencias de escala do mundo: ilha 4.2 de diametro, hex centro-centro 5.2, decoracao hoje 0.28-0.65, estruturas 0.8-0.95, moeda 0.5, Lobo 1.1, Morcego 0.38 (esfera), flecha 0.34 de comprimento.

---

## 5. Inventario de assets aproveitaveis

Formato / pasta / uso na `HexPrototipo` (conforme GUID encontrado na cena ou prefabs usados por ela). "Testes" = `Scenes/Testes.unity` (legado).

| Asset | Tipo | Pasta | Possivel papel | Usado na HexPrototipo? |
|---|---|---|---|---|
| `Arvore.fbx` + prefab `Arvore.prefab` | FBX + prefab | `Modelos/MateusModelos`, `Modelos/HexPrototype` | arvore (decoracao) | sim, `DecoracaoDeIlha.modelos[0]` |
| `ArvoreGrande.fbx` (+ `ArvoreGrande.png`, `ArvoreClara.png`) | FBX | `Modelos/MateusModelos`, `MateusTexturas` | arvore grande (candidata a "arvore" de verdade) | **nao** (sem prefab nem material; ver abaixo) |
| `Pedra.fbx` + `Pedra.prefab` | FBX + prefab | idem | pedra | sim, `modelos[1]` |
| `CogumeloMarrom`, `CogumeloVermelho` (fbx + prefab) | FBX + prefab | idem | vegetacao baixa (rasteira) | sim, `modelos[2,3]` |
| `CepoDeMadeira`, `TroncoDeMadeira` (fbx + prefab) | FBX + prefab | idem | detalhes de chao | sim, `modelos[4,5]` |
| `BarracaArmada.fbx` + prefab | FBX + prefab | idem | estrutura (barraca; Torre/Cabana) | **nao** |
| `Fogueira.fbx` (+ `Fogueira.png`) | FBX | `MateusModelos`, `MateusTexturas` | acampamento | **nao** (sem prefab) |
| `IlhaFlutuante.fbx` + prefab | FBX + prefab | idem | ilha base | sim, `HexWorld.modeloDaIlha` (cena:3752+); e instancia do FBX inativa na cena |
| `Moeda.fbx`/prefab, `Coracao.prefab`, `Flecha.prefab`, `Mola.prefab` | prefabs | `HexPrototype` | itens de carta | sim via `Dados/Plataformas/*` e moeda |
| `Morcegp.fbx` (+ `Morcego.png`) | FBX (sem skin) | `MateusModelos`, `MateusTexturas` | Morcego | **nao** (`Morcego.asset.prefab = null`) |
| `Lobo (1).fbx` | FBX (com skin) | `Modelos/` | Lobo | sim, `Dados/Lobo.asset` |
| `PersonagemJam.fbx`, `PersonagemJam (1).fbx`, `Lucca.prefab` | FBX + prefab | `Modelos/`, `Modelos/Cenário` | Lucca (legado) | nao (so Testes) |
| `Arvore (1).fbx` | FBX | `Modelos/Cenário` | arvore da maquete | sim, 58x dentro de `Cenario background.prefab` |
| `Pedra.fbx` (Cenario), `Pedra 1.fbx` | FBX | `Modelos/Cenário` | rochas | Pedra 1x via maquete; `Pedra 1.fbx` **nao** usada |
| `Torre.obj`, `Torre2.obj`, `banderas.fbx`, `moinho.fbx`, `Plano (1).fbx` | OBJ/FBX | `Modelos/Cenário` | estruturas de fundo | so dentro da maquete |
| `PAREDE.obj` | OBJ | `Modelos/Cenário` | parede | **nao** |
| `base.fbx` e `Modelos/PreFabs/*` (Bloco, Boss, Cartas, Plataformas, Trampola, Player, Inimigo, Lobo) | FBX/prefabs legados | `Modelos/PreFabs` | legado | **nao** (so Testes, ver AGENTS.md secao 3) |
| `FumacaDeMorte.prefab` | VFX prefab | `HexPrototype` | efeito de morte | referenciado por Lobo/Morcego (efeito), nao via cena direta |

Nao existe nenhum asset de vegetacao rasteira (capim, arbusto, flor) nem de moita.

**`ArvoreGrande.fbx` (alerta de versionamento).** O FBX (189.548 bytes) e `ArvoreGrande.png` estao rastreados no git (commit `0b9513e`), mas os `.meta` de `ArvoreGrande.fbx` (GUID `0a4e9653...`), `Morcegp.fbx`, `ArvoreGrande.png` e `Morcego.png` aparecem como `??` (nao rastreados) em `git status`. Qualquer prefab/cena que referenciar o GUID antes do commit do `.meta` quebra em outra maquina (`AGENTS.md` regra 2 e `ESTADO_ATUAL.md`). Nao existe `Materials/ArvoreGrande.mat` em `MateusTexturas/Materials` (so `Flecha.mat`, `IlhaVoadora.mat`); materiais de arvore existentes sao `HexPrototype/Arvore.mat`. Dimensoes do FBX: nao verificado.

---

## 6. Causas provaveis por sintoma relatado

| Sintoma | Causa no codigo/dados | Parametro | Confianca | Falta confirmar no Editor |
|---|---|---|---|---|
| Decoracoes extremamente pequenas | `tamanho = Lerp(0.28, 0.65)` aplicado como MAIOR dimensao a todos os prefabs (`DecoracaoDeIlha.cs:45-46`, `VisualDeIlha.cs:19-21`). A camera ortografica mostra ~38-56 unidades de altura (1.2), entao 0.28-0.65 vale ~6-18 px. A arvore e menor que o Lucca (1.0) e vale ~15% do diametro da ilha (4.2) | `DecoracaoDeIlha.quantidade`, range 0.28/0.65 (hardcoded), `HexIsoCamera.margem 1.3`, raio do anel 4-6 | alta | medir em pixels; confirmar altura real dos prefabs; confirmar orthoSize em runtime |
| Escala inconsistente entre arvores, torres, personagem, estruturas e ilhas | Nao ha uma escala de mundo unica: 4 contratos independentes (ilha 4.2 em eixo X/Z; decoracao 0.28-0.65 max-dim; estrutura 0.8-0.95 max-dim; Lucca capsula 1.0 hardcoded na cena; Lobo 1.1 max-dim; Morcego 0.38 esfera). Cada um normaliza por MAIOR dimensao do seu modelo, entao a altura relativa depende das proporcoes de cada FBX. Nenhuma categoria (arvore/arbusto/pedra) | `PlataformaDef.tamanhoDoModelo`, `InimigoDef.escala`, `DecoracaoDeIlha` range, `HexWorld.tamanhoDoHexagono` | alta | medir bounds reais de cada prefab/FBX para ver quao desproporcionais estao |
| Fundo visualmente desconectado do mundo jogavel | Maquete e um prefab independente (58 arvores, moinho, torres, rochas, lago) reescalado para cobrir 1.5x a tela (`CenarioContinuo.cs:36-40`), preso a `y=-10.87` e recentralizado pela camera (`:44-49`), sem nenhuma referencia ao `HexWorld`. Escala do fundo muda com o raio do anel (via orthoSize). Materiais/paleta/iluminacao diferentes, fog espesso, sem contato visual (sombras, costa, bruma ate a base das ilhas) | `CenarioContinuo.margem 1.5`, `_teto`, `HexIsoCamera.margem`, transform do prefab | alta (estrutural) / media (paleta e fog) | olhar lado a lado com raio 4 e 6; comparar escala do moinho/arvore de fundo vs. ilha |
| Pouca vegetacao baixa | Pool de decoracao tem 2 cogumelos, 2 madeiras, 1 pedra, 1 arvore; 4 objetos por ilha ate 6 slots; nao ha asset de capim/arbusto; nao ha categoria/peso (`DecoracaoDeIlha.cs:43,50-58`) | `quantidade`, `modelos[]`, posicoes so nos 6 vertices | alta | confirmar contagem real por ilha em Play |
| Rio desconectado da geografia/margens | Rio e uma malha fixa em espaco local da maquete (`RioFundo.asset`, ~14.7 x 40 unidades), copiando transform do cenario (`RioDeFundo.cs:143-144`), em y ~ -15; nenhum vinculo com a forma do anel; leito e margens sao assets da maquete (`CenarioLeitoRio`); `CenarioContinuo.agua = null`; `FundoContinuo` desligado | `RioDeFundo.alturaDasOndas 0.07`, `profundidadeDaCor 0.6`, transform do prefab | media | ver em Play se o rio sai da costa da maquete ou cruza buracos; qual GO usa `CenarioLeitoRio` |
| Neblina e iluminacao prejudicam a leitura | `DayNightController` reescreve fog/ambiente todo frame: `Exponential`, densidade 0.01, `fogColor = skyColor` (`:122-137`); camera 57-84 do alvo implica 43-57% de mistura calculada; ciclo de 2 min muda cor de ceu/sol; Volume da cena com contraste/saturacao (o segundo Volume do prefab nao esta na cena); nuvens e maquete na mesma cor de neblina | `fogDensity 0.01` (cena:2038), `dayLengthInMinutes 2`, `syncFog`, perfis de volume | media (formula do fog em ortho nao verificada; DoF em ortho nao verificado) | desligar `syncFog` e Volume `Post Processing` e comparar; capturar em varias horas |

Observacao: `Lobo (1).fbx` e `Morcego` (primitiva) sao causas adicionais de escala incoerente (tamanho do Lobo > Lucca, Morcego e uma esfera).

---

## 7. Recomendacoes sucintas

Preferencia: composicao estatica/parametrizada, sem sistema procedural grande, sem NavMesh nem rebake, sem pacote novo (`AGENTS.md` regras 4, 6, 7). Nenhuma recomendacao abaixo toca `NavegacaoDoMundo`.

1. Definir uma tabela de escala por categoria (por exemplo ScriptableObject em arquivo proprio, ja prevista em `docs/handoff/ESTADO_ATUAL.md`): arvore, arbusto/baixa, pedra, estrutura, personagem, com altura-alvo em unidades. `DecoracaoDeIlha` passa a sortear dentro da faixa da categoria do prefab, e `VisualDeIlha` ganha modo "por altura" alem de "maior dimensao".
2. Ampliar o pool de `DecoracaoDeIlha.modelos` com modelos baixos novos (capim/arbusto/flores) e usar `ArvoreGrande` como arvore; manter limite por ilha e as reservas de corredor/centro, ja validas.
3. Compor a maquete de fundo com escala FIXA em relacao ao mundo (em vez de rescalar pela camera) e adiciona-la a paleta/bruma das ilhas; avaliar remover ou reduzir `Post Processing` do prefab (DoF) e reduzir a densidade de neblina ou a mistura na camada das ilhas.
4. Rio: reposiciona-lo/escala-lo como asset estatico alinhado com margens da maquete, sem depender de `lossyScale` do cenario.
5. Atribuir modelos as 7 cartas sem arte (Barraca, Torre, Cabana, Floresta etc.) pelo mesmo `PlataformaDef.modelo`, definindo `tamanhoDoModelo` como parte da tabela de categorias.
6. Antes de usar qualquer modelo novo (inclusive personagem), garantir `.meta` commitado e que `VisualDeIlha.LimitesLocais` cubra `SkinnedMeshRenderer`.

Pendencias que exigem o Editor: medir bounds de cada FBX/prefab, medir `orthographicSize` real por raio, comparar render com/sem fog e sem o Volume do prefab, confirmar como o Lobo renderiza com `VisualDeIlha`, identificar qual GO recebe `CenarioLeitoRio` e quanto a maquete se desloca ao mudar de raio.

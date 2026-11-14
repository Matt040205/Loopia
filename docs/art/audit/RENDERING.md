# Auditoria de Renderizacao e Desempenho (analise estatica)

Escopo: `HexPrototipo.unity` (build index 1), Unity 6000.3.10f1, URP 17.3. Nada foi medido nem aberto no Editor: tudo abaixo vem de leitura de YAML, scripts e shaders. O que depende de medicao esta marcado **a medir**, com o como medir na secao 7. Numeros de GPU/CPU nao aparecem em lugar nenhum deste documento porque nao existem medicoes.

Convencao de evidencia: `arquivo:linha` (YAML ou codigo). Valores brutos de enum sao citados como estao no arquivo; a traducao em nome e minha leitura do enum do URP e esta marcada "(verificar no Inspector)" quando nao e inequivoca.

---

## 1. Configuracao efetiva

### 1.1 Qual pipeline roda

| Item | Valor | Evidencia |
|---|---|---|
| Pipeline em Graphics | `PC_RPAsset` | `ProjectSettings/GraphicsSettings.asset:40` |
| Qualidade atual | indice 1 = "PC" (Standalone usa 1) | `QualitySettings.asset:7`, `:129` |
| Qualidade "Mobile" (indice 0) | `Mobile_RPAsset` | `QualitySettings.asset:50` |
| Qualidade "PC" | `PC_RPAsset` | `QualitySettings.asset:104` |
| Cor | Linear (`m_ActiveColorSpace: 1`) | `ProjectSettings.asset:50` |
| VSync (ambos os niveis) | 0 | `QualitySettings.asset:32`, `:86` |

Os campos de sombra/MSAA do `QualitySettings.asset` (`shadowDistance: 40`, `shadowCascades: 2`, `antiAliasing: 0`) sao do pipeline legado; com URP ativo valem os do asset URP (secao 1.2).

### 1.2 URP asset: PC x Mobile

| Parametro | `PC_RPAsset` | `Mobile_RPAsset` | Evidencia |
|---|---|---|---|
| Renderer | `PC_Renderer` | `Mobile_Renderer` | PC_RPAsset.asset:19-20 |
| Rendering path | **Forward+** (`m_RenderingMode: 2` no renderer; 2 = Forward+ no URP 17, verificar) | idem | PC_Renderer.asset:56, Mobile_Renderer.asset:48 |
| Depth priming | 0 (desligado) | 0 | PC_Renderer.asset:57 |
| MSAA | `m_MSAA: 1` = desligado (1x) | 1 | PC_RPAsset.asset:28 |
| Camera AA | `m_Antialiasing: 0` (None) | - | HexPrototipo.unity:3523 |
| Render scale | 1 | 0.8 | PC_RPAsset.asset:29 |
| HDR | ligado (`m_SupportsHDR: 1`); camera `m_HDR: 1` | ligado | PC_RPAsset.asset:26; HexPrototipo.unity:3472 |
| Depth texture | **ligada** | desligada | PC_RPAsset.asset:22 |
| Opaque texture | **ligada** (downsample 2x bilinear) | desligada | PC_RPAsset.asset:23 |
| SRP Batcher | ligado | ligado | `m_UseSRPBatcher: 1` |
| Dynamic batching | desligado | - | `m_SupportsDynamicBatching: 0` |
| Sombra da luz principal | ligada, mapa **2048** | 1024 | PC_RPAsset.asset:46 |
| Distancia de sombra | **50** | 50 | PC_RPAsset.asset:57 |
| Cascatas | **4** (splits 0.123 / 0.293 / 0.536, border 0.108) | **1** | PC_RPAsset.asset:58-61 |
| Soft shadows | suportado, qualidade 3 (High) | nao suportado | PC_RPAsset.asset:66,69 |
| Bias (asset) | depth 0.1 / normal 0.5 | 1 / 1 | PC_RPAsset.asset:63-64 |
| Luzes adicionais | modo 1 (per pixel), limite 4, sombras adicionais suportadas | sombras adicionais off | PC_RPAsset.asset:47-50 |
| Mixed lighting suportado | sim | - | `m_MixedLightingSupported: 1` |
| Light layers | suportado | - | `m_SupportsLightLayers: 1` |
| LOD cross fade | ligado | - | `m_EnableLODCrossFade: 1` |
| GPU Resident Drawer | desligado | - | `m_GPUResidentDrawerMode: 0` |
| Color grading | LDR/HDR `m_ColorGradingMode: 0` (LDR), LUT 32 | - | PC_RPAsset.asset:80-81 |
| Post-processing | ligado na camera (`m_RenderPostProcessing: 1`) | - | HexPrototipo.unity:3522 |
| Volume profile do asset | `SampleSceneProfile` (vazio: nenhum override) | idem | PC_RPAsset.asset:95; SampleSceneProfile.asset |
| Adaptive Performance | `m_UseAdaptivePerformance: 1` | 1 | PC_RPAsset.asset:79 (so tem efeito se o pacote do provider existir) |

Pontos de atencao (todos estaticos):

1. **Ha uma unica luz na cena** (o Sol, ver secao 3). Forward+ paga um passe de culling de luzes por tile que, com 1 luz direcional, nao traz ganho. **A medir** se Forward simples e mais barato aqui. Nao trocar sem medir (secao 8).
2. **Opaque texture ligada sem consumidor encontrado.** Nenhum shader do projeto referencia `_CameraOpaqueTexture`/`SampleSceneColor`/`SceneColor` (`grep` em `Assets`: zero ocorrencias), e a agua (`WaterLowPoly.shadergraph`) usa so `SceneDepth`. O custo e uma copia de cor por frame mais downsample. **A medir** (Frame Debugger: passe "Copy Color").
3. **Depth texture ligada tem consumidores reais:** agua (2 nos `SceneDepth`, `WaterLowPoly.shadergraph` na contagem de nos), `SoftCloud` (`SampleSceneDepth`, `SoftCloud.shader:130`) e SSAO.
4. **Sem anti-aliasing nenhum** (MSAA 1x e camera sem FXAA/SMAA/TAA): formas pequenas e estilizadas tendem a serrilhar, principalmente bordas de props de 0.28 a 0.65 unidades. Visual, **a avaliar em captura**.
5. Soft shadow High + 4 cascatas + 2048 para uma cena ortografica pequena: ver 5.4.

### 1.3 Renderer `PC_Renderer`

Uma renderer feature: **ScreenSpaceAmbientOcclusion**, ativa (`PC_Renderer.asset:60-90`):

| Campo | Valor bruto | Leitura |
|---|---|---|
| `m_Active` | 1 | ligada |
| `AOMethod` | 0 | Interleaved Gradient (verificar) |
| `Source` | 1 | **Depth Normals** (verificar): exige pre-passe de normais, ou seja, geometria opaca desenhada de novo |
| `Intensity` | 0.4 | |
| `Radius` | 0.3 | em unidades de mundo |
| `Falloff` | 100 | |
| `DirectLightingStrength` | 0.25 | |
| `Samples` / `NormalSamples` / `BlurQuality` | 1 / 1 / 0 | media / media / alta (verificar) |
| `Downsample` | 0 | resolucao cheia |
| `AfterOpaque` | 0 | |

`Mobile_Renderer`: sem features, mesmo `m_RenderingMode: 2`.

Obs: `UniversalRenderPipelineGlobalSettings.asset:375-378` aponta o **perfil de volume padrao** global para `Assets/Settings/DefaultVolumeProfile.asset` (nao e o `SampleSceneProfile`); ver secao 2.

### 1.4 Pacote `com.unity.postprocessing` (legado)

`Packages/manifest.json:11` pede `com.unity.postprocessing 3.4.0`. Busca por GUID de `PostProcessLayer` (`948f4100...`) e `PostProcessVolume` (`8b9a305e...`) em `Assets/` e `ProjectSettings/` (`.unity`, `.prefab`, `.asset`): **nenhuma referencia**. O pos-processamento em uso e o do URP (`Volume` + `VolumeCena`). **Candidato a remocao futura** (nao remover agora; qualquer remocao exige PR proprio e checagem de compilacao).

### 1.5 Camera (cena `HexPrototipo`)

| Item | Valor | Evidencia |
|---|---|---|
| Projecao | ortografica, `orthographic size` 5 na cena, **sobrescrito em runtime** por `HexIsoCamera.Enquadrar` | HexPrototipo.unity:3462-3463; `HexIsoCamera.cs:80-96` |
| Rotacao | 30 / 45 / 0 | `HexIsoCamera` `inclinacao: 30`, `giro: 45` |
| Near / far (cena) | 0.1 / 1000 | HexPrototipo.unity:3459-3460 |
| Near / far (runtime) | near 0.1; far = `max(1000, distanciaReal*4)` | `HexIsoCamera.cs:103-104` |
| Distancia ao centro | `max(40, meiaAltura*3)` | `HexIsoCamera.cs:89` |
| Clear | Solid Color (`m_ClearFlags: 2`), cor = cor do ceu do `DayNightController` | HexPrototipo.unity:3437; `Daynightcontroller.cs:116-119` |
| Depth/Opaque texture na camera | opcao 2 = seguir o pipeline | HexPrototipo.unity:3512-3513 |
| Reenquadramento | a cada 0.25 s (itera todos os `Renderer` de todas as ilhas) | `HexIsoCamera.cs:42-46`, `MedirIlhas` |

O Editor serializa near/far da cena, mas o runtime reescreve (near 0.1 e far >= 1000 com camera a 40+ unidades do centro). Ver 5.5 para a consequencia na precisao de profundidade.

---

## 2. Volume e pos-processamento

### 2.1 Volumes da cena

**Nota do revisor:** o arquivo `Assets/Cenario background.prefab` contem um segundo Volume global (`:1431-1435`, DoF Bokeh/Lens Distortion/Vignette), mas a instancia na cena **nao** o tem: no Editor, `FindObjectsByType<Volume>(Include inactive)` retorna 1 (verificado 2026-11-14). Risco latente: reinstanciar o prefab traria o segundo Volume de volta com a mesma prioridade. Volume da cena: GameObject "Global Volume" (`:3073`), `m_IsGlobal: 1`, `priority 0`, `weight 1`, `blendDistance 0`, `sharedProfile` = `Assets/Settings/VolumeCena.asset` (`:3095`). Acampamento.unity nao tem Volume, nem Luz (`grep` `!u!108`: 0), nem `DayNightController`.

### 2.2 `VolumeCena.asset` (overrides ativos)

| Componente | Campo | Valor | Evidencia |
|---|---|---|---|
| Bloom | threshold | **1** | VolumeCena.asset:36-38 |
| Bloom | intensity | **0.6** | :39-41 |
| Bloom | scatter | **0.6** | :42-44 |
| Bloom | demais (tint, clamp 65472, maxIterations 6, downscale, high quality) | sem override (default) | |
| Vignette | intensity | **0.2** | :89-91 |
| Vignette | smoothness | **0.45** | :92-94 |
| Vignette | cor/centro/rounded | sem override (preto, 0.5/0.5, false) | |
| ShadowsMidtonesHighlights | shadows | **(0.84, 0.865, 1.0)** (azulado) | :111-116 |
| ShadowsMidtonesHighlights | highlights | **(1.0, 0.989, 0.843)** (quente) | :117-122 |
| ShadowsMidtonesHighlights | midtones / start/end | sem override (default 0 / 0.3 / 0.55 / 1) | |
| ColorAdjustments | postExposure | **+0.1** | :145-147 |
| ColorAdjustments | contrast | **+12** | :148-150 |
| ColorAdjustments | saturation | **+20** | :157-159 |
| ColorAdjustments | colorFilter / hueShift | sem override | |

Nao ha Tonemapping, DepthOfField, ColorCurves, WhiteBalance, FilmGrain etc. no `VolumeCena`.

### 2.3 Perfil padrao global (`Assets/Settings/DefaultVolumeProfile.asset`)

Esse perfil entra por baixo de qualquer Volume da cena. Ele guarda 24 componentes, incluindo componentes de teste do URP (`TestVolume`, `CopyPasteTestComponent1..3`, `OutlineVolumeComponent`, `OasisFogVolumeComponent`, `VolumeComponentSupportedEverywhere`...). Sao restos do template/pacote, nao mexem no visual. Valores que importam para o visual final, todos sob o `VolumeCena`:

| Componente | Valor no perfil padrao | Efeito |
|---|---|---|
| Tonemapping | `mode: 0` (**None**) (DefaultVolumeProfile.asset ~:212-221) | HDR ligado sem tonemap: valores >1 saturam; Bloom (threshold 1) pega o excesso |
| Bloom | threshold 0.9, intensity 0 (sobrescritos pelo VolumeCena) | |
| Vignette | intensity 0 (sobrescrito, 0.2) | |
| ColorAdjustments, LiftGammaGain, SplitToning, WhiteBalance, FilmGrain, MotionBlur, Panini, ChromaticAberration, LensDistortion, DepthOfField | todos neutros/zerados (a maioria com `intensity: 0`) | sem efeito identificado |

Conclusao: a cadeia de pos efetiva e SSAO (renderer feature) + Bloom + Vignette + SMH + ColorAdjustments, **sem tonemapping**. **A medir** o custo do pos (Frame Debugger: passes Bloom/UberPost).

### 2.4 Sombras-vs-cor

`ShadowsMidtonesHighlights` tinge sombras para azul frio e luzes para creme quente (coerente com "miniatura ilustrada"), mas combinado com `contrast +12` e `saturation +20` aumenta a separacao entre claros e escuros: relevante para legibilidade (secao 5).

---

## 3. Iluminacao e neblina

### 3.1 Luzes na cena

Uma unica luz (`!u!108`):

| Luz | Tipo | Sombra | Intensidade | Cor | Modo | Evidencia |
|---|---|---|---|---|---|---|
| "Sun" | Directional (`m_Type: 1`) | Soft (`m_Shadows.m_Type: 2`), forca 1, usa configuracao do pipeline (`m_UsePipelineSettings: 1`) | 1.085 (serializada; sobrescrita todo frame) | (0.973, 0.822, 0.790) (sobrescrita) | `m_Lightmapping: 1` (Mixed) | HexPrototipo.unity:419-470, 476-505 |

Sem luzes adicionais. `m_Sun` em RenderSettings = nenhum (`:36`; o URP escolhe a direcional mais forte).

### 3.2 Lighting asset e ambiente

- Lighting Settings: `Assets/testes shaders/New Lighting Settings.lighting` (GUID `e2eefbd8...`), baked lightmaps habilitado, realtime GI off, Mixed `m_MixedBakeMode: 2` (Shadowmask, verificar), `m_AO: 0`, lightmap max 1024.
- `LightingData.asset` (`Assets/Scenes/HexPrototipo/`, 17 KB). **Nenhum objeto da cena tem flag Static** (`grep m_StaticEditorFlags: [1-9]`: zero), e o mundo e gerado em runtime: nao ha nada para o bake iluminar. O unico dado util do bake sao as probes.
- `LightProbeGroup` (`:1616-1650`): 8 probes em caixa escalada (~23 x 2.3 x 1.9) posicionada ao redor da origem. O mundo atual e um anel hexagonal gerado em runtime; **a caixa nao acompanha o anel** (resto de cena anterior). Probes sao baratas; o risco e ruido visual (iluminacao por SH inconsistente), **a validar em captura**.
- Ambiente (cena): `m_AmbientMode: 1` (Trilight), sky (0.775, 0.721, 0.709), equator (0.835, 0.751, 0.733), ground (0.388, 0.360, 0.354), intensity 1 (`:22-26`). Todos esses valores sao produto do `DayNightController` (ver 3.4).
- Skybox material: **nenhum** (`m_SkyboxMaterial: {fileID: 0}`). Fundo = Solid Color da camera. `m_DefaultReflectionMode: 0` (skybox) sem skybox: reflexo especular dos Lit vem do default; materiais usam smoothness 0.05 a 0.1 (HexWorld.cs:301, Inimigo.cs:305), efeito pequeno.

### 3.3 Neblina: cena vs runtime

| | Cena (serializado) | `DayNightController` (runtime, todo frame) |
|---|---|---|
| Fog ligado | `m_Fog: 1` | `RenderSettings.fog = true` |
| Modo | `m_FogMode: 2` (Exponential) | `FogMode.Exponential` |
| Densidade | 0.01 | `fogDensity` (cena: 0.01) |
| Cor | (0.775, 0.721, 0.709) | `skyColor` do gradiente da hora |
| Linear start/end | 0 / 300 (nao usados em Exp) | nao toca |

A cor serializada na cena **nao e uma decisao autoral**: e o resultado do controlador em `timeOfDay = 14.8` (interpolando `9FD8E8`->`F2937A`, fator 0.47 entre 12h e 18h, da (0.776, 0.720, ...) como no YAML) gravada no `.unity` porque o componente e `[ExecuteAlways]` e `OnValidate` chama `Apply()`. O mesmo vale para ambiente e `m_BackGroundColor` da camera. Consequencia: abrir/validar o componente no Editor suja a cena com valores de fog/ambient/camera.

### 3.4 `Daynightcontroller.cs` (lido por inteiro)

Arquivo: `Assets/testes shaders/Daynightcontroller.cs` (classe global `DayNightController`, `[ExecuteAlways]`, `Update()` chama `Apply()` todo frame, tambem em `OnValidate`).

Fiacao na cena (`HexPrototipo.unity:1895-2000`): `sunLight` = Sun, `targetCamera` = Main Camera, `timeOfDay: 14.8`, **`dayLengthInMinutes: 2`**, `autoAdvanceTime: 1`, `sunAzimuth: -30`, sunrise 6h, sunset 18h, `syncAmbientLight: 1`, `syncFog: 1`, `fogDensity: 0.01`.

O que escreve por frame:

| Alvo | Como |
|---|---|
| `Light.color`, `Light.intensity` do Sol | gradiente de cor e curva de intensidade por `t = hora/24` |
| Rotacao do Sol | `Euler(anguloX, -30, 0)`; X vai de 0 (6h) a 180 (18h) de dia, de 180 a 360 (18h->6h) de noite (`:95-110`) |
| `Camera.backgroundColor` | gradiente do ceu, se clearFlags = SolidColor (`:115-119`) |
| `RenderSettings.ambientMode = Trilight`, `ambientSkyColor = sky`, `ambientEquatorColor = Lerp(sky, sunColor, 0.3)`, `ambientGroundColor = sky*0.5` | `:122-128` |
| `RenderSettings.fog = true`, `fogMode = Exponential`, `fogDensity`, `fogColor = sky` | `:131-137` |

**Nao toca** Volume, post-processing, sombra, nem materiais. Outros leitores do controlador: `CloudController` (opacidade/tint por `sun.intensity`/`sun.color`) e `DappledLightSync` (opacidade por intensidade). Ambos so leem.

Chaves do gradiente (cena, `HexPrototipo.unity:1907-2000`, identicas a `SetDefaultPalette`):

| Hora (t) | Cor do sol | Intensidade do sol | Cor do ceu = fog = ambient sky | Angulo X do sol |
|---|---|---|---|---|
| 00h (0.00) | `4A5A8C` | 0.10 | `2B3A67` | 270 (abaixo do horizonte) |
| 05h17 (0.22) | (interp. entre 4A5A8C e FFC896) | 0.20 | (interp.) | ~349 (abaixo) |
| 06h (0.25) | `FFC896` | **1.00** | `E8A9C3` | 0 (rasante) |
| 12h (0.50) | `F2F5FF` | **1.40** | `9FD8E8` | 90 (a pino) |
| 18h (0.75) | `FFA98C` | 0.70 | `F2937A` | 180 (rasante, lado oposto) |
| 20h24 (0.85) | (interp.) | 0.20 | (interp.) | ~216 (abaixo) |
| 24h (1.00) | `4A5A8C` | 0.10 | `2B3A67` | 360/0 |

Faixas (derivadas das chaves acima, nao medidas): **amanhecer** 5h17-6h (+ ate o meio da manha, ceu lavanda-rosa `E8A9C3` em 6h); **dia** ~8h-16h (ceu azul pastel); **entardecer/poente** ~16h-20h24 (ceu coral `F2937A` em 18h, intensidade 0.7 caindo para 0.2); **noite** ~20h24-5h17 (sol a 0.10-0.20, ceu `2B3A67`). Entre as chaves a cor e interpolada em Blend; a curva de intensidade tem tangentes zero nas chaves (`tangentMode: 0`, slopes 0), entao o trecho entre chaves e suave em "S", nao linear: por isso a tabela so lista os valores exatos das chaves.

Observacoes que vem direto do codigo/dados:

1. **Salto de intensidade no amanhecer.** Entre `t=0.22` (0.2) e `t=0.25` (1.0) sao 0.72 h de jogo; com `dayLengthInMinutes = 2` (24 h em 120 s) isso e **3.6 s reais** de 0.2 -> 1.0. O mesmo intervalo em que o Sol ainda esta abaixo do horizonte (X de ~349 a 360). Resultado esperado: "estouro" de luz no inicio do dia. **A validar em captura.**
2. **Sol abaixo do horizonte ainda ilumina.** De 18h em diante o angulo X passa de 180, ou seja a direcional aponta para cima e ilumina **por baixo**, com intensidade 0.7 -> 0.2 (18h-20h24) e 0.1-0.2 na noite. Topos de objetos recebem so ambiente (Trilight de `2B3A67`...). Isso pode ser o "luar" desejado, mas e um modelo fisicamente invertido; sombras do Sol tambem vem "de baixo". **A validar visualmente.**
3. **Ciclo de 2 min** = 12 h de jogo por minuto real: noite ~(8.9 h de jogo) ~ 44 s reais, escuro. Para playtest de legibilidade o look muda continuamente.
4. `timeOfDay` serializado = 14.8; ao abrir a cena o look inicial depende desse numero.
5. Uso de `Camera.main` e `FindFirstObjectByType` em `CloudController`/`DappledLightSync` e cacheado depois da primeira busca (so se for null).

### 3.5 Quem mais escreve fog/ambient

`grep` por `RenderSettings` em `Assets` (excluindo `Assets/Tests`): **somente `Daynightcontroller.cs:124-136`**. Nenhum script em `Assets/Scripts/**`, `Assets/VFX e Shaders` ou shaders escreve fog/ambient. Segundo escritor **nao existe em codigo**; o "segundo escritor" efetivo e o **YAML da cena** (valores gravados na hora de salvar, 3.3). Para o ciclo dia/noite, o dono unico ja e `DayNightController` (conforme `AGENTS.md`), mas ele nao pode ser removido sem substituto, e mora em `Assets/testes shaders/` e no namespace global.

Fluxo de fog nos shaders: `DappledLight` e `SoftCloud` leem `RenderSettings` via keywords de fog (o SoftCloud mistura a cor da neblina no volume, `_FogBlend`, e dissipa pela intensidade da neblina, `_FogFade`).

---

## 4. Materiais e shaders

### 4.1 Inventario (cena `HexPrototipo` e codigo de runtime)

SRP Batcher: "sim" = shader URP padrao ou custom com `CBUFFER UnityPerMaterial`; "bloqueado" = renderer usa `MaterialPropertyBlock`, que **tira o renderer do caminho SRP Batcher** (ele cai para draw por renderer) mesmo com shader compativel.

| Uso | Material / criacao | Shader | Transparencia / queue | SRP Batcher | Evidencia |
|---|---|---|---|---|---|
| Base da ilha (hex mesh) | `HexWorld.CriarMaterial("Ilha")` (runtime, 1 compartilhado) | URP/Lit (`Shader.Find`) | opaco | shader sim; **bloqueado** por MPB do `HexTile` | HexWorld.cs:94,292-303; HexTile.cs:150-152 |
| Visual da ilha (prefab `IlhaFlutuante`, na pratica o que aparece: o hex mesh e desligado quando ha modelo) | `Modelos/HexPrototype/IlhaFlutuante.mat` | URP/Lit, 1 textura base, smoothness 0.1, metallic 0 | opaco | shader sim; **bloqueado** (MPB em todo MeshRenderer filho) | HexWorld.cs:121-126; HexTile.cs:154-163; IlhaFlutuante.mat |
| Decoracao (6 prefabs: Arvore, Pedra, CogumeloMarrom, CogumeloVermelho, CepoDeMadeira, TroncoDeMadeira) | `HexPrototype/*.mat` (um por prefab) | URP/Lit | opaco | sim **se** nao entrarem no MPB do `HexTile` (ver nota 1) | HexPrototipo.unity:3838-3845 |
| Lucca | prefab `Lucca.prefab` (material do FBX, ex.: `TexturaLucca.mat`) / placeholder "Corpo" com `vermelhor shader.mat` | URP/Lit | opaco | sim (confirmar o material efetivo) | Materials/TexturaLucca.mat; HexPrototipo.unity:2167 |
| Inimigos com prefab | material do prefab | URP/Lit | opaco | sim | Inimigo.cs:274-277 |
| Inimigos sem prefab (esfera/cubo) | `new Material(Lit)` **por inimigo** | URP/Lit | opaco | sim; **vaza material** | Inimigo.cs:299-306 |
| Flecha (projetil) | `new Material(Lit)` **por flecha** | URP/Lit | opaco | sim; **vaza material** | Projetil.cs:34-40 |
| Moeda procedural | `CriarMaterial` (1 compartilhado) | URP/Lit | opaco | sim | GeradorDeMoedas.cs:167,175-184 |
| Marcador de colocacao | `new Material(URP/Unlit)` + MPB a cada frame de hover | URP/Unlit transparente (Surface=1, SrcAlpha/OneMinusSrcAlpha, ZWrite 0, queue Transparent) | transparente | **bloqueado** (MPB) | CartasUI.cs:153-192, 176-181 |
| Grade de colocacao (linhas) | `CriarMaterialDeLinha` (por linha/cor) | URP/Unlit transparente | transparente | sim | HexPlacementGrid.cs:137-157 |
| Rio de fundo ("Rio de fundo", Plane builtin) | `Shader Graphs_WaterLowPoly 1.mat` | Shader Graph URP Lit **Transparent** (alpha), cast/receive shadows off, ZWrite auto | transparente, queue do ShaderGraph | shader sim (SG e compativel); **bloqueado** por MPB `RioDeFundo` todo `LateUpdate` | WaterLowPoly.shadergraph:1360-1367; RioDeFundo.cs:36-40 |
| Cenario de fundo (`Cenario background.prefab`) | `MaterialMoinho.mat`, `Material Pedra.mat`, moinho.fbx | URP/Lit | opaco | sim | Cenario background.prefab:68-70,159,348,438 |
| Nuvens volumetricas (18 instancias runtime) | `SoftCloud.mat` (1 material compartilhado) | `Custom/SoftCloud` | transparente premultiplicado | declarado compativel; **bloqueado** (MPB por nuvem) | CamadaDeNuvens.cs:112-120; CloudController.cs:96-100 |
| QuadClouds (6 objetos da cena) | `SoftCloud.mat` | `Custom/SoftCloud` | transparente | todos **inativos** (`m_IsActive: 0`) | HexPrototipo.unity:547,2525,2801,3577,4236,2218 |
| Plane "DappledLight" | `Custom_DappledLight.mat` | `Custom/DappledLight` | multiply (`Blend DstColor Zero`) | declarado compativel | **inativo** (`:3866`) |
| Cube de teste | `Materialshader teste.mat` | URP/Lit | opaco | sim | **inativo** (`:779`) |
| SmoothSmoke | VFX Graph `SmoothSmoke.vfx` + `SmoothSMoke.mat` (Lit) | VFX Graph | `blendMode: 3` | n/a (VFX) | **inativo** (`:4116`), capacidade 64 |
| UI (Canvas, `Image`) | sem material custom (`m_Material: 0`) | UI/Default | transparente | n/a | `Image` varias |

Notas:

1. `HexTile.Initialize` pega `GetComponentsInChildren<MeshRenderer>()` (HexTile.cs:65) e aplica MPB em todos (HexTile.cs:154-163). O que ja estiver como filho da ilha nesse momento (o visual do modelo, criado antes do `AddComponent<HexTile>` em HexWorld.cs:121-130) recebe MPB. Decoracoes (`DecoracaoDeIlha.Decorar`) sao criadas depois (`HexWorld.cs:131` Initialize, `:134` Decorar) e **nao** entram em `_arte`: ficam sem MPB (no caminho do SRP Batcher) e sem tint de hover/cor de carta (verificado pelo revisor).
2. Cada renderer da ilha/arte que recebe `SetPropertyBlock` sai do SRP Batcher. O numero de ilhas depende de `raioMinimo 4`/`raioMaximo 6` e das deformacoes (HexPrototipo.unity:3735-3737): **a medir** (draw calls, SRP Batcher "batches" vs "SetPass").
3. Em URP, `MaterialPropertyBlock` com `_BaseColor` num Lit **funciona** para tintar, mas a tintura e multiplicada pelo `_BaseMap`: nas ilhas com textura, o hover/previa muda o tom da textura inteira (HexTile.cs:156-160), o que e deliberado (mantem textura quando branco).
4. `Shader.Find("Universal Render Pipeline/Unlit")` (CartasUI.cs:186, HexPlacementGrid.cs:144): **nenhum `.mat` do projeto referencia o URP Unlit** (GUID `650dd952...` em `Assets`/`ProjectSettings`: zero), e `Always Included Shaders` nao o lista (GraphicsSettings.asset:29-36). Em **build**, `Shader.Find` pode retornar `null` e cair para `Unlit/Color`/`Sprites/Default` (este esta nos Always Included). Marcador e grade podem ficar com visual/blend diferente do Editor. **Verificar em build** (secao 7).
5. Os materiais criados em runtime marcam `_Surface=1` mas nao habilitam a keyword `_SURFACE_TYPE_TRANSPARENT`/`_ALPHAPREMULTIPLY_ON`; o blend e definido por `_SrcBlend/_DstBlend` manualmente, entao tende a funcionar, mas e fragil. **Verificar em build.**

### 4.2 Shaders customizados

| Shader | Arquivo | Proposito | Custo estatico |
|---|---|---|---|
| `Custom/SoftCloud` | `Assets/testes shaders/SoftCloud.shader` | Nuvem volumetrica em quad billboard por ray-march em caixa | ALU pesado: loop de **24 passos** por pixel (`:136-139`). Cada passo chama `density()` e, se `d>=0.001`, mais **2** `density()` (self-shadow, `:148`) = ate **72** `density()` por pixel; cada `density()` = 5 elipsoides com smooth-union + **2 value-noise 3D** (8 hashes cada). Early-out em `opacity>0.985` (`:153`) e quando o raio erra a caixa (`:128`). **1 amostra de textura** (depth, `:130`), zero texturas de ruido. Transparente (`Blend One OneMinusSrcAlpha`), **`ZTest Always`** (`:23`) + `Cull Off`, `ZWrite Off`: ocupa pixels mesmo atras de geometria e rejeita por profundidade manual; overdraw entre nuvens sobrepostas acumula. `CBUFFER UnityPerMaterial` presente (`:52-57`). `#pragma target 3.5`, `multi_compile_fog`, `multi_compile_instancing` |
| `Custom/DappledLight` | `Assets/testes shaders/DappledLight.shader` | Sombra de copa multiplicativa, procedural | Leve: 2 value-noise 2D por pixel, zero texturas, sem depth. `Blend DstColor Zero`, transparente, overdraw proporcional a area do plano. CBUFFER ok. Hoje inativo na cena |
| `WaterLowPoly` (Shader Graph) | `Assets/VFX e Shaders/Water/WaterLowPoly.shadergraph` + `SubgrafhWater.shadersubgraph` | Agua low-poly com ondas no vertex, normais por DDX/DDY e depth fade/espuma | 7 `SubGraphNode` (cada um com `GradientNoise`, 1 por subgraph) ligados a Vertex Position/Normal/Tangent e a SurfaceDescription; 2 `SceneDepth`; 2 `ScreenPosition`. Transparente Lit (forward + luzes). Nao vi `SceneColor`. Custo real: **a medir** |
| `SmoothSmoke` | `Assets/VFX e Shaders/SmoothSmoke/SmoothSmoke.vfx` | Fumaca (VFX Graph) | capacidade 64 particulas, inativo na cena |

Materiais de teste soltos: `New Material.mat` (Lit, sem textura), `Materialshader teste.mat`, `vermelhor shader.mat` (Lit). `Vermelho.mat` e `Terreno.mat` (Lit). Sem custo relevante.

### 4.3 Criacao de material em runtime (greps)

| Local | Padrao | Risco |
|---|---|---|
| `HexWorld.cs:294-303` | `new Material(Lit)` 1x por mundo | ok; nunca destruido (vaza 1 por geracao) |
| `GeradorDeMoedas.cs:175-184` | 1x | ok |
| `HexPlacementGrid.cs:144-157` | 1 por linha | poucos |
| `CartasUI.cs:186-192` | 1x | ok |
| **`Inimigo.cs:299-306`** | **1 por inimigo** sem prefab, nunca destruido | vazamento acumulativo por spawn |
| **`Projetil.cs:34-40`** | **1 por flecha**, `Destroy(gameObject)` nao destroi o Material | vazamento por tiro |
| `renderer.material` / `.material` | **nenhuma ocorrencia** em `Assets/Scripts/Hex` e `testes shaders` | bom: `AGENTS.md` regra 5 cumprida no codigo ativo |
| `SetPropertyBlock` por frame | `RioDeFundo.LateUpdate` (1), `CloudController.AtualizarAparencia` (18 nuvens, chamada duas vezes por frame: `CloudController.Update` e `CamadaDeNuvens.Atualizar`, `CloudController.cs:56` e `CamadaDeNuvens.cs:90`), `CartasUI` (hover), `DappledLightSync` (inativo) | CPU/GC baixo, mas impede SRP Batcher |

---

## 5. Riscos de legibilidade (renderizacao)

Prioridade do projeto: legibilidade do gameplay antes de estetica. Todos os itens abaixo sao hipoteses derivadas dos dados; confirmar com capturas (secao 7).

1. **Neblina Exponential 0.01 e cor = ceu.** Se a neblina por pixel usa a distancia de visao, a camera esta a >= 40 unidades do centro (`HexIsoCamera.cs:89`), e `exp(-0.01*40) ~ 0.67`: cerca de **33% de mistura com a cor do ceu** no centro e mais nas ilhas distantes (conta a partir da formula do URP, `ShaderVariablesFunctions.hlsl:362-363`, nao medida). Isso dessatura/clareia as ilhas e piora a diferenca entre caminho, ilha construida e inimigos, justamente nas horas em que o ceu e claro. **A medir** em captura 16:9.
2. **Neblina com camera ortografica.** O URP calcula o fator de neblina por vertice a partir do z do clip space (`ComputeFogFactor(positionCS.z)` -> `UNITY_Z_0_FAR_FROM_CLIPSPACE`, `Core.hlsl:30-46`), formula pensada para perspectiva; para projecao ortografica o z de clip e linear e a conversao nao e equivalente. Ja o caminho `_FOG_FRAGMENT` usa `viewZ` (`ShaderVariablesFunctions.hlsl:408-414`) e e consistente com ortho. Ou seja: **o comportamento da neblina pode divergir entre materiais** (shaders que usam fog por vertice x por fragmento), e o `SoftCloud` usa `InitializeInputDataFog` por fragmento. **Verificar em captura se ilhas e nuvens estao com neblina coerente**; nao afirmo qual caminho o Lit usa nesta combinacao de keywords.
3. **Neblina varia com a hora** (cor = ceu). A 18h a neblina fica coral `F2937A`; a 0h azul escuro `2B3A67`, com sol 0.1: ilhas e inimigos podem perder contraste a noite.
4. **SSAO** (intensity 0.4, radius 0.3, falloff 100, DirectLightingStrength 0.25): o raio de 0.3 e do mesmo tamanho de props de 0.28 a 0.65 de altura (DecoracaoDeIlha.cs:23): escurece contato props/chao e encontros de ilhas. Com `Source` Depth Normals e sem AA, bordas de AO podem tremer. Pode ajudar volume ou sujar cogumelos/pedras pequenos. **A avaliar**: desligar SSAO e comparar.
5. **Bloom threshold 1, intensity 0.6, scatter 0.6, sem tonemapping, HDR ligado.** Sol pico 1.4 com `F2F5FF` somado a ambiente `9FD8E8`: superficies claras (acampamento amarelo (1, 0.82, 0.32), nuvens brancas) podem passar de 1.0 e brilhar. Com `contrast +12` e `saturation +20`, bordas claras viram halos. Nuvens (`SoftCloud`) com cor branca premultiplicada tambem entram. **A medir** (capturas ao meio-dia; sem Bloom para comparar).
6. **Vignette 0.2 / smoothness 0.45:** escurece cantos. Em ortho com anel hexagonal quase encostando nas bordas (`margem: 1.3` na cena) as ilhas dos cantos recebem menos luz. Pequeno, mas e a regiao de HUD/cartas. **A medir** (luminancia de canto vs centro).
7. **Sombras:** distancia 50 medida a partir da camera, que esta a >= 40-60 do centro (`max(40, meiaAltura*3)`); a parte distante do anel pode cair fora da distancia de sombra, ou ficar em cascata baixa (4 cascatas com splits 0.12/0.29/0.54 distribuem pouca resolucao ao "meio" da cena). Ortografico nao precisa de 4 cascatas se a profundidade util for pequena. **A medir** (qualidade de sombra no lado distante do anel; `Rendering Debugger > Lighting > Shadow Cascades`).
8. **Bias de sombra** do asset (depth 0.1, normal 0.5) para props de 0.28-0.65: risco de "peter panning"/vazamento de luz em pecas pequenas. **A avaliar**.
9. **Nuvens** sob as ilhas (`ZTest Always`, queue transparente): podem desenhar sobre HUD de mundo e marcadores transparentes se a ordem de fila inverter; opacidade ate 0.8 (CamadaDeNuvens `opacidade 0.8`) com `_FogBlend` 1. Como ficam **abaixo** das ilhas, a legibilidade do gameplay e pouco afetada; o risco e visual (nuvem cortando a silhueta das ilhas em ortho a 30 graus).
10. **Sem AA:** silhuetas estilizadas pequenas serrilham; legibilidade de shapes (principio Tunic) sofre. **A avaliar**: FXAA/SMAA de camera x custo.
11. **Marcador/grade transparentes** (`Unlit` transparente, `ZWrite 0`, y + 0.05): podem ficar atras de agua transparente ou sob nuvem; plano transparente sobre ilha com `receiveShadows false` ok. **Verificar em build** (item 4.1.4).
12. **Acampamento.unity sem luz** (0 luzes, ambient Skybox com material default): qualquer objeto Lit fica so com ambiente. Fora do escopo desta cena, mas e uma cena do build (index 0).

### 5.5 Camera ortografica: near/far e precisao

Near 0.1 e far >= 1000 com a camera 40+ unidades afastada: o buffer de profundidade distribui o intervalo [0.1, 1000] para uma geometria que ocupa uma faixa util de, no maximo, algumas dezenas de unidades. Em ortografico o mapeamento e linear (nao ha a perda de precisao 1/z da perspectiva), entao z-fighting e menos provavel que em perspectiva, mas a **resolucao de profundidade para SSAO, depth fade da agua e do `SoftCloud`** depende de `far - near`: um far menor tende a ajudar. **Proposta (nao medida):** ajustar near/far ao envelope real (`distanciaReal` +/- raio da cena); decidir apos medicao.

---

## 6. Riscos de desempenho (estaticos)

Nenhum numero e medido. Ordem aproximada por probabilidade de peso, a validar:

1. **`SoftCloud` x 18 instancias.** Pior caso estatico: 24 passos x ate 3 `density()` por passo x 2 noises 3D = 144 noises 3D por pixel coberto, mais depth sample. Custo proporcional a area em pixels das nuvens (13% a 21% da altura visivel por nuvem, `tamanho` (0.13, 0.21)) x overdraw. Em 4K/21:9 a area em pixels sobe muito. **A medir:** GPU ms do passe das nuvens, em 1080p/1440p/4K.
2. **Pre-passe de Depth Normals** (SSAO `Source: 1`) + copia de depth + copia de cor (opaque texture) + SSAO em resolucao cheia (`Downsample: 0`) + Bloom: varios passes de tela cheia para uma cena com 1 luz. **A medir** por passe.
3. **Forward+** com uma luz so: overhead de culling de luzes por tile sem proveito. **A medir.**
4. **Sombras:** 4 cascatas x 2048 + soft shadows High: ate 4 renders de cena para sombra por frame, para mundo que cabe em um envelope pequeno. **A medir** (Frame Debugger, "Main Light Shadowmap").
5. **MPB em ilhas e arte:** cada renderer com MPB sai do SRP Batcher; `HexTile` coloca MPB em todos os filhos. Para N ilhas x (1 + decoracoes) renderers vira N draws por objeto. **A medir** (SRP Batcher ativo? "SetPass calls", batches).
6. **Agua transparente Lit** com 7 nos de ruido e 2 `SceneDepth`; tela grande se o plano cobre o fundo (`FundoContinuo`/`CenarioContinuo` ajustam ao enquadramento). **A medir.**
7. **CPU/GC por frame:** `CloudController.AtualizarAparencia` 2x por nuvem por frame (36 `GetPropertyBlock`/`SetPropertyBlock`); `RioDeFundo.Ajustar` todo `LateUpdate`; `DayNightController.Apply` todo frame (alocacao: `Gradient.Evaluate` nao aloca, `colorKeys.Length` so em `OnValidate`). `HexIsoCamera.MedirIlhas` (a cada 0.25 s, `GetComponentsInChildren<Renderer>` por ilha, aloca arrays) e `CamadaDeNuvens` (a cada 0.5 s, `GetComponentsInChildren<MeshRenderer>` por ilha) geram GC periodico. **A medir** no Profiler (GC.Alloc por frame).
8. **Vazamento de materiais** (Inimigo sem prefab, Projetil): cresce memoria de materiais/Shader keywords por spawn; **a medir** (Profiler Memory: contagem de `Material`).
9. **Mobile** (se for alvo): `Mobile_RPAsset` com render scale 0.8, 1 cascata, sem SSAO, sem depth/opaque texture: o `SoftCloud` e a agua precisam de depth texture, que esta **desligada** no Mobile; nuvens e profundidade da agua dependem dela e **vao quebrar** no nivel Mobile. Confirmar se Mobile e alvo; senao, o nivel deve ser removido/documentado.

---

## 7. Plano de medicao (baseline)

Objetivo: gerar numeros reais antes de qualquer decisao. Tudo isso e **pendente**; nenhum dado existe hoje.

### 7.1 Ambiente e protocolo

- Rodar em **build Development** (Windows, IL2CPP ou Mono conforme o alvo) e comparar com o Editor. O Editor soma overhead de Scene/Game view; use o numero de build como referencia, o do Editor so para depuracao.
- Maquina de referencia anotada (CPU, GPU, driver, resolucao, RAM). Mesma maquina em todas as rodadas.
- Fixar o estado: `Daynightcontroller.autoAdvanceTime = false` (via script de teste ou inspecao) e medir em horas fixas: **00h, 06h, 12h, 18h** (4 cenarios de iluminacao; nuvens variam com a luz). Fixar `usarSementeFixa = true` com a mesma semente (HexLoopGenerator) para comparar o mesmo anel.
- VSync desligado (ja esta 0) para medir ms de GPU; repetir uma vez com VSync ligado.
- Resolucoes: **1920x1080 (16:9)**, **2560x1080 (21:9)**, **3440x1440 (21:9)** e 3840x2160 se for alvo. Em ortho o enquadramento muda com o aspecto (`MedirIlhas` usa `Cam.aspect`): registrar `orthographicSize` e `distanciaReal` por aspecto.
- Aquecer 10 s antes de medir; coletar 60 s por cenario; reportar media, p95 e p99 de frame time (nao so FPS).

### 7.2 Ferramentas e contadores

| Ferramenta | O que registrar |
|---|---|
| **Unity Profiler** (build, deep profile desligado) | CPU: frame ms (main/render thread), `PlayerLoop`, `Update/LateUpdate` dos scripts acima (`DayNightController`, `CamadaDeNuvens`, `CloudController`, `RioDeFundo`, `HexIsoCamera`); `GC.Alloc` por frame e total; `SetPass Calls`, `Batches`, `Draw Calls`, `Triangles` no modulo Rendering |
| **Profiler > Memory / Memory Profiler** | contagem de `Material` e `Shader`/keywords antes e depois de gerar N inimigos e N flechas (confirmar vazamento) |
| **Frame Debugger** | lista de passes (DepthNormals, SSAO, Copy Depth/Color, Shadows x4, Opaque, Water, Clouds x18, Bloom, UberPost), quantos draws por passe, quais renderers fora do SRP Batcher ("Node is not compatible" / `MaterialPropertyBlock`); confirmar se existe copia de cor e se algum consumidor a usa |
| **Rendering Debugger** (URP) | Shadow Cascades, overdraw de transparentes, AO isolado, Lighting only |
| **RenderDoc** (ou PIX/Nsight) | GPU ms por passe com timestamp; custo do passe das nuvens (pixel shader), do SSAO e do Bloom; ocupacao e overdraw das nuvens em 4K |
| **Profiler > GPU / `FrameTimingManager`** | `gpuFrameTime`, `cpuMainThreadFrameTime`, `cpuRenderThreadFrameTime` por cenario |
| **Capturas** | PNG por hora x aspecto (com e sem SSAO/Bloom/Vignette/fog) para auditoria de legibilidade (secao 5) |

### 7.3 Experimentos A/B (um fator por vez, todos reversiveis)

1. Baseline atual.
2. Nuvens desligadas (`CamadaDeNuvens` desativada) -> custo isolado das nuvens.
3. SSAO off -> custo do SSAO + pre-passe de normais; ver se o Frame Debugger mostra o prepass desaparecer.
4. Opaque texture off -> custo da copia de cor; conferir que nada quebra visualmente.
5. Bloom off / Vignette off / ColorAdjustments off -> custo do pos.
6. Forward (nao Forward+) -> diferenca com uma luz.
7. Cascatas 4 -> 2 -> 1 e distancia 50 -> ajustada ao envelope; soft shadow Low/High -> custo e qualidade visual.
8. Fog off vs on -> custo e efeito na legibilidade (comparar capturas).
9. Remover MPB de ilhas (somente em experimento descartavel; por exemplo cor via 2 materiais compartilhados ou atributo por vertex color) -> ganho de batching.
10. Agua desligada.

Cada experimento reporta delta de GPU ms, CPU ms, `SetPass`, `Batches`. So depois disso ha base para otimizar.

### 7.4 Orcamento (PROPOSTA, nao medicao)

Faixas de partida para uma cena isometrica indie pequena; ajustar apos medir no hardware-alvo real.

| Item | Proposta de orcamento |
|---|---|
| Frame total | 16.6 ms (60 FPS) com folga; 8.3 ms se alvo 120 FPS; confirmar hardware-alvo |
| GPU | <= ~10 ms em 1080p 16:9 no minimo recomendado, com margem para 21:9 |
| CPU main thread | <= ~6 ms de logica + render submission |
| `SetPass Calls` | dezenas (ordem de 50-100) em vez de centenas |
| Batches | idem; usar SRP Batcher sempre que possivel |
| Triangulos | cena de baixa poly; ordem de poucas centenas de milhares e suficiente |
| Passes full-screen extras | no maximo 3-4 (ex.: SSAO opcional, Bloom, UberPost) |
| Alocacao por frame | **0 bytes** em estado estavel (excluindo spawns) |
| Memoria de texturas | definir apos inventario de importacao (nao auditado aqui) |

Esses numeros sao ponto de partida de planejamento, sem base em medicao deste projeto.

---

## 8. Recomendacoes

Principios: **nenhuma mudanca de arquitetura de render sem medicao**; um dono unico para `RenderSettings`; nada de instanciar material por frame/por spawn; uma mudanca por PR.

### 8.1 Governanca (sem medir, sao decisoes de organizacao)

1. **Dono unico de `RenderSettings`:** manter `DayNightController` como unico escritor (ja e). Documentar que a cor de fog/ambient/camera no YAML e um artefato (3.3) e que o hex fixo do Volume nao deve ser alterado em conflito com o controlador. Quando for reorganizar, mover para o namespace `Loopia.Hex` + pasta fora de `testes shaders/` em PR proprio (respeitando `AGENTS.md` sec. 3: nao mover script referenciado em cena sem `.meta`).
2. **Evitar suja da cena:** `[ExecuteAlways]` + `OnValidate` grava fog/ambient/camera na cena a cada ajuste. Avaliar (depois, por PR) separar "preview no Editor" de "estado salvo". Cena so por Editor/MCP (AGENTS.md regra 1).
3. **Sem instanciar material por frame/spawn:** corrigir `Inimigo.MontarVisual` (sem prefab) e `Projetil` para usar material compartilhado estatico + `MaterialPropertyBlock` (ou um unico material por cor), e destruir materiais criados em `OnDestroy`. Conforme `AGENTS.md` regra 5; correcao pequena e de baixo risco.
4. **Shader de marcador/grade:** garantir que o shader usado exista no build (referenciar um material URP Unlit no projeto ou incluir o shader em Always Included, ou criar o material como asset em vez de `Shader.Find`). Confirmar em build antes de decidir.

### 8.2 Depende de medicao (nao fazer antes)

- Trocar Forward+ por Forward; reduzir cascatas/soft shadow; ajustar distancia de sombra e near/far.
- Desligar Opaque Texture (se confirmado que nada a usa).
- Reduzir custo do `SoftCloud` (menos passos, menos nuvens, resolucao do passe, cache de ruido em textura) e remover `ZTest Always`.
- SSAO: manter, reduzir ou remover conforme custo x legibilidade.
- Substituir MPB nas ilhas por material compartilhado por estado (ganho de SRP Batcher) **so se** o Frame Debugger mostrar que ele quebra o batching de forma relevante.
- Remover `com.unity.postprocessing` (nao usado): PR proprio, verificando compilacao.
- Limpar `DefaultVolumeProfile` dos componentes de teste do URP e dos restos de cena (probes, Plane/Cube/QuadClouds inativos, Cenario LightProbeGroup fora do anel), sempre pelo Editor.
- Tonemapping: hoje ausente com HDR + Bloom; decidir olhando capturas ao meio-dia.
- Anti-aliasing (FXAA/SMAA) para silhuetas: decidir com captura e custo.
- Nivel Mobile: definir se e alvo; senao documentar/remover (nuvens e agua quebram sem depth texture, secao 6.9).

### 8.3 Pendencias desta auditoria

- Nao li o material efetivo de Lucca (FBX/`Lucca.prefab`), nem importacao de texturas (tamanho, compressao, mipmaps).
- Traducao de enums brutos (`m_RenderingMode: 2`, SSAO `Source: 1`, `AOMethod`, `m_MixedBakeMode: 2`) e leitura do URP 17.3; confirmar no Inspector.
- Qual caminho de fog (vertex ou fragment) o Lit usa nesta combinacao de keywords em ortho: confirmar em captura.
- Nenhum dado de performance foi coletado.

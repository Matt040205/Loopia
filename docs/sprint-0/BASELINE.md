# Baseline tecnico - Sprint 0

Inventario factual do projeto Unity na branch `sprint-0/preparacao-tecnica`.
Tudo abaixo foi verificado por leitura de arquivos ou saida de `git log/show/status/ls-files`. Quando nao foi possivel verificar, esta escrito "nao verificado".

---

## 1. Referencia

| Item | Valor |
|---|---|
| Branch | `sprint-0/preparacao-tecnica` |
| Branch base | `main` |
| Commit base (`git log -1 main`) | `0b9513e79afc439a58c1a0bc6a5d816aa08e399b` |
| Autor do commit base | Matt040205 |
| Data do commit base | Fri Oct 9 10:49:14 2026 -0300 |
| Assunto do commit base | Modelo e textura da arvore e do Morcego |
| Merge-base da branch com `main` | `0b9513e` (verificado) |
| Data desta baseline | 2026-11-14 |
| Versao do Unity (`ProjectSettings/ProjectVersion.txt`) | `6000.3.10f1` (revisao `e35f0c77bd8e`) |

---

## 2. Dependencias

### 2.1 Pacotes pedidos em `Packages/manifest.json` (47 entradas: 15 pacotes + 32 modulos)

| Pacote | Versao pedida | Versao resolvida (`packages-lock.json`) | Fonte no lock | Observacao |
|---|---|---|---|---|
| `com.coplaydev.unity-mcp` | `https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity#main` | hash `30d22075093d1d35dfb0091c1c7550e9ad948577` | git | **NAO FIXADO**: aponta para a branch `#main`. O hash e o commit atualmente travado no lock. |
| `com.unity.2d.sprite` | 1.0.0 | 1.0.0 | builtin | |
| `com.unity.ai.navigation` | 2.0.6 | 2.0.6 | registry | NavMesh usado pelo Lucca (ver secao 4). |
| `com.unity.collab-proxy` | 2.8.2 | 2.8.2 | registry | |
| `com.unity.ide.rider` | 3.0.31 | 3.0.31 | registry | |
| `com.unity.ide.visualstudio` | 2.0.22 | 2.0.22 | registry | |
| `com.unity.inputsystem` | 1.13.1 | 1.13.1 | registry | Ver nota de uso de input na secao 2.2. |
| `com.unity.multiplayer.center` | 1.0.0 | **1.0.1** | builtin | **Divergencia** entre manifest e lock. Motivo nao verificado. |
| `com.unity.postprocessing` | 3.4.0 | 3.4.0 | registry | Pacote legado de pos-processamento convivendo com URP (ver 2.2). |
| `com.unity.render-pipelines.universal` | 17.3.0 | 17.3.0 | builtin | Pipeline ativa (secao 8). |
| `com.unity.test-framework` | 1.4.6 | **1.6.0** | builtin | **Divergencia** entre manifest e lock. Motivo nao verificado. |
| `com.unity.timeline` | 1.8.7 | 1.8.7 | registry | |
| `com.unity.ugui` | 2.0.0 | 2.0.0 | builtin | |
| `com.unity.visualeffectgraph` | 17.3.0 | 17.3.0 | builtin | Usado por `FumacaDeMorte.cs` (VisualEffect). |
| `com.unity.visualscripting` | 1.9.5 | 1.9.5 | registry | |
| `com.unity.modules.*` (32 modulos) | 1.0.0 cada | 1.0.0 cada, todos `builtin` | builtin | accessibility, ai, androidjni, animation, assetbundle, audio, cloth, director, imageconversion, imgui, jsonserialize, particlesystem, physics, physics2d, screencapture, terrain, terrainphysics, tilemap, ui, uielements, umbra, unityanalytics, unitywebrequest, unitywebrequestassetbundle, unitywebrequestaudio, unitywebrequesttexture, unitywebrequestwww, vehicles, video, vr, wind, xr |

### 2.2 Pacotes relevantes para gameplay e testes

| Pacote | Versao | Relevancia | Nota |
|---|---|---|---|
| `com.unity.ai.navigation` | 2.0.6 | Gameplay | `NavegacaoDoMundo.cs` faz bake de NavMesh em tempo de jogo e cria NavMeshLinks. |
| `com.unity.inputsystem` | 1.13.1 | Gameplay | Scripts Hex de entrada (`HexPointer`, `CartasUI`, `HexPlacementGrid`) existem. **Qual API de input eles usam (Input legado ou Input System) nao verificado.** |
| `com.unity.test-framework` | manifest 1.4.6 / lock 1.6.0 | Testes | Ver secao 10: `Assets/Tests` existe mas esta vazio no rastreamento. |
| `com.unity.render-pipelines.universal` | 17.3.0 | Render | Pipeline ativa. |
| `com.unity.visualeffectgraph` | 17.3.0 | Render/VFX | Usado por `FumacaDeMorte.cs`. |
| `com.unity.postprocessing` | 3.4.0 | Render | **Flag:** pacote legado de pos-processamento coexistindo com URP. `VolumeCena.asset` usa overrides de Volume (secao 8). Qual pipeline consome cada override nao verificado. |

---

## 3. Cenas no Build Settings

Fonte: `ProjectSettings/EditorBuildSettings.asset`. Fluxo ativo: indices 0 e 1. Os demais sao do prototipo legado.

| Indice | Caminho | Habilitada | GUID | Fluxo |
|---|---|---|---|---|
| 0 | `Assets/Scenes/Acampamento.unity` | sim | `c1d006eb4dfd9b142abaf21e0325ad6a` | **ATIVO** |
| 1 | `Assets/Scenes/HexPrototipo.unity` | sim | `353c34180e7aa7a4abe37f2d1a018240` | **ATIVO** |
| 2 | `Assets/Scenes/Menu.unity` | sim | `a5f844f090b9b7745864aad49197f49d` | Legado (usa `MenuController`, de `Assets/Scripts/Manager`) |
| 3 | `Assets/Scenes/Testes.unity` | sim | `99c9720ab356a0642a771bea13969a05` | Legado (usa `LoopGenerator`, `GameManager`, `BossManager`, `UI*`, `PlayerInteraction` etc.) |
| 4 | `Assets/Scenes/GameOver.unity` | sim | `7bde523a7d5334c4cb7152a1d28ec01f` | Legado (usa `MenuController`) |
| 5 | `Assets/Scenes/Win.unity` | sim | `0de03488fb9093a4d93a6e3a601839c1` | Legado (usa `MenuController`) |

Observacao: as cenas legadas estao **habilitadas** no Build Settings.

---

## 4. Sistemas ativos (`Assets/Scripts/Hex/`, namespace `Loopia.Hex`)

43 arquivos `.cs`, 5629 linhas no total (`wc -l`). Responsabilidades vem do `/// <summary>` de cada arquivo. Quando nao ha resumo, a linha indica "lido do codigo".

### 4.1 Mundo / Geracao (13)

| Arquivo | Linhas | Classe principal | Responsabilidade |
|---|---|---|---|
| HexCoord.cs | 89 | `struct HexCoord` | Coordenada axial (q, r) pointy-top; eixo s derivado. |
| HexLayout.cs | 53 | `struct HexLayout` | Conversao entre coordenada axial e posicao no plano XZ. |
| HexMesh.cs | 79 | `static class HexMesh` | Gera a malha do hexagono (laje extrudada, topo em Y=0). |
| HexWorld.cs | 306 | `HexWorld` (ordem -200) | Grade sem borda; so as ilhas existem. Cada casa esta em: com ilha, livre ou bloqueada (miolo do anel, reservado ao Boss). |
| HexTile.cs | 185 | `HexTile`, `enum HexTileKind`, `struct HexPalette` | Uma casa do anel: o caminho que o Lucca percorre. |
| HexLoopGenerator.cs | 408 | `HexLoopGenerator` (ordem -150) | Gera o anel com formato sorteado, com miolo minimo de 3x3 para o Boss. Emite `AoGerarMapa`. |
| HexLoopPath.cs | 56 | `class HexLoopPath` | Caminho em loop: lista ordenada de casas, a ultima vizinha da primeira. |
| VisualDeIlha.cs | 50 | `static class VisualDeIlha` | Centraliza modelos com pivos diferentes; mantem a arte fora do bake. |
| DecoracaoDeIlha.cs | 68 | `DecoracaoDeIlha` | Reserva o centro e os seis acessos, inclusive para ilhas colocadas depois. |
| CenarioContinuo.cs | 74 | `CenarioContinuo` (ExecuteAlways, ordem 210) | Ajusta a escala do cenario de fundo para a camera. Sem resumo; lido do codigo. |
| FundoContinuo.cs | 40 | `FundoContinuo` (ExecuteAlways, ordem 200) | Cobre a projecao inteira da camera no plano do fundo. |
| RioDeFundo.cs | 54 | `RioDeFundo` (ExecuteAlways, ordem 220) | Mantem o rio no espaco do cenario e as ondas em unidades de mundo. |
| CamadaDeNuvens.cs | 154 | `CamadaDeNuvens` (ExecuteAlways, ordem 230) | Distribui volumes de nuvem abaixo das ilhas; recicla fora da tela. |

### 4.2 Movimento / Navegacao (5)

| Arquivo | Linhas | Classe principal | Responsabilidade |
|---|---|---|---|
| HexPlayer.cs | 314 | `HexPlayer` | O Lucca sobre o mundo: anda por NavMeshAgent e salta vaos por NavMeshLink. Emite `AoEntrarNoHexagono` e `AoChegarNoDestino`. |
| HexLoopRunner.cs | 157 | `HexLoopRunner` | Faz o Lucca percorrer o anel sozinho. Emite `AoCompletarVolta`. |
| NavegacaoDoMundo.cs | 146 | `NavegacaoDoMundo` (ordem -100) | Faz o bake do NavMesh em tempo de jogo a cada mapa novo e cria NavMeshLinks na area "Jump". |
| HexIsoCamera.cs | 149 | `HexIsoCamera` | Camera isometrica ortografica (inclinacao 30, giro 45), com enquadramento automatico. |
| HexPointer.cs | 163 | `HexPointer` | Traduz o mouse em ordens: destaque e caminho ao passar; botao direito manda o player andar. |

### 4.3 Cartas / Plataformas (12)

| Arquivo | Linhas | Classe principal | Responsabilidade |
|---|---|---|---|
| PlataformaDef.cs | 71 | `abstract PlataformaDef : ScriptableObject`, `enum ColocacaoPermitida` | Base de carta: onde pode ser solta (NoCaminho, ForaDoCaminho, Qualquer) e o efeito. |
| PlataformaSimplesDef.cs | 23 | `PlataformaSimplesDef` | Bonus fixo (basica, campo pacifico, torre). |
| PlataformaVizinhancaDef.cs | 40 | `PlataformaVizinhancaDef : PlataformaSimplesDef` | Bonus que cresce com as vizinhas 3x3 (cabana, area de treino). |
| PlataformaRecompensaDef.cs | 43 | `PlataformaRecompensaDef` | Recompensa ao passar por cima: coracao (+5 PV), flechas (+10), moedas (+3). |
| PlataformaFlorestaDef.cs | 45 | `PlataformaFlorestaDef` | Repovoa lobos na propria casa a cada volta fechada, se estiver vazia. |
| PlataformaMolaDef.cs | 14 | `PlataformaMolaDef` | Ao ser passada, carrega um impulso (`alturaDoImpulso`) no HexPlayer. Sem resumo; lido do codigo. |
| PlataformaRelevoDef.cs | 11 | `PlataformaRelevoDef` | Ilha alta (altura 2.2). Disponivel para compra so se o mapa tem mola. Sem resumo; lido do codigo. |
| Plataforma.cs | 48 | `Plataforma` | Uma plataforma colocada: par "que carta" + "em que casa", mais o estado do efeito. |
| MapaDePlataformas.cs | 375 | `MapaDePlataformas` | Guarda o que foi construido em cada casa; unico lugar que decide se uma carta pode ser solta; recalcula bonus. Emite `AoMudar`. |
| MaoDeCartas.cs | 148 | `MaoDeCartas` | Mao com 4 espacos; carta nova ao subir de nivel. |
| CartasUI.cs | 341 | `CartasUI` | Barra de cartas e arraste; marcador verde ou vermelho sobre a casa. |
| HexPlacementGrid.cs | 312 | `HexPlacementGrid` | Grade que aparece so durante o arraste de carta. Ligada por tecla enquanto o sistema de cartas nao existe (segundo o resumo). |

### 4.4 Combate / Inimigos (7)

| Arquivo | Linhas | Classe principal | Responsabilidade |
|---|---|---|---|
| Inimigo.cs | 309 | `Inimigo` | Inimigo vivo. Saidas por UnityEvent: `aoAtacar`, `aoComecarRasante`, `aoSerDerrotado`. |
| InimigoDef.cs | 73 | `InimigoDef : ScriptableObject`, `enum EstiloDeAtaque` | Define o estilo de ataque (ex.: `CorpoACorpo`, usado pelo lobo). Demais valores do enum nao lidos. |
| InimigoSpawner.cs | 209 | `InimigoSpawner` | Coloca morcegos no anel e lobos na floresta. Repassa `aoAtacar` por `aoInimigoAtacar`. |
| LuccaCombate.cs | 125 | `LuccaCombate` | Combate automatico: atira enquanto anda; cada tiro gasta uma flecha. |
| LuccaStatus.cs | 167 | `LuccaStatus` | Status do Lucca: 50 PV, 35 ATK, 15 flechas, 0 moedas, 0 XP. Emite `AoMorrer`, `AoSubirDeNivel`, `AoMudar`. |
| Projetil.cs | 79 | `Projetil` | Flecha: persegue o alvo e aplica dano; some se o alvo morrer antes. |
| FumacaDeMorte.cs | 20 | `FumacaDeMorte` | Toca o VFX de fumaca e se destroi apos a duracao. Sem resumo; lido do codigo. |

### 4.5 Progressao / Run (6)

| Arquivo | Linhas | Classe principal | Responsabilidade |
|---|---|---|---|
| LoopDirector.cs | 174 | `LoopDirector` | Regras ao fechar volta (recupera PV e municao, +10% nos inimigos). Boss apos 4 loops, mas o Boss ainda nao existe (segundo o resumo). |
| DadosDoJogador.cs | 141 | `DadosDoJogador` (ordem -1000) | Singleton persistente (DontDestroyOnLoad): moedas e recorde de voltas, salvos em JSON. Emite `AoMudar`. |
| GeradorDeMoedas.cs | 186 | `GeradorDeMoedas` | Moedas que caem nas ilhas do caminho e sao coletadas pelo Lucca. |
| ItemGiratorio.cs | 17 | `ItemGiratorio` | Gira e balanca verticalmente um item (visual). Sem resumo; lido do codigo. |
| TelaDeMorte.cs | 60 | `TelaDeMorte` | Tela de morte, com botao "Volte ao acampamento" (`VoltarAoAcampamento`). |
| TelaDoAcampamento.cs | 53 | `TelaDoAcampamento` | Mostra moedas e recorde lidos de `DadosDoJogador`; `NovaJornada()` inicia a jornada. |

**Total dos grupos:** 13 + 5 + 12 + 7 + 6 = 43 arquivos, 5629 linhas.

---

## 5. Eventos e dependencias entre sistemas

### 5.1 Eventos C# (`public event`) e assinantes (`+=`)

| Evento | Declarado em | Disparado em | Assinantes encontrados |
|---|---|---|---|
| `HexLoopGenerator.AoGerarMapa` | HexLoopGenerator.cs:65 | HexLoopGenerator.cs:131 | GeradorDeMoedas.cs:68 (Recomecar); HexLoopRunner.cs:95 (MapaFoiGerado); InimigoSpawner.cs:86 (LimparTodos); MapaDePlataformas.cs:72 (LimparTudo); NavegacaoDoMundo.cs:57 (Reconstruir) |
| `HexLoopRunner.AoCompletarVolta` | HexLoopRunner.cs:42 | HexLoopRunner.cs:154 | GeradorDeMoedas.cs:69 (VoltaFechou); MapaDePlataformas.cs:71 (VoltaFechou); LoopDirector.cs:74 (FechouUmaVolta) |
| `HexPlayer.AoEntrarNoHexagono` | HexPlayer.cs:86 | HexPlayer.cs:254 | GeradorDeMoedas.cs:67 (Coletar); HexLoopRunner.cs:114; MapaDePlataformas.cs:70 (PlayerPassouPor) |
| `HexPlayer.AoChegarNoDestino` | HexPlayer.cs:89 | HexPlayer.cs:260 | **Nenhum** |
| `LuccaStatus.AoMorrer` | LuccaStatus.cs:52 | LuccaStatus.cs:104 | LoopDirector.cs:75 (LuccaMorreu) |
| `LuccaStatus.AoSubirDeNivel` | LuccaStatus.cs:49 | LuccaStatus.cs:161 | MaoDeCartas.cs:69 (SubiuDeNivel) |
| `LuccaStatus.AoMudar` | LuccaStatus.cs:46 | 8 disparos (ex.: :76, :164) | **Nenhum** |
| `MaoDeCartas.AoMudar` | MaoDeCartas.cs:55 | :105, :117, :135 | **Nenhum** |
| `MapaDePlataformas.AoMudar` | MapaDePlataformas.cs:49 | :372 | **Nenhum** |
| `DadosDoJogador.AoMudar` | DadosDoJogador.cs:53 | :81, :91, :138 | TelaDoAcampamento.cs:30 (Atualizar) |
| `LoopDirector.AoCompletarVolta` | LoopDirector.cs:54 | LoopDirector.cs:123 | **Nenhum** |
| `LoopDirector.AoLiberarBoss` | LoopDirector.cs:55 | LoopDirector.cs:129 | **Nenhum** (Boss ainda nao existe) |
| `LoopDirector.AoPerder` | LoopDirector.cs:56 | LoopDirector.cs:100 | **Nenhum** |

### 5.2 UnityEvents (`UnityEvent`) e ligacoes

| UnityEvent | Declarado em | Disparado em | Ligacao |
|---|---|---|---|
| `Inimigo.aoAtacar` (`UnityEvent<int>`) | Inimigo.cs:36 | Inimigo.cs:255 (Golpear) | `InimigoSpawner.cs:138` (`AddListener(RepassarAtaque)`) |
| `InimigoSpawner.aoInimigoAtacar` (`UnityEvent<int>`) | InimigoSpawner.cs:50 | InimigoSpawner.cs:159 (RepassarAtaque) | HexPrototipo.unity:4009 chama `LuccaStatus.ReceberDano`. **Alvo verificado; componente de origem no Inspector nao verificado.** |
| `LoopDirector.aoGameOver` (`UnityEvent`) | LoopDirector.cs:35 | LoopDirector.cs:101 | HexPrototipo.unity:2436 chama `TelaDeMorte.Mostrar` (alvo verificado pelo `m_TargetAssemblyTypeName`). |
| Botao de volta ao acampamento | TelaDeMorte.cs | - | HexPrototipo.unity:2734 chama `TelaDeMorte.VoltarAoAcampamento`. |
| `Acampamento` (botao de nova jornada) | TelaDoAcampamento.cs | - | Acampamento.unity:871 chama `TelaDoAcampamento.NovaJornada`. |
| `Inimigo.aoComecarRasante` (`UnityEvent`) | Inimigo.cs:39 | Inimigo.cs:151 | Ligacao no Inspector nao verificada. |
| `Inimigo.aoSerDerrotado` (`UnityEvent<Inimigo>`) | Inimigo.cs:42 | Inimigo.cs:267 | Ligacao no Inspector nao verificada. |

Nota: existem dois metodos chamados `Mostrar` (`TelaDeMorte` e `HexPlacementGrid`). A ligacao da cena aponta para `TelaDeMorte`.

### 5.3 `[DefaultExecutionOrder]`

| Classe | Arquivo | Valor |
|---|---|---|
| `DadosDoJogador` | DadosDoJogador.cs:15 | -1000 |
| `HexWorld` | HexWorld.cs:16 | -200 |
| `HexLoopGenerator` | HexLoopGenerator.cs:21 | -150 |
| `NavegacaoDoMundo` | NavegacaoDoMundo.cs:19 | -100 |
| `FundoContinuo` | FundoContinuo.cs:6 | 200 (ExecuteAlways) |
| `CenarioContinuo` | CenarioContinuo.cs:5 | 210 (ExecuteAlways) |
| `RioDeFundo` | RioDeFundo.cs:8 | 220 (ExecuteAlways) |
| `CamadaDeNuvens` | CamadaDeNuvens.cs:8 | 230 (ExecuteAlways) |

Demais classes Hex nao declaram ordem (padrao 0). O comentario em `NavegacaoDoMundo.cs:16` diz que a ordem -100 existe para rodar antes do NavMeshAgent do Lucca.

### 5.4 Cadeias principais

- Ciclo de volta: `HexLoopRunner.AoCompletarVolta` -> `LoopDirector.FechouUmaVolta` -> `LoopDirector.AoCompletarVolta` (sem assinante) e `GeradorDeMoedas` / `MapaDePlataformas`.
- Morte: `LuccaStatus.AoMorrer` -> `LoopDirector.LuccaMorreu` -> `aoGameOver` (UnityEvent) -> `TelaDeMorte.Mostrar`.
- Dano de inimigo: `Inimigo.aoAtacar` -> `InimigoSpawner.RepassarAtaque` -> `aoInimigoAtacar` (UnityEvent) -> `LuccaStatus.ReceberDano`.

---

## 6. Fora do escopo ativo (legado)

**Regra:** nao usar scripts legados como referencia de arquitetura.

| Pasta / arquivo | Arquivos `.cs` | Conteudo |
|---|---|---|
| `Assets/Scripts/Loop` | 1 | LoopGenerator.cs |
| `Assets/Scripts/Player` | 4 | Flecha.cs, LoopMover.cs, PlayerAttack.cs, PlayerStatus.cs |
| `Assets/Scripts/Plataformas` | 14 | BlocoDestrutivel, GridAdjacencySystem, PlataformaAcampamentoBarbaro, PlataformaAlvo, PlataformaBase, PlataformaBlocos, PlataformaCabana, PlataformaCampoPacifico, PlataformaCoracao, PlataformaFlecha, PlataformaFlorestaProfunda, PlataformaLar, PlataformaMoeda, PlataformaTrampolim |
| `Assets/Scripts/Inimigo` | 2 | BossManager.cs, InimigoBase.cs |
| `Assets/Scripts/Manager` | 6 | GameManager, GameOverManager, MenuController, OptionsManager, PlacementManager, PlayerInteraction |
| `Assets/Scripts/UI` | 5 | UICardManager, UILevelUp, UIMoedas, UIPlatformCard, UIPlayerHealth |
| `Assets/Scripts/TesteDeTrigger.cs` | 1 | - |
| `Assets/testes shaders/` | 3 | CloudController.cs, Dappledlightsync.cs, Daynightcontroller.cs (mais shaders, materiais e `.lighting`) |

Total de `.cs` legados listados: 36.

Uso confirmado de legado em cenas que estao no fluxo ou no Build Settings:

| Script legado | GUID (do `.cs.meta`) | Uso verificado |
|---|---|---|
| `Daynightcontroller.cs` | `580e5a1fd815adb46afad1674c619890` | **HexPrototipo.unity**: 1 referencia (confirmado). |
| `CloudController.cs` | `9a0a043ab17cdaa4783d5b7e2aa0f2c6` | **HexPrototipo.unity**: 6 referencias (`m_Script`). Nao esperado pela lista do usuario. |
| `MenuController.cs` | `3bd365848c415534baa521cc23e783ba` | Menu, GameOver, Win (cenas do Build Settings). |
| `Dappledlightsync.cs` | `b4b9410d8ae736c43a4886ee3b2c4253` | Nenhuma cena do Build Settings usa este GUID. |

Nota: Testes.unity (legado, habilitada no Build Settings) usa varios scripts legados (LoopGenerator, GameManager, BossManager, UI*, PlayerInteraction etc.). Nenhum script Hex foi encontrado nela.

---

## 7. Dados (ScriptableObjects)

Fonte: `Assets/Dados/**/*.asset`. Todos os `m_Script` resolvem para um `.cs.meta` de `Assets/Scripts/Hex/`. **Nenhum `m_Script: {fileID: 0}` (quebrado) foi encontrado.**

| Asset | Script (arquivo) | `nomeExibido` | `onde` | `modelo` |
|---|---|---|---|---|
| `Dados/Lobo.asset` | InimigoDef.cs | (campo nao existe neste tipo) | (nao existe) | (nao existe) |
| `Dados/Morcego.asset` | InimigoDef.cs | (campo nao existe neste tipo) | (nao existe) | (nao existe) |
| `Dados/Plataformas/AreaDeTreino.asset` | PlataformaVizinhancaDef.cs | Area de Treino | 1 (ForaDoCaminho) | vazio (campo ausente) |
| `Dados/Plataformas/Basica.asset` | PlataformaSimplesDef.cs | Basica | 0 (NoCaminho) | vazio (campo ausente) |
| `Dados/Plataformas/Cabana.asset` | PlataformaVizinhancaDef.cs | Cabana | 1 (ForaDoCaminho) | vazio (campo ausente) |
| `Dados/Plataformas/CampoPacifico.asset` | PlataformaSimplesDef.cs | Campo Pacifico | 1 (ForaDoCaminho) | vazio (campo ausente) |
| `Dados/Plataformas/Coracao.asset` | PlataformaRecompensaDef.cs | Coracao | 0 (NoCaminho) | **definido** (`Coracao.prefab`, fileID 5633466962914129290) |
| `Dados/Plataformas/Flechas.asset` | PlataformaRecompensaDef.cs | Flechas | 0 (NoCaminho) | **definido** (`Flecha.prefab`, fileID 975229957496290958) |
| `Dados/Plataformas/Floresta.asset` | PlataformaFlorestaDef.cs | Floresta | 0 (NoCaminho) | vazio (campo ausente) |
| `Dados/Plataformas/IlhaAlta.asset` | PlataformaRelevoDef.cs | Ilha alta | 0 (NoCaminho) | **vazio** (`{fileID: 0}`) |
| `Dados/Plataformas/Moedas.asset` | PlataformaRecompensaDef.cs | Moedas | 0 (NoCaminho) | **definido** (`Moeda.prefab`, fileID 3222942014257985404) |
| `Dados/Plataformas/Mola.asset` | PlataformaMolaDef.cs | Mola | 0 (NoCaminho) | **definido** (`Mola.prefab`, fileID 8397554545510225194) |
| `Dados/Plataformas/Torre.asset` | PlataformaSimplesDef.cs | Torre | 1 (ForaDoCaminho) | vazio (campo ausente) |

Observacoes:
- Os 4 GUIDs de `modelo` resolvem para prefabs em `Assets/Modelos/HexPrototype/` (verificado).
- O enum `ColocacaoPermitida` nao tem valores explicitos. Por isso `onde` 0 = NoCaminho, 1 = ForaDoCaminho, 2 = Qualquer (ordem do enum, verificada).
- `IlhaAlta.asset` tem `modelo` vazio. Os assets sem o campo `modelo` ficam com valor nulo. Efeito em tempo de jogo nao verificado.

---

## 8. Configuracao de render

Fontes: `ProjectSettings/GraphicsSettings.asset`, `QualitySettings.asset`, `EditorSettings.asset`, `Assets/Settings/*.asset`.

### 8.1 Assets atribuidos

| Item | Asset | GUID | Onde foi lido |
|---|---|---|---|
| Pipeline padrao (`GraphicsSettings`) | `PC_RPAsset.asset` | `4b83569d67af61e458304325a23e5dfd` | GraphicsSettings.asset:40 |
| Global settings URP | `UniversalRenderPipelineGlobalSettings.asset` | `18dc0cd2c080841dea60987a38ce93fa` | GraphicsSettings.asset:61 |
| Qualidade atual (`m_CurrentQuality`) | indice 1 = **PC** | - | QualitySettings.asset:7 |
| Qualidade 0 (Mobile) | `Mobile_RPAsset.asset` | `5e6cbd92db86f4b18aec3ed561671858` | QualitySettings.asset:50 |
| Qualidade 1 (PC) | `PC_RPAsset.asset` | `4b83569d67af61e458304325a23e5dfd` | QualitySettings.asset:104 |

Qualidade ativa: **PC**, que usa `PC_RPAsset`.

### 8.2 Parametros dos assets URP

| Parametro | `PC_RPAsset` | `Mobile_RPAsset` |
|---|---|---|
| Renderer data | `PC_Renderer.asset` (`f288ae1f...`) | `Mobile_Renderer.asset` (`65bc7dbf...`) |
| `m_UseSRPBatcher` | 1 | 1 |
| `m_RequireDepthTexture` | 1 | 0 |
| `m_RequireOpaqueTexture` | 1 | 0 |
| `m_RenderScale` | 1 | 0.8 |
| `m_MSAA` (valor bruto) | 1 | 1 |
| `m_ShadowDistance` | 50 | 50 |
| Volume profile do asset | `SampleSceneProfile.asset` (`10fc4df2...`) | `SampleSceneProfile.asset` |

Nota: o valor bruto `m_MSAA: 1` esta registrado; a semantica exata (MSAA desligado ou nao) nao verificada.

### 8.3 Renderer features

| Renderer | Features | Detalhe |
|---|---|---|
| `PC_Renderer.asset` | 1 feature listada | Objeto `ScreenSpaceAmbientOcclusion` (SSAO) com `m_Enabled: 1` (linhas 61-71). |
| `Mobile_Renderer.asset` | nenhuma (`m_RendererFeatures: []`) | - |

`m_RenderingMode: 2` nos dois renderers (valor bruto; significado nao verificado).

### 8.4 Volumes

| Volume | Asset | Usado por | Overrides |
|---|---|---|---|
| `VolumeCena.asset` (`bb6e2e09...`) | `Assets/Settings/VolumeCena.asset` | HexPrototipo.unity:3095 (`sharedProfile`) | Bloom, Vignette, ShadowsMidtonesHighlights, ColorAdjustments |
| `DefaultVolumeProfile.asset` | `Assets/Settings/DefaultVolumeProfile.asset` | nao verificado | LiftGammaGain |

Acampamento.unity nao tem `sharedProfile` (nenhum Volume encontrado).

### 8.5 Enter Play Mode (`EditorSettings.asset`)

| Campo | Valor |
|---|---|
| `m_EnterPlayModeOptionsEnabled` | 1 |
| `m_EnterPlayModeOptions` | 0 |

Interpretacao padrao da Unity: valor 0 com a opcao habilitada mantem o reload de domain e o reload de cena ativos. A interpretacao do bit nao foi verificada no arquivo.

---

## 9. Save

Fonte: `Assets/Scripts/Hex/DadosDoJogador.cs`.

| Item | Valor |
|---|---|
| Nome do arquivo | `loopia_save.json` (linha 19) |
| Pasta | `Application.persistentDataPath` (linha 51) |
| Campos gravados | `moedas` (int), `recordeDeVoltas` (int) |
| Escrita | `JsonUtility.ToJson(dados, true)` com `File.WriteAllText` (linha 101) |
| Leitura | `JsonUtility.FromJson<Save>(File.ReadAllText(...))` (linha 118) |
| Classe singleton | `DadosDoJogador` (DontDestroyOnLoad; ordem -1000) |

Caminho real (Windows), derivado de `ProjectSettings/ProjectSettings.asset`:

- `companyName`: `DefaultCompany` (linha 15)
- `productName`: `GameJam13-06` (linha 16)
- Caminho derivado: `%USERPROFILE%\AppData\LocalLow\DefaultCompany\GameJam13-06\loopia_save.json`

**Derivado, nao verificado em execucao.** A pasta usa o nome `GameJam13-06` e nao `loopia`, porque o `productName` ainda e o do jam.

---

## 10. Observacoes de integridade

### 10.1 Assets da ultima commit sem `.meta` rastreado

`git show --stat 0b9513e` mostra 4 arquivos adicionados, todos sem `.meta` rastreado:

- `Assets/Modelos/MateusModelos/ArvoreGrande.fbx`
- `Assets/Modelos/MateusModelos/Morcegp.fbx` (nome gravado com grafia "Morcegp", como esta no repositorio)
- `Assets/Modelos/MateusTexturas/ArvoreGrande.png`
- `Assets/Modelos/MateusTexturas/Morcego.png`

Os `.meta` correspondentes existem **no disco, mas nao estao rastreados** (aparecem em `git status` como `??`). Por isso cada maquina gera um GUID diferente ao importar esses assets, e as referencias de prefabs e materiais podem quebrar entre maquinas ate os `.meta` serem commitados. Esta observacao e so uma explicacao geral do comportamento da Unity, e nao foi verificada no projeto.

### 10.2 Contagem geral (`git ls-files Assets`)

| Metrica | Valor |
|---|---|
| Entradas rastreadas em `Assets/` | 638 |
| Arquivos rastreados que nao sao `.meta` | 300 |
| Arquivos `.meta` rastreados | 338 |
| Arquivos rastreados **sem** `.meta` rastreado | **4** (lista acima; os 4 da ultima commit) |
| `.meta` rastreados sem asset correspondente no disco | 0 |

### 10.3 Itens nao rastreados (working tree, `git status` na abertura)

| Item | Situacao |
|---|---|
| `Assets/Modelos/MateusModelos/ArvoreGrande.fbx.meta` | `??` |
| `Assets/Modelos/MateusModelos/Morcegp.fbx.meta` | `??` |
| `Assets/Modelos/MateusTexturas/ArvoreGrande.png.meta` | `??` |
| `Assets/Modelos/MateusTexturas/Morcego.png.meta` | `??` |
| `Assets/Tests.meta` | `??` |
| `Assets/Tests/Editor.meta` | `??` (a pasta `Assets/Tests/Editor` esta vazia no disco) |

### 10.4 Divergencias e riscos encontrados

- `com.coplaydev.unity-mcp` esta em `#main` e nao fixado. O hash do lock e o commit atual.
- `com.unity.test-framework`: manifest 1.4.6, lock 1.6.0. `com.unity.multiplayer.center`: manifest 1.0.0, lock 1.0.1. Motivo nao verificado.
- `Assets/Tests` existe, mas so tem a pasta `Editor` vazia e o `.meta`. Nao ha testes escritos.
- `CloudController` (legado) tem 6 referencias em HexPrototipo.unity, que e cena ativa.
- Cenas legadas (Menu, Testes, GameOver, Win) estao habilitadas no Build Settings.
- Eventos sem assinante: `AoMudar` de `LuccaStatus` e `MaoDeCartas` e `MapaDePlataformas`, `HexPlayer.AoChegarNoDestino`, `LoopDirector.AoPerder`, `AoLiberarBoss`, `AoCompletarVolta`.
- Pacote `com.unity.postprocessing` convive com URP.
- GUIDs de scripts nao resolvidos dentro de `Assets/` existem nas cenas (ex.: 11 em HexPrototipo.unity). Provavelmente sao built-in ou de pacote, mas **nao verificado**.

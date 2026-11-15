# Pass B — Composição do fundo (2026-11-15)

Branch `sprint-1/pass-b-composicao`, empilhada sobre `sprint-1/pass-d1-nevoa` (PR #2).
O responsável aprovou a prévia v6 ("Seguir com a v6"). Este passe leva a v6 para a `HexPrototipo` **sem tocar ilhas, câmera, luz, NavMesh nem gameplay**.

## Problema (da auditoria e do plano)
A maquete `Cenario background` era escalada para cobrir a tela (`CenarioContinuo`). Árvores, torres, moinho e bandeira apareciam maiores que o dressing das ilhas. O rio azul saturado passava por baixo do anel. Ao meio-dia, o fundo perto do anel era verde-escuro e saturado. Custo: 490 588 tris na maquete mais o rio, que ainda tinha um `MeshCollider`.

## O que muda
- **`FundoDePaisagem`** (`Assets/Scripts/Hex/FundoDePaisagem.cs`), um componente na raiz da cena (fora de `Mundo`):
  - gera ao carregar a cena um vale em miniatura com o KayKit Forest Nature Pack (CC0);
  - **nada de malha é versionado**: tudo é gerado em memória (`HideFlags.DontSave`), de forma determinística (semente 20261115), e combinado por material e por bloco (81 renderers);
  - sem collider e sem sombra.
  - Geração: 44 ms medidos no Editor, ao aplicar.
- **Conceito**, com câmera ortográfica (a distância não diminui nada na tela):
  - **Escala de mundo menor:** árvores de 0,8–1,6 u, contra 1,8–2,4 u da árvore grande das ilhas.
  - **Materiais de paisagem dessaturados:** 3 tons de folhagem em mosaico e acentos ocre raros.
  - **Altura:** chão a y = −24, então a névoa linear do D1 dá perspectiva aérea.
  - **Detalhe por zona da tela:**
    - anel e faixa da mão de cartas mais calmos;
    - laterais com matas volumosas (copa, copa secundária e árvore emergente);
    - campo com bosquetes, moitas, capim, pedras, árvores secas e troncos caídos;
    - 4 lagoas;
    - rio no vale da faixa de cima e uma serra atrás dele.
- **`MateriaisDaPaisagem`** (`Assets/Editor`), menu `Loopia/Paisagem/...`: gera de forma reproduzível os 8 atlas/gradientes e os 9 materiais em `Assets/Modelos/Paisagem/`, liga **Read/Write** nos FBX do KayKit (o `CombineMeshes` em build exige) e preenche o componente.
- **Cena** (via Editor, commit isolado):
  - **Adicionado:** `Fundo de paisagem`.
  - **Removidos:** a instância `Cenario background`, `Rio de fundo` e as sobras inativas do plano (`QuadCloud`×6, `Plane`, `Cube`, `IlhaFlutuante`).
  - Diff semântico: 58 documentos YAML removidos, 3 novos, e só a lista de raízes alterada. Nenhum componente restante referenciava os removidos (verificado no Editor por `SerializedObject`).
- **Dependência nova:** `Assets/ThirdParty/KayKit_ForestNature_Free/` (105 FBX + atlas, 3,7 MB), licença **CC0** com `License.txt` junto.
  - É a mesma que a pesquisa de licenças classificou como segura para repositório público.
  - Os pacotes da Asset Store (EULA) e o Quaternius (licença ambígua) ficaram de fora.
- **Ferramenta:** `pontos.cs` passa a tratar `Fundo de paisagem` como fundo na métrica de legibilidade.
- `.gitignore`: as prévias locais (`Assets/Previa/`, ~80 MB de malhas geradas) não são versionadas.

## Aceite (matriz do D1: seeds 12345/7/2024 × 12/18/22 h × 1920×1080 e 2520×1080)
**Legibilidade:** contraste de luminância entre o topo das ilhas e o fundo imediato (`legibilidade.py`, pontos recalculados nesta cena).

| Hora | Mínimo na Bible | Antes (D1) | **Depois** | Saturação do fundo: antes → depois |
|---|---|---|---|---|
| 12h | ≥ 1,25 | 2,48–2,85 | **2,59–2,80** | 0,45–0,49 → **0,32–0,33** |
| 18h | ≥ 1,25 | 1,80–2,12 | **1,77–2,01** | 0,49–0,52 → **0,40–0,41** |
| 22h | ≥ 2,0 | 2,85–3,23 | **2,85–3,01** | 0,47–0,54 → **0,40–0,41** |

- O contraste de luminância se mantém (às 18h o mínimo cai 0,03; o máximo das 22h cai 0,22). O fundo fica bem menos saturado.
- Às 12h o matiz do fundo fica em ~140°–146°, contra ~102° do topo das ilhas: a separação passa a vir também da cor.

**Custo:** UnityStats do Editor, mesma sessão, seed 12345, 12h, 1920×1080. O "antes" é a cena da `HEAD` recriada localmente. São valores do Editor, não de build.

| | Antes | Depois | Δ |
|---|---|---|---|
| Batches | 615 | 416 | **−32 %** |
| SetPass | 73 | 64 | −12 % |
| Triângulos (frame) | 906 011 | 873 221 | −4 % |
| Vértices (frame) | 1 010 937 | 970 469 | −4 % |
| Shadow casters | 4 | 0 | — |
| Frametime do Editor | 3,56 ms | 3,55 ms | igual |

**Arquivos:**
- capturas em `depois/` (18 imagens + `manifest.json`);
- antes × depois em `antes_depois_h12/18/22.jpg`;
- escala de cinza em `depois/cinza/`;
- números em `depois_legibilidade.json`, `stats_antes/`, `stats_depois/` e `_raw_lighting.json`.
- O "antes" é `docs/art/pass-d1/depois/`.

## Testes
- **Novos:** 12 testes.
  - **Logic** (`FundoDePaisagemTests`):
    - gera no `OnEnable` dentro do orçamento (menos tris que a maquete antiga);
    - é determinístico, e uma semente diferente muda o resultado;
    - nada tem collider ou sombra, e nada é salvo;
    - desativar limpa as malhas;
    - sem configuração, avisa e não gera;
    - o terreno cobre a projeção da câmera (3 alvos × 4 zooms × 16:9/21:9).
  - **SceneStatic** (`FundoDePaisagemSceneTests`):
    - o componente está configurado e fora de `Mundo`;
    - o NavMesh coleta só os filhos de `Mundo`;
    - a maquete e as sobras saíram;
    - o arquivo da cena não guarda nada gerado;
    - os FBX têm Read/Write.
  - **PlayModeSmoke** (`FundoDePaisagemPlayModeTests`): o fundo é gerado em Play, o anel continua sendo gerado e o NavMesh não tem vértice na altura do vale.
- **Suíte inteira: 230/230.**
  - Logic + SceneStatic: 224/224, job `023d3911c13a445098ca77d4e5bd092d`.
  - PlayModeSmoke: 6/6, `TestResults.xml` 2026-11-15 05:01:57Z, de 8 a 18 s por caso.
- **Console:** 0 erros. O único aviso esperado vem do teste `SemConfiguracao_AvisaENaoGera`, ou de quando o componente é adicionado vazio.

## Pendente / riscos
- **Não verificado:** desempenho em **build** (geração em runtime no player, memória das malhas legíveis) e a HUD em resolução real por cima da faixa de baixo.
- **Ao meio-dia o relevo não aparece,** porque o sol do D1 vem a 90°. A profundidade vem da cor e da névoa. Ajustar isso é uma decisão de luz (D2).
- **Ficaram no projeto, sem uso pela cena:**
  - `CenarioContinuo`, `FundoContinuo` e `RioDeFundo`;
  - o prefab `Cenario background.prefab`, que ainda guarda o Volume latente.
  - Remover os scripts e o prefab fica para um PR de limpeza, fora do escopo.
- **Estações:** basta trocar os materiais de paisagem (mesma técnica testada no laboratório). Fora da Sprint 1.
- **Rollback:** `git revert` do commit da cena volta a maquete antiga. O componente, os assets e os testes podem ficar ou ser revertidos juntos.

# Auditoria dos assets de Lucca

A branch parte de `origin/sprint-3/gameplay-cena` em `ee46799`, com a integração do chefe do PR #8. O trabalho usa a worktree `loopia-lucca`. As outras worktrees permaneceram disponíveis durante a tarefa.

## Origem verificada

Comparamos os 46 arquivos do pacote da Sprint 2C no workspace com os arquivos em `Downloads/Loopia - Progresso de Rigging e Animacao/Sprint 2C`. Todos os SHA-256 coincidem. O Blender MCP abriu o .blend de produção no Blender 5.2.0 LTS. A fonte editável, os seis FBXs de ação, as duas texturas e os relatórios permanecem no pacote da Sprint 2C.

| Fonte | SHA-256 |
| --- | --- |
| Loopia-Animacao-2C.blend | `4a43045d1fefd38b43687e36ff6a3afc761cafafb6a289d1a27d1451996b6212` |
| Loopia-Game-Ready-2B.2.fbx | `d698ff7103980f389617ad616b252d5c791a87df5cab1ac4832cfe8afe9c0640` |

O .blend tem unidades métricas, armature sem transformação de objeto, 57 ossos, seis ações de produção a 60 fps e duas ações históricas. O rig usa deformação por armature, com no máximo quatro pesos por vértice nas partes inspecionadas. As ações são tocadas diretamente e não usam NLA. O rig exportado é FK, sem constraints ativas. A saia sai do quadril e a capa sai do tórax. O arco está em `Hand.L/Bow_Grip`. A corda tem `Bow_StringNock` e a leitura da mão direita usa `Socket_DrawHand`. Os pontos de saída e direção ficam sob `Bow_Grip`.

O parser FBX do Blender confirmou a correspondência de nomes e pais nos 57 ossos do modelo e dos seis FBXs. O modelo tem zero takes. Cada FBX de ação tem um take. As origens anteriores registram eixos -Z/Y, exportação sem leaf bones, inclusão dos sockets e bake dos clipes a cada frame. A avaliação no Blender encontrou deslocamento zero de `Root` nos frames dos seis clipes.

A validação BVH da fonte na Sprint 2C cobriu 2.427 amostras, incluindo transições. Ela é referência para as malhas aprovadas. A combinação de mira em runtime é avaliada separadamente nesta integração.

## Importação de produção

O projeto real usa Unity 6000.3.10f1 e URP 17.3. O modelo está em `Assets/Modelos/Lucca`. O importador preserva a configuração Humanoid de 21 ossos. O Avatar é válido e Humanoid. A cena usa 37.242 triângulos em duas malhas deformáveis, Corpo e Arco, com um atlas URP Lit por malha. Referência do modelo ~1,7 m antes da escala visual de 0,66. A altura visual de repouso ronda 1,13 unidades.

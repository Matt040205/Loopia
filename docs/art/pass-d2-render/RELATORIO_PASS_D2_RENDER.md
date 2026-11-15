# Pass D2 — Render e materiais (2026-11-15)

Branch `sprint-1/pass-d2-render`, empilhada sobre `sprint-1/pass-d2-luz` (PR #5).
É o restante do D2 que o responsável liberou, **sem o desempenho em build** ("pode prosseguir, menos para o desempenho em build").

## 1. Opaque Texture: desligada
- **Medição:** em runtime (Play, seed 12345), os únicos shaders ativos na cena são `Universal Render Pipeline/Lit` (395 materiais) e `Unlit` (2). Nenhum lê `_CameraOpaqueTexture`.
- O único Shader Graph de cena do projeto (`WaterLowPoly`, a água antiga) usa só *Scene Depth*, e saiu da cena no Pass B.
- **Efeito:** batches 531 → 525, SetPass 67 → 66 (uma cópia de tela a menos por frame). Nenhuma diferença visual.

## 2. SSAO: desligado
- **Configuração anterior:** `PC_Renderer` com SSAO ativo (fonte *Depth Normals*, intensidade 0,4, raio 0,3).
- **Custo:** no Forward, a fonte *Depth Normals* obriga um **prepass que desenha de novo toda a geometria opaca**. Mesmo build, mesma sessão do Editor, seed 12345, 12h:

  | | Batches | SetPass | Triângulos | Frametime do Editor | GPU (FrameTiming, Editor) |
  |---|---|---|---|---|---|
  | A: atual (Opaque + SSAO) | 531 | 67 | 912 661 | 3,65 ms | 0,556 ms |
  | B: sem Opaque Texture | 525 | 66 | 906 698 | 3,86 ms | 0,545 ms |
  | C: sem Opaque e sem SSAO | **290** | **59** | **455 133** | 3,46 ms | 0,481 ms |

- **Efeito visual:** imperceptível na câmera do jogo (`avaliacao/ssao_com_sem.jpg`).
  - A imagem de diferença, realçada, só acende o que se move entre as capturas: nuvens, moedas e o Lucca.
  - Recortes só de fundo têm luminância idêntica (94,9 / 156,2 vs 156,5 / 122,8).
  - O topo das ilhas varia 1 %.
- **Cuidado na medição:** o primeiro frame depois de desligar a feature em Play saiu sem fundo nem decoração (o renderer sendo reconstruído). Refiz a captura com aquecimento; os objetos estavam lá.
- **Ruído da métrica:** as nuvens se movem entre as capturas e entram no "fundo imediato" da métrica de legibilidade. Por isso há uma variação de cerca de ±0,25 na razão de uma captura isolada, sem relação com a mudança. Os números de aceite abaixo usam a matriz inteira.

## 3. Material compartilhado em `Inimigo` e `Projetil`
- **Antes:** cada inimigo sem prefab (morcego) e **cada flecha** criavam um `new Material`, que nunca era destruído. Era um vazamento a cada spawn e a cada tiro.
- **Agora:** `MateriaisCompartilhados.Lit(nome, cor, suavidade, metal)` guarda um material por combinação. As mesmas propriedades de antes foram mantidas: flecha com suavidade 0,5 e morcego com 0,1, então o visual não muda.
- **Medição em Play:** 140 s de jogo acelerado (4×), com morcegos nascendo e flechas sendo disparadas. O total de materiais ficou **estável em 1 391**, com 1 material de flecha e 2 compartilhados ao todo (`materiais_em_combate.json`).
- `GeradorDeMoedas`, `HexWorld`, `HexPlacementGrid` e `CartasUI` já criavam um material só (cache em campo) e não foram mexidos.

## Aceite
**Legibilidade** (matriz do D1; os pontos são os da cena atual):

| Hora | Antes (D2 luz) | Depois |
|---|---|---|
| 12h | 2,34–2,56 | **2,34–2,56** |
| 18h | 1,77–2,01 | **1,77–2,01** |
| 22h | 2,84–3,01 | **2,84–3,01** |

**Custo** (Editor, a mesma medição do resto da sprint):
- batches **531 → 287 (−46 %)**;
- triângulos **912 mil → 451 mil (−51 %)**;
- SetPass 67 → 59.

O `stats_depois` foi medido com a configuração final salva. **Não é medição de build.**

## Arquivos alterados
- `Assets/Settings/PC_RPAsset.asset`: `m_RequireOpaqueTexture: 0`.
  - Ao salvar, a Unity também reformatou algumas linhas longas de YAML (mesmos valores).
- `Assets/Settings/PC_Renderer.asset`: SSAO com `m_Active: 0`.
  - Ao salvar, a Unity **atualizou o asset do URP 17** (`m_AssetVersion` 2 → 3, campos novos com valores padrão).
  - Também tirou as referências de textura e shader da feature de SSAO, que o URP 17 resolve pelos recursos globais.
  - Para religar, basta marcar a feature como ativa.
- Os ajustes foram feitos pelo Editor (não à mão). Os assets "Mobile" não foram mexidos, porque a cena roda na qualidade "PC".

## Testes
- **Novos:** 4 testes.
  - **Logic** (`MateriaisCompartilhadosTests`): a mesma cor devolve o mesmo material em 50 spawns, sem crescer o cache; cor ou suavidade diferente devolve outro material; as propriedades estão certas.
  - **SceneStatic** (`RenderDoPCTests`): o pipeline do PC está sem Opaque Texture e sem SSAO, e nenhum shader da cena usa `SceneColorNode`.
- **Suíte: 263/263.**
  - Logic + SceneStatic: 256/256, job `577ff50527464f51b74c0453bdd7703c`.
  - PlayModeSmoke: 7/7, `TestResults.xml` 2026-11-15 13:51Z, 6–16 s por caso, incluindo as voltas com inimigos e flechas.
  - Nenhum teste existente foi alterado.
- **Console:** 0 erros.

## Pendente
- **Desempenho em build** (fps, CPU, GPU, memória, overdraw das nuvens): fora do escopo, a pedido do responsável.
- **Rollback:** `git revert`. Ou, sem código: religar a Opaque Texture no `PC_RPAsset` e/ou ativar a feature de SSAO no `PC_Renderer`.

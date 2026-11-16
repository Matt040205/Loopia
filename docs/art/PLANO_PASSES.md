# Plano técnico — passes visuais A–D

Base: `AUDITORIA_VISUAL.md` (medidas reais) e `SCENE_BIBLE.md` (alvos). Um passe por vez, cada um em branch e PR
próprios, com capturas antes/depois feitas com `tools/visual-baseline/run_baseline.py` na **mesma matriz** (seeds
12345/7/2024 × 12/18/22 h × 16:9/21:9) e a suíte da Sprint 0 completa.

Ordem: **A → D (só o item de névoa/legibilidade) → B → C → resto de D.** Justificativa: A corrige o que é erro de
escala; a névoa uniforme é o segundo maior problema de leitura e é barata; B e C dependem de A para ter escala certa;
o resto de D (paleta, pós, desempenho) fecha a cena.

---

## Pass A — Scale & Camera (P0) · branch `sprint-1/pass-a-escala-camera`

| # | Mudança | Arquivos | Tipo |
|---|---|---|---|
| A0 | Captura "antes" do lobo e do morcego em jogo (confirma o lobo 7,65 u visualmente) | `tools/visual-baseline/` (novo comando `enemies`) | ferramenta |
| A1 | `VisualDeIlha.LimitesLocais` também considera `SkinnedMeshRenderer` (`sharedMesh.bounds`); parâmetro de eixo de medida (altura / comprimento / maior eixo, padrão = atual) | `Assets/Scripts/Hex/VisualDeIlha.cs` | código |
| A2 | `ArtScaleProfile : ScriptableObject` (arquivo próprio) com categorias da Bible (alturas em L, eixo de medida, regra de posição "lado oposto à câmera") e mapeamento prefab → categoria | `Assets/Scripts/Hex/ArtScaleProfile.cs` (novo) | código |
| A3 | `DecoracaoDeIlha` usa o perfil quando atribuído; sem perfil, comportamento idêntico ao atual (fallback testado). Mantém `PosicaoLivre`, seed por coordenada e corredores | `Assets/Scripts/Hex/DecoracaoDeIlha.cs` | código |
| A4 | Asset do perfil com os 6 modelos atuais categorizados; `InimigoDef.escala` de Lobo/Morcego ajustada à tabela (só visual: collider da primitiva é destruído, combate não muda) | `Assets/Dados/Arte/EscalaDeArte.asset` (novo), `Assets/Dados/Lobo.asset`, `Morcego.asset` | dados (via Editor) |
| A5 | `HexIsoCamera` enquadra pelo **topo das ilhas** (malha hex) + folga fixa para a base pendurada, ignorando arte; resultado independe de decoração/variante | `Assets/Scripts/Hex/HexIsoCamera.cs` | código |
| A6 | Ligação do perfil no `DecoracaoDeIlha` da cena e, se necessário, `margem` da câmera — **únicas** mudanças de cena, feitas pelo MCP (`SerializedObject`), em commit isolado | `Assets/Scenes/HexPrototipo.unity` | cena (via Editor) |
| A7 | Validador de Editor: menu `Loopia/Validar escala de arte` lista prefabs do perfil fora da faixa, skinned sem bounds, pivô fora da base, colliders ativos | `Assets/Editor/ValidadorDeEscala.cs` (novo) | ferramenta |
| A8 | Testes novos + capturas depois + relatório | `Assets/Tests/Editor/*`, `docs/art/pass-a/` | testes/docs |

Fora do escopo de A: modelos novos do Lucca (outro agente), `ArvoreGrande.fbx` (aguarda `.meta` commitado pelo dono),
fundo/rio (B), vegetação nova (C), luz/névoa (D), tint MPB das decorações.

**Testes do Pass A**
- EditMode: bounds de prefab só-skinned ≠ zero; escala por categoria dentro da faixa (±5 %) para os 6 modelos;
  fallback sem perfil = valores atuais; 11 seeds: nenhuma decoração invade centro/corredor, ≥ 1 decoração por ilha;
  ortho idêntico com e sem decorações e com perfil diferente; ortho ≤ alvo por seed.
- SceneStatic: perfil atribuído; todas as entradas do perfil com prefab válido.
- PlayModeSmoke (existentes, sem mudança): NavMesh, `Pulos == Loop.Count`, volta real, boss, game over.
  Novo: toda decoração tem `NavMeshModifier.ignoreFromBuild` e nenhum collider ativo.

**Critérios de aceite (1920×1080)**
| Métrica | Hoje | Alvo |
|---|---|---|
| Lucca (seeds 12345 / 2024) | 33 / 35 px | ≥ 40 px |
| Lucca (seed 7, maior anel) | 31 px | ≥ 36 px |
| Topo da ilha | ≈ 95 px | ≥ 115 px |
| Árvore grande | 20 px máx. | ≥ 45 px, sem cobrir o Lucca no caminho |
| Lobo / morcego | 312 / 14 px | 30–50 / ≥ 20 px |
| Ortho com × sem decoração | 0 % | 0 % (mantido por construção) |
| Anel inteiro visível 16:9 e 21:9 | sim | sim, inclusive sem ficar sob a mão de cartas (validação manual com HUD) |
| Suíte Sprint 0 + novos | 119/119 | 100 % |
| Console | 0 erros | 0 erros |
| Batches / tris (Editor, indicativo) | 625 / 909 k | ≤ +10 % |

---

## Pass D1 — Névoa e legibilidade (P1, antecipado) · branch `sprint-1/pass-d1-nevoa`
- Dono único de `RenderSettings`: `Daynightcontroller` continua sendo o único escritor, ganhando parâmetros em vez de
  valores fixos (sem novo controlador ainda).
- Névoa **linear com início além da profundidade do anel** (medido: ilhas a 49–83 u da câmera; maquete 90–180 u): ilhas
  sem névoa, fundo com perspectiva aérea. Alternativa a avaliar: manter exponencial e reduzir densidade.
- Noite: ambient mínimo para o topo das ilhas ≥ 2× o fundo; corrigir sol abaixo do horizonte depois das 18h e a subida
  brusca 5h17–6h.
- Arquivos: `Assets/testes shaders/Daynightcontroller.cs`, valores via Editor na cena. Aceite: teste de escala de cinza
  (anel identificável nas 3 horas), sem regressão de batches.

## Pass B — Scene Composition (P1) · `sprint-1/pass-b-composicao`
- `CenarioContinuo`: escala de **mundo fixa** para a maquete (deixar de cobrir a tela por escala), ancorada ao centro
  do anel, mais baixa; árvores do fundo menores na tela que as árvores das ilhas. Preencher bordas com camada de
  nuvens/névoa em vez de esticar a maquete.
- Rio: decidir entre (a) afastá-lo para fora da projeção do anel com margens visíveis ou (b) mantê-lo bem abaixo, mais
  estreito e esmaecido pela névoa. Sem malha nova se a (b) resolver.
- Limpar sobras inativas (`QuadCloud`×6, `Plane`, `Cube`, `IlhaFlutuante`) e o Volume latente do prefab de fundo —
  pelo Editor.
- Arquivos: `CenarioContinuo.cs`, `FundoContinuo.cs`, `RioDeFundo.cs`, prefab `Cenario background` (via Editor), cena.

## Pass C — Environment Dressing (P1) · `sprint-1/pass-c-dressing`
- C1 (legibilidade de gameplay primeiro): marcadores simples para as 7 cartas sem modelo, compostos de primitivas e
  props existentes na escala da Bible (torre, cabana, campo, alvo, floresta, platô, básica).
- C2: vegetação baixa (arbustos, tufos de capim/flores) com materiais compartilhados e no máximo 12 objetos por ilha;
  posições pela mesma regra de corredores; categorias do `ArtScaleProfile`.
- C3: variação por ilha (densidade, mistura) parametrizada no perfil, sem sistema procedural novo.

## Pass D2 — Lighting & Validation (P1) · `sprint-1/pass-d2-luz`
- Paleta por hora conforme Bible; avaliar tonemapping Neutral/ACES com HDR + Bloom (antes/depois).
- Medir e, se não houver consumidor, desligar Opaque Texture; reavaliar SSAO.
- Corrigir `new Material` por spawn em `Inimigo`/`Projetil` (material compartilhado).
- Baseline de desempenho **em build** 1080p (16:9 e 21:9): fps, CPU main, GPU, batches, overdraw (nuvens), memória.

---

## Riscos e rollback
| Risco | Passe | Mitigação | Rollback |
|---|---|---|---|
| Decoração maior oclui o Lucca/caminho | A, C | árvores grandes só nos vértices do lado oposto à câmera; teste de oclusão por raycast câmera→Lucca em 11 seeds | `git revert`; ou remover o perfil da cena (fallback = comportamento atual) |
| Zoom maior corta o anel ou entra sob a HUD | A | folga calculada por aspecto; validação manual com HUD em 16:9/21:9 | reverter `HexIsoCamera.cs` (1 arquivo) |
| Rebake/NavMesh afetado por arte | A, C | arte só via `VisualDeIlha.Criar` (ignore + colliders off); teste dedicado | revert |
| Edição de cena conflitar com outro trabalho | A, B | commit de cena isolado, feito pelo MCP; `git fetch` antes; ninguém mais toca a cena no período | revert do commit de cena + reabrir cena |
| Lobo com bounds corrigidos fica pequeno/errado | A | A0 captura antes; escala definida pela altura alvo | reverter `VisualDeIlha.cs` |
| Névoa linear mal ajustada esconde o fundo inteiro | D1 | capturas nas 3 horas; parâmetros expostos | reverter script + valores |
| Custo de vegetação | C | orçamento por ilha; materiais compartilhados; medir em build | desligar categoria no perfil |
| Modelos do Lucca chegando no meio | todos | contrato de escala na Bible; não integrar até liberação | — |

Regra geral: cada passe = 1 PR com checklist do `AGENTS.md` §7, capturas antes/depois (mesma matriz), tabela de métricas
acima preenchida com valores medidos e `docs/handoff/ESTADO_ATUAL.md` atualizado.

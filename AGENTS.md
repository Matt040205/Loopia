# AGENTS.md — contrato de trabalho para agentes no Loopia

Leia este arquivo inteiro antes de qualquer alteração. Ele vale para qualquer agente (Claude,
GPT/Codex, outros) e para humanos. Se algo aqui conflitar com um pedido, pare e pergunte.

## 1. O que é o projeto
Roguelite de estratégia e construção de ilhas em Unity 6000.3.10f1 (URP 17.3). O herói (Lucca)
percorre sozinho um anel hexagonal fechado; o jogador manipula o mundo com cartas (plataformas),
adjacência, risco e recursos. Ciclo-alvo: acampamento → 4 voltas → boss → fim da run; morte antes
do boss = derrota. **Não** adicionar controle direto do herói nem mundo aberto. Não ampliar escopo
(estações, biomas, dezenas de cartas) antes do ciclo completo funcionar.

Trabalho de faculdade: o avaliador procura, visíveis no Inspector, **player em NavMesh, ataque dos
morcegos via UnityEvents, cartas em ScriptableObject, moedas num Singleton e save em JSON**. Não
remover nem esconder essas técnicas.

## 2. Ordem de leitura (fonte da verdade)
1. `docs/handoff/ESTADO_ATUAL.md` — onde o trabalho parou, o que está pendente, em que branch.
2. Este arquivo.
3. O documento da sprint corrente em `docs/sprints/`.
4. `docs/sprint-0/` — baseline, auditoria validada, testes, riscos/rollback.
5. `README.md` / `README.pt-BR.md` — visão técnica.
`docs/plans/analise-requisitos-loopia.md` é **histórico** (protótipo quadrado antigo): não usar como
diagnóstico atual.

## 3. Arquitetura
- **Ativo:** `Assets/Scripts/Hex/` (namespace `Loopia.Hex`), cenas `Acampamento.unity` (build 0) e
  `HexPrototipo.unity` (build 1).
- **Legado, não usar como referência nem misturar:** `Assets/Scripts/{Loop,Player,Plataformas,
  Inimigo,Manager,UI}`, `TesteDeTrigger.cs`, cenas Menu/Testes/GameOver/Win.
- **Exceção:** `Assets/testes shaders/Daynightcontroller.cs` e `CloudController.cs` (namespace
  global) estão ativos na `HexPrototipo`; o primeiro escreve `RenderSettings` (fog/ambient) todo frame.
- Não há `.asmdef` no projeto: gameplay compila em `Assembly-CSharp`; testes em
  `Assembly-CSharp-Editor` (`Assets/Tests/Editor/`). Introduzir asmdef exige PR próprio: as cenas
  guardam UnityEvents com `m_TargetAssemblyTypeName: Loopia.Hex.X, Assembly-CSharp`, e o
  `Daynightcontroller` (global) ficaria fora do alcance.

Pontos de autoridade (não duplicar):
| Responsabilidade | Dono |
|---|---|
| Regra de colocação de carta | `MapaDePlataformas.PodeColocar` (UI só pergunta) |
| Geração/validação do anel e miolo do boss | `HexLoopGenerator` (`CicloEhValido`) |
| Estado das casas (ilha/livre/bloqueada) | `HexWorld` |
| NavMesh e links de pulo | `NavegacaoDoMundo.Reconstruir` (só quando topologia/altura muda) |
| Contagem de voltas, escala dos inimigos, gatilho do boss | `HexLoopRunner` → `LoopDirector` |
| Persistência entre runs | `DadosDoJogador` (singleton, JSON em `persistentDataPath/loopia_save.json`) |
| Fog/ambient/sol | hoje `Daynightcontroller`; um único dono deve permanecer |

## 4. Regras invioláveis
1. **Cenas YAML:** não editar `.unity` à mão nem "às cegas". Mudanças de cena: pelo Unity (MCP
   `execute_code`/`manage_scene`), uma pessoa/agente por vez, em PR isolado. Nunca dois PRs abertos
   mexendo em `HexPrototipo.unity`. Em conflito de cena: ficar com a versão do outro e reaplicar a
   sua mudança pelo Editor.
2. **GUIDs e `.meta`:** todo asset novo vai com seu `.meta` no mesmo commit. Nunca apagar/regerar
   `.meta` existente. Não renomear/mover scripts referenciados em cena sem o `.meta` junto.
3. **ScriptableObject:** cada classe em um `.cs` com o mesmo nome. Antes de commitar `.asset`,
   conferir que `m_Script` não é `{fileID: 0}`.
4. **NavMesh:** clima, estação e decoração nunca disparam rebake. Arte é filho visual com
   `NavMeshModifier.ignoreFromBuild` e colliders desligados (`VisualDeIlha.Criar`).
5. **Materiais:** não usar `renderer.material` em loops/transições (gera instâncias). Preferir
   `sharedMaterial`/`MaterialPropertyBlock` medido.
6. **Sem dependência nova sem justificativa escrita no PR.** O pacote `com.coplaydev.unity-mcp`
   aponta para `#main` (não fixado): não atualizar sem combinar.
7. **Não refatorar fora do escopo da sprint.** Mudança pequena, um assunto por PR.
8. **Evidência:** só declarar teste/compilação/build aprovado com saída real do Editor (job do
   Test Runner, console). Sem execução → registrar como **PENDENTE**.

## 5. Unity Editor via MCP
O bridge responde em `127.0.0.1:6400` com o Editor aberto. Cliente e pegadinhas:
`tools/unity-mcp/README.md`. Só um agente por vez dirige o Editor. Antes de `git checkout`/rebase
que reescreva a cena aberta, fechar a cena no Editor.

## 6. Testes
- Local: `Assets/Tests/Editor/` (Assembly-CSharp-Editor). Categorias: `Logic` (puro, rápido),
  `SceneStatic` (abre cena sem Play), `PlayModeSmoke` (entra em Play a partir de `[UnityTest]`),
  `KnownIssue` (comportamento atual errado documentado; não "consertar" o teste).
- Rodar: Window > General > Test Runner > EditMode, ou via MCP `run_tests` + `get_test_job`.
- Testes **não** podem salvar cena nem alterar assets; quem toca `DadosDoJogador` faz backup e
  restaura `loopia_save.json`.
- Relatório e inventário: `docs/sprint-0/TEST_REPORT.md`.

## 7. Fluxo de trabalho por passo (Definition of Done)
1. Branch a partir de `main` atualizada: `sprint-N/<assunto-curto>` (ex.: `sprint-1/art-scale`).
2. Ler `ESTADO_ATUAL.md` e a sprint; listar arquivos que vai tocar antes de tocar.
3. Implementar o mínimo. Sem mudar cena/material/shader se a sprint não pedir.
4. Rodar a suíte inteira (`Logic` + `SceneStatic` + `PlayModeSmoke`) e o console sem erros novos.
5. Checklist do PR (copiar para a descrição):
   - [ ] Escopo: só os arquivos listados; `git diff --stat` revisado
   - [ ] `.meta` de todo arquivo novo incluído; nenhum `m_Script: {fileID: 0}`
   - [ ] Testes: job id + total/passou/falhou (ou PENDENTE com motivo)
   - [ ] Console: sem erros novos (colar contagem)
   - [ ] Cena(s) tocadas: nenhuma / quais e por quê
   - [ ] Captura antes/depois quando houver mudança visual
   - [ ] Plano de rollback (normalmente `git revert <sha>`)
6. **Handoff:** atualizar `docs/handoff/ESTADO_ATUAL.md` (modelo em
   `docs/handoff/MODELO_HANDOFF.md`) e o registro da sprint. Outro agente deve conseguir continuar
   só lendo esses arquivos.

## 8. Papéis
- **Humano (responsável):** direção de produto/arte, cortes de escopo, roda Unity e builds, aprova PR.
- **GPT (orquestrador/revisor):** define sprints e critérios de aceite, distribui passos, revisa PRs.
- **Claude Code (analista de qualidade + implementação):** implementa passos pequenos em branches,
  roda testes pelo MCP, revisa entregas de subagentes e registra evidências.

## 9. Convenções de código
- Identificadores e comentários em português sem acento, como no código existente.
- Referências entre componentes: campo público opcional + `FindFirstObjectByType` como fallback
  (padrão atual). Eventos C# (`event Action`) para sistemas; `UnityEvent` quando a técnica precisa
  aparecer no Inspector.
- Coordenadas: axial (q, r), pointy-top; `HexLayout` converte.

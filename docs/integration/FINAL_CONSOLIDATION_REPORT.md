# Loopia — relatório final de consolidação (2026-11-16)

Uma única versão com: MVP integrado (G1 + arte + Lucca do Codex + G2), estabilização (PR #12), cartas do Codex
(PR #13) e revisão de interface (`sprint-4/revisao-interface`), com a interface aprovada instalada nas
cenas oficiais.

## 1. Git

| Item | Valor |
|---|---|
| Branch consolidada | `integration/loopia-unified` |
| Ponto de partida | `fix/mvp-stabilization` @ `c0b56db` (PR #12) |
| Incorporada | `sprint-4/revisao-interface` @ `2d36c27` (contém `feature/card-visual-system` @ `850aabe`, PR #13) |
| Merge | `18b2da9` — `git merge --no-ff`, **sem conflitos** (`ESTADO_ATUAL.md` combinado automaticamente) |
| PR de consolidação | ver §8 |
| SHA final | ver §8 (o commit deste relatório) |

SHAs de referência confirmados após `git fetch --all --prune` (todos iguais aos do pedido):
`integration/mvp-20261117` `f49cee1`, `fix/mvp-stabilization` `c0b56db`, `feature/card-visual-system` `850aabe`,
`sprint-4/revisao-interface` `2d36c27`. `main` = `0b9513e`, ancestral de tudo (a promoção é fast-forward).

**Ancestralidade provada** (`git merge-base --is-ancestor <sha> HEAD`): `f49cee1`, `850aabe`, `2d36c27`, `c0b56db`,
`0be4374`, os nove commits da estabilização posteriores ao ancestral comum com a prévia (`a7a5c56` … `c0b56db`),
os commits de recuperação do Codex `9a70328` / `9362e9f` e o merge G2 `1d5407d`. As branches dos PRs #1–#13,
`sprint-0/preparacao-tecnica`, `sprint-1/auditoria-visual` e as locais `feature/lucca-character-integration`,
`feature/lucca-locomotion-2c2`, `sprint-4/legibilidade-tela` e `prototipo-hex` também estão na ancestralidade.
O PR #13 **não** foi mesclado de novo: entrou pela ancestralidade de `sprint-4/revisao-interface`.

Commits da consolidação (sobre `c0b56db`):

| SHA | Assunto |
|---|---|
| `18b2da9` | merge da revisão de interface (cartas do Codex + HUD da jornada) |
| `5969cfc` | `.meta` de `ArvoreGrande`/`Morcegp` (os modelos entraram na `main` sem `.meta`) |
| `c461989` | gerador instala a interface nas cenas oficiais; prévia só troca a navegação; F3 do acampamento no runtime |
| `f555883` | cenas oficiais com a interface (aplicada pelo Editor, sem editar YAML) |
| `d05dfd8` | testes de integração da interface; arraste usa a HUD da cena |
| (este) | relatório, evidências e handoff |

**Arquivos de atenção especial** — nenhum teve conflito. `CartasUI`, `HudDeCartas`, `TooltipDeCarta`,
`HexIsoCamera` vêm da revisão de interface; `LuccaAnimationBridge`, `LoopDirector`, `TelaDeMorte`, `LuccaStatus`
vêm da estabilização (as duas frentes não tocaram os mesmos trechos depois do ancestral `0be4374`).
`HexPrototipo.unity` e `Acampamento.unity` só mudaram no `f555883`, pelo Editor.

**`.meta` de ArvoreGrande/Morcegp.** Cada worktree gerou GUIDs próprios para esses 4 assets (sem `.meta` na `main`).
Ficou o conjunto da `loopia-mvp` (igual ao de `loopia-g2` e `loopia-gameplay`). Nenhuma cena, prefab ou material
referencia esses GUIDs. O conjunto alternativo commitado localmente em `loopia` (`08d4a78`, nunca publicado) não foi
usado e está no bundle do backup.

**Trabalho local não publicado encontrado** (preservado, não incorporado por não ser pertinente):
- `loopia` (`sprint-1/pass-d2-render`): `ea12ae7` (cópia de recuperação da Unity) e `08d4a78` (GUIDs alternativos) —
  bundle `worktrees/loopia/sprint-1-pass-d2-render.bundle`.
- `loopia-mvp`: `Assets/_Recovery/0.unity` (cena de recuperação de uma queda da Unity, não versionada) — movida para o
  backup; ao abrir, a Unity recriou outra cópia (dos `Temp/__Backupscenes`), também movida para o backup.
- Em todas as worktrees: `.vscode/settings.json` (nome da `.sln`), `Dressing_Vegetacao.mat` / `BarracaArmada.mat`
  (a Unity preenche `_MainTex` a partir de `_BaseMap`), `ProjectSettings/*` regravados pelo Editor/build. Ruído de
  Editor; diffs guardados no backup.

## 2. Backup (checkpoint anterior à consolidação)

`C:\Users\Sitr3n\Documents\Loopia_Backups\consolidacao-20261116\` (fora de `Assets` e de qualquer worktree):
- `MANIFESTO_GIT.txt` — todas as refs locais/remotas com SHA e `git worktree list`.
- `worktrees/<nome>/` — HEAD, status, `diff_unstaged.patch`, `diff_staged.patch`, commits não publicados,
  `untracked.tar` (sem a prévia) e `previa_gerada.tar` (cenas da prévia em `Assets/Previa`) das 8 worktrees.
- Bundles: commits locais de `sprint-1/pass-d2-render`, `prototipo-hex` e branches locais sem remoto.
- `recovery-loopia-mvp/` — as duas cenas de recuperação (`514086a7…` e `8f87e949…`).
- `saves/` — cópia de `DefaultCompany\Loopia`, `GameJam13-06` e `Loopia QA Cartas` com `SHA256.txt`.
- `builds/REFERENCIAS.txt` — builds existentes. O backup do Codex (`Loopia_Recovery_20261115`) não foi tocado.

## 3. Unity

| Item | Estado |
|---|---|
| Editor principal | Unity 6000.3.10f1, **único processo** aberto (`-projectPath ...\loopia-mvp -loopia-perfil qa-consolidacao`) |
| Worktree principal | `C:\Users\Sitr3n\Documents\Github\loopia-mvp` (branch `integration/loopia-unified`; após o merge, `main`) |
| Outros Editors | nenhum estava aberto no início (só `Temp/UnityLockfile` órfãos em `loopia-mvp` e `loopia-gameplay`) |
| Cenas oficiais (build) | `Assets/Scenes/Acampamento.unity` (0), `Assets/Scenes/HexPrototipo.unity` (1) |
| Prefab instalado | `Assets/Arte/Cartas/HUD de Cartas.prefab` na `HexPrototipo`, `origem` = `CartasUI` da cena |
| EventSystem | 1 por cena, só `InputSystemUIInputModule` |
| Canvas `HexPrototipo` | `Interface da jornada` (5, sem raycaster), `HUD de Cartas` (6, sem raycaster; o `CartasUI` lê o ponteiro), `HUD`/resultado (10, `GraphicRaycaster`). Todos Overlay, `ScaleWithScreenSize` 1920×1080, match 1 |
| Canvas `Acampamento` | `Acampamento` (10, raycaster) |
| Botões | `Nova jornada` → `TelaDoAcampamento.NovaJornada`; `Volte ao acampamento` → `TelaDeMorte.VoltarAoAcampamento`; `aoGameOver` → `TelaDeMorte.Mostrar`; `aoVencer` → `TelaDeMorte.MostrarVitoria` |
| Editor-only | `NavegacaoDaPreviaHud` (`#if UNITY_EDITOR`) não aparece em nenhuma cena do build (teste) |
| Fallback IMGUI | mantido: o `CartasUI` desenha a mão IMGUI se a HUD não assumir (catálogo incompleto, HUD desligada) |

**Como a interface entrou nas cenas.** O gerador da prévia (`Assets/Editor/MontarPreviaDaInterface.cs`) ganhou
`Loopia > Interface > Aplicar nas cenas oficiais`, que monta a mesma interface aprovada nas cenas de `Assets/Scenes`
sem trocar os botões (ficam nos métodos runtime) e recusa rodar duas vezes. `Loopia > Prévia da interface >
Recriar cópias locais` continua como ferramenta: copia as cenas oficiais para `Assets/Previa/Hud` e só troca os
botões por `NavegacaoDaPreviaHud`. O F3 do caminho do save no acampamento, que antes só existia no componente
Editor-only da prévia, virou `TelaDoAcampamento.arquivoSoComF3` (runtime).

**Resultado.** O `ResultadoDaJornadaVisual` só reescreve textos no `OnEnable` do painel; o fade de 0,35 s continua
sendo o único (o da `TelaDeMorte`, `CanvasGroup` criado em runtime). Verificado por teste e na build.

**Saves.**
| Arquivo | SHA-256 antes | Depois | Observação |
|---|---|---|---|
| `Loopia\loopia_save.json` (pessoal) | `76e429e4…` (210 moedas, recorde 6, 1 vitória) | igual | **já não era** a referência `3d27854e…` (209) da memória: foi alterado antes desta sessão (provavelmente por um Editor/build sem perfil). Não restaurado, como pedido |
| `GameJam13-06\loopia_save.json` | `c11fb73d…` (212) | igual | save do produto antigo, também divergente da referência |
| `loopia_save_qa.json` | `ea838f55…` | igual | perfil QA da estabilização |
| `loopia_save_hud-preview.json` | `eb44449f…` | igual | perfil da prévia |
| `Loopia QA Cartas\loopia_save.json` | `e4826e7c…` | igual | perfil do Codex |
| `loopia_save_qa-consolidacao.json` | — | novo (115 moedas, 4 vitórias) | perfil desta consolidação (Editor e build) |

## 4. QA

### Testes (Editor `loopia-mvp`, MCP `run_tests`)
| Execução | Total | Passou | Falhou | Evidência |
|---|---|---|---|---|
| Logic + SceneStatic | 404 | 404 | 0 | job `ea19f97f…` |
| PlayModeSmoke | 28 | 28 | 0 | `evidencias/TestResults_playmodesmoke_consolidacao.xml` |
| PlayModeMedicao | 4 | 4 | 0 | `evidencias/TestResults_playmodemedicao_consolidacao.xml` |
| **Suíte inteira, sem filtro** | **436** | **436** | **0** | `evidencias/TestResults_suite_completa_consolidacao.xml` |

Referências anteriores: estabilização 427/427, revisão de interface 418/418 (conjuntos sobrepostos; não se somam).
Nenhum teste removido ou ignorado. Novos: `InterfaceIntegradaTests` (5 SceneStatic + 2 PlayModeSmoke).
Adaptado: `CartasVisuaisTests.Arraste_*` passou a usar a HUD já instalada na cena (só instancia o prefab se faltar);
as asserções não mudaram. Console do Editor: 0 erros (2 avisos de TMP sobre reticências na LiberationSans, já
existentes). Saves pessoais intactos após as suítes.

Cobertura dos itens pedidos: HUD presente na cena oficial e ligada ao `CartasUI`; catálogo completo (14 cartas,
teste do Codex); ausência de interfaces e EventSystems duplicados; botões nos métodos runtime; nenhuma referência a
`NavegacaoDaPreviaHud` nas cenas do build; cartas G2 no baralho (`G2SceneTests`); Mirante (`MirantePlayModeTests`,
5); mira e locomoção da Lucca (`LuccaIntegracaoTests`, `LuccaMiraTests`, `ApoioDosPesMedicaoTests`); vitória e
derrota com os painéis novos; save (`SaveIsolado`, `DadosDoJogador*`).

### Build Windows
| Build | Pasta | Resultado |
|---|---|---|
| Release x64 | `Documents\Loopia_Builds\loopia-unified-d05dfd8-release\` | Succeeded, 0 erros, 0 avisos, 149,8 MB |
| Development x64 | `Documents\Loopia_Builds\loopia-unified-d05dfd8-dev\` | Succeeded, 0 erros, 213,4 MB |

Builds anteriores intactas. Cenas: Acampamento, HexPrototipo.

### Jornada completa no Editor consolidado
`HexPrototipo` em Play no Editor `loopia-mvp`, sem atalhos (Lucca anda e atira sozinho, sem cartas colocadas):
voltas 0→4 em ~2 min, confronto com o chefe, **derrota diante do chefe**, resultado novo, botão do resultado →
Acampamento. Capturas do Game View (`ScreenCapture`, uma por momento) em `docs/integration/evidencias/editor/`
`01`–`14`. A Game View do Editor estava em 472×266 e as capturas foram ampliadas 4×: servem para o estado da
jornada (HUD, mão, barra do chefe, resultado), não para ler texto. As capturas legíveis são as da build (abaixo).
Vitória no Editor: coberta pelos testes `InterfaceIntegradaTests.Vitoria_*` e `EncontroDoChefePlayModeTests`.

### Partida manual na Release (mouse real, perfil `qa-consolidacao`, 1920×1080 tela cheia)
Evidências em `docs/integration/evidencias/build/` (captura da janela do jogo via `PrintWindow`).
1. Acampamento novo (título Loopia em Marcellus, moedas/recorde, botão) — `01`.
2. `Nova jornada` com clique real → jornada; painel da jornada, quatro cartas ilustradas e dica de câmera.
3. Hover na Torre → tooltip com nome, efeito e restrição — `02`.
4. Arraste da Torre para o miolo → grade visível, marcador vermelho, motivo "o miolo do anel é reservado ao Boss";
   soltar **não consumiu** a carta — `03`.
5. Arraste para casa válida → marcador verde, "Solte para construir", "+1 pendente" — `04`; soltar construiu a Torre,
   consumiu a carta e a recompensa pendente entrou na mão (Oficina de Arqueiro) — `05`.
6. Flechas e Coração no caminho, Fonte Restauradora e Santuário fora dele, todos com a regra certa.
7. Chefe com a barra nova e aviso de Revoada — `06`, `08`.
8. **Derrota** diante do chefe (1ª jornada) → resultado novo, cartas e painel somem — `07`; botão real → acampamento
   com 70 + 21 = 91 moedas.
9. F3 no acampamento mostra/esconde o caminho do save (`loopia_save_qa-consolidacao.json`).
10. **Vitória** (2ª jornada) → "Chefe derrotado · Lucca venceu depois de 4 voltas" — `09`; botão → 115 moedas.
11. Fechar e reabrir a build → 115 moedas preservadas — `10`.
Salto (Mola) e combate rodaram durante as jornadas (o Lucca atira sozinho; a Mola é coberta por
`LuccaIntegracaoTests.MolaRealUsaSpringJumpEDepoisRetomaONavMesh`). Sem exceção visível; os `Player.log` disponíveis
não têm exceções, mas o log da sessão principal foi sobrescrito por aberturas posteriores.

**Resoluções.** 1920×1080: validado com entrada física. 2560×1440 e 3440×1440: **PENDENTE** — o monitor é 1920×1080
e a `ResolucaoDaTela` prende a janela à resolução nativa. As únicas capturas nesses tamanhos continuam sendo as
renderizações fora de tela da revisão de interface e do Codex (não valem como validação física).

**Desempenho.** No Editor (Game View pequena, só para custo relativo): 384 batches / 387 draw calls / ~60 SetPass /
~530 mil triângulos com a interface; sem os Canvas novos, 357 / 359 / 46. A HUD custa ~27 batches e ~15 SetPass.
Memória do Editor em Play: 1,13 GB alocados, 364 MB de driver gráfico. FPS e frame time CPU/GPU **na build: PENDENTE**
(sem ferramenta de captura nesta sessão; a jogabilidade foi fluida a olho). Nenhum `Camera.Render()` em laço.

### Problemas encontrados
- Tooltip da carta fica preso na tela depois de soltar a carta (reproduzido 3×; some no próximo hover) — P1.
- Ver §6.

## 5. Arte e interface
- **Cartas do Codex:** as 14 ilustrações, moldura, shader dourado, parallax e tooltips funcionando na Release. Não
  houve redesenho.
- **HUD de jornada:** painel (voltas, PV, flechas, nível, moedas; passos e inimigos com F3), barra do chefe com
  Revoada, dica de câmera até o primeiro zoom.
- **Acampamento:** tema novo; o caminho do save só com F3.
- **Resultado:** três desfechos (vitória, derrota no chefe, queda numa volta), botão clicável desde o primeiro frame,
  fade único.

## 6. Pendências

**P0 — Bloqueantes:** nenhuma encontrada.

**P1 — Qualidade e experiência**
1. Tooltip preso após soltar a carta (`HudDeCartas`/`TooltipDeCarta`).
2. Cartas compactas ilegíveis em 1080p: nome e efeito com ~8–9 px; só o tooltip é legível.
3. Chefe e morcegos ainda são esferas; o modelo `Morcegp.fbx` está no projeto e não é usado.
4. Lucca com ~20 px de altura na vista padrão; difícil de acompanhar no combate.
5. Jornada curta: 4 voltas + chefe em ~1,5 min; flechas zeram com frequência.
6. Validação física em 2560×1440 e 3440×1440 e FPS da build (pendentes de equipamento/ferramenta).
7. A carta arrastada cobre a casa-alvo; o marcador fica parcialmente escondido.

**P2 — Polimento adicional**
- Acentos ausentes em textos de dados ("Morcego Anciao", "o miolo do anel e reservado ao Boss", "esta carta so vai no
  caminho", "coracao").
- Flechas/Coração mudam a cor da ilha em vez de mostrar uma construção; Torre e Fonte têm modelo.
- Espaço vazio da mão sem indicação (moldura escura).
- Aviso de TMP: LiberationSans sem o caractere de reticências.

**P3 — Evoluções futuras:** estações/biomas, mais cartas, meta-progressão no acampamento, áudio.

## 7. Sondagem dos próximos passos

| # | Tema | Impacto visual | Impacto gameplay | Risco técnico | Complexidade | Prioridade |
|---|---|---|---|---|---|---|
| 1 | Legibilidade das cartas compactas | Alto | Médio (ler a carta sem hover) | Baixo | Baixa | **Alta** |
| 2 | Layout/proporções da HUD | Médio (bem resolvido; mão pequena no centro) | Baixo | Baixo | Baixa | Média |
| 3 | Qualidade das ilustrações | Alto (já boa) | Baixo | Baixo | — | Baixa |
| 4 | Fidelidade ao ShaderLab | Médio (moldura e ouro fiéis em tamanho grande; perdem-se no compacto) | Baixo | Baixo | Baixa | Média |
| 5 | VFX de colocação/construção | Médio (pulso discreto) | Baixo | Baixo | Média | Média |
| 6 | Construções G2 x objetos nas ilhas | Alto (Flechas/Coração só tingem a ilha) | Médio (o jogador não reconhece o que pôs) | Médio (arte nova, NavMesh intacto) | Média | **Alta** |
| 7 | Modelos/animações da raposa e do morcego | Alto (esferas) | Médio (ler ameaça) | Médio (rig, escala, VisualDeIlha) | Média-alta | **Alta** |
| 8 | Feedback de dano, morte e spawn | Médio | Alto (entender por que perdeu) | Baixo | Média | **Alta** |
| 9 | Visibilidade da Lucca no combate | Alto | Alto | Baixo (contorno/escala de leitura) | Baixa-média | **Alta** |
| 10 | Desempenho na build | — | — | Médio (sem medida) | Baixa (medir) | Média |
| 11 | Duração e ritmo da jornada | Baixo | Alto (~1,5 min por run) | Baixo (dados) | Baixa | **Alta** |
| 12 | Acampamento e telas finais | Médio (limpas, mas um painel sobre fundo liso) | Baixo | Baixo | Média | Média |

**Cinco problemas de maior impacto na qualidade percebida**
1. Inimigos e chefe como esferas: o "Morcego Ancião" é uma bola vermelha.
2. Lucca minúscula no combate: o protagonista quase não é visto.
3. Ritmo: a jornada inteira dura ~1,5 min e o chefe decide tudo em segundos.
4. Cartas compactas ilegíveis sem hover (e tooltip que fica preso).
5. Construções das cartas sem leitura no mundo (ilha tingida em vez de objeto) e feedback fraco de dano/morte.

Nada disso foi implementado nesta tarefa.

## 8. Encerramento
- Workspace: uma worktree ativa (`loopia-mvp`), um Editor. As outras (`loopia`, `loopia-cards`, `loopia-g2`,
  `loopia-gameplay`, `loopia-hud-preview`, `loopia-hud-publicacao`, `loopia-lucca`) ficam como backup; todas têm o
  conteúdo publicado na ancestralidade da consolidação e as diferenças locais salvas no checkpoint. **Podem ser
  removidas numa limpeza autorizada** (`git worktree remove` sem `--force`, depois de conferir o status). A
  `loopia-hud-preview` está em HEAD destacado (`4f5d3ef`, já incluído).
- PR de consolidação, SHA final e merge: registrados em `docs/handoff/ESTADO_ATUAL.md`.

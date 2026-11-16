# Sprint corretiva de UI/UX (2026-11-16)

Branch `fix/sprint-ui-polish` a partir da `main` @ `5f5f532`. Uma worktree (`loopia-mvp`), um Editor (perfil de save
`qa-consolidacao`). Refina a interface aprovada; não cria outra HUD nem mexe em regras, balanceamento, NavMesh,
animações, modelos, saves ou geração do mapa.

## Resumo

| # | Problema | Estado |
|---|---|---|
| 1 | Tooltip preso depois de soltar a carta | **Corrigido** (causa real encontrada e coberta por teste) |
| 2 | Cartas compactas com texto de 8–11 px | **Corrigido** (títulos 11–16 px, valor ≈ 12–15 px; tooltip 15–28 px) |
| 3 | Fantasma do arraste cobre a casa-alvo | **Corrigido** (afasta-se pelo raio real da casa; troca de lado na borda) |
| 4 | Espaço vazio pouco informativo | **Corrigido** ("Espaço livre", silhueta a 42%, chegada da recompensa) |
| 5 | Textos sem acento / aviso de reticências | **Corrigido** (dados, cena e motivos; fallback da Marcellus) |
| 6 | Composição e escalonamento | Revisado em 1920×1080, 1280×720 e 3440×1440 (as duas últimas só no Editor) |
| — | Validação física na build nova | **PENDENTE** — o acesso de controle à build foi recusado nesta sessão |

## 1. Tooltip preso

**Reprodução.** Na Release consolidada (`loopia-unified-d05dfd8`), ao soltar uma carta numa casa válida o tooltip
da carta reaparecia como tooltip da plataforma recém-construída ([antes/04](ui-polish/antes/04_espaco_vazio_e_tooltip_preso_1080.jpg)).

**Causa real.** No quadro seguinte à soltura, `CartasUI.Update` chama `HudDeCartas.Observar` com a plataforma que
acabou de nascer sob o cursor. `Observar` tratava isso como hover voluntário e abria o tooltip dela. O teste novo
`ColocacaoValida_TooltipNaoFicaPreso_EReabreNumHoverNovo` falhou exatamente nesse ponto antes da correção.

**Correção** (`HudDeCartas`, `CartasUI`). Fim do arraste (`EncerrarArraste`, inclusive por cancelamento) fecha o
tooltip e marca uma transição: a plataforma que estiver sob o cursor no primeiro quadro depois de soltar fica
ignorada **até o ponteiro sair dela** (ou ir para outra plataforma ou para uma carta). Depois disso o hover na
plataforma volta a mostrar as informações dela normalmente — o tooltip não é escondido para sempre.
`LimparTransitorios()` zera hover, supressão e tooltip ao desligar a HUD, ao fim da jornada e na perda de foco
(`OnApplicationFocus(false)`).

**Achado do ambiente (não é defeito do jogo).** Na build, parte do "tooltip preso" observado na consolidação vinha do
próprio teste: movimentos do cursor para a faixa superior da tela (y ≈ 70–160 px do quadro) não chegam ao jogo — há
uma janela de outro aplicativo por cima nessa região. Com o cursor no centro da tela o tooltip fecha normalmente.
As validações com mouse real devem evitar essa faixa.

## 2. Legibilidade das cartas compactas

**Causa.** A mão usava a carta inteira a 0,445 (114 × 158 px): título de 25 unidades com autoajuste até 10 → 4–11 px;
resumo e valor ≈ 7 px.

**Correção** (`CartaVisual.Compactar`, `HudDeCartas`). Modo compacto só para a mão e o fantasma; a prévia do tooltip
segue com a carta cheia. Nada muda no prefab, na moldura, na arte, no shader dourado ou no parallax.
- Escala 0,445 → **0,6** (154 × 214 px em 1080p; mão de ~650 px centralizada, folga 12).
- Título ocupa a placa (x43–213, centrada em y36), 17–27 unidades. Medido nas 14 cartas: **16,2 px** para nomes
  curtos, **14,6–14,9 px** para "Área de Treino" e "Campo Pacífico", **11,4–11,5 px** para "Fonte Restauradora" e
  "Oficina de Arqueiro" (limite da placa de uma linha nesta escala; antes ≈ 8 px). Sem abreviações.
- Selo de valor maior (16–25 unidades → 9,6–15 px) e ícone de categoria 31 unidades. O resumo de 7 px saiu da mão.
- Tooltip ampliado: prévia a 0,7 + painel 316 × 286 com **nome** (22–28 px), **resumo · valor** (15–18 px, negrito),
  **efeito** (16–19 px), **regra** e **condição** (15–17 px). A regra segue logo após o efeito.
- Placa de pendentes 160 × 32, 17 px em negrito.

## 3. Fantasma do arraste

**Causa.** `MoverArraste` punha o fantasma a 14 unidades à direita do cursor, sem considerar o tamanho da casa na
tela; o hexágono e o marcador ficavam meio cobertos. O aviso de validade ficava fixo no canto inferior direito e
podia cobrir casas daquela região.

**Correção.** `CartasUI.RaioDaCasaNaTela` projeta o raio da casa sob o cursor; o fantasma fica a esse raio + 12
unidades, à direita, ou à esquerda quando não cabe; o eixo vertical é limitado ao quadro. O aviso acompanha o
fantasma (embaixo dele, ou em cima sem espaço), alinhado pela borda longe do cursor. O raycast continua na posição
real do cursor; `PodeColocar` e o consumo não mudaram. Testado nas bordas esquerda, direita e inferior
([depois/08a](ui-polish/depois/08a_alvo_borda_esquerda_1080_editor.jpg), [08b](ui-polish/depois/08b_alvo_borda_direita_1080_editor.jpg),
[08c](ui-polish/depois/08c_alvo_borda_inferior_1080_editor.jpg)).

## 4. Espaço vazio e estados da mão

| Estado | Leitura |
|---|---|
| Carta disponível | carta cheia, opacidade 1 |
| Espaço vazio | silhueta a 42% e "Espaço livre" em Marcellus |
| Recompensa pendente | placa "+N pendente(s)" sobre a mão |
| Recompensa chegando | brilho dourado já existente + crescimento de 5% que assenta em ~0,4 s |
| Carta sendo arrastada | espaço de origem com a arte a 27% e o fantasma ao lado do cursor |

A fila de recompensas não mudou.

## 5. Tipografia e acentuação

- **Dados** (pelo Editor, `SerializedObject`): nomes e descrições de Básica, Área de Treino, Cabana, Campo Pacífico,
  Coração, Floresta ("A cada volta, nasce um lobo nesta casa."), Fonte Restauradora, Ilha Alta, Mola, Oficina de
  Arqueiro e Santuário. Nomes de assets e chaves não mudaram.
- **Chefe:** "Morcego Ancião" na cena (pelo Editor; uma linha do YAML) e no padrão de `VarianteDoChefe`.
- **Motivos de colocação:** `TextoDoJogador.Motivo` converte o motivo interno em frase ("O centro do anel é reservado
  ao chefe.", "Esta carta só pode ser colocada no caminho.", "O acampamento não pode ser substituído."). As strings
  internas ficam como estão porque testes de regra (`PlacementRulesTests`, `ColocacaoNoFimDaJornadaTests`) dependem
  delas e a sprint proíbe mexer em `PodeColocar`. Instruções curtas ("Solte para construir") seguem sem ponto.
- **Fontes:** `LiberationSans SDF` (estática, 250 caracteres) cobre todos os acentos do português, `…`, `·`, `×`,
  `+`, `%`; falta apenas `−` (U+2212), que a interface não usa. `Marcellus SDF` cobre os acentos, mas não `… · × ª`:
  ganhou a Liberation Sans como fallback (nenhum atlas recriado). O aviso "Ellipsis is not available in font asset
  [LiberationSans SDF]" aparece só no Editor, no recarregamento de domínio (pilha em `OnPreRenderCanvas`); a fonte tem
  o glifo, e os `Player.log` das builds não têm o aviso. Teste novo verifica a cobertura de todo texto mostrado.
- Inconsistência registrada, não alterada: os dados dizem "o Lucca" (masculino); a equipe se refere à personagem como
  "a Lucca". Decidir o gênero do texto é do responsável.

## 6. Composição

Canvas (sem mudança): `Interface da jornada` 5, `HUD de Cartas` 6, resultado 10; um EventSystem com
`InputSystemUIInputModule` por cena; `CanvasScaler` 1920 × 1080, match pela altura.

| Resolução | Como | Resultado |
|---|---|---|
| 1920 × 1080 | Editor com a Game View renderizando em 1920 × 1080 (não ampliada) | mão, tooltip, arraste nas bordas, chefe e resultado conferidos |
| 1280 × 720 | Editor, idem | layout íntegro; tudo escala por 0,667 → títulos longos ≈ 7,6 px, curtos ≈ 10,8 px |
| 3440 × 1440 | Editor, idem (render, **sem** entrada física) | mão centralizada, tooltip à direita, fantasma não cobre o alvo na borda |
| 4:3 | não testado | risco: com canvas de 1440 de largura, tooltip (503) e mão (652) se sobrepõem |

## 7. Testes

| Execução | Total | Passou | Evidência |
|---|---|---|---|
| Logic + SceneStatic | 407 | 407 | job `932e1246…` |
| PlayModeSmoke | 34 | 34 | [TestResults_playmodesmoke.xml](ui-polish/TestResults_playmodesmoke.xml) |
| PlayModeMedicao | 4 | 4 | [TestResults_playmodemedicao.xml](ui-polish/TestResults_playmodemedicao.xml) |
| **Total** | **445** | **445** | baseline 436 + 9 novos; nenhum removido ou ignorado |

Novos: `HudDeCartasInteracaoTests` (6, PlayModeSmoke, eventos reais do Input System) — tooltip após colocação válida,
após colocação inválida, troca de hover carta/plataforma, perda de foco no arraste, fim da jornada no arraste, quatro
espaços + vazio + pendente + consumo. `LegibilidadeDasCartasTests` (3, SceneStatic) — títulos e valores das 14 cartas
na escala real, tooltip das 14 cartas sem corte e dentro do painel, cobertura de caracteres. Os testes de cena
oficial da consolidação (`InterfaceIntegradaTests`) continuam passando. Console: 0 erros. Save pessoal intacto
(`76e429e4…`).

## 8. Capturas (antes → depois)

"Antes": build consolidada em 1920 × 1080 com mouse real, exceto 02/03 (Editor em 1920 × 1080 real, antes de qualquer
mudança visual). "Depois": Editor com a Game View em 1920 × 1080 real e mouse virtual do Input System.

| Item | Antes | Depois |
|---|---|---|
| 1. Quatro cartas | [01](ui-polish/antes/01_quatro_cartas_1080.jpg) | [01](ui-polish/depois/01_quatro_cartas_1080_editor.jpg) |
| 2. Tooltip Oficina de Arqueiro | [02](ui-polish/antes/02_tooltip_oficina_de_arqueiro_1080_editor.jpg) | [02](ui-polish/depois/02_tooltip_oficina_de_arqueiro_1080_editor.jpg) |
| 3. Tooltip Fonte Restauradora | [03](ui-polish/antes/03_tooltip_fonte_restauradora_1080_editor.jpg) | [03](ui-polish/depois/03_tooltip_fonte_restauradora_1080_editor.jpg) |
| 4. Espaço vazio | [04 (com tooltip preso)](ui-polish/antes/04_espaco_vazio_e_tooltip_preso_1080.jpg) | [04](ui-polish/depois/04_espaco_livre_1080_editor.jpg) · [após soltar, sem tooltip](ui-polish/depois/04a_soltura_valida_sem_tooltip_e_espaco_livre_1080_editor.jpg) · [hover voluntário](ui-polish/depois/04b_hover_voluntario_na_torre_1080_editor.jpg) |
| 5. Recompensa pendente | [05](ui-polish/antes/05_recompensa_pendente_1080.jpg) | [05](ui-polish/depois/05_recompensa_pendente_1080_editor.jpg) · [chegando](ui-polish/depois/05a_recompensa_preenche_espaco_1080_editor.jpg) |
| 6. Arraste válido | [06](ui-polish/antes/06_arraste_valido_1080.jpg) | [06](ui-polish/depois/06_arraste_valido_1080_editor.jpg) |
| 7. Arraste inválido | [07](ui-polish/antes/07_arraste_invalido_1080.jpg) | [07](ui-polish/depois/07_arraste_invalido_miolo_1080_editor.jpg) |
| 8. Alvo na borda | [08 esquerda](ui-polish/antes/08_alvo_na_borda_esquerda_1080.jpg) | [esquerda](ui-polish/depois/08a_alvo_borda_esquerda_1080_editor.jpg) · [direita](ui-polish/depois/08b_alvo_borda_direita_1080_editor.jpg) · [inferior](ui-polish/depois/08c_alvo_borda_inferior_1080_editor.jpg) |
| 9. Chefe e HUD | [09](ui-polish/antes/09_chefe_e_hud_1080.jpg) | [09](ui-polish/depois/09_chefe_e_hud_1080_editor.jpg) |
| 10. Resultado | [10 derrota](ui-polish/antes/10_derrota_resultado_1080.jpg) | [10 derrota](ui-polish/depois/10_resultado_Derrota_1080_editor.jpg) |
| Outras resoluções | — | [1280 hover](ui-polish/depois/res_1280x720_hover_editor.jpg) · [1280 borda](ui-polish/depois/res_1280x720_arraste_borda_direita_editor.jpg) · [3440 hover](ui-polish/depois/res_3440x1440_hover_editor.jpg) · [3440 borda](ui-polish/depois/res_3440x1440_arraste_borda_direita_editor.jpg) |

## 9. Build

| Build | Pasta | Resultado |
|---|---|---|
| Release x64 | `Documents\Loopia_Builds\loopia-ui-polish-cf407af-release\` | Succeeded, 0 erros, 0 avisos, 149,8 MB |
| Development x64 | `Documents\Loopia_Builds\loopia-ui-polish-cf407af-dev\` | Succeeded, 0 erros, 213,4 MB |

Cenas: Acampamento e HexPrototipo. Builds anteriores intactas. A Release abriu com o perfil `qa-ui-polish`; o
`Player.log` dessa abertura não tem exceções nem o aviso de reticências.

**PENDENTE — validação com mouse real na build nova.** O pedido de controle da janela da build foi recusado nesta
sessão, então hover, arraste, cancelamento, soltura inválida/válida, recompensa, chefe, fim da jornada, volta ao
acampamento e reabertura **não** foram feitos com mouse físico nesta build. O comportamento está coberto por testes
de Play Mode com eventos reais do Input System e pelas capturas do Editor em 1080p real. Roteiro para a validação:
`Loopia.exe -loopia-perfil qa-ui-polish -screen-fullscreen 1 -window-mode borderless -screen-width 1920 -screen-height 1080`,
mantendo o cursor fora da faixa superior da tela.

## 10. Riscos residuais

- Títulos longos ("Fonte Restauradora", "Oficina de Arqueiro") ficam em ~11,4 px em 1080p e ~7,6 px em 720p: limite
  da placa de uma linha. Próximo passo possível: placa ou moldura alternativa para nomes longos (decisão de arte).
- A mão maior (214 px) cobre um pouco mais das ilhas do arco inferior.
- 4:3 não testado (sobreposição provável do tooltip com a mão).
- O aviso de reticências do Editor no recarregamento de domínio continua (não aparece no player).
- `Application.runInBackground = true` usado no Editor grava `runInBackground: 1` no `ProjectSettings`; foi revertido
  e não está nos commits. Builds também regravam `PC_RPAsset`, `UniversalRenderPipelineGlobalSettings`,
  `ProjectSettings` e `TimeManager` (fora dos commits, como nas sprints anteriores).

## 11. Arquivos

`Assets/Scripts/Hex/Cartas/{CartaVisual,HudDeCartas,TooltipDeCarta}.cs`, `Assets/Scripts/Hex/CartasUI.cs`,
`Assets/Scripts/Hex/TextoDoJogador.cs` (+ `.meta`), `Assets/Scripts/Hex/VarianteDoChefe.cs`,
`Assets/Dados/Plataformas/*.asset` (11 textos), `Assets/Arte/Cartas/Fontes/Marcellus SDF.asset` (fallback),
`Assets/Scenes/HexPrototipo.unity` (nome do chefe), `Assets/Tests/Editor/{HudDeCartasInteracaoTests,LegibilidadeDasCartasTests}.cs`
(+ `.meta`). Rollback: `git revert` dos commits da branch.

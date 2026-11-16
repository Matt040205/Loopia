# Validação de build — Sprint de estabilização do MVP

Toda validação aqui usa **entrada real de mouse** (computer use do Claude: `left_mouse_down`/`mouse_move`/
`left_mouse_up` e cliques) numa janela do jogo fora do Editor. Nenhum teste automatizado substitui estas partidas.

Método e pegadinhas (para repetir):
- O acesso do computer use à build só resolve pelo caminho completo do `.exe`.
- A captura do computer use mascara janelas que não estão na lista de acesso, mesmo atrás do jogo; as imagens de
  evidência foram tiradas da própria janela do jogo (`PrintWindow`, 1920×1080).
- Tela cheia sem mexer no registro do jogador: `-screen-fullscreen 1 -window-mode borderless -screen-width 1920
  -screen-height 1080`.
- A build de fallback tem `runInBackground` desligado: o jogo pausa quando outro app pega o foco (notificações do
  Fluxer, o próprio app do Claude). O foco era devolvido ao jogo antes de cada espera.
- Antes de cada execução: cópia e SHA-256 do save; depois: restauração. Hash de referência do save do responsável:
  `3d27854e1ccb…551efb` (moedas 209, recorde 6, vitórias 1).

## 1. Build de fallback `mvp-20261117-dev-55442a5` (estado inicial, P0-1)
`C:\Users\Sitr3n\Documents\Loopia_Builds\mvp-20261117-dev-55442a5\Loopia.exe`, Development, 2026-11-15 ~19:45–20:00.

| Passo | Resultado | Evidência |
|---|---|---|
| Abrir no Acampamento | ok: "Moedas: 209", "Recorde de voltas: 6" (save restaurado) | — |
| "Nova jornada" (clique real) | ok: HexPrototipo carregada, anel de 30 | — |
| Cartas pela UI real (arraste) | ok: Flechas, Coração, Moedas ×2, Flechas, Fonte Restauradora colocadas; Básica recusada fora do caminho com o motivo na tela ("esta carta só vai no caminho"); grade livre/bloqueada visível durante o arraste | — |
| Cartas G2 | Fonte Restauradora sorteada e colocada fora do anel ("Solte para construir em (2,4)") | — |
| 4 voltas → chefe | ok: "Voltas: 4 (CHEFE!)", barra "Morcego Ancião 662/732" | — |
| Cartas de dano | **nenhuma** oferecida em 4 níveis (Moedas, Básica, Flechas, Fonte) | ver P1-3 |
| Derrota | ok: "Fim da jornada — O Lucca caiu diante do chefe. Moedas nesta jornada: 43" (chefe a 172/732) | `evidencias/fallback_derrota_tela.png` |
| Botão "Volte ao acampamento" (clique real) | **ok**: Acampamento carregado, "Moedas: 253" | `evidencias/fallback_derrota_volta_acampamento.png` |
| Save depois da jornada | moedas 209 → 253 (**+44**, a tela disse 43; ver correção da moeda pós-morte) | — |
| Fechar e reabrir | ok: "Moedas: 253" preservadas | `evidencias/fallback_reabertura_acampamento.png` |
| Exceções nos logs do player (3 execuções) | 0 `Exception`, 0 `Error` | logs no scratchpad da sessão |
| Save do responsável | restaurado para `3d27854e…` depois da validação | — |

Conclusão: o clique real chega ao Input System (`InputSystemUIInputModule`); a lacuna do relatório do MVP era do
método de clique por mensagem de janela, não do jogo. **Derrota e retorno ao acampamento comprovados na build.**
**Vitória na build: não ocorreu nesta partida** (sem carta de dano no sorteio) — validar na build nova.

## 2. Builds novas (HEAD `7bf6492`, scripts idênticos a `8fe885e`)
| Build | Pasta | Resultado do build |
|---|---|---|
| Release (apresentação) | `C:\Users\Sitr3n\Documents\Loopia_Builds\mvp-estabilizacao-7bf6492-release\Loopia.exe` | Succeeded, 0 erros, 0 avisos, 129,9 MB; só Acampamento (0) e HexPrototipo (1) |
| Development (QA) | `C:\Users\Sitr3n\Documents\Loopia_Builds\mvp-estabilizacao-7bf6492-dev\Loopia.exe` | Succeeded, 0 erros, 0 avisos, 193,5 MB |
Nenhum arquivo de Project Settings/URP ficou modificado depois das duas builds (P0-5).

### 2.1 Release — instalação nova e instalação antiga (P0-3, LEG-1), 2026-11-15 ~20:55
| Passo | Resultado | Evidência |
|---|---|---|
| Registro sem chave `HKCU\Software\DefaultCompany\Loopia` (1ª abertura real) | confirmado antes de abrir | — |
| **Instalação nova** (save antigo movido temporariamente; nenhum save na pasta nova) | Acampamento "Moedas: 0", "Recorde de voltas: 0", "Salvo em …/DefaultCompany/Loopia/loopia_save.json"; janela "Loopia" | `evidencias/release_instalacao_nova_acampamento.png` |
| Resolução na 1ª abertura | tela cheia sem borda 1920×1080 (registro: modo 1, nativa), chave `loopia_resolucao_nativa_v1 = 1`; sem a marca "Development Build" | registro lido depois |
| Limpeza | save zerado da instalação nova apagado; save antigo devolvido (`3d27854e…`) | — |
| **Instalação antiga** (só o save de `GameJam13-06`) | log: "Save copiado de …GameJam13-06\loopia_save.json para …Loopia\loopia_save.json (o original continua lá)"; Acampamento "Moedas: 209", "Recorde de voltas: 6" | `evidencias/release_instalacao_antiga_acampamento.png` |
| Hashes depois de fechar | antigo `3d27854e…` (intacto) e novo `3d27854e…` (cópia idêntica) | — |
| Perfil de QA (`-loopia-perfil qa`) | log: "Perfil 'qa': save em …Loopia\loopia_save_qa.json"; arquivo novo zerado; save do jogador intacto | — |
| Exceções nos logs do player | 0 `Exception`, 0 `Error` | — |

### 2.2 Release `7bf6492` — partida completa com **vitória** (2026-11-16 ~00:20, `-loopia-perfil qa`)
1ª tentativa (15/11 ~21:00): parada antes do primeiro clique porque o computador estava em uso (partida de League of
Legends). 2ª: chegou ao chefe e o jogo foi fechado de fora enquanto a sessão estava parada. 3ª, com o responsável
liberando mouse e teclado:

| Passo | Resultado | Evidência |
|---|---|---|
| Abrir com `-loopia-perfil qa` | Acampamento do perfil de QA; foco por clique na janela (o Windows bloqueia `SetForegroundWindow`) | — |
| Mão inicial | Mola, Coração, **Torre**, Flechas | — |
| Cartas pela UI real | Flechas, Coração, Mola no anel; Torre fora (0,6); **Área de Treino vizinha da Torre** (1,6); 2ª Flechas; Santuário (1,5) → PV 47/53. Recusas com motivo na tela: "espere o Lucca terminar o salto", "o Lucca precisa passar por uma mola antes de cada subida alta" | — |
| 4 voltas → chefe | "Voltas: 4 (CHEFE!)"; barra "Morcego Ancião 226/732" logo no começo do confronto | — |
| **Vitória** | "O Lucca derrotou o chefe depois de 4 voltas! Moedas nesta jornada: 27"; log `Estado: Chefe -> Vitoria` | `evidencias/release_vitoria_tela.png` |
| Save de QA | `vitorias` 0 → **1** (uma vez); moedas 19 → 47 (**+28**, a tela disse 27 → corrigido em `a7a5c56`) | — |
| Botão "Volte ao acampamento" (clique real) | ok: Acampamento "Moedas: 47", "Recorde de voltas: 4", "Salvo em …loopia_save_qa.json" | `evidencias/release_vitoria_volta_acampamento.png` |
| Fechar e reabrir com o mesmo perfil | ok: 47 moedas, 1 vitória | `evidencias/release_vitoria_reabertura.png` |
| Exceções | 0 nas duas execuções | — |
| Save do jogador | não tocado pelas partidas de QA | — |

Derrota numa build nova: não repetida; o caminho (mesma `TelaDeMorte`, mesmo botão, mesmo `DadosDoJogador`) foi
comprovado com mouse real na build de fallback (§1). Roteiro para repetir à mão:
1. `Loopia.exe -loopia-perfil qa` (save separado; o do jogador não muda).
2. Acampamento → "Nova jornada". A mão começa com Mola, Coração, **Torre**, Flechas.
3. Arrastar Flechas e Coração para ilhas do anel; Torre numa casa livre fora do anel; Área de Treino (se vier) numa
   casa vizinha da Torre; liberar a mão para os níveis entrarem.
4. Quatro voltas → "Voltas: 4 (CHEFE!)" e barra do Morcego Ancião; vitória ou derrota.
5. Tela de resultado → clicar "Volte ao acampamento" → Acampamento.
6. Conferir `loopia_save_qa.json`: `vitorias` +1 (uma vez só); fechar e reabrir com o mesmo argumento: dados iguais.
7. Repetir para uma derrota (sem usar as cartas) e validar o mesmo retorno.

Observações durante a partida:
- Mesmo uma execução curta (≈30 s de jornada) somou 5 moedas ao save pessoal: motivo do perfil de save de QA (P0-2).
- O processo do Editor que caiu (medição de MSAA, ver `FIX_REPORT.md`) gravou o save ao terminar (`OnApplicationQuit`).

## 3. Builds finais (`a7a5c56`, com a correção da moeda depois da vitória)
| Build | Pasta | Resultado |
|---|---|---|
| Release (apresentação) | `Documents\Loopia_Builds\mvp-estabilizacao-a7a5c56-release\Loopia.exe` | Succeeded, 0 erros, 0 avisos, 129,9 MB |
| Development (QA) | `Documents\Loopia_Builds\mvp-estabilizacao-a7a5c56-dev\Loopia.exe` | Succeeded, 0 erros, 0 avisos, 193,5 MB |

Fumaça da Release final: abre no Acampamento do perfil de QA ("Moedas: 47", `evidencias/release_final_abertura.png`)
e fecha sem exceções. **A jornada nela não foi jogada**: o acesso do computer use a essa build foi negado pelo
responsável. A diferença para a `7bf6492` (validada acima) é só `a7a5c56` (moeda depois do fim), coberta por teste
Logic e pelo PlayModeSmoke da vitória.

## 4. Builds do Bloco C (`6dd6206`: P2-1, P2-2, P2-3)
| Build | Pasta | Resultado |
|---|---|---|
| Release (apresentação) | `Documents\Loopia_Builds\mvp-estabilizacao-6dd6206-release\Loopia.exe` | Succeeded, 0 erros, 0 avisos, 129,9 MB, 28 s |
| Development (QA) | `Documents\Loopia_Builds\mvp-estabilizacao-6dd6206-dev\Loopia.exe` | Succeeded, 0 erros, 0 avisos, 193,6 MB, 19 s |

Fumaça da Release (2026-11-16 ~02:25, sem computer use): aberta com `-loopia-perfil qa` em janela 1280×720, captura
da própria janela (`evidencias/release_6dd6206_abertura.png`: Acampamento do perfil de QA, "Moedas: 47", "Recorde de
voltas: 4"), fechada; log sem exceções; save do jogador com o mesmo hash antes e depois (`76e429e4…`).

**A jornada nesta build não foi jogada.** O Bloco C só mexe no visual do Lucca (`LuccaAnimationBridge`) e na tela de
resultado (fade); regras, save, cenas e troca de cena não mudaram, e os PlayModeSmoke de vitória/derrota do chefe e do
Mirante cobrem os caminhos. Para conferir à mão na Release: roteiro do §2.2, olhando também (a) o Lucca virando o corpo
para atirar no chefe e num alvo atrás no Mirante, (b) a corrida antes dos pulos sem os pés escorregando de lado, (c) a
pose de comemoração ou de queda e o fade da tela de resultado.

As builds `a7a5c56` e `7bf6492` continuam como versões anteriores; o fallback `mvp-20261117-dev-55442a5` não foi tocado.

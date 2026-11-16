# Auditoria de problemas — Sprint de estabilização do MVP

Branch `fix/mvp-stabilization`, base `integration/mvp-20261117` @ `f49cee1` (PR #11, Draft, sem mudança desde a
integração; conferido com `git fetch` em 2026-11-15). A branch já começa com os 7 commits do passo de legibilidade
(`docs/sprints/SPRINT-4.md`, aprovado pelo responsável), porque eles mudam a mesma cena, câmera e HUD que esta sprint
toca. Unity 6000.3.10f1 / URP 17.3, Editor na worktree `loopia-mvp` (bridge MCP pela porta do status do projeto).

Fallback preservado: `C:\Users\Sitr3n\Documents\Loopia_Builds\mvp-20261117-dev-55442a5\Loopia.exe` (não tocar).

Legenda de estado: **aberto** · **reproduzido** · **corrigido** (commit) · **validado** (build) · **pendente** (decisão).

## Bloco A — obrigatório

### LEG-1 · Elementos das ilhas minúsculos e pixelizados em telas grandes — **corrigido** (Editor), build pendente
- Estado inicial: captura do playtest (janela 1280×720 num monitor 1920×1080, anel de 36): topo da ilha ~52 px.
- Causa confirmada: resolução gravada no registro (janela 1280×720, não redimensionável), anel de 36 nunca
  validado, sem anti-aliasing.
- Solução: zoom do jogador, resolução nativa no build, HUD escalável, MSAA 4×, anel de até 30. Commits `9d7a759`…`0340291`.
- Validação: suíte 396/396 no Editor; falta confirmar `ResolucaoDaTela` num build novo.

### P0-1 · Vitória, derrota e retorno ao acampamento na build — **validado**: derrota e retorno na build de fallback; vitória, retorno, save e reabertura na Release `7bf6492`
- Estado inicial: no Editor, vitória/derrota/retorno validados (`MVP_INTEGRATION_REPORT.md` §7). Na build: derrota
  pela build de QA (sem clique); vitória e o botão "Voltar ao acampamento" **não** comprovados (o clique por mensagem
  de janela não chega ao Input System).
- Caminho no código: `LoopDirector` (estado → `aoVencer`/`aoGameOver`, UnityEvents na cena) → `TelaDeMorte.Mostrar`/
  `MostrarVitoria` → botão (uGUI, `InputSystemUIInputModule`) → `TelaDeMorte.VoltarAoAcampamento` →
  `DadosDoJogador.Salvar` + `SceneManager.LoadScene("Acampamento")`. Vitória gravada por `RegistrarVitoria`.
- Reprodução planejada: partida real na build com mouse real (computer use), backup e SHA-256 do save antes.
  Cenário de derrota (sem cartas de dano) e de vitória.
- Risco: baixo (validação). Se o mouse automatizado não alcançar o Input System, roteiro manual em `BUILD_VALIDATION.md`.

### P0-2 · Save do jogador compartilhado por Editor, build e build de QA — **corrigido** (`3bdd321`, `e009508`)
- Estado inicial: um único `loopia_save.json` em `%USERPROFILE%\AppData\LocalLow\DefaultCompany\GameJam13-06\`.
  Jogar qualquer build (inclusive a de QA, sem interação) altera moedas/vitórias do responsável. Exemplo desta
  sessão: capturas em Play mudaram moedas 209 → 210 (restaurado da cópia, SHA `3d27854e…`).
- Proteção existente nos testes (não reimplementar): `SaveIsolado` (backup por projeto em `%TEMP%`, trava
  `loopia_tests_save.lock` entre worktrees, guarda da suíte que compara SHA-256 no fim). A verificar com duas suítes.
- Solução proposta: perfil de save escolhido **só** por argumento de linha de comando (`-loopia-perfil <nome>` →
  `loopia_save_<nome>.json`), usado pelas builds de QA e pelas validações. Sem argumento, o caminho de sempre: a
  versão final não muda de lugar.
- Arquivos: `DadosDoJogador.cs` (+ testes Logic do nome do arquivo).
- Risco: baixo; sem argumento o comportamento é idêntico.

### P0-3 · Nome do produto ainda `GameJam13-06` — **validado** na build Release (`3bdd321`, `c9b6364`)
- Estado inicial: `companyName: DefaultCompany`, `productName: GameJam13-06`. A janela mostra "GameJam13-06".
- Efeito de renomear: `Application.persistentDataPath` muda para `...\LocalLow\DefaultCompany\Loopia\` (save novo
  vazio) e as preferências do registro (`HKCU\Software\DefaultCompany\Loopia`) começam do zero.
- Solução proposta: `productName: Loopia` + migração de leitura no `DadosDoJogador`: se não há save no caminho novo e
  há no antigo (`...\DefaultCompany\GameJam13-06\loopia_save.json`), **copiar** (o original fica intacto) e registrar.
  Testar instalação antiga (só o arquivo antigo) e nova (nenhum arquivo).
- Efeito colateral a documentar: `TestResults.xml` e o save do Editor também mudam de pasta (`tools/unity-mcp/README.md`).
- Risco: médio (persistência). Não renomear sem a migração testada.

### P0-4 · Cenas legadas habilitadas no `EditorBuildSettings` — **corrigido** (`c9b6364`)
- Estado inicial: Acampamento, HexPrototipo, Menu, Testes, GameOver e Win, todas `enabled: 1`. A build do MVP passou
  as cenas explicitamente; um "Build" pelo menu incluiria as 4 legadas.
- Solução proposta: desabilitar (não remover nem apagar) Menu, Testes, GameOver e Win, pelo Editor.
- Risco: baixo; teste SceneStatic para a lista.

### P0-5 · Project Settings e URP regravados pelo build (não commitados) — **corrigido** (`b0bbdf1`, `29a6059`)
- Arquivos: `PC_RPAsset` (flags de pré-filtragem de shader), `UniversalRenderPipelineGlobalSettings`,
  `ProjectSettings` (batching por plataforma), `TimeManager` (Fixed Timestep reserializado como racional; 0,02 s),
  `GraphicsSettings`, `.vscode/settings.json`, `Dressing_Vegetacao.mat` (`_MainTex` espelhando o `_BaseMap`).
- A decidir por item, com diff: o que é reserialização automática fica fora; Pass D2 Render (sem Opaque/SSAO)
  preservado.

## Bloco B — polimento prioritário

### P1-1 · Faixa rosa na borda inferior — **corrigido** (`1780146`); causa medida no Editor, ver `FIX_REPORT.md`
- Estado inicial: faixa ondulada rosa na base da tela no fim da tarde (relatório do MVP, `build_02`; também na
  captura do responsável: 1280×720, anel de 36).
- Hipótese (não confirmada): cor de fundo da câmera / névoa aparecendo além da borda da malha do `FundoDePaisagem`
  quando o enquadramento fica grande. O anel de 36 (ortho ~22–27) amplia a vista; com o anel limitado a 30 ela pode
  sumir sem a causa ter sido corrigida.
- Reproduzir: 16:9 meio-dia / fim da tarde / noite e 21:9 fim da tarde, Editor e build, anel 24, 30 e o maior
  enquadramento possível.

### P1-2 · Painel de depuração IMGUI grande na apresentação — **corrigido** (`cf5e968`)
- Estado inicial: `LoopDirector.OnGUI` mostra sempre Voltas, Passos, PV, Flechas, Nível/XP, Moedas (total) e
  Inimigos/multiplicador num painel 172×168.
- Solução proposta: painel de jogo compacto (volta, PV, flechas, moedas) sempre; detalhes técnicos só em
  Development Build ou com tecla explícita.

### P1-3 · Balanceamento do Morcego Ancião — **corrigido** (`b8881e1`): Torre no lugar de Moedas na mão inicial; medição antes/depois em `FIX_REPORT.md`
- Estado inicial: sem carta de dano o chefe vence (2/2 no G1); com Flechas+Torre o resultado oscila por 1 flecha.
- Investigar antes de mudar número: economia de flechas, dano, PV do chefe, duração, Revoada, cartas por volta,
  disponibilidade de cartas de dano, fila de recompensas.

## Bloco C — condicional (só depois de uma build validada como fallback)
- P2-1 · Mira do Mirante limitada a 65° de yaw (`KNOWN_ISSUES.md`).
- P2-2 · Deslizamento dos pés (P95 2,39 u/s) e contato do arco com túnica/saia.
- P2-3 · Fim de partida em Idle, sem pose de vitória/derrota.

# Loopia — release final da Sprint 5B (2026-11-17)

Encerramento da Sprint 5B (animação e integração dos inimigos 3D): validação final, merge do PR #17 na `main`,
build Release Windows x64 gerada da `main`, pacote ZIP testado depois de extraído.

## Identificação

| Item | Valor |
|---|---|
| PR integrado | [Matt040205/Loopia#17](https://github.com/Matt040205/Loopia/pull/17), merge commit (histórico preservado) em 2026-11-17 19:40 UTC |
| Código validado (head do PR) | `f1b7f4ec25d032729bd6323d67e54530ccd9ae84` |
| `main` depois do merge = código da build | `80488ec0bd2940385adf856c521d2fa0b3bcc9c1` (árvore idêntica a `f1b7f4e`: `git diff` vazio) |
| `main` anterior | `ef78f01120463c00ecba37fe4eafe4b36ac90877` |
| Unity | 6000.3.10f1, URP 17.3 |
| Plataforma | Windows x64, Release (Development Build e Script Debugging desligados), D3D12 |
| Cenas | `Acampamento` (0) e `HexPrototipo` (1) |

Este documento entra na `main` por um PR só de documentação; o SHA final da `main` depois dele está no handoff
(`docs/handoff/ESTADO_ATUAL.md`).

## O que esta versão traz

- **Inimigos 3D animados** (pacote 5A.1 v1.1, 17 clipes de produção feitos no Blender): Morcego comum (Idle, Fly,
  Attack, Hit, Death), Morcego Ancião com prefab próprio (inclui Revoada) e Lobo no NavMeshAgent (Idle, Walk, Run,
  Attack, Hit, Death). Ataque e Revoada sincronizados aos eventos reais de gameplay; dano continua 1×.
- **Escalas visuais aprovadas:** morcego 1,3, lobo 1,8, Ancião 4,5 (só visual).
- **Lucca:** fora do tiro, o braço do arco acompanha a marcha; o arco não gira na mão; na corrida fica deitado
  (pontas para frente e para trás), parada fica de pé.
- **Floresta:** a carta mostra o lobo; o lobo nasce mesmo com morcego pousado na casa.
- **Moedas** sorteadas numa ilha com item de carta ficam ao lado do item, sem atravessá-lo.
- **Mola desativada** (e, por consequência, a Ilha Alta indisponível). A mecânica segue no código.

Registro técnico completo: [SPRINT-5B-ANIMATION-INTEGRATION.md](../sprints/SPRINT-5B-ANIMATION-INTEGRATION.md).

## Testes

Editor 6000.3.10f1, perfil de save `qa-sprint5b`, no head `f1b7f4e`:

| Categoria | Resultado | Evidência |
|---|---|---|
| Logic + SceneStatic | **419/419** | job `3fdec576…` (18,0 s) |
| PlayModeSmoke | **38/38** | `sprint5b-final/TestResults_PlayModeSmoke_f1b7f4e.xml` (19:18Z) |
| PlayModeMedicao | **4/4** | `sprint5b-final/TestResults_PlayModeMedicao_f1b7f4e.xml` |
| **Total** | **461/461** | console sem erros |

Depois do merge, na `main` `80488ec`: compilação sem erros e Logic + SceneStatic **419/419** de novo
(job `92e54cb0…`). Nenhum teste foi alterado nesta etapa.

## Playtest (Release, mouse real por computer use, 1920×1080, perfil de QA isolado)

Release `loopia-sprint5b-f1b7f4e-release` (mesmo código da `main` final), perfil novo `qa-final5b` (save zerado):

| Passo | Resultado |
|---|---|
| Abrir o jogo / iniciar jornada | Acampamento com 0 moedas → Nova jornada |
| Usar cartas | Coração e Flechas no anel; Torre, Oficina, Área de Treino, Fonte fora do anel (regra "fora do caminho" respeitada) |
| Morcegos animados | Voando, mergulhando e atacando ao longo das voltas |
| Floresta e lobo | Lobo nasce na Floresta, vira para a Lucca, avança e morde quando ela passa, sai atrás dela e volta a circular; morre ao ser abatido (`03_lobo_avanca_morde_e_circula.jpg`) |
| Morcego Ancião e Revoada | Barra "Morcego Ancião", aviso "Revoada", disco de perigo e mergulho do Ancião (`02_anciao_revoada_e_mergulho.jpg`) |
| Vitória | 1ª jornada: derrota diante do chefe (33 moedas); 2ª: **"Chefe derrotado — Lucca venceu depois de 4 voltas"**, 28 moedas (`01_vitoria_chefe_derrotado_1080.jpg`) |
| Acampamento, moedas e vitórias | Save `qa-final5b`: 61 moedas (33 + 28), recorde 4, **vitórias 1** |
| Fechar e reabrir | Reaberto: 61 moedas e recorde 4 (`04_reaberto_save_persistiu_61_moedas.jpg`) |
| Player.log | 77 linhas, 0 erros, 0 exceções |

Uma segunda vitória aconteceu na sessão de medição (perfil `qa-final5b-perf`: 25 moedas, 1 vitória). Arco da Lucca
na corrida, moeda ao lado do item e escala dos inimigos foram verificados na Release `b8b651f` (mesmo código de
jogo; capturas em `docs/sprints/sprint-5b/build-rodada2/`). Nenhuma deformação grosseira nem personagem sumindo por
culling foi visto. A autointerseção interna do quadril do lobo não aparece na câmera oficial.

## Desempenho

Máquina de teste: CPU de 16 threads, AMD Radeon RX 9070 XT (16 GB), Windows 11, 1920×1080 tela cheia borderless.

**Release `f1b7f4e`**, jornada completa até a vitória (157 s), medido de fora do jogo, sem telemetria nova:
- FPS e frame time: eventos de Present do provedor ETW `Microsoft-Windows-DXGI` (sessão de usuário `logman`,
  convertida com `tracerpt`), filtrados pelo PID do jogo.
- CPU e memória: `Get-Process`. GPU: contadores do Windows `GPU Engine` (3D) e `GPU Process Memory`.

| Trecho | FPS médio | Frame time médio | Mediana | p99 | p99,9 | Pior quadro | Quadros > 16,7 ms |
|---|---:|---:|---:|---:|---:|---:|---:|
| Acampamento (0–9 s) | 2 587 | 0,39 ms | 0,36 ms | 0,83 ms | 1,06 ms | 15,9 ms | 0 |
| Jornada (10–153 s, 131 399 quadros) | **919** | **1,09 ms** | 1,04 ms | 1,89 ms | 2,37 ms | **7,0 ms** | **0** |
| Final da jornada com o chefe (128–153 s) | 917 | 1,09 ms | 1,04 ms | 1,89 ms | 2,56 ms | 4,2 ms | 0 |

| Processo (78 amostras) | Média | Máximo |
|---|---:|---:|
| CPU (% da máquina, 16 threads) | 22,5% | 28,4% |
| Working set | 622 MB | 644 MB |
| Memória privada | — | 1 074 MB |
| GPU 3D | 59,4% | 69,7% |
| Memória dedicada de GPU | — | 467 MB |

Inimigos simultâneos: a Release não exibe contagem. O HUD da build Development marcou 2 vivos no início da volta
1; o pico projetado é o teto do jogo (`maximoDeMorcegos` = 6, a Revoada respeita o mesmo teto, + lobos da Floresta +
chefe), já medido no Editor com essa carga (registro da sprint, §12: CPU +0,21 ms, GPU ≈ 0 em relação aos visuais
antigos). Bruto: `sprint5b-final/desempenho_release_f1b7f4e.json` e `desempenho_fps_por_segundo.json`.

**Limitação:** a tentativa com o Profiler do Editor conectado a uma build Development não deu dados confiáveis.
O jogo não limita quadros, o Profiler estourou o buffer de 128 MB e parou de enviar, e essa build caiu com
`Unrecoverable D3D12 device error` (device failed, 80004005, sem falta de memória). Nenhuma Release caiu hoje: 6 jornadas
completas (3 na `b8b651f`, 3 na `f1b7f4e`) e 3 aberturas curtas da build final.

## Pendências não bloqueantes

| Prioridade | Pendência |
|---|---|
| P1 | **Quadros sem limite**: `vSyncCount = 0` e nenhum `Application.targetFrameRate` → ~900 FPS em jogo e ~2 600 no acampamento, GPU 3D ~60%. Uma build Development com o Profiler caiu com erro de dispositivo D3D12 nessa condição. Recomendado para a próxima fase: VSync ou limite de quadros (mudança de ProjectSettings + revalidação). |
| P2 | **Testes PlayMode alteram PlayerSettings em memória**: ligam `Application.runInBackground`, o que no Editor muda `PlayerSettings.runInBackground`. Builds feitas na mesma sessão herdam o valor. A build final foi gerada com o valor da `main` (0), restaurado e conferido no disco. Fazer os testes restaurarem o valor. |
| P2 | Prega interna do quadril do lobo no galope (invisível na câmera oficial). |
| P3 | Fontes `.blend` da Lucca (2C.2) mostram o braço antigo; a revisão do arco está nos clipes Unity (`BracosDaLucca`). |
| P3 | A moeda ao lado do item foi vista na build só com o Coração; os demais itens usam a mesma regra (`Plataforma.TemItemNoCentro`). |

## Build e pacote

| Item | Valor |
|---|---|
| Pasta da build | `Documents\Loopia_Releases\Loopia_Sprint5B_Final\Loopia\` (`Loopia.exe`, `Loopia_Data\`, `UnityPlayer.dll`, `UnityCrashHandler64.exe`, `MonoBleedingEdge\`, `D3D12\`, `README.txt`) |
| Build | Succeeded, 0 erros, 0 avisos, 156 MB, a partir da `main` `80488ec` |
| Fora do pacote | `Loopia_BurstDebugInformation_DoNotShip\` (guardado em `Loopia_Sprint5B_Final\_nao_distribuir\`) |
| ZIP | `Documents\Loopia_Releases\Loopia_Sprint5B_Final\Loopia_Sprint5B_Windows_x64_Final.zip` |
| Tamanho do ZIP | 49 552 436 bytes (47,3 MB), 183 arquivos, raiz `Loopia/` |
| SHA-256 | `b9bf47283151497240659a3f55ffb28c29e7520580530545d3cf18e680072dd8` (também em `.zip.sha256`) |
| Integridade | `zipfile.testzip()` sem erros; arquivos essenciais presentes; extraído numa pasta temporária nova e comparado (`diff -r`) com a pasta de entrega: idêntico |

Testes da distribuição:
1. Pasta de entrega, perfil `qa-final5b-entrega`: abre, entra na jornada, sem marca "Development Build"
   (`05_build_final_pasta_entrega_jornada.jpg`); Player.log sem erros.
2. Pasta de entrega **sem argumento nenhum**: abre no acampamento lendo o save principal
   (`07_build_final_sem_perfil_acampamento.jpg`). O save principal não mudou (hash conferido antes e depois).
3. **Cópia extraída do ZIP** (`%TEMP%\loopia_zip_teste_final\Loopia\`), perfil `qa-final5b-zip`: abre, entra na
   jornada, carta colocada no anel (`06_zip_extraido_jornada.jpg`). O Player.log confirma execução da pasta
   extraída, 51 linhas, 0 erros.

O pacote não contém Library, caches, backups, `.blend`, código-fonte, saves nem logs.

## Como executar

1. Extrair o ZIP inteiro (a pasta `Loopia/`).
2. Abrir `Loopia\Loopia.exe` (não precisa da Unity). Requer Windows 10/11 64 bits com Direct3D 11/12.
3. Acampamento → "Nova jornada"; arrastar as cartas para as ilhas. Roda do mouse: zoom; botão direito: câmera.
4. Save em `%USERPROFILE%\AppData\LocalLow\DefaultCompany\Loopia\loopia_save.json`. Para jogar com um save
   separado: `Loopia.exe -loopia-perfil <nome>`.

## Saves

- Save pessoal `loopia_save.json`: SHA-256 `76e429e4…` igual antes e depois de toda a validação (backup extra em
  `Documents\Loopia_Backups\sprint5b-20261117\saves\loopia_save_antes_release_final.json`).
- Saves de QA criados nesta etapa (podem ser apagados): `loopia_save_qa-final5b.json` (61 moedas, 1 vitória),
  `qa-final5b-perf`, `qa-final5b-entrega`, `qa-final5b-zip`.
- Preferência de tela do jogador (registro `HKCU\Software\DefaultCompany\Loopia`): ficou em 1920×1080 tela cheia
  borderless, como antes. Um teste em janela 1280×720 a tinha mudado; ela foi restaurada.

## Evidências

`docs/releases/sprint5b-final/`: vitória, Revoada, lobo, reabertura, build final (pasta e ZIP extraído, com e sem
perfil), XMLs do PlayModeSmoke e do PlayModeMedicao, desempenho da Release. Bruto e capturas completas fora do repo
em `Documents\Loopia_Sprint5B\12_Final\`.

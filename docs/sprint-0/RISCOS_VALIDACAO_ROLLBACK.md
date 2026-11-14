# Riscos de regressão, procedimento de validação e rollback

## 1. Riscos de regressão nas cenas
| Risco | Onde | Gatilho típico | Como detectar | Mitigação |
|---|---|---|---|---|
| Conflito/merge de YAML de cena | `HexPrototipo.unity` (≈137 KB), `Acampamento.unity` | Dois PRs mexendo na cena | `git diff --stat` mostra `.unity`; `SceneStatic` falha | Um PR de cena por vez; resolver ficando com a versão do outro e reaplicando pelo Editor |
| UnityEvent perde listener | 4 chamadas persistentes (`TelaDoAcampamento`, `TelaDeMorte`×2, `LuccaStatus`) com `Assembly-CSharp` no nome do tipo | Mover scripts para asmdef, renomear classe/método | Teste `SceneStatic` de UnityEvents | Não renomear métodos-alvo; asmdef só em PR dedicado com teste |
| Script ausente / `m_Script: {fileID: 0}` | Assets de `Assets/Dados/Plataformas` | Várias classes num `.cs`, `.meta` perdido | Teste `SceneStatic` (carrega todos os `PlataformaDef`; conta missing scripts) | Uma classe por arquivo; conferir `.meta` no commit |
| GUID divergente entre máquinas | Assets sem `.meta` commitado (ex.: modelos do `0b9513e`) | Usar o asset em prefab/cena antes de commitar `.meta` | `git status` mostra `.meta` não rastreado | Commitar `.meta` uma única vez, pelo dono do asset |
| Rebake de NavMesh indevido | `NavegacaoDoMundo`, `MapaDePlataformas.AtualizarRelevo` | Novo sistema mexendo em altura/collider de ilha | Smoke: `Pulos.Count == Loop.Count`; Lucca anda | Arte sempre via `VisualDeIlha.Criar` (ignoreFromBuild + colliders off) |
| Zoom involuntário | `HexIsoCamera` (bounds de todos os renderers a cada 0,25 s) | Trocar decoração/variante sazonal | Observação em Play; (futuro) teste de bounds | Medir só malha das ilhas (Sprint de escala) |
| Dois donos de `RenderSettings` | `Daynightcontroller` | Novo controlador de clima | Visual oscila; valores trocam por frame | Dono único (PR `refactor/environment-ownership`) |
| Save real alterado por teste/Play | `%USERPROFILE%/AppData/LocalLow/<company>/<product>/loopia_save.json` | Rodar testes, Play Mode | Hash antes/depois no `TEST_REPORT.md` | Backup/restauração na suíte; `Apagar progresso` só manualmente |
| Diálogo modal trava o Editor/MCP | Cena aberta reescrita pelo git | `checkout`/`rebase` com a cena aberta | MCP para de responder | Fechar a cena antes de operações git que a alterem |
| Dependência não fixada | `com.coplaydev.unity-mcp#main` | Reimport/novo clone | Pacote muda de versão sem PR | Fixar por commit/tag quando estabilizar (combinar com a equipe) |

## 2. Procedimento de validação (a cada PR)
1. `git fetch` e conferir se `main` andou; rebase/merge antes de validar.
2. Abrir o projeto (Unity 6000.3.10f1). Console limpo após compilar (anotar contagem de erros/avisos).
3. Rodar a suíte completa (Test Runner > EditMode > Run All, ou MCP `run_tests` modo `EditMode`).
   Registrar job id e totais. `KnownIssue` ignorados não contam como falha nova.
4. Play manual curto na `Acampamento` → `Nova jornada` → `HexPrototipo`: Lucca anda, fecha 1 volta,
   arrastar 1 carta válida e 1 inválida (motivo aparece), morrer volta ao acampamento.
5. `git status`: nenhuma cena/asset alterado fora do escopo; nenhum `.meta` órfão.
6. Preencher o checklist do PR (AGENTS.md §7) e o handoff.

Itens que **só** valem com build (registrar como PENDENTE até rodar): FPS/CPU/GPU, draw calls,
overdraw das nuvens, 16:9 vs 21:9, caminho do save na build.

## 3. Rollback
- **PR de código/docs/testes:** `git revert <sha>` (ou do merge commit, `git revert -m 1 <sha>`).
  Testes ficam em `Assets/Tests/` e docs em `docs/`/`tools/` — apagar a pasta inteira (com `.meta`)
  também é seguro; nenhum asset de gameplay depende deles.
- **PR que tocou cena:** reverter o commit inteiro (nunca reverter só pedaços do YAML). Fechar a cena
  no Editor antes, reabrir depois e rodar `SceneStatic`.
- **Asset `.asset`/`.meta`:** reverter junto com o `.meta`; se GUID mudou, restaurar o `.meta` antigo
  do git (`git checkout <sha>^ -- caminho.meta`).
- **Save corrompido em teste:** restaurar o backup da suíte (`%TEMP%/loopia_tests_save.bak`, ver
  `TEST_REPORT.md` §7) ou apagar o arquivo (o jogo recomeça do zero sem travar — `DadosDoJogador.Carregar`).
- **Pacote:** reverter `Packages/manifest.json` **e** `Packages/packages-lock.json` juntos.

# Relatório G1 — ciclo da partida, chefe, vitória e derrota

Handoff da frente de gameplay (não substitui `docs/handoff/ESTADO_ATUAL.md`, que é coordenado entre as frentes).

## Status: **G1 CONCLUÍDA — validada no Editor e em Play Mode, aprovada pelo responsável em 2026-11-15**
Merge dos PRs #7 e #8 continua dependendo de autorização explícita do responsável.

Suíte inteira rodada no Editor da worktree (exclusivo desta frente), cena integrada pelo Editor, 4 voltas reais,
chefe, combate, vitória e derrota observados em Play Mode. Valores do chefe continuam **hipóteses** (medições em
`BALANCE_BASELINE.md` §8.2–8.3). G2 liberada pela aprovação, ainda não iniciada.

## Onde está
| Item | Branch / PR | Base |
|---|---|---|
| Código, testes e docs de G1 | `sprint-3/gameplay-loop`, PR #7 (rascunho) | `sprint-0/preparacao-tecnica` (`85c5276`) |
| Integração de cena + testes ajustados + este relatório | `sprint-3/gameplay-cena` (PR de cena) | `sprint-1/pass-d2-render` (`56c6bf1`, ponta da pilha de arte) + merge de `sprint-3/gameplay-loop` |
Worktree: `C:\Users\Sitr3n\Documents\Github\loopia-gameplay`. Editor: Unity 6000.3.10f1 aberto nessa pasta (bridge MCP
próprio; o Editor da pasta principal, da frente de ambiente, não foi usado).

## O que mudou na cena (`HexPrototipo.unity`, pelo Editor, com Undo + SaveScene)
- Objeto novo na raiz **`Encontro do chefe`**: `EncontroDoChefe` (referências preenchidas, `chefe` vazio → variante do
  morcego), `BarraDeVidaDoChefe`, `IndicadorDePerigoDoChefe`.
- `LoopDirector.encontro` → esse objeto; **`LoopDirector.aoVencer` → `TelaDeMorte.MostrarVitoria`** (persistent call,
  igual ao `aoGameOver`).
- `MapaDePlataformas.diretor` e `LuccaCombate.diretor` → `Diretor do Loop`.
- Campos novos serializados com o padrão: `HexLoopRunner.contarVoltas`, `InimigoSpawner.pausarSpawnComum`,
  `LoopDirector.recarregarFlechasNoChefe`.
- Diff da cena: só adições (135 linhas); nada da arte foi alterado.

## Testes atualizados de propósito
| Teste | Antes | Agora | Motivo |
|---|---|---|---|
| `HexPrototipoPlayModeTests.Quatro_Voltas_LiberamOBossUmaUnicaVez` | a 5ª volta contava (5 voltas, ×1,1⁵) | 4ª volta abre o confronto; 5ª não conta (4 voltas, ×1,1⁴) | regra de G1 com o encontro na cena |
| `EncontroDoChefePlayModeTests`, `PartidaRealPlayModeTests` | montavam o encontro em tempo de execução | usam o da cena (montam só se faltar) | integração |
Novo: `EncontroDoChefeSceneTests` (SceneStatic, 4): encontro único com barra e indicador, referências do Inspector,
`aoVencer → TelaDeMorte.MostrarVitoria`, cena abre limpa.

## Evidências (Editor da worktree, 2026-11-15)
| Execução | Job / fonte | Total | Passou | Falhou |
|---|---|---|---|---|
| Logic (antes da cena) | `cc7e31feb4f54a3598dd9cdb1d880704` | 265 | 265 | 0 |
| SceneStatic (antes da cena) | `e868e53271154cfe9a03306e66480c94` | 31 | 31 | 0 |
| PlayModeSmoke (antes da cena) | `TestResults.xml` 14:44:01Z–14:44:36Z | 13 | 13 | 0 |
| PlayModeMedicao (antes da cena) | `TestResults.xml` 14:45:46Z–14:46:27Z | 1 | 1 | 0 |
| **Logic (cena integrada)** | `e900b674b6234dc4844f63c163af06ab` | 265 | 265 | 0 |
| **SceneStatic (cena integrada)** | `a26fda7ae27c410895385cac2e24a429` | 35 | 35 | 0 |
| **PlayModeSmoke (cena integrada)** | `TestResults.xml` 14:51:56Z–14:52:40Z (job `a69763de…`) | 13 | 13 | 0 |
Os 40 Logic novos estão nos 265 (conferido por nome). Nenhum `Inconclusive`, nenhum teste ignorado.
Total da suíte na branch de integração: 265 + 35 + 13 + 1 (medição) = **314**.

**Console:** 1 erro, `ArgumentOutOfRangeException` em `UnityEditor.Search.SearchDatabase.GetDefaultSearchDatabase`
(`SearchInit.IndexationOnStartup`) — indexação da busca do próprio Unity na 1ª abertura com Library nova; não vem
do projeto e não se repetiu. Nenhum erro novo depois de todas as execuções e partidas. Avisos: só os do rastreador MCP.

**Save:** os testes restauram o `loopia_save.json` sozinhos; as partidas manuais gravaram nele e ele foi restaurado da
cópia feita antes (SHA-256 igual: `67be087c…a2f0c3`).

## Validação em Play Mode (o que o responsável pediu)
| Pedido | Evidência |
|---|---|
| 4 voltas reais | `QuatroVoltasReais_ChefeNasce_EAJornadaTermina` (NavMesh real, sem reflexão) + 4 partidas manuais: 4 voltas → `Chefe` |
| Chefe nasce (1×) | testes + capturas `*_02_chefe_nasceu` |
| Combate (flechas reais no chefe) | `Chefe_EAlvoDoTiroAutomatico_*`; métricas: 15–20 flechas no chefe por partida |
| Vitória | partidas 3, 6, 7 (§Medições); captura `hd-preparada_04_vitoria.png` |
| Derrota | partidas 1, 2, 4, 5; capturas `flechas-torre-*_04_derrota.png` ("O Lucca caiu diante do chefe.") |
| 5ª volta não conta | `QuatroVoltas_IniciamUmConfronto_EAQuintaNaoConta`; na partida real as recargas no chefe aconteceram e `VoltasCompletas` ficou 4 |
| Cartas bloqueadas no fim | `ColocacaoNoFimDaJornadaTests` + asserts nos Play; barra de cartas some na tela final (captura de vitória) |
| Vitórias persistidas e volta ao acampamento | `MorteDoChefe_VitoriaUmaVez_SalvaEVoltaAoAcampamentoComOsDados` (disco + troca de cena); manual: `vitorias` 0→1→2 no save |

## Medições do confronto (resumo; tabela completa em `BALANCE_BASELINE.md` §8.2)
- Sem cartas: **derrota 2/2** (28,8 s e 37,8 s de chefe; 15,8–22,6 s sem flechas; 1 recarga).
- Flechas + Torre: vitória 1/3 (morcegos desligados); com morcegos ligados, **2 derrotas com o chefe a 10/732** — 19
  flechas × 38 de dano, uma flecha a menos que o necessário (teto de 15 flechas).
- Flechas + Coração + Torre + Área de Treino: **vitória 2/2**.
- Jornada inteira: 2,3–2,9 min (volta de 27–34 s), abaixo da meta de 5–7 min do GDD.
Nenhum valor foi alterado: decisões de balanceamento ficam para depois de mais amostras com semente (G4).

## Achados visuais (capturas em `docs/gameplay/capturas/`)
- O chefe (esfera vermelho-escura no centro) e a barra de vida leem bem; a barra fica amarela durante o aviso.
- O disco de perigo aparece sob o chefe e o acompanha no mergulho (`hd-preparada_03_aviso_revoada.png`).
- O painel de depuração (IMGUI, canto superior esquerdo) cobre parte do anel em resoluções pequenas — HUD antigo,
  não desta sprint; registrar para a HUD definitiva.
- Mira do arco/tronco ainda não existe (plano em `APRESENTACAO_DO_CHEFE.md`, depende da frente de personagem).

## Arquivos que o Unity mexeu e que NÃO entram nos commits
`Assets/Modelos/Dressing/Materiais/Dressing_Vegetacao.mat` (ganhou `_MainTex` ao importar — material da frente de
ambiente), `ProjectSettings/TimeManager.asset` (upgrade de formato do 6000.3), `ShaderGraphSettings.asset` e
`EditorBuildSettings.asset` (só fim de linha), `.vscode/settings.json`, e os `.meta` gerados para `ArvoreGrande.fbx`,
`Morcegp.fbx`, `ArvoreGrande.png`, `Morcego.png` (o dono dos assets decide; a pilha de arte tem um commit local com eles).

## Pendências e riscos
1. Merge dos PRs #7 e #8 (autorização do responsável; #8 depende da pilha de arte #1–#6).
2. Balanceamento do chefe: mais amostras com semente fixa antes de mudar números (G4).
3. Duração da partida abaixo da meta: decisão de design.
4. Mira visual do Lucca: frente de personagem.
5. `IndicadorDePerigoDoChefe` usa `Shader.Find("Universal Render Pipeline/Unlit")`: conferir em build.
6. O PR de cena vai empilhado sobre o #6: a frente de ambiente não deve editar a `HexPrototipo` até ele entrar.

## Rollback
Reverter o PR de cena (`git revert` do commit de integração) antes do PR #7. Sem o objeto `Encontro do chefe` na cena,
o código de G1 volta ao comportamento antigo (aviso no console, voltas seguem contando).

## Checklist do PR de cena
- [x] Escopo: cena + testes ajustados + docs; arquivos que o Unity mexeu por fora excluídos (lista acima)
- [x] `.meta` de todo arquivo novo; nenhum `m_Script: {fileID: 0}`
- [x] Testes: Logic 265/265, SceneStatic 35/35, PlayModeSmoke 13/13, PlayModeMedicao 1/1 (jobs/XML acima)
- [x] Console: 1 erro externo do Unity Search na 1ª abertura; nenhum erro do projeto
- [x] Cena tocada: `HexPrototipo` (encontro, `aoVencer`, referências) — só adições
- [x] Capturas: chefe, aviso, vitória, derrota
- [x] Rollback descrito

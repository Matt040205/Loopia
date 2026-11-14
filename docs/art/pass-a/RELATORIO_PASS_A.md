# Relatorio - Pass A (Escala e Camera)

Branch `sprint-1/pass-a-escala-camera`. Unity 6000.3.10f1, URP 17.3. Nada foi commitado (o revisor commita). Todas as medidas sao do Editor
(Play Mode), nao de build. Plano: `docs/art/PLANO_PASSES.md` (Pass A); alvos: `docs/art/SCENE_BIBLE.md` secoes 2 e 5.

## 1. O que mudou (arquivo por arquivo)

| Arquivo | Mudanca |
|---|---|
| `Assets/Scripts/Hex/VisualDeIlha.cs` | `LimitesLocais` tambem considera `SkinnedMeshRenderer` (`sharedMesh.bounds` transformado pela `transform` do renderer, igual ao MeshFilter). Novo `enum EixoDeMedida { MaiorEixo, Altura, Comprimento }`, sobrecarga `Criar(prefab, pai, tamanho, topo, eixo)` e helper publico `Medida`. A assinatura antiga continua e mede o maior eixo (topo=true: x/z) como antes. |
| `Assets/Scripts/Hex/ArtScaleProfile.cs` (novo) | `ArtScaleProfile : ScriptableObject` (+ `CategoriaDeArte`, `RegraDeCategoria`, `ModeloDeArte`): tabela da Bible (secao 2) como padrao, `TryRegra`, `SortearTamanho` (Lerp(min,max)*L, 1 `NextDouble`), `Sortear` ponderado (ignora prefab nulo e peso 0), `FrenteDaCameraXZ`, `DirecaoDaCamera`; regra com `limiarDoLadoLonge`, `evitarCobrirCaminho`, `fracaoDaPegada`; perfil com `inclinacaoDaCamera`. |
| `Assets/Scripts/Hex/DecoracaoDeIlha.cs` | Novo campo `perfil`. `perfil == null` -> caminho antigo, mesma sequencia de sorteios. Com perfil: modelo ponderado, tamanho pela regra/eixo, mesmas regras (`PosicaoLivre`, corredor, 60 tentativas, sobreposicao, semente por coordenada) e filtro `Dot(p.normalized, frente) > 0.2` para categorias "so lado longe da camera". Tambem: `Destroy` virou `Descartar` (DestroyImmediate fora do Play), porque em Edit Mode `Destroy` loga erro e derrubava testes (ver 6). |
| `Assets/Scripts/Hex/HexIsoCamera.cs` | `MedirIlhas` mede so a malha do hexagono de cada ilha (`MeshFilter.sharedMesh.bounds` -> mundo), com a mesma `folga = tamanhoDoHexagono` e a mesma conta de `margem`. Independe do modelo da ilha, decoracoes e plataformas. `CentroDasIlhas` intacto. |
| `Assets/Dados/Arte/EscalaDeArte.asset` (+ pasta `.meta`) | Perfil com os 6 modelos da cena (7 entradas, ver 4). `m_Script` guid `92fb05e9...` (nao zero). |
| `Assets/Dados/Lobo.asset`, `Morcego.asset` | `escala` 1.1 -> 1.5 e 0.38 -> 0.6. |
| `Assets/Scenes/HexPrototipo.unity` | `DecoracaoDeIlha.perfil` atribuido; `HexIsoCamera.margem` 1.3 -> 1.14 (ver 3 e 5.4); `HexIsoCamera.distancia` 40 -> 70 (ver 5.1). |
| `Assets/Editor/ValidadorDeEscala.cs` (novo) | Menu `Loopia/Validar escala de arte`; `ValidadorDeEscala.Validar()` devolve `Resultado` (perfis, erros, avisos, infos, linhas). |
| `Assets/Tests/Editor/*` (5 arquivos novos) | `VisualDeIlhaTests`, `ArtScaleProfileTests`, `DecoracaoPerfilTests`, `EnquadramentoDaCameraTests` (Logic); `EscalaDeArteSceneTests` (SceneStatic); `EscalaDeArtePlayModeTests` (PlayModeSmoke). |
| `tools/visual-baseline/` | Comando `enemies` (`cs/enemies.cs`), `cs/framing.cs`, `LOOPIA_AUDIT_DIR` em `run_baseline.py` e `build_report.py` (+ `LOOPIA_REPORT_NAME`). `docs/art/audit/` nao foi tocado. |

`InimigoDef.escala` so e lido por `Inimigo.MontarVisual` (grep em `Assets`, fora dos diretorios legados): so visual, collider da primitiva continua destruido.

## 2. Tabela de aceite (1920x1080, Editor)

| Metrica | Antes | Depois | Alvo | OK? |
|---|---|---|---|---|
| orthoSize 12345 / 7 / 2024 (16:9) | 22.484 / 24.335 / 21.501 | 16.829 / 18.559 / 15.966 | menor | sim |
| orthoSize em 21:9 (12345 / 7 / 2024) | iguais ao 16:9 | 16.829 / 18.452 / 15.966 | - | - |
| Lucca (altura, px) seed 12345 / 2024 / 7 | 33.1 / 34.6 / 30.6 | 44.2 / 46.6 / 40.1 (varia ~2 px com a rotacao do Lucca: AABB) | >= 40 / >= 40 / >= 36 | sim |
| Topo da ilha (largura mediana, px) 12345 / 7 / 2024 | 95.4 / 88.1 / 99.7 | 127.4 / 115.6 / 134.3 | >= 115 | sim (seed 7 por 0.6 px) |
| Arvore grande (altura AABB, px) | 20 max (arvore unica) | min-max: 83.7-99.6 (12345), 70.4-86.9 (7), 86.7-107.1 (2024); arvore pequena 42-68 | >= 45 | sim |
| Arvore grande ocultando o caminho (teste AABB, 12 sementes) | n/a | **0 de 2160 pontos** (110 arvores grandes; sem a regra: 104 pontos ocultos, ver 5) | 0 | sim |
| Lobo / morcego (px, seed 12345) | 312 / 14 | 46.8 / 30.3 | 30-50 / >= 20 | sim |
| Ortho com x sem decoracao | 0% | 0% (teste: 3 sementes x 16:9/21:9, + outro perfil + modelo da ilha) | 0% | sim |
| Anel inteiro visivel 16:9 e 21:9 (menor distancia de qualquer canto de bounds do modelo da ilha, com a base pendurada, ate a borda) | n/a | 81.3 / 82.6 / 82.1 px (12345 / 7 / 2024 em 16:9); 80.0 px (seed 7 em 21:9); decoracoes >= 144 px | dentro do quadro | sim; **HUD real: pendente manual** |
| Profundidade camera -> anel (z, relevante para a nevoa) | 49.0 a 83.0 | 51.5 a 85.1 (seed 12345; +2.5 por `distancia` 70 contra 67.5 antigo; sem a correcao seria ~31 a ~65) | ~igual | sim |
| Batches / tris / shadowCasters, MESMA sessao (seed 12345 h12) | zoom antigo equivalente: 1326-1338 / 1.14-1.16 M / 122 | `distancia` 70: 1235-1248 / 1.11-1.12 M / 58 (-6% batches, -3% tris) | <= +10% | sim, relativo (ver 5) |
| Batches / tris contra o numero absoluto do baseline (625 / 909 k) | 625 / 909 k | 1244 / 1.117 M | <= +10% | **nao comparavel**: ver 5 |
| Suite | 119/119 | 200/200 (196 + 4) | 100% | sim |
| Console (erros) | 0 | 0 | 0 | sim |

Fontes: `framing_depois.json` (px, distancias), `MEDIDAS_DEPOIS.md` + `_raw_seed*.json` (decoracoes, Lucca, inimigos, profundidades), `framing_sweep_margem.json`
(varredura 1.1 a 1.3), `stats_mesma_sessao.json`, `oclusao_arvores.json`, `enemies.json` em `antes/` e `depois/`.

## 3. Camera: por que margem 1.14 e nao 1.2

Varredura (`framing_sweep_margem.json`, 1920x1080): com a formula nova, margem 1.2 deixa a seed 7 em Lucca 35.6 px (< 36) e topo 109.8 px (< 115).
Topo >= 115 na seed 7 exige margem <= 1.146; 1.14 da Lucca 38.4 / topo 115.6 nessa seed. Distancia minima do modelo da ilha (incluindo as
bases pendentes das ilhas da fileira de baixo) ate a borda: 80-83 px em 1.14 (104-106 px em 1.2; 63-66 px em 1.1). Nao precisou de complexidade extra.
Pinagem de regressao: `EnquadramentoDaCameraTests` (tolerancia 0.02): 12345 -> 16.829/16.829, 7 -> 18.559/18.452, 2024 -> 15.966/15.966 (16:9/21:9).

## 4. Dados

`EscalaDeArte.asset`: `alturaDeReferencia` 1, `giroDaCamera` 45, regras da Bible (tabela completa, ArvoreGrande 1.8-2.4 Altura so lado longe).
Modelos: Arvore -> ArvorePequena (peso 2) e ArvoreGrande (peso 1, mesmo prefab); Pedra -> Pedra; CogumeloMarrom e CogumeloVermelho -> Cogumelo;
TroncoDeMadeira (0.43 x 0.31 x 1.11, deitado) -> Tronco (Comprimento); **CepoDeMadeira (0.44 x 0.13 x 0.46: disco baixo, nem vertical nem deitado) -> `Outro`,
cuja regra neste asset foi ajustada para 0.3-0.5 MaiorEixo** (o padrao da classe continua 0.28-0.65).
Lobo: com a correcao dos bounds o prefab (skinned) mede 0.536 x 0.626 x 1.0 por unidade de maior eixo (malha); `escala` 1.5 da altura 0.94 / comprimento 1.50
pela malha e 0.86 / 1.59 pelos bounds do renderer (`MEDIDAS_DEPOIS`) - ambos dentro da Bible (0.8-1.1 altura, <= 1.6 comprimento). Escolhi 1.5, nao a escala
que daria exatamente 0.9 de altura (1.66), porque o comprimento passaria de 1.6. Morcego (forma simples, sem prefab): 0.6.

## 5. Achados, decisoes e pendencias

1. **Neutralidade de iluminacao (`distancia` 70).** A camera ortografica fica a `max(distancia, 3 * orthoSize)`; com o zoom novo isso caiu de 67.5 para ~50 e o anel entrou na
   `ShadowDistance` do URP (50) (shadowCasters 12 -> 136-147 e +20% de batches na primeira sessao). Para ortografica o enquadramento nao muda, entao a cena ganhou `HexIsoCamera.distancia = 70`
   (formula inalterada). Medicao na mesma sessao (`stats_mesma_sessao.json`): distancia 40 (sem correcao) 1631-1636 batches / 442-447 casters; 60: 1440 / 256; 67.5: 1276 / 86;
   **70: 1235-1248 / 58**; 80: 1188-1196 / 3; zoom antigo equivalente (perfil desligado, margem 1.523): 1326-1338 / 122. Com 70, batches e tris ficam -6% / -3% contra o zoom antigo e
   os casters ficam abaixo dele. Nao toquei em URP/luz.
   **Limite da medicao:** os numeros *absolutos* mudaram entre sessoes do Editor sem mudanca de codigo (o mesmo "zoom antigo" deu 627 batches / 12 casters na primeira sessao e ~1333 / 122 depois; nao
   achei a causa; apontar a camera da Scene view para o vazio nao mudou nada). Por isso nao da para afirmar "+-10% de 625 / 909 k" em valor absoluto; a comparacao valida e a da mesma sessao.
   Pendente: medir em build (o numero do Editor nao e prova).
   Efeito colateral inerente: `CenarioContinuo` reescala o fundo para cobrir a tela, entao com menos zoom-out o fundo ficou menor (bounds 135 u contra 180 u); nao toquei nele.
2. **Arvores grandes nao ocultam o caminho (alvo 0, conseguido).** Teste: raio da camera ate 6 pontos por trecho do anel a 0.5 u de altura contra AABB dos renderers das arvores grandes, 12 sementes
   (12345, 7, 2024, 1-6, 8-10; 2160 pontos). Tentativas na ordem pedida (arvores grandes / pontos ocultos nas 12 sementes):
   - base (limiar 0.2, altura 1.8-2.4, peso 1): 148 / 104
   - (a) limiar de lado longe 0.5: 143 / 102 (sobram so os vertices que cobrem o corredor da ilha de tras)
   - (b) altura 1.6-2.0 (limiar 0.2): 148 / 82; (a)+(b): 149 / 84
   - (c) peso 0.25 (com a+b): 41 / 9; peso 0.1: 7 / 0 (zero so porque quase nao sobram arvores grandes: 7 em 12 sementes, nenhuma em 9 delas)
   - **(d) escolhido, fora da lista pedida: `RegraDeCategoria.evitarCobrirCaminho`** (so ArvoreGrande): ao escolher a posicao, rejeita as que cobririam, vistas pela camera
     (`inclinacaoDaCamera` 30 + `giroDaCamera` 45 no perfil), algum ponto dos 6 corredores ate 2 ilhas de distancia (caixa alinhada ao mundo, folgada para qualquer rotacao em Y).
     Resultado: **110 arvores grandes, 0 pontos ocultos**, mantendo altura 1.8-2.4, peso 1 e limiar 0.2. O limiar virou campo (`limiarDoLadoLonge`, padrao 0.2 = comportamento original) e ficou 0.2.
   `inclinacaoDaCamera` tem que igualar `HexIsoCamera.inclinacao` (teste de cena).
3. `RegraDeCategoria.fracaoDaPegada` (nao estava na especificacao): com a pegada = meia-diagonal da caixa inteira nenhuma arvore de 1.1 u ou mais cabe em `PosicaoLivre` (limite pratico ~0.35-0.4 nos vertices),
   entao a ArvoreGrande nunca seria colocada. A fracao (ArvoreGrande 0.35, ArvorePequena 0.4, demais 1) multiplica so a pegada usada para reservar espaco; `PosicaoLivre`, corredor e as 60 tentativas ficaram.
4. **`margem` 1.14 (e nao ~1.2).** Com 1.2 a seed 7 fica em Lucca 35.6 px (< 36) e topo 109.8 px (< 115); topo >= 115 na seed 7 exige margem <= 1.146. Menor distancia ate a borda: 80-83 px (104-106 px em 1.2).
5. **CepoDeMadeira -> `Outro`** (disco 0.44 x 0.13 x 0.46, nem vertical nem deitado), com a regra `Outro` deste asset em 0.3-0.5 MaiorEixo (padrao da classe 0.28-0.65). TroncoDeMadeira -> Tronco (Comprimento).
6. **Lobo `escala` 1.5**, nao os 1.66 que dariam altura 0.9: o comprimento passaria de 1.6. Com 1.5: 0.94 x 1.50 pela malha e 0.86 x 1.59 pelos bounds do renderer (Bible: altura 0.8-1.1, comprimento <= 1.6). Morcego 0.6.
7. **`Destroy` -> `DestroyImmediate` fora do Play** em `DecoracaoDeIlha`: em Edit Mode `Destroy` loga erro e derrubaria os testes; em Play o resultado e identico (o teste de fallback prova posicoes, rotacoes, tamanhos
   e modelos iguais ao algoritmo antigo).
8. Lado "longe da camera": a camera olha para `+Euler(0, giro, 0) * forward`; longe = produto escalar positivo (confirmado por captura e por teste). `InimigoDef.escala` so e lido por `Inimigo.MontarVisual`.
9. Incidente de processo: depois das varreduras de oclusao o `.unity` em disco apareceu regravado com o estado de Play Mode (nao descobri qual passo gravou; nenhum dos meus scripts chama `SaveScene` fora do ajuste de campos).
   Corrigi: cena vazia no Editor, `git checkout` do arquivo, reabrir, reaplicar so os 3 campos e gravar uma vez. Diff final conferido (ver 7). Vale o revisor conferir `git diff --stat` da cena.
10. Pendente: validar o enquadramento com o HUD real (mao de cartas) em 1920x1080 e 2520x1080; desempenho em build; "<= 20% do topo da ilha vizinha" para arvores grandes nao foi medido.
11. `docs/art/pass-a/antes/enemies_*`: lobo de 7.65 u confirmado visualmente; no "antes" o Lucca nao aparece no close-up. No "depois" `enemies.cs` leva o Lucca ao acampamento antes de medir. Numeros do "antes" nao foram regerados.

## 6. Testes (evidencia real)

| Execucao | Job / fonte | Total | Passou | Falhou |
|---|---|---|---|---|
| Logic + SceneStatic (final) | job `338f3c5499d14f0aae0c523f29f6c94d` | 196 | 196 | 0 |
| PlayModeSmoke (final) | job `075ba9fd76d548c190bac0504da756db` (travou como esperado); `TestResults.xml` 2026-11-14 16:05:02 -03: `total=4 passed=4` | 4 | 4 | 0 |

(Uma execucao intermediaria, job `2b620f7e365046ceba03b2ddeffc1b15`, deu 195/196: o teste antigo `ParametrosSerializados_...` falhou por causa do `.unity` regravado em disco com estado de Play, ver 5.9; passou depois de restaurar a cena.)
Rastreador do MCP destravado com `clear_stuck_test_job.json` (`result: true`). 119 testes antigos passam sem alteracao. 81 novos: VisualDeIlha 15, ArtScaleProfile 11, DecoracaoPerfil 37
(fallback identico ao algoritmo antigo, 11 sementes x {regras de colocacao, >= 1 decoracao por ilha}, **12 sementes x oclusao = 0**, determinismo, faixas), Enquadramento 9, SceneStatic 8, PlayModeSmoke 1.
Nas 11 sementes (1-10 e 12345): 0 ilhas sem decoracao, nenhuma invade centro/corredor/borda, nenhuma sobreposicao, arvores grandes so no lado longe (limiar da regra).

## 7. Diff da cena (campo a campo)

`git diff --stat`: 1 arquivo, 13 linhas (7 + / 6 -). Campos:
```
-  margem: 1.3
+  margem: 1.14
-  distancia: 40
+  distancia: 70
+  perfil: {fileID: 11400000, guid: 23008b90659e545428d3eb95b2dc19aa, type: 2}
```
Mais: 3 linhas do serializador (`m_Name:` e `value:` ganham espaco no fim; inevitavel sem editar o YAML a mao) e o `m_LocalPosition` da Main Camera
(`-24.49, 20.0, -24.49` -> `-42.87, 35.0, -42.87`: o `OnValidate` do `HexIsoCamera` reposiciona a camera para a nova `distancia`; a projecao ortografica nao muda).
`Cenario background` e `Rio de fundo` (`[ExecuteAlways]`, se reposicionam sozinhos) saem da gravacao com posicao/escala ligeiramente diferentes; devolvi os dois Transforms aos valores do HEAD na mesma chamada, antes de gravar.

## 8. Validador (`Loopia/Validar escala de arte`): 1 perfil, 0 erros, 0 avisos, 11 infos
```
INFO Arvore (ArvorePequena): bruto 1.193 x 2.420 x 1.211; medido em Altura de 1.10 a 1.50 u
INFO Arvore (ArvoreGrande): idem; medido em Altura de 1.80 a 2.40 u
INFO Pedra: pivo longe da base (min.y = -0.103 para altura 0.241); VisualDeIlha poe a base em y = 0
INFO Pedra: bruto 0.332 x 0.241 x 0.384; MaiorEixo de 0.30 a 0.80 u
INFO CogumeloMarrom / CogumeloVermelho: Altura de 0.20 a 0.45 u
INFO CepoDeMadeira (Outro): bruto 0.440 x 0.128 x 0.463; MaiorEixo de 0.30 a 0.50 u
INFO TroncoDeMadeira: pivo longe da base (min.y = -0.143 para altura 0.313); bruto 0.429 x 0.313 x 1.111; Comprimento de 0.50 a 1.00 u
INFO Lobo.asset: escala 1.50 => 0.80 x 0.94 x 1.50 u (x, y = altura, z = comprimento; pela malha)
INFO Morcego.asset: sem prefab; forma simples de lado 0.60 u
```
(resumo; a saida completa, uma linha por entrada, e o log do menu ou `ValidadorDeEscala.Validar().ToString()`.)

## 9. Capturas

- Antes: `docs/art/baseline/` (18 JPG + manifest, ja existentes) e `docs/art/pass-a/antes/` (`enemies_*` normal + close-up, `enemies.json`).
- Depois: `docs/art/pass-a/depois/` (18 JPG da mesma matriz 3 seeds x 12/18/22 h x 1920x1080/2520x1080 + `manifest.json`, `enemies_*` normal e close-up, `enemies.json`).
- Medidas: `docs/art/pass-a/MEDIDAS_DEPOIS.md`, `medidas.json`, `_raw_*.json` (incluindo `_raw_stats.json`), `framing_*.json`, `oclusao_arvores.json`.

## 10. Save e estado final

SHA-256 de `loopia_save.json`: antes `38433742f697c2bc9e7f7e8b482fd7d6315b4b5efeff8e39469c7287ba4508ac`; depois (restaurado de copia byte a byte)
`38433742f697c2bc9e7f7e8b482fd7d6315b4b5efeff8e39469c7287ba4508ac`. Play Mode parado, `timeScale = 1`, cena aberta sem alteracoes pendentes.

## 11. Rollback

`git revert` dos commits do Pass A; ou, so para voltar ao visual antigo das decoracoes, desatribuir `DecoracaoDeIlha.perfil` na cena (fallback testado como identico ao algoritmo antigo);
para o enquadramento antigo, reverter `HexIsoCamera.cs` (1 arquivo) e `margem` 1.3; para o lobo, reverter `VisualDeIlha.cs` e `Lobo.asset`/`Morcego.asset`.

## 12. Revisao de qualidade (analista, 2026-11-14)
- **Reexecucao independente:** Logic+SceneStatic 196/196 (job `8fd13927084946db977c0362c9c771a2`); PlayModeSmoke 4/4
  (`TestResults.xml` 2026-11-14 19:08:20Z). Console 0 erros; cena sem alteracoes pendentes; save com SHA-256 inalterado.
- **Diff da cena conferido:** so `margem`, `distancia`, `perfil`, a posicao da Main Camera recalculada pelo `OnValidate`
  e 3 linhas de espaco em branco do serializador. Fundo e rio nos valores do HEAD.
- **Revisao pedida e aplicada:** (1) `distancia = 70` para o Pass A nao alterar sombras/nevoa (decisao de luz fica no
  Pass D); (2) oclusao do caminho zerada com `evitarCobrirCaminho` (teste conservador por raio, independe dos vizinhos,
  protege tambem ilhas criadas depois por cartas).
- **Ressalvas registradas:**
  - Contagem absoluta de batches do Editor variou entre sessoes sem mudanca de codigo (625 → ~1330 para a mesma
    configuracao). So comparacoes na mesma sessao valem; a baseline de desempenho real continua pendente (build).
  - `EixoDeMedida.Altura` num modelo achatado (altura ~0) gera escala muito grande (`tamanho / 0.001`). Nenhum modelo
    atual cai nisso; o validador deve passar a sinalizar altura < 5 % da maior dimensao (pendencia pequena).
  - Validacao da HUD em 1920x1080/2520x1080 reais continua manual (responsavel).

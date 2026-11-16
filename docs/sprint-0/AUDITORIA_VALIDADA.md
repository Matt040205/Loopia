# Auditoria estática — validação contra o código (Sprint 0)

Base: `main` @ `0b9513e` (2026-10-09). Validação por leitura de código/YAML e pelo Editor aberto
via MCP em 2026-11-14. "Confirmado" = conferido no arquivo; nada aqui foi medido em build.

| # | Achado da auditoria | Veredito | Evidência / nuance |
|---|---|---|---|
| 1 | Gameplay ativo em `Scripts/Hex`; legado em outras pastas | Confirmado | Nenhum script legado referencia `Loopia.Hex` e vice-versa. **Nuance:** `testes shaders/Daynightcontroller.cs` (namespace global) está ativo na `HexPrototipo`. |
| 2 | Gerador: raio 4–6, deformação, validação de ciclo, miolo do boss | Confirmado | `HexLoopGenerator.cs`: 40 tentativas, `deformacoes=400`, `mioloMinimo=9` + exige 1 casa com 6 vizinhas no miolo. Semente fixa existe (`usarSementeFixa`, desligada na cena). |
| 3 | Cena: tamanho 3, folga 0.3, espessura 0.6, base ≈4,2 | Confirmado | `HexPrototipo.unity` linhas ~3753–3755. Defaults do script são 1 / 0.06 — os testes não devem assumir defaults. |
| 4 | `VisualDeIlha.LimitesLocais` só considera `MeshFilter` | Confirmado | Modelo só com `SkinnedMeshRenderer` gera bounds vazios → escala `tamanho/0.001`. Nenhum script Hex trata `SkinnedMeshRenderer`. |
| 5 | Decoração normalizada em 0,28–0,65 para qualquer prop | Confirmado | `DecoracaoDeIlha.cs:27`, pelo maior eixo. `PosicaoLivre` reserva centro e 6 corredores — preservar. |
| 6 | `HexTile._arte` coletado antes da decoração | Confirmado | `HexWorld.CriarIlha`: `Initialize` (l.131) antes de `Decorar` (l.134). Decorações não recebem o tint por MPB. O modelo da ilha (criado antes) recebe. |
| 7 | Câmera reenquadra a cada 0,25 s por `Renderer.bounds` | Confirmado | `HexIsoCamera.MedirIlhas` usa `GetComponentsInChildren<Renderer>` das ilhas (inclui decoração). `MapaDePlataformas.AtualizarRelevo` também força `Enquadrar()`. |
| 8 | DayNight escreve fog/ambient em `Update`; conflito com futuro clima | Confirmado | `Daynightcontroller.cs:124–136`. Cena: `timeOfDay 14.8`, avanço automático ligado. |
| 9 | Nuvens: 18 instâncias, 24 amostras/pixel | Confirmado | `CamadaDeNuvens.quantidade=18`; `SoftCloud.shader:139` loop de 24. Custo **não medido**. |
| 10 | URP: SRP Batcher, depth/opaque, SSAO, Volume | Confirmado | Qualidade ativa PC → `PC_RPAsset` (SRP Batcher, depth e opaque ligados), SSAO no `PC_Renderer`. Pacote legado `com.unity.postprocessing` coexiste com URP. Ver `BASELINE.md` §8. |
| 11 | 4 cartas com `modelo`, 7 sem | Confirmado | `IlhaAlta` com `modelo: {fileID: 0}`; Floresta, Torre, Cabana, AreaDeTreino, Basica, CampoPacifico sem o campo. Nenhum `m_Script` quebrado. Ver `BASELINE.md` §7. |
| 12 | `MaoDeCartas`: mão cheia descarta compra do level-up | Confirmado | `Comprar()` devolve `false` sem fila (l.139–140); `SubiuDeNivel` ignora o retorno. Coberto por teste de caracterização. |
| 13 | `LoopDirector`: boss não implementado; save só moedas/recorde | Confirmado | `AoLiberarBoss` sem assinantes no código Hex; `DadosDoJogador.Save` tem só `moedas`, `recordeDeVoltas`, sem `saveVersion`. |
| 14 | `BuildNavMesh()` síncrono; só relevo deveria rebake | Confirmado | `NavegacaoDoMundo.Reconstruir` (gerar mapa) e `MapaDePlataformas.AtualizarRelevo` (só quando altura muda). Surface: `CollectObjects=Children`, `UseGeometry=PhysicsColliders` → arte com colliders desligados fica fora. |
| 15 | Sem testes automatizados | Confirmado (até esta sprint) | Nenhum asmdef/teste existia. Test Framework 1.4.6 instalado. |
| 16 | `unity-mcp` via `#main` | Confirmado | `Packages/manifest.json`. |

## Divergências e achados novos
- **Commit `0b9513e` sem `.meta`:** `ArvoreGrande.fbx`, `Morcegp.fbx` (nome com erro de digitação) e
  duas PNGs entraram sem `.meta`; cada máquina gera GUIDs próprios. Quem for usar esses modelos em
  prefab/cena precisa que um único `.meta` seja commitado antes (decisão do dono dos assets).
- **Asmdef é risco, não só higiene:** 4 UnityEvents serializados em cena citam
  `Assembly-CSharp`. Por isso os testes da Sprint 0 ficaram em `Assembly-CSharp-Editor`.
- **Save compartilhado Editor/Build:** testes ou Play Mode alteram o save real do desenvolvedor;
  a suíte faz backup/restauração.
- **Mais scripts fora de `Loopia.Hex` ativos na cena:** além do `Daynightcontroller`, o
  `CloudController` (`Assets/testes shaders`, 6 referências na `HexPrototipo`).
- **Versões resolvidas ≠ manifest:** `com.unity.test-framework` pedido 1.4.6, resolvido 1.6.0 no
  `packages-lock.json` (provável versão mínima do Editor 6000.3); `multiplayer.center` 1.0.0 → 1.0.1.
- **Build Settings** ainda inclui Menu, Testes, GameOver e Win (legado) habilitadas.
- **Save** fica em `LocalLow/DefaultCompany/GameJam13-06/` (productName antigo do jam).
- **Enter Play Mode Options** habilitado com valor 0 (recarrega domínio e cena) — testes não
  dependem de estado estático sobrevivendo ao Play.

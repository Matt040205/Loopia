# Problemas restantes — Sprint de estabilização do MVP

O que ficou em aberto, por quê e o que fazer a seguir. Atualizado ao fim de cada bloco.

| Prioridade | Item | Situação | Próximo passo |
|---|---|---|---|
| Validação | Jornada jogada nas builds `a7a5c56` e **`6dd6206`** (Bloco C); derrota numa build nova | vitória validada na `7bf6492`; a `a7a5c56` difere só pela moeda e a `6dd6206` só no visual do Lucca e no fade (cobertos por PlayModeSmoke); só fumaça nas duas; derrota validada no fallback | roteiro em `BUILD_VALIDATION.md` §2.2 e §4 |
| **Saves** | Outras frentes (worktrees `loopia-cards`, `loopia-hud-preview`, Editors abertos 21:48 e 22:41) gravaram no save do jogador: `Loopia\loopia_save.json` 209 → 210 (15/11 21:41) e `GameJam13-06\loopia_save.json` 209 → 212 (22:09) | não foi esta sessão; **não restaurado** sem confirmação do responsável | decidir se volta a `3d27854e…` (cópia em `Documents\Loopia_Recovery_20261115\save\`); orientar as outras frentes a usar `-loopia-perfil` ou backup |
| P2 | Mira correndo com o alvo bem atrás (> ~95°): o corpo só gira 30° correndo para não deslizar os pés | parado (Mirante) corrigido: ~99° → ~15°; de lado (chefe) corrigido | clipes de corrida lateral/de costas no Blender (com backup) ou IK de braço; sem eles, mais giro = mais deslize |
| P2 | Contato do arco com a túnica/saia na mistura de tiro | não mexido (não piorou: o tronco continua ≤ 65°) | editar o clipe Bow Shot no Blender com backup; varredura de ângulos no Unity |
| P2 | Deslize do pé numa curva longe de pulo (4% das amostras, 4,86 → 5,61 u/s) e no pouso (~0,95) | antes do pulo corrigido (3,05 → 0,76); estes ficaram | IK de pé (travar o apoio) seria o próximo passo; medir com `ApoioDosPesMedicaoTests` |
| Medição | Custo de GPU do MSAA 4× (1080p e 4K) | pendente: a medição no Editor derrubou a Unity | medir no build com um render por frame; em GPU fraca, alternativa MSAA 2× |
| Medição | Desempenho em build (pendente desde o Pass D2) | pendente | fps/frame time na build Release |
| Ferramenta | Play Mode do Editor grava no save do Editor | mitigado (backup/restauração nas ferramentas; perfil `-loopia-perfil` só para builds) | se incomodar, perfil automático no Editor em PR próprio |
| Ferramenta | RNG de spawn, Revoada e compra de cartas sem semente | limitação documentada | `System.Random` injetável (G4) antes de qualquer conclusão estatística |
| Design | Jornada de ~2,5 min (anel 24–30) contra a meta de 5–7 min do GDD | aberto, decisão do responsável | mais voltas, anel maior ou Lucca mais lento — não mexido nesta sprint |
| Balanceamento | Semente 2 (anel de 30) perdeu em todos os cenários medidos | observado, amostra pequena | medir com RNG semeado (G4) antes de mexer em número do chefe |
| Fallback antigo | A build `mvp-20261117-dev-55442a5` ainda usa a pasta `GameJam13-06`: depois da migração, os dois saves andam separados | esperado | usar só as builds novas; o fallback continua para emergência |

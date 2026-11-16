# Problemas restantes — Sprint de estabilização do MVP

O que ficou em aberto, por quê e o que fazer a seguir. Atualizado ao fim de cada bloco.

| Prioridade | Item | Situação | Próximo passo |
|---|---|---|---|
| Validação | Jornada jogada na build **final** `a7a5c56`; derrota numa build nova | vitória validada na `7bf6492` (difere só pela correção da moeda, coberta por testes); acesso à build final negado; derrota validada no fallback | roteiro em `BUILD_VALIDATION.md` §2.2 |
| **Saves** | Outras frentes (worktrees `loopia-cards`, `loopia-hud-preview`, Editors abertos 21:48 e 22:41) gravaram no save do jogador: `Loopia\loopia_save.json` 209 → 210 (15/11 21:41) e `GameJam13-06\loopia_save.json` 209 → 212 (22:09) | não foi esta sessão; **não restaurado** sem confirmação do responsável | decidir se volta a `3d27854e…` (cópia em `Documents\Loopia_Recovery_20261115\save\`); orientar as outras frentes a usar `-loopia-perfil` ou backup |
| P2 | Mira do Mirante limitada a 65° de yaw (arco não aponta para alvos atrás) | não iniciado: liberado (há build validada), aguardando decisão de começar | ver `KNOWN_ISSUES.md`; avaliar distribuir rotação entre tronco/ombros e feedback de trajetória |
| P2 | Deslizamento dos pés (P95 2,39 u/s) e contato do arco com túnica/saia | não iniciado (Bloco C) | sincronia Blend Tree × fase dos clipes antes de mexer no Blender |
| P2 | Fim de partida em Idle, sem pose de vitória/derrota | não iniciado (Bloco C) | pose/estado existente reaproveitado, sem atrasar a troca de cena |
| Medição | Custo de GPU do MSAA 4× (1080p e 4K) | pendente: a medição no Editor derrubou a Unity | medir no build com um render por frame; em GPU fraca, alternativa MSAA 2× |
| Medição | Desempenho em build (pendente desde o Pass D2) | pendente | fps/frame time na build Release |
| Ferramenta | Play Mode do Editor grava no save do Editor | mitigado (backup/restauração nas ferramentas; perfil `-loopia-perfil` só para builds) | se incomodar, perfil automático no Editor em PR próprio |
| Ferramenta | RNG de spawn, Revoada e compra de cartas sem semente | limitação documentada | `System.Random` injetável (G4) antes de qualquer conclusão estatística |
| Design | Jornada de ~2,5 min (anel 24–30) contra a meta de 5–7 min do GDD | aberto, decisão do responsável | mais voltas, anel maior ou Lucca mais lento — não mexido nesta sprint |
| Balanceamento | Semente 2 (anel de 30) perdeu em todos os cenários medidos | observado, amostra pequena | medir com RNG semeado (G4) antes de mexer em número do chefe |
| Fallback antigo | A build `mvp-20261117-dev-55442a5` ainda usa a pasta `GameJam13-06`: depois da migração, os dois saves andam separados | esperado | usar só as builds novas; o fallback continua para emergência |

# Análise de Requisitos — Loopia

**Projeto:** Loop (Unity 2D/3D game with hex grid, auto-movement, combat, island card building)  
**Origem dos requisitos:** `Loopia programaçao.pdf` (2 páginas)  
**Data da análise:** 2026-09-16

---

## 1. Outcome (Problema e Objetivo)

O PDF define um jogo completo com **3 grandes sistemas**:

| Sistema | Escopo | Status atual |
|---------|--------|-------------|
| **Movimentação** | Grid hexagonal, auto-mover no loop, pular gaps configuráveis | ~60% — funciona mas grid é quadrado 3x3, não hexagonal |
| **Combate** | Inimigos voadores (morcegos), flechas com dano/munição/velocidade configuráveis, XP por kill, level-up, drop de cartas | ~70% — sistema funcional mas sem voo, sem investida, sem escalonamento de stats entre loops |
| **Cartas de Ilhas** | Sistema de drag-and-drop para construir plataformas com efeitos e adjacências | ~50% — placeholder básico funciona, mas plataformas complexas faltam |

### Aceitação mínima para "concluir as requisições":
- Todos os 3 sistemas devem funcionar em jogo jogável dentro da cena Game
- Cada requisito do PDF deve ser mapeado a código existente OU plano de implementação

---

## 2. Mapeamento Detalhado: Requisito → Código Existente → Gap

### MOVIMENTAÇÃO

| # | Requisito do PDF | Arquivo Responsável | Status | Gap |
|---|-----------------|---------------------|--------|-----|
| M1 | Grid hexagonal com loop (não se cruza) | `LoopGenerator.cs` | ⚠️ Parcial | Gera grid quadrado 3x3, não hexagonal. Precisa converter para coordenadas hex (axial/cube). |
| M2 | Ilhas básicas entre caminhos com gap pulável (valor configurável) | `LoopGenerator.cs`, `PlataformaTrampolim.cs` | ⚠️ Parcial | Gaps existem mas são teleportados, não physics jump. Valor de altura fixo no trampolim, não facilmente editável no inspector como "gap distance". |
| M3 | Ilha de início/fim do loop onde jogador começa | `PlataformaLar.cs`, `LoopGenerator.cs` | ✅ Implementado | Plataforma Lar é instanciada como posição 0. Funciona. |
| M4 | Movimentação automática no caminho/loops, pulando espaços se necessário | `LoopMover.cs` | ⚠️ Parcial | Auto-movement funciona perfeitamente. Pulo é teleportação, não physics-based. Precisa trocar para Rigidbody jump. |

### COMBATE

| # | Requisito do PDF | Arquivo Responsável | Status | Gap |
|---|-----------------|---------------------|--------|-----|
| C1 | Inimigos voadores (morcegos) que atacam ao chegar perto (esfera configurável) com investida | `InimigoBase.cs`, prefab `Inimigo.prefab` | ⚠️ Parcial | Inimigos existem, raio configurável (`raioDeDeteccao = 5f`). Mas: NÃO voam, NÃO fazem investida (charge). Prefabs de morcego/animacao faltam. |
| C2 | Flechas com X dano, Y munição, Z velocidade — facilmente alteráveis | `PlayerAttack.cs`, `PlayerStatus.cs` | ✅ Funcional (parcial) | Valores são públicos e editáveis no inspector: `tempoEntreAtaques=1f`, `flechas=10`. Dano vem de `PlayerStatus.GetDano() = danoBase * nivel`. Falta: campo explícito para velocidade do arco. |
| C3 | XP por kill (apenas no loop, não global), level-up 0→1=100X, próximos +75%, drop cartas | `PlayerStatus.cs`, `InimigoBase.cs` | ⚠️ Parcial | XP funciona mas: fórmula atual é `fatorCrescimentoXP = 1.5f` (deve ser 1.75 = +75%). XP não é resetado entre loops (não há "apenas no loop"). Drop de cartas funciona via `AdicionarCartaAleatoria()`. |
| C4 | Jogador a 0 HP → morre → vai para menu principal | `PlayerStatus.cs`, `GameManager.cs` | ⚠️ Parcial | `onDeath` evento existe mas não está conectado ao `GameOverManager.IrParaGameOver()` nem ao `LoadMainMenu()`. |
| C5 | Stats dos inimigos aumentam % ao fim de cada loop | `GameManager.cs` | ✅ Implementado (parcial) | `multiplicadorDeStatus` escala a cada loop. Inimigos usam `GameManager.Instance.multiplicadorDeStatus` no Awake. Mas: não incrementa automaticamente — precisa chamar em `AvancarLoop()`. |
| C6 | Após N loops, chefão aparece no centro do loop | (Nenhum) | ❌ Não implementado | Zero código para boss spawning. Precisa criar `BossManager.cs` ou similar. |

### CARTAS / ILHAS

| # | Requisito do PDF | Arquivo Responsável | Status | Gap |
|---|-----------------|---------------------|--------|-----|
| I1 | Cartas aparecem no HUD embaixo, arrastar e soltar na grid compatível | `UICardManager.cs`, `UIPlatformCard.cs`, `PlacementManager.cs` | ⚠️ Parcial | Click-to-place funciona via raycast. Drag-and-drop real não implementado (mas click é funcional). Grid de compatibilidade básica existe via `PlataformaAlvo`. |
| I2 | Plataforma básica — instanciada no início, pode ser substituída | `PlataformaBase.cs`, `base.prefab` | ✅ Implementado | Funciona como marcador vazio. Pode receber carta via PlacementManager. |
| I3 | Barraca (caminho) — não removível, restaura vida+munição ao voltar, inimigos +10% PV/ATK/XP | `PlataformaLar.cs` | ⚠️ Parcial | Restaura flechas mas NÃO restaura vida. Não aumenta stats dos inimigos em 10%. Deveria ser imutável (proteção existe no PlacementManager). |
| I4 | Plataforma com blocos — destruíveis com cabeçada, dropam corações/flechas/moedas, recriados ao fim do loop | (Nenhum) | ❌ Não implementado | Zero código para blocos destrutíveis. Precisa criar `PlataformaBlocos.cs` e `BlocoDestrutivel.cs`. |
| I5 | Plataforma com coração — coleta ao passar | (Nenhuma) | ❌ Não implementada | Precisa criar `PlataformaCoracao.cs`. |
| I6 | Plataforma com flechas — coleta ao passar | `PlataformaFlecha.cs` | ✅ Implementado | Funciona: coleta e destrói a plataforma. `quantidadeDeFlechas=5` configurável. |
| I7 | Plataforma com moedas — coleta ao passar | (Nenhuma) | ❌ Não implementada | Precisa criar `PlataformaMoeda.cs`. |
| I8 | Vazio — remove plataforma, recompensa seguinte dobra | `PlataformaVazia.cs` | ⚠️ Parcial | Script existe mas vazio. Precisa implementar lógica de "remover plataforma adjacente" e "dobrar recompensa". |
| I9 | Plataforma de pulo — trampolim para próxima plataforma | `PlataformaTrampolim.cs` | ⚠️ Parcial | Implementado como teleportação. Precisa mudar para physics-based jump com Rigidbody.AddForce. |
| I10 | Floresta (caminho) — lobos na área do loop | `PlataformaAcampamentoBarbaro.cs`, `Lobo.prefab` | ⚠️ Parcial | Spawner de inimigos funciona dentro da plataforma. Lobos existem como prefabs mas não têm lógica especial de floresta. |
| I11 | Floresta profunda (fora) — 3x3 florestas → lobos com -15% vida | (Nenhum) | ❌ Não implementado | Precisa de sistema de adjacência para verificar grid 3x3 e aplicar debuff em lobos. |
| I12 | Campo pacífico (fora) — +2 PV permanente ao personagem | (Nenhum) | ❌ Não implementada | Precisa criar `PlataformaCampoPacifico.cs`. |
| I13 | Cabana (fora) — +2 PV, se campo pacífico adjacente 3x3 → +2 PV extra por área | (Nenhum) | ❌ Não implementada | Precisa criar `PlataformaCabana.cs` com verificação de adjacência. |

---

## 3. Approach (Abordagem Proposta)

### Fase 1 — Correções Críticas (bloqueante para jogo jogável)
| # | Arquivo(s) | Ação |
|---|-----------|------|
| F1.1 | `PlayerStatus.cs` | Corrigir fórmula XP: `fatorCrescimentoXP = 1.75f` (+75%) |
| F1.2 | `InimigoBase.cs` + `GameManager.cs` | Conectar `AvancarLoop()` para incrementar multiplicador de stats dos inimigos em 10% por loop |
| F1.3 | `PlayerStatus.cs` + `GameOverManager.cs` | Conectar evento `onDeath` → `GameOverManager.IrParaGameOver()` (ou direto `LoadMainMenu()`) |
| F1.4 | `PlataformaLar.cs` | Adicionar restauração de vida máxima além das flechas |

### Fase 2 — Plataforma e Movimentação
| # | Arquivo(s) | Ação |
|---|-----------|------|
| F2.1 | `LoopGenerator.cs` | Converter grid de quadrado 3x3 para hexagonal (coordenadas axiais). Manter compatibilidade com plataformas existentes. |
| F2.2 | `PlataformaTrampolim.cs`, `LoopMover.cs` | Trocar teleportação por Rigidbody.AddForce vertical (physics jump). Configurar altura como campo público editável. |
| F2.3 | `PlataformaVazia.cs` + `PlacementManager.cs` | Implementar remoção de plataforma adjacente e flag "recompensa dobrada" na próxima plataforma. |

### Fase 3 — Novos Tipos de Plataforma (do zero)
| # | Arquivo(s) | Ação |
|---|-----------|------|
| F3.1 | `PlataformaCoracao.cs` | Coleta: +vida ao passar, destroy after pickup |
| F3.2 | `PlataformaMoeda.cs` | Coleta: +moedas (ou score), destroy after pickup |
| F3.3 | `BlocoDestrutivel.cs`, `PlataformaBlocos.cs` | Blocos com HP, destruíveis por cabeçada do jogador, dropam corações/flechas/moedas, recriados no fim do loop |

### Fase 4 — Combate Avançado
| # | Arquivo(s) | Ação |
|---|-----------|------|
| F4.1 | `InimigoBase.cs` + `Animator` | Adicionar animação de voo (move em Y oscillating). Trocar tag para "FlyingEnemy" se necessário. |
| F4.2 | `InimigoBase.cs` | Adicionar investida: quando inicia ataque, mover-se rapidamente até o player e voltar. |
| F4.3 | Novo arquivo → `BossManager.cs` | Spawnar chefão após N loops (configurável) no centro do loop. Stats escalados 5x-10x. HP bar própria. |

### Fase 5 — Adjacências e Sistemas Complexos
| # | Arquivo(s) | Ação |
|---|-----------|------|
| F5.1 | Novo arquivo → `GridAdjacencySystem.cs` | Verificar grid adjacente 3x3 para efeitos: floresta profunda (-15% vida lobos), cabana (+2 PV extra com campo pacífico), campo pacífico (+2 PV permanente). |
| F5.2 | `PlataformaAcampamentoBarbaro.cs` | Revisar lógica de spawn — deveria restaurar vida/munição e aumentar stats inimigos em 10% (conforme requisito I3 barraca). |

### Fase 6 — Refinamento e UX
| # | Arquivo(s) | Ação |
|---|-----------|------|
| F6.1 | `UICardManager.cs` | Conectar descrição das cartas ao campo de texto da UI (atualmente está em inglês "Select a card...") |
| F6.2 | `UILevelUp.cs` + `PlayerStatus.cs` | Conectar evento `onLevelUp` → `UILevelUp.AtualizarNivel()` |
| F6.3 | `PlayerAttack.cs` | Adicionar campo público para velocidade do arco (projétil de flecha) |

---

## 4. Decisões (Evidence-Based)

### D1 — Grid Hexagonal vs Quadrado
- **Decisão:** Converter para coordenadas hex axiais (Q, R). Manter o loop como 8 pontos (bordas), mas espaçados em grid hex.
- **Evidência:** PDF diz "grid hexagonal". O código atual usa Vector3(x,z) simples que é quadrado. Mudança necessária para parecer visualmente hex.
- **Risco:** Quebra compatibilidade com prefabs de plataformas existentes se não houver um adapter layer.

### D2 — Jump: Physics vs Teleportação
- **Decisão:** Trocar teleportação por `Rigidbody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse)`.
- **Evidência:** PDF diz "pulável" e "valor facilmente mudado no inspector". Physics-based é mais natural e permite controle via inspector.
- **Risco:** Pode precisar ajustar timing do LoopMover para não mover o player enquanto está pulando.

### D3 — Boss: Centralizado vs Distribuído
- **Decisão:** Boss aparece no centro do loop (posição 4,5 do grid) após N loops configuráveis. Stats baseados em multiplicador atual * fator boss (ex: 8x).
- **Evidência:** PDF diz "chefão ira aparecer no centro do loop". Centro do grid 3x3 = posição (1,1) que é vazia por padrão no código atual.

### D4 — Drag-and-Drop vs Click-to-Place
- **Decisão:** Manter click-to-place (raycast). Não implementar drag-and-drop real pois: (a) funciona, (b) adiciona complexidade desnecessária agora, (c) o PDF diz "arrastar" mas não especifica UI framework.
- **Evidência:** O PlacementManager + raycast já funciona corretamente. Mudança para drag seria refatoração grande sem benefício proporcional.

### D5 — XP Reset entre Loops
- **Decisão:** XP é resetado a cada loop (como o PDF diz "apenas no loop, não globalmente"). O level-up dá buffs permanentes mas XP acumula dentro do loop.
- **Evidência:** PDF explicitamente menciona "apenas no loop".

---

## 5. Boundaries (Exclusões)

| Item | Por que excluído |
|------|-----------------|
| Texturas/sprites dos inimigos voadores | Escopo artístico — código pode usar placeholders |
| Animações de investida do boss | Pode ser feito com animação Unity separada após o código estar funcionando |
| Sistema de economia (moedas têm função) | PDF menciona moedas mas não especifica uso além de coletar |
| Save/load game data | Não mencionado no PDF |
| Multiplayer ou networking | Jogo single-player local |

---

## 6. Verification (Critérios de Teste)

### Para cada requisito, um teste verificável:

| Req | Cenário de Teste | Resultado Esperado |
|-----|-----------------|-------------------|
| M1 | Iniciar jogo | Path aparece em grid hexagonal visualmente reconhecível |
| M2 | Player atinge gap no caminho | Pula com animação physics, aterrissa na próxima plataforma |
| C3 | Matar inimigo com flecha | Ganha XP, sobe de nível se meta atingida, recebe carta aleatória |
| C4 | Vida do player chega a 0 | Tela Game Over → botão para Menu Principal (ou auto-load) |
| C5 | Completar 2 loops | Inimigos no loop 3 têm 1.2x stats vs loop 1 |
| I6 | Player passa por plataforma de flechas | Recebe 5 flechas, plataforma é destruída |

---

## 7. Recovery (Rollback)

- **Git branch:** `bionic/planejamento-analise` — criar antes de qualquer implementação
- **Backup:** Todos os arquivos atuais estão versionados no git (`main` branch presumido)
- **Estratégia:** Cada fase pode ser revertida individualmente via `git checkout HEAD~1 -- path/do/arquivo`
- **Segurança:** Plataforma Lar tem proteção contra substituição — se quebrar, voltar para o backup garante que o jogo não trava no spawn

---

## 8. Resumo Executivo: Quanto falta para "concluir"?

| Categoria | % Completo | Esforço Estimado |
|-----------|-----------|-----------------|
| Movimentação básica | ~60% | Médio — converter grid hex + physics jump |
| Combate básico | ~70% | Baixo-Médio — corrigir fórmulas, conectar death → menu |
| Plataformas/Ilhas | ~35% | Alto — 5+ novos scripts de plataforma |
| Adjacências e efeitos complexos | ~10% | Alto — sistema de grid adjacency novo |
| Boss fight | 0% | Médio-High — spawn logic + boss stats + HP bar |

**Estimativa total:** ~60-70% do código necessário já existe. Os gaps críticos (Fase 1) podem ser resolvidos em 2-4 horas de implementação. O sistema completo (todas as fases) deve levar ~2-3 semanas de desenvolvimento focado.

**Prioridade recomendada:** Fase 1 (correções críticas) → Fase 2 (movimentação) → Fase 3 (novas plataformas) → Fase 4 (combate avançado) → Fase 5 (adjacências).

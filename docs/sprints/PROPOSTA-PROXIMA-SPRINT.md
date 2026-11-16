# Proposta da próxima sprint — inimigos, leitura do combate e ritmo

Preparada em 2026-11-16, depois da sprint corretiva de UI ([SPRINT-UI-POLISH.md](SPRINT-UI-POLISH.md)). **Só
planejamento:** nada daqui foi implementado; a sprint precisa de autorização do responsável.

Base observada: build consolidada e Editor (`main` + UI polish), relatório de consolidação §6–7 e as partidas
desta rodada. Pontos de partida no código:
- `InimigoDef.prefab` vazio no `Morcego.asset` → `Inimigo.cs` cria uma **esfera primitiva** (voadores) ou um cubo.
  O chefe usa a variante do morcego, por isso é uma esfera vermelha grande.
- O `Lobo.asset` já usa um prefab (`Assets/Modelos/PreFabs/Lobo.prefab`, de `Lobo (1).fbx`). Não há asset chamado
  "raposa" no projeto; **confirmar com o responsável** se "raposa" é esse modelo do lobo.
- `Assets/Modelos/MateusModelos/Morcegp.fbx` (37 KB, rig *Generic*, importação de animação ligada, **nenhum clipe
  listado**) e `Morcego.png` estão na `main`, sem uso.
- `MedidorDaPartida`/`MetricasDaPartida` já registram duração por volta, duração do confronto, tempo e maior trecho
  sem flechas no chefe. `FumacaDeMorte` existe como efeito de morte.
- O GDD v2 **não fixa meta de duração** da jornada; observado nas partidas: ~1,5 min na build (4 voltas + chefe),
  ~2,5 min no Editor sem cartas.

## Avaliação por frente

| Frente | Objetivo | Problema atual | Impacto visual | Impacto no gameplay | Complexidade | Risco | Dependências | Escopo relativo | Ordem |
|---|---|---|---|---|---|---|---|---|---|
| **D. Ritmo** | Saber quanto dura e onde o jogador decide | Jornada curta (~1,5 min); flechas zeram com frequência; sem meta no GDD | Baixo | **Alto** | Baixa (medir) | Baixo | `MedidorDaPartida`; meta definida pelo responsável | P | **1** |
| **B. Lucca e câmera** | Lucca legível no combate sem perder o tabuleiro | ~20 px de altura na vista padrão; some entre ilhas e inimigos | **Alto** | **Alto** | Baixa–média | Baixo | Nenhuma (só visual; NavMesh e animações intactos) | P–M | **2** |
| **A. Inimigos** | Morcego e chefe com modelo próprio e animação simples | Esferas; chefe não lê como "Morcego Ancião" | **Alto** | Médio (ler ameaça) | Média–alta | Médio (escala, pivô, `VisualDeIlha`, sem clipes no FBX) | Investigar `Morcegp.fbx`; decisão sobre a "raposa" | M–G | **3** |
| **C. Feedback de combate** | Entender dano, ataque, spawn, morte e munição | Dano e morte pouco sinalizados; flecha acabando só aparece no número | Médio | **Alto** | Média | Baixo–médio (custo de VFX; sem `renderer.material`) | A (para spawn/morte casarem com o modelo) | M | **4** |
| **E. Construções no mundo** | Carta colocada = objeto reconhecível, coerente com a ilustração | Flechas, Coração e afins só tingem a ilha; Torre e Fonte têm modelo | Médio–alto | Médio | Média | Médio (decoração não pode disparar rebake; `NavMeshModifier.ignoreFromBuild`) | Catálogo de arte das cartas | M | **5** |

## Sequência proposta (entregas pequenas, verificáveis e reversíveis)

Cada passo é um PR próprio a partir da `main`, com testes, capturas antes/depois em 1080p real e rollback por `git revert`.

1. **D1 — Medição do ritmo (sem mudar números).** Rodar 5–10 jornadas com `MedidorDaPartida` (Editor e build de
   desenvolvimento, perfil de QA) e registrar: duração por volta, tempo até o chefe, duração do confronto, intervalos
   entre decisões (carta jogada / recompensa recebida), tempo e trecho sem flechas. Saída: tabela + proposta de meta
   para o responsável aprovar. *Aceite:* relatório com dados reais; nenhum valor de jogo alterado.
2. **B1 — Leitura da Lucca.** Contorno/silhueta leve ou anel no chão sob a Lucca, contraste contra a ilha, e revisão
   do enquadramento padrão/zoom mínimo. Sem mexer em NavMesh, locomoção ou clipes. *Aceite:* Lucca reconhecível
   em captura 1080p durante o chefe; testes de câmera e Lucca verdes.
3. **A1 — Investigação do `Morcegp.fbx` (spike, 1 dia).** Importar numa cena de teste, medir escala/pivô/orientação,
   checar se há malha com skin e clipes. Resultado decide entre: (a) usar o FBX com animação procedural (bater de
   asas por transform, balanço de voo), ou (b) pedir clipes ao artista. *Aceite:* nota técnica com capturas.
4. **A2 — Morcego com modelo.** Preencher `Morcego.asset` → prefab com o modelo e animação simples (voo, ataque como
   investida curta, morte com a `FumacaDeMorte`). Colliders desligados no visual, como em `VisualDeIlha`.
   *Aceite:* nenhum `PrimitiveType.Sphere` em jogo para morcegos; testes de combate/Mirante verdes.
5. **A3 — Chefe legível.** O Morcego Ancião usa o mesmo modelo com escala, cor/material próprios (via
   `MaterialPropertyBlock`) e uma presença extra (aura/sombra no chão) — sem mudar balanceamento.
6. **C1 — Dano recebido e munição.** Flash/vinheta curta quando a Lucca toma dano; aviso de flechas baixas no painel
   da jornada (cor + ícone) e quando acabam. *Aceite:* visível em captura; sem custo novo por quadro relevante.
7. **C2 — Ataque, spawn e morte.** Flash de acerto no inimigo, partícula curta de spawn, morte com fumaça e queda.
   Medir draw calls antes/depois (uma leitura de `UnityStats`, nunca `Camera.Render()` em laço).
8. **E1 — Construções.** Para as cartas que só tingem a ilha (Flechas, Coração, Moedas, Campo Pacífico, Cabana etc.),
   usar o marcador já previsto em `PlataformaDef.marcador` com a mesma silhueta da ilustração da carta. Decoração
   nunca dispara rebake. *Aceite:* cada uma das 14 cartas tem leitura no mundo coerente com a ilustração.

## Decisões que dependem do responsável

- Meta de duração da jornada e do confronto (depois de D1).
- "Raposa" = modelo do lobo atual? Há outro modelo previsto?
- Morcego: animação procedural aceitável ou clipes do artista?
- Gênero do texto da personagem ("o Lucca" nos dados × "a Lucca" na equipe).
- Placa alternativa para nomes longos nas cartas compactas (risco residual da sprint de UI).

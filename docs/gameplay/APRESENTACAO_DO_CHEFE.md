# Apresentação do chefe — orientação do arco e do tronco (plano para a frente de personagem)

Status: **plano**. A frente de gameplay só expõe dados de leitura; quem implementa a animação é a frente de personagem.
Nada aqui altera rig, clipes, Avatar ou Animator Controller.

## Problema
O Morcego Ancião fica no centro do anel. O Lucca o contorna atirando: o alvo está quase sempre **de lado** (às
vezes atrás). Hoje a flecha sai do peito e voa teleguiada, mas o corpo continua olhando para onde anda. Sem
orientação, o tiro no chefe não lê como "o Lucca está atirando nele".

## O que o gameplay já fornece (commit desta sprint)
| Dado | Onde | Uso previsto |
|---|---|---|
| `LuccaCombate.AlvoAtual` (Inimigo ou null, todo frame) | `Assets/Scripts/Hex/LuccaCombate.cs` | direção de mira contínua (inclusive entre tiros) |
| `LuccaCombate.AoDisparar(Inimigo alvo, Vector3 origem)` | idem | gatilho do clipe/camada de disparo, som |
| `Inimigo.AlvoDeQualquerAngulo` | `Inimigo.cs` | saber que é o chefe (mira lateral/traseira esperada) |
| `LoopDirector.Estado` / `Terminou` | `LoopDirector.cs` | parar de mirar na vitória/derrota (`AlvoAtual` já vira null) |
| `HexPlayer.EstaPulando` | `HexPlayer.cs` (não alterado) | suspender a mira durante o pulo |

## Regras de apresentação propostas
1. **Movimento intocado.** O `NavMeshAgent` continua girando a raiz (`velocidadeAngular` 720°/s). A mira só gira
   do quadril para cima. Nenhuma escrita em `transform.rotation` da raiz nem em `NavMeshAgent`.
2. **Tronco:** girar até ±70° em torno do eixo vertical para o alvo; acima disso, limitar e deixar o tiro sair
   assim mesmo (a flecha é teleguiada). Suavizar (ex.: 0,15 s) para não tremer quando o alvo troca.
3. **Arco/braços:** apontar para o centro do alvo (o chefe voa a 2,2 u; morcegos a 1,1 u) — pitch limitado a ±35°.
4. **Sem alvo:** voltar ao neutro em ~0,25 s. **No pulo:** peso da mira 0.
5. **Ordem de prioridade visual:** a mira segue `AlvoAtual` (que já prioriza o inimigo mais próximo; o chefe só
   quando não há morcego mais perto). Assim a Revoada "rouba" visivelmente o arco do chefe — é a leitura que a
   mecânica quer.
6. **Origem da flecha:** hoje `alturaDoTiro` (0,6 u) acima do pé. Se o arco tiver um socket, o gameplay pode passar
   a usar a posição dele em vez da altura fixa (mudança pequena em `LuccaCombate`, a combinar).

## Implementação sugerida (frente de personagem)
- Camada de Animator só do tronco (máscara do quadril para cima) + `Animator.SetLookAtPosition`/`SetLookAtWeight`
  em `OnAnimatorIK`, **ou** Animation Rigging `MultiAimConstraint` no tronco/braço se o pacote for aprovado
  (dependência nova: precisa de justificativa no PR, AGENTS.md §4.6).
- Um componente da frente de personagem (ex.: `MiraDoLucca`) lê `LuccaCombate.AlvoAtual` em `LateUpdate`.
  Gameplay não referencia esse componente (dependência só de lá para cá).

## Critérios de aceite (para a revisão conjunta)
- Captura com o chefe de lado: tronco e arco voltados a ele durante a sequência de tiros.
- Movimento e tempo de volta iguais aos de antes (comparar `MetricasDaPartida.voltas[].duracao` com/sem a mira).
- Nenhum teste de navegação regride (`HexPrototipoPlayModeTests`).

## Pendências
- Combinar com a frente de personagem o socket do arco e o formato do rig (Humanoid ou Generic).
- Se o modelo final mudar a altura do peito, atualizar `alturaDoTiro` e `Inimigo.AlturaDoPeito` juntos.

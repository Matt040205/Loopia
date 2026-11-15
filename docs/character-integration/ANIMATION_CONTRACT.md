# Contrato de animação

| Camada | Estado | Duração de origem | Loop |
| --- | --- | ---: | --- |
| Base Layer | Idle | 3,20 s | Sim |
| Base Layer | Walk | 0,8667 s | Sim |
| Base Layer | Run | 0,50 s | Sim |
| Base Layer | Jump | 0,35 s | Não |
| Base Layer | Spring Jump | 0,70 s | Não |
| Upper Body | Bow Ready | 1 frame a 60 fps | Sim |
| Upper Body | Bow Shot | 0,65 s | Não |

`MoveSpeed` alimenta a Blend Tree 1D de `Locomotion`, com Idle a 0, Walk a 0,90 e Run a 3,50 u/s. Walk e Run compartilham o contato inicial esquerdo. `RitmoPulo` ajusta o clipe ao tempo que `HexPlayer` comunica. `AoIniciarPulo(bool usaMola, float duracao)` nasce dentro da travessia do link. `EstaPulando` comanda o retorno à locomoção, inclusive em teleporte. A absorção de impacto está nos clipes de salto e na mistura curta para `Locomotion`. `HexPlayer` e o NavMeshAgent são donos da trajetória. Root Motion está desligado. Os testes de velocidade e transição estão em [LOCOMOTION_REWORK.md](LOCOMOTION_REWORK.md).

A base já carrega o arco em Idle, Walk, Run e nos saltos. A camada superior tem peso zero em Bow Ready e recebe peso durante Bow Shot. `Loopia_BowUpper` exclui Hips, pernas, Root e saia, preservando a contribuição da base. Saia, capa, mãos e arco continuam com curvas auxiliares no .anim quando aplicável.

`OnBowRelease(AnimationEvent)` ocorre a 19/60 s (0,316667 s). O callback valida clipe e estado. O bridge confirma a soltura depois de aplicar a mira ao frame. Se o evento for omitido, a passagem pelo mesmo ponto do estado confirma a soltura. O combate tem uma reserva identificada por tiro e consome essa reserva antes do disparo, descartando confirmações repetidas. A cadência mínima é medida entre lançamentos reais. `AoDisparar(Inimigo, Vector3)` é emitido após criar o projétil e descontar uma flecha. A munição permanece intacta em preparações canceladas.

A mira usa o vetor entre `Arrow_Spawn` e `Arrow_Aim` como referência na puxada e `AlvoDaPreparacao` durante a reserva. Os limites iniciais são 65° de yaw e 25° de pitch. A mira em repouso é mais leve. Em voo o repouso volta ao neutro e a mira perde peso durante o disparo. Os limites evitam torcer a silhueta em alvos muito laterais ou traseiros.

Referências do Editor. [Unity Animation Layer](https://docs.unity3d.com/6000.3/Documentation/Manual/AnimationLayers.html) · [AnimationEvent](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/AnimationEvent.html)

# Limites verificados

- Na Sprint 2C.2, Run passou a atender os 3,5 u/s do circuito. O arrasto mediano no apoio central caiu de 2,55 para 0,45 u/s. O P95 de 2,39 u/s mostra que ainda há deslizamento localizado, sobretudo onde a orientação e o blend mudam depressa. A caminhada de baixa velocidade tem seu clipe próprio. [Relatório de locomoção](LOCOMOTION_REWORK.md)
- O limite de torção do tórax e o peso reduzido no ar deixam erro angular em alguns tiros laterais e a partir dos links. O projétil continua seguindo o alvo selecionado. Os ângulos da primeira partida instrumentada foram medidos antes do refinamento de mira no ar.
- A sequência Blender revisada manteve folga entre saia e pernas nos pares medidos. Há contatos localizados do arco com a túnica e a barra da saia em parte da mistura de tiro. Há verificação visual na cena real. Não há varredura volumétrica de cada ângulo possível da mira no Unity.
- A medição de memória é de assets isolados pelo Profiler do Editor. O custo de GPU/Animator em build e aparelhos-alvo segue pendente.
- A integração usa Idle ao encerrar a partida. Não foi criado clipe de vitória ou de morte neste escopo.

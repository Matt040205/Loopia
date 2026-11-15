# Limites verificados

- O ciclo Walk aprovado tem recuo mediano do pé de ~0,56 m/s no trecho baixo medido no Blender a 1×. A escala visual de 0,66 reduz essa ordem de grandeza para 0,37 unidade/s. O controle de ritmo reduz a diferença, mas a passada ainda desliza em trechos de velocidade cheia de 3,5 unidades/s. Um ciclo de corrida com passada calibrada é a próxima opção se o time quiser fechar a diferença.
- O limite de torção do tórax e o peso reduzido no ar deixam erro angular em alguns tiros laterais e a partir dos links. O projétil continua seguindo o alvo selecionado. Os ângulos da primeira partida instrumentada foram medidos antes do refinamento de mira no ar.
- As medidas de colisão do Blender pertencem aos clipes de origem. Há verificação visual na cena real. Não há varredura volumétrica de cada ângulo possível da mira no Unity.
- A medição de memória é de assets isolados pelo Profiler do Editor. O custo de GPU/Animator em build e aparelhos-alvo segue pendente.
- A integração usa Idle ao encerrar a partida. Não foi criado clipe de vitória ou de morte neste escopo.

# Implementação das exigências do Zé

Base: branch `prototipo-hex`, atualizada até `7ea3eab`. Cena: `Assets/Scenes/HexPrototipo.unity`. Validado pelo MCP no Unity 6000.3.10f1.

## Mudanças

1. Ilhas usam o modelo importado e tamanho da grade de 3 unidades, antes 1. As decorações são sorteadas por coordenada na periferia. O centro e os seis corredores entre ilhas ficam reservados, inclusive para futuras cartas. As decorações não participam do NavMesh.
2. A câmera enquadra os limites dos modelos e do relevo. O cenário acompanha a projeção da câmera com escala proporcional, mantendo seu topo abaixo das ilhas. O plano de água cobre os quatro cantos da tela em cada zoom.
3. Coração, moeda e flecha aparecem no centro, giram e flutuam. Somem após a coleta e reaparecem na próxima volta. Moedas aleatórias também usam o novo modelo. O coração anterior era um marcador esférico; foi criado um coração em malha 3D.
4. Lobos usam NavMesh para perseguir o Lucca dentro da própria ilha, atacar por proximidade com intervalo entre golpes e voltar quando ele sai. Não atravessam os links de salto. A posição e o NavMesh dos inimigos acompanham alterações de relevo.
5. A carta Ilha alta só é liberada com uma mola colocada. A mola carrega 3 unidades de impulso para a próxima subida alta; a ilha alta sobe 2,2 unidades, acima do salto normal de 0,7. A colocação verifica a ordem do percurso e o impulso atual, evitando uma subida sem mola anterior. Mouse, marcador de colocação e navegação consideram a altura real.
6. Mortes de lobo e morcego instanciam o VFX existente `SmoothSmoke`. A emissão para após 0,18 s e o objeto é removido após 5 s.

URP foi alinhado à versão 17.3.0 já usada pelo VFX Graph e pelo editor, resolvendo os erros de compilação dos binders de VFX. O grafo de fumaça original foi preservado.

## Como experimentar

Abra `HexPrototipo` e entre em Play. A mão inicial contém Mola, Coração, Moedas e Flechas. Arraste a Mola para uma ilha do caminho antes do trecho em que deseja subir. A carta Ilha alta será oferecida uma vez no espaço liberado, assim que a mola existir no mapa. Coloque a ilha alta depois da mola, no sentido em que o Lucca corre. A UI explica colocações recusadas. Depois dessa oferta inicial, as cartas seguem o sorteio do baralho ao subir de nível.

Para ver a IA dos lobos no fluxo do jogo, use a carta Floresta; o nascimento de lobos por tempo continua configurável no InimigoSpawner. Os valores de velocidade, alcance e altura estão expostos no Inspector para ajustes de balanceamento.

## Validação realizada

| Verificação | Resultado |
| --- | --- |
| Compilação e Console ao terminar | Sem erros ou avisos |
| Cena e 13 prefabs novos | Sem scripts ou materiais ausentes |
| Mapas de 24, 36 e 60 ilhas | Geração bem-sucedida |
| Decorações nos três mapas, sementes 177/179/183 | 88/126/208; mínimo 2/1/2 por ilha; zero invasões do centro ou corredores e zero sobreposições entre suas áreas reservadas |
| Fundo em 16:9, 4:3 e 9:16, nos três tamanhos | Os quatro cantos projetados ficaram dentro da água em todas as nove combinações |
| Relevo | Carta bloqueada sem mola; liberada com mola; segunda subida sem recarga recusada; subida sem impulso bloqueada; salto com impulso chegou à ilha alta e consumiu a carga |
| Percurso automático | Uma volta completa com entrada real na mola e travessia da ilha alta, sem bloqueio por altura; teste acelerado com inimigos desativados |
| Seleção da ilha alta | Raio pelo centro da ilha selecionou a coordenada correta |
| Inimigo em ilha elevada | Lobo reposicionado em Y ≈ 2,23 e ligado ao NavMesh após elevar a ilha |
| Recompensas | Cura +5, moedas +3 e flechas até o limite; sem coleta repetida na mesma volta; visual restaurado ao fechar volta |
| IA de lobo | Aproximou-se até ≈ 0,57 unidade e causou dano; a ≈ 9,05 unidades voltou sem causar dano |
| Fumaça | Dois efeitos visíveis após matar um lobo e um morcego; zero efeitos restantes após o prazo de limpeza |
| Oferta para teste | Mão inicial correta; ilha alta indisponível antes da mola e entregue ao liberar o espaço depois da colocação |

As capturas abaixo foram feitas em Play Mode com uma câmera temporária de aproximação. Essa câmera não foi salva na cena. Os testes não substituem ajuste de dificuldade nem uma sessão de playtest de balanceamento. O arquivo de progresso foi preservado no estado anterior aos testes.

- [Modelos e decorações](../Captures/items-final.png)
- [Subida com mola](../Captures/spring-ascent.png)
- [Fumaça nas duas mortes](../Captures/death-smoke-final.png)

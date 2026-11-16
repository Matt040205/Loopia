# Sprint 2C.2 — Locomoção da Lucca

## Diagnóstico

Na versão 2C, Walk tinha 1,20 s para dois passos. No Blender, a fase baixa do pé recuava cerca de 0,56 m/s no clipe a 1×, equivalente a 0,37 unidade/s após a escala visual de 0,66. O bridge acelerava esse clipe, ainda bem abaixo dos 3,5 u/s do NavMesh. Em Play Mode, a mediana do deslocamento horizontal da ponta do pé durante a fase central de apoio foi 2,55 u/s no chão a plena velocidade. O ciclo foi substituído por uma caminhada calibrada para velocidade baixa e uma corrida própria.

## Autoria no Blender

| Ação | Duração | Uso |
| --- | ---: | --- |
| Idle | 3,20 s | Respiração e postura com arco da 2C |
| Walk | 0,8667 s | Dois passos, apoio de 60% do ciclo por pé, velocidade de referência 0,90 u/s |
| Run | 0,50 s | Dois passos, apoio de 27% por pé, suspensão, joelhos em recuperação e tronco inclinado |
| Jump | 0,35 s | Preparação curta a partir da passada e compressão ao tocar |
| Spring Jump | 0,70 s | Impulso/distinção aérea da mola e retorno à passada |
| Bow Shot | 0,65 s | Camada superior da 2C, soltura em 19/60 s |

A corrida foi animada a 60 fps. O recuo nominal do pé ao longo do apoio usa distância = velocidade × duração do ciclo × fração de apoio ÷ escala do modelo. Isso dá ~0,72 m por apoio no espaço Blender para os 3,5 u/s medidos. Os pés são resolvidos por pose com limite anatômico e altura de sola. As poses de vento da capa chegam com defasagem. A saia abre discretamente para liberar o joelho. A mão direita segue a contrarrotação do tronco em marcha e corrida, e o arco acompanha a mão esquerda.

O .blend editável inclui as ações revisadas, a Action `Demonstração · Sprint 2C.2` e os marcadores da sequência. As ações 2C de Walk/Jump/Spring Jump estão disponíveis pelo prefixo `2C ·` e no backup integral. Os quatro FBXs têm um take cada e a mesma hierarquia de 57 ossos. A verificação comparou o bind pose, posições, topologia, UVs, pesos e slots de material das 67 peças com a versão 2C. O núcleo do rig e o FBX do modelo usado pelo prefab foram preservados.

## Integração Unity

O estado `Base Layer.Locomotion` usa uma Blend Tree 1D com `MoveSpeed` em unidades/s. Idle 0, Walk 0,90, Run 3,50. Walk e Run compartilham o contato inicial esquerdo. O blend amortece a velocidade horizontal medida do agente em 45 ms. Valores abaixo de 0,06 u/s vão a zero. A mistura curta respondeu à aceleração do agente sem exigir clipes separados de partida e parada. [Unity 6.3, Blend Tree 1D](https://docs.unity3d.com/6000.3/Documentation/Manual/BlendTree-1DBlending.html)

`HexPlayer.AoIniciarPulo` fornece tipo e duração da travessia real. A base entra no salto em 25 ms. `RitmoPulo` ajusta o clipe à duração emitida. Ao fechar o link, o bridge restitui a locomoção em 65 ms, usando a última velocidade horizontal por até 75 ms se o agente ainda devolver zero e houver caminho válido. A absorção foi incorporada aos clipes de salto e à transição. Se o destino encerra a rota, a velocidade medida leva a Idle. Os scripts do NavMesh, o balanceamento de 3,5 u/s e a trajetória de pulo permanecem na implementação de gameplay. Root Motion desligado.

A Upper Body conserva AvatarMask, evento `OnBowRelease` e o socket da flecha. Foi observado início de preparo durante o pulo e soltura aérea. Nenhum bloqueio artificial de tiro por estado locomotor foi introduzido. O prefab expõe o amortecimento e o tempo de mistura. Os clipes foram importados como Humanoid e receberam as curvas auxiliares por binding de osso já usado pelo rig.

## Resultados em Play Mode

| Teste | Resultado |
| --- | --- |
| Circuito antes e depois a 60 Hz simulados, seed 12345 | 24 ilhas na mesma ordem, 24 saltos e 26,83 s nas duas versões |
| Volta após a troca a 30 Hz simulados | 24 ilhas na mesma ordem e 24 estados de salto correspondentes em 27,90 s |
| Arrasto no apoio central a 3,5 u/s | Mediana de 2,55 u/s com Walk anterior para 0,45 u/s com Run, redução ~82,5%. P95 de 3,01 para 2,39 u/s |
| Walk a 0,90 u/s | Mediana de 0,12 u/s em 124 amostras de apoio, P95 0,54 u/s |
| Sequência interativa do vídeo | 7 saltos normais e 1 de mola, todos com estado de animação correspondente. Retomada durante rota contínua e parada no destino |
| Tiro na sequência | 6 solturas no socket, 3 delas no ar. Dois preparos iniciaram no ar. Três solturas durante velocidade de corrida |
| Root e empunhadura | Maior desvio local medido ~0,000000022 u na raiz visual/osso Root. Desvio da posição local de Bow_Grip igual a zero |
| Test Runner | 301/301 Logic e SceneStatic, 18/18 PlayModeSmoke e PlayModeMedicao, incluindo regressão de locomoção, vitória e derrota no combate |

As medições de apoio usam a velocidade horizontal da ponta do pé em mundo em amostras consecutivas da fase central de contato. O recorte depende da duração de apoio de cada clipe. Trata-se de um indicador observável de arrasto. O P95 inclui transições e curvas, onde persistem amostras com arrasto maior. Os trajetos de raiz amostrados a 60 Hz coincidiram após os 0,10 s iniciais de preparação dos ensaios. A maior diferença na preparação foi 0,158 u. Para reproduzir, abrir `HexPrototipo`, seed 12345. O vídeo 'Ciclo contínuo' usa velocidades temporárias de 0,9 e 3,5 u/s, três alvos de teste e cartas Mola/Ilha Alta. A volta usa o corredor automático de produção. As capturas acompanham o personagem com ângulo isométrico da cena. Os frames incluem iluminação de 12h e 22h.

Os jobs MCP foram `94489007c9214564b375a328860f4c2a` e `4cff3e9a972f4026823916f1c26d7ac9`. Outra worktree também escreveu no diretório global de resultados da Unity. O log MCP do job Logic e SceneStatic e o XML Play Mode identificado pela assembly da worktree `loopia-lucca` estão em Evidências. O Play Mode final teve vitória em 158,6 s com Flechas e Torre, e derrota em 193,4 s sem cartas, ambas após quatro voltas. O console da sessão final teve zero erros e avisos. A suíte restaurou o save ao hash que encontrou no início. As cópias locais pré-ensaio e pós-teste foram guardadas, mantendo no disco o save compartilhado com a outra worktree.

## Deformação, limites e arquivos

O teste Blender BVH incluiu o Walk, Run, saltos, disparo sobre Run e a demonstração interpolada. Registrou zero interseções saia/perna, saia/saia e capa/túnica nas ações revisadas. Persistiram contatos localizados do arco com a túnica e com a barra externa em parte da mistura de disparo. O P95 máximo de alongamento da saia interna no Run foi 1,674×. A avaliação visual na câmera do jogo cobriu os movimentos e dois horários de iluminação. A varredura volumétrica cobriu as ações e combinações do Blender. A mira procedural no jogo inclui ângulos fora desse recorte.

Abra a pasta `outputs/Loopia-Sprint-2C.2` para o .blend, backup, FBXs, vídeos, capturas e telemetria. A cena real usa o controller e o prefab atualizados na worktree `loopia-lucca`, branch `feature/lucca-locomotion-2c2` sobre `89dc640`. Os ativos da 2C e a cena no commit anterior permitem rollback.

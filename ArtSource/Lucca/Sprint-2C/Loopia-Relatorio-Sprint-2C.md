# Loopia · Sprint 2C — arco e saltos

Os seis clipes foram produzidos no Blender 5.2 e testados no Avatar Humanoid da Unity 6000.3.10f1 com URP 17.3.0. A validação foi feita em um projeto isolado. O projeto de gameplay do Loopia ficou fora dessas alterações.

## Clipes a 60 fps

| Clipe | Duração | Loop | Movimento |
| --- | ---: | --- | --- |
| Bow Ready | 0,0167 s | Sim | Pose sustentada com o arco na esquerda e a direita livre junto ao corpo. |
| Idle | 3,20 s | Sim | Respiração, cabeça e tecido com a empunhadura em repouso. |
| Walk | 1,20 s | Sim | Dois passos in-place com a empunhadura e o balanço da capa. |
| Bow Shot | 0,65 s | Único | Elevação, encaixe, puxada, soltura e recuperação. |
| Jump | 0,35 s | Único | Antecipação curta, impulsão, ápice, preparação e absorção. |
| Spring Jump | 0,70 s | Único | Preparação mais profunda, gesto aéreo mais amplo e recuperação de tecido mais longa. |

O `.blend` abre com Idle. Selecione outra ação no Action Editor. As ações originais Idle e Walk da 2B.2 estão identificadas com o prefixo `2B.2`. Os [vídeos](videos) mostram as ações completas e as combinações, com pausas de apresentação nos clipes curtos. A flecha nas capturas e nos vídeos é uma guia visual. Em jogo, o projétil será instanciado pelo código no socket existente.

## Disparo e combinação com Walk

Use `unity/Loopia_BowUpper.mask` em camada **Override** com `Bow Shot.anim`. A ação no Blender só tem canais da parte superior. A máscara exclui a raiz, quadril, pernas, saia e capa. Os caminhos de transform da empunhadura, dedos e corda são preservados. O corte de peso testado usa cerca de 67 ms para entrar e 84 ms para sair.

- **Soltura.** `OnBowRelease` em **0,316667 s**, quadro **20** no Blender com início em 1. Corresponde ao intervalo 19 desde zero e a **48,72%** de Bow Shot. O clip termina em 0,65 s, dentro do intervalo mínimo de 0,80 s do combate.
- **Origem e direção.** `Arrow_Spawn` é a origem visual. Use `normalize(Arrow_Aim.position - Arrow_Spawn.position)` para o eixo, conforme a política de mira do jogo. `Bow_Grip` está sob `Hand.L` e `Socket_DrawHand` sob `Hand.R`.
- **Integração.** O evento existe no `.anim` e o contrato está em [eventos-e-duracoes.json](unity/eventos-e-duracoes.json). O recebedor do callback e a conexão com `LuccaCombate` ficam para a integração. O teste confirmou o tempo serializado e a resposta da corda, sem disparar um projétil no jogo.
- **Corda no Humanoid.** Calibrei a curva de posição do nock no rig importado, pois a conversão para músculos deslocava a mão direita em relação ao arco. Durante a sustentação, o desvio máximo entre `Bow_StringNock` e `Socket_DrawHand` foi de **0,0071 mm** nas quatro fases de Walk testadas. A corda alcançou um curso de **0,326 m** e voltou à posição de repouso após a soltura.

## Deformação e testes em movimento

Reabri os arquivos e comparei coordenadas de vértices, topologia, UVs, pesos e slots das **67 peças** com a Sprint 2B.2. O repouso e a hierarquia dos **57 ossos** são iguais aos da base, e o FBX do modelo é idêntico byte a byte. Os ajustes de folga estão nas curvas de animação.

A auditoria no Blender usou BVH em **2.427 amostras**, incluindo subquadros a cada 1/240 s. Os pares monitorados cobrem saia interna e pernas, saia externa e pernas, capa e túnica, as duas saias entre si, capa e saia, e o corpo do arco contra as saias, a capa e a túnica, incluindo as mangas. Após os ajustes de trajetória da empunhadura, a última execução registrou **zero interseções nesses pares**. O contato da mão com a empunhadura é intencional.

| Movimento | Amostras no Blender | Maior P95 da saia interna |
| --- | ---: | ---: |
| Bow Ready / Idle | 5 / 769 | 1,00× / 1,00× |
| Walk | 289 | 1,40× |
| Bow Shot | 157 | 1,00× |
| Jump | 85 | 1,13× |
| Spring Jump | 169 | 1,16× |
| Walk + Bow Shot, quatro fases | 628 | 1,40× |
| Cinco transições de locomoção | 325 | 1,13× |

O P95 compara o comprimento de cada aresta da saia interna com seu comprimento na malha em repouso. A maior deformação em Walk é localizada e ficou sem interseção na amostragem. Consulte as [medições quadro a quadro](evidencias/deformacao-blender.json). Os testes BVH foram realizados no Blender. Na Unity, a prova mediu poses, raízes, a corda e a separação das camadas, com uma [captura em URP](capturas/unity-walk-bow-shot.png).

## Unity 6.3

- Importe o FBX do rig como **Humanoid**. O `.meta` entregue preserva o mapa de 21 ossos. Os atlas Base Color estão embutidos e também disponíveis em `texturas-fbx/`.
- Use os seis `.anim` em `unity/`. Eles combinam curvas Humanoid com os canais Transform recuperados da importação Generic. Os seis FBXs separados em `fbx/` servem como fontes de intercâmbio. Use o `.anim` entregue para o disparo final, que contém a calibração da corda e o evento.
- A importação e a execução dos seis clipes passaram em Unity 6000.3.10f1. Em **316 amostras** de Walk + Bow Shot, o braço mudou até **60,0°**. Quadril, pernas e raiz conservaram os transforms da caminhada.
- As passagens Idle → Walk, Walk → Jump → Walk e Walk → Spring Jump → Walk somaram **165 amostras** no Animator. A raiz permaneceu fixa. Idle e Walk rodaram três ciclos por teste, com diferença de fechamento abaixo de 1 µm e sem deriva da raiz.
- Mantenha `Animator.applyRootMotion` desligado. O `HexPlayer` continua responsável pelo deslocamento e pela trajetória dos saltos. A cadência e o contato do Walk com o chão à velocidade de 3,5 u/s ainda precisam ser conferidos na cena de gameplay.

A [evidência da Unity](evidencias/unity.json) contém as durações, as medidas de fechamento dos loops, a importação Humanoid e os resultados da camada superior.

### Referências

- [Unity 6.3 — Animation Layers](https://docs.unity.com/en-us/engine/6000.3/manual/animation-section/animation-mecanim/animation-animator-controller/animation-state-machines/animation-layers)
- [Unity 6.3 — AnimationEvent](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/animationevent)


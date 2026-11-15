# Resultados de validação

Editor Unity 6000.3.10f1, URP 17.3, worktree `loopia-lucca`, cena `HexPrototipo`. A auditoria Blender usou a versão 5.2.0 LTS.

## Test Runner

| Grupo | Job MCP | Resultado |
| --- | --- | ---: |
| Logic + SceneStatic | `7aea187d7e6b4670b4b182ad5b7f8daf` | 301 aprovados de 301 |
| PlayModeSmoke + PlayModeMedicao | `8cd7ed8843aa49de8ef5cafa7a7f9f32` | 17 aprovados de 17 |

Os testes em Play Mode são `UnityTest` da categoria EditMode e abrem a cena de produção. O reload de domínio separa o XML do rastreador de tarefas do MCP. Conferimos o XML de cada execução e a data de finalização. Foram cobertos Avatar Humanoid, sockets, preservação da raiz, evento e fallback por tempo do clipe, confirmação duplicada, alvo morto substituído após novo preparo, interrupção, morte antes de soltar, ausência de Animator, falta de flechas e travessia Mola–Ilha Alta. O teste de marcadores estabiliza a posição do Lucca antes de colocar cartas de relevo, pois a regra de cartas bloqueia a colocação no ar.

## Partida e apresentação

- Partida de medição com Flechas e Torre. Vitória após quatro voltas em um anel de 24 casas, 126,8 s no relógio do jogo, 21 flechas contra o chefe e uma Revoada.
- Partida sem cartas, com quatro voltas completas no anel. Derrota após 136,5 s, com 15 flechas contra o chefe, uma Revoada e uma recarga no confronto.
- A primeira partida instrumentada registrou 19 tiros, 16 no Morcego Ancião e três em inimigos comuns. O spawn comum foi desativado após os primeiros dois tiros de teste para garantir a chegada ao chefe. A Revoada permaneceu ativa. A partida terminou em derrota após quatro voltas e 102 saltos. Esse registro precede a revisão do visual da flecha e da mira no ar.
- A captura final usa a câmera próxima em Play Mode, seed 12345. O inimigo de demonstração foi configurado com mais PV e ataque zero. Três solturas foram observadas com o agente a 3,5 unidades/s. As quatro solturas saíram no socket (desvio medido 0), entre 0,318 e 0,333 s do clipe, com quantização do frame e aplicação da cadência. Os intervalos entre elas foram 0,804–0,818 s, com cadência mínima de 0,8 s.
- A mesma captura registra Jump de 0,35 s e Spring Jump de 0,70 s, com retorno ao NavMesh. A maior diferença de posição local em `VisualRoot` e no osso `Root` foi cerca de 0,00000002 unidade. Inspecionamos a montagem de dia e a captura noturna a 22h. A amostragem do aproximar da mão inclui antes de encostar na corda. O menor erro medido com a mão já na corda foi ~0,00000024 unidade.
- O vídeo de 10 s contém 220 frames em 800×450. A troca de local é parte do setup entre o ensaio de tiro e o ensaio das cartas de mola/relevo. Os arquivos de apoio incluem fotos do Game View, diagnósticos em câmera próxima e telemetria.
- O console final não registrou erros. O único aviso pós-suíte inspecionado antes de reentrar em Play Mode foi do MCP ao limpar o job que ficou pendente após o reload. Depois do comparativo visual, o console do Editor estava sem erros e avisos. O save original foi restaurado e teve SHA-256 conferido.

## Otimização

Consolidamos 72.170 chaves constantes redundantes dos clipes importados. Preservamos os primeiros e últimos pontos das curvas, bem como eventos, loops, duração, Channel Bindings e Avatar Humanoid. A comparação de poses no Editor a 120 amostras por segundo mostrou erro máximo de 0,00000020 unidade e 0,000064°. A soma dos seis .anim passou de 61.720.155 para 32.017.647 bytes (−48,1%). A memória de assets medida pelo Profiler do Editor passou de 10.859.496 para 9.148.893 bytes (−15,8%).

O personagem usa duas malhas, um material compartilhado por malha e nenhuma instância de material por tiro. A flecha usa uma malha de três submalhas. O recebimento do evento fora de quadro mantém o Animator do protagonista em `AlwaysAnimate`. No comparativo em repouso, a mediana de `Animators.Update` foi 0,0502 ms com Lucca e 0 ms com o protótipo, em 12 amostras de marcador por condição. `UnityStats.drawCalls` reportou mediana de 427 para a cena nas duas condições. É uma medida curta no Editor com a mesma câmera. O custo em build segue pendente.

## Evidências

No workspace de entrega, abra `outputs/Loopia-Integracao-Lucca`. O arquivo `Evidencias/partida-instrumentada-previa.txt` guarda a primeira partida. `Evidencias/video-qa.txt` guarda a demonstração final. Os XMLs do Test Runner e as métricas completas de vitória e derrota ficam na mesma pasta.

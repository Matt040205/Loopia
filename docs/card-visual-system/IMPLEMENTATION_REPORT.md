# Sistema visual de cartas

Branch **feature/card-visual-system**, base **integration/mvp-20261117** no commit **f49cee1**.
O prefab foi preparado para ativação isolada. O [PR #12](https://github.com/Matt040205/Loopia/pull/12) ainda
edita **HexPrototipo.unity**. A ativação na cena versionada fica para a integração após o término dessa edição.

## Modelo, assets e tipografia

| Caminho | Conteúdo |
|---|---|
| Assets/Arte/Cartas/Arte | Moldura original, gradiente original, máscara derivada da silhueta, cinco céus e planos atmosféricos, 14 miniaturas, seis ícones e seis detalhes A/B |
| Assets/Arte/Cartas | Catálogo com referências diretas a PlataformaDef, texturas, fontes e materiais. Prefabs Carta e HUD de Cartas |
| Assets/Scripts/Hex/Cartas | Composição, parallax, HUD, tooltip e efeito de construção |
| Assets/Editor | Autoria serializada dos prefabs e exportação ortográfica usando os marcadores e modelos reais |
| tools/card-visual-system | Composição de texturas finais a partir dos renders de autoria |
| Assets/Tests/Editor/CartasVisuaisTests.cs | Cobertura do catálogo e prova do arraste real por eventos do Input System |

A moldura frontal usa **T_Card_Border_Blank.png** em 1024 × 1424. O cartão tem 256 × 356 unidades uGUI e mantém a
proporção em todas as escalas. O gabarito orientou a placa do título (x140–884, y92–196), o pivô (512,720), a
zona segura do objeto (x140–884, y300–1172), o rodapé (a partir de y1180) e o selo de valor (x744–908, y1240–1320).
A área da arte fica sob a moldura. As coordenadas de montagem e as áreas de texto trabalham em quartos de pixel
do modelo e incluem margem interna para a tipografia.

**Marcellus** com atlas TMP SDF latino para o título e **Liberation Sans** do projeto para texto, números e regras.
Ambas são distribuídas sob SIL Open Font License 1.1. Os avisos estão junto às fontes no projeto e devem
acompanhar um pacote de distribuição que inclua a HUD.
[Licença da Marcellus](../../Assets/Arte/Cartas/Fontes/Marcellus-OFL.txt) ·
[Licença da Liberation Sans](../../Assets/Settings/TextMesh%20Pro/Fonts/LiberationSans%20-%20OFL.txt)

O [catálogo de arte](CARD_ART_CATALOG.md) identifica as 14 cartas, os prefabs de origem ou o desenho original, e os
arquivos editáveis. O ZIP forneceu os PNGs de referência. As miniaturas, camadas, ícones e detalhes animados foram
produzidos localmente.

## Composição, materiais e estados

- **Ouro.mat** usa o shader uGUI **OuroDaCarta**. A diagonal percorre devagar apenas o pigmento dourado amostrado
  em **T_Rainbow_Gold.png**. O shader conserva o alfa original, usa a profundidade da UI e suporta stencil e clipping.
  O título fica em uma camada TMP independente. Foi usado HLSL/ShaderLab para controlar as convenções de uGUI e
  conservar a transparência e os canais de cor do PNG.
- **Pulso.mat** usa **PulsoDaCarta**, um passe URP transparente sem sombra. O anel hexagonal é reutilizado e recebe
  a cor por MaterialPropertyBlock. O pulso é ignorado pelo NavMesh e dura 0,46 s.
- A máscara de stencil fecha a silhueta externa da moldura e deixa os quatro cantos transparentes. O RectMask2D
  recorta a janela. Em unidades do cartão, o céu pode deslocar-se até 7, a atmosfera até 3,5 em oposição ao
  cursor, a miniatura até 2,4 na direção do cursor e os detalhes até 3,6. Céu e atmosfera têm 10 unidades de
  margem em volta da janela. O retorno ao repouso usa suavização exponencial. Moldura e textos permanecem
  fixos na composição da carta. A/B usa interpolação com período de cerca de 3,85 s em hover, prévia ou arraste.
- O Canvas instancia cada slot uma vez. A mão reage a **AoMudar**, e o indicador a **AoMudarPendentes**.
  O slot vazio mostra a silhueta apagada. Aquisição reforça temporariamente o dourado. O hover eleva a carta
  e abre uma prévia lateral com o efeito completo e a regra. O tooltip de plataforma colocada segue disponível.
- **CartasUI** mantém o único arraste, a grade e o raycast. Com Canvas conectado, o clique na mão passa por
  hit-test dos retângulos uGUI. O fantasma preserva a proporção e deixa o cursor visível sobre a casa.
  **MapaDePlataformas.PodeColocar** governa o marcador e a mensagem. **Colocar** retorna a plataforma antes de
  **MaoDeCartas.Gastar**. O consumo contrai e apaga o fantasma; o pulso marca a ilha correspondente.
- A barra antiga reaparece ao remover ou desativar a HUD. Uma configuração visual incompleta também usa o
  caminho IMGUI. As duas apresentações ocultam a mão e encerram o arraste ao fim da jornada.

## Capturas

- [Moldura fornecida e Torre renderizada no Unity](capturas/torre-comparacao.png)
- [Galeria das 14 cartas renderizada no Unity](capturas/catalogo.png)
- [HUD anterior no player do MVP](../integration/capturas/buildqa_01_partida.png)
- [Mão, prévia e pendentes em 1920 × 1080](capturas/hover-1920.jpg)
- [Mão, prévia e pendentes em 3440 × 1440](capturas/hover-3440.jpg)
- [Soltura inválida no acampamento](capturas/colocacao-invalida-1920.jpg) ·
  [Soltura válida no anel](capturas/colocacao-valida-1920.jpg) · [Consumo e pulso](capturas/consumo-1920.jpg)

## QA

Unity **6000.3.10f1**, URP **17.3.0**, uGUI **2.0.0** e TextMeshPro existente no projeto.
**Build de QA.** O Unity gerou um player Windows x64 Development. Um callback de build instanciou o prefab
na cópia em memória da cena HexPrototipo e a execução começou diretamente na partida. O player de QA usou
um diretório de save próprio. Os scripts temporários usados na geração do player e nas capturas ficaram
fora do commit.

O teste enviou eventos do Input System para selecionar e mover uma carta. Confirmou o bloqueio no acampamento,
a colocação válida com consumo após sucesso, a entrega da recompensa pendente e a ocultação na derrota e
na vitória. A prova de vitória acionou as transições do diretor de partida de forma controlada.

**Captura e amostragem.** O Windows limitou a janela oculta a 1920 × 1080. Para capturar também o perfil
ultrawide, a câmera da partida renderizou para um alvo de 1920 × 1080 ou 3440 × 1440. O harness usou
Screen Space Camera e a escala equivalente por altura somente durante essas capturas. A entrada foi
exercitada na janela 1920 × 1080. O modo normal do prefab é Screen Space Overlay.

As amostras de desempenho usam 90 quadros de uma cena parada, após 30 quadros de aquecimento, em Windows,
Direct3D 11, Ryzen 7 7700X e Radeon RX 9070 XT. A referência **Cena** é a mesma cena sem o Canvas de cartas
no alvo fora de tela. O IMGUI fica fora dessa medição. Batches e draw calls vieram do ProfilerRecorder.

| Resolução | Cena / com HUD — mediana | Cena / com HUD — P95 | Batches e draw calls | GC da cena por quadro |
|---|---:|---:|---:|---:|
| 1920 × 1080 | 0,90 / 1,19 ms | 1,15 / 1,43 ms | 271 / 297 | 9 / 5 B |
| 3440 × 1440 | 1,18 / 1,47 ms | 1,90 / 1,85 ms | 407 / 433 | 9 / 5 B |

A seed da partida variou entre resoluções. Cada linha compara a mesma cena para medir o custo incremental
do Canvas. A build terminou com **0 erros**, e as duas execuções registraram **0 exceções** do jogo.

A arte foi alinhada para compressão em blocos de 4 px. No relatório de build, a miniatura da Torre passou
de 1,7 MB a 437,6 KB e a build de QA de 263,3 MB a 223,2 MB. Moldura e gradiente permaneceram byte a byte
iguais aos PNGs entregues. O player reutiliza cartões, materiais, o pulso e as texturas de paisagem.
Desempenho em hardware de entrada e entrada física em monitor ultrawide ficam pendentes.

**Test Runner.** A suíte inteira passou **372/372** em EditMode (**Logic 309**, **SceneStatic 38**,
**PlayModeSmoke 24**, **PlayModeMedicao 1**) antes do ajuste final de posição da mensagem. Execução do
15/11/2026, 21h27 BRT, ID XML 2 em **%TEMP%/Loopia-Cards-QA/resultados-finais.xml**. O save local foi
restaurado com SHA-256 idêntico. Os dois testes de cartas passaram na revisão final em **confirmacao-posicao.xml**.

## Ativação após a estabilização

1. Depois do término das edições da cena pelo PR #12, integre o trabalho aprovado na base comum e reabra
   **HexPrototipo** no Editor numa worktree exclusiva.
2. Instancie [HUD de Cartas.prefab](../../Assets/Arte/Cartas/HUD%20de%20Cartas.prefab) na raiz de **HexPrototipo** pelo
   Editor. O campo **Origem** pode receber o componente CartasUI da cena; vazio usa a busca padrão. Mantenha
   o CartasUI da cena ativo para preservar o arraste, a validação e o retorno ao IMGUI. Ao incorporar o PR #12,
   preserve EscalaDaInterface no ramo IMGUI de OnGUI. O Canvas usa CanvasScaler.
3. Entre em Play e repita arraste válido e inválido, slot vazio, fila, Ilha Alta, resultado e verificação
   nos dois perfis. Rode toda a suíte e confira o console e uma build Windows pelas cenas de MVP.
4. Grave a cena pelo Editor em PR de integração da cena. Para rollback, desative ou remova o prefab, ou reverta
   o commit de ativação no Git. O PR de cartas pode ser revertido isoladamente antes dessa ativação.

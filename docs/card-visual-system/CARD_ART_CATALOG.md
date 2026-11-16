# Catálogo de arte das cartas

O [catálogo visual](../../Assets/Arte/Cartas/Catalogo.asset) referencia cada [PlataformaDef](../../Assets/Dados/Plataformas)
pelo asset do projeto. Os dados de combate, sorteio, recompensa e posicionamento continuam nas definições de gameplay.
A apresentação usa o título acentuado, o resumo curto, o ícone e a cor de destaque do catálogo. O tooltip lê a
descrição e a categoria de colocação da definição.

As famílias de céu, terreno distante e névoa são compartilhadas. Cada carta tem uma miniatura transparente própria.
As construções reutilizam a silhueta e o material do marcador sempre que o jogo fornece um. A ilha das miniaturas é
uma base de apresentação e reproduz a forma hexagonal do tabuleiro. Os três desenhos G2 representam seus bônus
com pedra, água, madeira e materiais já usados visualmente no Loopia.

| Carta | Miniatura em Assets/Arte/Cartas/Arte | Referência e decisão artística | Detalhe A/B |
|---|---|---|---|
| Básica | Basica_Arte.png | Marcador Básica, laje e pedras baixas numa ilha de grama e solo | — |
| Floresta | Floresta_Arte.png | Marcador Floresta, mesmo grupo de copas e arbustos, com uma ilha | — |
| Ilha Alta | IlhaAlta_Arte.png | Marcador Ilha Alta, bandeirola rosa e pilha de pedras sobre um penhasco facetado | — |
| Mola | Mola_Arte.png | Prefab Mola, espiral de aço e duas placas circulares | — |
| Flechas | Flechas_Arte.png | Múltiplas instâncias do prefab Flecha numa aljava original de couro com aros de latão | — |
| Coração | Coracao_Arte.png | Prefab Coração, mesmo volume vermelho girado para a vista ortográfica | — |
| Moedas | Moedas_Arte.png | Três instâncias de Moeda em inclinações distintas, com brilho dourado discreto | Reflexos |
| Torre | Torre_Arte.png | Marcador Torre, alvenaria circular, ameias e bandeira do próprio tabuleiro | — |
| Área de Treino | AreaDeTreino_Arte.png | Marcador Área de Treino, dois alvos, boneco de palha e fardo | — |
| Cabana | Cabana_Arte.png | Marcador Cabana, barraca inclinada do jogo, lenha e caixote na ilha | — |
| Campo Pacífico | CampoPacifico_Arte.png | Marcador Campo Pacífico, leiras plantadas e espantalho | — |
| Santuário | Santuario_Arte.png | Altar novo de pedra com cristal e três pontos luminosos | Luz do cristal |
| Fonte Restauradora | FonteRestauradora_Arte.png | Fonte nova de duas bacias, pedra clara e jatos turquesa | Gotas luminosas |
| Oficina de Arqueiro | OficinaDeArqueiro_Arte.png | Bancada nova, arco, aljava, peças de madeira e martelo | — |

A torre, as demais construções estáticas e os objetos rígidos mantêm uma só pose. As três animações opcionais
interpolam somente o detalhe transparente. A miniatura e o pivô permanecem estáveis durante a interpolação.

## Atualizar uma carta

1. Abra **Assets → Arte → Cartas → Catalogo** no Inspector e encontre o campo **Carta** que referencia a definição desejada.
2. Substitua o PNG da miniatura no mesmo caminho para preservar seu .meta, ou atribua ao campo **Objeto** uma textura nova
   com seu .meta. Use RGBA em 640 × 700 e deixe folga para o parallax. O quadro da arte corresponde à janela em
   **CartaVisual.Montar**. O centro de composição fica perto de (320, 313) na textura, perto do pivô do gabarito.
3. Atribua **Fundo** e **Atmosfera** da mesma família. Deixe **Pose A** e **Pose B** vazias quando não houver detalhe móvel.
   Para uma animação, use duas texturas transparentes de 640 × 700 no mesmo alinhamento.
4. Ajuste **Resumo**, **Valor**, **Categoria** e **Destaque** conforme a descrição da definição. Edite efeito, regra e
   números de gameplay na respectiva PlataformaDef quando o design mudar.
5. Confira a miniatura, o tooltip, o recorte e o arraste no perfil compacto. Rode **CatalogoCobreAsDefinicoesComArtePropriaEAssetsDeUi**.

Para refazer os renders, use **Loopia → Cartas → Exportar miniaturas ortográficas**. O comando grava PNGs transparentes em
**%TEMP%/Loopia-Cards-QA** ou em **LOOPIA_CARD_QA**. Rode em seguida o script abaixo a partir da raiz do repositório.
O Pillow é uma ferramenta opcional de autoria externa ao Unity. Os PNGs finais já estão versionados.

~~~powershell
python tools/card-visual-system/compor_arte.py "$env:TEMP/Loopia-Cards-QA" Tudo
~~~

Para autorar uma carta, forneça o ID no lugar de **Tudo**. Se trocar a composição serializada do prefab ou recriar as
referências padrão, use **Loopia → Cartas → Regravar catálogo e prefabs**. Esse comando reaplica as associações da tabela
em [MontagemDasCartas.cs](../../Assets/Editor/MontagemDasCartas.cs). Para manter uma referência personalizada após
regenerar o catálogo, ajuste a tabela de autoria.

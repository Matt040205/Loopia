using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Loopia.Hex
{
    [DisallowMultipleComponent]
    public sealed class CartaVisual : MonoBehaviour
    {
        public static readonly Vector2 Tamanho = new Vector2(256, 356);
        static readonly Color Marfim = new Color32(255, 235, 183, 255);
        static readonly Color TextoSuave = new Color32(237, 220, 194, 255);

        [SerializeField] CanvasGroup grupo;
        [SerializeField] RectTransform conteudo;
        [SerializeField] RawImage moldura;
        [SerializeField] RawImage categoria;
        [SerializeField] TextMeshProUGUI titulo;
        [SerializeField] TextMeshProUGUI resumo;
        [SerializeField] TextMeshProUGUI valor;
        [SerializeField] ParallaxDaCarta parallax;

        public RectTransform Caixa => (RectTransform)transform;
        public PlataformaDef Def { get; private set; }
        /// <summary>Espaco da mao sem carta (leitura propria, diferente de carta, pendente e arraste).</summary>
        public bool Vazio => Def == null;
        public bool Pronta => grupo != null && conteudo != null && moldura != null && parallax != null;
        float _altura;
        float _brilho;
        float _aquisicao;
        bool _compacta;
        Leiaute _leiauteCompleto;
        TextMeshProUGUI _espacoLivre;

        /// <summary>Retangulo e tamanhos de fonte de um texto, para alternar entre carta cheia e compacta.</summary>
        struct Medidas
        {
            public Vector2 posicao, tamanho;
            public float maximo, minimo;
            public bool ativo;
        }

        struct Leiaute
        {
            public bool lido;
            public Medidas titulo, valor;
            public Vector2 posicaoCategoria, tamanhoCategoria;
        }

        // Montagem de autoria usada para gravar o prefab. Coordenadas do gabarito em 1/4.
        public void Montar(CatalogoVisualDeCartas catalogo)
        {
            Caixa.sizeDelta = Tamanho;
            grupo = gameObject.AddComponent<CanvasGroup>();
            grupo.blocksRaycasts = false;
            grupo.interactable = false;
            conteudo = ElementosDaCarta.Caixa(transform, "Composicao");

            var recorte = ElementosDaCarta.Painel(conteudo, "Silhueta", Color.white);
            recorte.sprite = catalogo.recorte;
            recorte.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            ElementosDaCarta.Painel(recorte.transform, "Suporte da carta", ElementosDaCarta.Cor("#211C31"));

            var janela = ElementosDaCarta.Caixa(recorte.transform, "Janela ilustrada");
            NoModelo(janela, new Rect(11, 43, 234, 255));
            janela.gameObject.AddComponent<RectMask2D>();
            parallax = new ParallaxDaCarta(janela);

            moldura = ElementosDaCarta.Textura(conteudo, "Moldura original", catalogo.moldura);
            moldura.material = catalogo.ouro;
            titulo = ElementosDaCarta.Texto(conteudo, "Titulo", catalogo.fonteTitulo, 25, Marfim);
            ElementosDaCarta.AjustarFonte(titulo, 10, 25);
            NoModelo(titulo.rectTransform, new Rect(38, 24, 180, 24));

            categoria = ElementosDaCarta.Textura(conteudo, "Categoria");
            NoModelo(categoria.rectTransform, new Rect(25, 309, 23, 23));
            resumo = ElementosDaCarta.Texto(conteudo, "Efeito compacto", catalogo.fonteCorpo, 17,
                TextoSuave, TextAlignmentOptions.MidlineLeft);
            ElementosDaCarta.AjustarFonte(resumo, 11, 17);
            NoModelo(resumo.rectTransform, new Rect(52, 302, 128, 36));

            valor = ElementosDaCarta.Texto(conteudo, "Valor", catalogo.fonteCorpo, 16, Marfim);
            valor.fontStyle = FontStyles.Bold;
            ElementosDaCarta.AjustarFonte(valor, 9, 16);
            NoModelo(valor.rectTransform, new Rect(187, 311, 39, 18));
        }

        /// <summary>
        /// Leitura na mao (escala pequena): titulo maior ocupando a placa, selo de valor e icone maiores, e o resumo
        /// sai da carta (vai para o tooltip ampliado). Moldura, arte, shader e parallax nao mudam.
        /// </summary>
        public void Compactar(bool compacta)
        {
            if (!_leiauteCompleto.lido)
            {
                _leiauteCompleto = new Leiaute
                {
                    lido = true,
                    titulo = Ler(titulo),
                    valor = Ler(valor),
                    posicaoCategoria = categoria.rectTransform.anchoredPosition,
                    tamanhoCategoria = categoria.rectTransform.sizeDelta,
                };
            }
            if (_compacta == compacta) return;
            _compacta = compacta;
            if (compacta)
            {
                // Placa do titulo: x140-884, y92-196 do gabarito 1024 x 1424 (= 35-221, 23-49 em unidades da carta).
                Aplicar(titulo, new Rect(43, 18, 170, 36), 27, 17);
                // Selo de valor: x744-908, y1240-1320 (= 186-227, 310-330).
                Aplicar(valor, new Rect(180, 307, 53, 26), 25, 16);
                NoModelo(categoria.rectTransform, new Rect(21, 303, 31, 31));
                resumo.gameObject.SetActive(false);
            }
            else
            {
                Restaurar(titulo, _leiauteCompleto.titulo);
                Restaurar(valor, _leiauteCompleto.valor);
                categoria.rectTransform.anchoredPosition = _leiauteCompleto.posicaoCategoria;
                categoria.rectTransform.sizeDelta = _leiauteCompleto.tamanhoCategoria;
                resumo.gameObject.SetActive(true);
            }
            AtualizarEspacoLivre();
        }

        static Medidas Ler(TMP_Text texto) => new Medidas
        {
            posicao = texto.rectTransform.anchoredPosition,
            tamanho = texto.rectTransform.sizeDelta,
            maximo = texto.fontSizeMax,
            minimo = texto.fontSizeMin,
        };

        static void Aplicar(TMP_Text texto, Rect area, float maximo, float minimo)
        {
            NoModelo(texto.rectTransform, area);
            texto.fontSizeMax = maximo;
            texto.fontSizeMin = minimo;
        }

        static void Restaurar(TMP_Text texto, Medidas m)
        {
            texto.rectTransform.anchoredPosition = m.posicao;
            texto.rectTransform.sizeDelta = m.tamanho;
            texto.fontSizeMax = m.maximo;
            texto.fontSizeMin = m.minimo;
        }

        /// <summary>Espaco da mao sem carta: silhueta apagada e a indicacao "Espaço livre" (so na mao).</summary>
        void AtualizarEspacoLivre()
        {
            bool mostrar = _compacta && Def == null;
            if (mostrar && _espacoLivre == null)
            {
                _espacoLivre = ElementosDaCarta.Texto(conteudo, "Espaco livre", titulo.font, 30, Marfim);
                NoModelo(_espacoLivre.rectTransform, new Rect(28, 150, 200, 76));
                _espacoLivre.textWrappingMode = TextWrappingModes.Normal;
                _espacoLivre.text = "Espaço\nlivre";
                _espacoLivre.richText = false;
            }
            if (_espacoLivre != null) _espacoLivre.gameObject.SetActive(mostrar);
        }

        static void NoModelo(RectTransform caixa, Rect a)
        {
            ElementosDaCarta.Retangulo(caixa, Vector2.up, Vector2.up, new Rect(a.x, -a.y, a.width, a.height));
        }

        public void Exibir(PlataformaDef def, ArteDeCarta arte, bool adquirida = false)
        {
            Def = def;
            parallax.Exibir(arte);
            titulo.text = arte?.titulo ?? def?.nomeExibido ?? "";
            resumo.text = arte?.resumo ?? "";
            valor.text = arte?.valor ?? "";
            categoria.texture = arte?.categoria;
            categoria.gameObject.SetActive(categoria.texture != null);
            categoria.color = arte?.destaque ?? Color.white;
            grupo.alpha = def != null ? 1f : AlfaDoVazio;
            _aquisicao = adquirida ? 1f : 0f;
            AtualizarEspacoLivre();
        }

        /// <summary>Opacidade do espaco vazio: a silhueta fica visivel, mas claramente sem carta.</summary>
        public const float AlfaDoVazio = .42f;

        public void Animar(Vector2 cursor, bool elevado, bool animarArte, float dt)
        {
            float alvo = elevado ? 10f : 0f;
            _altura = Mathf.Lerp(_altura, alvo, 1f - Mathf.Exp(-15f * dt));
            if (Mathf.Abs(_altura - alvo) < .025f) _altura = alvo;
            conteudo.anchoredPosition = Vector2.up * _altura;
            _aquisicao = Mathf.MoveTowards(_aquisicao, 0f, dt * 2.7f);
            float brilho = Mathf.Max(_aquisicao, elevado || animarArte ? .78f : 0f);
            _brilho = Mathf.MoveTowards(_brilho, brilho, dt * 5f);
            moldura.color = new Color(1f, 1f - _brilho, 1f, 1f);
            titulo.color = Color.Lerp(Marfim, new Color32(255, 251, 228, 255), _brilho * .75f);
            parallax.Atualizar(cursor, animarArte, dt);
            // Uma recompensa que chega a mao cresce de leve e assenta (sem animacao permanente).
            float pulo = _aquisicao > 0f ? 1f + .05f * Mathf.Sin(_aquisicao * Mathf.PI) : 1f;
            conteudo.localScale = Vector3.one * pulo;
        }

        public void Opacidade(float alpha) => grupo.alpha = alpha;
    }
}

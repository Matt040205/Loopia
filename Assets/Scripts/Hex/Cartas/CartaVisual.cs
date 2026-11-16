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
            grupo.alpha = def != null ? 1f : .31f;
            _aquisicao = adquirida ? 1f : 0f;
        }

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
        }

        public void Opacidade(float alpha) => grupo.alpha = alpha;
    }
}

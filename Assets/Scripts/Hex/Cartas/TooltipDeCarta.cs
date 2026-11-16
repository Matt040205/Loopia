using TMPro;
using UnityEngine;

namespace Loopia.Hex
{
    internal sealed class TooltipDeCarta
    {
        readonly RectTransform _raiz;
        readonly CartaVisual _previa;
        readonly RectTransform _texto;
        readonly TextMeshProUGUI _titulo;
        readonly TextMeshProUGUI _destaque;
        readonly TextMeshProUGUI _corpo;
        readonly TextMeshProUGUI _regra;
        PlataformaDef _def;
        bool _ampliar;

        // Painel de texto 316 x 286 e previa da carta a 0,7 (179 x 249): nome 22-28 px, efeito 16-19 px, regra 15-17 px
        // em 1080p (antes 17-23 / 13,5-16 / 13-15). Cabe nome, resumo + valor, efeito, regra e condicao das 14 cartas.
        const float LarguraDoTexto = 316f;
        const float AlturaDoTexto = 286f;
        const float EscalaDaPrevia = .7f;

        public TooltipDeCarta(RectTransform pai, CartaVisual prefab, CatalogoVisualDeCartas catalogo)
        {
            float largura = CartaVisual.Tamanho.x * EscalaDaPrevia + 8f + LarguraDoTexto;
            _raiz = ElementosDaCarta.Caixa(pai, "Detalhes da carta");
            ElementosDaCarta.Retangulo(_raiz, Vector2.right, Vector2.right, new Rect(-22, 19, largura, AlturaDoTexto + 3f));

            _previa = Object.Instantiate(prefab, _raiz, false);
            ElementosDaCarta.Retangulo(_previa.Caixa, Vector2.zero, Vector2.zero, new Rect(0, 0, 256, 356));
            _previa.Caixa.localScale = Vector3.one * EscalaDaPrevia;

            var filete = ElementosDaCarta.Painel(_raiz, "Filete do tooltip", ElementosDaCarta.Cor("#A68A57"));
            ElementosDaCarta.Retangulo(filete.rectTransform, Vector2.right, Vector2.right,
                new Rect(0, 0, LarguraDoTexto, AlturaDoTexto));
            var painel = ElementosDaCarta.Painel(filete.transform, "Fundo do tooltip", ElementosDaCarta.Cor("#211C30F7"));
            ElementosDaCarta.Preencher(painel.rectTransform, 1f);
            _texto = painel.rectTransform;
            _titulo = Rotulo("Nome", catalogo.fonteTitulo, 28, "#FFE8B3", 10, 44);
            ElementosDaCarta.AjustarFonte(_titulo, 22, 28);
            _destaque = Rotulo("Resumo e valor", catalogo.fonteCorpo, 18, "#E9CF95", 56, 26);
            _destaque.fontStyle = FontStyles.Bold;
            ElementosDaCarta.AjustarFonte(_destaque, 15, 18);
            _corpo = Rotulo("Efeito", catalogo.fonteCorpo, 19, "#F0E4D2", 88, 104);
            ElementosDaCarta.AjustarFonte(_corpo, 16, 19);
            _regra = Rotulo("Regra", catalogo.fonteCorpo, 17, "#E5C887", 198, 80);
            ElementosDaCarta.AjustarFonte(_regra, 15, 17);
            foreach (var t in new[] { _titulo, _destaque, _corpo, _regra })
            {
                t.textWrappingMode = TextWrappingModes.Normal;
                t.overflowMode = TextOverflowModes.Overflow;
            }
            _titulo.textWrappingMode = TextWrappingModes.NoWrap;
            Ocultar();
        }

        TextMeshProUGUI Rotulo(string nome, TMP_FontAsset fonte, float tamanho, string cor, float y, float altura)
        {
            var texto = ElementosDaCarta.Texto(_texto, nome, fonte, tamanho, ElementosDaCarta.Cor(cor),
                TextAlignmentOptions.TopLeft);
            ElementosDaCarta.Retangulo(texto.rectTransform, Vector2.up, Vector2.up,
                new Rect(15, -y, LarguraDoTexto - 30f, altura));
            return texto;
        }

        public void Mostrar(PlataformaDef def, ArteDeCarta arte, bool ampliar)
        {
            if (_def == def && _ampliar == ampliar && _raiz.gameObject.activeSelf) return;
            _def = def;
            _ampliar = ampliar;
            _raiz.gameObject.SetActive(true);
            _previa.gameObject.SetActive(ampliar);
            if (ampliar) _previa.Exibir(def, arte);
            _titulo.text = arte?.titulo ?? def.nomeExibido;
            string valor = arte?.valor;
            string resumo = arte?.resumo;
            _destaque.text = string.IsNullOrEmpty(valor) ? resumo ?? "" :
                string.IsNullOrEmpty(resumo) ? valor : resumo + " · " + valor;
            _destaque.gameObject.SetActive(!string.IsNullOrEmpty(_destaque.text));
            _corpo.text = def.descricao;
            switch (def.onde)
            {
                case ColocacaoPermitida.NoCaminho: _regra.text = "Coloque no caminho."; break;
                case ColocacaoPermitida.ForaDoCaminho: _regra.text = "Coloque fora do caminho."; break;
                default: _regra.text = "Coloque no caminho ou fora dele."; break;
            }
            if (!string.IsNullOrEmpty(arte?.condicao)) _regra.text += "\n" + arte.condicao;

            // A regra vem logo depois do efeito (sem vao fixo); tudo continua dentro do painel.
            float topoDoEfeito = _destaque.gameObject.activeSelf ? 88f : 60f;
            ElementosDaCarta.Retangulo(_corpo.rectTransform, Vector2.up, Vector2.up,
                new Rect(15, -topoDoEfeito, LarguraDoTexto - 30f, 104));
            float altura = Mathf.Min(_corpo.GetPreferredValues(_corpo.text, LarguraDoTexto - 30f, 0).y, 104f);
            float topoDaRegra = Mathf.Min(topoDoEfeito + altura + 14f, AlturaDoTexto - 84f);
            ElementosDaCarta.Retangulo(_regra.rectTransform, Vector2.up, Vector2.up,
                new Rect(15, -topoDaRegra, LarguraDoTexto - 30f, AlturaDoTexto - topoDaRegra - 8f));
        }

        /// <summary>Textos do tooltip (para teste de cabimento): nome, resumo/valor, efeito e regra.</summary>
        internal TextMeshProUGUI[] Textos => new[] { _titulo, _destaque, _corpo, _regra };

        public void Animar(Vector2 cursor, float dt)
        {
            if (_raiz.gameObject.activeSelf && _ampliar) _previa.Animar(cursor, false, true, dt);
        }

        public void Ocultar() => _raiz.gameObject.SetActive(false);
    }
}

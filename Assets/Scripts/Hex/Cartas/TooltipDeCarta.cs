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
        readonly TextMeshProUGUI _corpo;
        readonly TextMeshProUGUI _regra;
        PlataformaDef _def;
        bool _ampliar;

        public TooltipDeCarta(RectTransform pai, CartaVisual prefab, CatalogoVisualDeCartas catalogo)
        {
            _raiz = ElementosDaCarta.Caixa(pai, "Detalhes da carta");
            ElementosDaCarta.Retangulo(_raiz, Vector2.right, Vector2.right, new Rect(-22, 19, 392, 223));

            _previa = Object.Instantiate(prefab, _raiz, false);
            ElementosDaCarta.Retangulo(_previa.Caixa, Vector2.zero, Vector2.zero, new Rect(0, 0, 256, 356));
            _previa.Caixa.localScale = Vector3.one * .61f;

            var filete = ElementosDaCarta.Painel(_raiz, "Filete do tooltip", ElementosDaCarta.Cor("#A68A57"));
            ElementosDaCarta.Retangulo(filete.rectTransform, Vector2.right, Vector2.right, new Rect(0, 0, 226, 218));
            var painel = ElementosDaCarta.Painel(filete.transform, "Fundo do tooltip", ElementosDaCarta.Cor("#211C30F7"));
            ElementosDaCarta.Preencher(painel.rectTransform, 1f);
            _texto = painel.rectTransform;
            _titulo = Rotulo("Nome", catalogo.fonteTitulo, 21, "#FFE8B3", 12, 40);
            _corpo = Rotulo("Efeito", catalogo.fonteCorpo, 16, "#F0E4D2", 57, 84);
            _regra = Rotulo("Regra", catalogo.fonteCorpo, 14.5f, "#E5C887", 145, 62);
            Ocultar();
        }

        TextMeshProUGUI Rotulo(string nome, TMP_FontAsset fonte, float tamanho, string cor, float y, float altura)
        {
            var texto = ElementosDaCarta.Texto(_texto, nome, fonte, tamanho, ElementosDaCarta.Cor(cor),
                TextAlignmentOptions.TopLeft);
            ElementosDaCarta.Retangulo(texto.rectTransform, Vector2.up, Vector2.up, new Rect(13, -y, 198, altura));
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
            _corpo.text = def.descricao;
            switch (def.onde)
            {
                case ColocacaoPermitida.NoCaminho: _regra.text = "Coloque no caminho."; break;
                case ColocacaoPermitida.ForaDoCaminho: _regra.text = "Coloque fora do caminho."; break;
                default: _regra.text = "Coloque no caminho ou fora dele."; break;
            }
            if (!string.IsNullOrEmpty(arte?.condicao)) _regra.text += "\n" + arte.condicao;
        }

        public void Animar(Vector2 cursor, float dt)
        {
            if (_raiz.gameObject.activeSelf && _ampliar) _previa.Animar(cursor, false, true, dt);
        }

        public void Ocultar() => _raiz.gameObject.SetActive(false);
    }
}

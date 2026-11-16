using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Loopia.Hex
{
    internal static class ElementosDaCarta
    {
        public static RectTransform Caixa(Transform pai, string nome)
        {
            var objeto = new GameObject(nome, typeof(RectTransform));
            objeto.layer = pai.gameObject.layer;
            var caixa = (RectTransform)objeto.transform;
            caixa.SetParent(pai, false);
            Preencher(caixa);
            return caixa;
        }

        public static void Preencher(RectTransform caixa, float margem = 0)
        {
            caixa.anchorMin = Vector2.zero;
            caixa.anchorMax = Vector2.one;
            caixa.pivot = new Vector2(.5f, .5f);
            caixa.offsetMin = Vector2.one * margem;
            caixa.offsetMax = Vector2.one * -margem;
        }

        public static void Retangulo(RectTransform caixa, Vector2 ancora, Vector2 pivo, Rect rect)
        {
            caixa.anchorMin = ancora;
            caixa.anchorMax = ancora;
            caixa.pivot = pivo;
            caixa.sizeDelta = rect.size;
            caixa.anchoredPosition = rect.position;
        }

        public static RawImage Textura(Transform pai, string nome, Texture textura = null)
        {
            var imagem = Caixa(pai, nome).gameObject.AddComponent<RawImage>();
            imagem.texture = textura;
            imagem.raycastTarget = false;
            return imagem;
        }

        public static Image Painel(Transform pai, string nome, Color cor)
        {
            var imagem = Caixa(pai, nome).gameObject.AddComponent<Image>();
            imagem.color = cor;
            imagem.raycastTarget = false;
            return imagem;
        }

        public static TextMeshProUGUI Texto(Transform pai, string nome, TMP_FontAsset fonte, float tamanho,
            Color cor, TextAlignmentOptions alinhamento = TextAlignmentOptions.Center)
        {
            var texto = Caixa(pai, nome).gameObject.AddComponent<TextMeshProUGUI>();
            texto.font = fonte;
            texto.fontSize = tamanho;
            texto.color = cor;
            texto.alignment = alinhamento;
            texto.raycastTarget = false;
            texto.richText = false;
            texto.overflowMode = TextOverflowModes.Ellipsis;
            return texto;
        }

        public static void AjustarFonte(TMP_Text texto, float minimo, float maximo)
        {
            texto.enableAutoSizing = true;
            texto.fontSizeMin = minimo;
            texto.fontSizeMax = maximo;
        }

        public static Color Cor(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out Color cor);
            return cor;
        }
    }
}

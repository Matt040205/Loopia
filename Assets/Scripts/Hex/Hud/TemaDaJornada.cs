using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Loopia.Hex
{
    public static class TemaDaJornada
    {
        public static readonly Color Ouro = new Color32(241, 214, 154, 255);
        public static readonly Color Marfim = new Color32(250, 239, 219, 255);
        public static readonly Color Secundario = new Color32(220, 206, 186, 255);
        public static readonly Color Sucesso = new Color32(201, 229, 186, 255);
        public static readonly Color Perigo = new Color32(245, 178, 168, 255);
        public static readonly Color Aviso = new Color32(246, 193, 121, 255);

        public static Color Cor(string hex) => ElementosDaCarta.Cor(hex);

        public static void Posicionar(RectTransform caixa, Vector2 ancora, Vector2 pivo, Rect rect) =>
            ElementosDaCarta.Retangulo(caixa, ancora, pivo, rect);

        public static void NoTopo(RectTransform caixa, float x, float y, float largura, float altura) =>
            Posicionar(caixa, Vector2.up, Vector2.up, new Rect(x, -y, largura, altura));

        public static Canvas ConfigurarCanvas(GameObject raiz, int ordem)
        {
            var canvas = raiz.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = ordem;
            var scaler = raiz.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 1;
            return canvas;
        }

        public static RectTransform Janela(Transform pai, string nome, Vector2 ancora, Vector2 pivo, Rect area)
        {
            var borda = ElementosDaCarta.Painel(pai, nome, Cor("#8B724FED"));
            Posicionar(borda.rectTransform, ancora, pivo, area);
            var fundo = ElementosDaCarta.Painel(borda.transform, "Fundo ameixa", Cor("#211C31F2"));
            ElementosDaCarta.Preencher(fundo.rectTransform, 1);
            return borda.rectTransform;
        }

        public static Image Bloco(Transform pai, string nome, Color cor, float x, float y, float w, float h)
        {
            var imagem = ElementosDaCarta.Painel(pai, nome, cor);
            NoTopo(imagem.rectTransform, x, y, w, h);
            return imagem;
        }

        public static TextMeshProUGUI Rotulo(Transform pai, string nome, TMP_FontAsset fonte, string texto,
            float tamanho, Color cor, TextAlignmentOptions alinhamento, Rect area)
        {
            var rotulo = ElementosDaCarta.Texto(pai, nome, fonte, tamanho, cor, alinhamento);
            rotulo.text = texto;
            NoTopo(rotulo.rectTransform, area.x, area.y, area.width, area.height);
            return rotulo;
        }

        public static void EstilizarBotao(Button botao, TMP_Text rotulo, TMP_FontAsset fonte,
            RectTransform pai, Rect area)
        {
            botao.transform.SetParent(pai, false);
            NoTopo((RectTransform)botao.transform, area.x, area.y, area.width, area.height);
            var cores = botao.colors;
            cores.normalColor = Cor("#E0BE7F");
            cores.highlightedColor = Cor("#F9D998");
            cores.selectedColor = cores.highlightedColor;
            cores.pressedColor = Cor("#C69C5D");
            cores.disabledColor = Cor("#A39175");
            cores.colorMultiplier = 1;
            cores.fadeDuration = .12f;
            botao.colors = cores;
            botao.transition = Selectable.Transition.ColorTint;
            botao.targetGraphic.color = Color.white;
            rotulo.font = fonte;
            rotulo.fontStyle = FontStyles.Bold;
            rotulo.color = Cor("#241C30");
            rotulo.fontSize = 22;
            rotulo.enableAutoSizing = false;
            rotulo.alignment = TextAlignmentOptions.Center;
            rotulo.raycastTarget = false;
            ElementosDaCarta.Preencher(rotulo.rectTransform, 6);
        }
    }
}

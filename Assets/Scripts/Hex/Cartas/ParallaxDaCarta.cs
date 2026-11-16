using UnityEngine;
using UnityEngine.UI;

namespace Loopia.Hex
{
    [System.Serializable]
    internal sealed class ParallaxDaCarta
    {
        [SerializeField] RawImage fundo;
        [SerializeField] RawImage atmosfera;
        [SerializeField] RawImage objeto;
        [SerializeField] RawImage poseA;
        [SerializeField] RawImage poseB;

        Vector2 _posicao;
        float _fase;

        public ParallaxDaCarta(RectTransform janela)
        {
            fundo = ElementosDaCarta.Textura(janela, "Ceu");
            atmosfera = ElementosDaCarta.Textura(janela, "Atmosfera");
            objeto = ElementosDaCarta.Textura(janela, "Ilustracao");
            poseA = ElementosDaCarta.Textura(janela, "Detalhe A");
            poseB = ElementosDaCarta.Textura(janela, "Detalhe B");
            ElementosDaCarta.Preencher(fundo.rectTransform, -10f);
            ElementosDaCarta.Preencher(atmosfera.rectTransform, -10f);
        }

        public void Exibir(ArteDeCarta arte)
        {
            Trocar(fundo, arte?.fundo);
            Trocar(atmosfera, arte?.atmosfera);
            Trocar(objeto, arte?.objeto);
            Trocar(poseA, arte?.poseA);
            Trocar(poseB, arte?.poseB);
            _fase = 0f;
            Posicionar(Vector2.zero);
        }

        static void Trocar(RawImage imagem, Texture textura)
        {
            imagem.texture = textura;
            imagem.gameObject.SetActive(textura != null);
        }

        public void Atualizar(Vector2 alvo, bool animarDetalhe, float dt)
        {
            // Suavizacao independente da taxa de quadros. Uma carta em repouso so volta a sujar o Canvas se mover.
            Vector2 seguinte = Vector2.Lerp(_posicao, alvo, 1f - Mathf.Exp(-11f * dt));
            if ((seguinte - alvo).sqrMagnitude < .00005f) seguinte = alvo;
            if (seguinte != _posicao) Posicionar(seguinte);

            if (!poseA.gameObject.activeSelf || !poseB.gameObject.activeSelf) return;
            _fase += animarDetalhe ? dt : 0f;
            float mistura = .5f - .5f * Mathf.Cos(_fase * 1.63f);
            poseA.canvasRenderer.SetAlpha(1f - mistura);
            poseB.canvasRenderer.SetAlpha(mistura);
        }

        void Posicionar(Vector2 posicao)
        {
            _posicao = posicao;
            fundo.rectTransform.anchoredPosition = posicao * -7f;
            atmosfera.rectTransform.anchoredPosition = posicao * -3.5f;
            objeto.rectTransform.anchoredPosition = posicao * 2.4f;
            poseA.rectTransform.anchoredPosition = posicao * 3.6f;
            poseB.rectTransform.anchoredPosition = poseA.rectTransform.anchoredPosition;
        }
    }
}

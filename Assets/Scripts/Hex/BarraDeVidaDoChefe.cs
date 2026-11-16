using UnityEngine;

namespace Loopia.Hex
{
    /// <summary>
    /// Barra de vida do chefe no topo da tela, separada da HUD do Lucca. IMGUI, como o painel do
    /// LoopDirector e a barra de cartas do prototipo: nao precisa de Canvas nem de prefab na cena.
    /// So aparece enquanto o chefe esta vivo e a jornada nao terminou.
    /// </summary>
    [DisallowMultipleComponent]
    public class BarraDeVidaDoChefe : MonoBehaviour
    {
        [Tooltip("Deixe vazio para achar automaticamente na cena.")]
        public EncontroDoChefe encontro;
        public LoopDirector diretor;

        [Header("Aparencia")]
        [Range(0.2f, 0.9f)] public float larguraNaTela = 0.5f;
        [Min(8f)] public float altura = 18f;
        [Min(0f)] public float margemDoTopo = 14f;
        public Color corDaVida = new Color(0.78f, 0.12f, 0.16f);
        public Color corDoFundo = new Color(0f, 0f, 0f, 0.6f);
        public Color corDoAviso = new Color(1f, 0.85f, 0.2f);

        GUIStyle _estilo;

        /// <summary>Fracao de vida (0..1) do chefe vivo, ou -1 quando nao ha barra para mostrar.</summary>
        public float FracaoVisivel
        {
            get
            {
                if (encontro == null || encontro.Chefe == null || !encontro.Chefe.EstaVivo) return -1f;
                if (diretor != null && diretor.Terminou) return -1f;
                return Mathf.Clamp01((float)encontro.Chefe.PvAtual / Mathf.Max(1, encontro.Chefe.PvMaximo));
            }
        }

        void Start()
        {
            if (encontro == null) encontro = GetComponent<EncontroDoChefe>();
            if (encontro == null) encontro = FindFirstObjectByType<EncontroDoChefe>();
            if (diretor == null) diretor = FindFirstObjectByType<LoopDirector>();
        }

        void OnGUI()
        {
            float fracao = FracaoVisivel;
            if (fracao < 0f) return;

            if (_estilo == null)
            {
                _estilo = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 12,
                    fontStyle = FontStyle.Bold,
                };
                _estilo.normal.textColor = Color.white;
            }

            // Telas acima de 1080p: altura, margem e fonte escalam junto (EscalaDaInterface); a largura segue a tela.
            Matrix4x4 matrizAnterior = EscalaDaInterface.Aplicar(out float larguraDaTela, out _);
            try
            {
                Inimigo chefe = encontro.Chefe;
                float largura = larguraDaTela * larguraNaTela;
                var fundo = new Rect((larguraDaTela - largura) * 0.5f, margemDoTopo, largura, altura);
                var vida = new Rect(fundo.x + 2f, fundo.y + 2f, (fundo.width - 4f) * fracao, fundo.height - 4f);

                Color anterior = GUI.color;
                GUI.color = corDoFundo;
                GUI.DrawTexture(fundo, Texture2D.whiteTexture);
                GUI.color = encontro.AvisandoEspecial ? corDoAviso : corDaVida;
                GUI.DrawTexture(vida, Texture2D.whiteTexture);
                GUI.color = anterior;

                string nome = chefe.Def != null ? chefe.Def.nome : "Chefe";
                GUI.Label(fundo, nome + "  " + chefe.PvAtual + " / " + chefe.PvMaximo, _estilo);
            }
            finally
            {
                GUI.matrix = matrizAnterior;
            }
        }
    }
}

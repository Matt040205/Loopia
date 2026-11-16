using System;
using UnityEngine;

namespace Loopia.Hex
{
    /// <summary>
    /// Resolucao do build (Sprint 4, legibilidade). A Unity grava no registro o ultimo modo e a ultima resolucao usados,
    /// e isso vence o Player Settings: no playtest o jogo abriu numa janela de 1280x720 num monitor 1920x1080 (44% dos
    /// pixels), sem poder redimensionar. Em tela cheia com resolucao menor que a do monitor a imagem e esticada e fica
    /// borrada/pixelizada.
    ///
    /// Regras (so no player; nunca no Editor nem nos testes):
    /// 1. Tela cheia com resolucao diferente da do monitor: vai para a do monitor. Vale tambem depois de Alt+Enter.
    /// 2. Primeira abertura com esta regra (chave em PlayerPrefs), em janela: tela cheia sem borda na resolucao do
    ///    monitor. Depois disso a janela escolhida pelo jogador (Alt+Enter, redimensionar) e respeitada.
    /// 3. Resolucao ou modo passados na linha de comando (-screen-width, -window-mode, ...) nunca sao tocados.
    ///
    /// Criado sozinho antes da primeira cena (RuntimeInitializeOnLoadMethod): nao precisa estar em cena.
    /// </summary>
    [DisallowMultipleComponent]
    public class ResolucaoDaTela : MonoBehaviour
    {
        public const string ChaveDoAjuste = "loopia_resolucao_nativa_v1";

        /// <summary>O que fazer com a tela. <see cref="mudar"/> = false: deixar como esta.</summary>
        public readonly struct Decisao
        {
            public readonly bool mudar;
            public readonly int largura;
            public readonly int altura;
            public readonly FullScreenMode modo;

            public Decisao(int largura, int altura, FullScreenMode modo)
            {
                mudar = true;
                this.largura = largura;
                this.altura = altura;
                this.modo = modo;
            }

            public static Decisao Manter => default;
        }

        static readonly string[] ArgumentosDeTela =
        {
            "-screen-width", "-screen-height", "-screen-fullscreen", "-window-mode", "-popupwindow",
        };

        public static bool EmTelaCheia(FullScreenMode modo) =>
            modo == FullScreenMode.FullScreenWindow || modo == FullScreenMode.ExclusiveFullScreen;

        /// <summary>A linha de comando escolheu resolucao ou modo de janela?</summary>
        public static bool TemArgumentoDeTela(string[] argumentos)
        {
            if (argumentos == null) return false;
            foreach (string argumento in argumentos)
            {
                if (string.IsNullOrEmpty(argumento)) continue;
                foreach (string conhecido in ArgumentosDeTela)
                    if (string.Equals(argumento, conhecido, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        /// <summary>Regras da classe, sem tocar na tela (testavel).</summary>
        public static Decisao Decidir(int larguraAtual, int alturaAtual, FullScreenMode modoAtual,
            int larguraDoMonitor, int alturaDoMonitor, bool jaAjustou, bool argumentoDeTela)
        {
            if (argumentoDeTela || larguraDoMonitor <= 0 || alturaDoMonitor <= 0) return Decisao.Manter;

            if (EmTelaCheia(modoAtual))
            {
                bool nativa = larguraAtual == larguraDoMonitor && alturaAtual == alturaDoMonitor;
                return nativa ? Decisao.Manter : new Decisao(larguraDoMonitor, alturaDoMonitor, modoAtual);
            }

            if (!jaAjustou) return new Decisao(larguraDoMonitor, alturaDoMonitor, FullScreenMode.FullScreenWindow);
            return Decisao.Manter;
        }

#if !UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void AoAbrirOJogo()
        {
            if (Application.isMobilePlatform) return;

            bool argumento = TemArgumentoDeTela(Environment.GetCommandLineArgs());
            bool jaAjustou = PlayerPrefs.GetInt(ChaveDoAjuste, 0) == 1;
            if (!jaAjustou)
            {
                PlayerPrefs.SetInt(ChaveDoAjuste, 1);
                PlayerPrefs.Save();
            }
            if (argumento) return;

            var vigia = new GameObject("ResolucaoDaTela") { hideFlags = HideFlags.HideInHierarchy };
            DontDestroyOnLoad(vigia);
            vigia.AddComponent<ResolucaoDaTela>().Conferir(jaAjustou);
        }
#endif

        float _proximaConferencia;

        // Estado da tela quando o ultimo pedido foi feito. Enquanto a tela continuar nele, nao pede de novo.
        bool _pediu;
        int _larguraAoPedir, _alturaAoPedir;
        FullScreenMode _modoAoPedir;

        void Update()
        {
            if (Time.unscaledTime < _proximaConferencia) return;
            _proximaConferencia = Time.unscaledTime + 0.5f;
            Conferir(true);
        }

        void Conferir(bool jaAjustou)
        {
            DisplayInfo monitor = Screen.mainWindowDisplayInfo;
            int largura = monitor.width, altura = monitor.height;
            if (largura <= 0 || altura <= 0)
            {
                largura = Display.main.systemWidth;
                altura = Display.main.systemHeight;
            }

            int larguraAtual = Screen.width, alturaAtual = Screen.height;
            FullScreenMode modoAtual = Screen.fullScreenMode;

            // A tela mudou desde o ultimo pedido (atendido, ou o jogador trocou de modo): pode pedir de novo.
            if (_pediu && (larguraAtual != _larguraAoPedir || alturaAtual != _alturaAoPedir || modoAtual != _modoAoPedir))
                _pediu = false;

            Decisao d = Decidir(larguraAtual, alturaAtual, modoAtual, largura, altura, jaAjustou, false);
            // Um pedido por estado: se o driver recusar a resolucao do monitor, nao fica trocando de modo sem parar.
            if (!d.mudar || _pediu) return;

            _pediu = true;
            _larguraAoPedir = larguraAtual;
            _alturaAoPedir = alturaAtual;
            _modoAoPedir = modoAtual;
            Screen.SetResolution(d.largura, d.altura, d.modo);
        }
    }
}

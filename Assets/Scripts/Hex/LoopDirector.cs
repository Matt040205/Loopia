using System;
using UnityEngine;

namespace Loopia.Hex
{
    /// <summary>
    /// Junta as regras do GDD que dependem de fechar a volta:
    ///
    /// - "Quando Lucca completa uma volta inteira no caminho em loop, ele recupera toda a
    ///   sua Vida e recarrega sua Municao";
    /// - a barraca, na mesma passagem, faz "os inimigos aumentam sua PV, ATK e XP em 10%";
    /// - "Apos o jogador completar 4 loops, um Boss e gerado".
    ///
    /// O Boss em si ainda nao existe: aqui a contagem chega no numero e dispara o evento.
    /// </summary>
    [DisallowMultipleComponent]
    public class LoopDirector : MonoBehaviour
    {
        [Header("Referencias")]
        [Tooltip("Deixe vazio para achar automaticamente na cena.")]
        public HexLoopRunner corredor;
        public LuccaStatus status;
        public InimigoSpawner inimigos;

        [Header("Regras do GDD")]
        [Tooltip("Quanto os inimigos ganham de PV, ATK e XP a cada volta fechada. 0.10 = +10%.")]
        [Range(0f, 1f)] public float crescimentoDosInimigos = 0.10f;

        [Tooltip("Voltas necessarias para liberar o Boss.")]
        [Min(1)] public int voltasParaOBoss = 4;

        [Header("Depuracao")]
        [Tooltip("Painel no canto da tela com voltas, PV, flechas e a escala dos inimigos.")]
        public bool mostrarPainelNaTela = true;

        public int VoltasCompletas { get; private set; }

        /// <summary>
        /// Multiplicador acumulado que os inimigos devem aplicar em PV, ATK e XP.
        /// Comeca em 1 e cresce 10% a cada volta.
        /// </summary>
        public float MultiplicadorDosInimigos { get; private set; } = 1f;

        public bool BossLiberado { get; private set; }

        /// <summary>GDD: "Se sua vida chegar a zero, a partida terminou (Game Over)".</summary>
        public bool GameOver { get; private set; }

        public event Action<int> AoCompletarVolta;
        public event Action AoLiberarBoss;
        public event Action AoPerder;

        GUIStyle _estiloDoPainel;
        bool _inscrito;

        void Start()
        {
            if (corredor == null) corredor = FindFirstObjectByType<HexLoopRunner>();
            if (status == null) status = FindFirstObjectByType<LuccaStatus>();
            if (inimigos == null) inimigos = FindFirstObjectByType<InimigoSpawner>();

            if (corredor == null)
            {
                Debug.LogError("[LoopDirector] Nenhum HexLoopRunner encontrado na cena.", this);
                enabled = false;
                return;
            }

            corredor.AoCompletarVolta += FechouUmaVolta;
            if (status != null) status.AoMorrer += LuccaMorreu;
            _inscrito = true;
        }

        void OnDestroy()
        {
            if (!_inscrito) return;

            if (corredor != null) corredor.AoCompletarVolta -= FechouUmaVolta;
            if (status != null) status.AoMorrer -= LuccaMorreu;
            _inscrito = false;
        }

        void LuccaMorreu()
        {
            if (GameOver) return;

            GameOver = true;
            if (corredor != null) corredor.andando = false;

            Debug.Log("[LoopDirector] Game Over: o Lucca caiu na volta " + (VoltasCompletas + 1) + ".", this);
            AoPerder?.Invoke();
        }

        /// <summary>Aplica a escala acumulada num valor base de inimigo (PV, ATK ou XP).</summary>
        public int EscalarValorDeInimigo(int valorBase)
        {
            return Mathf.RoundToInt(valorBase * MultiplicadorDosInimigos);
        }

        void FechouUmaVolta(int volta)
        {
            VoltasCompletas = volta;

            // Beneficio do loop.
            if (status != null) status.RestaurarVidaEMunicao();

            // Preco do loop: os inimigos ficam mais fortes.
            MultiplicadorDosInimigos *= 1f + crescimentoDosInimigos;

            AoCompletarVolta?.Invoke(volta);

            if (!BossLiberado && VoltasCompletas >= voltasParaOBoss)
            {
                BossLiberado = true;
                Debug.Log("[LoopDirector] " + voltasParaOBoss + " voltas completas: o Boss deveria aparecer agora.", this);
                AoLiberarBoss?.Invoke();
            }
        }

        void OnGUI()
        {
            if (!mostrarPainelNaTela) return;

            if (_estiloDoPainel == null)
            {
                _estiloDoPainel = new GUIStyle(GUI.skin.label) { fontSize = 10, richText = false };
            }

            // Canto superior esquerdo: a base da tela e das cartas, como manda a HUD do GDD.
            const float largura = 172f;
            const float altura = 152f;
            GUILayout.BeginArea(new Rect(12f, 12f, largura, altura), GUI.skin.box);

            GUILayout.Label("Voltas: " + VoltasCompletas + " / " + voltasParaOBoss +
                            (BossLiberado ? "  (BOSS!)" : ""), _estiloDoPainel);

            if (corredor != null)
            {
                GUILayout.Label("Passos: " + corredor.PassosNaVoltaAtual +
                                " / " + corredor.TamanhoDoLoop, _estiloDoPainel);
            }

            if (status != null)
            {
                GUILayout.Label("PV: " + status.PvAtual + " / " + status.PvMaximoTotal, _estiloDoPainel);
                GUILayout.Label("Flechas: " + status.FlechasAtuais + " / " + status.flechasMaximo, _estiloDoPainel);
                GUILayout.Label("Nivel " + status.Nivel + "  (XP " + status.Xp +
                                "/" + status.XpParaOProximoNivel + ")", _estiloDoPainel);
            }

            int vivos = inimigos != null ? inimigos.Vivos.Count : 0;
            GUILayout.Label("Inimigos: " + vivos + " vivos, x" +
                            MultiplicadorDosInimigos.ToString("F2"), _estiloDoPainel);

            if (GameOver) GUILayout.Label("--- GAME OVER ---", _estiloDoPainel);

            GUILayout.EndArea();
        }
    }
}

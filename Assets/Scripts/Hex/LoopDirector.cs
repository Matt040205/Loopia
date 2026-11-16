using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Loopia.Hex
{
    /// <summary>
    /// Dono do estado da jornada (Preparando -> Correndo -> Chefe -> Vitoria / Derrota) e das regras do
    /// GDD que dependem de fechar a volta:
    ///
    /// - "Quando Lucca completa uma volta inteira no caminho em loop, ele recupera toda a
    ///   sua Vida e recarrega sua Municao";
    /// - a barraca, na mesma passagem, faz "os inimigos aumentam sua PV, ATK e XP em 10%";
    /// - "Apos o jogador completar 4 loops, um Boss e gerado".
    ///
    /// Na 4a volta, se a cena tiver um EncontroDoChefe, a partida entra no estado Chefe: a contagem de
    /// voltas para (o corredor continua andando), o chefe nasce uma vez e a morte dele e a vitoria.
    /// Sem EncontroDoChefe na cena vale o comportamento antigo (so o evento AoLiberarBoss, e as voltas
    /// seguem contando), para uma cena ainda nao integrada nao ficar presa num confronto sem chefe.
    /// </summary>
    [DisallowMultipleComponent]
    public class LoopDirector : MonoBehaviour
    {
        [Header("Referencias")]
        [Tooltip("Deixe vazio para achar automaticamente na cena.")]
        public HexLoopRunner corredor;
        public LuccaStatus status;
        public InimigoSpawner inimigos;

        [Tooltip("Confronto final. Vazio: procura na cena quando a 4a volta fechar.")]
        public EncontroDoChefe encontro;

        [Header("Regras do GDD")]
        [Tooltip("Quanto os inimigos ganham de PV, ATK e XP a cada volta fechada. 0.10 = +10%.")]
        [Range(0f, 1f)] public float crescimentoDosInimigos = 0.10f;

        [Tooltip("Voltas necessarias para liberar o Boss.")]
        [Min(1)] public int voltasParaOBoss = 4;

        [Header("Confronto final")]
        [Tooltip("Durante o chefe, passar pelo acampamento recarrega as flechas (nao a vida nem as cartas). " +
                 "Evita um impasse sem municao.")]
        public bool recarregarFlechasNoChefe = true;

        [Header("Eventos")]
        [Tooltip("Disparado quando o Lucca morre. Na cena, ligado a TelaDeMorte.Mostrar.")]
        public UnityEvent aoGameOver = new UnityEvent();

        [Tooltip("Disparado uma vez quando o chefe cai. Ligar a TelaDeMorte.MostrarVitoria.")]
        public UnityEvent aoVencer = new UnityEvent();

        [Header("Depuracao")]
        [Tooltip("Painel no canto da tela com voltas, PV, flechas e moedas. Os dados tecnicos (passos, XP, total de " +
                 "moedas, inimigos e escala) so aparecem no Editor e em Development Build; F3 alterna em qualquer build.")]
        public bool mostrarPainelNaTela = true;

        /// <summary>Painel com os dados tecnicos. Comeca ligado no Editor e em Development Build; F3 alterna.</summary>
        public bool MostrandoDepuracao { get; set; }

        readonly CicloDaPartida _ciclo = new CicloDaPartida();

        public EstadoDaPartida Estado => _ciclo.Estado;

        /// <summary>Vitoria ou Derrota: nada mais muda nesta jornada.</summary>
        public bool Terminou => _ciclo.Terminou;

        /// <summary>O Lucca caiu durante o confronto final.</summary>
        public bool DerrotaNoChefe =>
            Estado == EstadoDaPartida.Derrota && _ciclo.EstadoAntesDoFim == EstadoDaPartida.Chefe;

        public int VoltasCompletas { get; private set; }

        /// <summary>
        /// Multiplicador acumulado que os inimigos devem aplicar em PV, ATK e XP.
        /// Comeca em 1 e cresce 10% a cada volta.
        /// </summary>
        public float MultiplicadorDosInimigos { get; private set; } = 1f;

        public bool BossLiberado { get; private set; }

        /// <summary>GDD: "Se sua vida chegar a zero, a partida terminou (Game Over)".</summary>
        public bool GameOver => Estado == EstadoDaPartida.Derrota;

        public bool Venceu => Estado == EstadoDaPartida.Vitoria;

        public event Action<int> AoCompletarVolta;
        public event Action AoLiberarBoss;
        public event Action AoPerder;
        public event Action AoVencer;

        /// <summary>Toda transicao de estado aceita: (anterior, novo).</summary>
        public event Action<EstadoDaPartida, EstadoDaPartida> AoMudarEstado;

        GUIStyle _estiloDoPainel;
        bool _inscrito;
        EncontroDoChefe _encontroInscrito;

        void Awake()
        {
            _ciclo.AoMudar += RepassarMudanca;
            MostrandoDepuracao = Debug.isDebugBuild;
        }

        void Update()
        {
            if (TeclaDeDepuracao()) MostrandoDepuracao = !MostrandoDepuracao;
        }

        static bool TeclaDeDepuracao()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.f3Key.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.F3);
#endif
        }

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
            corredor.AoPassarPeloAcampamento += PassouPeloAcampamento;
            if (status != null) status.AoMorrer += LuccaMorreu;
            _inscrito = true;

            _ciclo.Comecar();
        }

        void OnDestroy()
        {
            _ciclo.AoMudar -= RepassarMudanca;
            if (_encontroInscrito != null) _encontroInscrito.AoChefeDerrotado -= ChefeDerrotado;
            _encontroInscrito = null;

            if (!_inscrito) return;

            if (corredor != null)
            {
                corredor.AoCompletarVolta -= FechouUmaVolta;
                corredor.AoPassarPeloAcampamento -= PassouPeloAcampamento;
            }
            if (status != null) status.AoMorrer -= LuccaMorreu;
            _inscrito = false;
        }

        void RepassarMudanca(EstadoDaPartida anterior, EstadoDaPartida novo)
        {
            Debug.Log("[LoopDirector] Estado: " + anterior + " -> " + novo + ".", this);
            AoMudarEstado?.Invoke(anterior, novo);
        }

        void LuccaMorreu()
        {
            // Depois da vitoria (ou de uma derrota ja registrada) nada muda.
            if (!_ciclo.RegistrarDerrota()) return;

            if (status != null) status.EncerrarRecompensas();
            if (corredor != null) corredor.andando = false;

            // O recorde tambem e checado aqui, para uma morte na primeira volta nao passar batida.
            DadosDoJogador.Instancia.RegistrarVoltas(VoltasCompletas);
            DadosDoJogador.Instancia.Salvar();

            Debug.Log(DerrotaNoChefe
                ? "[LoopDirector] Game Over: o Lucca caiu diante do chefe."
                : "[LoopDirector] Game Over: o Lucca caiu na volta " + (VoltasCompletas + 1) + ".", this);
            AoPerder?.Invoke();
            aoGameOver.Invoke();
        }

        void ChefeDerrotado()
        {
            // Se o Lucca ja tinha caido (flecha em voo matando o chefe depois), a derrota vale.
            if (!_ciclo.RegistrarVitoria()) return;

            // Antes do aoVencer: a tela escreve "Moedas nesta jornada" e nada mais pode entrar depois disso.
            if (status != null) status.EncerrarRecompensas();
            if (corredor != null) corredor.andando = false;
            if (inimigos != null)
            {
                inimigos.pausarSpawnComum = true;
                inimigos.CongelarTodos();
            }

            DadosDoJogador.Instancia.RegistrarVoltas(VoltasCompletas);
            DadosDoJogador.Instancia.RegistrarVitoria();

            Debug.Log("[LoopDirector] Vitoria: o chefe caiu depois de " + VoltasCompletas + " voltas.", this);
            AoVencer?.Invoke();
            aoVencer.Invoke();
        }

        /// <summary>Aplica a escala acumulada num valor base de inimigo (PV, ATK ou XP).</summary>
        public int EscalarValorDeInimigo(int valorBase)
        {
            return Mathf.RoundToInt(valorBase * MultiplicadorDosInimigos);
        }

        void FechouUmaVolta(int volta)
        {
            // So conta enquanto a partida corre: no chefe, na vitoria e na derrota nao existe volta nova.
            if (!_ciclo.ContaVoltas) return;

            VoltasCompletas = volta;

            // Recorde de voltas vai para o JSON, pelo singleton.
            DadosDoJogador.Instancia.RegistrarVoltas(volta);

            // Beneficio do loop.
            if (status != null) status.RestaurarVidaEMunicao();

            // Preco do loop: os inimigos ficam mais fortes.
            MultiplicadorDosInimigos *= 1f + crescimentoDosInimigos;

            AoCompletarVolta?.Invoke(volta);

            if (!BossLiberado && VoltasCompletas >= voltasParaOBoss)
            {
                BossLiberado = true;
                Debug.Log("[LoopDirector] " + voltasParaOBoss + " voltas completas: hora do chefe.", this);
                AoLiberarBoss?.Invoke();
                ComecarConfronto();
            }
        }

        void ComecarConfronto()
        {
            if (encontro == null) encontro = FindFirstObjectByType<EncontroDoChefe>();
            if (encontro == null)
            {
                Debug.LogWarning("[LoopDirector] Sem EncontroDoChefe na cena: o chefe nao nasce e as voltas " +
                                 "continuam contando (comportamento antigo).", this);
                return;
            }

            if (!encontro.PodeIniciar(out string motivo))
            {
                Debug.LogWarning("[LoopDirector] O confronto nao pode comecar (" + motivo + "): as voltas " +
                                 "continuam contando.", this);
                return;
            }

            if (!_ciclo.ComecarChefe()) return;

            // O Lucca segue andando; so a contagem para.
            if (corredor != null) corredor.contarVoltas = false;

            encontro.AoChefeDerrotado += ChefeDerrotado;
            _encontroInscrito = encontro;

            if (!encontro.Iniciar())
            {
                // PodeIniciar acabou de aceitar; isto so acontece se o spawner recusar o chefe.
                Debug.LogWarning("[LoopDirector] O EncontroDoChefe recusou o inicio depois de aceitar.", this);
            }
        }

        void PassouPeloAcampamento()
        {
            if (Estado != EstadoDaPartida.Chefe) return;
            if (recarregarFlechasNoChefe && status != null) status.GanharFlechas(status.FlechasMaximoTotal);
        }

        void OnGUI()
        {
            // IMGUI desenha por cima de qualquer Canvas: no fim da jornada sai da frente da tela de resultado.
            if (!mostrarPainelNaTela || Terminou) return;

            if (_estiloDoPainel == null)
            {
                _estiloDoPainel = new GUIStyle(GUI.skin.label) { fontSize = 10, richText = false };
            }

            // Telas acima de 1080p: o painel escala junto (EscalaDaInterface); ate 1080p fica igual.
            Matrix4x4 anterior = EscalaDaInterface.Aplicar(out _, out _);
            try
            {
                DesenharPainel();
            }
            finally
            {
                GUI.matrix = anterior;
            }
        }

        readonly List<string> _linhas = new List<string>();

        void DesenharPainel()
        {
            var dados = new DadosDoPainel
            {
                voltas = VoltasCompletas, voltasParaOBoss = voltasParaOBoss,
                chefe = Estado == EstadoDaPartida.Chefe, bossLiberado = BossLiberado,
                temCorredor = corredor != null,
                passos = corredor != null ? corredor.PassosNaVoltaAtual : 0,
                passosNaVolta = corredor != null ? corredor.TamanhoDoLoop : 0,
                temStatus = status != null,
                inimigosVivos = inimigos != null ? inimigos.Vivos.Count : 0,
                multiplicador = MultiplicadorDosInimigos,
            };
            if (status != null)
            {
                dados.pv = status.PvAtual; dados.pvMaximo = status.PvMaximoTotal;
                dados.flechas = status.FlechasAtuais; dados.flechasMaximo = status.FlechasMaximoTotal;
                dados.nivel = status.Nivel; dados.xp = status.Xp; dados.xpParaOProximo = status.XpParaOProximoNivel;
                dados.moedas = status.Moedas;
                if (MostrandoDepuracao) dados.moedasTotais = DadosDoJogador.Instancia.MoedasTotais;
            }
            LinhasDoPainel(dados, MostrandoDepuracao, _linhas);

            // Canto superior esquerdo: a base da tela e das cartas, como manda a HUD do GDD. A altura segue as linhas:
            // na apresentacao sao 5 (volta, PV, flechas, nivel, moedas), com depuracao 7.
            const float largura = 172f;
            float altura = 14f + _linhas.Count * 22f;
            GUILayout.BeginArea(new Rect(12f, 12f, largura, altura), GUI.skin.box);
            foreach (string linha in _linhas) GUILayout.Label(linha, _estiloDoPainel);
            GUILayout.EndArea();
        }

        /// <summary>Valores lidos pelo painel (separados para o texto poder ser testado sem cena).</summary>
        public struct DadosDoPainel
        {
            public int voltas, voltasParaOBoss;
            public bool chefe, bossLiberado;
            public bool temCorredor;
            public int passos, passosNaVolta;
            public bool temStatus;
            public int pv, pvMaximo, flechas, flechasMaximo, nivel, xp, xpParaOProximo, moedas, moedasTotais;
            public int inimigosVivos;
            public float multiplicador;
        }

        /// <summary>
        /// Linhas do painel. Sem depuracao so o que o jogador usa (volta, PV, flechas, nivel, moedas da jornada); com
        /// depuracao tambem passos, XP, total de moedas do save, inimigos vivos e a escala por volta.
        /// </summary>
        public static void LinhasDoPainel(DadosDoPainel d, bool depuracao, List<string> linhas)
        {
            linhas.Clear();
            linhas.Add(d.chefe
                ? "Voltas: " + d.voltas + "  (CHEFE!)"
                : "Voltas: " + d.voltas + " / " + d.voltasParaOBoss + (d.bossLiberado ? "  (BOSS!)" : ""));

            if (depuracao && d.temCorredor) linhas.Add("Passos: " + d.passos + " / " + d.passosNaVolta);

            if (d.temStatus)
            {
                linhas.Add("PV: " + d.pv + " / " + d.pvMaximo);
                linhas.Add("Flechas: " + d.flechas + " / " + d.flechasMaximo);
                linhas.Add(depuracao ? "Nivel " + d.nivel + "  (XP " + d.xp + "/" + d.xpParaOProximo + ")" : "Nivel " + d.nivel);
                linhas.Add(depuracao ? "Moedas: " + d.moedas + "  (total " + d.moedasTotais + ")" : "Moedas: " + d.moedas);
            }

            if (depuracao) linhas.Add("Inimigos: " + d.inimigosVivos + " vivos, x" + d.multiplicador.ToString("F2"));
        }
    }
}

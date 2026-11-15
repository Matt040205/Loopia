using System;
using System.Collections.Generic;
using UnityEngine;

namespace Loopia.Hex
{
    /// <summary>
    /// A mao do jogador: "espaco das cartas (...) com 4 espacos disponiveis para vir as cartas
    /// de plataforma". Carta nova chega ao subir de nivel, como pede o GDD em Progressao:
    /// "Ao atingir um novo nivel, cartas de cenario sao disponibilizadas".
    ///
    /// G2: a carta de nivel nunca se perde. Cada uma vira um direito numa fila de pendentes, entregue
    /// quando abrir espaco e houver carta disponivel (docs/gameplay/g2/REWARD_QUEUE_DESIGN.md).
    /// </summary>
    [DisallowMultipleComponent]
    public class MaoDeCartas : MonoBehaviour
    {
        [Header("Referencias")]
        [Tooltip("Deixe vazio para achar automaticamente na cena.")]
        public LuccaStatus status;
        public MapaDePlataformas mapa;

        [Header("Baralho")]
        [Tooltip("Todas as cartas que podem sair. Por enquanto o sorteio e uniforme.")]
        public List<PlataformaDef> baralho = new List<PlataformaDef>();

        [Header("Mao")]
        [Tooltip("GDD: 4 espacos disponiveis na base da tela.")]
        [Min(1)] public int espacos = 4;

        [Tooltip("Quantas cartas o jogador ja comeca segurando.")]
        [Min(0)] public int cartasIniciais = 2;
        [Tooltip("Cartas garantidas no inicio; o restante da mao inicial vem do sorteio.")]
        public List<PlataformaDef> cartasGarantidas = new List<PlataformaDef>();
        [Tooltip("Oferece esta carta uma vez, quando sua condição for liberada e houver espaço na mão.")]
        public PlataformaDef cartaAoLiberar;

        [Tooltip("Quantas cartas chegam a cada nivel novo.")]
        [Min(0)] public int cartasPorNivel = 1;

        [Header("Recompensas pendentes (G2)")]
        [Tooltip("Limite SUAVE da fila: acima dele so avisa no console. Nenhuma recompensa e descartada " +
                 "(docs/gameplay/g2/REWARD_QUEUE_DESIGN.md).")]
        [Min(0)] public int limiteDeAlertaDePendentes = 4;

        [Tooltip("Opcional. Vazio usa o diretor do mapa. Com a jornada encerrada a fila nao entrega cartas.")]
        public LoopDirector diretor;

        PlataformaDef[] _mao;
        bool _inscrito;
        bool _inscritoNoMapa;
        bool _entregouCartaLiberada;

        // Direitos de compra ganhos por nivel e ainda nao entregues, em ordem (guarda o nivel de origem).
        // A carta so e sorteada na entrega, entre as disponiveis naquele momento.
        readonly Queue<int> _pendentes = new Queue<int>();
        bool _entregando;
        bool _avisouLimite;

        /// <summary>A mao, com null nos espacos vazios.</summary>
        public IReadOnlyList<PlataformaDef> Mao => _mao;

        public int EspacosLivres
        {
            get
            {
                int livres = 0;
                for (int i = 0; i < _mao.Length; i++) if (_mao[i] == null) livres++;
                return livres;
            }
        }

        public event Action AoMudar;

        /// <summary>Recompensas de nivel aguardando espaco na mao (ou uma carta disponivel).</summary>
        public int Pendentes => _pendentes.Count;

        /// <summary>A fila passou do limite suave (so sinaliza; nada e descartado).</summary>
        public bool AcimaDoLimite => _pendentes.Count > limiteDeAlertaDePendentes;

        /// <summary>Disparado sempre que a quantidade de pendentes muda.</summary>
        public event Action AoMudarPendentes;

        /// <summary>Uma pendente virou carta na mao: (nivel que gerou o direito, carta entregue). Ordem FIFO.</summary>
        public event Action<int, PlataformaDef> AoEntregarPendente;

        void Awake()
        {
            _mao = new PlataformaDef[Mathf.Max(1, espacos)];
        }

        void Start()
        {
            if (status == null) status = FindFirstObjectByType<LuccaStatus>();
            if (mapa == null) mapa = FindFirstObjectByType<MapaDePlataformas>();

            if (status != null)
            {
                status.AoSubirDeNivel += SubiuDeNivel;
                _inscrito = true;
            }
            if (mapa != null)
            {
                // Colocar/remover plataformas pode liberar cartas (Mola -> Ilha alta): tenta entregar de novo.
                mapa.AoMudar += AoMudarOMapa;
                _inscritoNoMapa = true;
            }

            for (int i = 0; i < cartasIniciais; i++)
            {
                if (i < cartasGarantidas.Count && DarCarta(cartasGarantidas[i])) continue;
                Comprar();
            }
        }

        void OnDestroy()
        {
            if (_inscrito && status != null) status.AoSubirDeNivel -= SubiuDeNivel;
            if (_inscritoNoMapa && mapa != null) mapa.AoMudar -= AoMudarOMapa;
            _inscrito = false;
            _inscritoNoMapa = false;
        }

        void Update()
        {
            if (!_entregouCartaLiberada && cartaAoLiberar != null && DarCarta(cartaAoLiberar))
                _entregouCartaLiberada = true;
        }

        public PlataformaDef CartaNoEspaco(int espaco)
        {
            if (_mao == null || espaco < 0 || espaco >= _mao.Length) return null;
            return _mao[espaco];
        }

        /// <summary>Tira a carta do espaco, depois de ela ter sido colocada no mapa.</summary>
        public bool Gastar(int espaco)
        {
            if (_mao == null || espaco < 0 || espaco >= _mao.Length) return false;
            if (_mao[espaco] == null) return false;

            _mao[espaco] = null;
            // O espaco aberto vai primeiro para a recompensa pendente mais antiga.
            if (!TentarEntregarPendentes()) AoMudar?.Invoke();
            return true;
        }

        /// <summary>Entrega uma carta disponível no primeiro espaço livre.</summary>
        public bool DarCarta(PlataformaDef carta)
        {
            if (carta == null || !carta.DisponivelParaCompra(mapa)) return false;
            int espaco = PrimeiroEspacoLivre();
            if (espaco < 0) return false;

            _mao[espaco] = carta;
            AoMudar?.Invoke();
            return true;
        }

        /// <summary>
        /// Sorteia uma carta do baralho para o primeiro espaco livre. Com a mao cheia devolve false
        /// e nada muda; quem nao pode perder a carta (o nivel) passa pela fila de pendentes.
        /// </summary>
        public bool Comprar() => Comprar(out _);

        bool Comprar(out PlataformaDef comprada)
        {
            comprada = null;
            if (baralho == null || baralho.Count == 0) return false;

            int espaco = PrimeiroEspacoLivre();
            if (espaco < 0) return false;

            var disponiveis = baralho.FindAll(carta => carta != null && carta.DisponivelParaCompra(mapa));
            if (disponiveis.Count == 0) return false;

            comprada = disponiveis[UnityEngine.Random.Range(0, disponiveis.Count)];
            _mao[espaco] = comprada;
            AoMudar?.Invoke();
            return true;
        }

        int PrimeiroEspacoLivre()
        {
            for (int i = 0; i < _mao.Length; i++) if (_mao[i] == null) return i;
            return -1;
        }

        /// <summary>Jornada encerrada ou Lucca morto: a mao fica como esta.</summary>
        bool EntregaBloqueada
        {
            get
            {
                if (status != null && !status.EstaVivo) return true;
                LoopDirector d = diretor != null ? diretor : (mapa != null ? mapa.diretor : null);
                return d != null && d.Terminou;
            }
        }

        /// <summary>
        /// Cada carta de nivel vira um direito na fila; com espaco e carta disponivel ele e entregue na hora.
        /// Antes de G2 a carta de um nivel com a mao cheia simplesmente se perdia.
        /// </summary>
        void SubiuDeNivel(int nivel)
        {
            if (cartasPorNivel <= 0) return;
            for (int i = 0; i < cartasPorNivel; i++) _pendentes.Enqueue(nivel);
            AoMudarPendentes?.Invoke();
            AvisarSeAcimaDoLimite();
            TentarEntregarPendentes();
        }

        void AoMudarOMapa() => TentarEntregarPendentes();

        /// <summary>
        /// Entrega pendentes, da mais antiga para a mais nova, enquanto houver espaco e carta disponivel.
        /// Sem carta disponivel o direito continua na fila (nunca e consumido sem virar carta).
        /// Devolve true se entregou ao menos uma (e, portanto, ja disparou AoMudar).
        /// </summary>
        public bool TentarEntregarPendentes()
        {
            if (_entregando || _mao == null || _pendentes.Count == 0 || EntregaBloqueada) return false;

            _entregando = true;
            bool entregou = false;
            try
            {
                while (_pendentes.Count > 0)
                {
                    if (!Comprar(out PlataformaDef carta)) break;
                    int nivel = _pendentes.Dequeue();
                    entregou = true;
                    AoMudarPendentes?.Invoke();
                    AoEntregarPendente?.Invoke(nivel, carta);
                }
            }
            finally
            {
                _entregando = false;
            }

            if (!AcimaDoLimite) _avisouLimite = false;
            return entregou;
        }

        void AvisarSeAcimaDoLimite()
        {
            if (!AcimaDoLimite || _avisouLimite) return;
            _avisouLimite = true;
            Debug.LogWarning("[MaoDeCartas] " + _pendentes.Count + " recompensas pendentes (limite de alerta " +
                             limiteDeAlertaDePendentes + "). Nenhuma foi descartada; a mao esta travada.", this);
        }
    }
}

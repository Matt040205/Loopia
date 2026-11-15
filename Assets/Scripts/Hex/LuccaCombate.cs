using UnityEngine;

namespace Loopia.Hex
{
    /// <summary>
    /// O combate automatico do GDD: "Inimigo a Frente (Com Municao): Ataca automaticamente com
    /// projeteis, ele continua andando e atirando seus projeteis, se nao tiver, ele os ignora e
    /// continua andando."
    ///
    /// Ou seja: o Lucca nunca para para lutar, e ficar sem flecha nao trava nada — ele so deixa
    /// de atirar. Cada tiro gasta uma flecha.
    /// </summary>
    [DisallowMultipleComponent]
    public class LuccaCombate : MonoBehaviour
    {
        [Header("Referencias")]
        [Tooltip("Deixe vazio para achar automaticamente na cena.")]
        public HexPlayer player;
        public LuccaStatus status;
        public InimigoSpawner inimigos;

        [Tooltip("De onde vem o bonus de dano das torres e areas de treino. Opcional.")]
        public MapaDePlataformas plataformas;

        [Header("Tiro")]
        [Tooltip("Alcance do tiro, em hexagonos.")]
        [Min(1)] public int alcance = 3;

        [Tooltip("Segundos entre um tiro e outro. O GDD deixou o atk SPD do Lucca em aberto ('? atk SPD').")]
        [Min(0.05f)] public float intervaloDeTiro = 0.8f;

        [Min(1f)] public float velocidadeDoProjetil = 14f;

        [Tooltip("Altura de onde a flecha sai, medida do pe do Lucca.")]
        public float alturaDoTiro = 0.6f;

        public Color corDaFlecha = new Color(1f, 0.85f, 0.35f);
        [Tooltip("Visual opcional do projetil, com ponta no eixo +Z.")]
        public GameObject visualDaFlecha;

        [Header("Mira")]
        [Tooltip("Ligado: so atira em quem esta a frente, como diz o GDD. " +
                 "Desligado: atira no mais proximo, esteja onde estiver.")]
        public bool somenteAFrente = true;

        [Tooltip("Opcional. Com a jornada encerrada (vitoria/derrota) o Lucca para de atirar.")]
        public LoopDirector diretor;

        float _proximoTiro;
        LuccaAnimationBridge _animacao;
        Inimigo _alvoDoTiro;
        int _sequenciaDoTiro;
        int _tiroPendente;
        float _prazoDoTiro;

        /// <summary>
        /// Quem o Lucca esta mirando agora (dentro do alcance, com ou sem flecha), ou null. Atualizado todo frame.
        /// Gancho de leitura para a frente de personagem orientar arco/tronco (ex.: o chefe no centro do anel)
        /// sem mexer no movimento automatico. Ver docs/gameplay/APRESENTACAO_DO_CHEFE.md.
        /// </summary>
        public Inimigo AlvoAtual { get; private set; }

        /// <summary>Disparado apos criar a flecha e gastar municao: (alvo, ponto de saida).</summary>
        public event System.Action<Inimigo, Vector3> AoDisparar;

        public bool PodeAgir => status != null && status.EstaVivo && (diretor == null || !diretor.Terminou);
        public Inimigo AlvoDaPreparacao => _alvoDoTiro;
        internal bool CadenciaPermiteSoltar => Time.time >= _proximoTiro;

        void Start()
        {
            if (player == null) player = GetComponent<HexPlayer>();
            if (player == null) player = FindFirstObjectByType<HexPlayer>();
            if (status == null) status = GetComponent<LuccaStatus>();
            if (status == null) status = FindFirstObjectByType<LuccaStatus>();
            if (inimigos == null) inimigos = FindFirstObjectByType<InimigoSpawner>();
            if (plataformas == null) plataformas = FindFirstObjectByType<MapaDePlataformas>();
            if (diretor == null) diretor = FindFirstObjectByType<LoopDirector>();
            _animacao = GetComponentInChildren<LuccaAnimationBridge>(true);

            if (player == null || status == null || inimigos == null)
            {
                Debug.LogError("[LuccaCombate] Faltou referencia: precisa de HexPlayer, LuccaStatus e InimigoSpawner.", this);
                enabled = false;
            }
        }

        void Update()
        {
            if (!PodeAgir)
            {
                AlvoAtual = null;
                CancelarTiro(_tiroPendente);
                return;
            }

            AlvoAtual = EscolherAlvo();
            if (_tiroPendente != 0)
            {
                if (!PodeDispararEm(_alvoDoTiro) || status.FlechasAtuais == 0 || Time.time > _prazoDoTiro)
                    CancelarTiro(_tiroPendente);
                return;
            }

            if (AlvoAtual == null || status.FlechasAtuais == 0) return;

            if (_animacao != null && _animacao.AnimaTiros)
            {
                if (!_animacao.PodePrepararTiro || Time.time < _proximoTiro - _animacao.TempoDeSoltura) return;
                _tiroPendente = ++_sequenciaDoTiro;
                _alvoDoTiro = AlvoAtual;
                _prazoDoTiro = Time.time + _animacao.DuracaoDoTiro + 0.12f;
                _animacao.PrepararTiro(_tiroPendente);
            }
            else if (Time.time >= _proximoTiro)
            {
                Disparar(AlvoAtual, transform.position + Vector3.up * alturaDoTiro);
            }
        }

        // Chamado depois de avaliar a pose e a mira, no frame da soltura.
        internal bool SoltarTiro(int id, Transform origem)
        {
            if (!isActiveAndEnabled || id == 0 || id != _tiroPendente || !CadenciaPermiteSoltar) return false;
            Inimigo alvo = _alvoDoTiro;
            _tiroPendente = 0;
            _alvoDoTiro = null;
            return Disparar(alvo, origem != null ? origem.position : transform.position + Vector3.up * alturaDoTiro);
        }

        internal void CancelarTiro(int id)
        {
            if (id == 0 || id != _tiroPendente) return;
            _tiroPendente = 0;
            _alvoDoTiro = null;
            if (_animacao != null) _animacao.CancelarTiro(id);
        }

        void OnDisable() => CancelarTiro(_tiroPendente);

        bool Disparar(Inimigo alvo, Vector3 origem)
        {
            if (!PodeAgir || !PodeDispararEm(alvo) || status.FlechasAtuais == 0) return false;
            if (Projetil.Disparar(origem, alvo, DanoDoTiro(), velocidadeDoProjetil, corDaFlecha, visualDaFlecha) == null)
                return false;
            _proximoTiro = Time.time + intervaloDeTiro;
            status.GastarFlecha();
            AoDisparar?.Invoke(alvo, origem);
            return true;
        }

        bool PodeDispararEm(Inimigo inimigo)
        {
            return inimigo != null && inimigo.EstaVivo && EstaAFrente(inimigo)
                && RegrasDoChefe.DentroDoAlcance(HexCoord.Distance(player.Coord, inimigo.Casa),
                    alcance, inimigo.AlcanceExtraComoAlvo);
        }

        /// <summary>ATK do Lucca mais o que torres e areas de treino somam.</summary>
        public int DanoDoTiro()
        {
            float bonus = plataformas != null ? plataformas.BonusDeDanoTotal : 0f;
            return Mathf.Max(1, Mathf.RoundToInt(status.ataque * (1f + bonus)));
        }

        /// <summary>Inimigo vivo mais proximo dentro do alcance, preferindo quem esta a frente.</summary>
        Inimigo EscolherAlvo()
        {
            Inimigo melhor = null;
            int menorDistancia = int.MaxValue;

            var vivos = inimigos.Vivos;
            for (int i = 0; i < vivos.Count; i++)
            {
                Inimigo inimigo = vivos[i];
                if (inimigo == null || !inimigo.EstaVivo) continue;

                // A prioridade e pela distancia real; o alcance extra so vale para alvos grandes (o chefe).
                int distancia = HexCoord.Distance(player.Coord, inimigo.Casa);
                if (!RegrasDoChefe.DentroDoAlcance(distancia, alcance, inimigo.AlcanceExtraComoAlvo)) continue;
                if (distancia >= menorDistancia) continue;
                if (!EstaAFrente(inimigo)) continue;

                menorDistancia = distancia;
                melhor = inimigo;
            }

            return melhor;
        }

        bool EstaAFrente(Inimigo inimigo)
        {
            if (!somenteAFrente) return true;

            // Alvo grande (chefe no centro do anel): fica de lado o tempo todo enquanto o Lucca contorna.
            if (inimigo.AlvoDeQualquerAngulo) return true;

            // Quem esta na mesma casa conta sempre: nao da para ignorar quem esta em cima de voce.
            if (inimigo.Casa == player.Coord) return true;

            Vector3 paraOInimigo = inimigo.transform.position - transform.position;
            paraOInimigo.y = 0f;
            if (paraOInimigo.sqrMagnitude < 0.0001f) return true;

            return Vector3.Dot(transform.forward, paraOInimigo.normalized) > 0f;
        }
    }
}

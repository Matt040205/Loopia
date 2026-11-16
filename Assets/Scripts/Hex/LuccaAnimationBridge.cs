using UnityEngine;

namespace Loopia.Hex
{
    /// <summary>Le o gameplay e apresenta a pose sem escrever na raiz de navegacao.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(Animator))]
    public sealed class LuccaAnimationBridge : MonoBehaviour
    {
        const int Base = 0;
        const int Tronco = 1;
        static readonly int Locomotion = Animator.StringToHash("Base Layer.Locomotion");
        static readonly int Jump = Animator.StringToHash("Base Layer.Jump");
        static readonly int SpringJump = Animator.StringToHash("Base Layer.Spring Jump");
        static readonly int Ready = Animator.StringToHash("Upper Body.Bow Ready");
        static readonly int Shot = Animator.StringToHash("Upper Body.Bow Shot");
        static readonly int MoveSpeed = Animator.StringToHash("MoveSpeed");
        static readonly int RitmoPulo = Animator.StringToHash("RitmoPulo");

        [SerializeField] AnimationClip bowShot;
        [SerializeField] Transform peito;
        [SerializeField] Transform origem;
        [SerializeField] Transform direcao;
        [SerializeField, Range(0f, 0.15f)] float amortecimentoVelocidade = 0.045f;
        [SerializeField, Range(0f, 0.15f)] float misturaAterrissagem = 0.065f;
        [SerializeField, Range(0, 70)] float giroMaximo = 65f;
        [SerializeField, Range(0, 35)] float inclinacaoMaxima = 25f;

        [Header("Tiro alem do tronco (Mirante e chefe atiram em qualquer direcao)")]
        [Tooltip("O que passa da folga do tronco vira o CORPO visual inteiro (este objeto), ate este angulo, so durante o " +
                 "tiro. A raiz de navegacao (NavMeshAgent) nao gira; o tronco continua limitado a giroMaximo.")]
        [SerializeField, Range(0, 150)] float giroMaximoDoCorpo = 120f;
        [Tooltip("Limite do giro do corpo com o Lucca correndo na velocidade cheia: as pernas seguem correndo para a " +
                 "frente, entao o pe de apoio desliza ~2 x velocidade x sen(giro/2). Parado vale giroMaximoDoCorpo; entre " +
                 "os dois, proporcional a velocidade da locomocao.")]
        [SerializeField, Range(0, 150)] float giroDoCorpoCorrendo = 30f;
        [Tooltip("Fracao do giroMaximo do tronco usada antes de o corpo comecar a virar (sobra para a mira fina).")]
        [SerializeField, Range(0.3f, 1f)] float folgaDoTronco = 0.8f;

        [Header("Corpo para onde anda (a raiz do agente leva ~0,3 s para encarar cada nova direcao)")]
        [Tooltip("Antes de cada pulo e depois de cada pouso o caminho muda de direcao e a raiz do NavMeshAgent ainda olha " +
                 "para a direcao antiga (ate ~60 graus). O corpo visual vira para onde a raiz anda nesse meio tempo; sem isso a " +
                 "corrida empurra os pes para a frente do corpo enquanto ele anda de lado e o pe de apoio desliza. " +
                 "A raiz e o combate (EstaAFrente) nao mudam. 0 = desligado.")]
        [SerializeField, Range(0, 90)] float giroDaMarchaMaximo = 75f;
        [Tooltip("Suavizacao (s) do giro da marcha. Precisa ser bem mais rapida que a raiz.")]
        [SerializeField, Range(0.01f, 0.2f)] float suavizacaoDaMarcha = 0.035f;

        [Header("Fim da jornada (sem clipe proprio: reaproveita os saltos, a raiz fica com o NavMeshAgent)")]
        [Tooltip("Vitoria: o Spring Jump no lugar para neste ponto (0..1), com o braco erguido.")]
        [SerializeField, Range(0f, 1f)] float poseDaVitoria = 0.78f;
        [Tooltip("Derrota: segundos do Jump onde o Lucca fica agachado (0 = o agachamento da preparacao).")]
        [SerializeField, Range(0f, 0.35f)] float poseDaDerrota = 0f;
        [SerializeField, Range(0f, 0.5f)] float misturaDoFim = 0.2f;

        Animator _animator;
        HexPlayer _player;
        LuccaCombate _combate;
        LuccaStatus _status;
        int _estadoDoFim;
        float _congelarEm = -1f;
        Quaternion _rotacaoBase = Quaternion.identity;
        float _giroDoCorpo;
        float _velocidadeDoGiroDoCorpo;
        float _giroDaMarcha;
        float _velocidadeDoGiroDaMarcha;
        Vector3 _posicaoAnterior;
        Vector3 _velocidadeDaRaiz;

        /// <summary>Angulo horizontal (graus) entre o arco e o alvo no instante da ultima soltura. NaN antes do 1o tiro.</summary>
        public float ErroDeMiraNaSoltura { get; private set; } = float.NaN;

        /// <summary>Giro atual do corpo visual em relacao a raiz para a mira (graus).</summary>
        public float GiroDoCorpoAtual => _giroDoCorpo;

        /// <summary>Giro atual do corpo visual em relacao a raiz para olhar para onde anda (graus).</summary>
        public float GiroDaMarchaAtual => _giroDaMarcha;
        int _idDoTiro;
        bool _esperandoEstadoDeTiro;
        bool _recebeuSoltura;
        bool _tocandoPulo;
        bool _encerrado;
        float _ultimoInicio;
        float _velocidadeAntesDoPulo;
        float _fimDaJanelaDeRetomada;
        Vector2 _mira;
        Vector2 _velocidadeDaMira;
        float _soltura = 19f / 60f;

        public float TempoDeSoltura => _soltura;
        public float DuracaoDoTiro => bowShot != null ? bowShot.length : 0.65f;
        public bool AnimaTiros => isActiveAndEnabled && _animator != null && _animator.isActiveAndEnabled
            && _animator.isInitialized && _animator.runtimeAnimatorController != null
            && _animator.layerCount > Tronco && bowShot != null && _animator.HasState(Tronco, Shot);
        public bool PodePrepararTiro => AnimaTiros && _idDoTiro == 0
            && _animator.GetCurrentAnimatorStateInfo(Tronco).fullPathHash != Shot
            && (!_animator.IsInTransition(Tronco) || _animator.GetNextAnimatorStateInfo(Tronco).fullPathHash != Shot);

        void Awake()
        {
            _animator = GetComponent<Animator>();
            _player = GetComponentInParent<HexPlayer>();
            _combate = GetComponentInParent<LuccaCombate>();
            _status = GetComponentInParent<LuccaStatus>();
            _rotacaoBase = transform.localRotation;
            if (_player != null) _posicaoAnterior = _player.transform.position;
            _animator.applyRootMotion = false;
            if (bowShot == null) return;
            foreach (AnimationEvent evento in bowShot.events)
            {
                if (evento.functionName != nameof(OnBowRelease)) continue;
                _soltura = evento.time;
                break;
            }
        }

        void OnEnable()
        {
            if (_player != null) _player.AoIniciarPulo += IniciarPulo;
        }

        void OnDisable()
        {
            if (_player != null) _player.AoIniciarPulo -= IniciarPulo;
            if (_combate != null) _combate.CancelarTiro(_idDoTiro);
            _idDoTiro = 0;
        }

        void Update()
        {
            if (_player == null || _combate == null || !AnimaTiros) return;
            if (!_combate.PodeAgir)
            {
                if (!_encerrado)
                {
                    _encerrado = true;
                    _animator.SetFloat(MoveSpeed, 0f);
                    _animator.SetLayerWeight(Tronco, 0f);
                    ComecarPoseDoFim(_status != null && _status.EstaVivo);
                }
                AtualizarPoseDoFim();
                return;
            }

            if (_player.EstaPulando) return;

            var agente = _player.Agente;
            bool noChao = agente != null && agente.isActiveAndEnabled && agente.isOnNavMesh;
            float velocidade = noChao ? VelocidadeHorizontal(agente.velocity) : 0f;
            bool retoma = noChao && _player.EstaAndando && !_player.BloqueadoPorAltura;
            if (_tocandoPulo)
            {
                _tocandoPulo = false;
                _fimDaJanelaDeRetomada = Time.time + 0.075f;
                if (retoma && velocidade < 0.1f)
                    velocidade = Mathf.Min(_velocidadeAntesDoPulo, agente.speed);
                _animator.SetFloat(MoveSpeed, velocidade);
                _animator.CrossFadeInFixedTime(Locomotion, misturaAterrissagem, Base, 0f);
                return;
            }

            // O agente pode devolver zero no primeiro frame apos fechar o link.
            if (retoma && Time.time < _fimDaJanelaDeRetomada && velocidade < 0.1f) return;
            _animator.SetFloat(MoveSpeed, velocidade < 0.06f ? 0f : velocidade, amortecimentoVelocidade, Time.deltaTime);
        }

        /// <summary>Pose do fim da jornada: comemoracao na vitoria, agachado na derrota (antes: parava em Idle).</summary>
        public bool FimComVitoria { get; private set; }

        /// <summary>A pose do fim ja parou (RitmoPulo 0).</summary>
        public bool PoseDoFimParada => _encerrado && _congelarEm < 0f;

        void ComecarPoseDoFim(bool vitoria)
        {
            FimComVitoria = vitoria;
            if (vitoria)
            {
                // Spring Jump no lugar ate o braco erguido; para ali (o estado nao tem transicao de volta).
                _estadoDoFim = SpringJump;
                _congelarEm = poseDaVitoria;
                _animator.SetFloat(RitmoPulo, 1f);
                _animator.CrossFadeInFixedTime(SpringJump, misturaDoFim, Base, 0f);
            }
            else
            {
                // Agachado no comeco do Jump, parado desde o inicio: postura de derrota.
                _estadoDoFim = Jump;
                _congelarEm = -1f;
                _animator.SetFloat(RitmoPulo, 0f);
                _animator.CrossFadeInFixedTime(Jump, misturaDoFim, Base, poseDaDerrota);
            }
        }

        void AtualizarPoseDoFim()
        {
            if (_congelarEm < 0f) return;
            AnimatorStateInfo estado = _animator.IsInTransition(Base)
                ? _animator.GetNextAnimatorStateInfo(Base)
                : _animator.GetCurrentAnimatorStateInfo(Base);
            if (estado.fullPathHash != _estadoDoFim || estado.normalizedTime < _congelarEm) return;
            _animator.SetFloat(RitmoPulo, 0f);
            _congelarEm = -1f;
        }

        void IniciarPulo(bool usaMola, float duracao)
        {
            if (_combate == null || !_combate.PodeAgir || !AnimaTiros) return;
            _tocandoPulo = true;
            _velocidadeAntesDoPulo = _animator.GetFloat(MoveSpeed);
            _animator.SetFloat(RitmoPulo, (usaMola ? 0.70f : 0.35f) / duracao);
            _animator.CrossFadeInFixedTime(usaMola ? SpringJump : Jump, 0.025f, Base, 0f);
        }

        static float VelocidadeHorizontal(Vector3 velocidade) =>
            new Vector2(velocidade.x, velocidade.z).magnitude;

        internal void PrepararTiro(int id)
        {
            _idDoTiro = id;
            _ultimoInicio = Time.time;
            _esperandoEstadoDeTiro = true;
            _recebeuSoltura = false;
            _animator.SetLayerWeight(Tronco, 1f);
            _animator.CrossFadeInFixedTime(Shot, 0.035f, Tronco, 0f);
        }

        internal void CancelarTiro(int id)
        {
            if (_idDoTiro != id) return;
            _idDoTiro = 0;
            _recebeuSoltura = false;
            if (AnimaTiros) _animator.CrossFadeInFixedTime(Ready, 0.085f, Tronco, 0f);
        }

        public void OnBowRelease(AnimationEvent evento)
        {
            if (_idDoTiro == 0 || !evento.isFiredByAnimator
                || evento.animatorStateInfo.fullPathHash != Shot || evento.animatorClipInfo.clip != bowShot
                || evento.animatorStateInfo.normalizedTime + 0.025f < _soltura / DuracaoDoTiro) return;
            _recebeuSoltura = true;
        }

        bool EmEstadoDeTiro(out float progresso)
        {
            progresso = 0f;
            if (!AnimaTiros) return false;
            var estado = _animator.IsInTransition(Tronco) ? _animator.GetNextAnimatorStateInfo(Tronco)
                : _animator.GetCurrentAnimatorStateInfo(Tronco);
            if (estado.fullPathHash != Shot) return false;
            progresso = estado.normalizedTime;
            return true;
        }

        void LateUpdate()
        {
            if (_player == null || _combate == null) return;
            bool atirando = EmEstadoDeTiro(out float progresso);
            if (AnimaTiros)
            {
                // Bow Ready fica disponivel, mas Idle/Walk/Jump ja carregam o arco na base.
                float peso = atirando ? 1f : _animator.GetLayerWeight(Tronco);
                _animator.SetLayerWeight(Tronco, _combate.PodeAgir
                    ? Mathf.MoveTowards(peso, atirando ? 1f : 0f, Time.deltaTime * 14f) : 0f);
                AtualizarMira(atirando ? progresso : 0f);
            }

            if (_idDoTiro == 0) return;
            if (!atirando)
            {
                if (!_esperandoEstadoDeTiro || !AnimaTiros || Time.time - _ultimoInicio > 0.10f)
                    _combate.CancelarTiro(_idDoTiro);
                return;
            }
            _esperandoEstadoDeTiro = false;
            if (progresso * DuracaoDoTiro + 0.0001f < _soltura && !_recebeuSoltura) return;
            // Se o evento nao veio, a passagem pelo mesmo ponto do clipe confirma a soltura.
            if (!_combate.CadenciaPermiteSoltar) return;
            int id = _idDoTiro;
            _idDoTiro = 0;
            Inimigo alvoDoTiro = _combate.AlvoDaPreparacao;
            if (alvoDoTiro != null && origem != null && direcao != null)
                ErroDeMiraNaSoltura = ErroHorizontal(direcao.position - origem.position,
                    alvoDoTiro.transform.position - origem.position, _player.transform.up);
            _combate.SoltarTiro(id, origem);
        }

        void AtualizarMira(float progresso)
        {
            if (peito == null || origem == null || direcao == null) return;
            Inimigo alvo = _combate.AlvoDaPreparacao != null ? _combate.AlvoDaPreparacao : _combate.AlvoAtual;
            bool mirar = _combate.PodeAgir && alvo != null && (!_player.EstaPulando || progresso > 0f);
            Transform raiz = _player.transform;
            float pesoDoTiro = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.14f, 0.42f, progresso))
                * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.56f, 0.93f, progresso)));

            // Marcha: o corpo olha para onde a raiz de fato anda enquanto ela ainda gira. Mede o deslocamento da raiz (e nao
            // a velocidade do agente) para valer tambem no pulo, que move a raiz a mao; teleporte (ColocarEm) nao conta.
            Vector3 deslocamento = raiz.position - _posicaoAnterior;
            _posicaoAnterior = raiz.position;
            if (Time.deltaTime > 0f) _velocidadeDaRaiz = deslocamento / Time.deltaTime;
            if (_velocidadeDaRaiz.sqrMagnitude > 144f) _velocidadeDaRaiz = Vector3.zero;
            float alvoDaMarcha = GiroDaMarcha(raiz.forward, _velocidadeDaRaiz, raiz.up, giroDaMarchaMaximo);
            _giroDaMarcha = Mathf.SmoothDampAngle(_giroDaMarcha, alvoDaMarcha, ref _velocidadeDoGiroDaMarcha, suavizacaoDaMarcha);

            // Corpo primeiro (o tronco mede a referencia do arco ja com o corpo virado): so durante o tiro e fora do pulo.
            float alvoDoCorpo = 0f;
            if (mirar && pesoDoTiro > 0f && !_player.EstaPulando)
            {
                // Direcao do arco na pose da animacao, descontado o giro de mira do frame anterior (o transform guarda; a
                // marcha fica: ela e a base, para onde as pernas correm).
                float arcoSemCorpo = Angulos(raiz.InverseTransformDirection(direcao.position - origem.position)).x - _giroDoCorpo;
                float paraOAlvo = Angulos(raiz.InverseTransformDirection(alvo.transform.position - origem.position)).x;
                float rapidez = _animator.GetFloat(MoveSpeed) / Mathf.Max(0.1f, _player.velocidade);
                alvoDoCorpo = GiroDoCorpo(Mathf.DeltaAngle(arcoSemCorpo, paraOAlvo), giroMaximo * folgaDoTronco,
                    LimiteDoCorpo(giroMaximoDoCorpo, giroDoCorpoCorrendo, rapidez));
            }
            _giroDoCorpo = Mathf.SmoothDampAngle(_giroDoCorpo, alvoDoCorpo, ref _velocidadeDoGiroDoCorpo,
                alvoDoCorpo != 0f ? 0.07f : 0.16f);
            transform.localRotation = Quaternion.AngleAxis(_giroDaMarcha + _giroDoCorpo, Vector3.up) * _rotacaoBase;

            Vector2 ajuste = Vector2.zero;
            if (mirar)
            {
                Vector2 referencia = Angulos(raiz.InverseTransformDirection(direcao.position - origem.position));
                Vector2 desejado = Angulos(raiz.InverseTransformDirection(alvo.transform.position - origem.position));
                ajuste.x = Mathf.Clamp(Mathf.LerpAngle(desejado.x * 0.22f,
                    Mathf.DeltaAngle(referencia.x, desejado.x), pesoDoTiro), -giroMaximo, giroMaximo);
                ajuste.y = Mathf.Clamp(Mathf.Lerp(desejado.y * 0.18f, desejado.y - referencia.y, pesoDoTiro),
                    -inclinacaoMaxima, inclinacaoMaxima);
                if (_player.EstaPulando) ajuste *= 0.8f * pesoDoTiro;
            }

            _mira = Vector2.SmoothDamp(_mira, ajuste, ref _velocidadeDaMira, mirar ? 0.085f : 0.17f);
            Vector3 eixoVertical = _player.transform.up;
            Quaternion giro = Quaternion.AngleAxis(_mira.x, eixoVertical);
            Vector3 seta = giro * (direcao.position - origem.position);
            Vector3 eixoLateral = Vector3.Cross(eixoVertical, seta.normalized);
            peito.rotation = Quaternion.AngleAxis(-_mira.y, eixoLateral.normalized) * giro * peito.rotation;
        }

        /// <summary>
        /// Parte do giro horizontal desejado (graus, relativo a raiz) que vai para o corpo inteiro: o que passa da folga do
        /// tronco, com o mesmo sinal, limitado a maximoDoCorpo. Dentro da folga, zero (so o tronco mira).
        /// </summary>
        public static float GiroDoCorpo(float desejado, float folgaDoTronco, float maximoDoCorpo)
        {
            float excesso = Mathf.Abs(Mathf.DeltaAngle(0f, desejado)) - folgaDoTronco;
            return excesso <= 0f ? 0f : Mathf.Sign(Mathf.DeltaAngle(0f, desejado)) * Mathf.Min(excesso, maximoDoCorpo);
        }

        /// <summary>
        /// Limite do giro do corpo para a rapidez da locomocao (0 parado, 1 na velocidade cheia): de parado ate correndo,
        /// sem nunca passar do limite parado.
        /// </summary>
        public static float LimiteDoCorpo(float parado, float correndo, float rapidez) =>
            Mathf.Lerp(parado, Mathf.Min(correndo, parado), Mathf.Clamp01(rapidez));

        /// <summary>
        /// Quanto (graus, com sinal, em torno de cima) o corpo precisa girar a partir da frente da raiz para olhar na direcao
        /// da velocidade. Zero abaixo de velocidadeMinima (parado ou arrancando, a direcao nao e confiavel); limitado a maximo.
        /// </summary>
        public static float GiroDaMarcha(Vector3 frente, Vector3 velocidade, Vector3 cima, float maximo, float velocidadeMinima = 0.5f)
        {
            Vector3 plano = Vector3.ProjectOnPlane(velocidade, cima);
            if (plano.magnitude < velocidadeMinima) return 0f;
            float angulo = Vector3.SignedAngle(Vector3.ProjectOnPlane(frente, cima), plano, cima);
            return Mathf.Clamp(angulo, -maximo, maximo);
        }

        /// <summary>Angulo (graus) entre dois vetores projetados no plano horizontal.</summary>
        public static float ErroHorizontal(Vector3 arco, Vector3 alvo, Vector3 cima) =>
            Vector3.Angle(Vector3.ProjectOnPlane(arco, cima), Vector3.ProjectOnPlane(alvo, cima));

        static Vector2 Angulos(Vector3 vetor)
        {
            return new Vector2(Mathf.Atan2(vetor.x, vetor.z) * Mathf.Rad2Deg,
                Mathf.Atan2(vetor.y, new Vector2(vetor.x, vetor.z).magnitude) * Mathf.Rad2Deg);
        }
    }
}

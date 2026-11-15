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

        Animator _animator;
        HexPlayer _player;
        LuccaCombate _combate;
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
                    _animator.CrossFadeInFixedTime(Locomotion, 0.1f, Base, 0f);
                }
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
            _combate.SoltarTiro(id, origem);
        }

        void AtualizarMira(float progresso)
        {
            if (peito == null || origem == null || direcao == null) return;
            Inimigo alvo = _combate.AlvoDaPreparacao != null ? _combate.AlvoDaPreparacao : _combate.AlvoAtual;
            bool mirar = _combate.PodeAgir && alvo != null && (!_player.EstaPulando || progresso > 0f);
            Vector2 ajuste = Vector2.zero;
            if (mirar)
            {
                Transform raiz = _player.transform;
                float pesoDoTiro = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.14f, 0.42f, progresso))
                    * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.56f, 0.93f, progresso)));
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

        static Vector2 Angulos(Vector3 vetor)
        {
            return new Vector2(Mathf.Atan2(vetor.x, vetor.z) * Mathf.Rad2Deg,
                Mathf.Atan2(vetor.y, new Vector2(vetor.x, vetor.z).magnitude) * Mathf.Rad2Deg);
        }
    }
}

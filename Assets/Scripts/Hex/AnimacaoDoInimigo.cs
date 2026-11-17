using UnityEngine;
using UnityEngine.AI;

namespace Loopia.Hex
{
    /// <summary>
    /// Visual animado de um inimigo (Sprint 5B). Fica na raiz do prefab visual, filho do GameObject de gameplay.
    ///
    /// So LE o estado do <see cref="Inimigo"/> e os eventos que ja existem (aoComecarRasante, aoAtacar,
    /// aoSerDerrotado, AoSofrerDano; no chefe, AoAvisarEspecial/AoUsarEspecial do EncontroDoChefe) e escreve
    /// parametros no Animator. Nao move a entidade, nao aplica dano e nao muda nenhum numero de jogo: o Animator
    /// fica sem root motion e nenhum Animation Event chama gameplay.
    ///
    /// Sincronia: os clipes Attack e Revoada rodam por "motion time" (parametros TempoDoAtaque/TempoDaRevoada), entao
    /// a mordida cai no instante real do acerto e o Raise da Revoada cai no AoUsarEspecial, qualquer que seja a
    /// distancia do mergulho ou a duracao do aviso.
    ///
    /// Morte: o Inimigo continua sendo destruido no mesmo frame (recompensa, lista de vivos e eventos intactos). Este
    /// visual se solta antes disso, toca o clipe Death num objeto sem logica nenhuma e se destroi sozinho.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(100)] // depois do Inimigo: o Animator avalia no mesmo frame em que o estado mudou
    public class AnimacaoDoInimigo : MonoBehaviour
    {
        public enum TipoDeLocomocao { Voo, Patas }

        public static readonly int PVelocidade = Animator.StringToHash("Velocidade");
        public static readonly int PRitmo = Animator.StringToHash("Ritmo");
        public static readonly int PVoando = Animator.StringToHash("Voando");
        public static readonly int PAtacando = Animator.StringToHash("Atacando");
        public static readonly int PTempoDoAtaque = Animator.StringToHash("TempoDoAtaque");
        public static readonly int PDano = Animator.StringToHash("Dano");
        public static readonly int PMorto = Animator.StringToHash("Morto");
        public static readonly int PRevoada = Animator.StringToHash("Revoada");
        public static readonly int PTempoDaRevoada = Animator.StringToHash("TempoDaRevoada");
        static readonly int EAttack = Animator.StringToHash("Attack");
        static readonly int EDeath = Animator.StringToHash("Death");

        [Header("Modelo")]
        public Animator animator;
        public TipoDeLocomocao locomocao = TipoDeLocomocao.Voo;

        [Header("Ataque (segundos do clipe a 1x)")]
        [Min(0.05f)] public float duracaoDoAtaque = 0.75f;
        [Tooltip("Instante da mordida no clipe Attack: cai no aoAtacar real.")]
        [Min(0f)] public float mordida = 0.417f;

        [Header("Locomocao no chao (unidades do modelo/s, medidas no Blender)")]
        [Min(0.01f)] public float velocidadeDaCaminhada = 0.989f;
        [Min(0.01f)] public float velocidadeDaCorrida = 4.571f;

        [Header("Revoada (so o chefe)")]
        public bool temRevoada;
        [Min(0.05f)] public float duracaoDaRevoada = 3.5f;
        [Tooltip("Instante do Revoada_Raise no clipe: cai no AoUsarEspecial.")]
        [Min(0f)] public float raiseDaRevoada = 2f;

        [Header("Morte")]
        [Min(0.05f)] public float duracaoDaMorte = 1f;
        [Tooltip("Tempo deitado no chao antes de sumir.")]
        [Min(0f)] public float tempoNoChao = 0.6f;
        [Min(0.01f)] public float tempoParaSumir = 0.25f;
        [Tooltip("Voadores caem ate o chao da casa: fracao da duracao da morte gasta na queda.")]
        [Range(0.1f, 1f)] public float fracaoDaQueda = 0.75f;
        [Tooltip("Altura do pivo acima do ponto mais baixo da malha no fim do clipe Death (unidades do modelo).")]
        [Min(0f)] public float alturaDoPivoNoChao = 0.3f;
        [Tooltip("Queda maxima em unidades do mundo (o chefe fica sobre o miolo oco: cai um pouco e some).")]
        [Min(0f)] public float quedaMaxima = 99f;

        [Header("Direcao visual (voadores)")]
        [Tooltip("Pousado, o corpo vira para o Lucca; no mergulho segue a direcao do voo com o pitch limitado.")]
        [Range(0f, 80f)] public float inclinacaoMaxima = 35f;
        [Min(0.1f)] public float suavizacaoDoGiro = 8f;

        public Inimigo Inimigo => _inimigo;
        public bool Destacado => _destacado;
        public float EscalaVisual => _escala;
        public float TempoDoAtaqueAtual => _tAtaque;
        public float TempoDaRevoadaAtual => _tRevoada;

        Inimigo _inimigo;
        NavMeshAgent _agente;
        HexPlayer _player;
        EncontroDoChefe _encontro;
        float _escala = 1f;
        bool _ligado, _destacado;

        // ataque
        bool _atacando, _contato;
        float _inicioDoAtaque, _previsaoDoContato, _instanteDoContato, _tAtaque, _ultimoGolpe = -999f;
        // revoada
        bool _revoada, _usada;
        float _inicioDoAviso, _duracaoDoAviso, _instanteDoUso, _tRevoada;
        // morte
        float _inicioDaMorte, _yInicial, _yDoChao;
        Vector3 _escalaInicial;

        /// <summary>Chamado pelo Inimigo ao montar o visual. escalaVisual = escala uniforme aplicada a este objeto.</summary>
        public void Ligar(Inimigo inimigo, float escalaVisual)
        {
            _inimigo = inimigo;
            _escala = Mathf.Max(0.0001f, escalaVisual);
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (animator != null) animator.applyRootMotion = false;
            // O NavMeshAgent do lobo e criado pelo Inimigo DEPOIS de montar o visual: busca preguicosa em Agente.
            _agente = null;
            _player = FindFirstObjectByType<HexPlayer>();
            _encontro = FindFirstObjectByType<EncontroDoChefe>();
            inimigo.aoComecarRasante.AddListener(AoComecarRasante);
            inimigo.aoAtacar.AddListener(AoAtacar);
            inimigo.aoSerDerrotado.AddListener(AoSerDerrotado);
            inimigo.AoSofrerDano += AoSofrerDano;
            if (temRevoada && _encontro != null)
            {
                _encontro.AoAvisarEspecial += AoAvisarEspecial;
                _encontro.AoUsarEspecial += AoUsarEspecial;
            }
            _ligado = true;
        }

        void OnDestroy() => Desligar();

        void Desligar()
        {
            if (!_ligado) return;
            _ligado = false;
            if (_inimigo != null)
            {
                _inimigo.aoComecarRasante.RemoveListener(AoComecarRasante);
                _inimigo.aoAtacar.RemoveListener(AoAtacar);
                _inimigo.aoSerDerrotado.RemoveListener(AoSerDerrotado);
                _inimigo.AoSofrerDano -= AoSofrerDano;
            }
            if (_encontro != null)
            {
                _encontro.AoAvisarEspecial -= AoAvisarEspecial;
                _encontro.AoUsarEspecial -= AoUsarEspecial;
            }
        }

        bool EuSouOChefe => _encontro != null && _inimigo != null && _encontro.Chefe == _inimigo;

        NavMeshAgent Agente
        {
            get
            {
                if (_agente == null && _inimigo != null) _agente = _inimigo.GetComponent<NavMeshAgent>();
                return _agente;
            }
        }

        /// <summary>
        /// Entra no estado agora, mesmo no meio de outra transicao (as transicoes do controller nao sao interrompiveis,
        /// e o Any State esperaria o fim delas): o ataque e a morte precisam comecar no frame do evento.
        /// </summary>
        void Entrar(int estado, float duracao)
        {
            if (animator == null || !animator.isActiveAndEnabled) return;
            var atual = animator.GetCurrentAnimatorStateInfo(0);
            bool indo = animator.IsInTransition(0) && animator.GetNextAnimatorStateInfo(0).shortNameHash == estado;
            if (atual.shortNameHash != estado && !indo) animator.CrossFadeInFixedTime(estado, duracao, 0);
        }

        // ------------------------------------------------------------------ eventos do gameplay

        void AoComecarRasante()
        {
            float distancia = 1f;
            if (_player != null)
                distancia = Vector3.Distance(_inimigo.transform.position,
                    _player.transform.position + Vector3.up * Inimigo.AlturaDoPeito);
            float velocidade = Mathf.Max(0.1f, _inimigo.Def.velocidadeDoRasante);
            float previsto = Mathf.Clamp(distancia / velocidade, 0.12f, _inimigo.Def.duracaoMaximaDoRasante);
            ComecarAtaque(previsto);
        }

        void AoAtacar(int _)
        {
            _ultimoGolpe = Time.time;
            if (!_atacando)
            {
                // Golpe sem aviso visual (lobo que ja estava no alcance): entra pouco antes da mordida.
                ComecarAtaque(0.06f);
                _tAtaque = Mathf.Max(0f, (mordida - 0.06f) / duracaoDoAtaque);
            }
            if (!_contato)
            {
                _contato = true;
                _instanteDoContato = Time.time;
            }
        }

        void ComecarAtaque(float previsaoDoContato)
        {
            _atacando = true;
            _contato = false;
            _inicioDoAtaque = Time.time;
            _previsaoDoContato = Mathf.Max(0.05f, previsaoDoContato);
            _tAtaque = 0f;
        }

        void AoSofrerDano(Inimigo quem, int dano)
        {
            if (quem != _inimigo || animator == null || !quem.EstaVivo) return;
            animator.SetTrigger(PDano);
        }

        void AoAvisarEspecial(float segundos)
        {
            if (!EuSouOChefe) return;
            _revoada = true;
            _usada = false;
            _inicioDoAviso = Time.time;
            _duracaoDoAviso = Mathf.Max(0.05f, segundos);
            _tRevoada = 0f;
        }

        void AoUsarEspecial(int quantos)
        {
            if (!_revoada || !EuSouOChefe) return;
            _usada = true;
            _instanteDoUso = Time.time;
        }

        void AoSerDerrotado(Inimigo quem)
        {
            if (quem != _inimigo || _destacado) return;
            // O Inimigo sera destruido no fim deste frame: o visual se solta e vive so o tempo da morte.
            Desligar();
            _destacado = true;
            _inicioDaMorte = Time.time;
            _yInicial = transform.position.y;
            _yDoChao = _yInicial;
            if (quem.Def != null && quem.Def.voa)
            {
                float chao = quem.Poleiro.y - quem.Def.alturaDeVoo + alturaDoPivoNoChao * _escala;
                _yDoChao = Mathf.Max(chao, _yInicial - quedaMaxima);
            }
            transform.SetParent(null, true);
            _escalaInicial = transform.localScale;
            name = name + " (morte)";
            if (animator != null)
            {
                animator.SetBool(PAtacando, false);
                if (temRevoada) animator.SetBool(PRevoada, false);
                animator.SetBool(PMorto, true);
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                Entrar(EDeath, 0.12f);
            }
        }

        // ------------------------------------------------------------------ quadro a quadro

        void Update()
        {
            if (_destacado)
            {
                AtualizarMorte();
                return;
            }
            if (!_ligado || _inimigo == null || animator == null) return;

            bool ativo = _inimigo.enabled && _inimigo.EstaVivo;
            AtualizarAtaque(ativo);
            if (temRevoada) AtualizarRevoada(ativo);

            if (locomocao == TipoDeLocomocao.Patas)
            {
                var agente = Agente;
                float v = agente != null && agente.enabled ? agente.velocity.magnitude / _escala : 0f;
                if (!ativo) v = 0f;
                animator.SetFloat(PVelocidade, v);
                float ritmo = 1f;
                if (v < velocidadeDaCaminhada) ritmo = Mathf.Clamp(v / velocidadeDaCaminhada, 0.5f, 1f);
                else if (v > velocidadeDaCorrida) ritmo = v / velocidadeDaCorrida;
                animator.SetFloat(PRitmo, ritmo);
                if (ativo && !_atacando && _inimigo.EstadoDaIA == "Atacando")
                {
                    // Proximo golpe previsto pelo intervalo do Def: a antecipacao comeca antes e a mordida cai nele.
                    float proximo = _ultimoGolpe + _inimigo.Def.intervaloDeAtaque;
                    if (_ultimoGolpe > 0f && Time.time >= proximo - mordida && Time.time < proximo)
                        ComecarAtaque(proximo - Time.time);
                }
            }
            else
            {
                animator.SetBool(PVoando, ativo && _inimigo.EstaEmRasante && !_inimigo.EstaMergulhando);
            }
        }

        void AtualizarAtaque(bool ativo)
        {
            if (!_atacando)
            {
                animator.SetBool(PAtacando, false);
                return;
            }
            float tMordida = mordida / duracaoDoAtaque;
            if (!_contato)
            {
                // Mergulho acabou sem acertar (o Lucca escapou): a recuperacao comeca agora.
                bool voo = locomocao == TipoDeLocomocao.Voo;
                bool acabou = voo ? !_inimigo.EstaMergulhando && Time.time > _inicioDoAtaque + 0.05f
                                  : Time.time > _inicioDoAtaque + _previsaoDoContato + 0.25f; // golpe previsto nao veio
                if (!ativo || acabou)
                {
                    _contato = true;
                    _instanteDoContato = Time.time;
                }
                else
                {
                    float p = Mathf.Clamp01((Time.time - _inicioDoAtaque) / _previsaoDoContato);
                    // Sem passar da mordida antes do acerto real.
                    _tAtaque = Mathf.Min(tMordida * 0.98f, tMordida * p);
                }
            }
            if (_contato)
            {
                float desde = Time.time - _instanteDoContato;
                float alvo = tMordida + desde / duracaoDoAtaque;
                // Se o acerto chegou antes da previsao, alcanca a mordida a 3x em vez de pular de pose.
                _tAtaque = _tAtaque < tMordida
                    ? Mathf.MoveTowards(_tAtaque, alvo, Time.deltaTime * 3f / duracaoDoAtaque)
                    : alvo;
                if (_tAtaque >= 1f)
                {
                    _tAtaque = 1f;
                    _atacando = false;
                }
            }
            animator.SetFloat(PTempoDoAtaque, _tAtaque);
            animator.SetBool(PAtacando, _atacando);
            if (_atacando) Entrar(EAttack, 0.08f);
        }

        void AtualizarRevoada(bool ativo)
        {
            if (_revoada)
            {
                float tRaise = raiseDaRevoada / duracaoDaRevoada;
                if (!_usada)
                    _tRevoada = tRaise * Mathf.Clamp01((Time.time - _inicioDoAviso) / _duracaoDoAviso);
                else
                    _tRevoada = tRaise + (Time.time - _instanteDoUso) / duracaoDaRevoada;
                if (_tRevoada >= 1f || !ativo)
                {
                    _tRevoada = 1f;
                    _revoada = false;
                }
            }
            animator.SetFloat(PTempoDaRevoada, _tRevoada);
            animator.SetBool(PRevoada, _revoada);
        }

        void LateUpdate()
        {
            if (_destacado || !_ligado || _inimigo == null || locomocao != TipoDeLocomocao.Voo) return;
            // Direcao so do visual: o transform de gameplay continua como o Inimigo deixou.
            Vector3 frente = _inimigo.transform.forward;
            float pitch = 0f;
            if (_inimigo.EstaEmRasante)
            {
                pitch = -Mathf.Asin(Mathf.Clamp(frente.y, -1f, 1f)) * Mathf.Rad2Deg;
                pitch = Mathf.Clamp(pitch, -inclinacaoMaxima, inclinacaoMaxima);
            }
            else if (_player != null)
            {
                Vector3 paraOLucca = _player.transform.position - _inimigo.transform.position;
                paraOLucca.y = 0f;
                if (paraOLucca.sqrMagnitude > 0.01f) frente = paraOLucca;
            }
            frente.y = 0f;
            if (frente.sqrMagnitude < 1e-4f) return;
            Quaternion alvo = Quaternion.LookRotation(frente.normalized, Vector3.up) * Quaternion.Euler(pitch, 0f, 0f);
            float k = 1f - Mathf.Exp(-suavizacaoDoGiro * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, alvo, k);
        }

        void AtualizarMorte()
        {
            float t = Time.time - _inicioDaMorte;
            if (_yDoChao < _yInicial)
            {
                // perda de sustentacao: acelera ate o chao da casa (gravidade aparente, so visual)
                float u = Mathf.Clamp01(t / (duracaoDaMorte * fracaoDaQueda));
                Vector3 p = transform.position;
                p.y = Mathf.Lerp(_yInicial, _yDoChao, u * u);
                transform.position = p;
            }
            float sumir = t - duracaoDaMorte - tempoNoChao;
            if (sumir > 0f)
            {
                float s = 1f - Mathf.Clamp01(sumir / tempoParaSumir);
                transform.localScale = _escalaInicial * s;
                if (s <= 0f) Destroy(gameObject);
            }
        }

        /// <summary>Tempo total que o visual destacado vive depois da morte logica.</summary>
        public float TempoDeVidaDaMorte => duracaoDaMorte + tempoNoChao + tempoParaSumir;
    }
}

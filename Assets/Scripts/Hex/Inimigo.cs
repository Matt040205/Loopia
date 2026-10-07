using UnityEngine;
using UnityEngine.Events;
using UnityEngine.AI;

namespace Loopia.Hex
{
    /// <summary>
    /// Um inimigo vivo. Tudo que ele faz para fora sai por UnityEvent: ele nao conhece o
    /// LuccaStatus, so dispara aoAtacar com o dano, e quem estiver ouvindo aplica. Na cena, o
    /// InimigoSpawner repassa esse evento para um UnityEvent dele, ligado no Inspector ao
    /// LuccaStatus.ReceberDano.
    ///
    /// Dois jeitos de atacar, vindos do InimigoDef:
    /// - corpo a corpo (lobo): golpeia de onde esta quando o Lucca entra no alcance;
    /// - rasante (morcego): mergulha em direcao ao Lucca, so causa dano se alcancar, e volta
    ///   para o poleiro antes de poder mergulhar de novo.
    ///
    /// Os numeros ja nascem multiplicados pela escala da volta: e aqui que o "+10% de PV, ATK e
    /// XP por volta" do GDD vira efeito de verdade.
    /// </summary>
    [DisallowMultipleComponent]
    public class Inimigo : MonoBehaviour
    {
        enum Estado
        {
            Pousado,
            Mergulhando,
            Voltando,
        }

        /// <summary>Altura do peito do Lucca, medida do pe: e para la que o morcego mergulha.</summary>
        const float AlturaDoPeito = 0.6f;

        [Header("Eventos")]
        [Tooltip("Disparado quando o golpe acerta, com o dano ja escalado pela volta.")]
        public UnityEvent<int> aoAtacar = new UnityEvent<int>();

        [Tooltip("Disparado quando o morcego comeca o mergulho. Bom lugar para som e efeito.")]
        public UnityEvent aoComecarRasante = new UnityEvent();

        [Tooltip("Disparado quando o PV chega a zero.")]
        public UnityEvent<Inimigo> aoSerDerrotado = new UnityEvent<Inimigo>();

        public InimigoDef Def { get; private set; }
        public HexCoord Casa { get; private set; }

        public int PvMaximo { get; private set; }
        public int PvAtual { get; private set; }
        public int Ataque { get; private set; }
        public int Xp { get; private set; }

        public bool EstaVivo => PvAtual > 0;
        public bool EstaEmRasante => _estado != Estado.Pousado;

        HexPlayer _player;
        HexWorld _mundo;
        NavMeshAgent _agente;
        float _proximaBusca;
        public string EstadoDaIA { get; private set; } = "Esperando";
        Estado _estado;

        float _proximoAtaque;
        float _fimDoRasante;
        float _faseDoBalanco;
        Vector3 _poleiro;

        public void Nascer(InimigoDef def, HexCoord casa, float escalaDaVolta, HexWorld mundo, HexPlayer player)
        {
            Def = def;
            Casa = casa;
            _player = player;
            _mundo = mundo;
            _estado = Estado.Pousado;

            PvMaximo = Mathf.Max(1, Mathf.RoundToInt(def.pv * escalaDaVolta));
            PvAtual = PvMaximo;
            Ataque = Mathf.RoundToInt(def.ataque * escalaDaVolta);
            Xp = Mathf.RoundToInt(def.xp * escalaDaVolta);

            name = def.nome + " " + casa;

            _poleiro = mundo.PosicaoDe(casa) + Vector3.up * (def.voa ? def.alturaDeVoo : 0f);
            transform.position = _poleiro;

            // Fases diferentes para os morcegos nao balancarem todos juntos.
            _faseDoBalanco = Random.Range(0f, Mathf.PI * 2f);

            // Espera um ciclo antes do primeiro golpe, para nao machucar no mesmo frame em que nasce.
            _proximoAtaque = Time.time + def.intervaloDeAtaque;

            MontarVisual(def);
            if (!def.voa)
            {
                // Configura o tipo antes de ativar: o agente padrão pode não existir neste NavMesh.
                gameObject.SetActive(false);
                _agente = gameObject.AddComponent<NavMeshAgent>();
                _agente.enabled = false;
                var navegacao = mundo.GetComponent<NavegacaoDoMundo>();
                if (navegacao != null) _agente.agentTypeID = navegacao.superficie.agentTypeID;
                _agente.radius = 0.18f;
                _agente.height = 0.7f;
                _agente.speed = def.velocidadeDePerseguicao;
                _agente.acceleration = 30f;
                _agente.angularSpeed = 540f;
                _agente.stoppingDistance = def.distanciaCorpoACorpo * 0.8f;
                _agente.areaMask = NavMesh.AllAreas & ~(1 << 2);
                _agente.autoTraverseOffMeshLink = false;
                gameObject.SetActive(true);
                var filtro = new NavMeshQueryFilter { agentTypeID = _agente.agentTypeID, areaMask = _agente.areaMask };
                if (NavMesh.SamplePosition(_poleiro, out NavMeshHit hit, 1f, filtro))
                {
                    transform.position = hit.position;
                    _agente.enabled = true;
                    _agente.Warp(hit.position);
                }
            }
        }

        void Update()
        {
            if (!EstaVivo) return;
            if (!Def.voa && Def.estilo == EstiloDeAtaque.CorpoACorpo)
            {
                AtualizarLobo();
                return;
            }

            switch (_estado)
            {
                case Estado.Pousado: AtualizarPousado(); break;
                case Estado.Mergulhando: AtualizarMergulho(); break;
                case Estado.Voltando: AtualizarVolta(); break;
            }
        }

        void AtualizarPousado()
        {
            if (Def.voa && Def.balanco > 0f)
            {
                float altura = Mathf.Sin(Time.time * 2.5f + _faseDoBalanco) * Def.balanco;
                transform.position = _poleiro + Vector3.up * altura;
            }

            if (_player == null || Time.time < _proximoAtaque) return;
            if (HexCoord.Distance(Casa, _player.Coord) > Def.alcance) return;

            if (Def.estilo == EstiloDeAtaque.Rasante)
            {
                _estado = Estado.Mergulhando;
                _fimDoRasante = Time.time + Def.duracaoMaximaDoRasante;
                aoComecarRasante.Invoke();
                return;
            }

            Golpear();
            _proximoAtaque = Time.time + Def.intervaloDeAtaque;
        }

        void AtualizarLobo()
        {
            if (_agente == null || !_agente.enabled || !_agente.isOnNavMesh || _player == null) return;
            bool naIlha = _mundo.MundoParaHex(_player.transform.position) == Casa;
            float distancia = Vector3.Distance(transform.position, _player.transform.position);
            bool detectou = naIlha && distancia <= Def.raioDeDeteccao;
            Vector3 alvo = detectou ? _player.transform.position : _poleiro;
            EstadoDaIA = detectou ? "Perseguindo" : "Voltando";
            if (detectou && !_player.EstaPulando && distancia <= Def.distanciaCorpoACorpo)
            {
                EstadoDaIA = "Atacando";
                _agente.isStopped = true;
                Vector3 direcao = alvo - transform.position;
                direcao.y = 0f;
                if (direcao.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(direcao);
                if (Time.time >= _proximoAtaque)
                {
                    Golpear();
                    _proximoAtaque = Time.time + Def.intervaloDeAtaque;
                }
                return;
            }
            _agente.isStopped = false;
            if (Time.time < _proximaBusca) return;
            _proximaBusca = Time.time + 0.15f;
            var filtro = new NavMeshQueryFilter { agentTypeID = _agente.agentTypeID, areaMask = _agente.areaMask };
            if (NavMesh.SamplePosition(alvo, out NavMeshHit hit, 0.75f, filtro)) _agente.SetDestination(hit.position);
        }

        void OnDisable()
        {
            if (_agente != null && _agente.enabled && _agente.isOnNavMesh) _agente.isStopped = true;
        }

        public void AjustarAoRelevo()
        {
            Vector3 novoPoleiro = _mundo.PosicaoDe(Casa) + Vector3.up * (Def.voa ? Def.alturaDeVoo : 0f);
            float mudanca = novoPoleiro.y - _poleiro.y;
            _poleiro = novoPoleiro;
            if (Mathf.Approximately(mudanca, 0f)) return;
            bool parado = _agente != null && _agente.enabled && _agente.isOnNavMesh && _agente.isStopped;
            if (_agente != null) _agente.enabled = false;
            transform.position += Vector3.up * mudanca;
            if (_agente == null) return;
            var filtro = new NavMeshQueryFilter { agentTypeID = _agente.agentTypeID, areaMask = _agente.areaMask };
            if (!NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 1f, filtro)) return;
            transform.position = hit.position;
            _agente.enabled = true;
            _agente.Warp(hit.position);
            _agente.isStopped = parado;
            _proximaBusca = 0f;
        }

        void AtualizarMergulho()
        {
            if (_player == null)
            {
                _estado = Estado.Voltando;
                return;
            }

            Vector3 alvo = _player.transform.position + Vector3.up * AlturaDoPeito;
            VoarAte(alvo, Def.velocidadeDoRasante);

            float alcance = Def.distanciaDoAcerto;
            if ((transform.position - alvo).sqrMagnitude <= alcance * alcance)
            {
                // So o mergulho que chega de fato no Lucca machuca.
                Golpear();
                _estado = Estado.Voltando;
                return;
            }

            // O Lucca nunca para de andar: se escapou, o morcego desiste e volta sem dano.
            if (Time.time >= _fimDoRasante) _estado = Estado.Voltando;
        }

        void AtualizarVolta()
        {
            VoarAte(_poleiro, Def.velocidadeDoRasante * 0.6f);

            if ((transform.position - _poleiro).sqrMagnitude > 0.0004f) return;

            transform.position = _poleiro;
            _estado = Estado.Pousado;
            _proximoAtaque = Time.time + Def.intervaloDeAtaque;
        }

        void VoarAte(Vector3 destino, float velocidade)
        {
            Vector3 direcao = destino - transform.position;
            if (direcao.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(direcao);

            transform.position = Vector3.MoveTowards(transform.position, destino, velocidade * Time.deltaTime);
        }

        void Golpear() => aoAtacar.Invoke(Ataque);

        public void ReceberDano(int dano)
        {
            if (!EstaVivo || dano <= 0) return;

            PvAtual = Mathf.Max(0, PvAtual - dano);
            if (PvAtual > 0) return;

            if (Def.efeitoDeMorte != null)
                Instantiate(Def.efeitoDeMorte, transform.position + Vector3.up * 0.35f, Quaternion.identity);

            aoSerDerrotado.Invoke(this);
            Destroy(gameObject);
        }

        void MontarVisual(InimigoDef def)
        {
            GameObject corpo;
            if (def.prefab != null)
            {
                corpo = VisualDeIlha.Criar(def.prefab, transform, def.escala);
            }
            else
            {
                // Sem arte ainda: bolinha para quem voa, caixa para quem anda.
                corpo = GameObject.CreatePrimitive(def.voa ? PrimitiveType.Sphere : PrimitiveType.Cube);
                Collider colisor = corpo.GetComponent<Collider>();
                if (colisor != null) Destroy(colisor);
                corpo.transform.SetParent(transform, false);
            }

            corpo.name = "Corpo";
            if (def.prefab == null)
            {
                corpo.transform.localScale = Vector3.one * def.escala;
                corpo.transform.localPosition = Vector3.up * (def.voa ? 0f : def.escala * 0.5f);
            }

            if (def.prefab != null) return;

            var renderizador = corpo.GetComponent<MeshRenderer>();
            if (renderizador == null) return;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            var material = new Material(shader) { name = def.nome };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", def.cor);
            if (material.HasProperty("_Color")) material.SetColor("_Color", def.cor);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.1f);
            renderizador.sharedMaterial = material;
        }
    }
}

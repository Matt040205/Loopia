using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Loopia.Hex
{
    /// <summary>
    /// O Lucca sobre o mundo. Anda pelo NavMesh com um NavMeshAgent, uma ilha por vez, e salta
    /// o vao entre elas quando o agente chega num NavMeshLink — o "Espaco Pequeno (Salto): Pula
    /// automaticamente" do GDD.
    ///
    /// Por fora continua funcionando por casas: quem manda nele (o percurso do loop, o controle
    /// de depuracao) so enfileira hexagonos, e ele avisa em AoEntrarNoHexagono a cada chegada.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NavMeshAgent))]
    public class HexPlayer : MonoBehaviour
    {
        [Header("Mundo")]
        [Tooltip("Deixe vazio para achar o HexWorld da cena automaticamente.")]
        public HexWorld mundo;

        [Header("Movimento (NavMeshAgent)")]
        [Tooltip("Unidades por segundo.")]
        [Min(0.1f)] public float velocidade = 3.5f;

        [Tooltip("Graus por segundo que o agente gira para encarar a direcao em que anda.")]
        [Min(1f)] public float velocidadeAngular = 720f;

        [Tooltip("Quao perto do centro da ilha conta como ter chegado nela.")]
        [Min(0.01f)] public float toleranciaDeChegada = 0.12f;

        [Header("Pulo entre ilhas")]
        [Tooltip("Altura maxima do arco do pulo.")]
        [Min(0f)] public float alturaDoPulo = 0.55f;

        [Tooltip("Quanto tempo o pulo leva, em segundos.")]
        [Min(0.05f)] public float duracaoDoPulo = 0.35f;

        readonly List<HexCoord> _caminho = new List<HexCoord>();
        readonly List<HexCoord> _buffer = new List<HexCoord>();

        NavMeshAgent _agente;
        HexCoord _destinoAtual;
        bool _temDestino;
        bool _pulando;

        /// <summary>Hexagono que o player ocupa agora.</summary>
        public HexCoord Coord { get; private set; }

        public bool EstaAndando => _caminho.Count > 0;

        public bool EstaPulando => _pulando;

        public NavMeshAgent Agente => _agente;

        /// <summary>Passos que ainda faltam, na ordem. Somente leitura.</summary>
        public IReadOnlyList<HexCoord> CaminhoAtual => _caminho;

        /// <summary>
        /// Ultimo hexagono da fila de movimento, ou seja, de onde o proximo passo tem que sair.
        /// Com a fila vazia, e o hexagono atual.
        /// </summary>
        public HexCoord FimDaFila => _caminho.Count > 0 ? _caminho[_caminho.Count - 1] : Coord;

        /// <summary>Disparado toda vez que o player termina de entrar num hexagono.</summary>
        public event Action<HexCoord> AoEntrarNoHexagono;

        /// <summary>Disparado quando o player chega ao ultimo hexagono do caminho.</summary>
        public event Action<HexCoord> AoChegarNoDestino;

        void Awake()
        {
            _agente = GetComponent<NavMeshAgent>();
            ConfigurarAgente();
        }

        void Start()
        {
            if (mundo == null) mundo = FindFirstObjectByType<HexWorld>();

            if (mundo == null)
            {
                Debug.LogError("[HexPlayer] Nenhum HexWorld encontrado na cena.", this);
                enabled = false;
                return;
            }

            NascerNoAcampamento();
        }

        void ConfigurarAgente()
        {
            _agente.speed = velocidade;
            _agente.angularSpeed = velocidadeAngular;
            _agente.acceleration = 60f;

            // Sem frear na chegada: o proximo destino e dado antes, entao ele nao para entre ilhas.
            _agente.autoBraking = false;
            _agente.stoppingDistance = 0f;

            // O pulo e animado aqui, em arco, em vez do deslize reto padrao do agente.
            _agente.autoTraverseOffMeshLink = false;

            _agente.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
        }

        /// <summary>Coloca o player no acampamento, sem animacao, e cancela o caminho.</summary>
        [ContextMenu("Nascer no acampamento")]
        public void NascerNoAcampamento()
        {
            if (mundo == null) return;
            ColocarEm(mundo.Acampamento);
        }

        /// <summary>Teleporta o player para um hexagono e limpa o caminho.</summary>
        public void ColocarEm(HexCoord coord)
        {
            StopAllCoroutines();
            _pulando = false;
            _caminho.Clear();
            _temDestino = false;
            Coord = coord;

            Vector3 alvo = mundo.PosicaoDe(coord);
            transform.position = alvo;

            // O agente fica desligado na cena ate aqui: ligar antes do NavMesh existir da erro.
            if (!_agente.enabled) _agente.enabled = true;
            if (_agente.isOnOffMeshLink) _agente.CompleteOffMeshLink();

            _agente.Warp(alvo);
            if (_agente.isOnNavMesh) _agente.ResetPath();
        }

        /// <summary>
        /// Manda o player andar ate o hexagono informado. Se ele ja estiver andando,
        /// termina o passo atual antes de seguir pelo novo caminho, para nao girar no meio do hexagono.
        /// Devolve false se nao existir caminho ate la.
        /// </summary>
        public bool MoverPara(HexCoord destino)
        {
            if (mundo == null) return false;

            // Enquanto anda, o proximo hexagono ja esta comprometido: e dali que o novo caminho sai.
            HexCoord origem = _caminho.Count > 0 ? _caminho[0] : Coord;

            if (!mundo.TryFindPath(origem, destino, _buffer)) return false;

            _caminho.Clear();
            if (origem != Coord) _caminho.Add(origem);
            _caminho.AddRange(_buffer);
            _temDestino = false;
            return true;
        }

        /// <summary>
        /// Acrescenta UM passo no fim da fila, sem pathfinding: o hexagono precisa ser vizinho
        /// do fim da fila. E assim que o percurso automatico do loop alimenta o player, mantendo
        /// a fila sempre cheia para ele nunca parar entre um hexagono e outro.
        /// </summary>
        public bool EnfileirarPasso(HexCoord vizinho)
        {
            if (mundo == null) return false;
            if (HexCoord.Distance(FimDaFila, vizinho) != 1) return false;
            if (!mundo.IsWalkable(vizinho)) return false;

            _caminho.Add(vizinho);
            return true;
        }

        /// <summary>Caminho que o player faria ate o destino, sem comecar a andar.</summary>
        public bool PreverCaminho(HexCoord destino, List<HexCoord> resultado)
        {
            if (mundo == null)
            {
                resultado.Clear();
                return false;
            }
            return mundo.TryFindPath(Coord, destino, resultado);
        }

        /// <summary>Para no proximo hexagono, sem cortar o passo pela metade.</summary>
        public void Parar()
        {
            if (_caminho.Count > 1) _caminho.RemoveRange(1, _caminho.Count - 1);
        }

        void Update()
        {
            if (_pulando || !_agente.enabled) return;

            if (_agente.isOnOffMeshLink)
            {
                StartCoroutine(Pular());
                return;
            }

            if (_caminho.Count == 0 || !_agente.isOnNavMesh) return;

            HexCoord alvo = _caminho[0];
            Vector3 destino = mundo.PosicaoDe(alvo);

            if (!_temDestino || _destinoAtual != alvo)
            {
                _agente.SetDestination(destino);
                _destinoAtual = alvo;
                _temDestino = true;
            }

            Vector3 falta = destino - transform.position;
            falta.y = 0f;
            if (falta.sqrMagnitude > toleranciaDeChegada * toleranciaDeChegada) return;

            Chegou(alvo);
        }

        void Chegou(HexCoord casa)
        {
            Coord = casa;
            _caminho.RemoveAt(0);
            _temDestino = false;

            AoEntrarNoHexagono?.Invoke(Coord);

            // Quem ouviu o evento pode ter teleportado o player ou limpado a fila.
            if (_caminho.Count == 0)
            {
                if (_agente.isOnNavMesh) _agente.ResetPath();
                AoChegarNoDestino?.Invoke(Coord);
                return;
            }

            // Ja aponta para a proxima ilha, senao o agente para no centro desta.
            _destinoAtual = _caminho[0];
            _temDestino = true;
            _agente.SetDestination(mundo.PosicaoDe(_destinoAtual));
        }

        /// <summary>
        /// Atravessa o NavMeshLink em arco. E o mesmo padrao do exemplo oficial da Unity para links
        /// (AgentLinkMover): posicao controlada a mao durante o link e CompleteOffMeshLink no fim.
        /// </summary>
        IEnumerator Pular()
        {
            _pulando = true;

            OffMeshLinkData link = _agente.currentOffMeshLinkData;
            Vector3 inicio = transform.position;
            Vector3 fim = link.endPos + Vector3.up * _agente.baseOffset;

            Vector3 direcao = fim - inicio;
            direcao.y = 0f;
            Quaternion giroInicial = transform.rotation;
            Quaternion giroFinal = direcao.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(direcao) : giroInicial;

            float t = 0f;
            while (t < 1f)
            {
                t = Mathf.Min(1f, t + Time.deltaTime / duracaoDoPulo);

                // Parabola que comeca e termina na altura zero e chega no topo no meio do salto.
                float altura = alturaDoPulo * 4f * (t - t * t);
                transform.position = Vector3.Lerp(inicio, fim, t) + Vector3.up * altura;
                transform.rotation = Quaternion.Slerp(giroInicial, giroFinal, Mathf.Clamp01(t * 3f));

                yield return null;
            }

            _agente.CompleteOffMeshLink();
            _pulando = false;
        }
    }
}

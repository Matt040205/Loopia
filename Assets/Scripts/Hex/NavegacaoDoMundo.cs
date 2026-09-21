using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace Loopia.Hex
{
    /// <summary>
    /// Monta o NavMesh em que o Lucca anda. Como as ilhas sao geradas em tempo de jogo, o bake
    /// tambem acontece em tempo de jogo, logo depois de cada mapa novo.
    ///
    /// As ilhas ficam separadas por um vao, entao cada uma vira um pedaco de NavMesh isolado.
    /// O que liga uma na seguinte sao os NavMeshLinks criados aqui, na area "Jump": e por eles
    /// que o agente atravessa, e o HexPlayer anima o salto quando chega num link.
    ///
    /// Roda antes dos scripts comuns (DefaultExecutionOrder) porque o NavMeshAgent do Lucca
    /// precisa encontrar o NavMesh pronto quando for ligado no Start.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public class NavegacaoDoMundo : MonoBehaviour
    {
        const int AreaDePulo = 2; // "Jump" em Project Settings > Navigation > Areas

        [Header("Referencias")]
        [Tooltip("Deixe vazio para achar automaticamente na cena.")]
        public HexWorld mundo;
        public HexLoopGenerator gerador;

        [Tooltip("A superficie que faz o bake. Fica no objeto Mundo e coleta as ilhas filhas dele.")]
        public NavMeshSurface superficie;

        [Header("Pulos")]
        [Tooltip("Quanto a ponta do pulo fica para dentro da area andavel, longe da beirada.")]
        [Min(0f)] public float recuoDaBeirada = 0.06f;

        readonly List<NavMeshLink> _pulos = new List<NavMeshLink>();

        Transform _raizDosPulos;
        bool _inscrito;

        public IReadOnlyList<NavMeshLink> Pulos => _pulos;

        void Awake()
        {
            if (mundo == null) mundo = FindFirstObjectByType<HexWorld>();
            if (gerador == null) gerador = FindFirstObjectByType<HexLoopGenerator>();
            if (superficie == null && mundo != null) superficie = mundo.GetComponent<NavMeshSurface>();

            if (mundo == null || gerador == null || superficie == null)
            {
                Debug.LogError("[NavegacaoDoMundo] Precisa de HexWorld, HexLoopGenerator e NavMeshSurface.", this);
                enabled = false;
                return;
            }

            gerador.AoGerarMapa += Reconstruir;
            _inscrito = true;

            // O gerador (ordem de execucao anterior) ja criou o primeiro mapa no Awake dele.
            if (gerador.Loop != null) Reconstruir();
        }

        void OnDestroy()
        {
            if (_inscrito && gerador != null) gerador.AoGerarMapa -= Reconstruir;
            _inscrito = false;
        }

        [ContextMenu("Reconstruir NavMesh")]
        public void Reconstruir()
        {
            if (gerador == null || gerador.Loop == null || gerador.Loop.Count == 0) return;

            superficie.BuildNavMesh();
            RecriarPulos();
        }

        void RecriarPulos()
        {
            for (int i = 0; i < _pulos.Count; i++)
            {
                if (_pulos[i] == null) continue;

                // Desligar tira o link do NavMesh na hora; o Destroy so acontece no fim do frame.
                _pulos[i].enabled = false;
                Destroy(_pulos[i].gameObject);
            }
            _pulos.Clear();

            if (_raizDosPulos == null)
            {
                _raizDosPulos = new GameObject("Pulos").transform;
                _raizDosPulos.SetParent(mundo.transform, false);
            }

            float alcance = AlcanceAndavel();
            HexLoopPath loop = gerador.Loop;

            // Um link por par de ilhas vizinhas do anel, e bidirecional para as duas maos da volta.
            for (int i = 0; i < loop.Count; i++)
            {
                CriarPulo(loop[i], loop[i + 1], alcance);
            }
        }

        /// <summary>
        /// Distancia do centro da ilha ate onde ainda da para pisar: o apotema do hexagono
        /// menos o raio do agente (que o bake desconta da beirada) e um recuo de seguranca.
        /// </summary>
        float AlcanceAndavel()
        {
            float tamanhoDaIlha = mundo.tamanhoDoHexagono * (1f - mundo.folgaEntreHexagonos);
            float apotema = tamanhoDaIlha * HexLayout.Sqrt3 * 0.5f;
            float raioDoAgente = NavMesh.GetSettingsByID(superficie.agentTypeID).agentRadius;

            return Mathf.Max(0.02f, apotema - raioDoAgente - recuoDaBeirada);
        }

        void CriarPulo(HexCoord de, HexCoord para, float alcance)
        {
            Vector3 origem = mundo.PosicaoDe(de);
            Vector3 destino = mundo.PosicaoDe(para);

            Vector3 direcao = destino - origem;
            direcao.y = 0f;
            direcao.Normalize();

            var go = new GameObject("Pulo " + de + " -> " + para);
            go.transform.SetParent(_raizDosPulos, false);
            go.transform.position = origem;

            var link = go.AddComponent<NavMeshLink>();
            link.agentTypeID = superficie.agentTypeID;
            link.area = AreaDePulo;
            link.width = 0f;
            link.bidirectional = true;

            // Pontas do link no espaco local do objeto: da beirada de uma ilha a beirada da outra.
            link.startPoint = go.transform.InverseTransformPoint(origem + direcao * alcance);
            link.endPoint = go.transform.InverseTransformPoint(destino - direcao * alcance);

            _pulos.Add(link);
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace Loopia.Hex
{
    /// <summary>
    /// Moedas que caem nas ilhas do caminho. No comeco e a cada volta fechada, cada ilha do anel
    /// tem uma chance de ganhar uma moeda; quando o Lucca passa por ela, pega, e a moeda vai para
    /// o inventario (e dali para o total salvo). GDD: "moedas: adiciona 1 moeda no inventario".
    ///
    /// As moedas ficam fora do objeto Mundo de proposito: ele e o que o NavMesh assa, e moeda
    /// nao e chao.
    /// </summary>
    [DisallowMultipleComponent]
    public class GeradorDeMoedas : MonoBehaviour
    {
        [Header("Referencias")]
        [Tooltip("Deixe vazio para achar automaticamente na cena.")]
        public HexWorld mundo;
        public HexLoopGenerator gerador;
        public HexLoopRunner corredor;
        public HexPlayer player;
        public LuccaStatus status;

        [Header("Queda")]
        [Tooltip("Chance de cada ilha do caminho ganhar uma moeda, sorteada no comeco e a cada volta.")]
        [Range(0f, 1f)] public float chancePorIlha = 0.2f;

        [Tooltip("GDD: cada moeda adiciona 1 ao inventario.")]
        [Min(1)] public int valorDaMoeda = 1;

        [Header("Visual")]
        public Color corDaMoeda = new Color(1f, 0.80f, 0.20f);
        [Min(0f)] public float alturaDaMoeda = 0.5f;
        public float velocidadeDeGiro = 180f;

        readonly Dictionary<HexCoord, Transform> _moedas = new Dictionary<HexCoord, Transform>();

        Transform _raiz;
        Material _material;
        bool _inscrito;

        public int MoedasNoChao => _moedas.Count;

        public bool TemMoedaEm(HexCoord casa) => _moedas.ContainsKey(casa);

        void Start()
        {
            if (mundo == null) mundo = FindFirstObjectByType<HexWorld>();
            if (gerador == null) gerador = FindFirstObjectByType<HexLoopGenerator>();
            if (corredor == null) corredor = FindFirstObjectByType<HexLoopRunner>();
            if (player == null) player = FindFirstObjectByType<HexPlayer>();
            if (status == null) status = FindFirstObjectByType<LuccaStatus>();

            if (mundo == null || gerador == null || player == null || status == null)
            {
                Debug.LogError("[GeradorDeMoedas] Faltou referencia na cena.", this);
                enabled = false;
                return;
            }

            _raiz = new GameObject("Moedas").transform;
            _raiz.SetParent(transform, false);
            _material = CriarMaterial(corDaMoeda);

            player.AoEntrarNoHexagono += Coletar;
            gerador.AoGerarMapa += Recomecar;
            if (corredor != null) corredor.AoCompletarVolta += VoltaFechou;
            _inscrito = true;

            Sortear();
        }

        void OnDestroy()
        {
            if (!_inscrito) return;

            if (player != null) player.AoEntrarNoHexagono -= Coletar;
            if (gerador != null) gerador.AoGerarMapa -= Recomecar;
            if (corredor != null) corredor.AoCompletarVolta -= VoltaFechou;
            _inscrito = false;
        }

        void Update()
        {
            float agora = Time.time;
            foreach (KeyValuePair<HexCoord, Transform> par in _moedas)
            {
                Transform moeda = par.Value;
                if (moeda == null) continue;

                moeda.Rotate(0f, velocidadeDeGiro * Time.deltaTime, 0f, Space.World);

                // Cada moeda flutua numa fase propria, para nao subirem todas juntas.
                float flutuacao = Mathf.Sin(agora * 3f + par.Key.q * 1.7f + par.Key.r) * 0.06f;
                moeda.position = mundo.PosicaoDe(par.Key) + Vector3.up * (alturaDaMoeda + flutuacao);
            }
        }

        void VoltaFechou(int volta) => Sortear();

        /// <summary>Da a cada ilha do anel, fora o acampamento e a casa do Lucca, a chance de ganhar moeda.</summary>
        public void Sortear()
        {
            HexLoopPath loop = gerador.Loop;
            if (loop == null) return;

            for (int i = 0; i < loop.Count; i++)
            {
                HexCoord casa = loop.Casas[i];

                if (casa == mundo.Acampamento) continue;
                if (casa == player.Coord) continue;
                if (_moedas.ContainsKey(casa)) continue;
                if (Random.value >= chancePorIlha) continue;

                Criar(casa);
            }
        }

        void Coletar(HexCoord casa)
        {
            if (!_moedas.TryGetValue(casa, out Transform moeda)) return;

            _moedas.Remove(casa);
            if (moeda != null) Destroy(moeda.gameObject);

            status.GanharMoedas(valorDaMoeda);
        }

        void Recomecar()
        {
            foreach (Transform moeda in _moedas.Values)
            {
                if (moeda != null) Destroy(moeda.gameObject);
            }
            _moedas.Clear();

            Sortear();
        }

        void Criar(HexCoord casa)
        {
            Transform moeda = new GameObject("Moeda " + casa).transform;
            moeda.SetParent(_raiz, false);
            moeda.position = mundo.PosicaoDe(casa) + Vector3.up * alturaDaMoeda;

            // Cilindro achatado e em pe: girando em torno do Y, parece moeda rodando.
            GameObject disco = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Collider colisor = disco.GetComponent<Collider>();
            if (colisor != null) Destroy(colisor);

            disco.name = "Disco";
            disco.transform.SetParent(moeda, false);
            disco.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            disco.transform.localScale = new Vector3(0.32f, 0.03f, 0.32f);

            MeshRenderer renderizador = disco.GetComponent<MeshRenderer>();
            renderizador.sharedMaterial = _material;
            renderizador.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            _moedas[casa] = moeda;
        }

        static Material CriarMaterial(Color cor)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            var material = new Material(shader) { name = "Moeda" };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", cor);
            if (material.HasProperty("_Color")) material.SetColor("_Color", cor);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0.6f);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.6f);
            return material;
        }
    }
}

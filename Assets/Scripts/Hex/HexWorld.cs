using System.Collections.Generic;
using UnityEngine;

namespace Loopia.Hex
{
    /// <summary>
    /// O mundo hexagonal. A grade NAO tem borda: ela e so matematica e vale ate onde a camera
    /// alcancar. O que existe de verdade sao as ilhas, e so elas sao desenhadas — o resto do
    /// plano fica vazio ate o jogador soltar uma carta ali.
    ///
    /// Cada casa esta num de tres estados:
    /// - com ilha: tem um hexagono ali (o anel, o acampamento, ou uma carta ja colocada);
    /// - livre: nao tem nada, mas da para construir;
    /// - bloqueada: o miolo do anel, reservado para o Boss. Nunca pode ser preenchida.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    [DisallowMultipleComponent]
    public class HexWorld : MonoBehaviour
    {
        [Header("Hexagonos")]
        [Tooltip("Distancia do centro do hexagono ate um vertice.")]
        [Min(0.1f)] public float tamanhoDoHexagono = 1f;

        [Tooltip("Folga entre ilhas vizinhas, como fracao do tamanho. So muda o visual, nao a grade.")]
        [Range(0f, 0.4f)] public float folgaEntreHexagonos = 0.06f;

        [Tooltip("Altura da ilha. Valores maiores deixam o visual mais de 'bloco' que de ladrilho.")]
        [Min(0f)] public float espessura = 0.6f;

        [Header("Cores")]
        public Color corDoCaminho = new Color(0.62f, 0.78f, 0.42f);
        public Color corDoAcampamento = new Color(1f, 0.82f, 0.32f);
        public Color corConstruida = new Color(0.55f, 0.70f, 0.85f);
        public Color corHover = new Color(0.85f, 0.95f, 0.65f);

        [Tooltip("Previa do trajeto, usada so pelo controle manual de depuracao.")]
        public Color corDaPrevia = new Color(1f, 0.72f, 0.75f);

        readonly Dictionary<HexCoord, HexTile> _ilhas = new Dictionary<HexCoord, HexTile>();
        readonly HashSet<HexCoord> _bloqueadas = new HashSet<HexCoord>();

        // Reaproveitados pela busca em largura para nao alocar a cada consulta.
        readonly Dictionary<HexCoord, HexCoord> _cameFrom = new Dictionary<HexCoord, HexCoord>();
        readonly Queue<HexCoord> _frontier = new Queue<HexCoord>();

        HexLayout _layout;
        Transform _raiz;
        Mesh _malhaDaIlha;
        Material _materialDaIlha;
        HexPalette _palette;
        bool _preparado;

        /// <summary>Onde fica a ilha do acampamento. E dali que o Lucca comeca e onde a volta fecha.</summary>
        public HexCoord Acampamento { get; private set; }

        public HexLayout Layout => _layout;
        public int TotalDeIlhas => _ilhas.Count;
        public IEnumerable<HexTile> Ilhas => _ilhas.Values;
        public IEnumerable<HexCoord> Bloqueadas => _bloqueadas;

        /// <summary>Plano onde o Lucca anda, usado para converter o ponteiro do mouse em casa.</summary>
        public Plane PlanoDaSuperficie => new Plane(transform.up, transform.position);

        void Awake()
        {
            GarantirPreparado();
        }

        /// <summary>
        /// Monta layout, malha e material uma unica vez. Pode ser chamado por quem precisar do
        /// mundo pronto antes do proprio Awake rodar, ja que a ordem entre Awakes nao e garantida.
        /// </summary>
        public void GarantirPreparado()
        {
            if (_preparado) return;

            _layout = new HexLayout(tamanhoDoHexagono);

            _palette = new HexPalette
            {
                caminho = corDoCaminho,
                acampamento = corDoAcampamento,
                construida = corConstruida,
                hover = corHover,
                previa = corDaPrevia,
            };

            float tamanhoVisual = tamanhoDoHexagono * (1f - folgaEntreHexagonos);
            _malhaDaIlha = HexMesh.Create(tamanhoVisual, espessura);
            _materialDaIlha = CriarMaterial("Ilha", Color.white);

            _raiz = new GameObject("Ilhas").transform;
            _raiz.SetParent(transform, false);

            _preparado = true;
        }

        // --- Ilhas ---

        /// <summary>Cria a ilha, ou so troca o tipo se ja existir uma ali.</summary>
        public HexTile CriarIlha(HexCoord coord, HexTileKind tipo)
        {
            GarantirPreparado();

            HexTile existente;
            if (_ilhas.TryGetValue(coord, out existente))
            {
                existente.DefinirTipo(tipo);
                return existente;
            }

            var go = new GameObject("Ilha");
            go.transform.SetParent(_raiz, false);
            go.transform.localPosition = _layout.ToWorld(coord);

            go.AddComponent<MeshFilter>().sharedMesh = _malhaDaIlha;
            go.AddComponent<MeshRenderer>().sharedMaterial = _materialDaIlha;

            var tile = go.AddComponent<HexTile>();
            tile.Initialize(coord, tipo, _palette);

            _ilhas[coord] = tile;
            if (tipo == HexTileKind.Acampamento) Acampamento = coord;

            return tile;
        }

        public void RemoverIlha(HexCoord coord)
        {
            HexTile tile;
            if (!_ilhas.TryGetValue(coord, out tile)) return;

            _ilhas.Remove(coord);
            if (tile == null) return;

            // Desliga antes de destruir: o Destroy so acontece no fim do frame, e o NavMesh e
            // reassado no mesmo frame em que um mapa novo e gerado. Desligada, a ilha velha
            // fica fora do bake.
            tile.gameObject.SetActive(false);

            if (Application.isPlaying) Destroy(tile.gameObject);
            else DestroyImmediate(tile.gameObject);
        }

        public void LimparMundo()
        {
            var coords = new List<HexCoord>(_ilhas.Keys);
            for (int i = 0; i < coords.Count; i++) RemoverIlha(coords[i]);

            _ilhas.Clear();
            _bloqueadas.Clear();
        }

        public void DefinirAcampamento(HexCoord coord) => Acampamento = coord;

        // --- Miolo bloqueado ---

        public void DefinirBloqueadas(IEnumerable<HexCoord> casas)
        {
            _bloqueadas.Clear();
            if (casas == null) return;

            foreach (HexCoord casa in casas) _bloqueadas.Add(casa);
        }

        /// <summary>Miolo do anel: reservado para o Boss, nunca pode receber ilha.</summary>
        public bool EstaBloqueada(HexCoord coord) => _bloqueadas.Contains(coord);

        // --- Consultas ---

        public bool TemIlha(HexCoord coord) => _ilhas.ContainsKey(coord);

        public bool TryGetIlha(HexCoord coord, out HexTile tile) => _ilhas.TryGetValue(coord, out tile);

        /// <summary>Casa vazia e liberada: e aqui que uma carta pode ser solta.</summary>
        public bool PodeConstruir(HexCoord coord) => !TemIlha(coord) && !EstaBloqueada(coord);

        public bool IsWalkable(HexCoord coord)
        {
            HexTile tile;
            return _ilhas.TryGetValue(coord, out tile) && tile.Walkable;
        }

        /// <summary>Centro da casa em coordenadas de mundo. Vale para qualquer casa, tendo ilha ou nao.</summary>
        public Vector3 PosicaoDe(HexCoord coord) => transform.TransformPoint(_layout.ToWorld(coord));

        /// <summary>Casa que contem a posicao informada. A grade nao tem borda, entao isso sempre responde.</summary>
        public HexCoord MundoParaHex(Vector3 mundo) => _layout.ToHex(transform.InverseTransformPoint(mundo));

        /// <summary>Ilha existente sob a posicao de mundo. False se ali nao tem nada.</summary>
        public bool TryIlhaEmMundo(Vector3 mundo, out HexTile tile)
        {
            return _ilhas.TryGetValue(MundoParaHex(mundo), out tile);
        }

        public void LimparDestaques()
        {
            foreach (HexTile tile in _ilhas.Values) tile.ClearHighlights();
        }

        // --- Caminho ---

        /// <summary>
        /// Busca em largura pelo caminho mais curto entre duas ilhas, andando so por ilhas que existem.
        /// O resultado NAO inclui a origem e inclui o destino. False se nao houver caminho.
        /// </summary>
        public bool TryFindPath(HexCoord from, HexCoord to, List<HexCoord> resultado)
        {
            resultado.Clear();

            if (from == to) return true;
            if (!TemIlha(from) || !IsWalkable(to)) return false;

            _cameFrom.Clear();
            _frontier.Clear();

            _frontier.Enqueue(from);
            _cameFrom[from] = from;

            bool achou = false;
            while (_frontier.Count > 0)
            {
                HexCoord atual = _frontier.Dequeue();
                if (atual == to)
                {
                    achou = true;
                    break;
                }

                for (int i = 0; i < 6; i++)
                {
                    HexCoord vizinho = atual.Neighbor(i);
                    if (_cameFrom.ContainsKey(vizinho)) continue;
                    if (!IsWalkable(vizinho)) continue;

                    _cameFrom[vizinho] = atual;
                    _frontier.Enqueue(vizinho);
                }
            }

            if (!achou) return false;

            HexCoord passo = to;
            while (passo != from)
            {
                resultado.Add(passo);
                passo = _cameFrom[passo];
            }
            resultado.Reverse();
            return true;
        }

        // --- Material ---

        static Material CriarMaterial(string nome, Color cor)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Sprites/Default");

            var material = new Material(shader) { name = nome };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", cor);
            if (material.HasProperty("_Color")) material.SetColor("_Color", cor);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.05f);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0.05f);
            return material;
        }
    }
}

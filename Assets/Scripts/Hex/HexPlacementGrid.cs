using System.Collections.Generic;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Loopia.Hex
{
    /// <summary>
    /// A grade que so aparece enquanto o jogador esta arrastando uma carta. Ela nao e feita de
    /// ilhas: e so o contorno das casas, desenhado ate onde a camera alcanca, mostrando onde da
    /// para soltar a carta (livre) e onde nunca vai dar (o miolo do anel, reservado ao Boss).
    ///
    /// O sistema de cartas ainda nao existe, entao por enquanto ela e ligada por uma tecla.
    /// Quando as cartas chegarem, e so chamar Mostrar() ao pegar a carta e Esconder() ao soltar.
    /// </summary>
    [DisallowMultipleComponent]
    public class HexPlacementGrid : MonoBehaviour
    {
        [Header("Referencias")]
        [Tooltip("Deixe vazio para achar automaticamente na cena.")]
        public HexWorld mundo;

        [Tooltip("Deixe vazio para usar a Camera.main.")]
        public Camera camera3d;

        [Header("Cores")]
        [Tooltip("Casa vazia onde da para soltar a carta.")]
        public Color corLivre = new Color(1f, 1f, 1f, 0.28f);

        [Tooltip("Miolo do anel: reservado para o Boss, nunca recebe ilha.")]
        public Color corBloqueada = new Color(1f, 0.35f, 0.35f, 0.35f);

        public bool mostrarBloqueadas = true;

        [Header("Limites")]
        [Tooltip("Teto de casas desenhadas por vez, para a grade nunca pesar se a camera abrir muito.")]
        [Min(64)] public int maximoDeCasas = 4000;

        [Header("Depuracao")]
        [Tooltip("Enquanto nao existem cartas, segure esta tecla para espiar a grade.")]
        public bool teclaDeDebugLigada = true;

        public bool Visivel { get; private set; }

        // Quem pediu a grade: o jogo (arraste de carta) ou a tecla de depuracao.
        bool _pedidaPeloJogo;

        readonly List<Vector3> _vertices = new List<Vector3>();
        readonly List<int> _indices = new List<int>();

        Transform _raiz;
        Mesh _malhaLivre;
        Mesh _malhaBloqueada;

        // Estado do ultimo desenho, para nao refazer a malha a cada frame a toa.
        Vector3 _ultimaPosicaoDaCamera;
        Quaternion _ultimaRotacaoDaCamera;
        float _ultimoZoom = -1f;
        int _ultimoTotalDeIlhas = -1;

        void Start()
        {
            if (mundo == null) mundo = FindFirstObjectByType<HexWorld>();
            if (camera3d == null) camera3d = Camera.main;

            if (mundo == null || camera3d == null)
            {
                Debug.LogError("[HexPlacementGrid] Precisa de um HexWorld e de uma Camera na cena.", this);
                enabled = false;
                return;
            }

            Montar();
            Aplicar(false);
        }

        void Update()
        {
            bool espiando = teclaDeDebugLigada && TeclaDeDebugSegurada();
            Aplicar(_pedidaPeloJogo || espiando);

            if (!Visivel) return;
            if (PrecisaRedesenhar()) Redesenhar();
        }

        /// <summary>Chame ao pegar uma carta.</summary>
        public void Mostrar()
        {
            _pedidaPeloJogo = true;
            Aplicar(true);
            Redesenhar();
        }

        /// <summary>Chame ao soltar a carta.</summary>
        public void Esconder() => _pedidaPeloJogo = false;

        void Aplicar(bool visivel)
        {
            if (Visivel == visivel) return;

            Visivel = visivel;
            if (_raiz != null) _raiz.gameObject.SetActive(visivel);
        }

        void Montar()
        {
            // Fica sob o mundo porque a malha e construida no espaco local dele. Por isso mesmo
            // precisa ser ignorada pelo bake do NavMesh, que coleta os filhos do Mundo.
            _raiz = new GameObject("Grade de colocacao").transform;
            _raiz.SetParent(mundo.transform, false);
            _raiz.gameObject.AddComponent<Unity.AI.Navigation.NavMeshModifier>().ignoreFromBuild = true;

            _malhaLivre = NovaMalha("GradeLivre");
            _malhaBloqueada = NovaMalha("GradeBloqueada");

            NovoRenderizador("Livre", _malhaLivre, corLivre);
            NovoRenderizador("Bloqueada", _malhaBloqueada, corBloqueada);
        }

        static Mesh NovaMalha(string nome)
        {
            var malha = new Mesh { name = nome };
            malha.MarkDynamic();
            return malha;
        }

        void NovoRenderizador(string nome, Mesh malha, Color cor)
        {
            var go = new GameObject(nome);
            go.transform.SetParent(_raiz, false);

            go.AddComponent<MeshFilter>().sharedMesh = malha;

            var renderizador = go.AddComponent<MeshRenderer>();
            renderizador.sharedMaterial = CriarMaterialDeLinha(nome, cor);
            renderizador.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderizador.receiveShadows = false;
        }

        static Material CriarMaterialDeLinha(string nome, Color cor)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null) shader = Shader.Find("Sprites/Default");

            var material = new Material(shader) { name = nome };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", cor);
            if (material.HasProperty("_Color")) material.SetColor("_Color", cor);

            // Transparencia, para a grade nao tapar as ilhas.
            if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

            return material;
        }

        bool PrecisaRedesenhar()
        {
            Transform cam = camera3d.transform;

            bool mudou =
                cam.position != _ultimaPosicaoDaCamera ||
                cam.rotation != _ultimaRotacaoDaCamera ||
                !Mathf.Approximately(camera3d.orthographicSize, _ultimoZoom) ||
                mundo.TotalDeIlhas != _ultimoTotalDeIlhas;

            if (!mudou) return false;

            _ultimaPosicaoDaCamera = cam.position;
            _ultimaRotacaoDaCamera = cam.rotation;
            _ultimoZoom = camera3d.orthographicSize;
            _ultimoTotalDeIlhas = mundo.TotalDeIlhas;
            return true;
        }

        void Redesenhar()
        {
            HexCoord min, max;
            if (!FaixaVisivel(out min, out max))
            {
                _malhaLivre.Clear();
                _malhaBloqueada.Clear();
                return;
            }

            DesenharFaixa(min, max, false, _malhaLivre);
            if (mostrarBloqueadas) DesenharFaixa(min, max, true, _malhaBloqueada);
            else _malhaBloqueada.Clear();
        }

        /// <summary>
        /// Projeta os quatro cantos da tela no plano do mundo e devolve a caixa de coordenadas
        /// axiais que cobre o que a camera esta vendo. E isso que faz a grade ir "ate onde a
        /// camera alcanca" sem a grade precisar ter tamanho nenhum.
        /// </summary>
        bool FaixaVisivel(out HexCoord min, out HexCoord max)
        {
            min = default(HexCoord);
            max = default(HexCoord);

            Plane plano = mundo.PlanoDaSuperficie;
            var cantosDaTela = new Vector2[]
            {
                new Vector2(0f, 0f),
                new Vector2(Screen.width, 0f),
                new Vector2(0f, Screen.height),
                new Vector2(Screen.width, Screen.height),
            };

            int minQ = int.MaxValue, maxQ = int.MinValue;
            int minR = int.MaxValue, maxR = int.MinValue;

            for (int i = 0; i < cantosDaTela.Length; i++)
            {
                Ray raio = camera3d.ScreenPointToRay(cantosDaTela[i]);

                float distancia;
                if (!plano.Raycast(raio, out distancia)) return false;

                HexCoord casa = mundo.MundoParaHex(raio.GetPoint(distancia));
                minQ = Mathf.Min(minQ, casa.q);
                maxQ = Mathf.Max(maxQ, casa.q);
                minR = Mathf.Min(minR, casa.r);
                maxR = Mathf.Max(maxR, casa.r);
            }

            // Os cantos da tela dao um losango na grade, entao a caixa precisa de folga.
            const int folga = 2;
            min = new HexCoord(minQ - folga, minR - folga);
            max = new HexCoord(maxQ + folga, maxR + folga);
            return true;
        }

        void DesenharFaixa(HexCoord min, HexCoord max, bool bloqueadas, Mesh destino)
        {
            _vertices.Clear();
            _indices.Clear();

            HexLayout layout = mundo.Layout;

            // Contorno um pouco menor que a casa, pela mesma razao das ilhas terem folga:
            // encostado na aresta da ilha vizinha, o contorno briga por profundidade com ela.
            var contorno = new HexLayout(mundo.tamanhoDoHexagono * (1f - mundo.folgaEntreHexagonos));
            int desenhadas = 0;

            for (int r = min.r; r <= max.r && desenhadas < maximoDeCasas; r++)
            {
                for (int q = min.q; q <= max.q && desenhadas < maximoDeCasas; q++)
                {
                    var casa = new HexCoord(q, r);

                    if (mundo.TemIlha(casa)) continue;
                    if (mundo.EstaBloqueada(casa) != bloqueadas) continue;
                    if (!EstaNaTela(casa)) continue;

                    AcrescentarContorno(layout, contorno, casa);
                    desenhadas++;
                }
            }

            destino.Clear();
            if (_vertices.Count == 0) return;

            destino.SetVertices(_vertices);
            destino.SetIndices(_indices, MeshTopology.Lines, 0);
            destino.RecalculateBounds();
        }

        bool EstaNaTela(HexCoord casa)
        {
            Vector3 naTela = camera3d.WorldToScreenPoint(mundo.PosicaoDe(casa));
            if (naTela.z <= 0f) return false;

            // Margem de um hexagono, para nao cortar as casas da borda da tela.
            const float margem = 64f;
            return naTela.x >= -margem && naTela.x <= Screen.width + margem
                && naTela.y >= -margem && naTela.y <= Screen.height + margem;
        }

        void AcrescentarContorno(HexLayout posicoes, HexLayout contorno, HexCoord casa)
        {
            // Um tiquinho abaixo da superficie, para a ilha sempre ganhar de qualquer empate.
            Vector3 centro = posicoes.ToWorld(casa) + Vector3.down * 0.01f;
            int baseIndice = _vertices.Count;

            for (int i = 0; i < 6; i++)
            {
                _vertices.Add(centro + contorno.Corner(i));
            }

            for (int i = 0; i < 6; i++)
            {
                _indices.Add(baseIndice + i);
                _indices.Add(baseIndice + (i + 1) % 6);
            }
        }

        static bool TeclaDeDebugSegurada()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.tabKey.isPressed;
#else
            return Input.GetKey(KeyCode.Tab);
#endif
        }
    }
}

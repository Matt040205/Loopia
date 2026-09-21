using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Loopia.Hex
{
    /// <summary>
    /// A barra de cartas e o arraste, conforme os Controles do GDD: "se o jogador escolher uma
    /// carta de plataforma e foi arrastada ate uma area disponivel e aplicavel, solta ela, sera
    /// instanciada".
    ///
    /// Enquanto o jogador segura a carta, a grade de colocacao aparece e um marcador mostra a
    /// casa sob o mouse: verde quando da para soltar, vermelho quando nao da, e o motivo aparece
    /// escrito. Quem decide se pode e sempre o MapaDePlataformas — aqui so se pergunta.
    ///
    /// Isto e IMGUI de prototipo, para a mecanica poder ser jogada antes da HUD de verdade.
    /// </summary>
    [DisallowMultipleComponent]
    public class CartasUI : MonoBehaviour
    {
        [Header("Referencias")]
        [Tooltip("Deixe vazio para achar automaticamente na cena.")]
        public MaoDeCartas mao;
        public MapaDePlataformas mapa;
        public HexWorld mundo;
        public HexPlacementGrid grade;

        [Tooltip("Deixe vazio para usar a Camera.main.")]
        public Camera camera3d;

        [Header("Cores do marcador")]
        public Color corPodeColocar = new Color(0.35f, 1f, 0.45f, 0.75f);
        public Color corNaoPode = new Color(1f, 0.35f, 0.35f, 0.7f);

        int _espacoArrastado = -1;
        int _espacoSobOMouse = -1;

        HexCoord _casaSobOMouse;
        bool _casaValida;
        bool _podeSoltarAqui;
        string _motivo;

        Transform _marcador;
        MeshRenderer _marcadorRenderer;
        MaterialPropertyBlock _bloco;

        GUIStyle _estiloCarta;
        GUIStyle _estiloTexto;

        /// <summary>A carta que o jogador esta segurando agora, ou null.</summary>
        public PlataformaDef CartaArrastada =>
            _espacoArrastado >= 0 && mao != null ? mao.CartaNoEspaco(_espacoArrastado) : null;

        void Start()
        {
            if (mao == null) mao = FindFirstObjectByType<MaoDeCartas>();
            if (mapa == null) mapa = FindFirstObjectByType<MapaDePlataformas>();
            if (mundo == null) mundo = FindFirstObjectByType<HexWorld>();
            if (grade == null) grade = FindFirstObjectByType<HexPlacementGrid>();
            if (camera3d == null) camera3d = Camera.main;

            if (mao == null || mapa == null || mundo == null || camera3d == null)
            {
                Debug.LogError("[CartasUI] Faltou referencia: precisa de MaoDeCartas, " +
                               "MapaDePlataformas, HexWorld e uma Camera.", this);
                enabled = false;
                return;
            }

            MontarMarcador();
        }

        bool LuccaMorreu => mao != null && mao.status != null && !mao.status.EstaVivo;

        void Update()
        {
            if (LuccaMorreu)
            {
                if (_espacoArrastado >= 0) CancelarArraste();
                return;
            }

            if (CartaArrastada == null)
            {
                if (_espacoArrastado >= 0) CancelarArraste();
                return;
            }

            AtualizarCasaSobOMouse();
            AtualizarMarcador();

            if (BotaoEsquerdoSolto()) Soltar();
        }

        // --- Arraste ---

        void ComecarArraste(int espaco)
        {
            _espacoArrastado = espaco;
            if (grade != null) grade.Mostrar();
        }

        void CancelarArraste()
        {
            _espacoArrastado = -1;
            if (grade != null) grade.Esconder();
            if (_marcador != null) _marcador.gameObject.SetActive(false);
        }

        void Soltar()
        {
            PlataformaDef carta = CartaArrastada;

            if (carta != null && _casaValida && _podeSoltarAqui)
            {
                if (mapa.Colocar(carta, _casaSobOMouse) != null) mao.Gastar(_espacoArrastado);
            }

            CancelarArraste();
        }

        void AtualizarCasaSobOMouse()
        {
            _casaValida = false;
            _podeSoltarAqui = false;
            _motivo = null;

            if (!TryPosicaoDoMouse(out Vector3 naTela)) return;

            Ray raio = camera3d.ScreenPointToRay(naTela);
            if (!mundo.PlanoDaSuperficie.Raycast(raio, out float distancia)) return;

            _casaSobOMouse = mundo.MundoParaHex(raio.GetPoint(distancia));
            _casaValida = true;
            _podeSoltarAqui = mapa.PodeColocar(CartaArrastada, _casaSobOMouse, out _motivo);
        }

        // --- Marcador da casa ---

        void MontarMarcador()
        {
            var go = new GameObject("Marcador de colocacao");
            go.transform.SetParent(mundo.transform, false);

            // Fica embaixo do Mundo, que e onde o NavMesh e assado: sem isto, o marcador
            // viraria chao andavel no proximo bake.
            go.AddComponent<Unity.AI.Navigation.NavMeshModifier>().ignoreFromBuild = true;

            go.AddComponent<MeshFilter>().sharedMesh =
                HexMesh.Create(mundo.tamanhoDoHexagono * (1f - mundo.folgaEntreHexagonos), 0f);

            _marcadorRenderer = go.AddComponent<MeshRenderer>();
            _marcadorRenderer.sharedMaterial = CriarMaterialTransparente();
            _marcadorRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _marcadorRenderer.receiveShadows = false;

            _bloco = new MaterialPropertyBlock();
            _marcador = go.transform;
            go.SetActive(false);
        }

        void AtualizarMarcador()
        {
            if (_marcador == null) return;

            if (!_casaValida)
            {
                _marcador.gameObject.SetActive(false);
                return;
            }

            _marcador.gameObject.SetActive(true);

            // Um pouco acima da superficie, senao briga por profundidade com a ilha embaixo.
            _marcador.localPosition = mundo.Layout.ToWorld(_casaSobOMouse) + Vector3.up * 0.05f;

            Color cor = _podeSoltarAqui ? corPodeColocar : corNaoPode;
            _marcadorRenderer.GetPropertyBlock(_bloco);
            _bloco.SetColor("_BaseColor", cor);
            _bloco.SetColor("_Color", cor);
            _marcadorRenderer.SetPropertyBlock(_bloco);
        }

        static Material CriarMaterialTransparente()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null) shader = Shader.Find("Sprites/Default");

            var material = new Material(shader) { name = "MarcadorDeColocacao" };
            if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            return material;
        }

        // --- Barra de cartas ---

        void OnGUI()
        {
            // IMGUI desenha por cima de qualquer Canvas: no Game Over sai da frente da tela de morte.
            if (mao == null || LuccaMorreu) return;

            if (_estiloCarta == null)
            {
                _estiloCarta = new GUIStyle(GUI.skin.box)
                {
                    fontSize = 10,
                    wordWrap = true,
                    alignment = TextAnchor.UpperCenter,
                };
                _estiloTexto = new GUIStyle(GUI.skin.label) { fontSize = 10, wordWrap = true };
            }

            DesenharBarra();
            DesenharDescricao();
        }

        void DesenharBarra()
        {
            int espacos = mao.Mao.Count;

            float alturaDaCarta = Mathf.Min(96f, Screen.height * 0.22f);
            float larguraDaCarta = alturaDaCarta * 0.86f;
            const float folga = 6f;

            float larguraTotal = espacos * larguraDaCarta + (espacos - 1) * folga;
            float x = (Screen.width - larguraTotal) * 0.5f;
            float y = Screen.height - alturaDaCarta - 8f;

            _espacoSobOMouse = -1;

            for (int i = 0; i < espacos; i++)
            {
                var area = new Rect(x + i * (larguraDaCarta + folga), y, larguraDaCarta, alturaDaCarta);
                PlataformaDef carta = mao.CartaNoEspaco(i);

                if (area.Contains(Event.current.mousePosition)) _espacoSobOMouse = i;

                Color guardada = GUI.color;
                if (carta != null) GUI.color = Color.Lerp(carta.cor, Color.white, 0.35f);
                GUI.Box(area, carta != null ? carta.nomeExibido : "", _estiloCarta);
                GUI.color = guardada;

                if (carta == null) continue;

                // Pegar a carta: o GDD manda usar o botao esquerdo.
                if (Event.current.type == EventType.MouseDown && Event.current.button == 0 &&
                    area.Contains(Event.current.mousePosition))
                {
                    ComecarArraste(i);
                    Event.current.Use();
                }
            }
        }

        /// <summary>
        /// GDD (HUD): "ao jogador colocar o mouse em cima da carta, ou da plataforma colocada,
        /// um quadrado ira aparecer no canto inferior direito da tela".
        /// </summary>
        void DesenharDescricao()
        {
            string titulo = null;
            string corpo = null;

            PlataformaDef carta = _espacoSobOMouse >= 0 ? mao.CartaNoEspaco(_espacoSobOMouse) : null;

            if (CartaArrastada != null)
            {
                titulo = CartaArrastada.nomeExibido;
                corpo = _podeSoltarAqui
                    ? "Solte para construir em " + _casaSobOMouse
                    : "Nao da: " + (_motivo ?? "fora do mapa");
            }
            else if (carta != null)
            {
                titulo = carta.nomeExibido;
                corpo = carta.descricao;
            }
            else if (mapa != null && ApontandoPlataforma(out Plataforma apontada))
            {
                titulo = apontada.Def.nomeExibido;
                corpo = apontada.Def.descricao;
            }

            if (titulo == null) return;

            const float largura = 210f;
            const float altura = 86f;
            var area = new Rect(Screen.width - largura - 12f, Screen.height - altura - 12f, largura, altura);

            GUILayout.BeginArea(area, GUI.skin.box);
            GUILayout.Label(titulo, _estiloCarta);
            GUILayout.Label(corpo, _estiloTexto);
            GUILayout.EndArea();
        }

        bool ApontandoPlataforma(out Plataforma plataforma)
        {
            plataforma = null;

            if (!TryPosicaoDoMouse(out Vector3 naTela)) return false;

            Ray raio = camera3d.ScreenPointToRay(naTela);
            if (!mundo.PlanoDaSuperficie.Raycast(raio, out float distancia)) return false;

            return mapa.TryGet(mundo.MundoParaHex(raio.GetPoint(distancia)), out plataforma)
                   && plataforma != null;
        }

        // --- Entrada ---

        static bool BotaoEsquerdoSolto()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame;
#else
            return Input.GetMouseButtonUp(0);
#endif
        }

        static bool TryPosicaoDoMouse(out Vector3 posicao)
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current == null)
            {
                posicao = default;
                return false;
            }
            Vector2 leitura = Mouse.current.position.ReadValue();
            posicao = new Vector3(leitura.x, leitura.y, 0f);
            return true;
#else
            posicao = Input.mousePosition;
            return true;
#endif
        }
    }
}

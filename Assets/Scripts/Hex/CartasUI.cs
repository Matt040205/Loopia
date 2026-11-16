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
    /// A apresentacao Canvas e opcional. Na sua ausencia, a barra IMGUI continua disponivel.
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

        HudDeCartas _hud;
        Mesh _malhaMarcador;
        Material _materialMarcador;

        GUIStyle _estiloCarta;
        GUIStyle _estiloTexto;

        string _textoPendentes;
        int _pendentesNoTexto = -1;

        // Tamanho logico da tela no OnGUI (Screen dividido pelo fator da EscalaDaInterface).
        float _larguraDaTela;
        float _alturaDaTela;

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

        /// <summary>Lucca morto ou jornada encerrada (vitoria/derrota): sem arraste e sem barra de cartas.</summary>
        bool JornadaEncerrada => LuccaMorreu || (mapa != null && mapa.diretor != null && mapa.diretor.Terminou);

        internal bool AtivarHud(HudDeCartas hud)
        {
            if (!isActiveAndEnabled || _hud != null || mao == null || mundo == null || !hud.Preparar(mao, mundo))
                return false;
            CancelarArraste();
            _hud = hud;
            return true;
        }

        internal void DesativarHud(HudDeCartas hud)
        {
            if (_hud != hud) return;
            CancelarArraste();
            _hud = null;
        }

        void OnDisable()
        {
            CancelarArraste();
            if (_hud != null) _hud.Visivel(false);
        }

        void OnDestroy()
        {
            if (_marcador != null) Destroy(_marcador.gameObject);
            Destroy(_malhaMarcador);
            Destroy(_materialMarcador);
        }

        void OnApplicationFocus(bool temFoco)
        {
            if (temFoco) return;
            if (_espacoArrastado >= 0) CancelarArraste();
            if (_hud != null) _hud.LimparTransitorios();
        }

        void Update()
        {
            bool encerrada = JornadaEncerrada;
            if (_hud != null) _hud.Visivel(!encerrada);
            if (encerrada)
            {
                if (_espacoArrastado >= 0) CancelarArraste();
                return;
            }

            bool temPonteiro = TryPosicaoDoMouse(out Vector3 ponteiro);
            float dt = Time.unscaledDeltaTime;

            if (_hud != null && temPonteiro)
            {
                Plataforma apontada = null;
                if (_espacoArrastado < 0 && !_hud.SobreAMao(ponteiro))
                    ApontandoPlataforma(out apontada);
                _hud.Observar(ponteiro, apontada, dt);

                if (_espacoArrastado < 0 && BotaoEsquerdoPressionado())
                {
                    int espaco = _hud.EspacoSobCursor(ponteiro);
                    var carta = mao.CartaNoEspaco(espaco);
                    if (carta != null)
                    {
                        ComecarArraste(espaco);
                        _hud.IniciarArraste(espaco, carta, ponteiro);
                    }
                }
            }

            if (CartaArrastada == null || !temPonteiro)
            {
                if (_espacoArrastado >= 0) CancelarArraste();
                return;
            }

            AtualizarCasaSobOMouse(ponteiro);
            AtualizarMarcador();
            if (_hud != null)
            {
                string retorno = _podeSoltarAqui ? "Solte para construir"
                    : _motivo ?? "Aponte para uma casa do tabuleiro";
                _hud.Arrastando(ponteiro, _podeSoltarAqui, retorno, RaioDaCasaNaTela(), dt);
            }

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
            if (_hud != null) _hud.EncerrarArraste();
            if (grade != null) grade.Esconder();
            if (_marcador != null) _marcador.gameObject.SetActive(false);
        }

        void Soltar()
        {
            PlataformaDef carta = CartaArrastada;

            if (carta != null && _casaValida && _podeSoltarAqui)
            {
                if (mapa.Colocar(carta, _casaSobOMouse) != null && mao.Gastar(_espacoArrastado) && _hud != null)
                    _hud.Consumir(carta, _casaSobOMouse);
            }

            CancelarArraste();
        }

        void AtualizarCasaSobOMouse(Vector3 naTela)
        {
            _casaValida = false;
            _podeSoltarAqui = false;
            _motivo = null;

            if (_hud != null && _hud.SobreAMao(naTela))
            {
                _motivo = "Aponte para fora da mão";
                return;
            }

            Ray raio = camera3d.ScreenPointToRay(naTela);
            if (!mundo.TryCasaNoRaio(raio, out _casaSobOMouse)) return;
            _casaValida = true;
            _podeSoltarAqui = mapa.PodeColocar(CartaArrastada, _casaSobOMouse, out _motivo);
        }

        /// <summary>Raio (px) da casa sob o cursor na tela, para o fantasma da carta nao cobrir o alvo.</summary>
        float RaioDaCasaNaTela()
        {
            if (!_casaValida) return 0f;
            Vector3 centro = mundo.PosicaoDe(_casaSobOMouse);
            Vector3 a = camera3d.WorldToScreenPoint(centro);
            Vector3 b = camera3d.WorldToScreenPoint(centro + camera3d.transform.right * mundo.tamanhoDoHexagono);
            Vector3 c = camera3d.WorldToScreenPoint(centro + Vector3.ProjectOnPlane(camera3d.transform.up, Vector3.up).normalized * mundo.tamanhoDoHexagono);
            if (a.z <= 0f) return 0f;
            return Mathf.Max(Vector2.Distance(a, b), Vector2.Distance(a, c));
        }

        // --- Marcador da casa ---

        void MontarMarcador()
        {
            var go = new GameObject("Marcador de colocacao");
            go.transform.SetParent(mundo.transform, false);

            // Fica embaixo do Mundo, que e onde o NavMesh e assado: sem isto, o marcador
            // viraria chao andavel no proximo bake.
            go.AddComponent<Unity.AI.Navigation.NavMeshModifier>().ignoreFromBuild = true;

            _malhaMarcador = HexMesh.Create(mundo.tamanhoDoHexagono * (1f - mundo.folgaEntreHexagonos), 0f);
            go.AddComponent<MeshFilter>().sharedMesh = _malhaMarcador;

            _materialMarcador = CriarMaterialTransparente();
            _marcadorRenderer = go.AddComponent<MeshRenderer>();
            _marcadorRenderer.sharedMaterial = _materialMarcador;
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
            _marcador.position = mundo.PosicaoDe(_casaSobOMouse) + mundo.transform.up * 0.05f;

            Color cor = _hud == null
                ? (_podeSoltarAqui ? corPodeColocar : corNaoPode)
                : (_podeSoltarAqui ? new Color(.43f, .89f, .69f, .40f) : new Color(.95f, .44f, .42f, .36f));
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
            // IMGUI desenha por cima de qualquer Canvas: no fim da jornada sai da frente da tela de resultado.
            if (_hud != null || mao == null || JornadaEncerrada) return;

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

            // Telas acima de 1080p: tudo escala junto (EscalaDaInterface); o layout usa o tamanho logico da tela.
            Matrix4x4 anterior = EscalaDaInterface.Aplicar(out _larguraDaTela, out _alturaDaTela);
            try
            {
                DesenharBarra();
                DesenharDescricao();
            }
            finally
            {
                GUI.matrix = anterior;
            }
        }

        void DesenharBarra()
        {
            int espacos = mao.Mao.Count;

            float alturaDaCarta = Mathf.Min(96f, _alturaDaTela * 0.22f);
            float larguraDaCarta = alturaDaCarta * 0.86f;
            const float folga = 6f;

            float larguraTotal = espacos * larguraDaCarta + (espacos - 1) * folga;
            float x = (_larguraDaTela - larguraTotal) * 0.5f;
            float y = _alturaDaTela - alturaDaCarta - 8f;

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

            DesenharPendentes(x + larguraTotal, y);
        }

        /// <summary>
        /// G2: "+N pendentes" logo acima do canto direito da barra, fora da area clicavel das cartas.
        /// So um aviso: a pendente nao vira carta arrastavel ate ocupar um espaco da mao.
        /// </summary>
        void DesenharPendentes(float direitaDaBarra, float topoDaBarra)
        {
            int pendentes = mao.Pendentes;
            if (pendentes <= 0) return;

            // Texto em cache: OnGUI roda varias vezes por frame e nao deve alocar string a cada chamada.
            if (pendentes != _pendentesNoTexto || _textoPendentes == null)
            {
                _pendentesNoTexto = pendentes;
                _textoPendentes = "+" + pendentes + (pendentes == 1 ? " pendente" : " pendentes");
            }

            const float largura = 92f;
            const float altura = 20f;
            var area = new Rect(direitaDaBarra - largura, topoDaBarra - altura - 4f, largura, altura);
            GUI.Box(area, _textoPendentes, _estiloCarta);
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
                    ? "Solte para construir"
                    : TextoDoJogador.Motivo(_motivo ?? "fora do mapa");
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
            var area = new Rect(_larguraDaTela - largura - 12f, _alturaDaTela - altura - 12f, largura, altura);

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
            if (!mundo.TryCasaNoRaio(raio, out HexCoord casa)) return false;

            return mapa.TryGet(casa, out plataforma)
                   && plataforma != null;
        }

        // --- Entrada ---

        static bool BotaoEsquerdoPressionado()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
            return Input.GetMouseButtonDown(0);
#endif
        }

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

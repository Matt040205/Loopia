using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Loopia.Hex
{
    /// <summary>
    /// Camera isometrica nos valores que o time definiu: ortografica, inclinacao 30 e giro 45.
    /// Por padrao ela mede o anel gerado e se afasta o suficiente para mostrar a volta inteira;
    /// desligando o enquadramento automatico, o zoom passa a ser o valor manual.
    ///
    /// Zoom do jogador (Sprint 4): a roda do mouse aproxima ate <see cref="menorMeiaAltura"/>, mantendo parado o
    /// ponto do mundo sob o cursor, e o botao direito (ou o do meio) arrasta a vista. Afastado ao maximo volta a ser
    /// o anel inteiro, que continua sendo o padrao. A camera so anda no proprio plano e a distancia ate o anel nao
    /// muda: nevoa, sombra e o enquadramento-base (testes do Pass A) ficam iguais. A vista aproximada nunca sai do
    /// retangulo do anel inteiro.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Camera))]
    [DisallowMultipleComponent]
    public class HexIsoCamera : MonoBehaviour
    {
        [Header("Alvo")]
        [Tooltip("Deixe vazio para achar o HexWorld da cena automaticamente.")]
        public HexWorld mundo;

        [Header("Angulo")]
        [Tooltip("Quanto a camera olha para baixo. O time definiu 30.")]
        [Range(10f, 89f)] public float inclinacao = 30f;

        [Tooltip("Rotacao em torno do mapa. O time definiu 45.")]
        public float giro = 45f;

        [Header("Enquadramento")]
        public bool ortografica = true;

        [Tooltip("Ligado: mede o anel e mostra a volta inteira. Desligado: usa o zoom manual.")]
        public bool enquadrarAutomaticamente = true;

        [Tooltip("Folga em volta do anel. 1 = colado nas bordas.")]
        [Range(1f, 2f)] public float margem = 1.15f;

        [Tooltip("Zoom usado quando o enquadramento automatico esta desligado.")]
        [Min(0.5f)] public float zoomManual = 5f;

        [Tooltip("Distancia da camera ate o centro. So muda a projecao em perspectiva.")]
        [Min(1f)] public float distancia = 40f;

        [Header("Zoom do jogador")]
        [Tooltip("Roda do mouse aproxima/afasta; botao direito ou do meio arrasta a vista. So com camera ortografica, em Play.")]
        public bool permitirZoom = true;

        [Tooltip("Menor meia-altura da vista (orthographicSize) ao aproximar, em unidades de mundo. " +
                 "Se o anel inteiro ja cabe nisso, nao ha zoom.")]
        [Min(1f)] public float menorMeiaAltura = 7f;

        [Tooltip("Cada passo da roda multiplica a meia-altura por isto ao aproximar (e divide ao afastar).")]
        [Range(0.5f, 0.95f)] public float passoDoZoom = 0.85f;

        [Tooltip("Tempo, em segundos de tempo real, para o zoom chegar perto do alvo. 0 = instantaneo.")]
        [Min(0f)] public float suavizacao = 0.08f;

        [Tooltip("Mostra no canto inferior esquerdo como usar o zoom, ate o jogador usar pela primeira vez.")]
        public bool mostrarDica = true;

        Camera _camera;
        float _proximoEnquadramento;

        // Enquadramento-base (anel inteiro), medido em Enquadrar().
        Vector3 _centroBase;
        Quaternion _rotacao = Quaternion.identity;
        float _meiaAlturaBase = -1f;
        float _distanciaReal;

        // Zoom: fracao da meia-altura base (1 = anel inteiro) e deslocamento no plano da camera (x = direita, y = cima).
        float _zoom = 1f, _zoomAlvo = 1f;
        Vector2 _desloc, _deslocAlvo;

        bool _arrastando;
        Vector2 _ultimoMouse;
        float _rodaAcumulada;
        bool _usouZoom;
        LoopDirector _diretor;
        GUIStyle _estiloDaDica;

        Camera Cam
        {
            get
            {
                if (_camera == null) _camera = GetComponent<Camera>();
                return _camera;
            }
        }

        float Aspecto => Cam != null && Cam.aspect > 0.01f ? Cam.aspect : 16f / 9f;

        bool ZoomAtivo => ortografica && permitirZoom && _meiaAlturaBase > 0f;

        bool JornadaEncerrada => _diretor != null && _diretor.Terminou;

        /// <summary>Fracao atual da meia-altura base: 1 = anel inteiro; menor = mais perto.</summary>
        public float Zoom => _zoom;

        /// <summary>Para onde o zoom esta indo (a suavizacao leva <see cref="Zoom"/> ate aqui).</summary>
        public float ZoomAlvo => _zoomAlvo;

        /// <summary>Deslocamento atual da vista no plano da camera, em unidades de mundo (x = direita, y = cima).</summary>
        public Vector2 Deslocamento => _desloc;

        /// <summary>orthographicSize do anel inteiro (sem o zoom do jogador). Menor que zero antes do primeiro enquadramento.</summary>
        public float MeiaAlturaBase => _meiaAlturaBase;

        void OnEnable() => Enquadrar();

        void Start()
        {
            if (Application.isPlaying) _diretor = FindFirstObjectByType<LoopDirector>();
            Enquadrar();
        }

        void OnValidate() => Enquadrar();

        void Update()
        {
            if (!Application.isPlaying) return;
            if (ZoomAtivo && !JornadaEncerrada) LerEntrada();
            Suavizar(Time.unscaledDeltaTime);
        }

        void LateUpdate()
        {
            if (!Application.isPlaying) return;

            if (enquadrarAutomaticamente && Time.unscaledTime >= _proximoEnquadramento)
            {
                _proximoEnquadramento = Time.unscaledTime + 0.25f;
                Enquadrar();
            }
            else
            {
                Aplicar();
            }
        }

        [ContextMenu("Enquadrar mapa")]
        public void Enquadrar()
        {
            if (Cam == null) return;
            if (mundo == null) mundo = FindFirstObjectByType<HexWorld>();

            Quaternion rotacao = Quaternion.Euler(inclinacao, giro, 0f);

            Vector3 centro = mundo != null ? mundo.transform.position : Vector3.zero;
            float meiaAltura = zoomManual;

            bool temIlhas = mundo != null && mundo.TotalDeIlhas > 0;
            if (enquadrarAutomaticamente && temIlhas)
            {
                centro = CentroDasIlhas(mundo);
                meiaAltura = MedirIlhas(mundo, centro, rotacao);
            }

            float distanciaReal = Mathf.Max(distancia, meiaAltura * 3f);
            if (!ortografica)
            {
                float tangente = Mathf.Tan(Cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
                distanciaReal = meiaAltura / Mathf.Max(0.01f, tangente);
            }

            _rotacao = rotacao;
            _centroBase = centro;
            _meiaAlturaBase = meiaAltura;
            _distanciaReal = distanciaReal;
            Aplicar();
        }

        /// <summary>Posiciona a camera: enquadramento-base + zoom e deslocamento do jogador (limitados ao anel inteiro).</summary>
        void Aplicar()
        {
            if (Cam == null || _meiaAlturaBase <= 0f) return;

            if (!ZoomAtivo)
            {
                _zoom = _zoomAlvo = 1f;
                _desloc = _deslocAlvo = Vector2.zero;
            }

            float aspecto = Aspecto;
            float minimo = ZoomMinimo(_meiaAlturaBase, menorMeiaAltura);
            _zoomAlvo = Mathf.Clamp(_zoomAlvo, minimo, 1f);
            _zoom = Mathf.Clamp(_zoom, minimo, 1f);
            _deslocAlvo = LimitarDeslocamento(_deslocAlvo, _meiaAlturaBase, aspecto, _zoomAlvo);
            _desloc = LimitarDeslocamento(_desloc, _meiaAlturaBase, aspecto, _zoom);

            Cam.orthographic = ortografica;
            if (ortografica) Cam.orthographicSize = _meiaAlturaBase * _zoom;

            Vector3 centro = _centroBase + _rotacao * new Vector3(_desloc.x, _desloc.y, 0f);
            transform.rotation = _rotacao;
            transform.position = centro - _rotacao * Vector3.forward * _distanciaReal;

            Cam.nearClipPlane = 0.1f;
            Cam.farClipPlane = Mathf.Max(1000f, _distanciaReal * 4f);
        }

        // --- Zoom do jogador ---

        /// <summary>
        /// Aproxima (passos maior que 0) ou afasta (menor que 0) mantendo parado o ponto do mundo que esta em
        /// <paramref name="pontoNaTela"/> (pixels, origem embaixo a esquerda, como o mouse).
        /// </summary>
        public void Aproximar(int passos, Vector2 pontoNaTela, bool imediato = false)
        {
            if (!ZoomAtivo || passos == 0) return;

            float minimo = ZoomMinimo(_meiaAlturaBase, menorMeiaAltura);
            float antes = _zoomAlvo;
            float depois = Mathf.Clamp(antes * Mathf.Pow(passoDoZoom, passos), minimo, 1f);
            if (Mathf.Approximately(antes, depois)) return;

            _deslocAlvo = DeslocamentoAoAproximar(_deslocAlvo, PontoNaVista(pontoNaTela), _meiaAlturaBase, Aspecto, antes, depois);
            _zoomAlvo = depois;
            _usouZoom = true;

            if (imediato)
            {
                _zoom = _zoomAlvo;
                _desloc = _deslocAlvo;
                Aplicar();
            }
        }

        /// <summary>Arrasta a vista: o mundo acompanha o mouse por <paramref name="deltaEmPixels"/>.</summary>
        public void Arrastar(Vector2 deltaEmPixels)
        {
            if (!ZoomAtivo || deltaEmPixels == Vector2.zero) return;

            float unidadesPorPixel = 2f * _meiaAlturaBase * _zoom / Mathf.Max(1, Cam.pixelHeight);
            Vector2 d = deltaEmPixels * unidadesPorPixel;
            float aspecto = Aspecto;
            _desloc = LimitarDeslocamento(_desloc - d, _meiaAlturaBase, aspecto, _zoom);
            _deslocAlvo = LimitarDeslocamento(_deslocAlvo - d, _meiaAlturaBase, aspecto, _zoomAlvo);
            _usouZoom = true;
            Aplicar();
        }

        /// <summary>Volta a mostrar o anel inteiro.</summary>
        public void VoltarAoAnelInteiro(bool imediato = false)
        {
            _zoomAlvo = 1f;
            _deslocAlvo = Vector2.zero;
            if (!imediato) return;
            _zoom = 1f;
            _desloc = Vector2.zero;
            Aplicar();
        }

        /// <summary>
        /// Vista do anel inteiro, sem o zoom do jogador: posicao e rotacao da camera e meia-altura ortografica.
        /// Camadas que se medem pela tela (nuvens) usam isto para ficarem presas ao mundo quando o jogador aproxima.
        /// </summary>
        public bool TryVistaBase(out Vector3 posicao, out Quaternion rotacao, out float meiaAltura)
        {
            rotacao = _rotacao;
            meiaAltura = _meiaAlturaBase;
            posicao = _centroBase - _rotacao * Vector3.forward * _distanciaReal;
            return ortografica && _meiaAlturaBase > 0f;
        }

        /// <summary>Menor fracao de zoom permitida: menorMeiaAltura / meia-altura do anel inteiro (1 = sem zoom).</summary>
        public static float ZoomMinimo(float meiaAlturaBase, float menorMeiaAltura)
        {
            if (meiaAlturaBase <= 0f) return 1f;
            return Mathf.Clamp(menorMeiaAltura / meiaAlturaBase, 0.05f, 1f);
        }

        /// <summary>Limita o deslocamento para a vista com este zoom nunca sair do retangulo do anel inteiro.</summary>
        public static Vector2 LimitarDeslocamento(Vector2 deslocamento, float meiaAlturaBase, float aspecto, float zoom)
        {
            float folga = Mathf.Max(0f, 1f - zoom) * Mathf.Max(0f, meiaAlturaBase);
            return new Vector2(
                Mathf.Clamp(deslocamento.x, -folga * aspecto, folga * aspecto),
                Mathf.Clamp(deslocamento.y, -folga, folga));
        }

        /// <summary>
        /// Deslocamento que mantem parado o ponto do mundo sob o cursor ao trocar o zoom.
        /// <paramref name="cursorNaVista"/> vai de -1 a 1 (centro = 0) nos dois eixos.
        /// </summary>
        public static Vector2 DeslocamentoAoAproximar(Vector2 deslocamento, Vector2 cursorNaVista, float meiaAlturaBase,
            float aspecto, float zoomAntes, float zoomDepois)
        {
            float diferenca = (zoomAntes - zoomDepois) * meiaAlturaBase;
            Vector2 novo = deslocamento + new Vector2(cursorNaVista.x * aspecto * diferenca, cursorNaVista.y * diferenca);
            return LimitarDeslocamento(novo, meiaAlturaBase, aspecto, zoomDepois);
        }

        Vector2 PontoNaVista(Vector2 pontoNaTela)
        {
            Rect r = Cam.pixelRect;
            float largura = Mathf.Max(1f, r.width);
            float altura = Mathf.Max(1f, r.height);
            return new Vector2((pontoNaTela.x - r.x) / largura * 2f - 1f, (pontoNaTela.y - r.y) / altura * 2f - 1f);
        }

        void Suavizar(float deltaTime)
        {
            if (_zoom == _zoomAlvo && _desloc == _deslocAlvo) return;

            // Zoom e deslocamento andam com o mesmo t: o ponto sob o cursor fica parado durante a transicao.
            float t = suavizacao <= 0f ? 1f : 1f - Mathf.Exp(-deltaTime / suavizacao);
            _zoom = Mathf.Lerp(_zoom, _zoomAlvo, t);
            _desloc = Vector2.Lerp(_desloc, _deslocAlvo, t);

            if (Mathf.Abs(_zoom - _zoomAlvo) < 1e-4f && (_desloc - _deslocAlvo).sqrMagnitude < 1e-8f)
            {
                _zoom = _zoomAlvo;
                _desloc = _deslocAlvo;
            }
        }

        void LerEntrada()
        {
            if (!LerMouse(out Vector2 posicao, out float roda, out bool comecouArraste, out bool segurandoArraste))
            {
                _arrastando = false;
                return;
            }

            bool naTela = Cam.pixelRect.Contains(posicao);

            // Input System 1.8+: 1 = um "dente" da roda; touchpad manda fracoes. Acumula e limita a 3 passos por frame.
            if (roda != 0f && naTela)
            {
                _rodaAcumulada += roda;
                int passos = (int)_rodaAcumulada;
                if (passos != 0)
                {
                    _rodaAcumulada -= passos;
                    Aproximar(Mathf.Clamp(passos, -3, 3), posicao);
                }
            }

            if (comecouArraste && naTela)
            {
                _arrastando = true;
                _ultimoMouse = posicao;
            }
            else if (_arrastando && segurandoArraste)
            {
                Arrastar(posicao - _ultimoMouse);
                _ultimoMouse = posicao;
            }
            else if (!segurandoArraste)
            {
                _arrastando = false;
            }
        }

        static bool LerMouse(out Vector2 posicao, out float roda, out bool comecouArraste, out bool segurandoArraste)
        {
#if ENABLE_INPUT_SYSTEM
            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                posicao = default;
                roda = 0f;
                comecouArraste = segurandoArraste = false;
                return false;
            }
            posicao = mouse.position.ReadValue();
            roda = mouse.scroll.ReadValue().y;
            comecouArraste = mouse.rightButton.wasPressedThisFrame || mouse.middleButton.wasPressedThisFrame;
            segurandoArraste = mouse.rightButton.isPressed || mouse.middleButton.isPressed;
            return true;
#else
            posicao = Input.mousePosition;
            roda = Input.mouseScrollDelta.y;
            comecouArraste = Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2);
            segurandoArraste = Input.GetMouseButton(1) || Input.GetMouseButton(2);
            return Input.mousePresent;
#endif
        }

        void OnGUI()
        {
            if (!Application.isPlaying || !mostrarDica || _usouZoom || !ZoomAtivo || JornadaEncerrada) return;
            if (ZoomMinimo(_meiaAlturaBase, menorMeiaAltura) >= 1f) return;

            if (_estiloDaDica == null)
                _estiloDaDica = new GUIStyle(GUI.skin.box) { fontSize = 10, alignment = TextAnchor.MiddleLeft };

            Matrix4x4 anterior = EscalaDaInterface.Aplicar(out _, out float alturaDaTela);
            try
            {
                GUI.Box(new Rect(12f, alturaDaTela - 30f, 236f, 22f), "Roda do mouse: zoom    Botao direito: mover", _estiloDaDica);
            }
            finally
            {
                GUI.matrix = anterior;
            }
        }

        // --- Medicao do anel ---

        static Vector3 CentroDasIlhas(HexWorld mundo)
        {
            Vector3 soma = Vector3.zero;
            int quantas = 0;

            foreach (HexTile ilha in mundo.Ilhas)
            {
                soma += ilha.transform.position;
                quantas++;
            }

            return quantas > 0 ? soma / quantas : mundo.transform.position;
        }

        /// <summary>
        /// Mede as ilhas no espaco da camera para saber quanto precisa caber na tela.
        /// Usa so a malha do hexagono de cada ilha (MeshFilter.sharedMesh, mesmo com o renderer desligado), entao o
        /// enquadramento nao depende do modelo da ilha, das decoracoes nem de plataformas. A folga de uma casa
        /// cobre a parte pendurada do modelo e as ilhas da borda.
        /// </summary>
        float MedirIlhas(HexWorld mundo, Vector3 centro, Quaternion rotacao)
        {
            Quaternion inversa = Quaternion.Inverse(rotacao);

            float maiorX = 0f;
            float maiorY = 0f;
            foreach (HexTile ilha in mundo.Ilhas)
            {
                Transform t = ilha.transform;
                var filtro = ilha.GetComponent<MeshFilter>();
                Bounds b = filtro != null && filtro.sharedMesh != null
                    ? filtro.sharedMesh.bounds
                    : new Bounds(Vector3.zero, Vector3.zero);
                for (int i = 0; i < 8; i++)
                {
                    Vector3 canto = b.center + Vector3.Scale(b.extents,
                        new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    Vector3 p = t.TransformPoint(canto);
                    Vector3 noEspacoDaCamera = inversa * (p - centro);
                    maiorX = Mathf.Max(maiorX, Mathf.Abs(noEspacoDaCamera.x));
                    maiorY = Mathf.Max(maiorY, Mathf.Abs(noEspacoDaCamera.y));
                }
            }

            // Uma casa de folga, senao as ilhas da borda ficam cortadas ao meio.
            float folga = mundo.tamanhoDoHexagono;
            maiorX += folga;
            maiorY += folga;

            float aspecto = Cam.aspect > 0.01f ? Cam.aspect : 16f / 9f;
            return Mathf.Max(maiorY, maiorX / aspecto) * margem;
        }
    }
}

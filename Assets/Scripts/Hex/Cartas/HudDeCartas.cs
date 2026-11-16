using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Loopia.Hex
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas), typeof(CanvasScaler))]
    [DefaultExecutionOrder(60)]
    public sealed class HudDeCartas : MonoBehaviour
    {
        [SerializeField] CatalogoVisualDeCartas catalogo;
        [SerializeField] CartaVisual prefabDeCarta;
        public CartasUI origem;

        const float EscalaCompacta = .445f;
        const float EscalaArraste = .465f;
        const float Folga = 9f;
        static readonly Color Valida = new Color32(165, 239, 207, 255);
        static readonly Color Invalida = new Color32(255, 169, 165, 255);

        Canvas _canvas;
        RectTransform _conteudo;
        RectTransform _maoVisual;
        CartaVisual[] _espacos;
        CartaVisual _arrastada;
        CartaVisual _consumida;
        TooltipDeCarta _tooltip;
        RectTransform _placaPendentes;
        TextMeshProUGUI _pendentes;
        RectTransform _faixaStatus;
        TextMeshProUGUI _status;
        EfeitoDaConstrucao _pulso;
        MaoDeCartas _mao;
        HexWorld _mundo;
        int _origemDoArraste = -1;
        int _hover = -1;
        float _tempoConsumo;
        bool _iniciada;
        bool _ativa;
        string _mensagem;

        public void Configurar(CatalogoVisualDeCartas dados, CartaVisual prefab)
        {
            catalogo = dados;
            prefabDeCarta = prefab;
            var canvas = GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 6;
            var escala = GetComponent<CanvasScaler>();
            escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            escala.referenceResolution = new Vector2(1920, 1080);
            escala.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            escala.matchWidthOrHeight = 1f;
        }

        void Start()
        {
            _iniciada = true;
            Conectar();
        }

        void OnEnable()
        {
            if (_iniciada) Conectar();
        }

        void Conectar()
        {
            if (origem == null) origem = FindFirstObjectByType<CartasUI>();
            if (origem == null || !origem.AtivarHud(this)) return;
            _ativa = true;
            AtualizarMao(false);
            AtualizarPendentes();
            _mao.AoMudar += AoMudarMao;
            _mao.AoMudarPendentes += AtualizarPendentes;
        }

        void OnDisable()
        {
            if (!_ativa) return;
            _ativa = false;
            if (origem != null) origem.DesativarHud(this);
            if (_mao != null)
            {
                _mao.AoMudar -= AoMudarMao;
                _mao.AoMudarPendentes -= AtualizarPendentes;
            }
            EncerrarArraste();
            Visivel(false);
        }

        void OnDestroy()
        {
            _pulso?.Liberar();
        }

        internal bool Preparar(MaoDeCartas mao, HexWorld mundo)
        {
            if (catalogo == null || !catalogo.Completo || prefabDeCarta == null || !prefabDeCarta.Pronta ||
                mao.Mao == null || !CobrirBaralho(mao))
            {
                Debug.LogWarning("[HudDeCartas] Existem referencias ou cartas sem arte. A HUD IMGUI segue ativa.", this);
                return false;
            }

            _mao = mao;
            _mundo = mundo;
            if (_espacos != null)
            {
                Visivel(true);
                return true;
            }
            _canvas = GetComponent<Canvas>();
            _conteudo = ElementosDaCarta.Caixa(transform, "Apresentacao da mao");
            _maoVisual = ElementosDaCarta.Caixa(_conteudo, "Espacos da mao");
            var espaco = CartaVisual.Tamanho * EscalaCompacta;
            float largura = mao.Mao.Count * (espaco.x + Folga) - Folga;
            ElementosDaCarta.Retangulo(_maoVisual, new Vector2(.5f, 0), new Vector2(.5f, 0),
                new Rect(0, 11, largura, espaco.y));

            _espacos = new CartaVisual[mao.Mao.Count];
            for (int i = 0; i < _espacos.Length; i++)
            {
                _espacos[i] = Instanciar(_maoVisual, $"Espaco {i + 1}", EscalaCompacta);
                ElementosDaCarta.Retangulo(_espacos[i].Caixa, Vector2.zero, Vector2.zero,
                    new Rect(i * (espaco.x + Folga), 0, CartaVisual.Tamanho.x, CartaVisual.Tamanho.y));
            }

            _placaPendentes = TemaDaJornada.Janela(_maoVisual, "Fila de recompensas", Vector2.one,
                new Vector2(1, 0), new Rect(0, 12, 134, 26));
            _pendentes = ElementosDaCarta.Texto(_placaPendentes, "Contador", catalogo.fonteCorpo, 14,
                TemaDaJornada.Ouro);
            ElementosDaCarta.Preencher(_pendentes.rectTransform, 3f);

            _tooltip = new TooltipDeCarta(_conteudo, prefabDeCarta, catalogo);
            _faixaStatus = ElementosDaCarta.Painel(_conteudo, "Validade da colocacao",
                ElementosDaCarta.Cor("#211C30EF")).rectTransform;
            ElementosDaCarta.Retangulo(_faixaStatus, Vector2.right, Vector2.right,
                new Rect(-22, 19, 426, 45));
            _status = ElementosDaCarta.Texto(_faixaStatus, "Motivo da colocacao", catalogo.fonteCorpo, 16, Valida);
            ElementosDaCarta.Preencher(_status.rectTransform, 6f);
            ElementosDaCarta.AjustarFonte(_status, 13, 16);
            _faixaStatus.gameObject.SetActive(false);

            _arrastada = Instanciar(_conteudo, "Carta arrastada", EscalaArraste);
            _consumida = Instanciar(_conteudo, "Carta consumida", EscalaArraste);
            _arrastada.gameObject.SetActive(false);
            _consumida.gameObject.SetActive(false);
            _pulso = new EfeitoDaConstrucao(mundo.transform, catalogo.pulsoNoMapa);
            return true;
        }

        bool CobrirBaralho(MaoDeCartas mao)
        {
            foreach (var def in mao.baralho)
                if (def != null && !catalogo.Cobre(def)) return false;
            foreach (var def in mao.cartasGarantidas)
                if (def != null && !catalogo.Cobre(def)) return false;
            foreach (var def in mao.Mao)
                if (def != null && !catalogo.Cobre(def)) return false;
            return mao.cartaAoLiberar == null || catalogo.Cobre(mao.cartaAoLiberar);
        }

        CartaVisual Instanciar(Transform pai, string nome, float escala)
        {
            var visual = Instantiate(prefabDeCarta, pai, false);
            visual.name = nome;
            visual.Caixa.localScale = Vector3.one * escala;
            return visual;
        }

        void AoMudarMao() => AtualizarMao(true);

        void AtualizarMao(bool animar)
        {
            for (int i = 0; i < _espacos.Length; i++)
            {
                PlataformaDef def = _mao.CartaNoEspaco(i);
                if (animar && _espacos[i].Def == def && _origemDoArraste != i) continue;
                _espacos[i].Exibir(def, catalogo.Buscar(def), animar && def != null);
            }
        }

        void AtualizarPendentes()
        {
            int n = _mao.Pendentes;
            _placaPendentes.gameObject.SetActive(n > 0);
            if (n > 0) _pendentes.text = n == 1 ? "+1 pendente" : $"+{n} pendentes";
        }

        internal void Visivel(bool visivel)
        {
            if (_conteudo == null || _conteudo.gameObject.activeSelf == visivel) return;
            _conteudo.gameObject.SetActive(visivel);
            if (!visivel)
            {
                _hover = -1;
                _tooltip.Ocultar();
                _consumida.gameObject.SetActive(false);
                _pulso.Apagar();
            }
        }

        Camera CameraDaUi => _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;

        internal int EspacoSobCursor(Vector2 ponto)
        {
            if (!_conteudo.gameObject.activeInHierarchy) return -1;
            for (int i = 0; i < _espacos.Length; i++)
                if (RectTransformUtility.RectangleContainsScreenPoint(_espacos[i].Caixa, ponto, CameraDaUi))
                    return i;
            return -1;
        }

        internal bool SobreAMao(Vector2 ponto) =>
            RectTransformUtility.RectangleContainsScreenPoint(_maoVisual, ponto, CameraDaUi);

        Vector2 DirecaoDoCursor(RectTransform caixa, Vector2 tela)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(caixa, tela, CameraDaUi, out Vector2 local);
            Vector2 normalizado = (local - caixa.rect.center) * 2f / caixa.rect.size;
            return Vector2.ClampMagnitude(normalizado, 1f);
        }

        internal void Observar(Vector2 ponto, Plataforma apontada, float dt)
        {
            _hover = _origemDoArraste < 0 ? EspacoSobCursor(ponto) : -1;
            PlataformaDef def = _mao.CartaNoEspaco(_hover);
            Vector2 movimento = def != null ? DirecaoDoCursor(_espacos[_hover].Caixa, ponto) : Vector2.zero;

            if (def != null) _tooltip.Mostrar(def, catalogo.Buscar(def), true);
            else if (_origemDoArraste >= 0 || apontada == null) _tooltip.Ocultar();
            else _tooltip.Mostrar(apontada.Def, catalogo.Buscar(apontada.Def), false);

            for (int i = 0; i < _espacos.Length; i++)
            {
                bool elevada = i == _hover && def != null;
                _espacos[i].Animar(elevada ? movimento : Vector2.zero, elevada, elevada, dt);
            }
            _tooltip.Animar(movimento, dt);
            AnimarConsumo(dt);
            _pulso.Atualizar(dt, _mundo.tamanhoDoHexagono);
        }

        internal void IniciarArraste(int espaco, PlataformaDef def, Vector2 ponto)
        {
            _origemDoArraste = espaco;
            _espacos[espaco].Opacidade(.27f);
            _arrastada.Exibir(def, catalogo.Buscar(def));
            _arrastada.gameObject.SetActive(true);
            MoverArraste(ponto);
            _tooltip.Ocultar();
        }

        void MoverArraste(Vector2 tela)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_conteudo, tela, CameraDaUi, out Vector2 ponto);
            // Mantem o cursor livre para apontar a ilha e limita o fantasma ao quadro.
            Vector2 meia = CartaVisual.Tamanho * (EscalaArraste * .5f);
            Rect seguro = _conteudo.rect;
            ponto.x = Mathf.Clamp(ponto.x + meia.x + 14f, seguro.xMin + meia.x, seguro.xMax - meia.x);
            ponto.y = Mathf.Clamp(ponto.y + meia.y * .18f, seguro.yMin + meia.y, seguro.yMax - meia.y);
            _arrastada.Caixa.anchoredPosition = ponto;
        }

        internal void Arrastando(Vector2 ponto, bool valida, string motivo, float dt)
        {
            MoverArraste(ponto);
            _arrastada.Animar(DirecaoDoCursor(_arrastada.Caixa, ponto) * .64f, false, true, dt);
            if (_mensagem != motivo)
            {
                _mensagem = motivo;
                _status.text = motivo;
            }
            _status.color = valida ? Valida : Invalida;
            _faixaStatus.gameObject.SetActive(true);
        }

        internal void EncerrarArraste()
        {
            if (_origemDoArraste < 0) return;
            var entrada = _espacos[_origemDoArraste];
            entrada.Opacidade(entrada.Def == null ? .31f : 1f);
            _origemDoArraste = -1;
            _arrastada.gameObject.SetActive(false);
            _faixaStatus.gameObject.SetActive(false);
        }

        internal void Consumir(PlataformaDef def, HexCoord casa)
        {
            ArteDeCarta arte = catalogo.Buscar(def);
            _tempoConsumo = 0f;
            _consumida.Exibir(def, arte, true);
            _consumida.Caixa.anchoredPosition = _arrastada.Caixa.anchoredPosition;
            _consumida.Caixa.localScale = Vector3.one * EscalaArraste;
            _consumida.gameObject.SetActive(true);
            _pulso.Iniciar(_mundo, casa, arte?.destaque ?? Color.white);
        }

        void AnimarConsumo(float dt)
        {
            if (!_consumida.gameObject.activeSelf) return;
            _tempoConsumo += dt;
            float t = Mathf.Clamp01(_tempoConsumo / .27f);
            _consumida.Opacidade(1f - t);
            _consumida.Caixa.localScale = Vector3.one * (EscalaArraste * Mathf.Lerp(1.05f, .65f, t));
            _consumida.Animar(Vector2.zero, false, false, dt);
            if (t >= 1f) _consumida.gameObject.SetActive(false);
        }
    }
}

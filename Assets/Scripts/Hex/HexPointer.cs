using System.Collections.Generic;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Loopia.Hex
{
    /// <summary>
    /// Traduz o mouse em ordens para o player:
    /// - passar o mouse destaca o hexagono e mostra o caminho ate ele;
    /// - BOTAO DIREITO manda o player andar ate aquele hexagono.
    ///
    /// Nao usa colisores: o raio do mouse e cruzado com o plano do mundo e a
    /// posicao resultante e convertida direto em coordenada axial.
    /// </summary>
    [DisallowMultipleComponent]
    public class HexPointer : MonoBehaviour
    {
        [Header("Referencias")]
        [Tooltip("Deixe vazio para achar automaticamente na cena.")]
        public HexWorld mundo;
        public HexPlayer player;

        [Tooltip("Deixe vazio para usar a Camera.main.")]
        public Camera camera3d;

        [Header("Controle manual")]
        [Tooltip("No GDD o Lucca anda sozinho pelo loop. Ligue isto so para depurar o mundo " +
                 "a mao: ai o botao direito volta a mandar o player para o hexagono clicado.")]
        public bool moverComBotaoDireito = false;

        [Header("Destaques")]
        [Tooltip("Pinta o hexagono que esta embaixo do mouse.")]
        public bool destacarHexagonoSobOMouse = true;

        [Tooltip("Pinta o caminho que o player faria ate o hexagono sob o mouse.")]
        public bool mostrarPreviaDoCaminho = true;

        readonly List<HexCoord> _previa = new List<HexCoord>();
        readonly List<HexTile> _destacados = new List<HexTile>();

        HexTile _sobOMouse;
        HexCoord _coordDoPlayer;
        bool _temHexagonoValido;

        void Start()
        {
            if (mundo == null) mundo = FindFirstObjectByType<HexWorld>();
            if (player == null) player = FindFirstObjectByType<HexPlayer>();
            if (camera3d == null) camera3d = Camera.main;

            if (mundo == null || player == null || camera3d == null)
            {
                Debug.LogError(
                    "[HexPointer] Faltou referencia: precisa de um HexWorld, um HexPlayer e uma Camera na cena.", this);
                enabled = false;
            }
        }

        void Update()
        {
            HexTile alvo = HexagonoSobOMouse();

            bool mudouAlvo = alvo != _sobOMouse;
            bool mudouPlayer = player.Coord != _coordDoPlayer;

            if (mudouAlvo || mudouPlayer)
            {
                _sobOMouse = alvo;
                _coordDoPlayer = player.Coord;
                AtualizarDestaques();
            }

            if (moverComBotaoDireito && alvo != null && BotaoDireitoFoiPressionado())
            {
                player.MoverPara(alvo.Coord);
            }
        }

        void OnDisable()
        {
            LimparDestaques();
            _sobOMouse = null;
        }

        HexTile HexagonoSobOMouse()
        {
            _temHexagonoValido = false;

            if (!TryPosicaoDoMouse(out Vector3 posicaoNaTela)) return null;

            Ray raio = camera3d.ScreenPointToRay(posicaoNaTela);
            Plane plano = mundo.PlanoDaSuperficie;

            if (!plano.Raycast(raio, out float distancia)) return null;

            Vector3 ponto = raio.GetPoint(distancia);
            if (!mundo.TryIlhaEmMundo(ponto, out HexTile tile)) return null;

            _temHexagonoValido = true;
            return tile;
        }

        void AtualizarDestaques()
        {
            LimparDestaques();

            if (_sobOMouse == null || !_temHexagonoValido) return;

            if (moverComBotaoDireito && mostrarPreviaDoCaminho && player.PreverCaminho(_sobOMouse.Coord, _previa))
            {
                foreach (HexCoord coord in _previa)
                {
                    if (!mundo.TryGetIlha(coord, out HexTile tile)) continue;
                    tile.SetOnPath(true);
                    _destacados.Add(tile);
                }
            }

            if (destacarHexagonoSobOMouse)
            {
                _sobOMouse.SetHovered(true);
                if (!_destacados.Contains(_sobOMouse)) _destacados.Add(_sobOMouse);
            }
        }

        void LimparDestaques()
        {
            foreach (HexTile tile in _destacados)
            {
                // Um mapa novo destroi as ilhas antigas, entao a lista pode ter referencia morta.
                if (tile == null) continue;
                tile.ClearHighlights();
            }
            _destacados.Clear();
        }

        // --- Entrada, funcionando com o Input System novo ou com o antigo ---

        static bool BotaoDireitoFoiPressionado()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
#else
            return Input.GetMouseButtonDown(1);
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

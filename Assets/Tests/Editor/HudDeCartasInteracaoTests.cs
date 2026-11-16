using System.Collections;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

namespace Loopia.Hex.Tests
{
    /// <summary>
    /// Regressao da interacao com a HUD de cartas na cena oficial, com eventos reais do Input System: tooltip depois
    /// de soltar (valida e invalida), troca de hover carta/plataforma, perda de foco e fim da jornada durante o arraste.
    /// </summary>
    [Category("PlayModeSmoke")]
    public class HudDeCartasInteracaoTests
    {
        sealed class Sessao
        {
            public CartasUI ui;
            public HudDeCartas hud;
            public Transform grupo;
            public Mouse mouse;
            public InputSettings original, configuracao;

            public MaoDeCartas Mao => ui.mao;
            public Transform Detalhes => grupo.Find("Detalhes da carta");
            public bool TooltipAberto => Detalhes.gameObject.activeSelf;
            public bool TooltipAmpliado => Detalhes.GetComponentInChildren<CartaVisual>(true).gameObject.activeSelf;
            public string TituloDoTooltip => Detalhes.GetComponentsInChildren<TMP_Text>(true).First(t => t.name == "Nome").text;
            public GameObject Fantasma => grupo.Find("Carta arrastada").gameObject;

            public Vector2 CentroDoEspaco(int i)
            {
                var slot = (RectTransform)grupo.Find("Espacos da mao").GetChild(i);
                return RectTransformUtility.WorldToScreenPoint(null, slot.TransformPoint(slot.rect.center));
            }

            /// <summary>Ponto de tela que aponta exatamente para a casa (raycast real), longe da mao.</summary>
            public bool PontoDaCasa(HexCoord casa, out Vector2 tela)
            {
                Vector3 p = ui.camera3d.WorldToScreenPoint(ui.mundo.PosicaoDe(casa));
                tela = p;
                if (p.z <= 0 || p.x < 2 || p.x > Screen.width - 2 || p.y < Screen.height * .3f || p.y > Screen.height - 2)
                    return false;
                return ui.mundo.TryCasaNoRaio(ui.camera3d.ScreenPointToRay(p), out HexCoord apontada) && apontada.Equals(casa);
            }
        }

        Sessao _s;

        [UnityTearDown]
        public IEnumerator Encerrar()
        {
            if (_s != null && _s.mouse != null && EditorApplication.isPlaying)
            {
                InputSystem.RemoveDevice(_s.mouse);
                InputSystem.settings = _s.original;
                Object.Destroy(_s.configuracao);
            }
            _s = null;
            if (EditorApplication.isPlaying) yield return new ExitPlayMode();
            SaveIsolado.Restaurar();
            CenaDoTeste.Restaurar();
        }

        static void AntesDoPlay()
        {
            SaveIsolado.Backup();
            CenaDoTeste.AbrirCenaPrincipal();
        }

        IEnumerator Preparar()
        {
            Application.runInBackground = true;
            yield return null;
            Object.FindFirstObjectByType<HexLoopRunner>().andando = false;
            Object.FindFirstObjectByType<InimigoSpawner>().nascerMorcegos = false;
            _s = new Sessao { ui = Object.FindFirstObjectByType<CartasUI>(), hud = Object.FindFirstObjectByType<HudDeCartas>() };
            _s.original = InputSystem.settings;
            _s.configuracao = Object.Instantiate(_s.original);
            _s.configuracao.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            _s.configuracao.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            _s.configuracao.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
            InputSystem.settings = _s.configuracao;
            _s.mouse = InputSystem.AddDevice<Mouse>();
            yield return null;
            Canvas.ForceUpdateCanvases();
            _s.grupo = _s.hud.transform.Find("Apresentacao da mao");
            Assert.IsNotNull(_s.grupo, "a HUD da cena oficial assumiu a mao");
        }

        IEnumerator Ponteiro(Vector2 posicao, bool pressionado)
        {
            _s.mouse.MakeCurrent();
            InputSystem.QueueStateEvent(_s.mouse, new MouseState { position = posicao, buttons = (ushort)(pressionado ? 1 : 0) });
            InputSystem.Update();
            yield return null;
            yield return null;
        }

        /// <summary>Casa do anel onde a Mola (no caminho) pode ir, visivel acima da mao.</summary>
        HexCoord CasaValida(PlataformaDef carta, out Vector2 tela, HexCoord? exceto = null)
        {
            foreach (HexCoord casa in Object.FindFirstObjectByType<HexLoopGenerator>().Loop.Casas)
            {
                if (exceto.HasValue && casa.Equals(exceto.Value)) continue;
                if (!_s.ui.mapa.PodeColocar(carta, casa, out _) || !_s.PontoDaCasa(casa, out tela)) continue;
                return casa;
            }
            Assert.Fail("nenhuma casa valida visivel para " + carta.name);
            tela = default;
            return default;
        }

        [UnityTest]
        public IEnumerator ColocacaoValida_TooltipNaoFicaPreso_EReabreNumHoverNovo()
        {
            AntesDoPlay();
            yield return new EnterPlayMode();
            yield return Preparar();
            var mola = _s.Mao.CartaNoEspaco(0);
            Assert.AreEqual("Mola", mola.name);
            Vector2 slot = _s.CentroDoEspaco(0);
            HexCoord alvo = CasaValida(mola, out Vector2 soltar);
            CasaValida(mola, out Vector2 vazia, alvo);

            yield return Ponteiro(slot, false);
            Assert.IsTrue(_s.TooltipAberto && _s.TooltipAmpliado, "hover na carta abre o tooltip ampliado");

            yield return Ponteiro(slot, true);
            Assert.AreSame(mola, _s.ui.CartaArrastada);
            Assert.IsFalse(_s.TooltipAberto, "iniciar o arraste fecha o tooltip");
            yield return Ponteiro(soltar, true);
            Assert.IsFalse(_s.TooltipAberto, "durante o arraste so o feedback de colocacao aparece");

            yield return Ponteiro(soltar, false);
            Assert.IsTrue(_s.ui.mapa.TryGet(alvo, out Plataforma colocada) && colocada.Def == mola, "a carta foi construida");
            Assert.IsFalse(_s.TooltipAberto, "soltar nao abre o tooltip da plataforma recem-construida");
            yield return Ponteiro(soltar + Vector2.right * 1f, false);
            Assert.IsFalse(_s.TooltipAberto, "o cursor parado sobre a construcao nao conta como hover novo");

            yield return Ponteiro(vazia, false);
            Assert.IsFalse(_s.TooltipAberto, "casa sem plataforma: sem tooltip");
            yield return Ponteiro(soltar, false);
            Assert.IsTrue(_s.TooltipAberto && !_s.TooltipAmpliado, "hover voluntario na plataforma mostra suas informacoes");
            Assert.AreEqual("Mola", _s.TituloDoTooltip);
            yield return Ponteiro(vazia, false);
            Assert.IsFalse(_s.TooltipAberto, "sair da plataforma fecha o tooltip");

            yield return Ponteiro(_s.CentroDoEspaco(1), false);
            Assert.IsTrue(_s.TooltipAberto && _s.TooltipAmpliado, "hover numa carta reabre o tooltip ampliado");
        }

        [UnityTest]
        public IEnumerator ColocacaoInvalida_CartaVoltaAoEspaco_SemTooltipPreso()
        {
            AntesDoPlay();
            yield return new EnterPlayMode();
            yield return Preparar();
            var mola = _s.Mao.CartaNoEspaco(0);
            Vector2 slot = _s.CentroDoEspaco(0);
            var gerador = Object.FindFirstObjectByType<HexLoopGenerator>();
            Assert.IsTrue(_s.PontoDaCasa(gerador.CentroDoMiolo, out Vector2 miolo), "o centro do miolo esta visivel");
            int livresAntes = _s.Mao.EspacosLivres;

            yield return Ponteiro(slot, false);
            yield return Ponteiro(slot, true);
            yield return Ponteiro(miolo, true);
            Assert.IsFalse(_s.TooltipAberto);
            yield return Ponteiro(miolo, false);

            Assert.IsNull(_s.ui.CartaArrastada);
            Assert.IsFalse(_s.Fantasma.activeSelf, "o fantasma some");
            Assert.AreSame(mola, _s.Mao.CartaNoEspaco(0), "a carta volta ao espaco");
            Assert.AreEqual(livresAntes, _s.Mao.EspacosLivres, "nada consumido");
            Assert.IsFalse(_s.ui.mapa.Tem(gerador.CentroDoMiolo));
            Assert.IsFalse(_s.TooltipAberto, "sem tooltip preso depois de soltar no lugar invalido");
            var espaco = _s.grupo.Find("Espacos da mao").GetChild(0).GetComponent<CanvasGroup>();
            Assert.AreEqual(1f, espaco.alpha, 1e-3f, "a carta volta com opacidade cheia");

            yield return Ponteiro(slot, false);
            Assert.IsTrue(_s.TooltipAberto && _s.TooltipAmpliado, "um hover novo reabre o tooltip");
        }

        [UnityTest]
        public IEnumerator HoverAlternaEntreCartaEPlataforma_SemEstadoAntigo()
        {
            AntesDoPlay();
            yield return new EnterPlayMode();
            yield return Preparar();
            var mola = _s.Mao.CartaNoEspaco(0);
            HexCoord alvo = CasaValida(mola, out Vector2 ponto);
            Assert.IsNotNull(_s.ui.mapa.Colocar(mola, alvo));
            Assert.IsTrue(_s.Mao.Gastar(0));
            CasaValida(_s.Mao.CartaNoEspaco(1) ?? mola, out Vector2 vazia, alvo);

            yield return Ponteiro(ponto, false);
            Assert.IsTrue(_s.TooltipAberto && !_s.TooltipAmpliado);
            Assert.AreEqual("Mola", _s.TituloDoTooltip);

            yield return Ponteiro(_s.CentroDoEspaco(2), false);
            string carta = _s.TituloDoTooltip;
            Assert.IsTrue(_s.TooltipAberto && _s.TooltipAmpliado);
            Assert.AreNotEqual("Mola", carta, "o tooltip troca para a carta");

            yield return Ponteiro(ponto, false);
            Assert.IsTrue(_s.TooltipAberto && !_s.TooltipAmpliado);
            Assert.AreEqual("Mola", _s.TituloDoTooltip, "e volta para a plataforma");

            yield return Ponteiro(vazia, false);
            Assert.IsFalse(_s.TooltipAberto);
        }

        [UnityTest]
        public IEnumerator PerdaDeFocoDuranteArraste_CancelaSemConsumir()
        {
            AntesDoPlay();
            yield return new EnterPlayMode();
            yield return Preparar();
            var mola = _s.Mao.CartaNoEspaco(0);
            HexCoord alvo = CasaValida(mola, out Vector2 soltar);
            yield return Ponteiro(_s.CentroDoEspaco(0), false);
            yield return Ponteiro(_s.CentroDoEspaco(0), true);
            yield return Ponteiro(soltar, true);
            Assert.IsTrue(_s.Fantasma.activeSelf);

            Reflexao.Chamar(_s.ui, "OnApplicationFocus", false);
            yield return null;
            Assert.IsNull(_s.ui.CartaArrastada, "perder o foco cancela o arraste");
            Assert.IsFalse(_s.Fantasma.activeSelf);
            Assert.IsFalse(_s.TooltipAberto);
            Assert.IsFalse(_s.grupo.Find("Validade da colocacao").gameObject.activeSelf);
            Assert.IsFalse(Object.FindFirstObjectByType<HexPlacementGrid>().Visivel, "a grade some");

            yield return Ponteiro(soltar, false);
            Assert.IsFalse(_s.ui.mapa.Tem(alvo), "soltar depois de perder o foco nao constroi");
            Assert.AreSame(mola, _s.Mao.CartaNoEspaco(0));
            Assert.IsFalse(_s.TooltipAberto, "sem tooltip preso");
        }

        [UnityTest]
        public IEnumerator FimDaJornadaDuranteArraste_LimpaOEstadoESemConsumo()
        {
            AntesDoPlay();
            yield return new EnterPlayMode();
            yield return Preparar();
            var mola = _s.Mao.CartaNoEspaco(0);
            HexCoord alvo = CasaValida(mola, out Vector2 soltar);
            yield return Ponteiro(_s.CentroDoEspaco(0), false);
            yield return Ponteiro(_s.CentroDoEspaco(0), true);
            yield return Ponteiro(soltar, true);

            _s.Mao.status.ReceberDano(9999);
            yield return null;
            Assert.IsNull(_s.ui.CartaArrastada);
            Assert.IsFalse(_s.grupo.gameObject.activeSelf, "a mao some no fim da jornada");
            Assert.IsFalse(_s.Fantasma.activeSelf);
            Assert.IsFalse(_s.TooltipAberto);
            yield return Ponteiro(soltar, false);
            Assert.IsFalse(_s.ui.mapa.Tem(alvo));
        }

        [UnityTest]
        public IEnumerator QuatroEspacos_VazioEPendenteSaoDistintos_EConsumoSemRegressao()
        {
            AntesDoPlay();
            yield return new EnterPlayMode();
            yield return Preparar();
            Transform espacos = _s.grupo.Find("Espacos da mao");
            Assert.AreEqual(4, _s.Mao.Mao.Count);
            for (int i = 0; i < 4; i++)
            {
                var visual = espacos.GetChild(i).GetComponent<CartaVisual>();
                Assert.AreSame(_s.Mao.CartaNoEspaco(i), visual.Def, "espaco " + i);
            }
            // Coracao (espaco 1): colocar a Mola liberaria a Ilha alta, que ocuparia o espaco na hora.
            var coracao = _s.Mao.CartaNoEspaco(1);
            Assert.AreEqual("Coracao", coracao.name);
            HexCoord alvo = CasaValida(coracao, out Vector2 soltar);
            yield return Ponteiro(_s.CentroDoEspaco(1), false);
            yield return Ponteiro(_s.CentroDoEspaco(1), true);
            yield return Ponteiro(soltar, true);
            yield return Ponteiro(soltar, false);
            Assert.IsTrue(_s.ui.mapa.Tem(alvo));
            Assert.AreEqual(1, _s.Mao.EspacosLivres, "uma carta consumida, sem pendente");
            var vazio = espacos.GetChild(1).GetComponent<CartaVisual>();
            Assert.IsNull(vazio.Def);
            Assert.IsTrue(vazio.Vazio, "o espaco vazio tem leitura propria");
            Assert.IsFalse(espacos.GetChild(0).GetComponent<CartaVisual>().Vazio);

            _s.Mao.status.GanharXp(_s.Mao.status.XpParaOProximoNivel);
            yield return null;
            Assert.AreEqual(0, _s.Mao.EspacosLivres, "a recompensa preenche o espaco vazio");
            Assert.IsNotNull(_s.Mao.CartaNoEspaco(1));
            Assert.IsFalse(vazio.Vazio);
            Assert.AreSame(_s.Mao.CartaNoEspaco(1), vazio.Def);
        }
    }
}

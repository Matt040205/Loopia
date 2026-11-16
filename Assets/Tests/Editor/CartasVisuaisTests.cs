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
    public class CartasVisuaisTests
    {
        const string Arte = "Assets/Arte/Cartas/";
        const string Dados = "Assets/Dados/Plataformas/";

        [Test, Category("SceneStatic")]
        public void CatalogoCobreAsDefinicoesComArtePropriaEAssetsDeUi()
        {
            var catalogo = AssetDatabase.LoadAssetAtPath<CatalogoVisualDeCartas>(Arte + "Catalogo.asset");
            var prefab = AssetDatabase.LoadAssetAtPath<HudDeCartas>(Arte + "HUD de Cartas.prefab");
            Assert.IsNotNull(catalogo);
            Assert.IsNotNull(prefab);
            Assert.IsTrue(catalogo.Completo);
            Assert.IsTrue(catalogo.ouro.shader.isSupported);
            Assert.AreEqual(1024, catalogo.moldura.width);
            Assert.AreEqual(1424, catalogo.moldura.height);
            Assert.AreEqual(14, catalogo.cartas.Count);
            Assert.AreEqual(14, catalogo.cartas.Select(x => x.objeto).Distinct().Count());

            var defs = AssetDatabase.FindAssets("t:PlataformaDef", new[] { Dados.TrimEnd('/') })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<PlataformaDef>);
            foreach (var def in defs)
            {
                Assert.IsTrue(catalogo.Cobre(def), def.name);
                ArteDeCarta arte = catalogo.Buscar(def);
                Assert.IsNotNull(arte.atmosfera, def.name);
                Assert.IsNotNull(arte.categoria, def.name);
                Assert.AreEqual(arte.poseA == null, arte.poseB == null, def.name);
            }

            Assert.IsTrue(catalogo.fonteTitulo.HasCharacters("Básica Área Coração Pacífico Santuário"));
        }

        [UnityTest, Category("PlayModeSmoke")]
        public IEnumerator Arraste_ValidaAntesDeGastar_EntregaPendente_EOcultaAoTerminar()
        {
            SaveIsolado.Backup();
            CenaDoTeste.AbrirCenaPrincipal();
            yield return new EnterPlayMode();
            yield return ExercitarHud();
            yield return new ExitPlayMode();
        }

        [UnityTearDown]
        public IEnumerator RestaurarCena()
        {
            if (EditorApplication.isPlaying) yield return new ExitPlayMode();
            SaveIsolado.Restaurar();
            CenaDoTeste.Restaurar();
        }

        static IEnumerator ExercitarHud()
        {
            Application.runInBackground = true;
            yield return null;
            Object.FindFirstObjectByType<HexLoopRunner>().andando = false;
            Object.FindFirstObjectByType<InimigoSpawner>().nascerMorcegos = false;
            var ui = Object.FindFirstObjectByType<CartasUI>();
            var prefab = AssetDatabase.LoadAssetAtPath<HudDeCartas>(Arte + "HUD de Cartas.prefab");
            HudDeCartas hud = Object.Instantiate(prefab);
            hud.origem = ui;
            var configuracaoOriginal = InputSystem.settings;
            var configuracao = Object.Instantiate(configuracaoOriginal);
            configuracao.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            configuracao.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            configuracao.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
            InputSystem.settings = configuracao;
            Mouse mouse = InputSystem.AddDevice<Mouse>();
            try
            {
                yield return null;
                Canvas.ForceUpdateCanvases();
                var grupo = hud.transform.Find("Apresentacao da mao");
                Assert.IsNotNull(grupo, "o Canvas deveria ter assumido a apresentacao");
                Transform maoVisual = grupo.Find("Espacos da mao");
                Assert.AreEqual(5, maoVisual.childCount, "quatro espacos e o indicador");
                var slot = (RectTransform)maoVisual.GetChild(0);
                var centro = RectTransformUtility.WorldToScreenPoint(null, slot.TransformPoint(slot.rect.center));
                var mao = ui.mao;
                Assert.AreEqual(0, mao.EspacosLivres);

                var mola = mao.CartaNoEspaco(0);
                Assert.AreEqual("Mola", mola.name);
                var ilhaAlta = AssetDatabase.LoadAssetAtPath<PlataformaDef>(Dados + "IlhaAlta.asset");
                Assert.IsFalse(ilhaAlta.DisponivelParaCompra(ui.mapa));
                mao.status.GanharXp(mao.status.XpParaOProximoNivel);
                Assert.AreEqual(1, mao.Pendentes);
                Assert.AreEqual("+1 pendente", maoVisual.GetChild(4).GetComponentInChildren<TMP_Text>().text);

                yield return Ponteiro(mouse, centro, false);
                Assert.IsTrue(grupo.Find("Detalhes da carta").gameObject.activeSelf,
                    $"O slot em {centro} nao abriu o tooltip em {Screen.width}x{Screen.height}.");

                yield return Ponteiro(mouse, centro, true);
                Assert.AreSame(mola, ui.CartaArrastada);
                Assert.IsTrue(grupo.Find("Carta arrastada").gameObject.activeSelf);
                yield return Ponteiro(mouse, centro, false);
                Assert.AreSame(mola, mao.CartaNoEspaco(0), "soltar sobre a mao nao consome");
                Assert.IsNull(ui.CartaArrastada);
                Assert.AreEqual(1, mao.Pendentes);

                HexCoord alvo = default;
                Vector2 soltar = default;
                bool achou = false;
                foreach (HexCoord casa in Object.FindFirstObjectByType<HexLoopGenerator>().Loop.Casas)
                {
                    if (!ui.mapa.PodeColocar(mola, casa, out _)) continue;
                    Vector3 ponto = ui.camera3d.WorldToScreenPoint(ui.mundo.PosicaoDe(casa));
                    if (ponto.z <= 0 || ponto.x < 2 || ponto.x > Screen.width - 2 ||
                        ponto.y < Screen.height * .21f || ponto.y > Screen.height - 2) continue;
                    Ray raio = ui.camera3d.ScreenPointToRay(ponto);
                    if (!ui.mundo.TryCasaNoRaio(raio, out HexCoord apontada) || !apontada.Equals(casa)) continue;
                    alvo = casa;
                    soltar = ponto;
                    achou = true;
                    break;
                }
                Assert.IsTrue(achou, "nenhuma casa valida ficou projetada acima da mao");
                yield return Ponteiro(mouse, centro, true);
                yield return Ponteiro(mouse, soltar, true);
                Assert.AreEqual("Solte para construir",
                    grupo.Find("Validade da colocacao").GetComponentInChildren<TMP_Text>().text);
                yield return Ponteiro(mouse, soltar, false);
                Assert.IsTrue(ui.mapa.TryGet(alvo, out Plataforma colocada));
                Assert.AreSame(mola, colocada.Def);
                Assert.IsTrue(ilhaAlta.DisponivelParaCompra(ui.mapa));
                Assert.AreEqual(0, mao.Pendentes);
                Assert.AreEqual(0, mao.EspacosLivres, "a pendente preencheu o espaco consumido");
                Assert.IsFalse(maoVisual.GetChild(4).gameObject.activeSelf);

                mao.status.ReceberDano(9999);
                yield return null;
                Assert.IsTrue(ui.mapa.diretor.Terminou);
                Assert.IsFalse(grupo.gameObject.activeSelf);
                yield return Ponteiro(mouse, centro, true);
                Assert.IsNull(ui.CartaArrastada);
            }
            finally
            {
                InputSystem.RemoveDevice(mouse);
                InputSystem.settings = configuracaoOriginal;
                Object.Destroy(configuracao);
                Object.Destroy(hud.gameObject);
            }
        }

        static IEnumerator Ponteiro(Mouse mouse, Vector2 posicao, bool pressionado)
        {
            mouse.MakeCurrent();
            InputSystem.QueueStateEvent(mouse, new MouseState
            {
                position = posicao,
                buttons = (ushort)(pressionado ? 1 : 0),
            });
            InputSystem.Update();
            Assert.AreEqual(posicao, mouse.position.ReadValue(), "o evento deve chegar ao dispositivo de teste");
            yield return null;
            yield return null;
        }
    }
}

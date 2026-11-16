using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Loopia.Hex.Tests
{
    /// <summary>
    /// A interface aprovada (cartas do Codex + HUD da jornada) instalada nas cenas oficiais: presenca, unicidade,
    /// botoes nos metodos runtime, nada Editor-only nas cenas do build e os paineis novos na vitoria e na derrota.
    /// </summary>
    public class InterfaceIntegradaTests
    {
        const string Jogo = "Assets/Scenes/HexPrototipo.unity";
        const string Acampamento = "Assets/Scenes/Acampamento.unity";

        [TearDown]
        public void RestaurarCenaEditMode()
        {
            if (!EditorApplication.isPlaying) CenaDoTeste.Restaurar();
        }

        static void Abrir(string cena)
        {
            CenaDoTeste.Registrar();
            EditorSceneManager.OpenScene(cena, OpenSceneMode.Single);
        }

        static T[] Todos<T>() where T : Object =>
            Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        static void UmEventSystemComInputSystem()
        {
            var sistemas = Todos<EventSystem>();
            Assert.AreEqual(1, sistemas.Length, "exatamente um EventSystem");
            Assert.IsNotNull(sistemas[0].GetComponent<InputSystemUIInputModule>(), "modulo do Input System");
            Assert.AreEqual(1, sistemas[0].GetComponents<BaseInputModule>().Length, "um so modulo de entrada");
        }

        static void Clique(Button botao, System.Type alvo, string metodo)
        {
            Assert.AreEqual(1, botao.onClick.GetPersistentEventCount(), botao.name + ": um listener");
            Assert.AreEqual(metodo, botao.onClick.GetPersistentMethodName(0));
            Assert.IsInstanceOf(alvo, botao.onClick.GetPersistentTarget(0));
        }

        [Test, Category("SceneStatic")]
        public void HexPrototipo_TemUmaHudDeCartasLigadaAoCartasUIEUmaInterfaceDaJornada()
        {
            Abrir(Jogo);
            var huds = Todos<HudDeCartas>();
            Assert.AreEqual(1, huds.Length, "uma HUD de cartas");
            var ui = Todos<CartasUI>().Single();
            Assert.AreSame(ui, huds[0].origem);
            Assert.IsTrue(PrefabUtility.IsPartOfPrefabInstance(huds[0]), "instancia do prefab do Codex");
            Assert.AreEqual("Assets/Arte/Cartas/HUD de Cartas.prefab",
                PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(huds[0]));
            var hudSo = new SerializedObject(huds[0]);
            Assert.IsNotNull(hudSo.FindProperty("catalogo").objectReferenceValue, "catalogo");
            Assert.IsNotNull(hudSo.FindProperty("prefabDeCarta").objectReferenceValue, "prefab da carta");

            var interfaces = Todos<InterfaceDaJornada>();
            Assert.AreEqual(1, interfaces.Length, "uma interface da jornada");
            var so = new SerializedObject(interfaces[0]);
            foreach (string campo in new[] { "diretor", "encontro", "barraAntiga", "cameraIso", "jornada", "chefe", "dica" })
                Assert.IsNotNull(so.FindProperty(campo).objectReferenceValue, "InterfaceDaJornada." + campo);

            // Mao, mapa, grade, mundo e status continuam na cena (a HUD so apresenta).
            Assert.IsNotNull(ui.mao);
            Assert.IsNotNull(ui.mapa);
            Assert.IsNotNull(ui.mundo);
            Assert.AreEqual(1, Todos<MaoDeCartas>().Length);
            Assert.AreEqual(1, Todos<MapaDePlataformas>().Length);
            Assert.AreEqual(1, Todos<HexPlacementGrid>().Length);
            Assert.AreEqual(1, Todos<HexWorld>().Length);
            Assert.AreEqual(1, Todos<LuccaStatus>().Length);
            Assert.IsTrue(ui.mao.baralho.Count > 0, "baralho");
        }

        [Test, Category("SceneStatic")]
        public void HexPrototipo_CanvasEEntradaSemDuplicatas()
        {
            Abrir(Jogo);
            UmEventSystemComInputSystem();
            var raizes = Todos<Canvas>().Where(c => c.isRootCanvas).ToArray();
            Assert.AreEqual(3, raizes.Length, string.Join(", ", raizes.Select(c => c.name)));
            foreach (var c in raizes)
            {
                Assert.AreEqual(RenderMode.ScreenSpaceOverlay, c.renderMode, c.name);
                var scaler = c.GetComponent<CanvasScaler>();
                Assert.AreEqual(CanvasScaler.ScaleMode.ScaleWithScreenSize, scaler.uiScaleMode, c.name);
                Assert.AreEqual(new Vector2(1920, 1080), scaler.referenceResolution, c.name);
            }
            // So o resultado recebe clique; jornada e cartas nao interceptam a mao (o CartasUI le o ponteiro).
            Assert.IsNull(Todos<InterfaceDaJornada>()[0].GetComponent<GraphicRaycaster>());
            Assert.IsNull(Todos<HudDeCartas>()[0].GetComponent<GraphicRaycaster>());
            var tela = Todos<TelaDeMorte>().Single();
            Assert.IsNotNull(tela.GetComponentInParent<Canvas>().GetComponent<GraphicRaycaster>());
            Assert.Greater(tela.GetComponentInParent<Canvas>().sortingOrder, Todos<HudDeCartas>()[0].GetComponent<Canvas>().sortingOrder,
                "o resultado fica acima das cartas");
        }

        [Test, Category("SceneStatic")]
        public void HexPrototipo_ResultadoUsaOsMetodosRuntimeEOFadeDaEstabilizacao()
        {
            Abrir(Jogo);
            var tela = Todos<TelaDeMorte>().Single();
            var diretor = Todos<LoopDirector>().Single();
            Assert.AreEqual(0.35f, tela.fade, 1e-4f, "fade da estabilizacao");
            Assert.IsFalse(tela.painel.activeSelf, "o resultado comeca escondido");
            Assert.IsNotNull(tela.painel.GetComponent<ResultadoDaJornadaVisual>());
            Assert.IsNull(tela.painel.GetComponent<CanvasGroup>(), "a TelaDeMorte cria o grupo do fade; nao ha outro fade");
            Clique(tela.painel.GetComponentsInChildren<Button>(true).Single(), typeof(TelaDeMorte), "VoltarAoAcampamento");
            Assert.AreEqual("Mostrar", diretor.aoGameOver.GetPersistentMethodName(0));
            Assert.AreSame(tela, diretor.aoGameOver.GetPersistentTarget(0));
            Assert.AreEqual("MostrarVitoria", diretor.aoVencer.GetPersistentMethodName(0));
            Assert.AreSame(tela, diretor.aoVencer.GetPersistentTarget(0));
            Assert.AreEqual("Acampamento", tela.cenaDoAcampamento);
        }

        [Test, Category("SceneStatic")]
        public void Acampamento_TemaNovoComBotaoRuntimeEUmEventSystem()
        {
            Abrir(Acampamento);
            UmEventSystemComInputSystem();
            var tela = Todos<TelaDoAcampamento>().Single();
            Assert.IsTrue(tela.arquivoSoComF3, "caminho do save so com F3");
            Assert.AreEqual("HexPrototipo", tela.cenaDaJornada);
            Clique(tela.GetComponentsInChildren<Button>(true).Single(), typeof(TelaDoAcampamento), "NovaJornada");
            var titulo = tela.GetComponentsInChildren<TMP_Text>(true).Single(t => t.name == "Titulo");
            Assert.AreEqual("Loopia", titulo.text);
            Assert.IsTrue(titulo.font.name.StartsWith("Marcellus"), titulo.font.name);
        }

        [Test, Category("SceneStatic")]
        public void CenasDoBuild_SemNavegacaoDaPreviaNemComponentesEditorOnly()
        {
            string guid = AssetDatabase.AssetPathToGUID("Assets/Scripts/Hex/Hud/NavegacaoDaPreviaHud.cs");
            Assert.IsFalse(string.IsNullOrEmpty(guid));
            var cenas = EditorBuildSettings.scenes.Where(c => c.enabled).Select(c => c.path).ToArray();
            CollectionAssert.AreEqual(new[] { Acampamento, Jogo }, cenas);
            foreach (string cena in cenas)
                StringAssert.DoesNotContain(guid, File.ReadAllText(cena), cena + " referencia NavegacaoDaPreviaHud");

            // O componente da previa so existe no Editor; o build nunca o compila.
            string fonte = File.ReadAllText("Assets/Scripts/Hex/Hud/NavegacaoDaPreviaHud.cs");
            Assert.IsTrue(fonte.TrimStart().StartsWith("#if UNITY_EDITOR"));
        }

        // --- Play Mode: os paineis novos nos dois finais ---

        [UnityTearDown]
        public IEnumerator Encerrar()
        {
            Time.timeScale = 1f;
            if (EditorApplication.isPlaying) yield return new ExitPlayMode();
            SaveIsolado.Restaurar();
            CenaDoTeste.Restaurar();
        }

        static TMP_Text Texto(GameObject raiz, string nome) =>
            raiz.GetComponentsInChildren<TMP_Text>(true).First(t => t.name == nome);

        static IEnumerator EsperarFade(TelaDeMorte tela)
        {
            float fim = Time.realtimeSinceStartup + 3f;
            var grupos = tela.painel.GetComponentsInChildren<CanvasGroup>(true);
            Assert.AreEqual(1, grupos.Length, "um unico fade no resultado");
            while (grupos[0].alpha < 1f && Time.realtimeSinceStartup < fim) yield return null;
            Assert.AreEqual(1f, grupos[0].alpha, 1e-4f, "o fade termina");
        }

        static void ConferirResultado(TelaDeMorte tela, string titulo, string trechoDoResumo)
        {
            Assert.IsTrue(tela.painel.activeInHierarchy, "resultado visivel");
            Assert.AreEqual(titulo, Texto(tela.painel, "Titulo").text);
            StringAssert.Contains(trechoDoResumo, Texto(tela.painel, "Desfecho").text);
            StringAssert.StartsWith("Moedas nesta jornada", Texto(tela.painel, "Moedas da jornada").text);
            Assert.IsFalse(tela.textoResumo.gameObject.activeSelf, "o resumo antigo fica escondido");
            var botao = tela.painel.GetComponentsInChildren<Button>(true).Single();
            Assert.IsTrue(botao.IsActive() && botao.IsInteractable(), "botao clicavel");

            var jornada = Object.FindFirstObjectByType<InterfaceDaJornada>();
            foreach (Transform filho in jornada.transform)
                Assert.IsFalse(filho.gameObject.activeSelf, filho.name + " some no fim da jornada");
            var cartas = Object.FindFirstObjectByType<HudDeCartas>().transform.Find("Apresentacao da mao");
            Assert.IsTrue(cartas == null || !cartas.gameObject.activeSelf, "as cartas somem no fim da jornada");
        }

        [UnityTest, Category("PlayModeSmoke")]
        public IEnumerator Derrota_ResultadoNovo_VoltaAoAcampamentoPeloBotaoRuntime()
        {
            SaveIsolado.Backup();
            CenaDoTeste.AbrirCenaPrincipal();
            yield return new EnterPlayMode();
            Application.runInBackground = true;
            yield return null;
            yield return null;
            Object.FindFirstObjectByType<InimigoSpawner>().nascerMorcegos = false;
            Object.FindFirstObjectByType<HexLoopRunner>().andando = false;

            var hud = Object.FindFirstObjectByType<HudDeCartas>();
            Assert.IsNotNull(hud.transform.Find("Apresentacao da mao"), "a HUD do Codex assumiu a mao na cena oficial");
            Assert.IsFalse(Object.FindFirstObjectByType<LoopDirector>().mostrarPainelNaTela,
                "o painel IMGUI cede lugar a interface da jornada");
            var jornada = Object.FindFirstObjectByType<InterfaceDaJornada>();
            Assert.IsTrue(jornada.transform.Find("Painel da jornada").gameObject.activeSelf, "painel da jornada visivel");

            var tela = Object.FindFirstObjectByType<TelaDeMorte>();
            Object.FindFirstObjectByType<LuccaStatus>().ReceberDano(9999);
            yield return null;
            ConferirResultado(tela, "Fim da jornada", "Lucca caiu na volta 1");
            yield return EsperarFade(tela);

            tela.painel.GetComponentsInChildren<Button>(true).Single().onClick.Invoke();
            yield return null;
            yield return null;
            Assert.AreEqual("Acampamento", SceneManager.GetActiveScene().name);
            var acampamento = Object.FindFirstObjectByType<TelaDoAcampamento>();
            Assert.IsNotNull(acampamento);
            Assert.IsFalse(acampamento.textoArquivo.gameObject.activeSelf, "caminho do save escondido sem F3");
            StringAssert.StartsWith("Moedas", acampamento.textoMoedas.text);
            yield return new ExitPlayMode();
        }

        [UnityTest, Category("PlayModeSmoke")]
        public IEnumerator Vitoria_BarraDoChefeNovaEResultadoNovo()
        {
            SaveIsolado.Backup();
            CenaDoTeste.AbrirCenaPrincipal();
            yield return new EnterPlayMode();
            Application.runInBackground = true;
            yield return null;
            yield return null;
            var inimigos = Object.FindFirstObjectByType<InimigoSpawner>();
            inimigos.nascerMorcegos = false;
            inimigos.LimparTodos();
            var corredor = Object.FindFirstObjectByType<HexLoopRunner>();
            corredor.andando = false;
            var mundo = Object.FindFirstObjectByType<HexWorld>();
            var encontro = Object.FindFirstObjectByType<EncontroDoChefe>();
            encontro.usarRevoada = false;
            yield return null;

            for (int v = 0; v < 4; v++) Reflexao.Chamar(corredor, "AoEntrarNoHexagono", mundo.Acampamento);
            Assert.AreEqual(EstadoDaPartida.Chefe, Object.FindFirstObjectByType<LoopDirector>().Estado);
            yield return null;
            yield return null;

            var jornada = Object.FindFirstObjectByType<InterfaceDaJornada>();
            var barra = jornada.transform.Find("Barra do chefe");
            Assert.IsTrue(barra.gameObject.activeSelf, "barra nova do chefe visivel no confronto");
            Assert.IsFalse(Object.FindFirstObjectByType<BarraDeVidaDoChefe>().enabled, "a barra IMGUI cede lugar");
            Inimigo chefe = encontro.Chefe;
            Assert.AreEqual($"{chefe.PvAtual} / {chefe.PvMaximo}", Texto(barra.gameObject, "PV do chefe").text);

            var tela = Object.FindFirstObjectByType<TelaDeMorte>();
            chefe.ReceberDano(chefe.PvMaximo);
            yield return null;
            ConferirResultado(tela, "Chefe derrotado", "venceu depois de 4 voltas");
            Assert.IsFalse(barra.gameObject.activeSelf, "a barra some com o chefe");
            yield return EsperarFade(tela);
            yield return new ExitPlayMode();
        }
    }
}

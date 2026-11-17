using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Loopia.Hex.Tests
{
    /// <summary>Pass A na cena HexPrototipo (sem Play, sem salvar): perfil ligado, camera, inimigos e validador.</summary>
    [Category("SceneStatic")]
    public class EscalaDeArteSceneTests
    {
        Scene _cena;

        [OneTimeSetUp]
        public void AbrirCena()
        {
            CenaDoTeste.AbrirCenaPrincipal();
            _cena = SceneManager.GetActiveScene();
        }

        [OneTimeTearDown]
        public void Restaurar() => CenaDoTeste.Restaurar();

        static T Unico<T>() where T : UnityEngine.Object
        {
            T[] todos = UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.AreEqual(1, todos.Length, "esperava exatamente um " + typeof(T).Name + " na cena");
            return todos[0];
        }

        [Test]
        public void DecoracaoDeIlha_TemOPerfilDeEscalaAtribuido()
        {
            var d = Unico<DecoracaoDeIlha>();
            Assert.IsNotNull(d.perfil, "DecoracaoDeIlha.perfil nao atribuido (sem perfil vale o comportamento antigo)");
            Assert.AreEqual("Assets/Dados/Arte/EscalaDeArte.asset", AssetDatabase.GetAssetPath(d.perfil));
            Assert.AreSame(d, Unico<HexWorld>().decoracao, "HexWorld.decoracao tem que ser este DecoracaoDeIlha");
        }

        [Test]
        public void PerfilDeEscala_GiroDaCameraBateComHexIsoCamera()
        {
            var d = Unico<DecoracaoDeIlha>();
            var cam = Unico<HexIsoCamera>();
            Assert.AreEqual(cam.giro, d.perfil.giroDaCamera, 1e-4f,
                "perfil.giroDaCamera precisa ser igual a HexIsoCamera.giro (a regra 'lado longe da camera' depende disso)");
        }

        [Test]
        public void PerfilDeEscala_InclinacaoDaCameraBateComHexIsoCamera()
        {
            Assert.AreEqual(Unico<HexIsoCamera>().inclinacao, Unico<DecoracaoDeIlha>().perfil.inclinacaoDaCamera, 1e-4f);
        }

        [Test]
        public void HexIsoCamera_MargemDoPassA()
        {
            var cam = Unico<HexIsoCamera>();
            // Distancia >= 70: com ortho menor, max(distancia, 3*ortho) cairia abaixo de 70 e o anel entraria na
            // ShadowDistance do URP (50), mudando sombras/batches. Pass A tem que ser neutro de iluminacao.
            Assert.GreaterOrEqual(cam.distancia, 70f);
            Assert.AreEqual(EnquadramentoDaCameraTests.MargemDaCena, cam.margem, 1e-4f);
            Assert.IsTrue(cam.enquadrarAutomaticamente);
            Assert.IsTrue(cam.ortografica);
            Assert.AreEqual(30f, cam.inclinacao, 1e-4f);
            Assert.AreEqual(45f, cam.giro, 1e-4f);
        }

        [Test]
        public void PerfilDeEscala_EntradasValidas()
        {
            var perfil = Unico<DecoracaoDeIlha>().perfil;
            Assert.Greater(perfil.modelos.Count, 0);
            foreach (var m in perfil.modelos)
            {
                Assert.IsNotNull(m, "entrada nula");
                Assert.IsNotNull(m.prefab, "entrada sem prefab (" + m.categoria + ")");
                Assert.Greater(m.peso, 0f, m.prefab.name + ": peso");
                Assert.IsTrue(perfil.TryRegra(m.categoria, out RegraDeCategoria r), m.prefab.name + ": categoria sem regra");
                Assert.Greater(r.tamanhoMinimo, 0f);
                Assert.GreaterOrEqual(r.tamanhoMaximo, r.tamanhoMinimo);
                Bounds b = VisualDeIlha.LimitesLocais(m.prefab.transform);
                Assert.Greater(VisualDeIlha.Medida(b.size, false, r.eixo), 0.0001f, m.prefab.name + ": sem bounds no eixo " + r.eixo);
            }
            // As 6 arvores/pedras/cogumelos/troncos atuais continuam todos no perfil.
            foreach (string nome in new[] { "Arvore", "Pedra", "CogumeloMarrom", "CogumeloVermelho", "CepoDeMadeira", "TroncoDeMadeira" })
                Assert.IsTrue(perfil.modelos.Exists(x => x.prefab != null && x.prefab.name == nome), nome + " ficou fora do perfil");
            Assert.IsTrue(perfil.modelos.Exists(x => x.categoria == CategoriaDeArte.ArvoreGrande), "falta ArvoreGrande");
        }

        [Test]
        public void ValidadorDeEscala_NaoReportaNenhumErro()
        {
            var r = ValidadorDeEscala.Validar();
            Debug.Log("[Testes] ValidadorDeEscala: " + r.perfis + " perfil(is), " + r.erros + " erros, " + r.avisos + " avisos, " + r.infos + " infos\n" + r);
            Assert.GreaterOrEqual(r.perfis, 1);
            Assert.AreEqual(0, r.erros, "o validador achou erros:\n" + r);
        }

        static InimigoDef Def(string nome) => AssetDatabase.LoadAssetAtPath<InimigoDef>("Assets/Dados/" + nome + ".asset");

        [Test]
        public void Lobo_TemAlturaEComprimentoDentroDaSceneBible()
        {
            var lobo = Def("Lobo");
            Assert.IsNotNull(lobo.prefab);
            Bounds b = VisualDeIlha.LimitesLocais(lobo.prefab.transform);
            float f = lobo.escala / Mathf.Max(b.size.x, b.size.y, b.size.z);
            float altura = b.size.y * f;
            float comprimento = Mathf.Max(b.size.x, b.size.z) * f;
            Assert.That(altura, Is.InRange(0.8f, 1.1f), "altura do lobo (Bible 0.8 a 1.1 L)");
            Assert.LessOrEqual(comprimento, 1.6f, "comprimento do lobo (Bible <= 1.6 L)");
        }

        [Test]
        public void Morcego_TemEscalaDaSceneBible()
        {
            // Sprint 5B: com o modelo animado (Bat_Common) a escala e a envergadura. A 0,6 o morcego ficava com 18 px na
            // camera oficial em 1080p, abaixo do criterio "morcego >= 20 px" da propria Bible (secao 5); 0,85 = 26 px.
            // Comparacao 0,60 / 0,85 / 1,034: docs/sprints/SPRINT-5B-ANIMATION-INTEGRATION.md (Escala).
            Assert.That(Def("Morcego").escala, Is.InRange(0.8f, 0.9f), "morcego 0.8 a 0.9 L de envergadura (Bible, Sprint 5B)");
        }
    }
}

using System.IO;
using NUnit.Framework;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;

namespace Loopia.Hex.Tests
{
    /// <summary>
    /// Pass B na cena HexPrototipo (sem Play, sem salvar): o fundo e o FundoDePaisagem, configurado, fora de "Mundo";
    /// a maquete antiga e as sobras inativas sairam; nada gerado foi salvo no arquivo da cena; os FBX tem Read/Write
    /// (CombineMeshes em build).
    /// </summary>
    [Category("SceneStatic")]
    public class FundoDePaisagemSceneTests
    {
        [OneTimeSetUp]
        public void AbrirCena() => CenaDoTeste.AbrirCenaPrincipal();

        [OneTimeTearDown]
        public void Restaurar() => CenaDoTeste.Restaurar();

        static FundoDePaisagem Fundo()
        {
            var todos = Object.FindObjectsByType<FundoDePaisagem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.AreEqual(1, todos.Length, "esperava exatamente um FundoDePaisagem na cena");
            return todos[0];
        }

        [Test]
        public void FundoDePaisagem_ConfiguradoEForaDoMundo()
        {
            var f = Fundo();
            Assert.IsTrue(f.gameObject.activeInHierarchy && f.enabled);
            Assert.IsNull(f.transform.parent, "o fundo fica na raiz da cena (fora de Mundo)");
            Assert.AreEqual(Vector3.zero, f.transform.position);
            foreach (var lista in new[] { f.arvores, f.arvoresAltas, f.secas, f.copaLeve, f.moitas, f.capim, f.pedrasPequenas, f.pedrasMedias })
            {
                Assert.IsNotNull(lista); Assert.IsNotEmpty(lista);
                foreach (var m in lista) Assert.IsNotNull(m, "modelo nao atribuido");
            }
            Assert.AreEqual(3, f.folhas.Length); Assert.AreEqual(3, f.copas.Length);
            foreach (var m in f.folhas) Assert.IsNotNull(m);
            foreach (var m in f.copas) Assert.IsNotNull(m);
            Assert.IsNotNull(f.acento); Assert.IsNotNull(f.solo); Assert.IsNotNull(f.agua);
            Assert.IsNotNull(f.Gerado, "deveria estar gerado no Editor ao abrir a cena");
        }

        [Test]
        public void NavMesh_ColetaSoFilhosDoMundo_FundoNaoEFilho()
        {
            var mundo = Object.FindFirstObjectByType<HexWorld>();
            var superficie = mundo.GetComponent<NavMeshSurface>();
            Assert.AreEqual(CollectObjects.Children, superficie.collectObjects, "se o NavMesh passar a coletar tudo, o fundo entraria no rebake");
            Assert.IsFalse(Fundo().transform.IsChildOf(mundo.transform));
        }

        [Test]
        public void MaqueteAntigaESobras_ForamRemovidas()
        {
            Assert.IsEmpty(Object.FindObjectsByType<CenarioContinuo>(FindObjectsInactive.Include, FindObjectsSortMode.None));
            Assert.IsEmpty(Object.FindObjectsByType<RioDeFundo>(FindObjectsInactive.Include, FindObjectsSortMode.None));
            Assert.IsEmpty(Object.FindObjectsByType<FundoContinuo>(FindObjectsInactive.Include, FindObjectsSortMode.None));
            var raizes = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var g in raizes)
            {
                Assert.AreNotEqual("Cenario background", g.name);
                Assert.IsFalse(g.name.StartsWith("QuadCloud"), g.name);
                Assert.IsFalse(g.name == "Plane" || g.name == "Cube" || g.name == "IlhaFlutuante", "sobra inativa na cena: " + g.name);
            }
        }

        [Test]
        public void ArquivoDaCena_NaoGuardaOGerado()
        {
            string texto = File.ReadAllText(CenaDoTeste.CenaPrincipal);
            StringAssert.DoesNotContain(FundoDePaisagem.NomeDoGerado, texto, "malhas/objetos gerados nao podem ser serializados");
            StringAssert.DoesNotContain("paisagem_", texto);
        }

        [Test]
        public void ModelosUsados_TemReadWrite()
        {
            var f = Fundo();
            foreach (var lista in new[] { f.arvores, f.arvoresAltas, f.secas, f.copaLeve, f.moitas, f.capim, f.pedrasPequenas, f.pedrasMedias })
            foreach (var m in lista)
            {
                var imp = (ModelImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(m));
                Assert.IsTrue(imp.isReadable, m.name + ": sem Read/Write o CombineMeshes falha no build");
            }
        }
    }
}

using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Loopia.Hex.Tests
{
    /// <summary>
    /// Integracao do G1 na HexPrototipo, sem Play: o encontro existe uma vez, as referencias e o UnityEvent da
    /// vitoria estao no Inspector. Nao salva a cena.
    /// </summary>
    [Category("SceneStatic")]
    public class EncontroDoChefeSceneTests
    {
        [OneTimeSetUp]
        public void Abrir() => CenaDoTeste.AbrirCenaPrincipal();

        [OneTimeTearDown]
        public void Restaurar() => CenaDoTeste.Restaurar();

        static T Unico<T>() where T : Object
        {
            T[] todos = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.AreEqual(1, todos.Length, "esperava exatamente um " + typeof(T).Name + " na HexPrototipo");
            return todos[0];
        }

        [Test]
        public void Encontro_UmSo_ComBarraEIndicadorNoMesmoObjeto()
        {
            var encontro = Unico<EncontroDoChefe>();
            Assert.AreSame(encontro.gameObject, Unico<BarraDeVidaDoChefe>().gameObject);
            Assert.AreSame(encontro.gameObject, Unico<IndicadorDePerigoDoChefe>().gameObject);
            Assert.IsTrue(encontro.gameObject.activeSelf);
            Assert.IsNull(Object.FindFirstObjectByType<MedidorDaPartida>(FindObjectsInactive.Include),
                "o medidor e so de teste: nao fica na cena");
        }

        [Test]
        public void Referencias_LigadasNoInspector()
        {
            var encontro = Unico<EncontroDoChefe>();
            var diretor = Unico<LoopDirector>();
            Assert.AreSame(encontro, diretor.encontro, "LoopDirector.encontro");
            Assert.AreSame(diretor, encontro.diretor);
            Assert.AreSame(Unico<InimigoSpawner>(), encontro.inimigos);
            Assert.AreSame(Unico<HexLoopGenerator>(), encontro.gerador);
            Assert.AreSame(Unico<LuccaCombate>(), encontro.combate);
            Assert.AreSame(Unico<LuccaStatus>(), encontro.status);
            Assert.AreSame(diretor, Unico<MapaDePlataformas>().diretor, "MapaDePlataformas.diretor (cartas no fim)");
            Assert.AreSame(diretor, Unico<LuccaCombate>().diretor, "LuccaCombate.diretor (para de atirar no fim)");
            Assert.IsNull(encontro.chefe, "sem asset de chefe: usa a variante do morcego");
            Assert.IsNotNull(Unico<InimigoSpawner>().morcego, "a variante precisa do morcego do spawner");
        }

        [Test]
        public void AoVencer_ChamaTelaDeMorteMostrarVitoria()
        {
            var diretor = Unico<LoopDirector>();
            Assert.AreEqual(1, diretor.aoVencer.GetPersistentEventCount(), "aoVencer sem listener");
            Assert.IsInstanceOf<TelaDeMorte>(diretor.aoVencer.GetPersistentTarget(0));
            Assert.AreEqual("MostrarVitoria", diretor.aoVencer.GetPersistentMethodName(0));
            Assert.AreEqual(1, diretor.aoGameOver.GetPersistentEventCount(), "aoGameOver continua com o listener da derrota");
            Assert.AreEqual("Mostrar", diretor.aoGameOver.GetPersistentMethodName(0));
        }

        [Test]
        public void Cena_AbreSemFicarSuja()
        {
            Assert.IsFalse(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().isDirty);
        }
    }
}

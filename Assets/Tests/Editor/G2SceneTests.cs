using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Loopia.Hex.Tests
{
    /// <summary>G2 na HexPrototipo salva (sem Play): as cartas de sobrevivencia estao no baralho da mao.</summary>
    [Category("SceneStatic")]
    public class G2SceneTests
    {
        [OneTimeSetUp]
        public void Abrir() => CenaDoTeste.AbrirCenaPrincipal();

        [OneTimeTearDown]
        public void Fechar() => CenaDoTeste.Restaurar();

        [Test]
        public void Baralho_TemAsCartasDeSobrevivencia_EMantemAsAntigas()
        {
            var maos = Object.FindObjectsByType<MaoDeCartas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.AreEqual(1, maos.Length);
            var nomes = maos[0].baralho.Where(c => c != null).Select(c => c.name).ToList();
            foreach (var n in new[] { "Santuario", "FonteRestauradora", "OficinaDeArqueiro" })
                CollectionAssert.Contains(nomes, n, n + " fora do baralho");
            foreach (var n in new[] { "Basica", "Coracao", "Flechas", "Moedas", "Floresta", "CampoPacifico", "Cabana",
                                      "Torre", "AreaDeTreino", "Mola", "IlhaAlta" })
                CollectionAssert.Contains(nomes, n, n + " sumiu do baralho");
            Assert.AreEqual(nomes.Count, nomes.Distinct().Count(), "carta repetida no baralho");
        }
    }
}

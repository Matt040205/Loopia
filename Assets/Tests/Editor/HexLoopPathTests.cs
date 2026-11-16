using System.Collections.Generic;
using NUnit.Framework;

namespace Loopia.Hex.Tests
{
    [Category("Logic")]
    public class HexLoopPathTests
    {
        // Anel de raio 1 em torno da origem: 6 casas, cada uma vizinha da proxima e a ultima da primeira.
        static HexLoopPath AnelMinimo()
        {
            var casas = new List<HexCoord>();
            for (int i = 0; i < 6; i++) casas.Add(HexCoord.Zero.Neighbor(i));
            var p = new HexLoopPath();
            p.Definir(casas);
            return p;
        }

        [Test]
        public void Indexador_EhCircular_ComNegativosEMaiorQueCount()
        {
            HexLoopPath p = AnelMinimo();
            Assert.AreEqual(6, p.Count);

            Assert.AreEqual(p[0], p[6]);
            Assert.AreEqual(p[3], p[6 * 7 + 3]);
            Assert.AreEqual(p[5], p[-1]);
            Assert.AreEqual(p[0], p[-6]);
            Assert.AreEqual(p[5], p[-7]);
            Assert.AreEqual(p[1], p[-5]);
        }

        [Test]
        public void Normalizar_SempreDevolveIndiceValido()
        {
            HexLoopPath p = AnelMinimo();
            for (int i = -40; i <= 40; i++)
            {
                int n = p.Normalizar(i);
                Assert.That(n, Is.InRange(0, 5), "indice " + i);
                Assert.AreEqual(((i % 6) + 6) % 6, n);
            }
        }

        [Test]
        public void Normalizar_CaminhoVazioDevolveZero()
        {
            Assert.AreEqual(0, new HexLoopPath().Normalizar(17));
            Assert.AreEqual(0, new HexLoopPath().Normalizar(-3));
        }

        [Test]
        public void TryIndiceDe_AchaCasasDoCaminhoERecusaAsOutras()
        {
            HexLoopPath p = AnelMinimo();
            for (int i = 0; i < 6; i++)
            {
                Assert.IsTrue(p.TryIndiceDe(p[i], out int indice));
                Assert.AreEqual(i, indice);
                Assert.IsTrue(p.Contem(p[i]));
            }

            Assert.IsFalse(p.TryIndiceDe(HexCoord.Zero, out _), "o centro nao faz parte do anel");
            Assert.IsFalse(p.Contem(new HexCoord(9, 9)));
        }

        [Test]
        public void TryProxima_AndaNosDoisSentidosEDaVolta()
        {
            HexLoopPath p = AnelMinimo();
            Assert.IsTrue(p.TryProxima(p[5], +1, out HexCoord depois));
            Assert.AreEqual(p[0], depois, "depois da ultima volta a primeira");
            Assert.IsTrue(p.TryProxima(p[0], -1, out HexCoord antes));
            Assert.AreEqual(p[5], antes);
            Assert.IsFalse(p.TryProxima(new HexCoord(9, 9), +1, out _));
        }

        [Test]
        public void Definir_SubstituiOConteudoAnterior()
        {
            HexLoopPath p = AnelMinimo();
            HexCoord antiga = p[2];
            p.Definir(new List<HexCoord> { new HexCoord(10, 10), new HexCoord(11, 10) });
            Assert.AreEqual(2, p.Count);
            Assert.IsFalse(p.Contem(antiga));
            Assert.IsTrue(p.Contem(new HexCoord(11, 10)));
        }
    }
}

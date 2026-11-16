using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Loopia.Hex.Tests
{
    /// <summary>Matematica pura da grade hexagonal: HexCoord e HexLayout.</summary>
    [Category("Logic")]
    public class HexCoordTests
    {
        static IEnumerable<HexCoord> Grade(int raio)
        {
            for (int q = -raio; q <= raio; q++)
                for (int r = -raio; r <= raio; r++)
                    yield return new HexCoord(q, r);
        }

        [Test]
        public void Distance_EhSimetricaENaoNegativa()
        {
            foreach (HexCoord a in Grade(4))
                foreach (HexCoord b in Grade(3))
                {
                    int d = HexCoord.Distance(a, b);
                    Assert.AreEqual(d, HexCoord.Distance(b, a), a + " <-> " + b);
                    Assert.GreaterOrEqual(d, 0);
                    Assert.AreEqual(a == b, d == 0);
                }
        }

        [Test]
        public void Distance_RespeitaDesigualdadeTriangular()
        {
            var amostra = new List<HexCoord>(Grade(2));
            foreach (var a in amostra)
                foreach (var b in amostra)
                    foreach (var c in amostra)
                        Assert.LessOrEqual(HexCoord.Distance(a, c), HexCoord.Distance(a, b) + HexCoord.Distance(b, c));
        }

        [Test]
        public void Vizinhos_SaoSeisDistintosADistanciaUm()
        {
            foreach (HexCoord c in Grade(4))
            {
                var vistos = new HashSet<HexCoord>();
                for (int d = 0; d < 6; d++)
                {
                    HexCoord v = c.Neighbor(d);
                    Assert.AreEqual(1, HexCoord.Distance(c, v), c + " dir " + d);
                    vistos.Add(v);
                }
                Assert.AreEqual(6, vistos.Count, "os 6 vizinhos de " + c + " tem que ser distintos");
            }
        }

        [Test]
        public void Neighbor_DirecaoDaVoltaCompletaEmMod6()
        {
            var c = new HexCoord(2, -3);
            Assert.AreEqual(c.Neighbor(0), c.Neighbor(6));
            Assert.AreEqual(c.Neighbor(5), c.Neighbor(-1));
            Assert.AreEqual(c.Neighbor(1), c.Neighbor(13));
        }

        [Test]
        public void Distance_QuantidadeNoAnelDeRaioNE6N()
        {
            for (int n = 1; n <= 6; n++)
            {
                int total = 0;
                foreach (HexCoord c in Grade(n)) if (HexCoord.Distance(HexCoord.Zero, c) == n) total++;
                Assert.AreEqual(6 * n, total, "anel de raio " + n);
            }
        }

        [Test]
        public void Directions_SaoAntiHorariasDe60EmXZ_ComecandoNoLeste()
        {
            // O DecoracaoDeIlha.PosicaoLivre depende disso: os seis acessos ficam a 0, 60, ... 300 graus.
            var layout = new HexLayout(1f);
            for (int i = 0; i < 6; i++)
            {
                Vector3 p = layout.ToWorld(HexCoord.Zero.Neighbor(i));
                float angulo = Mathf.Atan2(p.z, p.x) * Mathf.Rad2Deg;
                if (angulo < 0f) angulo += 360f;
                Assert.AreEqual(60f * i, angulo, 0.01f, "direcao " + i);
                Assert.AreEqual(HexLayout.Sqrt3, p.magnitude, 0.001f, "distancia entre centros vizinhos com size 1");
            }
        }

        [TestCase(3f)]
        [TestCase(1f)]
        public void ToWorldToHex_RoundTrip(float size)
        {
            var layout = new HexLayout(size);
            foreach (HexCoord c in Grade(12))
            {
                Vector3 p = layout.ToWorld(c);
                Assert.AreEqual(0f, p.y);
                Assert.AreEqual(c, layout.ToHex(p), "size " + size + " " + c);
            }
        }

        [TestCase(3f)]
        [TestCase(1f)]
        public void ToHex_PontosDentroDoHexagonoCaemNaMesmaCasa(float size)
        {
            // 0.8 do apotema: bem dentro do hexagono, em qualquer das 12 direcoes.
            var layout = new HexLayout(size);
            float raio = layout.Width * 0.5f * 0.8f;
            foreach (HexCoord c in Grade(5))
            {
                Vector3 centro = layout.ToWorld(c);
                for (int k = 0; k < 12; k++)
                {
                    float a = Mathf.Deg2Rad * 30f * k;
                    Vector3 p = centro + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * raio;
                    Assert.AreEqual(c, layout.ToHex(p), c + " angulo " + 30 * k);
                }
            }
        }

        [Test]
        public void Layout_MedidasDerivadasDoTamanho()
        {
            var l = new HexLayout(3f);
            Assert.AreEqual(3f * Mathf.Sqrt(3f), l.Width, 1e-4f);
            Assert.AreEqual(6f, l.Height, 1e-4f);
            Assert.AreEqual(4.5f, l.RowSpacing, 1e-4f);
            for (int i = 0; i < 6; i++) Assert.AreEqual(3f, l.Corner(i).magnitude, 1e-4f);

            // Pointy-top: o primeiro vertice fica a -30 graus (lado leste inferior).
            Vector3 c0 = l.Corner(0);
            Assert.AreEqual(-30f, Mathf.Atan2(c0.z, c0.x) * Mathf.Rad2Deg, 1e-3f);
        }

        [Test]
        public void Round_InteirosSaoIdentidade()
        {
            foreach (HexCoord c in Grade(6)) Assert.AreEqual(c, HexCoord.Round(c.q, c.r));
        }

        [Test]
        public void Round_FracionariosVaoParaOMaisProximo()
        {
            Assert.AreEqual(new HexCoord(0, 0), HexCoord.Round(0.2f, 0.2f));
            Assert.AreEqual(new HexCoord(1, 0), HexCoord.Round(0.9f, 0.1f));
            Assert.AreEqual(new HexCoord(1, 0), HexCoord.Round(1.4f, -0.6f));
            Assert.AreEqual(new HexCoord(-2, 1), HexCoord.Round(-1.9f, 1.2f));
        }

        [Test]
        public void Operadores_SomaSubtracaoEscalaIgualdade()
        {
            var a = new HexCoord(2, -1);
            var b = new HexCoord(-3, 4);
            Assert.AreEqual(new HexCoord(-1, 3), a + b);
            Assert.AreEqual(new HexCoord(5, -5), a - b);
            Assert.AreEqual(new HexCoord(6, -3), a * 3);
            Assert.IsTrue(a == new HexCoord(2, -1));
            Assert.IsTrue(a != b);
            Assert.AreEqual(-(a.q + a.r), a.S);
        }
    }
}

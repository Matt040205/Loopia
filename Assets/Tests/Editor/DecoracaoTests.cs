using NUnit.Framework;
using UnityEngine;

namespace Loopia.Hex.Tests
{
    /// <summary>
    /// DecoracaoDeIlha.PosicaoLivre com os valores da cena: tamanho 3, folga 0.3 =>
    /// raio visual = 3 * (1 - 0.3) = 2.1; apotema = raio * sqrt(3)/2 = 1.8187; corredor = 0.4.
    /// Os seis acessos saem do centro nos angulos 0, 60, ... 300 graus (plano XZ, de +X para +Z).
    /// </summary>
    [Category("Logic")]
    public class DecoracaoTests
    {
        const float Raio = 2.1f;       // 3 * (1 - 0.3)
        const float Corredor = 0.4f;   // larguraDoCorredor da cena
        const float Pegada = 0.1f;

        static Vector3 Polar(float grausNoPlanoXZ, float distancia)
        {
            float a = grausNoPlanoXZ * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * distancia;
        }

        [Test]
        public void Centro_EReservado()
        {
            Assert.IsFalse(DecoracaoDeIlha.PosicaoLivre(Vector3.zero, Pegada, Raio, Corredor));
            // |p| - pegada < 0.48 * raio (1.008): em d = 1.0 ainda e reservado.
            Assert.IsFalse(DecoracaoDeIlha.PosicaoLivre(Polar(30f, 1.0f), Pegada, Raio, Corredor));
        }

        [Test]
        public void SobreOsSeisCorredores_ERejeitado()
        {
            for (int i = 0; i < 6; i++)
                Assert.IsFalse(DecoracaoDeIlha.PosicaoLivre(Polar(60f * i, 1.4f), Pegada, Raio, Corredor),
                    "corredor " + i + " (angulo " + 60 * i + ")");
        }

        [Test]
        public void PertoDoCorredor_AteLarguraMaisPegada_ERejeitado()
        {
            // Distancia perpendicular ao corredor de 0 graus = d * sin(a). Com d = 1.4 e a = 10 graus, 0.243 < 0.5.
            Assert.IsFalse(DecoracaoDeIlha.PosicaoLivre(Polar(10f, 1.4f), Pegada, Raio, Corredor));
            // A 25 graus: 1.4 * sin(25) = 0.592 >= 0.4 + 0.1, e o corredor de 60 graus fica a 1.4*sin(35) = 0.80. Livre.
            Assert.IsTrue(DecoracaoDeIlha.PosicaoLivre(Polar(25f, 1.4f), Pegada, Raio, Corredor));
        }

        [Test]
        public void EntreCorredoresPertoDaBorda_NosSeisSetores_EAceito()
        {
            // Mesmos valores que Decorar usa: distancia = raio * 0.8 = 1.68, angulo no meio de dois acessos (30 + 60k).
            // Perpendicular ao corredor mais proximo: 1.68 * sin(30) = 0.84 >= 0.5.
            // Proximidade da borda: 1.68 * cos(30) + 0.1 = 1.555 < 0.99 * apotema (1.8007).
            for (int k = 0; k < 6; k++)
                Assert.IsTrue(DecoracaoDeIlha.PosicaoLivre(Polar(30f + 60f * k, Raio * 0.8f), Pegada, Raio, Corredor),
                    "setor " + k);
        }

        [Test]
        public void ForaDaBorda_OuPegadaGrande_ERejeitado()
        {
            // 2.0 * cos(30) + 0.1 = 1.832 > 1.8007: encosta no lado do hexagono.
            Assert.IsFalse(DecoracaoDeIlha.PosicaoLivre(Polar(30f, 2.0f), Pegada, Raio, Corredor));
            // Pegada de 0.9 invade o corredor (perpendicular 0.84 < 0.4 + 0.9).
            Assert.IsFalse(DecoracaoDeIlha.PosicaoLivre(Polar(30f, 1.68f), 0.9f, Raio, Corredor));
        }
    }
}

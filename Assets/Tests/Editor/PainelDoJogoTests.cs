using System.Collections.Generic;
using NUnit.Framework;

namespace Loopia.Hex.Tests
{
    /// <summary>
    /// Estabilizacao do MVP (P1-2): o painel do LoopDirector separa o que o jogador usa (volta, PV, flechas, nivel,
    /// moedas) dos dados tecnicos (passos, XP, total do save, inimigos e escala), que so aparecem com depuracao
    /// (Editor, Development Build ou F3).
    /// </summary>
    [Category("Logic")]
    public class PainelDoJogoTests
    {
        static LoopDirector.DadosDoPainel Exemplo() => new LoopDirector.DadosDoPainel
        {
            voltas = 2, voltasParaOBoss = 4, temCorredor = true, passos = 12, passosNaVolta = 30, temStatus = true,
            pv = 39, pvMaximo = 50, flechas = 5, flechasMaximo = 15, nivel = 2, xp = 85, xpParaOProximo = 306,
            moedas = 13, moedasTotais = 222, inimigosVivos = 4, multiplicador = 1.1f,
        };

        static List<string> Linhas(LoopDirector.DadosDoPainel d, bool depuracao)
        {
            var linhas = new List<string>();
            LoopDirector.LinhasDoPainel(d, depuracao, linhas);
            return linhas;
        }

        [Test]
        public void Apresentacao_SoOQueOJogadorUsa()
        {
            var linhas = Linhas(Exemplo(), false);
            CollectionAssert.AreEqual(new[] { "Voltas: 2 / 4", "PV: 39 / 50", "Flechas: 5 / 15", "Nivel 2", "Moedas: 13" }, linhas);
            foreach (string l in linhas)
            {
                StringAssert.DoesNotContain("Passos", l);
                StringAssert.DoesNotContain("XP", l);
                StringAssert.DoesNotContain("total", l);
                StringAssert.DoesNotContain("Inimigos", l);
            }
        }

        [Test]
        public void Depuracao_MostraTambemOsDadosTecnicos()
        {
            var linhas = Linhas(Exemplo(), true);
            Assert.AreEqual(7, linhas.Count);
            Assert.AreEqual("Passos: 12 / 30", linhas[1]);
            Assert.AreEqual("Nivel 2  (XP 85/306)", linhas[4]);
            Assert.AreEqual("Moedas: 13  (total 222)", linhas[5]);
            StringAssert.StartsWith("Inimigos: 4 vivos, x1", linhas[6]);
        }

        [Test]
        public void Chefe_AVoltaAvisaOConfronto()
        {
            var d = Exemplo();
            d.voltas = 4; d.chefe = true;
            Assert.AreEqual("Voltas: 4  (CHEFE!)", Linhas(d, false)[0]);
        }

        [Test]
        public void SemLucca_SoAVolta()
        {
            var d = Exemplo();
            d.temStatus = false;
            CollectionAssert.AreEqual(new[] { "Voltas: 2 / 4" }, Linhas(d, false));
        }
    }
}

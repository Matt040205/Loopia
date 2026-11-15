using NUnit.Framework;

namespace Loopia.Hex.Tests
{
    /// <summary>Contagem das metricas de balanceamento (classe pura).</summary>
    [Category("Logic")]
    public class MetricasDaPartidaTests
    {
        [Test]
        public void Voltas_SeparamFlechasDanoEAbatesPorVolta()
        {
            var m = new MetricasDaPartida();
            m.Comecar(10f, 30);
            m.RegistrarDisparo(false);
            m.RegistrarDisparo(false);
            m.RegistrarDano(5);
            m.RegistrarAbate();
            m.FecharVolta(1, 70f, 45);
            m.RegistrarDisparo(false);
            m.FecharVolta(2, 125f, 50);

            Assert.AreEqual(30, m.casasNoAnel);
            Assert.AreEqual(2, m.voltas.Count);
            Assert.AreEqual(60f, m.voltas[0].duracao, 1e-4f);
            Assert.AreEqual(2, m.voltas[0].flechasDisparadas);
            Assert.AreEqual(5, m.voltas[0].danoRecebido);
            Assert.AreEqual(1, m.voltas[0].inimigosDerrotados);
            Assert.AreEqual(45, m.voltas[0].pvAoFechar);
            Assert.AreEqual(55f, m.voltas[1].duracao, 1e-4f);
            Assert.AreEqual(1, m.voltas[1].flechasDisparadas);
        }

        [Test]
        public void Chefe_ContaFlechasRecargasDanoRevoadaETrechoSemFlechas()
        {
            var m = new MetricasDaPartida();
            m.Comecar(0f, 24);
            m.RegistrarRecarga(); // antes do chefe nao conta
            m.ComecarChefe(100f, 50);
            m.RegistrarDisparo(true);
            m.RegistrarDisparo(true);
            m.RegistrarDisparo(false);
            m.RegistrarDano(9);
            m.RegistrarRecarga();
            m.RegistrarRevoada(2);
            m.RegistrarRevoada(0);
            m.FecharVolta(5, 110f, 1); // no chefe nao existe volta

            m.Avancar(1f, true, 3);
            m.Avancar(2f, true, 0);
            m.Avancar(3f, true, 0);
            m.Avancar(1f, true, 15);
            m.Avancar(1f, true, 0);
            m.Avancar(4f, false, 0); // chefe morto nao conta

            m.Terminar(130f, EstadoDaPartida.Vitoria);
            m.RegistrarDisparo(true); // depois do fim nada conta
            m.RegistrarDano(99);

            Assert.AreEqual(0, m.voltas.Count);
            Assert.AreEqual(2, m.flechasNoChefe);
            Assert.AreEqual(1, m.flechasEmOutrosNoChefe);
            Assert.AreEqual(1, m.recargasNoChefe);
            Assert.AreEqual(9, m.danoRecebidoNoChefe);
            Assert.AreEqual(50, m.pvNoInicioDoChefe);
            Assert.AreEqual(2, m.revoadas);
            Assert.AreEqual(2, m.morcegosDaRevoada);
            Assert.AreEqual(6f, m.tempoSemFlechasNoChefe, 1e-4f);
            Assert.AreEqual(5f, m.maiorTrechoSemFlechasNoChefe, 1e-4f);
            Assert.IsTrue(m.PossivelImpasse(5f));
            Assert.IsFalse(m.PossivelImpasse(5.5f));
            Assert.AreEqual(30f, m.duracaoDoChefe, 1e-4f);
            Assert.AreEqual(130f, m.duracaoTotal, 1e-4f);
            Assert.AreEqual("Vitoria", m.resultado);
            StringAssert.Contains("chefe: 30.0s", m.Resumo());
        }

        [Test]
        public void Terminar_SoUmaVez()
        {
            var m = new MetricasDaPartida();
            m.Comecar(0f, 24);
            m.Terminar(10f, EstadoDaPartida.Derrota);
            m.Terminar(20f, EstadoDaPartida.Vitoria);
            Assert.AreEqual("Derrota", m.resultado);
            Assert.AreEqual(10f, m.duracaoTotal, 1e-4f);
            Assert.IsFalse(m.chefeComecou);
        }
    }
}

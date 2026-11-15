using NUnit.Framework;

namespace Loopia.Hex.Tests
{
    /// <summary>
    /// Formato do save, so pelo parser estatico: nenhum teste aqui le ou grava o loopia_save.json real.
    /// </summary>
    [Category("Logic")]
    public class DadosDoJogadorFormatoTests
    {
        [Test]
        public void SaveAntigo_SemVitorias_CarregaComZero()
        {
            string antigo = "{\n    \"moedas\": 42,\n    \"recordeDeVoltas\": 3\n}";
            Assert.IsTrue(DadosDoJogador.TentarLer(antigo, out int moedas, out int recorde, out int vitorias, out string motivo), motivo);
            Assert.AreEqual(42, moedas);
            Assert.AreEqual(3, recorde);
            Assert.AreEqual(0, vitorias);
        }

        [Test]
        public void SaveNovo_LeOsTresCampos()
        {
            string novo = "{\"moedas\":7,\"recordeDeVoltas\":4,\"vitorias\":2}";
            Assert.IsTrue(DadosDoJogador.TentarLer(novo, out int moedas, out int recorde, out int vitorias, out _));
            Assert.AreEqual(7, moedas);
            Assert.AreEqual(4, recorde);
            Assert.AreEqual(2, vitorias);
        }

        [Test]
        public void ValoresNegativos_ViramZero()
        {
            Assert.IsTrue(DadosDoJogador.TentarLer("{\"moedas\":-5,\"recordeDeVoltas\":-1,\"vitorias\":-9}",
                out int moedas, out int recorde, out int vitorias, out _));
            Assert.AreEqual(0, moedas);
            Assert.AreEqual(0, recorde);
            Assert.AreEqual(0, vitorias);
        }

        [TestCase("")]
        [TestCase("   ")]
        [TestCase((string)null)]
        [TestCase("{{{ isto nao e json")]
        public void Ilegivel_FalhaSemExcecao(string conteudo)
        {
            bool ok = true;
            int moedas = -1;
            string motivo = null;
            Assert.DoesNotThrow(() => ok = DadosDoJogador.TentarLer(conteudo, out moedas, out _, out _, out motivo));
            Assert.IsFalse(ok);
            Assert.AreEqual(0, moedas);
            Assert.IsFalse(string.IsNullOrEmpty(motivo));
        }
    }
}

using System;
using System.IO;
using NUnit.Framework;

namespace Loopia.Hex.Tests
{
    /// <summary>
    /// Estabilizacao do MVP (P0-2/P0-3, docs/mvp-stabilization/ISSUE_AUDIT.md): perfil de save por argumento para QA
    /// e migracao do save do produto antigo (GameJam13-06) para a pasta nova. So pastas temporarias; o save real do
    /// jogador nunca e tocado aqui.
    /// </summary>
    [Category("Logic")]
    public class SaveDoJogadorPerfilTests
    {
        string _raiz;

        [SetUp]
        public void CriarRaiz()
        {
            _raiz = Path.Combine(Path.GetTempPath(), "loopia_teste_migracao_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_raiz);
        }

        [TearDown]
        public void ApagarRaiz()
        {
            if (_raiz != null && Directory.Exists(_raiz)) Directory.Delete(_raiz, true);
        }

        [Test]
        public void NomeDoArquivo_SemPerfilEOdeSempre_ComPerfilESeparado()
        {
            Assert.AreEqual("loopia_save.json", DadosDoJogador.NomeDoArquivoDoPerfil(null));
            Assert.AreEqual("loopia_save.json", DadosDoJogador.NomeDoArquivoDoPerfil(""));
            Assert.AreEqual("loopia_save_qa.json", DadosDoJogador.NomeDoArquivoDoPerfil("qa"));
        }

        [Test]
        public void Perfil_LidoDaLinhaDeComando_EmMinusculas()
        {
            Assert.AreEqual("qa", DadosDoJogador.PerfilDaLinhaDeComando(new[] { "Loopia.exe", "-loopia-perfil", "QA" }));
            Assert.AreEqual("validacao_2", DadosDoJogador.PerfilDaLinhaDeComando(
                new[] { "Loopia.exe", "-logFile", "x.log", "-Loopia-Perfil", "validacao_2" }));
        }

        [Test]
        public void Perfil_AusenteOuInvalido_EOSaveDoJogador()
        {
            Assert.IsNull(DadosDoJogador.PerfilDaLinhaDeComando(null));
            Assert.IsNull(DadosDoJogador.PerfilDaLinhaDeComando(new[] { "Loopia.exe" }));
            Assert.IsNull(DadosDoJogador.PerfilDaLinhaDeComando(new[] { "Loopia.exe", "-loopia-perfil" }), "sem valor");
            Assert.IsNull(DadosDoJogador.PerfilDaLinhaDeComando(new[] { "Loopia.exe", "-loopia-perfil", "../save" }), "caminho");
            Assert.IsNull(DadosDoJogador.PerfilDaLinhaDeComando(new[] { "Loopia.exe", "-loopia-perfil", "a b" }), "espaco");
            Assert.IsNull(DadosDoJogador.PerfilDaLinhaDeComando(new[] { "Loopia.exe", "-loopia-perfil", new string('a', 33) }), "longo");
        }

        [Test]
        public void SaveAntigo_EIrmaoDaPastaNova_ENaoExisteSeOProdutoAindaEOAntigo()
        {
            string nova = Path.Combine(_raiz, "DefaultCompany", "Loopia");
            Assert.AreEqual(Path.Combine(_raiz, "DefaultCompany", "GameJam13-06", "loopia_save.json"),
                DadosDoJogador.CaminhoDoSaveAntigo(nova));
            Assert.AreEqual(Path.Combine(_raiz, "DefaultCompany", "GameJam13-06", "loopia_save.json"),
                DadosDoJogador.CaminhoDoSaveAntigo(nova + Path.DirectorySeparatorChar), "barra no fim");
            Assert.IsNull(DadosDoJogador.CaminhoDoSaveAntigo(Path.Combine(_raiz, "DefaultCompany", "GameJam13-06")),
                "antes de renomear o produto, o antigo e o proprio save");
            Assert.IsNull(DadosDoJogador.CaminhoDoSaveAntigo(null));
        }

        [Test]
        public void Migracao_InstalacaoAntiga_CopiaEPreservaOOriginal()
        {
            string antigo = Path.Combine(_raiz, "GameJam13-06", "loopia_save.json");
            string novo = Path.Combine(_raiz, "Loopia", "loopia_save.json");
            Directory.CreateDirectory(Path.GetDirectoryName(antigo));
            const string conteudo = "{\"moedas\": 209, \"recordeDeVoltas\": 6, \"vitorias\": 1}";
            File.WriteAllText(antigo, conteudo);
            DateTime dataDoAntigo = File.GetLastWriteTimeUtc(antigo);

            Assert.AreEqual(DadosDoJogador.ResultadoDaMigracao.Copiado, DadosDoJogador.MigrarSaveAntigo(novo, antigo, out string erro), erro);
            Assert.AreEqual(conteudo, File.ReadAllText(novo));
            Assert.IsTrue(DadosDoJogador.TentarLer(File.ReadAllText(novo), out int moedas, out int recorde, out int vitorias, out _));
            Assert.AreEqual(209, moedas);
            Assert.AreEqual(6, recorde);
            Assert.AreEqual(1, vitorias);
            Assert.AreEqual(conteudo, File.ReadAllText(antigo), "o original nao pode mudar");
            Assert.AreEqual(dataDoAntigo, File.GetLastWriteTimeUtc(antigo));
        }

        [Test]
        public void Migracao_SaveNovoJaExiste_NaoSobrescreve()
        {
            string antigo = Path.Combine(_raiz, "GameJam13-06", "loopia_save.json");
            string novo = Path.Combine(_raiz, "Loopia", "loopia_save.json");
            Directory.CreateDirectory(Path.GetDirectoryName(antigo));
            Directory.CreateDirectory(Path.GetDirectoryName(novo));
            File.WriteAllText(antigo, "{\"moedas\": 1}");
            File.WriteAllText(novo, "{\"moedas\": 500}");

            Assert.AreEqual(DadosDoJogador.ResultadoDaMigracao.NadaAFazer, DadosDoJogador.MigrarSaveAntigo(novo, antigo, out _));
            Assert.AreEqual("{\"moedas\": 500}", File.ReadAllText(novo));
        }

        [Test]
        public void Migracao_InstalacaoNova_NaoCriaNada()
        {
            string antigo = Path.Combine(_raiz, "GameJam13-06", "loopia_save.json");
            string novo = Path.Combine(_raiz, "Loopia", "loopia_save.json");

            Assert.AreEqual(DadosDoJogador.ResultadoDaMigracao.NadaAFazer, DadosDoJogador.MigrarSaveAntigo(novo, antigo, out _));
            Assert.IsFalse(File.Exists(novo));
            Assert.IsFalse(Directory.Exists(Path.GetDirectoryName(novo)), "sem save antigo, nao cria a pasta");
        }
    }
}

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using NUnit.Framework;

namespace Loopia.Hex.Tests
{
    /// <summary>
    /// Estabilizacao do MVP (P0-2): a protecao do save nos testes (SaveIsolado, da integracao) nao e reimplementada,
    /// so verificada. Simula outra worktree no meio de uma suite (trava com outro dataPath e um processo vivo) e confere
    /// que o Backup() recusa antes de tocar em qualquer arquivo e que o backup desta suite nao muda. A trava original e
    /// devolvida no fim.
    /// </summary>
    [Category("Logic")]
    public class SaveIsoladoConcorrenciaTests
    {
        static string Propriedade(string nome) =>
            (string)typeof(SaveIsolado).GetProperty(nome, BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);

        static string Conteudo(string caminho) => File.Exists(caminho) ? File.ReadAllText(caminho) : null;

        [Test]
        public void OutraWorktreeRodando_BackupRecusa_ENaoMexeNoBackupDestaSuite()
        {
            string trava = Propriedade("Trava");
            string backup = Propriedade("ArquivoDeBackup");
            string ausente = Propriedade("MarcaDeAusente");
            Assert.IsTrue(File.Exists(backup) || File.Exists(ausente), "a guarda da suite ja devia ter feito o backup");

            // A trava separa as worktrees pelo dataPath; o PID so precisa ser de um processo vivo (o do Editor serve).
            int vivo = Process.GetCurrentProcess().Id;

            string travaOriginal = Conteudo(trava);
            string backupAntes = Conteudo(backup);
            DateTime? dataDoBackup = File.Exists(backup) ? File.GetLastWriteTimeUtc(backup) : (DateTime?)null;
            try
            {
                File.WriteAllLines(trava, new[] { "C:/outra/worktree/Assets", vivo.ToString() });

                var falha = Assert.Throws<AssertionException>(SaveIsolado.Backup);
                StringAssert.Contains("Outra worktree", falha.Message);
                StringAssert.Contains("C:/outra/worktree/Assets", Conteudo(trava), "a trava da outra worktree nao pode ser tomada");
                Assert.AreEqual(backupAntes, Conteudo(backup), "o backup desta suite mudou");
                if (dataDoBackup.HasValue) Assert.AreEqual(dataDoBackup.Value, File.GetLastWriteTimeUtc(backup));
            }
            finally
            {
                if (travaOriginal != null) File.WriteAllText(trava, travaOriginal);
                else if (File.Exists(trava)) File.Delete(trava);
            }
        }

        [Test]
        public void MesmaSuite_BackupDeNovo_NaoSobrescreveOOriginal()
        {
            string backup = Propriedade("ArquivoDeBackup");
            string antes = Conteudo(backup);
            SaveIsolado.Backup();
            Assert.AreEqual(antes, Conteudo(backup), "Backup() tem que ser idempotente: o primeiro e o original");
        }
    }
}

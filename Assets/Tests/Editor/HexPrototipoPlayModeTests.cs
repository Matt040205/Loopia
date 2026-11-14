using System;
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace Loopia.Hex.Tests
{
    /// <summary>
    /// Smoke tests em Play Mode da cena HexPrototipo. Sao [UnityTest] do assembly de Editor: o Play Mode
    /// e ligado de dentro do teste (EnterPlayMode/ExitPlayMode). Qualquer Debug.LogError durante o
    /// Play Mode faz o teste falhar (comportamento padrao do UTF, mantido de proposito).
    ///
    /// O save do jogador e isolado: DadosDoJogador grava em disco ao fechar volta, morrer e sair do Play.
    /// O backup e feito no UnitySetUp e a restauracao no UnityTearDown, depois de sair do Play Mode.
    /// </summary>
    [Category("PlayModeSmoke")]
    public class HexPrototipoPlayModeTests
    {
        const float EscalaDeTempo = 4f;

        [UnitySetUp]
        public IEnumerator Preparar()
        {
            SaveIsolado.Backup();
            if (!EditorApplication.isPlaying) CenaDoTeste.AbrirCenaPrincipal();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Encerrar()
        {
            Time.timeScale = 1f;
            if (EditorApplication.isPlaying) yield return new ExitPlayMode();
            Time.timeScale = 1f;
            SaveIsolado.Restaurar();
            CenaDoTeste.Restaurar();
            yield return null;
        }

        /// <summary>Espera com limite de tempo REAL, para o teste nunca travar mesmo com timeScale alto.</summary>
        static IEnumerator Esperar(Func<bool> condicao, float segundosReais, Func<string> descricao)
        {
            float fim = Time.realtimeSinceStartup + segundosReais;
            while (!condicao())
            {
                if (Time.realtimeSinceStartup > fim)
                    Assert.Fail("timeout de " + segundosReais + "s esperando: " + descricao());
                yield return null;
            }
        }

        static T Achar<T>() where T : UnityEngine.Object
        {
            var o = UnityEngine.Object.FindFirstObjectByType<T>();
            Assert.IsNotNull(o, "nao achei " + typeof(T).Name + " em Play Mode");
            return o;
        }

        [UnityTest]
        public IEnumerator Cena_GeraAnelValido_TemNavMesh_ELuccaAndaEFechaUmaVolta()
        {
            // Depois do EnterPlayMode o dominio recarrega e as variaveis locais deste metodo se perdem
            // (inclusive as capturadas por lambdas); por isso todo o corpo roda em outro metodo.
            yield return new EnterPlayMode();
            yield return Cena_GeraAnelValido_TemNavMesh_ELuccaAndaEFechaUmaVoltaEmPlayMode();
            yield return new ExitPlayMode();
        }

        static IEnumerator Cena_GeraAnelValido_TemNavMesh_ELuccaAndaEFechaUmaVoltaEmPlayMode()
        {
            Application.runInBackground = true;
            Time.timeScale = 1f;
            yield return null;
            yield return null;

            var gerador = Achar<HexLoopGenerator>();
            var mundo = Achar<HexWorld>();
            var nav = Achar<NavegacaoDoMundo>();
            var player = Achar<HexPlayer>();
            var corredor = Achar<HexLoopRunner>();
            var diretor = Achar<LoopDirector>();
            var status = Achar<LuccaStatus>();

            // 1) Mapa
            Assert.IsNotNull(gerador.Loop, "o gerador nao produziu Loop no Awake");
            Assert.IsTrue(HexLoopGenerator.CicloEhValido(new System.Collections.Generic.List<HexCoord>(gerador.Loop.Casas), out string erro), erro);
            Assert.AreEqual(gerador.Loop.Count, mundo.TotalDeIlhas);
            Assert.AreEqual(gerador.Loop[0], mundo.Acampamento);

            // 2) NavMesh perto do acampamento e um pulo por par de ilhas vizinhas
            Vector3 acampamento = mundo.PosicaoDe(mundo.Acampamento);
            Assert.IsTrue(NavMesh.SamplePosition(acampamento, out NavMeshHit hit, 2f, NavMesh.AllAreas),
                "nao ha NavMesh perto do acampamento " + acampamento);
            Assert.AreEqual(gerador.Loop.Count, nav.Pulos.Count, "um NavMeshLink por casa do anel");

            // 3) Lucca nasce no acampamento
            Assert.AreEqual(mundo.Acampamento, player.Coord, "o Lucca tem que comecar no acampamento");
            Assert.IsTrue(player.Agente.isOnNavMesh, "o agente do Lucca nao esta no NavMesh");

            // 4) Lucca anda: PassosNaVoltaAtual aumenta
            Time.timeScale = EscalaDeTempo;
            yield return Esperar(() => corredor.PassosNaVoltaAtual >= 3 || !status.EstaVivo, 30f,
                () => "PassosNaVoltaAtual>=3 (atual " + corredor.PassosNaVoltaAtual + ", Lucca vivo=" + status.EstaVivo + ")");
            if (!status.EstaVivo) Assert.Inconclusive("o Lucca morreu para os inimigos antes de andar o suficiente (nao e falha de movimento)");
            Assert.GreaterOrEqual(corredor.PassosNaVoltaAtual, 3);
            Assert.AreNotEqual(mundo.Acampamento, player.Coord, "o Lucca nao saiu do acampamento");

            // 5) Fecha ao menos uma volta (limite de 90s reais com timeScale 4)
            yield return Esperar(() => corredor.VoltasCompletas >= 1 || !status.EstaVivo, 90f,
                () => "VoltasCompletas>=1 (passos " + corredor.PassosNaVoltaAtual + "/" + corredor.TamanhoDoLoop + ")");
            if (!status.EstaVivo && corredor.VoltasCompletas < 1)
                Assert.Inconclusive("o Lucca morreu para os inimigos antes de fechar a primeira volta");

            Assert.GreaterOrEqual(corredor.VoltasCompletas, 1);
            Assert.AreEqual(corredor.VoltasCompletas, diretor.VoltasCompletas, "o LoopDirector nao acompanhou o corredor");
            Assert.AreEqual(Mathf.Pow(1f + diretor.crescimentoDosInimigos, diretor.VoltasCompletas),
                diretor.MultiplicadorDosInimigos, 1e-4f,
                "multiplicador dos inimigos (1.10 por volta fechada)");
            if (diretor.VoltasCompletas == 1) Assert.AreEqual(1.10f, diretor.MultiplicadorDosInimigos, 1e-4f);
            Assert.IsFalse(diretor.BossLiberado, "o Boss nao pode sair antes da volta " + diretor.voltasParaOBoss);

            Time.timeScale = 1f;
        }

        [UnityTest]
        public IEnumerator Quatro_Voltas_LiberamOBossUmaUnicaVez()
        {
            // Depois do EnterPlayMode o dominio recarrega e as variaveis locais deste metodo se perdem
            // (inclusive as capturadas por lambdas); por isso todo o corpo roda em outro metodo.
            yield return new EnterPlayMode();
            yield return Quatro_Voltas_LiberamOBossUmaUnicaVezEmPlayMode();
            yield return new ExitPlayMode();
        }

        static IEnumerator Quatro_Voltas_LiberamOBossUmaUnicaVezEmPlayMode()
        {
            Application.runInBackground = true;
            Time.timeScale = 1f;
            yield return null;
            yield return null;

            var diretor = Achar<LoopDirector>();
            var corredor = Achar<HexLoopRunner>();
            var status = Achar<LuccaStatus>();

            // Isola da corrida real: o Lucca para, e as voltas sao disparadas por reflexao no metodo privado
            // que o corredor aciona (FechouUmaVolta). Isso testa a regra do diretor, nao o percurso.
            corredor.andando = false;
            int boss = 0;
            int voltas = 0;
            diretor.AoLiberarBoss += () => boss++;
            diretor.AoCompletarVolta += _ => voltas++;

            status.ReceberDano(10);
            Assert.Less(status.PvAtual, status.PvMaximoTotal);

            for (int v = 1; v <= 3; v++)
            {
                Reflexao.Chamar(diretor, "FechouUmaVolta", v);
                Assert.IsFalse(diretor.BossLiberado, "Boss liberado cedo, volta " + v);
                Assert.AreEqual(0, boss);
                Assert.AreEqual(Mathf.Pow(1.1f, v), diretor.MultiplicadorDosInimigos, 1e-4f, "volta " + v);
            }
            Assert.AreEqual(status.PvMaximoTotal, status.PvAtual, "fechar volta restaura a vida");

            Reflexao.Chamar(diretor, "FechouUmaVolta", 4);
            Assert.IsTrue(diretor.BossLiberado);
            Assert.AreEqual(1, boss, "AoLiberarBoss na 4a volta");

            Reflexao.Chamar(diretor, "FechouUmaVolta", 5);
            Assert.AreEqual(1, boss, "AoLiberarBoss nao pode disparar de novo");
            Assert.AreEqual(5, voltas);
            Assert.AreEqual(5, diretor.VoltasCompletas);
            Assert.AreEqual(Mathf.Pow(1.1f, 5), diretor.MultiplicadorDosInimigos, 1e-4f);

        }

        [UnityTest]
        public IEnumerator Morte_DisparaGameOver_MostraATelaEParaOCorredor()
        {
            // Depois do EnterPlayMode o dominio recarrega e as variaveis locais deste metodo se perdem
            // (inclusive as capturadas por lambdas); por isso todo o corpo roda em outro metodo.
            yield return new EnterPlayMode();
            yield return Morte_DisparaGameOver_MostraATelaEParaOCorredorEmPlayMode();
            yield return new ExitPlayMode();
        }

        static IEnumerator Morte_DisparaGameOver_MostraATelaEParaOCorredorEmPlayMode()
        {
            Application.runInBackground = true;
            Time.timeScale = 1f;
            yield return null;
            yield return null;

            var diretor = Achar<LoopDirector>();
            var corredor = Achar<HexLoopRunner>();
            var status = Achar<LuccaStatus>();
            var tela = Achar<TelaDeMorte>();

            int perdeu = 0;
            diretor.AoPerder += () => perdeu++;
            Assert.IsFalse(tela.painel.activeSelf, "a tela de morte comeca escondida");

            status.ReceberDano(status.PvMaximoTotal + 1000);
            yield return null;

            Assert.IsTrue(diretor.GameOver);
            Assert.AreEqual(1, perdeu);
            Assert.IsFalse(corredor.andando, "no Game Over o corredor para");
            Assert.IsTrue(tela.painel.activeSelf, "o UnityEvent aoGameOver tem que mostrar a TelaDeMorte");

        }
    }
}

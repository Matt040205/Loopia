using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Loopia.Hex.Tests
{
    /// <summary>
    /// Jornada de verdade, sem reflexao: o Lucca anda 4 voltas reais pelo NavMesh, o chefe nasce, o combate
    /// automatico e a Revoada rodam sozinhos ate a vitoria ou a derrota. O spawn comum de morcegos fica
    /// desligado para o Lucca sempre chegar ao chefe (e o resultado nao depender da sorte nas voltas).
    ///
    /// - PlayModeSmoke: valida o fluxo (qualquer final e aceito, desde que o fluxo termine corretamente).
    /// - PlayModeMedicao: cenario com cartas, para comparar metricas. Nao entra na suite padrao (demora).
    ///
    /// As metricas (MedidorDaPartida) vao para o console e para um .txt na pasta temporaria; elas sao
    /// medicoes de UM mapa sorteado, nao numeros de balanceamento.
    /// </summary>
    public class PartidaRealPlayModeTests
    {
        const float EscalaDeTempo = 4f;
        const float LimiteDasVoltas = 420f;
        const float LimiteDoChefe = 300f;

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

        static T Achar<T>() where T : UnityEngine.Object
        {
            var o = UnityEngine.Object.FindFirstObjectByType<T>();
            Assert.IsNotNull(o, "nao achei " + typeof(T).Name + " em Play Mode");
            return o;
        }

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

        [UnityTest, Category("PlayModeSmoke")]
        public IEnumerator QuatroVoltasReais_ChefeNasce_EAJornadaTermina()
        {
            yield return new EnterPlayMode();
            yield return Jornada("sem-cartas", false);
            yield return new ExitPlayMode();
        }

        [UnityTest, Category("PlayModeMedicao")]
        public IEnumerator Medicao_ComFlechasNoCaminhoETorre()
        {
            yield return new EnterPlayMode();
            yield return Jornada("flechas+torre", true);
            yield return new ExitPlayMode();
        }

        static IEnumerator Jornada(string cenario, bool comCartas)
        {
            Application.runInBackground = true;
            Time.timeScale = 1f;
            yield return null;
            yield return null;

            var diretor = Achar<LoopDirector>();
            var corredor = Achar<HexLoopRunner>();
            var status = Achar<LuccaStatus>();
            var inimigos = Achar<InimigoSpawner>();
            var gerador = Achar<HexLoopGenerator>();
            var mapa = Achar<MapaDePlataformas>();
            var mundo = Achar<HexWorld>();

            inimigos.nascerMorcegos = false;
            inimigos.LimparTodos();

            var go = new GameObject("Encontro do chefe (teste)");
            var encontro = go.AddComponent<EncontroDoChefe>();
            go.AddComponent<BarraDeVidaDoChefe>();
            go.AddComponent<IndicadorDePerigoDoChefe>();
            var medidor = go.AddComponent<MedidorDaPartida>();

            if (comCartas)
            {
                var flechas = AssetDatabase.LoadAssetAtPath<PlataformaDef>("Assets/Dados/Plataformas/Flechas.asset");
                var torre = AssetDatabase.LoadAssetAtPath<PlataformaDef>("Assets/Dados/Plataformas/Torre.asset");
                Assert.IsNotNull(flechas);
                Assert.IsNotNull(torre);
                Assert.IsNotNull(mapa.Colocar(flechas, gerador.Loop[gerador.Loop.Count / 2]), "nao deu para colocar Flechas");
                Assert.IsNotNull(mapa.Colocar(torre, CasaLivreFora(gerador, mundo)), "nao deu para colocar Torre");
            }

            int vitoriasAntes = DadosDoJogador.Instancia.Vitorias;
            int estadosChefe = 0;
            bool chefeAntesDaQuarta = false;
            diretor.AoMudarEstado += (_, novo) => { if (novo == EstadoDaPartida.Chefe) estadosChefe++; };
            diretor.AoCompletarVolta += v => { if (v < 4 && diretor.Estado != EstadoDaPartida.Correndo) chefeAntesDaQuarta = true; };
            yield return null; // Start do medidor, da barra e do indicador

            // 1) Quatro voltas reais.
            corredor.andando = true;
            Time.timeScale = EscalaDeTempo;
            yield return Esperar(() => diretor.Estado != EstadoDaPartida.Correndo, LimiteDasVoltas,
                () => "4 voltas reais (voltas " + corredor.VoltasCompletas + ", passos " + corredor.PassosNaVoltaAtual +
                      "/" + corredor.TamanhoDoLoop + ", estado " + diretor.Estado + ")");

            Assert.AreEqual(EstadoDaPartida.Chefe, diretor.Estado, "a 4a volta real tem que abrir o confronto");
            Assert.IsFalse(chefeAntesDaQuarta);
            Assert.AreEqual(4, corredor.VoltasCompletas);
            Assert.AreEqual(4, diretor.VoltasCompletas);
            Assert.AreEqual(1, estadosChefe);
            Assert.IsNotNull(encontro.Chefe, "o chefe nao nasceu");

            // 2) Confronto ate o fim, com o combate automatico real.
            yield return Esperar(() => diretor.Terminou, LimiteDoChefe,
                () => "fim do confronto (chefe " + (encontro.Chefe != null ? encontro.Chefe.PvAtual + " PV" : "-") +
                      ", Lucca " + status.PvAtual + " PV, flechas " + status.FlechasAtuais + ", recargas " +
                      medidor.Metricas.recargasNoChefe + ")");
            Time.timeScale = 1f;
            yield return null;

            MetricasDaPartida m = medidor.Metricas;
            string resumo = "[" + cenario + "] " + m.Resumo();
            Debug.Log(resumo);
            try
            {
                File.WriteAllText(Path.Combine(Path.GetTempPath(), "loopia_metricas_" + cenario + ".txt"), resumo);
            }
            catch (Exception) { /* so registro */ }

            Assert.AreEqual(4, corredor.VoltasCompletas, "nenhuma 5a volta durante o confronto (recargas " + m.recargasNoChefe + ")");
            Assert.AreEqual(4, diretor.VoltasCompletas);
            Assert.AreEqual(1, estadosChefe, "um confronto so");
            Assert.AreEqual(4, m.voltas.Count);
            Assert.IsTrue(m.chefeComecou);
            Assert.AreEqual(diretor.Estado.ToString(), m.resultado);

            if (diretor.Venceu)
            {
                Assert.AreEqual(vitoriasAntes + 1, DadosDoJogador.Instancia.Vitorias, "vitoria salva");
                Assert.Greater(m.flechasNoChefe, 0, "venceu sem atirar no chefe?");
            }
            else
            {
                Assert.IsTrue(diretor.DerrotaNoChefe, "a derrota tem que ter sido no chefe (sem morcegos nas voltas)");
                Assert.AreEqual(vitoriasAntes, DadosDoJogador.Instancia.Vitorias);
            }

            var basica = AssetDatabase.LoadAssetAtPath<PlataformaDef>("Assets/Dados/Plataformas/Basica.asset");
            Assert.IsFalse(mapa.PodeColocar(basica, gerador.Loop[2], out string motivo), "carta depois do fim");
            Assert.AreEqual("a jornada terminou", motivo);
        }

        static HexCoord CasaLivreFora(HexLoopGenerator gerador, HexWorld mundo)
        {
            foreach (HexCoord c in gerador.Loop.Casas)
                for (int d = 0; d < 6; d++)
                {
                    HexCoord v = c.Neighbor(d);
                    if (mundo.PodeConstruir(v)) return v;
                }
            Assert.Fail("nao achei casa livre fora do anel");
            return default;
        }
    }
}

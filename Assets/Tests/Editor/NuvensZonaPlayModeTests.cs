using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Loopia.Hex.Tests
{
    /// <summary>
    /// Nuvens na partida real (Sprint 6): com o mapa gerado, a zona protegida (envoltoria das ilhas na tela) existe e,
    /// em 2 minutos de jogo simulados, nenhuma nuvem visivel passa por ela, nenhuma nasce dentro de outra, e as nuvens
    /// nascem, vivem e renascem.
    /// O tempo do jogo fica parado (timeScale 0): a camada e avancada a mao, em passos de 0,1 s.
    /// </summary>
    [Category("PlayModeSmoke")]
    public class NuvensZonaPlayModeTests
    {
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

        [UnityTest]
        public IEnumerator Nuvens_NuncaPassamPorCimaDoTabuleiro()
        {
            // Depois do EnterPlayMode o dominio recarrega: o corpo roda em outro metodo (padrao da suite).
            yield return new EnterPlayMode();
            yield return EmPlayMode();
            yield return new ExitPlayMode();
        }

        static IEnumerator EmPlayMode()
        {
            Application.runInBackground = true;
            yield return null;
            yield return null;

            var mundo = Object.FindFirstObjectByType<HexWorld>();
            var camada = Object.FindFirstObjectByType<CamadaDeNuvens>();
            Assert.IsNotNull(camada, "a HexPrototipo tem a camada de nuvens");
            float limite = Time.realtimeSinceStartup + 15f;
            while (mundo.TotalDeIlhas == 0 || camada.ZonaProtegida.Count < 3 || camada.NuvensAtivas == 0)
            {
                if (Time.realtimeSinceStartup > limite) Assert.Fail("timeout esperando o anel, a zona e as nuvens");
                yield return null;
            }

            Time.timeScale = 0f;
            yield return null;
            int renascimentosAntes = camada.Renascimentos;
            float pior = float.PositiveInfinity, maisJuntaAoNascer = float.PositiveInfinity;
            for (int passo = 0; passo < 1200; passo++)
            {
                camada.Atualizar(0.1f);
                if (!float.IsPositiveInfinity(camada.MenorFolgaVisivel)) pior = Mathf.Min(pior, camada.MenorFolgaVisivel);
                maisJuntaAoNascer = Mathf.Min(maisJuntaAoNascer, camada.MenorDistanciaAoNascer);
                if (passo % 200 == 0) yield return null;
            }

            Assert.GreaterOrEqual(pior, 0f, "uma nuvem visivel passou por cima do tabuleiro (folga " + pior + ")");
            Assert.GreaterOrEqual(maisJuntaAoNascer, 0.7f,
                "uma nuvem nasceu dentro de outra (distancia " + maisJuntaAoNascer + " da soma dos raios): as duas se fundem");
            Assert.Greater(camada.Renascimentos, renascimentosAntes, "em 2 minutos alguma nuvem tem de nascer de novo");
            Assert.GreaterOrEqual(camada.NuvensAtivas, 4, "a moldura nao pode esvaziar");
            Assert.LessOrEqual(camada.NuvensAtivas, camada.TotalDeNuvens);
            Time.timeScale = 1f;
        }
    }
}

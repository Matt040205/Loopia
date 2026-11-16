using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Loopia.Hex.Tests
{
    /// <summary>
    /// Zoom do jogador na partida real: aproxima mantendo as nuvens presas ao mundo, arrasta e volta ao anel inteiro.
    /// </summary>
    [Category("PlayModeSmoke")]
    public class LegibilidadeDaTelaPlayModeTests
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
        public IEnumerator Zoom_AproximaArrastaEVolta_NuvensFicamPresasAoMundo()
        {
            // Depois do EnterPlayMode o dominio recarrega: o corpo roda em outro metodo (padrao da suite).
            yield return new EnterPlayMode();
            yield return ZoomEmPlayMode();
            yield return new ExitPlayMode();
        }

        static IEnumerator ZoomEmPlayMode()
        {
            Application.runInBackground = true;
            yield return null;
            yield return null;

            var iso = Object.FindFirstObjectByType<HexIsoCamera>();
            var mundo = Object.FindFirstObjectByType<HexWorld>();
            var camada = Object.FindFirstObjectByType<CamadaDeNuvens>();
            Assert.IsNotNull(iso);
            Assert.IsNotNull(mundo);
            Assert.IsNotNull(camada, "a HexPrototipo tem a camada de nuvens");
            Camera cam = iso.GetComponent<Camera>();

            float limite = Time.realtimeSinceStartup + 15f;
            while (mundo.TotalDeIlhas == 0 || iso.MeiaAlturaBase <= 0f || camada.transform.childCount == 0)
            {
                if (Time.realtimeSinceStartup > limite) Assert.Fail("timeout esperando o anel, o enquadramento e as nuvens");
                yield return null;
            }

            // timeScale 0: as nuvens param de andar e de balancar, entao as posicoes ficam comparaveis.
            Time.timeScale = 0f;
            yield return null;
            yield return null;

            float baseOrtho = iso.MeiaAlturaBase;
            Assert.AreEqual(baseOrtho, cam.orthographicSize, 1e-3f, "sem zoom a camera mostra o anel inteiro");
            Vector3[] antes = Posicoes(camada.transform);

            iso.Aproximar(3, new Vector2(Screen.width * 0.7f, Screen.height * 0.4f), imediato: true);
            yield return null;
            yield return null;

            Assert.AreEqual(baseOrtho * Mathf.Pow(iso.passoDoZoom, 3), cam.orthographicSize, 1e-3f);
            Vector3[] depois = Posicoes(camada.transform);
            Assert.AreEqual(antes.Length, depois.Length);
            for (int i = 0; i < antes.Length; i++)
                Assert.Less(Vector3.Distance(antes[i], depois[i]), 1e-2f,
                    "a nuvem " + i + " andou com o zoom (" + antes[i] + " -> " + depois[i] + "): tem que ficar presa ao mundo");

            iso.Arrastar(new Vector2(120f, -60f));
            yield return null;
            Assert.AreNotEqual(Vector2.zero, iso.Deslocamento, "o arraste tem que mover a vista");

            iso.VoltarAoAnelInteiro();
            limite = Time.realtimeSinceStartup + 5f;
            while (iso.Zoom < 1f || iso.Deslocamento != Vector2.zero)
            {
                if (Time.realtimeSinceStartup > limite) Assert.Fail("o zoom nao voltou ao anel inteiro (suavizacao em tempo real)");
                yield return null;
            }
            yield return null;
            Assert.AreEqual(iso.MeiaAlturaBase, cam.orthographicSize, 1e-3f);

            Time.timeScale = 1f;
        }

        static Vector3[] Posicoes(Transform pai)
        {
            var posicoes = new Vector3[pai.childCount];
            for (int i = 0; i < pai.childCount; i++) posicoes[i] = pai.GetChild(i).position;
            return posicoes;
        }
    }
}

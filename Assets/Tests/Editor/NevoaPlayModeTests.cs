using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Loopia.Hex.Tests
{
    /// <summary>
    /// Pass D1 em Play Mode: para varias seeds e os dois aspectos da matriz (16:9 e 21:9), a ilha mais profunda (cantos do bounds de
    /// todo renderer das ilhas) fica antes do inicio da nevoa linear que o controlador escreve nas RenderSettings.
    /// Mesmo esquema de save/cena do HexPrototipoPlayModeTests.
    /// </summary>
    [Category("PlayModeSmoke")]
    public class NevoaPlayModeTests
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
        public IEnumerator IlhasFicamAntesDoInicioDaNevoa_Seeds12345_7_2024_4()
        {
            // O dominio recarrega ao entrar em Play: o corpo roda em outro metodo.
            yield return new EnterPlayMode();
            yield return Medir_EmPlayMode();
            yield return new ExitPlayMode();
        }

        static IEnumerator Medir_EmPlayMode()
        {
            Application.runInBackground = true;
            Time.timeScale = 1f;
            yield return null;
            yield return null;

            var gerador = Object.FindFirstObjectByType<HexLoopGenerator>();
            var camera = Camera.main;
            var iso = camera.GetComponent<HexIsoCamera>();
            var dn = Object.FindFirstObjectByType<DayNightController>();
            Assert.IsNotNull(gerador); Assert.IsNotNull(iso); Assert.IsNotNull(dn);
            Time.timeScale = 0f;
            dn.autoAdvanceTime = false;
            dn.timeOfDay = 12f;
            yield return null;

            Assert.AreEqual(FogMode.Linear, RenderSettings.fogMode, "em Play Mode o controlador tem que escrever nevoa Linear");
            float inicio = RenderSettings.fogStartDistance;
            float pior = 0f;
            gerador.usarSementeFixa = true;
            foreach (int semente in new[] { 12345, 7, 2024, 4 })
            {
                gerador.semente = semente;
                Assert.IsTrue(gerador.Gerar(), "seed " + semente + " nao gerou");
                yield return null;
                foreach (float aspecto in new[] { 16f / 9f, 21f / 9f })
                {
                    camera.aspect = aspecto;
                    iso.Enquadrar();
                    Vector3 origem = camera.transform.position, frente = camera.transform.forward;
                    float maxima = 0f;
                    foreach (HexTile ilha in gerador.mundo.Ilhas)
                    foreach (Renderer r in ilha.GetComponentsInChildren<Renderer>())
                    {
                        Bounds b = r.bounds;
                        for (int i = 0; i < 8; i++)
                        {
                            Vector3 canto = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                            maxima = Mathf.Max(maxima, Vector3.Dot(canto - origem, frente));
                        }
                    }
                    pior = Mathf.Max(pior, maxima);
                    Assert.Less(maxima, inicio, "seed " + semente + " aspecto " + aspecto.ToString("F2") + ": ilha a " + maxima + " u, nevoa comeca em " + inicio);
                }
            }
            camera.ResetAspect();
            Debug.Log("[Testes] profundidade maxima das ilhas nas seeds testadas: " + pior.ToString("F2") + " u; fogStartDistance=" + inicio);
        }
    }
}

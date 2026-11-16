using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace Loopia.Hex.Tests
{
    /// <summary>
    /// Pass B em Play Mode: o fundo e gerado ao carregar a cena, o anel continua sendo gerado, e o NavMesh nao tem nada na
    /// altura do vale (o fundo nao entra no bake). Mesmo esquema de save/cena do HexPrototipoPlayModeTests.
    /// </summary>
    [Category("PlayModeSmoke")]
    public class FundoDePaisagemPlayModeTests
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
        public IEnumerator FundoGeradoEmPlay_AnelIntacto_NavMeshForaDoVale()
        {
            yield return new EnterPlayMode();
            yield return Verificar_EmPlayMode();
            yield return new ExitPlayMode();
        }

        static IEnumerator Verificar_EmPlayMode()
        {
            Application.runInBackground = true;
            Time.timeScale = 1f;
            for (int i = 0; i < 10; i++) yield return null;

            var fundo = Object.FindFirstObjectByType<FundoDePaisagem>();
            Assert.IsNotNull(fundo);
            Assert.IsNotNull(fundo.Gerado, "o fundo tem que ser gerado ao entrar em Play");
            Assert.Greater(fundo.Gerado.GetComponentsInChildren<MeshRenderer>().Length, 10);
            Assert.Greater(fundo.triangulosGerados, 200000);
            Debug.Log("[FundoDePaisagem] Play: " + fundo.triangulosGerados + " tris em " + fundo.milissegundosDaGeracao.ToString("0.0") + " ms");

            var mundo = Object.FindFirstObjectByType<HexWorld>();
            Assert.Greater(mundo.TotalDeIlhas, 0, "o anel deixou de ser gerado");

            var tri = NavMesh.CalculateTriangulation();
            Assert.Greater(tri.vertices.Length, 0, "NavMesh vazio");
            float minY = float.MaxValue;
            foreach (var v in tri.vertices) minY = Mathf.Min(minY, v.y);
            Assert.Greater(minY, fundo.alturaDoChao + 10f, "ha NavMesh na altura do vale: o fundo entrou no bake");
        }
    }
}

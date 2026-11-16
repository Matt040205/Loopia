using System.Collections;
using NUnit.Framework;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Loopia.Hex.Tests
{
    /// <summary>
    /// Pass A em Play Mode: toda decoracao gerada pela cena e arte pura (NavMeshModifier.ignoreFromBuild, sem collider ligado),
    /// o perfil esta em uso e o enquadramento nao depende da arte. Mesmo esquema de save/cena do HexPrototipoPlayModeTests.
    /// </summary>
    [Category("PlayModeSmoke")]
    public class EscalaDeArtePlayModeTests
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
        public IEnumerator Decoracoes_SaoArtePura_SemColliderLigado_ESemEntrarNoBake()
        {
            // O dominio recarrega ao entrar em Play: o corpo roda em outro metodo.
            yield return new EnterPlayMode();
            yield return Decoracoes_EmPlayMode();
            yield return new ExitPlayMode();
        }

        static IEnumerator Decoracoes_EmPlayMode()
        {
            Application.runInBackground = true;
            Time.timeScale = 1f;
            yield return null;
            yield return null;
            yield return null; // os Destroy das decoracoes que nao couberam so valem no fim do frame

            var mundo = Object.FindFirstObjectByType<HexWorld>();
            Assert.IsNotNull(mundo);
            Assert.IsNotNull(mundo.decoracao, "HexWorld.decoracao ausente");
            Assert.IsNotNull(mundo.decoracao.perfil, "perfil de escala nao esta em uso na cena");
            Assert.Greater(mundo.TotalDeIlhas, 0);

            int decoracoes = 0, ilhasComDecoracao = 0, grandes = 0;
            Vector3 frente = mundo.decoracao.perfil.FrenteDaCameraXZ;
            foreach (HexTile ilha in mundo.Ilhas)
            {
                Transform raiz = ilha.transform.Find("Decoracoes");
                if (raiz == null) continue;
                int daIlha = 0;
                foreach (Transform t in raiz)
                {
                    if (!t.gameObject.activeSelf) continue;
                    daIlha++; decoracoes++;
                    var modificador = t.GetComponent<NavMeshModifier>();
                    Assert.IsNotNull(modificador, ilha.Coord + "/" + t.name + " sem NavMeshModifier");
                    Assert.IsTrue(modificador.ignoreFromBuild, ilha.Coord + "/" + t.name + " entra no bake do NavMesh");
                    foreach (var c in t.GetComponentsInChildren<Collider>(true))
                        Assert.IsFalse(c.enabled, ilha.Coord + "/" + t.name + " tem collider ligado: " + c.GetType().Name);
                    // Arvore grande = altura de ~1.8 a 2.4 u: so pode estar no lado longe da camera.
                    Bounds b = VisualDeIlha.LimitesLocais(t);
                    if (t.name == "Arvore" && b.size.y > 1.6f)
                    {
                        grandes++;
                        Assert.Greater(Vector3.Dot(t.localPosition.normalized, frente), 0.2f, ilha.Coord + ": arvore grande perto da camera");
                    }
                }
                if (daIlha > 0) ilhasComDecoracao++;
            }
            Debug.Log("[Testes] Play Mode: " + decoracoes + " decoracoes em " + ilhasComDecoracao + "/" + mundo.TotalDeIlhas + " ilhas, " + grandes + " arvores grandes");
            Assert.Greater(decoracoes, 0, "nenhuma decoracao gerada");
            Assert.AreEqual(mundo.TotalDeIlhas, ilhasComDecoracao, "todas as ilhas deveriam ter pelo menos uma decoracao");
        }
    }
}

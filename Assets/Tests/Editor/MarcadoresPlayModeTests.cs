using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace Loopia.Hex.Tests
{
    /// <summary>
    /// Pass C em Play Mode: colocar a carta cria o marcador como filho da ilha (a Ilha alta sobe e o marcador sobe junto),
    /// a Floresta esconde a decoracao e a devolve ao sair, e o marcador nao entra no NavMesh. Mesmo esquema de save/cena.
    /// </summary>
    [Category("PlayModeSmoke")]
    public class MarcadoresPlayModeTests
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
        public IEnumerator Marcadores_FilhosDaIlha_SobemComORelevo_FlorestaEscondeEDevolveDecoracao()
        {
            yield return new EnterPlayMode();
            yield return Verificar_EmPlayMode();
            yield return new ExitPlayMode();
        }

        // A Mola esta fora do jogo (PlataformaDef.desativada), mas a mecanica segue no codigo: o teste a exercita com uma
        // copia em memoria ligada. O asset nao muda.
        static PlataformaDef D(string nome)
        {
            var d = AssetDatabase.LoadAssetAtPath<PlataformaDef>("Assets/Dados/Plataformas/" + nome + ".asset");
            if (d != null && d.desativada)
            {
                d = UnityEngine.Object.Instantiate(d);
                d.name = nome;
                d.desativada = false;
                d.hideFlags = HideFlags.DontSave;
            }
            return d;
        }

        static IEnumerator Verificar_EmPlayMode()
        {
            Application.runInBackground = true;
            for (int i = 0; i < 5; i++) yield return null;
            var g = Object.FindFirstObjectByType<HexLoopGenerator>();
            var mapa = Object.FindFirstObjectByType<MapaDePlataformas>();
            g.usarSementeFixa = true; g.semente = 12345;
            Assert.IsTrue(g.Gerar());
            // A carta de relevo e bloqueada durante um pulo; este teste so exercita seus marcadores.
            var corredor = Object.FindFirstObjectByType<HexLoopRunner>();
            corredor.andando = false;
            corredor.player.NascerNoAcampamento();
            yield return null;
            mapa.LimparTudo();
            var loop = g.Loop; int n = loop.Count;

            // Floresta: marcador filho da ilha, decoracao escondida; ao remover volta
            var casaF = loop[n / 2 + 3];
            Assert.IsTrue(g.mundo.TryGetIlha(casaF, out HexTile ilhaF));
            var dec = ilhaF.transform.Find("Decoracoes");
            var pf = mapa.Colocar(D("Floresta"), casaF);
            Assert.IsNotNull(pf); Assert.IsNotNull(pf.Marcador);
            Assert.AreSame(ilhaF.transform, pf.Marcador.transform.parent);
            if (dec != null) Assert.IsFalse(dec.gameObject.activeSelf, "a Floresta deveria esconder a decoracao");
            mapa.Remover(casaF);
            yield return null;
            Assert.IsTrue(pf == null || pf.Marcador == null, "marcador ficou para tras");
            if (dec != null) Assert.IsTrue(dec.gameObject.activeSelf, "a decoracao nao voltou");

            // Ilha alta (depois de uma mola): o marcador acompanha a altura da ilha
            Assert.IsNotNull(mapa.Colocar(D("Mola"), loop[n / 4]));
            var casaA = loop[n / 4 + 2];
            var pa = mapa.Colocar(D("IlhaAlta"), casaA);
            Assert.IsNotNull(pa, "nao colocou a Ilha alta");
            yield return null;
            Assert.IsTrue(g.mundo.TryGetIlha(casaA, out HexTile ilhaA));
            Assert.Greater(ilhaA.Altura, 1f);
            Assert.AreEqual(ilhaA.transform.position.y, pa.Marcador.transform.position.y, 1e-3f, "marcador nao subiu com a ilha");

            // nada do marcador no NavMesh: nenhum vertice acima do topo das ilhas + 0,3
            float topo = 0f;
            foreach (HexTile i in g.mundo.Ilhas) topo = Mathf.Max(topo, i.transform.position.y);
            var tri = NavMesh.CalculateTriangulation();
            foreach (var v in tri.vertices) Assert.LessOrEqual(v.y, topo + 0.3f, "o NavMesh subiu em cima de um marcador");
            mapa.LimparTudo();
        }
    }
}

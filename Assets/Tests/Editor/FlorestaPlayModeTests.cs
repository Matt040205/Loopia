using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Loopia.Hex.Tests
{
    /// <summary>
    /// Carta Floresta na HexPrototipo: o lobo nasce na colocacao e e reposto a cada volta, contando so os lobos da casa.
    /// Achado do playtest da Sprint 5B: com um morcego pousado na casa, a floresta contava o morcego como "lobo
    /// presente" e o lobo nunca nascia.
    /// </summary>
    [Category("PlayModeSmoke")]
    public class FlorestaPlayModeTests
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

        static T Achar<T>() where T : Object
        {
            var o = Object.FindFirstObjectByType<T>();
            Assert.IsNotNull(o, "nao achei " + typeof(T).Name);
            return o;
        }

        [UnityTest]
        public IEnumerator Floresta_MorcegoPousadoNaCasa_NaoImpedeOLobo_ENaoEmpilha()
        {
            yield return new EnterPlayMode();
            yield return Floresta_MorcegoPousadoNaCasa_NaoImpedeOLobo_ENaoEmpilhaEmPlayMode();
            yield return new ExitPlayMode();
        }

        // EnterPlayMode recarrega o dominio: o corpo roda num iterador novo, criado ja em Play Mode.
        static IEnumerator Floresta_MorcegoPousadoNaCasa_NaoImpedeOLobo_ENaoEmpilhaEmPlayMode()
        {
            Application.runInBackground = true; // Editor fora de foco congela o Play Mode no frame 2
            yield return null;
            yield return null;
            var inimigos = Achar<InimigoSpawner>();
            var gerador = Achar<HexLoopGenerator>();
            var mapa = Achar<MapaDePlataformas>();
            var mundo = Achar<HexWorld>();
            var player = Achar<HexPlayer>();
            Achar<HexLoopRunner>().andando = false;
            Achar<LuccaCombate>().enabled = false;
            inimigos.nascerMorcegos = false;
            inimigos.LimparTodos();
            player.ColocarEm(mundo.Acampamento);
            yield return null;

            var floresta = AssetDatabase.LoadAssetAtPath<PlataformaFlorestaDef>("Assets/Dados/Plataformas/Floresta.asset");
            Assert.IsNotNull(floresta);
            Assert.IsNotNull(floresta.loboQueNasce);
            Assert.IsTrue(floresta.nascerAoColocar);

            // casa do caminho longe da Lucca, onde a Floresta pode ir
            HexCoord casa = default;
            bool achou = false;
            for (int i = gerador.Loop.Count / 2; i < gerador.Loop.Count && !achou; i++)
            {
                casa = gerador.Loop[i];
                achou = HexCoord.Distance(casa, player.Coord) >= 3 && mapa.PodeColocar(floresta, casa, out _);
            }
            Assert.IsTrue(achou, "nenhuma casa do caminho aceita a Floresta");

            // o morcego pousado na casa (o caso do playtest)
            Inimigo morcego = inimigos.Nascer(inimigos.morcego, casa);
            morcego.enabled = false; // parado no poleiro durante o teste
            Assert.AreEqual(1, inimigos.QuantosEm(casa));
            Assert.AreEqual(0, inimigos.QuantosEm(casa, floresta.loboQueNasce));

            Assert.IsNotNull(mapa.Colocar(floresta, casa));
            Assert.AreEqual(1, inimigos.QuantosEm(casa, floresta.loboQueNasce), "o lobo nao nasceu com o morcego na casa");
            Assert.AreEqual(2, inimigos.QuantosEm(casa), "morcego + lobo");

            // volta fechada: nao empilha
            Reflexao.Chamar(mapa, "VoltaFechou", 1);
            Assert.AreEqual(1, inimigos.QuantosEm(casa, floresta.loboQueNasce), "a floresta empilhou lobos");

            // lobo morto: a proxima volta repoe (o morcego continua la e nao conta)
            Inimigo lobo = null;
            foreach (var i in inimigos.Vivos)
                if (i != null && i.Casa == casa && i.Def == floresta.loboQueNasce) lobo = i;
            Assert.IsNotNull(lobo);
            lobo.ReceberDano(999999);
            yield return null;
            Assert.AreEqual(0, inimigos.QuantosEm(casa, floresta.loboQueNasce));
            Reflexao.Chamar(mapa, "VoltaFechou", 2);
            Assert.AreEqual(1, inimigos.QuantosEm(casa, floresta.loboQueNasce), "a floresta nao repos o lobo");
            Assert.AreEqual(2, inimigos.QuantosEm(casa));
        }
    }
}

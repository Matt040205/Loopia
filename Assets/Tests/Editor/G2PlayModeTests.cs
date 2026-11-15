using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Loopia.Hex.Tests
{
    /// <summary>
    /// G2 na HexPrototipo real, em Play Mode: fila de recompensas com a mao inicial cheia, e as cartas de
    /// sobrevivencia colocadas, substituidas e removidas pelo MapaDePlataformas (o mesmo caminho da UI),
    /// inclusive a recarga de flechas no acampamento durante o chefe. As cartas novas entram em tempo de
    /// execucao (o baralho da cena nao e alterado; nada e salvo). Spawn comum desligado.
    /// </summary>
    [Category("PlayModeSmoke")]
    public class G2PlayModeTests
    {
        const string Pasta = "Assets/Dados/Plataformas/";

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
            Assert.IsNotNull(o, "nao achei " + typeof(T).Name + " em Play Mode");
            return o;
        }

        static T Carta<T>(string nome) where T : PlataformaDef
        {
            var d = AssetDatabase.LoadAssetAtPath<T>(Pasta + nome + ".asset");
            Assert.IsNotNull(d, nome);
            return d;
        }

        static void PararOMundo(out HexLoopRunner corredor, out InimigoSpawner inimigos)
        {
            Application.runInBackground = true;
            Time.timeScale = 1f;
            inimigos = Achar<InimigoSpawner>();
            inimigos.nascerMorcegos = false;
            inimigos.LimparTodos();
            corredor = Achar<HexLoopRunner>();
            corredor.andando = false;
        }

        /// <summary>Casas fora do anel onde da para construir, longe do miolo.</summary>
        static HexCoord[] CasasFora(HexWorld mundo, HexLoopGenerator gerador, int quantas)
        {
            var casas = new System.Collections.Generic.List<HexCoord>();
            foreach (HexCoord c in gerador.Loop.Casas)
                for (int d = 0; d < 6 && casas.Count < quantas; d++)
                {
                    HexCoord v = c.Neighbor(d);
                    if (!casas.Contains(v) && mundo.PodeConstruir(v)) casas.Add(v);
                }
            Assert.AreEqual(quantas, casas.Count, "faltaram casas livres fora do anel");
            return casas.ToArray();
        }

        // --- G2-A ---

        // Como nos outros testes de Play: o corpo fica num metodo separado, chamado depois do EnterPlayMode.
        // Lambdas que capturam variaveis no proprio metodo do [UnityTest] perdem o estado na recarga de dominio.
        [UnityTest]
        public IEnumerator FilaDeRecompensas_MaoInicialCheia_LevelUpFicaPendente_EEntraAoJogarUmaCarta()
        {
            yield return new EnterPlayMode();
            yield return FilaDeRecompensasEmPlayMode();
            yield return new ExitPlayMode();
        }

        static IEnumerator FilaDeRecompensasEmPlayMode()
        {
            yield return null;
            yield return null;
            PararOMundo(out _, out _);

            var mao = Achar<MaoDeCartas>();
            var status = Achar<LuccaStatus>();
            var mapa = Achar<MapaDePlataformas>();
            var gerador = Achar<HexLoopGenerator>();
            Achar<CartasUI>();
            Assert.AreEqual(0, mao.EspacosLivres, "a cena comeca com 4 cartas garantidas");
            int eventos = 0;
            mao.AoMudarPendentes += () => eventos++;

            // Dois morcegos de XP: o nivel 1, o caso que antes perdia a carta.
            status.GanharXp(status.XpParaOProximoNivel);
            Assert.AreEqual(1, status.Nivel);
            Assert.AreEqual(1, mao.Pendentes, "a carta do nivel ficou pendente");
            Assert.AreEqual(0, mao.EspacosLivres);
            yield return null; // um frame de OnGUI com o indicador

            // Joga a carta do espaco 0 (Mola) pelo mesmo caminho da UI: Colocar e depois Gastar.
            PlataformaDef jogada = mao.CartaNoEspaco(0);
            Assert.IsNotNull(jogada);
            HexCoord casa = gerador.Loop[gerador.Loop.Count / 2];
            Assert.IsTrue(mapa.PodeColocar(jogada, casa, out string motivo), motivo);
            Assert.IsNotNull(mapa.Colocar(jogada, casa));
            Assert.IsTrue(mao.Gastar(0));

            Assert.AreEqual(0, mao.Pendentes);
            Assert.IsNotNull(mao.CartaNoEspaco(0), "a pendente ocupou o espaco liberado");
            CollectionAssert.Contains(mao.baralho, mao.CartaNoEspaco(0), "veio do baralho da cena");
            Assert.AreEqual(2, eventos, "entrou e saiu da fila");

            yield return null;
            Assert.AreEqual(0, mao.EspacosLivres, "nenhuma carta extra apareceu depois");

            // Derrota: a fila deixa de entregar, mas nao perde o direito.
            status.ReceberDano(9999);
            var diretor = Achar<LoopDirector>();
            Assert.IsTrue(diretor.Terminou);
            mao.Gastar(1);
            status.GanharXp(10000);
            Assert.Greater(mao.Pendentes, 0);
            Assert.IsNull(mao.CartaNoEspaco(1), "nada entra na mao depois da derrota");
            yield return null;
        }

        // --- G2-B ---

        [UnityTest]
        public IEnumerator CartasDeSobrevivencia_ColocarSubstituirRemover_SemBonusFantasma_EValemNoChefe()
        {
            yield return new EnterPlayMode();
            yield return CartasDeSobrevivenciaEmPlayMode();
            yield return new ExitPlayMode();
        }

        static IEnumerator CartasDeSobrevivenciaEmPlayMode()
        {
            yield return null;
            yield return null;
            PararOMundo(out HexLoopRunner corredor, out _);

            var status = Achar<LuccaStatus>();
            var mapa = Achar<MapaDePlataformas>();
            var mundo = Achar<HexWorld>();
            var gerador = Achar<HexLoopGenerator>();
            var diretor = Achar<LoopDirector>();
            var combate = Achar<LuccaCombate>();

            var santuario = Carta<PlataformaSimplesDef>("Santuario");
            var fonte = Carta<PlataformaSimplesDef>("FonteRestauradora");
            var oficina = Carta<PlataformaSimplesDef>("OficinaDeArqueiro");
            var torre = Carta<PlataformaSimplesDef>("Torre");
            var coracao = Carta<PlataformaRecompensaDef>("Coracao");

            HexCoord[] fora = CasasFora(mundo, gerador, 4);
            int danoBase = combate.DanoDoTiro();

            Assert.IsNotNull(mapa.Colocar(santuario, fora[0]));
            Assert.IsNotNull(mapa.Colocar(oficina, fora[1]));
            Assert.IsNotNull(mapa.Colocar(fonte, fora[2]));
            Assert.AreEqual(53, status.PvMaximoTotal);
            Assert.AreEqual(53, status.PvAtual);
            Assert.AreEqual(17, status.FlechasMaximoTotal);
            Assert.AreEqual(17, status.FlechasAtuais);
            Assert.AreEqual(2, status.BonusDeCuraPorCoracao);
            Assert.AreEqual(danoBase, combate.DanoDoTiro(), "nenhuma das tres mexe no dano");

            // Coracao no caminho, coletado pelo mesmo handler que o HexPlayer dispara: 5 + 2.
            HexCoord casaDoCoracao = gerador.Loop[gerador.Loop.Count / 2];
            Assert.IsNotNull(mapa.Colocar(coracao, casaDoCoracao));
            status.ReceberDano(20);
            Reflexao.Chamar(mapa, "PlayerPassouPor", casaDoCoracao);
            Assert.AreEqual(40, status.PvAtual, "53 - 20 + 7");

            // Substituir a Oficina por uma Torre: o teto volta a 15 e as flechas sao limitadas.
            Assert.IsNotNull(mapa.Colocar(torre, fora[1]));
            Assert.AreEqual(15, status.FlechasMaximoTotal);
            Assert.AreEqual(15, status.FlechasAtuais);
            Assert.Greater(combate.DanoDoTiro(), danoBase, "a Torre que entrou da dano");

            // Remover Santuario e Fonte: nada fica acumulado.
            mapa.Remover(fora[0]);
            mapa.Remover(fora[2]);
            Assert.AreEqual(50, status.PvMaximoTotal);
            Assert.AreEqual(40, status.PvAtual, "remover so limita, nao cura nem fere");
            Assert.AreEqual(0, status.BonusDeCuraPorCoracao);
            Assert.AreEqual(mapa.Modificadores, status.Modificadores);
            Assert.AreEqual(new ModificadoresDoLucca(0, torre.bonusDeDano, 0, 0), status.Modificadores);

            // Recolocar a Oficina e chegar ao chefe: a recarga do acampamento usa o teto efetivo.
            Assert.IsNotNull(mapa.Colocar(oficina, fora[3]));
            Assert.AreEqual(17, status.FlechasMaximoTotal);
            for (int v = 1; v <= 4; v++)
                Reflexao.Chamar(corredor, "AoEntrarNoHexagono", mundo.Acampamento);
            Assert.AreEqual(EstadoDaPartida.Chefe, diretor.Estado);
            Assert.AreEqual(4, diretor.VoltasCompletas);

            for (int i = 0; i < 10; i++) status.GastarFlecha();
            Reflexao.Chamar(corredor, "AoEntrarNoHexagono", mundo.Acampamento);
            Assert.AreEqual(17, status.FlechasAtuais, "recarga no chefe ate 15 + 2");
            Assert.AreEqual(4, diretor.VoltasCompletas, "a passagem no chefe nao conta volta");
            Assert.AreEqual(EstadoDaPartida.Chefe, diretor.Estado);
            yield return null;
        }
    }
}

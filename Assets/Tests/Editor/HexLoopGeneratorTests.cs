using System.Collections.Generic;
using NUnit.Framework;

namespace Loopia.Hex.Tests
{
    /// <summary>
    /// Geracao do anel com parametros iguais aos da cena HexPrototipo (raio 4..6, deformacoes 400,
    /// miolo minimo 9, mundo size 3 / folga 0.3 / espessura 0.6), mas com semente fixa.
    /// O Awake nao roda em Edit Mode, entao o Gerar() e chamado a mao.
    /// </summary>
    [Category("Logic")]
    public class HexLoopGeneratorTests
    {
        // 1..10 + a semente que esta gravada na cena (12345).
        static readonly int[] Sementes = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 12345 };

        MundoDeTeste _m;

        [OneTimeSetUp]
        public void CenaVazia() => CenaDoTeste.NovaCenaVazia();

        [OneTimeTearDown]
        public void Restaurar() => CenaDoTeste.Restaurar();

        [TearDown]
        public void Limpar()
        {
            if (_m != null) _m.Destruir();
            _m = null;
        }

        MundoDeTeste Gerado(int semente)
        {
            _m = MundoDeTeste.Criar(semente);
            Assert.IsTrue(_m.Gerar(), "Gerar() falhou para a semente " + semente);
            return _m;
        }

        [TestCaseSource(nameof(Sementes))]
        public void Gerar_ProduzCicloValido(int semente)
        {
            var m = Gerado(semente);
            Assert.IsNotNull(m.gerador.Loop);
            Assert.IsTrue(HexLoopGenerator.CicloEhValido(new List<HexCoord>(m.gerador.Loop.Casas), out string erro), erro);
            Assert.GreaterOrEqual(m.gerador.Loop.Count, 6);
        }

        [TestCaseSource(nameof(Sementes))]
        public void Gerar_MioloCabeOBossEEhDisjuntoDoAnel(int semente)
        {
            var m = Gerado(semente);
            var anel = new HashSet<HexCoord>(m.gerador.Loop.Casas);
            var miolo = new HashSet<HexCoord>(m.gerador.Miolo);

            Assert.GreaterOrEqual(miolo.Count, m.gerador.mioloMinimo);
            Assert.AreEqual(m.gerador.Miolo.Count, miolo.Count, "miolo sem casas repetidas");

            bool temCasaCercada = false;
            foreach (HexCoord c in miolo)
            {
                Assert.IsFalse(anel.Contains(c), "casa do miolo " + c + " esta no anel");
                bool cercada = true;
                for (int d = 0; d < 6; d++) if (!miolo.Contains(c.Neighbor(d))) cercada = false;
                if (cercada) temCasaCercada = true;
            }
            Assert.IsTrue(temCasaCercada, "nenhuma casa do miolo tem as 6 vizinhas no miolo (Boss nao cabe)");
            Assert.IsTrue(miolo.Contains(m.gerador.CentroDoMiolo), "CentroDoMiolo fora do miolo");
        }

        [TestCaseSource(nameof(Sementes))]
        public void Gerar_MontaMundoComUmAcampamentoEMioloBloqueado(int semente)
        {
            var m = Gerado(semente);
            HexLoopPath loop = m.gerador.Loop;

            Assert.AreEqual(loop.Count, m.mundo.TotalDeIlhas, "uma ilha por casa do anel e mais nada");
            Assert.AreEqual(loop[0], m.mundo.Acampamento);

            int acampamentos = 0;
            foreach (HexTile ilha in m.mundo.Ilhas)
            {
                Assert.IsTrue(loop.Contem(ilha.Coord), "ilha fora do anel: " + ilha.Coord);
                Assert.IsTrue(ilha.EstaNoCaminho);
                if (ilha.Kind == HexTileKind.Acampamento)
                {
                    acampamentos++;
                    Assert.AreEqual(loop[0], ilha.Coord, "o acampamento tem que ficar no indice 0");
                }
            }
            Assert.AreEqual(1, acampamentos);

            foreach (HexCoord c in m.gerador.Miolo)
            {
                Assert.IsTrue(m.mundo.EstaBloqueada(c), "miolo nao bloqueado: " + c);
                Assert.IsFalse(m.mundo.TemIlha(c), "miolo nao pode ter ilha: " + c);
                Assert.IsFalse(m.mundo.PodeConstruir(c));
            }
        }

        [TestCaseSource(nameof(Sementes))]
        public void Gerar_EhDeterministicoComSementeFixa(int semente)
        {
            var m1 = Gerado(semente);
            var primeiro = new List<HexCoord>(m1.gerador.Loop.Casas);
            var mioloPrimeiro = new List<HexCoord>(m1.gerador.Miolo);
            m1.Destruir();
            _m = null;

            var m2 = Gerado(semente);
            CollectionAssert.AreEqual(primeiro, new List<HexCoord>(m2.gerador.Loop.Casas));
            CollectionAssert.AreEqual(mioloPrimeiro, new List<HexCoord>(m2.gerador.Miolo));
        }

        [Test]
        public void Gerar_DuasVezesNoMesmoGeradorRecriaOMundoSemAcumular()
        {
            _m = MundoDeTeste.Criar(7);
            int geradas = 0;
            _m.gerador.AoGerarMapa += () => geradas++;
            Assert.IsTrue(_m.Gerar());
            Assert.IsTrue(_m.Gerar());
            Assert.AreEqual(2, geradas, "AoGerarMapa dispara a cada geracao");
            Assert.AreEqual(_m.gerador.Loop.Count, _m.mundo.TotalDeIlhas, "LimparMundo tem que esvaziar o mundo anterior");
        }

        [Test]
        public void Gerar_SemHexWorldNaCenaFalhaELogaErro()
        {
            var go = new UnityEngine.GameObject("SemMundo");
            try
            {
                var g = go.AddComponent<HexLoopGenerator>();
                UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Error,
                    new System.Text.RegularExpressions.Regex(@"Nenhum HexWorld encontrado"));
                Assert.IsFalse(g.Gerar());
                Assert.IsNull(g.Loop);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        // --- CicloEhValido: casos negativos ---

        static List<HexCoord> AnelDeRaio1()
        {
            var l = new List<HexCoord>();
            for (int i = 0; i < 6; i++) l.Add(HexCoord.Zero.Neighbor(i));
            return l;
        }

        [Test]
        public void CicloEhValido_AnelDeRaio1EhValido()
        {
            Assert.IsTrue(HexLoopGenerator.CicloEhValido(AnelDeRaio1(), out string erro), erro);
        }

        [Test]
        public void CicloEhValido_Nulo()
        {
            Assert.IsFalse(HexLoopGenerator.CicloEhValido(null, out string erro));
            Assert.IsNotEmpty(erro);
        }

        [Test]
        public void CicloEhValido_MenosDe6Casas()
        {
            var l = AnelDeRaio1();
            l.RemoveAt(5);
            Assert.IsFalse(HexLoopGenerator.CicloEhValido(l, out string erro));
            StringAssert.Contains("6", erro);
        }

        [Test]
        public void CicloEhValido_CasaRepetida()
        {
            var l = AnelDeRaio1();
            l[5] = l[0];
            Assert.IsFalse(HexLoopGenerator.CicloEhValido(l, out string erro));
            StringAssert.Contains("repetida", erro);
        }

        [Test]
        public void CicloEhValido_CasasNaoVizinhas()
        {
            var l = AnelDeRaio1();
            var troca = l[1]; l[1] = l[2]; l[2] = troca; // 0 -> 2 deixa de ser adjacente
            Assert.IsFalse(HexLoopGenerator.CicloEhValido(l, out string erro));
            StringAssert.Contains("vizinhas", erro);
        }

        [Test]
        public void CicloEhValido_UltimaCasaNaoFechaNaPrimeira()
        {
            // Dois trechos de 3 casas em linha reta: consecutivas sao vizinhas, mas a ultima nao volta na primeira.
            var l = new List<HexCoord>();
            for (int q = 0; q < 6; q++) l.Add(new HexCoord(q, 0));
            Assert.IsFalse(HexLoopGenerator.CicloEhValido(l, out string erro));
            Assert.IsNotEmpty(erro);
        }

        [Test]
        public void CicloEhValido_AtalhoComTresVizinhasNoCaminho()
        {
            // Anel de raio 1 com a casa (1,1) encaixada entre (1,0) e (0,1): todas as consecutivas sao
            // vizinhas, mas (1,0) passa a encostar em tres casas do caminho (um atalho).
            var l = new List<HexCoord>
            {
                new HexCoord(1, 0), new HexCoord(1, 1), new HexCoord(0, 1),
                new HexCoord(-1, 1), new HexCoord(-1, 0), new HexCoord(0, -1), new HexCoord(1, -1),
            };
            Assert.IsFalse(HexLoopGenerator.CicloEhValido(l, out string erro));
            StringAssert.Contains("3 vizinhas", erro);
        }

        [Test]
        public void CicloEhValido_AnelComCentroTemSeisVizinhas()
        {
            var l = new List<HexCoord> { HexCoord.Zero };
            l.AddRange(AnelDeRaio1());
            Assert.IsFalse(HexLoopGenerator.CicloEhValido(l, out string erro));
            StringAssert.Contains("vizinhas", erro);
        }
    }
}

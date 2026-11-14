using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Loopia.Hex.Tests
{
    /// <summary>
    /// Caracterizacao da mao de cartas. O Awake (que cria o array de espacos) nao roda em Edit Mode;
    /// ele e chamado por reflexao. Foi a opcao menos fragil: o Start procura LuccaStatus/Mapa na cena e
    /// ja compra cartas, o que mudaria o estado inicial; o Awake so aloca o array.
    /// </summary>
    [Category("Logic")]
    public class MaoDeCartasTests
    {
        GameObject _go;
        MaoDeCartas _mao;
        readonly List<ScriptableObject> _defs = new List<ScriptableObject>();

        [OneTimeSetUp]
        public void CenaVazia() => CenaDoTeste.NovaCenaVazia();

        [OneTimeTearDown]
        public void Restaurar() => CenaDoTeste.Restaurar();

        [SetUp]
        public void Montar()
        {
            _go = new GameObject("Mao");
            _mao = _go.AddComponent<MaoDeCartas>();
            Reflexao.Chamar(_mao, "Awake");
        }

        [TearDown]
        public void Limpar()
        {
            foreach (var d in _defs) if (d != null) Object.DestroyImmediate(d);
            _defs.Clear();
            if (_go != null) Object.DestroyImmediate(_go);
        }

        PlataformaSimplesDef Carta(string nome)
        {
            var d = ScriptableObject.CreateInstance<PlataformaSimplesDef>();
            d.nomeExibido = nome;
            _defs.Add(d);
            return d;
        }

        [Test]
        public void MaoComecaVaziaComOsEspacosConfigurados()
        {
            Assert.AreEqual(4, _mao.espacos, "GDD: 4 espacos");
            Assert.AreEqual(4, _mao.Mao.Count);
            Assert.AreEqual(4, _mao.EspacosLivres);
            for (int i = 0; i < 4; i++) Assert.IsNull(_mao.CartaNoEspaco(i));
            Assert.IsNull(_mao.CartaNoEspaco(-1));
            Assert.IsNull(_mao.CartaNoEspaco(4));
        }

        [Test]
        public void DarCarta_PreencheOPrimeiroEspacoLivre()
        {
            var a = Carta("A"); var b = Carta("B"); var c = Carta("C");
            Assert.IsTrue(_mao.DarCarta(a));
            Assert.IsTrue(_mao.DarCarta(b));
            Assert.AreSame(a, _mao.CartaNoEspaco(0));
            Assert.AreSame(b, _mao.CartaNoEspaco(1));

            Assert.IsTrue(_mao.Gastar(0));
            Assert.IsNull(_mao.CartaNoEspaco(0));
            Assert.IsTrue(_mao.DarCarta(c));
            Assert.AreSame(c, _mao.CartaNoEspaco(0), "o buraco do espaco 0 e reaproveitado primeiro");
            Assert.AreEqual(2, _mao.Mao.Count - _mao.EspacosLivres);
        }

        [Test]
        public void DarCarta_Nula_EGastarEspacoVazioOuInvalido_Falham()
        {
            Assert.IsFalse(_mao.DarCarta(null));
            Assert.IsFalse(_mao.Gastar(0), "espaco vazio");
            Assert.IsFalse(_mao.Gastar(-1));
            Assert.IsFalse(_mao.Gastar(99));
        }

        [Test]
        public void DarCarta_ComMaoCheia_RetornaFalseEMantemAMao()
        {
            var cartas = new[] { Carta("1"), Carta("2"), Carta("3"), Carta("4") };
            foreach (var c in cartas) Assert.IsTrue(_mao.DarCarta(c));
            Assert.AreEqual(0, _mao.EspacosLivres);

            Assert.IsFalse(_mao.DarCarta(Carta("5")));
            for (int i = 0; i < 4; i++) Assert.AreSame(cartas[i], _mao.CartaNoEspaco(i));
        }

        [Test]
        public void DarCarta_IndisponivelParaCompra_NaoEntra()
        {
            // Ilha alta so esta disponivel com uma mola no mapa; sem mapa, nunca.
            var relevo = ScriptableObject.CreateInstance<PlataformaRelevoDef>();
            _defs.Add(relevo);
            Assert.IsFalse(_mao.DarCarta(relevo));
            Assert.AreEqual(4, _mao.EspacosLivres);
        }

        [Test]
        public void DarCarta_DisparaAoMudar()
        {
            int mudou = 0;
            _mao.AoMudar += () => mudou++;
            _mao.DarCarta(Carta("A"));
            _mao.Gastar(0);
            Assert.AreEqual(2, mudou);
        }

        [Test]
        public void Comprar_SemBaralho_OuSoComCartasIndisponiveis_RetornaFalse()
        {
            Assert.IsFalse(_mao.Comprar());

            var relevo = ScriptableObject.CreateInstance<PlataformaRelevoDef>();
            _defs.Add(relevo);
            _mao.baralho.Add(relevo);
            _mao.baralho.Add(null);
            Assert.IsFalse(_mao.Comprar());
            Assert.AreEqual(4, _mao.EspacosLivres);
        }

        [Test]
        public void Comprar_PreencheOPrimeiroEspacoLivre_ComCartaDoBaralho()
        {
            var a = Carta("A"); var b = Carta("B");
            _mao.baralho.Add(a);
            _mao.baralho.Add(b);

            Assert.IsTrue(_mao.Comprar());
            Assert.IsNotNull(_mao.CartaNoEspaco(0));
            Assert.That(_mao.CartaNoEspaco(0), Is.SameAs(a).Or.SameAs(b));
            Assert.AreEqual(3, _mao.EspacosLivres);
        }

        [Test]
        public void Characterization_MaoCheia_ComprarFalhaEACartaDoLevelUpESimplesmentePerdida()
        {
            // CARACTERIZACAO, NAO CORRECAO. Documenta o achado da auditoria: "recompensa de level-up e
            // perdida com a mao cheia". Hoje Comprar() devolve false e nada e guardado para depois.
            // Se isto mudar de proposito (fila de recompensas), este teste deve ser atualizado.
            var cartas = new[] { Carta("1"), Carta("2"), Carta("3"), Carta("4") };
            foreach (var c in cartas) _mao.DarCarta(c);
            _mao.baralho.Add(Carta("Recompensa"));

            int mudou = 0;
            _mao.AoMudar += () => mudou++;

            Assert.IsFalse(_mao.Comprar(), "mao cheia: Comprar nao entrega nada");
            Assert.AreEqual(0, _mao.EspacosLivres);
            for (int i = 0; i < 4; i++) Assert.AreSame(cartas[i], _mao.CartaNoEspaco(i), "a mao nao muda");
            Assert.AreEqual(0, mudou, "AoMudar nao dispara");

            // Mesmo caminho que o level-up usa (SubiuDeNivel -> Comprar): continua sem efeito.
            Reflexao.Chamar(_mao, "SubiuDeNivel", 1);
            Assert.AreEqual(0, _mao.EspacosLivres);
            Assert.AreEqual(0, mudou);

            // Liberar um espaco depois NAO devolve a recompensa perdida.
            _mao.Gastar(2);
            Assert.AreEqual(1, _mao.EspacosLivres);
            Assert.IsNull(_mao.CartaNoEspaco(2), "a carta do level-up nao fica pendente");
        }

        [Test]
        public void SubiuDeNivel_ComEspaco_EntregaCartasPorNivel()
        {
            _mao.baralho.Add(Carta("A"));
            _mao.cartasPorNivel = 2;
            Reflexao.Chamar(_mao, "SubiuDeNivel", 1);
            Assert.AreEqual(2, _mao.Mao.Count - _mao.EspacosLivres);
        }
    }
}

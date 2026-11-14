using NUnit.Framework;
using UnityEngine;

namespace Loopia.Hex.Tests
{
    /// <summary>
    /// Regras de PV/XP do Lucca. Nao toca em moedas (GanharMoedas grava o save via DadosDoJogador).
    /// O Awake e chamado por reflexao porque nao roda em Edit Mode.
    /// </summary>
    [Category("Logic")]
    public class LuccaStatusTests
    {
        GameObject _go;
        LuccaStatus _s;

        [OneTimeSetUp]
        public void CenaVazia() => CenaDoTeste.NovaCenaVazia();

        [OneTimeTearDown]
        public void Restaurar() => CenaDoTeste.Restaurar();

        [SetUp]
        public void Montar()
        {
            _go = new GameObject("Lucca");
            _s = _go.AddComponent<LuccaStatus>();
            Reflexao.Chamar(_s, "Awake");
        }

        [TearDown]
        public void Limpar() => Object.DestroyImmediate(_go);

        [Test]
        public void EstadoInicial_PvMunicaoENivel()
        {
            Assert.AreEqual(50, _s.PvAtual);
            Assert.AreEqual(50, _s.PvMaximoTotal);
            Assert.AreEqual(15, _s.FlechasAtuais);
            Assert.AreEqual(0, _s.Nivel);
            Assert.IsTrue(_s.EstaVivo);
        }

        [Test]
        public void ReceberDano_ZeraEDisparaAoMorrerUmaVez_ENaoRessuscita()
        {
            int mortes = 0;
            _s.AoMorrer += () => mortes++;

            _s.ReceberDano(20);
            Assert.AreEqual(30, _s.PvAtual);
            _s.ReceberDano(999);
            Assert.AreEqual(0, _s.PvAtual);
            Assert.IsFalse(_s.EstaVivo);
            Assert.AreEqual(1, mortes);

            _s.ReceberDano(5);
            _s.Curar(10);
            _s.RestaurarVidaEMunicao();
            Assert.AreEqual(0, _s.PvAtual, "morto nao e curado nem restaurado");
            Assert.AreEqual(1, mortes);
        }

        [Test]
        public void Curar_NaoPassaDoMaximo_ERestaurarVoltaVidaEFlechas()
        {
            _s.ReceberDano(10);
            _s.Curar(5);
            Assert.AreEqual(45, _s.PvAtual);
            _s.Curar(100);
            Assert.AreEqual(50, _s.PvAtual);

            _s.ReceberDano(30);
            _s.GastarFlecha();
            _s.RestaurarVidaEMunicao();
            Assert.AreEqual(50, _s.PvAtual);
            Assert.AreEqual(15, _s.FlechasAtuais);
        }

        [Test]
        public void BonusDePvMaximo_SobeOPvAtualJuntoComOTeto()
        {
            _s.DefinirBonusDePvMaximo(2);
            Assert.AreEqual(52, _s.PvMaximoTotal);
            Assert.AreEqual(52, _s.PvAtual);
            _s.DefinirBonusDePvMaximo(0);
            Assert.AreEqual(50, _s.PvMaximoTotal);
            Assert.AreEqual(50, _s.PvAtual, "o PV atual e limitado ao novo teto");
        }

        [Test]
        public void Flechas_GastarAteZerar_GanharLimitadoAoMaximo()
        {
            for (int i = 0; i < 15; i++) Assert.IsTrue(_s.GastarFlecha());
            Assert.IsFalse(_s.GastarFlecha());
            _s.GanharFlechas(10);
            Assert.AreEqual(10, _s.FlechasAtuais);
            _s.GanharFlechas(10);
            Assert.AreEqual(15, _s.FlechasAtuais);
        }

        [Test]
        public void GanharXp_SobeVariosNiveisDeUmaVez_EDisparaAoSubirDeNivel()
        {
            var niveis = new System.Collections.Generic.List<int>();
            _s.AoSubirDeNivel += n => niveis.Add(n);

            _s.GanharXp(99);
            Assert.AreEqual(0, _s.Nivel);
            _s.GanharXp(1);
            Assert.AreEqual(1, _s.Nivel);
            Assert.AreEqual(0, _s.Xp);
            Assert.AreEqual(175, _s.XpParaOProximoNivel, "100 * 1.75");

            _s.GanharXp(10000);
            CollectionAssert.AreEqual(Enumerable(niveis.Count), niveis, "niveis consecutivos, sem pular");
            Assert.Greater(_s.Nivel, 2);
        }

        static System.Collections.Generic.List<int> Enumerable(int total)
        {
            var l = new System.Collections.Generic.List<int>();
            for (int i = 1; i <= total; i++) l.Add(i);
            return l;
        }
    }
}

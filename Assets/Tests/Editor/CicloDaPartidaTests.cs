using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Loopia.Hex.Tests
{
    /// <summary>Maquina de estados da jornada (classe pura, sem cena).</summary>
    [Category("Logic")]
    public class CicloDaPartidaTests
    {
        static readonly EstadoDaPartida[] Todos = (EstadoDaPartida[])Enum.GetValues(typeof(EstadoDaPartida));

        [Test]
        public void Comeca_EmPreparando_SemContarVoltas()
        {
            var c = new CicloDaPartida();
            Assert.AreEqual(EstadoDaPartida.Preparando, c.Estado);
            Assert.IsFalse(c.Terminou);
            Assert.IsFalse(c.ContaVoltas);
        }

        [Test]
        public void FluxoCompleto_PreparandoCorrendoChefeVitoria_UmEventoPorTransicao()
        {
            var c = new CicloDaPartida();
            var vistas = new List<string>();
            c.AoMudar += (de, para) => vistas.Add(de + ">" + para);

            Assert.IsTrue(c.Comecar());
            Assert.IsTrue(c.ContaVoltas, "so Correndo conta volta");
            Assert.IsTrue(c.ComecarChefe());
            Assert.IsFalse(c.ContaVoltas, "no chefe a volta nao conta");
            Assert.IsTrue(c.RegistrarVitoria());

            Assert.AreEqual(EstadoDaPartida.Vitoria, c.Estado);
            Assert.IsTrue(c.Terminou);
            Assert.AreEqual(EstadoDaPartida.Chefe, c.EstadoAntesDoFim);
            CollectionAssert.AreEqual(new[] { "Preparando>Correndo", "Correndo>Chefe", "Chefe>Vitoria" }, vistas);
        }

        [Test]
        public void CadaTransicao_SoAconteceUmaVez()
        {
            var c = new CicloDaPartida();
            int eventos = 0;
            c.AoMudar += (_, __) => eventos++;

            Assert.IsTrue(c.Comecar());
            Assert.IsFalse(c.Comecar(), "comecar de novo");
            Assert.IsTrue(c.ComecarChefe());
            Assert.IsFalse(c.ComecarChefe(), "o chefe nao comeca duas vezes");
            Assert.IsTrue(c.RegistrarVitoria());
            Assert.IsFalse(c.RegistrarVitoria(), "vitoria duas vezes");
            Assert.AreEqual(3, eventos);
        }

        [Test]
        public void Vitoria_SoDoChefe()
        {
            var c = new CicloDaPartida();
            Assert.IsFalse(c.RegistrarVitoria(), "vitoria em Preparando");
            c.Comecar();
            Assert.IsFalse(c.RegistrarVitoria(), "vitoria sem chefe");
            Assert.AreEqual(EstadoDaPartida.Correndo, c.Estado);
        }

        [Test]
        public void Chefe_SoDepoisDeCorrer()
        {
            var c = new CicloDaPartida();
            Assert.IsFalse(c.ComecarChefe());
            Assert.AreEqual(EstadoDaPartida.Preparando, c.Estado);
        }

        [TestCase(EstadoDaPartida.Preparando)]
        [TestCase(EstadoDaPartida.Correndo)]
        [TestCase(EstadoDaPartida.Chefe)]
        public void Derrota_DeQualquerEstadoNaoTerminal_GuardaDeOndeVeio(EstadoDaPartida de)
        {
            var c = Levar(de);
            Assert.IsTrue(c.RegistrarDerrota());
            Assert.AreEqual(EstadoDaPartida.Derrota, c.Estado);
            Assert.AreEqual(de, c.EstadoAntesDoFim);
            Assert.IsFalse(c.ContaVoltas);
        }

        [Test]
        public void VitoriaEDerrota_SaoMutuamenteExclusivas_ValeAPrimeira()
        {
            var venceu = Levar(EstadoDaPartida.Chefe);
            venceu.RegistrarVitoria();
            int eventos = 0;
            venceu.AoMudar += (_, __) => eventos++;
            Assert.IsFalse(venceu.RegistrarDerrota(), "morrer depois de vencer");
            Assert.AreEqual(EstadoDaPartida.Vitoria, venceu.Estado);

            var perdeu = Levar(EstadoDaPartida.Chefe);
            perdeu.RegistrarDerrota();
            perdeu.AoMudar += (_, __) => eventos++;
            Assert.IsFalse(perdeu.RegistrarVitoria(), "o chefe cair depois da morte do Lucca");
            Assert.IsFalse(perdeu.RegistrarDerrota(), "morrer duas vezes");
            Assert.AreEqual(EstadoDaPartida.Derrota, perdeu.Estado);

            Assert.AreEqual(0, eventos, "estado terminal nao dispara evento");
        }

        [Test]
        public void TabelaDeTransicoes_Exata()
        {
            var validas = new HashSet<string>
            {
                "Preparando>Correndo", "Correndo>Chefe", "Chefe>Vitoria",
                "Preparando>Derrota", "Correndo>Derrota", "Chefe>Derrota",
            };
            foreach (EstadoDaPartida de in Todos)
                foreach (EstadoDaPartida para in Todos)
                {
                    string chave = de + ">" + para;
                    Assert.AreEqual(validas.Contains(chave), CicloDaPartida.TransicaoValida(de, para), chave);
                }
        }

        static CicloDaPartida Levar(EstadoDaPartida alvo)
        {
            var c = new CicloDaPartida();
            if (alvo == EstadoDaPartida.Preparando) return c;
            c.Comecar();
            if (alvo == EstadoDaPartida.Correndo) return c;
            c.ComecarChefe();
            return c;
        }
    }
}

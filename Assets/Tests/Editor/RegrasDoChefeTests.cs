using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Loopia.Hex.Tests
{
    /// <summary>
    /// Alcance do chefe no centro do miolo e a variante ampliada do morcego. Os mapas usam o mesmo gerador
    /// e os parametros da cena (MundoDeTeste), com as sementes do HexLoopGeneratorTests.
    /// </summary>
    [Category("Logic")]
    public class RegrasDoChefeTests
    {
        const int AlcanceDoTiroDaCena = 3;
        static readonly int[] Sementes = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 12345 };

        [OneTimeSetUp]
        public void CenaVazia() => CenaDoTeste.NovaCenaVazia();

        [OneTimeTearDown]
        public void Restaurar() => CenaDoTeste.Restaurar();

        [Test]
        public void ComAlcanceExtra_OChefeNoCentroEAlcancavelDeTodaCasaDoAnel([ValueSource(nameof(Sementes))] int semente)
        {
            var m = MundoDeTeste.Criar(semente);
            try
            {
                Assert.IsTrue(m.Gerar(), "o gerador falhou na semente " + semente);
                IReadOnlyList<HexCoord> anel = m.gerador.Loop.Casas;
                HexCoord centro = m.gerador.CentroDoMiolo;

                int extra = RegrasDoChefe.AlcanceExtraParaCobrirOAnel(anel, centro, AlcanceDoTiroDaCena);
                int maior = 0;
                for (int i = 0; i < anel.Count; i++)
                {
                    int d = HexCoord.Distance(anel[i], centro);
                    if (d > maior) maior = d;
                    Assert.IsTrue(RegrasDoChefe.DentroDoAlcance(d, AlcanceDoTiroDaCena, extra),
                        "casa " + anel[i] + " a " + d + " do centro fica fora do alcance (extra " + extra + ")");
                }

                Assert.AreEqual(Mathf.Max(0, maior - AlcanceDoTiroDaCena), extra, "extra e o minimo necessario");
                Assert.IsFalse(m.gerador.Loop.Contem(centro), "o chefe nasce no miolo, nunca no anel");
            }
            finally
            {
                m.Destruir();
            }
        }

        [Test]
        public void SemAlcanceExtra_OCentroFicaForaDoTiro_EmTodasAsSementes()
        {
            // Fundamenta a auditoria: colocar o chefe no centro e nada mais NAO funciona com o alcance 3.
            foreach (int semente in Sementes)
            {
                var m = MundoDeTeste.Criar(semente);
                try
                {
                    Assert.IsTrue(m.Gerar());
                    int fora = 0;
                    foreach (HexCoord casa in m.gerador.Loop.Casas)
                        if (!RegrasDoChefe.DentroDoAlcance(HexCoord.Distance(casa, m.gerador.CentroDoMiolo), AlcanceDoTiroDaCena, 0))
                            fora++;
                    Assert.Greater(fora, 0, "semente " + semente + ": esperava ao menos uma casa fora do alcance");
                }
                finally
                {
                    m.Destruir();
                }
            }
        }

        [Test]
        public void AlcanceExtra_ZeroQuandoOAnelJaCabe_EAnelVazio()
        {
            var perto = new List<HexCoord> { new HexCoord(1, 0), new HexCoord(0, 2), new HexCoord(-3, 0) };
            Assert.AreEqual(0, RegrasDoChefe.AlcanceExtraParaCobrirOAnel(perto, HexCoord.Zero, 3));
            Assert.AreEqual(2, RegrasDoChefe.AlcanceExtraParaCobrirOAnel(perto, HexCoord.Zero, 1));
            Assert.AreEqual(0, RegrasDoChefe.AlcanceExtraParaCobrirOAnel(new List<HexCoord>(), HexCoord.Zero, 3));
            Assert.AreEqual(0, RegrasDoChefe.AlcanceExtraParaCobrirOAnel(null, HexCoord.Zero, 3));
        }

        [Test]
        public void DentroDoAlcance_InimigoComumIgualAAntes()
        {
            Assert.IsTrue(RegrasDoChefe.DentroDoAlcance(3, 3, 0));
            Assert.IsFalse(RegrasDoChefe.DentroDoAlcance(4, 3, 0));
            Assert.IsTrue(RegrasDoChefe.DentroDoAlcance(0, 3, 0));
            Assert.IsFalse(RegrasDoChefe.DentroDoAlcance(4, 3, -5), "extra negativo vale 0");
            Assert.IsTrue(RegrasDoChefe.DentroDoAlcance(6, 3, 3));
        }

        [Test]
        public void Variante_DerivaDoMorcegoDoProjeto_SemAlterarOAsset()
        {
            var morcego = AssetDatabase.LoadAssetAtPath<InimigoDef>("Assets/Dados/Morcego.asset");
            Assert.IsNotNull(morcego, "Assets/Dados/Morcego.asset nao carregou como InimigoDef");
            int pv = morcego.pv, atk = morcego.ataque, xp = morcego.xp, alcance = morcego.alcance;
            float escala = morcego.escala;

            var variante = new VarianteDoChefe();
            InimigoDef chefe = variante.Montar(morcego);
            try
            {
                Assert.AreNotSame(morcego, chefe);
                Assert.IsFalse(AssetDatabase.Contains(chefe), "a variante e so em memoria");
                Assert.AreEqual(Mathf.RoundToInt(pv * variante.multiplicadorDePv), chefe.pv);
                Assert.AreEqual(Mathf.RoundToInt(atk * variante.multiplicadorDeAtaque), chefe.ataque);
                Assert.AreEqual(Mathf.RoundToInt(xp * variante.multiplicadorDeXp), chefe.xp);
                Assert.AreEqual(escala * variante.multiplicadorDeEscala, chefe.escala, 1e-5f);
                Assert.IsTrue(chefe.voa);
                Assert.AreEqual(EstiloDeAtaque.Rasante, chefe.estilo);
                Assert.GreaterOrEqual(chefe.alcance, 100, "do centro ele alcanca qualquer casa");
                Assert.AreEqual(variante.velocidadeDoRasante, chefe.velocidadeDoRasante);
                Assert.AreEqual(variante.duracaoMaximaDoRasante, chefe.duracaoMaximaDoRasante);
                Assert.AreSame(morcego.efeitoDeMorte, chefe.efeitoDeMorte);
                Assert.AreEqual(variante.nome, chefe.nome);

                // Valores do pedido: variante calibrada (hipotese) sobre 50/5/50.
                Assert.AreEqual(500, chefe.pv);
                Assert.AreEqual(6, chefe.ataque);
                Assert.AreEqual(200, chefe.xp);

                Assert.AreEqual(pv, morcego.pv, "o asset do morcego nao pode mudar");
                Assert.AreEqual(atk, morcego.ataque);
                Assert.AreEqual(xp, morcego.xp);
                Assert.AreEqual(alcance, morcego.alcance);
                Assert.AreEqual(escala, morcego.escala);
                Assert.IsFalse(EditorUtility.IsDirty(morcego), "o asset do morcego ficou sujo");
            }
            finally
            {
                Object.DestroyImmediate(chefe);
            }
        }

        [Test]
        public void Variante_SemBase_DevolveNull()
        {
            Assert.IsNull(new VarianteDoChefe().Montar(null));
        }
    }
}

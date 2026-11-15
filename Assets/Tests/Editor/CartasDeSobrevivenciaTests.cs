using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Loopia.Hex.Tests
{
    /// <summary>
    /// G2-B: os assets reais de Santuario, Fonte Restauradora e Oficina de Arqueiro e o efeito deles no
    /// fluxo existente (coracao de PlataformaRecompensaDef, carta Flechas, recargas). Sem cena, sem save.
    /// </summary>
    [Category("Logic")]
    public class CartasDeSobrevivenciaTests
    {
        const string Pasta = "Assets/Dados/Plataformas/";

        GameObject _go;
        LuccaStatus _s;
        GameObject _plataformaGo;
        Plataforma _plataforma;

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
            _plataformaGo = new GameObject("Plataforma de teste");
            _plataforma = _plataformaGo.AddComponent<Plataforma>();
        }

        [TearDown]
        public void Limpar()
        {
            if (_go != null) Object.DestroyImmediate(_go);
            if (_plataformaGo != null) Object.DestroyImmediate(_plataformaGo);
        }

        static T Carregar<T>(string nome) where T : PlataformaDef
        {
            var def = AssetDatabase.LoadAssetAtPath<T>(Pasta + nome + ".asset");
            Assert.IsNotNull(def, nome + ".asset ausente ou com tipo errado (m_Script quebrado?)");
            return def;
        }

        static ModificadoresDoLucca Mods(PlataformaDef def) => def.Modificadores(null);

        [Test]
        public void Assets_TemOsValoresDoGddV2_EVaoForaDoCaminho()
        {
            var santuario = Carregar<PlataformaSimplesDef>("Santuario");
            var fonte = Carregar<PlataformaSimplesDef>("FonteRestauradora");
            var oficina = Carregar<PlataformaSimplesDef>("OficinaDeArqueiro");

            Assert.AreEqual(new ModificadoresDoLucca(3, 0f, 0, 0), Mods(santuario));
            Assert.AreEqual(new ModificadoresDoLucca(0, 0f, 0, 2), Mods(fonte));
            Assert.AreEqual(new ModificadoresDoLucca(0, 0f, 2, 0), Mods(oficina), "Oficina nao mexe no dano");

            foreach (var d in new PlataformaDef[] { santuario, fonte, oficina })
            {
                Assert.AreEqual(ColocacaoPermitida.ForaDoCaminho, d.onde, d.name);
                Assert.IsTrue(d.podeSerSubstituida, d.name);
                Assert.IsTrue(d.DisponivelParaCompra(null), d.name + ": sem pre-requisito");
                Assert.IsFalse(string.IsNullOrEmpty(d.nomeExibido), d.name);
                Assert.IsFalse(string.IsNullOrEmpty(d.descricao), d.name);
            }
        }

        [Test]
        public void AssetsAntigos_NaoGanharamFlechasNemCura()
        {
            foreach (var nome in new[] { "Basica", "CampoPacifico", "Torre", "Cabana", "AreaDeTreino" })
            {
                var d = Carregar<PlataformaSimplesDef>(nome);
                Assert.AreEqual(0, d.bonusDeFlechas, nome);
                Assert.AreEqual(0, d.bonusDeCuraPorCoracao, nome);
            }
        }

        [Test]
        public void Coracao_Cura5_ComUmaFonteCura7_ComDuasCura9()
        {
            var coracao = Carregar<PlataformaRecompensaDef>("Coracao");
            var fonte = Carregar<PlataformaSimplesDef>("FonteRestauradora");
            Assert.AreEqual(5, coracao.cura);

            _s.ReceberDano(30); // 20/50
            Pegar(coracao);
            Assert.AreEqual(25, _s.PvAtual, "sem Fonte: +5");

            _s.DefinirModificadores(Mods(fonte));
            Pegar(coracao);
            Assert.AreEqual(32, _s.PvAtual, "uma Fonte: +7");

            _s.DefinirModificadores(Mods(fonte) + Mods(fonte));
            Pegar(coracao);
            Assert.AreEqual(41, _s.PvAtual, "duas Fontes somam: +9");
        }

        [Test]
        public void Fonte_NaoAfetaFlechasNemMoedasNemORestauroDaVolta()
        {
            var flechas = Carregar<PlataformaRecompensaDef>("Flechas");
            var fonte = Carregar<PlataformaSimplesDef>("FonteRestauradora");
            _s.DefinirModificadores(Mods(fonte));

            _s.ReceberDano(10);
            for (int i = 0; i < 12; i++) _s.GastarFlecha();
            Pegar(flechas);
            Assert.AreEqual(40, _s.PvAtual, "a carta Flechas nao cura");
            Assert.AreEqual(13, _s.FlechasAtuais);

            _s.RestaurarVidaEMunicao();
            Assert.AreEqual(50, _s.PvAtual, "o restauro da volta vai ao teto, sem 'bonus'");
        }

        [Test]
        public void Fonte_CoracaoNaoRessuscita()
        {
            var coracao = Carregar<PlataformaRecompensaDef>("Coracao");
            _s.DefinirModificadores(Mods(Carregar<PlataformaSimplesDef>("FonteRestauradora")));
            _s.ReceberDano(999);
            Pegar(coracao);
            Assert.AreEqual(0, _s.PvAtual);
            Assert.IsFalse(_s.EstaVivo);
        }

        [Test]
        public void Oficina_15Vira17_DuasViram19_ECartaFlechasRespeitaOTeto()
        {
            var oficina = Carregar<PlataformaSimplesDef>("OficinaDeArqueiro");
            var flechas = Carregar<PlataformaRecompensaDef>("Flechas");

            _s.DefinirModificadores(Mods(oficina));
            Assert.AreEqual(17, _s.FlechasMaximoTotal);
            _s.DefinirModificadores(Mods(oficina) + Mods(oficina));
            Assert.AreEqual(19, _s.FlechasMaximoTotal);
            Assert.AreEqual(19, _s.FlechasAtuais);
            Assert.AreEqual(35, _s.ataque, "dano base intacto");

            for (int i = 0; i < 12; i++) _s.GastarFlecha(); // 7/19
            Pegar(flechas);
            Assert.AreEqual(17, _s.FlechasAtuais, "+10");
            _plataforma.Carregada = true;
            Pegar(flechas);
            Assert.AreEqual(19, _s.FlechasAtuais, "limitado ao teto com duas Oficinas");
        }

        [Test]
        public void Santuario_Mais3PvMaximo_SomaComCampoPacifico()
        {
            var santuario = Carregar<PlataformaSimplesDef>("Santuario");
            var campo = Carregar<PlataformaSimplesDef>("CampoPacifico");
            _s.DefinirModificadores(Mods(santuario) + Mods(campo));
            Assert.AreEqual(55, _s.PvMaximoTotal);
            Assert.AreEqual(55, _s.PvAtual);
        }

        void Pegar(PlataformaRecompensaDef def)
        {
            _plataforma.Carregada = true;
            def.AoPlayerPassar(_plataforma, _s);
        }
    }
}

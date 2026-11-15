using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Loopia.Hex.Tests
{
    /// <summary>
    /// G2-C: Ilha alta como Mirante. Regras puras (RegrasDoMirante) e o componente MiranteDoLucca sobre um
    /// mapa de teste com Mola + Ilha alta de verdade (relevo levantado pelo MapaDePlataformas). O LuccaCombate
    /// ainda nao consulta o Mirante (integracao pendente); a coexistencia com o chefe e testada na regra.
    /// </summary>
    [Category("Logic")]
    public class MiranteTests
    {
        MundoDeTeste _m;
        MapaDePlataformas _mapa;
        MiranteDoLucca _mirante;
        readonly List<ScriptableObject> _defs = new List<ScriptableObject>();

        [OneTimeSetUp]
        public void CenaVazia() => CenaDoTeste.NovaCenaVazia();

        [OneTimeTearDown]
        public void Restaurar() => CenaDoTeste.Restaurar();

        [TearDown]
        public void Limpar()
        {
            foreach (var d in _defs) if (d != null) Object.DestroyImmediate(d);
            _defs.Clear();
            if (_mirante != null) Object.DestroyImmediate(_mirante.gameObject);
            if (_m != null) _m.Destruir();
            _m = null;
        }

        T Def<T>(string nome) where T : PlataformaDef
        {
            var d = ScriptableObject.CreateInstance<T>();
            d.nomeExibido = nome;
            d.onde = ColocacaoPermitida.NoCaminho;
            _defs.Add(d);
            return d;
        }

        /// <summary>Mapa com Mola em loop[2] e Ilha alta em loop[3]; devolve a casa alta.</summary>
        HexCoord MontarComIlhaAlta(out PlataformaRelevoDef relevo)
        {
            _m = MundoDeTeste.Criar(12345);
            Assert.IsTrue(_m.Gerar());
            _mapa = _m.raiz.AddComponent<MapaDePlataformas>();
            _mapa.mundo = _m.mundo;
            _mapa.gerador = _m.gerador;
            var raiz = new GameObject("Plataformas");
            raiz.transform.SetParent(_m.raiz.transform, false);
            Reflexao.DefinirCampo(_mapa, "_raiz", raiz.transform);

            HexLoopPath loop = _m.gerador.Loop;
            Assert.IsNotNull(_mapa.Colocar(Def<PlataformaMolaDef>("Mola"), loop[2]));
            relevo = Def<PlataformaRelevoDef>("Ilha alta");
            Assert.IsNotNull(_mapa.Colocar(relevo, loop[3]));
            Assert.IsTrue(_m.mundo.TryGetIlha(loop[3], out HexTile ilha));
            Assert.Greater(ilha.Altura, 1f, "com Mola no mapa a ilha alta sobe");

            _mirante = new GameObject("Lucca (mirante)").AddComponent<MiranteDoLucca>();
            _mirante.mapa = _mapa;
            _mirante.mundo = _m.mundo;
            return loop[3];
        }

        // --- Regras puras ---

        [Test]
        public void Regra_AlcanceSoSobeNoMirante()
        {
            Assert.AreEqual(3, RegrasDoMirante.AlcanceDoTiro(3, false, 1));
            Assert.AreEqual(4, RegrasDoMirante.AlcanceDoTiro(3, true, 1));
            Assert.AreEqual(3, RegrasDoMirante.AlcanceDoTiro(3, true, 0));
            Assert.AreEqual(3, RegrasDoMirante.AlcanceDoTiro(3, true, -2), "bonus negativo nao reduz");
        }

        [Test]
        public void Regra_DirecaoLivreNoMirante_EAlvoGrandeContinuaIndependente()
        {
            Assert.IsFalse(RegrasDoMirante.IgnoraDirecao(false, true, false), "fora do mirante: so a frente");
            Assert.IsTrue(RegrasDoMirante.IgnoraDirecao(true, true, false), "no mirante: qualquer direcao");
            Assert.IsFalse(RegrasDoMirante.IgnoraDirecao(true, false, false), "carta pode desligar");
            Assert.IsTrue(RegrasDoMirante.IgnoraDirecao(false, false, true), "chefe: qualquer angulo, como em G1");
        }

        [Test]
        public void Regra_SoIlhaAltaLevantadaEMirante()
        {
            var relevo = Def<PlataformaRelevoDef>("Ilha alta");
            var basica = Def<PlataformaSimplesDef>("Basica");
            Assert.IsTrue(RegrasDoMirante.EhMirante(relevo, 2.2f));
            Assert.IsFalse(RegrasDoMirante.EhMirante(relevo, 0f), "sem Mola a ilha fica no chao: nao e mirante");
            Assert.IsFalse(RegrasDoMirante.EhMirante(basica, 2.2f));
            Assert.IsFalse(RegrasDoMirante.EhMirante(null, 2.2f));
            relevo.ehMirante = false;
            Assert.IsFalse(RegrasDoMirante.EhMirante(relevo, 2.2f));
        }

        [Test]
        public void Regra_CoexisteComOChefe_MiranteNaoEncolheOAlcanceExtra()
        {
            // O chefe declara alcance extra = maior distancia do anel ao centro - alcance base (G1).
            int alcanceBase = 3;
            var anel = new List<HexCoord>(MundoDeTesteAnel());
            HexCoord centro = new HexCoord(0, 0);
            int extra = RegrasDoChefe.AlcanceExtraParaCobrirOAnel(anel, centro, alcanceBase);
            int alcanceNoMirante = RegrasDoMirante.AlcanceDoTiro(alcanceBase, true, 1);
            foreach (HexCoord c in anel)
            {
                int d = HexCoord.Distance(c, centro);
                Assert.IsTrue(RegrasDoChefe.DentroDoAlcance(d, alcanceBase, extra), "fora do mirante: G1");
                Assert.IsTrue(RegrasDoChefe.DentroDoAlcance(d, alcanceNoMirante, extra), "no mirante continua valendo");
            }
            // Morcego comum (extra 0) a 4 casas: so o mirante alcanca.
            Assert.IsFalse(RegrasDoChefe.DentroDoAlcance(4, alcanceBase, 0));
            Assert.IsTrue(RegrasDoChefe.DentroDoAlcance(4, alcanceNoMirante, 0));
            Assert.IsFalse(RegrasDoChefe.DentroDoAlcance(5, alcanceNoMirante, 0), "+1, nao mais");
        }

        static IEnumerable<HexCoord> MundoDeTesteAnel()
        {
            var m = MundoDeTeste.Criar(12345);
            try
            {
                Assert.IsTrue(m.Gerar());
                return new List<HexCoord>(m.gerador.Loop.Casas);
            }
            finally { m.Destruir(); }
        }

        // --- Componente ---

        [Test]
        public void ForaDaIlhaAlta_SemBonus()
        {
            HexCoord alta = MontarComIlhaAlta(out _);
            _mirante.Avaliar(_m.gerador.Loop[1]);
            Assert.IsFalse(_mirante.Ativo);
            Assert.AreEqual(0, _mirante.BonusDeAlcance);
            Assert.IsFalse(_mirante.TiroEmQualquerDirecao);
            Assert.AreEqual(3, _mirante.AlcanceDoTiro(3));
            Assert.AreNotEqual(alta, _m.gerador.Loop[1]);
        }

        [Test]
        public void NaIlhaAlta_MaisUmDeAlcance_EQualquerDirecao()
        {
            HexCoord alta = MontarComIlhaAlta(out _);
            _mirante.Avaliar(alta);
            Assert.IsTrue(_mirante.Ativo);
            Assert.AreEqual(1, _mirante.BonusDeAlcance);
            Assert.IsTrue(_mirante.TiroEmQualquerDirecao);
            Assert.AreEqual(4, _mirante.AlcanceDoTiro(3));
        }

        [Test]
        public void SaindoDaIlha_OEfeitoAcaba_EOEventoDisparaSoNaTroca()
        {
            HexCoord alta = MontarComIlhaAlta(out _);
            var trocas = new List<bool>();
            _mirante.AoMudar += ativo => trocas.Add(ativo);

            _mirante.Avaliar(_m.gerador.Loop[2]); // na Mola
            _mirante.Avaliar(alta);              // sobe
            _mirante.Avaliar(alta);              // continua (sem evento)
            _mirante.Avaliar(_m.gerador.Loop[4]); // desce
            _mirante.Avaliar(_m.gerador.Loop[5]);

            CollectionAssert.AreEqual(new[] { true, false }, trocas);
            Assert.IsFalse(_mirante.Ativo);
            Assert.AreEqual(0, _mirante.BonusDeAlcance);
            Assert.IsFalse(_mirante.TiroEmQualquerDirecao);
        }

        [Test]
        public void ValoresDaCarta_VemDoAsset()
        {
            HexCoord alta = MontarComIlhaAlta(out PlataformaRelevoDef relevo);
            relevo.bonusDeAlcance = 2;
            relevo.tiroEmQualquerDirecao = false;
            _mirante.Avaliar(alta);
            Assert.AreEqual(5, _mirante.AlcanceDoTiro(3));
            Assert.IsFalse(_mirante.TiroEmQualquerDirecao);
        }

        [Test]
        public void AssetIlhaAlta_TemOMiranteDoGddV2()
        {
            var ilha = UnityEditor.AssetDatabase.LoadAssetAtPath<PlataformaRelevoDef>("Assets/Dados/Plataformas/IlhaAlta.asset");
            Assert.IsNotNull(ilha);
            Assert.IsTrue(ilha.ehMirante);
            Assert.AreEqual(1, ilha.bonusDeAlcance, "+1 casa (GDD v2, hipotese)");
            Assert.IsTrue(ilha.tiroEmQualquerDirecao);
            Assert.IsFalse(ilha.DisponivelParaCompra(null), "continua exigindo Mola");
        }
    }
}

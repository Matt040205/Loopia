using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Loopia.Hex.Tests
{
    /// <summary>
    /// MapaDePlataformas.PodeColocar e a unica autoridade de colocacao. Aqui o mapa e montado sem Start
    /// (que exige HexPlayer): mundo e gerador sao atribuidos a mao e player fica null.
    /// </summary>
    [Category("Logic")]
    public class PlacementRulesTests
    {
        MundoDeTeste _m;
        MapaDePlataformas _mapa;
        readonly List<ScriptableObject> _defs = new List<ScriptableObject>();

        [OneTimeSetUp]
        public void CenaVazia() => CenaDoTeste.NovaCenaVazia();

        [OneTimeTearDown]
        public void Restaurar() => CenaDoTeste.Restaurar();

        [SetUp]
        public void Montar()
        {
            _m = MundoDeTeste.Criar(12345);
            Assert.IsTrue(_m.Gerar());
            _mapa = _m.raiz.AddComponent<MapaDePlataformas>();
            _mapa.mundo = _m.mundo;
            _mapa.gerador = _m.gerador;
        }

        [TearDown]
        public void Limpar()
        {
            foreach (var d in _defs) if (d != null) Object.DestroyImmediate(d);
            _defs.Clear();
            if (_m != null) _m.Destruir();
            _m = null;
        }

        T Def<T>(string nome, ColocacaoPermitida onde, bool substituivel = true) where T : PlataformaDef
        {
            var d = ScriptableObject.CreateInstance<T>();
            d.nomeExibido = nome;
            d.onde = onde;
            d.podeSerSubstituida = substituivel;
            _defs.Add(d);
            return d;
        }

        HexCoord Acampamento => _m.gerador.Loop[0];
        HexCoord CasaDoAnel => _m.gerador.Loop[1];
        HexCoord CasaDoMiolo => _m.gerador.CentroDoMiolo;

        /// <summary>Uma casa vazia fora do anel e fora do miolo, vizinha do anel.</summary>
        HexCoord CasaLivreFora()
        {
            foreach (HexCoord c in _m.gerador.Loop.Casas)
                for (int d = 0; d < 6; d++)
                {
                    HexCoord v = c.Neighbor(d);
                    if (_m.mundo.PodeConstruir(v)) return v;
                }
            Assert.Fail("nao achei casa livre fora do anel");
            return default;
        }

        void AssertRejeita(PlataformaDef def, HexCoord casa, string contexto)
        {
            bool ok = _mapa.PodeColocar(def, casa, out string motivo);
            Assert.IsFalse(ok, contexto);
            Assert.IsFalse(string.IsNullOrEmpty(motivo), contexto + ": motivo vazio");
        }

        void AssertAceita(PlataformaDef def, HexCoord casa, string contexto)
        {
            bool ok = _mapa.PodeColocar(def, casa, out string motivo);
            Assert.IsTrue(ok, contexto + " (motivo: " + motivo + ")");
        }

        [Test]
        public void CartaNula_ERejeitadaComMotivo()
        {
            Assert.IsFalse(_mapa.PodeColocar(null, CasaDoAnel, out string motivo));
            Assert.IsFalse(string.IsNullOrEmpty(motivo));
        }

        [Test]
        public void Acampamento_NuncaPodeSerSubstituido_QualquerQueSejaOOnde()
        {
            foreach (ColocacaoPermitida onde in new[] { ColocacaoPermitida.NoCaminho, ColocacaoPermitida.Qualquer, ColocacaoPermitida.ForaDoCaminho })
            {
                var def = Def<PlataformaSimplesDef>("Teste " + onde, onde);
                AssertRejeita(def, Acampamento, "acampamento com onde=" + onde);
            }

            _mapa.PodeColocar(Def<PlataformaSimplesDef>("X", ColocacaoPermitida.NoCaminho), Acampamento, out string motivo);
            StringAssert.Contains("acampamento", motivo);
        }

        [Test]
        public void Miolo_RejeitaTodasAsColocacoes()
        {
            foreach (HexCoord casa in _m.gerador.Miolo)
                foreach (ColocacaoPermitida onde in new[] { ColocacaoPermitida.NoCaminho, ColocacaoPermitida.Qualquer, ColocacaoPermitida.ForaDoCaminho })
                    AssertRejeita(Def<PlataformaSimplesDef>("M" + onde, onde), casa, "miolo " + casa + " com onde=" + onde);

            _mapa.PodeColocar(Def<PlataformaSimplesDef>("M", ColocacaoPermitida.ForaDoCaminho), CasaDoMiolo, out string motivo);
            StringAssert.Contains("Boss", motivo, "o motivo do miolo cita o Boss");
        }

        [Test]
        public void NoCaminho_RejeitaForaDoAnel_EAceitaNoAnel()
        {
            var def = Def<PlataformaSimplesDef>("Torre", ColocacaoPermitida.NoCaminho);
            AssertRejeita(def, CasaLivreFora(), "NoCaminho fora do anel");
            AssertAceita(def, CasaDoAnel, "NoCaminho numa casa do anel");

            // Toda casa do anel, exceto o acampamento, aceita.
            for (int i = 1; i < _m.gerador.Loop.Count; i++) AssertAceita(def, _m.gerador.Loop[i], "anel[" + i + "]");
        }

        [Test]
        public void ForaDoCaminho_RejeitaNoAnel_EAceitaEmCasaLivreFora()
        {
            var def = Def<PlataformaSimplesDef>("Campo", ColocacaoPermitida.ForaDoCaminho);
            AssertRejeita(def, CasaDoAnel, "ForaDoCaminho no anel");
            AssertAceita(def, CasaLivreFora(), "ForaDoCaminho fora do anel");
        }

        [Test]
        public void Qualquer_AceitaNoAnelEFora_MasNaoNoMiolo()
        {
            var def = Def<PlataformaSimplesDef>("Livre", ColocacaoPermitida.Qualquer);
            AssertAceita(def, CasaDoAnel, "Qualquer no anel");
            AssertAceita(def, CasaLivreFora(), "Qualquer fora do anel");
            AssertRejeita(def, CasaDoMiolo, "Qualquer no miolo");
        }

        [Test]
        public void CartaDeRelevo_SemMola_EIndisponivelERejeitadaComMotivo()
        {
            var relevo = Def<PlataformaRelevoDef>("Ilha alta", ColocacaoPermitida.NoCaminho);
            Assert.IsFalse(relevo.DisponivelParaCompra(_mapa));
            Assert.IsFalse(relevo.DisponivelParaCompra(null), "sem mapa tambem nao esta disponivel");

            Assert.IsFalse(_mapa.PodeColocar(relevo, CasaDoAnel, out string motivo));
            StringAssert.Contains("mola", motivo);
            Assert.IsFalse(_mapa.TemMola);
        }

        // --- Cenarios que precisam de Colocar() ---
        // Colocar() usa a raiz "Plataformas" criada no Start (que exige HexPlayer). Em Edit Mode a raiz
        // e injetada por reflexao no campo privado _raiz; nenhum Remover()/Destroy() e acionado.

        void PrepararRaiz()
        {
            var raiz = new GameObject("Plataformas");
            raiz.transform.SetParent(_m.raiz.transform, false);
            Reflexao.DefinirCampo(_mapa, "_raiz", raiz.transform);
        }

        [Test]
        public void Relevo_ComMolaAntesNoPercurso_EAceito_EAntesDaMolaERejeitado()
        {
            PrepararRaiz();
            var mola = Def<PlataformaMolaDef>("Mola", ColocacaoPermitida.NoCaminho);
            var relevo = Def<PlataformaRelevoDef>("Ilha alta", ColocacaoPermitida.NoCaminho);
            HexLoopPath loop = _m.gerador.Loop;

            Assert.IsNotNull(_mapa.Colocar(mola, loop[2]));
            Assert.IsTrue(_mapa.TemMola);
            Assert.IsTrue(relevo.DisponivelParaCompra(_mapa));

            // Relevo logo depois da mola (altura 2.2 <= impulso 3): o percurso ainda e possivel.
            AssertAceita(relevo, loop[3], "relevo depois da mola");

            // Relevo antes da mola: o Lucca chegaria na subida sem impulso.
            Assert.IsFalse(_mapa.PodeColocar(relevo, loop[1], out string motivo));
            StringAssert.Contains("mola", motivo);
        }

        [Test]
        public void CartaQueNaoPodeSerSubstituida_BloqueiaNovaCartaNaMesmaCasa()
        {
            PrepararRaiz();
            var fixa = Def<PlataformaSimplesDef>("Basica fixa", ColocacaoPermitida.NoCaminho, substituivel: false);
            var outra = Def<PlataformaSimplesDef>("Outra", ColocacaoPermitida.NoCaminho);
            HexCoord casa = CasaDoAnel;

            Assert.IsNotNull(_mapa.Colocar(fixa, casa));
            Assert.AreEqual(1, _mapa.Quantas);

            Assert.IsFalse(_mapa.PodeColocar(outra, casa, out string motivo));
            StringAssert.Contains("Basica fixa", motivo);
            StringAssert.Contains("substituida", motivo);
            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Nao da para colocar"));
            Assert.IsNull(_mapa.Colocar(outra, casa), "Colocar respeita o PodeColocar");
            Assert.AreEqual(1, _mapa.Quantas);
        }

        [Test]
        public void CartaSubstituivel_PodeReceberOutraNaMesmaCasa_ENaoViraPlataformaDuplicada()
        {
            PrepararRaiz();
            var basica = Def<PlataformaSimplesDef>("Basica", ColocacaoPermitida.NoCaminho, substituivel: true);
            var outra = Def<PlataformaSimplesDef>("Torre", ColocacaoPermitida.NoCaminho);
            HexCoord casa = CasaDoAnel;

            Assert.IsNotNull(_mapa.Colocar(basica, casa));
            // Apenas a regra (PodeColocar); a troca em si chama Remover/Destroy, que so existe em Play Mode.
            AssertAceita(outra, casa, "substituir carta substituivel");
        }

        [Test]
        public void CartaForaDoAnel_CriaIlhaConstruida_NaCasaLivre()
        {
            PrepararRaiz();
            var campo = Def<PlataformaSimplesDef>("Campo", ColocacaoPermitida.ForaDoCaminho);
            HexCoord casa = CasaLivreFora();
            int antes = _m.mundo.TotalDeIlhas;

            Assert.IsNotNull(_mapa.Colocar(campo, casa));
            Assert.AreEqual(antes + 1, _m.mundo.TotalDeIlhas);
            Assert.IsTrue(_m.mundo.TryGetIlha(casa, out HexTile ilha));
            Assert.AreEqual(HexTileKind.Construida, ilha.Kind);
            Assert.IsFalse(_m.mundo.PodeConstruir(casa), "casa ocupada deixa de ser construivel");
        }
    }
}

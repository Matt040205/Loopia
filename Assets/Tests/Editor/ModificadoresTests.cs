using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Loopia.Hex.Tests
{
    /// <summary>
    /// G2-B: ModificadoresDoLucca, a aplicacao no LuccaStatus e a soma pelo MapaDePlataformas.Recalcular
    /// (docs/gameplay/g2/MODIFIER_SYSTEM.md). Awake por reflexao; nada grava o save.
    /// Remover/substituir plataformas usa Destroy (so em Play): esta coberto em G2PlayModeTests.
    /// </summary>
    [Category("Logic")]
    public class ModificadoresTests
    {
        GameObject _go;
        LuccaStatus _s;
        MundoDeTeste _m;
        readonly List<ScriptableObject> _defs = new List<ScriptableObject>();

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
        public void Limpar()
        {
            foreach (var d in _defs) if (d != null) Object.DestroyImmediate(d);
            _defs.Clear();
            if (_go != null) Object.DestroyImmediate(_go);
            if (_m != null) _m.Destruir();
            _m = null;
        }

        static ModificadoresDoLucca M(int pv = 0, float dano = 0f, int flechas = 0, int coracao = 0) =>
            new ModificadoresDoLucca(pv, dano, flechas, coracao);

        // --- Struct ---

        [Test]
        public void Struct_SomaCampoACampo_ZeroNeutro_IgualdadePorValor()
        {
            var a = M(3, 0.08f, 2, 1);
            var b = M(2, 0.15f, 2, 2);
            var s = a + b;
            Assert.AreEqual(5, s.PvMaximo);
            Assert.AreEqual(0.23f, s.Dano, 1e-6f);
            Assert.AreEqual(4, s.FlechasMaximo);
            Assert.AreEqual(3, s.CuraPorCoracao);
            Assert.AreEqual(a, a + ModificadoresDoLucca.Zero);
            Assert.AreEqual(a.Somar(b), b + a);
            Assert.IsTrue(M(1) == M(1));
            Assert.IsTrue(M(1) != M(flechas: 1));
            Assert.AreEqual(M(9, 0.08f, 2, 1), a.ComPvMaximo(9));
        }

        [Test]
        public void PlataformaDef_Modificadores_ComponOsGanchos_ComPadraoZero()
        {
            var basica = Simples("Basica");
            Assert.AreEqual(ModificadoresDoLucca.Zero, basica.Modificadores(null));

            var oficina = Simples("Oficina", flechas: 2);
            var fonte = Simples("Fonte", coracao: 2);
            var santuario = Simples("Santuario", pv: 3);
            Assert.AreEqual(M(flechas: 2), oficina.Modificadores(null));
            Assert.AreEqual(M(coracao: 2), fonte.Modificadores(null));
            Assert.AreEqual(M(pv: 3), santuario.Modificadores(null));

            var relevo = ScriptableObject.CreateInstance<PlataformaRelevoDef>();
            _defs.Add(relevo);
            Assert.AreEqual(ModificadoresDoLucca.Zero, relevo.Modificadores(null), "outros tipos herdam 0");
        }

        // --- LuccaStatus ---

        [Test]
        public void Pv_TetoSobe_PvAtualSobeJunto_TetoDesce_SoLimita()
        {
            _s.ReceberDano(10); // 40/50
            _s.DefinirModificadores(M(pv: 3));
            Assert.AreEqual(53, _s.PvMaximoTotal);
            Assert.AreEqual(43, _s.PvAtual, "ganha a diferenca, nao cura cheia");

            _s.DefinirModificadores(ModificadoresDoLucca.Zero);
            Assert.AreEqual(50, _s.PvMaximoTotal);
            Assert.AreEqual(43, _s.PvAtual, "abaixo do novo teto: nao perde vida");

            _s.Curar(100);
            _s.DefinirModificadores(M(pv: 3));
            _s.DefinirModificadores(ModificadoresDoLucca.Zero);
            Assert.AreEqual(50, _s.PvAtual, "acima do novo teto: limitado, sem dano extra");
        }

        [Test]
        public void Flechas_TetoSobe_FlechasSobemJunto_TetoDesce_SoLimita()
        {
            Assert.AreEqual(15, _s.FlechasMaximoTotal);
            _s.DefinirModificadores(M(flechas: 2));
            Assert.AreEqual(17, _s.FlechasMaximoTotal);
            Assert.AreEqual(17, _s.FlechasAtuais);
            _s.DefinirModificadores(M(flechas: 4));
            Assert.AreEqual(19, _s.FlechasMaximoTotal);
            Assert.AreEqual(19, _s.FlechasAtuais);

            for (int i = 0; i < 10; i++) _s.GastarFlecha(); // 9/19
            _s.DefinirModificadores(M(flechas: 2));
            Assert.AreEqual(9, _s.FlechasAtuais, "abaixo do novo teto: nao perde flecha");

            _s.GanharFlechas(100);
            Assert.AreEqual(17, _s.FlechasAtuais);
            _s.DefinirModificadores(ModificadoresDoLucca.Zero);
            Assert.AreEqual(15, _s.FlechasAtuais, "acima do novo teto: limitado");
        }

        [Test]
        public void Recargas_RespeitamOTetoEfetivo()
        {
            _s.DefinirModificadores(M(flechas: 2));
            for (int i = 0; i < 17; i++) Assert.IsTrue(_s.GastarFlecha());
            Assert.IsFalse(_s.GastarFlecha());

            _s.GanharFlechas(10); // carta Flechas
            Assert.AreEqual(10, _s.FlechasAtuais);
            _s.GanharFlechas(10);
            Assert.AreEqual(17, _s.FlechasAtuais, "item limitado ao teto efetivo, nao ao base");

            for (int i = 0; i < 5; i++) _s.GastarFlecha();
            _s.RestaurarVidaEMunicao(); // fim de volta
            Assert.AreEqual(17, _s.FlechasAtuais);

            for (int i = 0; i < 5; i++) _s.GastarFlecha();
            _s.GanharFlechas(_s.FlechasMaximoTotal); // recarga do acampamento no chefe (LoopDirector)
            Assert.AreEqual(17, _s.FlechasAtuais);
        }

        [Test]
        public void Morto_SoOTetoMuda()
        {
            _s.ReceberDano(999);
            int flechas = _s.FlechasAtuais;
            _s.DefinirModificadores(M(pv: 3, flechas: 2));
            Assert.AreEqual(0, _s.PvAtual, "aumentar o teto nao ressuscita");
            Assert.AreEqual(flechas, _s.FlechasAtuais);
            Assert.AreEqual(53, _s.PvMaximoTotal);
            Assert.AreEqual(17, _s.FlechasMaximoTotal);
        }

        [Test]
        public void Negativos_ViramZero_EMesmoValorNaoNotifica()
        {
            int mudou = 0;
            _s.AoMudar += () => mudou++;
            _s.DefinirModificadores(M(-5, -1f, -2, -3));
            Assert.AreEqual(ModificadoresDoLucca.Zero, _s.Modificadores);
            Assert.AreEqual(0, mudou, "zero -> zero nao e mudanca");

            _s.DefinirModificadores(M(flechas: 2));
            _s.DefinirModificadores(M(flechas: 2));
            Assert.AreEqual(1, mudou);
        }

        [Test]
        public void DefinirBonusDePvMaximo_AtalhoAntigo_MantemOsOutros()
        {
            _s.DefinirModificadores(M(pv: 1, flechas: 2, coracao: 2));
            _s.DefinirBonusDePvMaximo(4);
            Assert.AreEqual(M(pv: 4, flechas: 2, coracao: 2), _s.Modificadores);
            Assert.AreEqual(4, _s.BonusDePvMaximo);
        }

        [Test]
        public void CurarComCoracao_SomaABonificacao_SoParaCoracao()
        {
            _s.ReceberDano(20); // 30/50
            _s.CurarComCoracao(5);
            Assert.AreEqual(35, _s.PvAtual, "sem Fonte: igual ao Curar");

            _s.DefinirModificadores(M(coracao: 2));
            _s.CurarComCoracao(5);
            Assert.AreEqual(42, _s.PvAtual, "5 + 2");

            _s.Curar(5);
            Assert.AreEqual(47, _s.PvAtual, "cura generica nao ganha o bonus");

            _s.CurarComCoracao(0);
            Assert.AreEqual(47, _s.PvAtual, "item sem cura nao vira cura de 2");

            _s.CurarComCoracao(5);
            Assert.AreEqual(50, _s.PvAtual, "limitado ao teto");
        }

        [Test]
        public void CurarComCoracao_NaoRessuscita()
        {
            _s.DefinirModificadores(M(coracao: 2));
            _s.ReceberDano(999);
            _s.CurarComCoracao(5);
            Assert.AreEqual(0, _s.PvAtual);
            Assert.IsFalse(_s.EstaVivo);
        }

        // --- MapaDePlataformas.Recalcular ---

        PlataformaSimplesDef Simples(string nome, int pv = 0, float dano = 0f, int flechas = 0, int coracao = 0)
        {
            var d = ScriptableObject.CreateInstance<PlataformaSimplesDef>();
            d.nomeExibido = nome;
            d.onde = ColocacaoPermitida.ForaDoCaminho;
            d.bonusDePv = pv;
            d.bonusDeDano = dano;
            d.bonusDeFlechas = flechas;
            d.bonusDeCuraPorCoracao = coracao;
            _defs.Add(d);
            return d;
        }

        MapaDePlataformas NovoMapa()
        {
            _m = MundoDeTeste.Criar(12345);
            Assert.IsTrue(_m.Gerar());
            var mapa = _m.raiz.AddComponent<MapaDePlataformas>();
            mapa.mundo = _m.mundo;
            mapa.gerador = _m.gerador;
            mapa.status = _s;
            var raiz = new GameObject("Plataformas");
            raiz.transform.SetParent(_m.raiz.transform, false);
            Reflexao.DefinirCampo(mapa, "_raiz", raiz.transform);
            return mapa;
        }

        List<HexCoord> CasasLivresFora(MapaDePlataformas mapa, int quantas)
        {
            var casas = new List<HexCoord>();
            foreach (HexCoord c in _m.gerador.Loop.Casas)
            {
                for (int d = 0; d < 6; d++)
                {
                    HexCoord v = c.Neighbor(d);
                    if (casas.Contains(v) || !_m.mundo.PodeConstruir(v)) continue;
                    casas.Add(v);
                    if (casas.Count == quantas) return casas;
                }
            }
            Assert.Fail("faltaram casas livres fora do anel");
            return casas;
        }

        [Test]
        public void Mapa_SomaTodasAsPlataformas_EEntregaAoLucca()
        {
            var mapa = NovoMapa();
            var casas = CasasLivresFora(mapa, 5);
            var torre = Simples("Torre", dano: 0.08f);
            var santuario = Simples("Santuario", pv: 3);
            var oficina = Simples("Oficina", flechas: 2);
            var fonte = Simples("Fonte", coracao: 2);

            Assert.IsNotNull(mapa.Colocar(torre, casas[0]));
            Assert.IsNotNull(mapa.Colocar(santuario, casas[1]));
            Assert.IsNotNull(mapa.Colocar(oficina, casas[2]));
            Assert.IsNotNull(mapa.Colocar(oficina, casas[3]));
            Assert.IsNotNull(mapa.Colocar(fonte, casas[4]));

            Assert.AreEqual(M(3, 0.08f, 4, 2), mapa.Modificadores);
            Assert.AreEqual(3, mapa.BonusDePvTotal, "API antiga preservada");
            Assert.AreEqual(0.08f, mapa.BonusDeDanoTotal, 1e-6f, "dano das torres preservado");
            Assert.AreEqual(mapa.Modificadores, _s.Modificadores);
            Assert.AreEqual(53, _s.PvMaximoTotal);
            Assert.AreEqual(19, _s.FlechasMaximoTotal, "15 + 2 + 2");
            Assert.AreEqual(19, _s.FlechasAtuais);
            Assert.AreEqual(2, _s.BonusDeCuraPorCoracao);
        }

        [Test]
        public void Mapa_VizinhancaContinuaIgual_EOAgregadorSomaJunto()
        {
            var mapa = NovoMapa();
            // Duas casas vizinhas fora do anel, para a Area de Treino enxergar a Torre.
            HexCoord a = default, b = default;
            bool achou = false;
            foreach (HexCoord c in CasasLivresFora(mapa, 20))
            {
                for (int d = 0; d < 6 && !achou; d++)
                {
                    HexCoord v = c.Neighbor(d);
                    if (!_m.mundo.PodeConstruir(v)) continue;
                    a = c; b = v; achou = true;
                }
                if (achou) break;
            }
            Assert.IsTrue(achou, "precisa de duas casas livres vizinhas");

            var torre = Simples("Torre", dano: 0.08f);
            var area = ScriptableObject.CreateInstance<PlataformaVizinhancaDef>();
            area.nomeExibido = "Area";
            area.onde = ColocacaoPermitida.ForaDoCaminho;
            area.bonusDeDano = 0.08f;
            area.danoPorVizinha = 0.15f;
            area.tipoQueConta = torre;
            _defs.Add(area);

            mapa.Colocar(torre, a);
            mapa.Colocar(area, b);
            Assert.AreEqual(0.31f, mapa.BonusDeDanoTotal, 1e-5f, "8 + 8 + 15 (BALANCE_BASELINE §3)");
            Assert.AreEqual(0, mapa.Modificadores.FlechasMaximo);
        }
    }
}

using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Loopia.Hex.Tests
{
    /// <summary>
    /// MapaDePlataformas.PodeColocar recusa qualquer carta com a jornada encerrada e continua aceitando no chefe.
    /// O diretor e levado ao estado pelo CicloDaPartida interno (sem Start, sem save).
    /// </summary>
    [Category("Logic")]
    public class ColocacaoNoFimDaJornadaTests
    {
        MundoDeTeste _m;
        MapaDePlataformas _mapa;
        LoopDirector _diretor;
        PlataformaSimplesDef _carta;

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

            _diretor = new GameObject("Diretor").AddComponent<LoopDirector>();
            Reflexao.Chamar(_diretor, "Awake");
            _mapa.diretor = _diretor;

            _carta = ScriptableObject.CreateInstance<PlataformaSimplesDef>();
            _carta.nomeExibido = "Basica de teste";
            _carta.onde = ColocacaoPermitida.NoCaminho;
        }

        [TearDown]
        public void Limpar()
        {
            if (_carta != null) Object.DestroyImmediate(_carta);
            if (_diretor != null) Object.DestroyImmediate(_diretor.gameObject);
            if (_m != null) _m.Destruir();
            _m = null;
        }

        CicloDaPartida Ciclo()
        {
            FieldInfo f = typeof(LoopDirector).GetField("_ciclo", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(f, "campo _ciclo do LoopDirector nao encontrado (renomeado? atualize o teste)");
            return (CicloDaPartida)f.GetValue(_diretor);
        }

        HexCoord CasaDoAnel => _m.gerador.Loop[2];

        [Test]
        public void Correndo_EChefe_AceitamCartas()
        {
            Ciclo().Comecar();
            Assert.IsTrue(_mapa.PodeColocar(_carta, CasaDoAnel, out string motivo), motivo);
            Ciclo().ComecarChefe();
            Assert.IsTrue(_mapa.PodeColocar(_carta, CasaDoAnel, out motivo), "no chefe ainda se constroi: " + motivo);
        }

        [Test]
        public void Vitoria_RecusaCartas()
        {
            Ciclo().Comecar();
            Ciclo().ComecarChefe();
            Ciclo().RegistrarVitoria();
            Assert.IsTrue(_diretor.Terminou);
            Assert.IsFalse(_mapa.PodeColocar(_carta, CasaDoAnel, out string motivo));
            Assert.AreEqual("a jornada terminou", motivo);
        }

        [Test]
        public void Derrota_RecusaCartas()
        {
            Ciclo().Comecar();
            Ciclo().RegistrarDerrota();
            Assert.IsFalse(_mapa.PodeColocar(_carta, CasaDoAnel, out string motivo));
            Assert.AreEqual("a jornada terminou", motivo);
        }

        [Test]
        public void SemDiretor_ComportamentoAntigo()
        {
            _mapa.diretor = null;
            Assert.IsTrue(_mapa.PodeColocar(_carta, CasaDoAnel, out string motivo), motivo);
        }
    }
}

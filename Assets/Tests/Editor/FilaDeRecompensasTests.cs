using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Loopia.Hex.Tests
{
    /// <summary>
    /// G2-A: fila de recompensas pendentes da MaoDeCartas (docs/gameplay/g2/REWARD_QUEUE_DESIGN.md).
    /// Awake por reflexao (nao roda em Edit Mode); o level-up e chamado direto em SubiuDeNivel, que e o
    /// handler de LuccaStatus.AoSubirDeNivel. Nada aqui grava o save.
    /// </summary>
    [Category("Logic")]
    public class FilaDeRecompensasTests
    {
        GameObject _go;
        MaoDeCartas _mao;
        readonly List<Object> _criados = new List<Object>();

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
            foreach (var o in _criados)
            {
                if (o == null) continue;
                if (o is Component c) Object.DestroyImmediate(c.gameObject);
                else Object.DestroyImmediate(o);
            }
            _criados.Clear();
            if (_go != null) Object.DestroyImmediate(_go);
        }

        PlataformaSimplesDef Carta(string nome)
        {
            var d = ScriptableObject.CreateInstance<PlataformaSimplesDef>();
            d.nomeExibido = nome;
            _criados.Add(d);
            return d;
        }

        void EncherAMao()
        {
            for (int i = 0; i < 4; i++) Assert.IsTrue(_mao.DarCarta(Carta("Inicial " + i)));
            Assert.AreEqual(0, _mao.EspacosLivres);
        }

        void SubirDeNivel(int nivel) => Reflexao.Chamar(_mao, "SubiuDeNivel", nivel);

        LoopDirector NovoDiretor()
        {
            var d = new GameObject("Diretor").AddComponent<LoopDirector>();
            _criados.Add(d);
            Reflexao.Chamar(d, "Awake");
            return d;
        }

        static CicloDaPartida Ciclo(LoopDirector d)
        {
            FieldInfo f = typeof(LoopDirector).GetField("_ciclo", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(f, "campo _ciclo do LoopDirector nao encontrado (renomeado? atualize o teste)");
            return (CicloDaPartida)f.GetValue(d);
        }

        [Test]
        public void ComEspaco_LevelUpEntregaNaHora_SemPendente()
        {
            var a = Carta("A");
            _mao.baralho.Add(a);
            int pendentesMudou = 0;
            _mao.AoMudarPendentes += () => pendentesMudou++;

            SubirDeNivel(1);

            Assert.AreSame(a, _mao.CartaNoEspaco(0));
            Assert.AreEqual(0, _mao.Pendentes);
            Assert.AreEqual(2, pendentesMudou, "entrou (1) e saiu (1) da fila");
        }

        [Test]
        public void MaoCheia_VariosNiveis_AcumulamPendentes_SemMudarAMao()
        {
            EncherAMao();
            _mao.baralho.Add(Carta("Recompensa"));
            var antes = new List<PlataformaDef>(_mao.Mao);

            SubirDeNivel(1);
            SubirDeNivel(2);
            SubirDeNivel(3);

            Assert.AreEqual(3, _mao.Pendentes);
            CollectionAssert.AreEqual(antes, new List<PlataformaDef>(_mao.Mao), "nenhuma carta da mao foi trocada");
        }

        [Test]
        public void CartasPorNivel_GeraUmDireitoPorCarta()
        {
            EncherAMao();
            _mao.baralho.Add(Carta("R"));
            _mao.cartasPorNivel = 2;
            SubirDeNivel(1);
            Assert.AreEqual(2, _mao.Pendentes);
        }

        [Test]
        public void GastarUma_EntregaSoUmaPendente_NoEspacoLiberado_SemDuplicar()
        {
            EncherAMao();
            var r = Carta("R");
            _mao.baralho.Add(r);
            SubirDeNivel(1);
            SubirDeNivel(2);

            Assert.IsTrue(_mao.Gastar(1));

            Assert.AreSame(r, _mao.CartaNoEspaco(1));
            Assert.AreEqual(0, _mao.EspacosLivres, "so um espaco abriu, so uma carta entrou");
            Assert.AreEqual(1, _mao.Pendentes, "a outra continua esperando");

            // Chamar de novo sem espaco nao muda nada.
            Assert.IsFalse(_mao.TentarEntregarPendentes());
            Assert.AreEqual(1, _mao.Pendentes);
        }

        [Test]
        public void Entrega_RespeitaAOrdemDosNiveis()
        {
            EncherAMao();
            _mao.baralho.Add(Carta("R"));
            var ordem = new List<int>();
            _mao.AoEntregarPendente += (nivel, carta) =>
            {
                Assert.IsNotNull(carta);
                ordem.Add(nivel);
            };

            SubirDeNivel(1);
            SubirDeNivel(2);
            SubirDeNivel(3);
            _mao.Gastar(0);
            _mao.Gastar(3);
            _mao.Gastar(2);

            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, ordem);
            Assert.AreEqual(0, _mao.Pendentes);
        }

        [Test]
        public void SemPerdaSilenciosa_TodoNivelViraExatamenteUmaCarta()
        {
            EncherAMao();
            _mao.baralho.Add(Carta("R"));
            int entregues = 0;
            _mao.AoEntregarPendente += (n, c) => entregues++;

            for (int nivel = 1; nivel <= 6; nivel++) SubirDeNivel(nivel);
            Assert.AreEqual(6, _mao.Pendentes);

            // Joga cartas ate esvaziar a fila: cada espaco liberado recebe exatamente uma.
            for (int i = 0; i < 6; i++) _mao.Gastar(i % 4);

            Assert.AreEqual(6, entregues);
            Assert.AreEqual(0, _mao.Pendentes);
            Assert.AreEqual(0, _mao.EspacosLivres);
        }

        [Test]
        public void BaralhoVazio_ODireitoFicaNaFila_EEntraQuandoHouverCarta()
        {
            SubirDeNivel(1);
            Assert.AreEqual(1, _mao.Pendentes, "sem carta para sortear o direito nao se perde");
            Assert.AreEqual(4, _mao.EspacosLivres);

            var r = Carta("R");
            _mao.baralho.Add(r);
            Assert.IsTrue(_mao.TentarEntregarPendentes());
            Assert.AreSame(r, _mao.CartaNoEspaco(0));
            Assert.AreEqual(0, _mao.Pendentes);
        }

        [Test]
        public void CartaIndisponivel_NaoEEntregue_EODireitoEsperaElaLiberar()
        {
            var alta = ScriptableObject.CreateInstance<CartaDeTesteControlavel>();
            alta.nomeExibido = "Ilha alta de teste";
            _criados.Add(alta);
            _mao.baralho.Add(alta);

            SubirDeNivel(1);
            Assert.AreEqual(1, _mao.Pendentes);
            Assert.AreEqual(4, _mao.EspacosLivres, "carta bloqueada nunca entra");

            // O mapa mudou (ex.: uma Mola foi colocada) e a carta ficou disponivel.
            alta.disponivel = true;
            Reflexao.Chamar(_mao, "AoMudarOMapa");
            Assert.AreSame(alta, _mao.CartaNoEspaco(0));
            Assert.AreEqual(0, _mao.Pendentes);
        }

        [Test]
        public void Sorteio_SoEntreDisponiveis()
        {
            EncherAMao();
            var bloqueada = ScriptableObject.CreateInstance<CartaDeTesteControlavel>();
            _criados.Add(bloqueada);
            var livre = Carta("Livre");
            _mao.baralho.Add(bloqueada);
            _mao.baralho.Add(livre);
            _mao.baralho.Add(null);

            for (int n = 1; n <= 20; n++) SubirDeNivel(n);
            for (int i = 0; i < 20; i++)
            {
                _mao.Gastar(i % 4);
                Assert.AreSame(livre, _mao.CartaNoEspaco(i % 4), "nunca sorteia a indisponivel");
            }
        }

        [Test]
        public void Eventos_AoMudarPendentes_EAoMudar_UmaVezPorMudanca()
        {
            EncherAMao();
            _mao.baralho.Add(Carta("R"));
            int pendentes = 0, mao = 0;
            _mao.AoMudarPendentes += () => pendentes++;
            _mao.AoMudar += () => mao++;

            SubirDeNivel(1);
            Assert.AreEqual(1, pendentes, "entrou na fila");
            Assert.AreEqual(0, mao, "a mao nao mudou");

            _mao.Gastar(0);
            Assert.AreEqual(2, pendentes, "saiu da fila");
            Assert.AreEqual(1, mao, "gastar + entregar = uma mudanca da mao");

            _mao.Gastar(1);
            Assert.AreEqual(2, pendentes, "fila vazia: sem evento de pendentes");
            Assert.AreEqual(2, mao);
        }

        [Test]
        public void ReentradaPeloAoMudar_NaoDuplicaEntregas()
        {
            EncherAMao();
            _mao.baralho.Add(Carta("R"));
            SubirDeNivel(1);
            SubirDeNivel(2);
            // Um ouvinte que tenta drenar de novo dentro do evento (ex.: UI ou mapa reagindo).
            _mao.AoMudar += () => _mao.TentarEntregarPendentes();

            _mao.Gastar(0);
            Assert.AreEqual(1, _mao.Pendentes, "um espaco, uma entrega");
            Assert.AreEqual(0, _mao.EspacosLivres);
        }

        [Test]
        public void JornadaEncerrada_NaoEntrega_MasNaoPerdeODireito()
        {
            EncherAMao();
            _mao.baralho.Add(Carta("R"));
            var diretor = NovoDiretor();
            _mao.diretor = diretor;
            Ciclo(diretor).Comecar();
            Ciclo(diretor).ComecarChefe();

            SubirDeNivel(1);
            _mao.Gastar(0);
            Assert.IsNotNull(_mao.CartaNoEspaco(0), "no chefe a fila entrega normalmente");

            SubirDeNivel(2);
            Ciclo(diretor).RegistrarVitoria();
            Assert.IsTrue(diretor.Terminou);
            int mao = 0;
            _mao.AoMudar += () => mao++;

            _mao.Gastar(1);
            Assert.IsNull(_mao.CartaNoEspaco(1), "vitoria: nada entra na mao");
            Assert.AreEqual(1, _mao.Pendentes, "o direito continua contado");
            Assert.AreEqual(1, mao, "so a carta gasta mudou a mao");

            SubirDeNivel(3);
            Assert.AreEqual(2, _mao.Pendentes);
            Assert.IsNull(_mao.CartaNoEspaco(1));
        }

        [Test]
        public void JornadaEncerrada_PeloDiretorDoMapa_TambemBloqueia()
        {
            var mapaGo = new GameObject("Mapa");
            var mapa = mapaGo.AddComponent<MapaDePlataformas>();
            _criados.Add(mapa);
            var diretor = NovoDiretor();
            mapa.diretor = diretor;
            _mao.mapa = mapa;
            _mao.baralho.Add(Carta("R"));
            Ciclo(diretor).Comecar();
            Ciclo(diretor).RegistrarDerrota();

            SubirDeNivel(1);
            Assert.AreEqual(4, _mao.EspacosLivres);
            Assert.AreEqual(1, _mao.Pendentes);
        }

        [Test]
        public void LuccaMorto_NaoEntrega()
        {
            var lucca = new GameObject("Lucca").AddComponent<LuccaStatus>();
            _criados.Add(lucca);
            Reflexao.Chamar(lucca, "Awake");
            _mao.status = lucca;
            _mao.baralho.Add(Carta("R"));

            lucca.ReceberDano(999);
            SubirDeNivel(1);
            Assert.AreEqual(4, _mao.EspacosLivres);
            Assert.AreEqual(1, _mao.Pendentes);
        }

        [Test]
        public void AcimaDoLimiteSuave_SoAvisa_NaoDescarta()
        {
            EncherAMao();
            _mao.baralho.Add(Carta("R"));
            _mao.limiteDeAlertaDePendentes = 2;

            SubirDeNivel(1);
            SubirDeNivel(2);
            Assert.IsFalse(_mao.AcimaDoLimite);

            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("3 recompensas pendentes"));
            SubirDeNivel(3);
            SubirDeNivel(4); // nao repete o aviso
            Assert.IsTrue(_mao.AcimaDoLimite);
            Assert.AreEqual(4, _mao.Pendentes, "nada foi descartado");
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void ComprarDireto_ContinuaSemFila()
        {
            EncherAMao();
            _mao.baralho.Add(Carta("R"));
            Assert.IsFalse(_mao.Comprar());
            Assert.AreEqual(0, _mao.Pendentes, "so o nivel gera direito; a mao inicial nao");
        }
    }
}

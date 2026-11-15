using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Loopia.Hex.Tests
{
    /// <summary>
    /// Ciclo completo da jornada em Play Mode na HexPrototipo: 4 voltas -> chefe -> vitoria/derrota.
    ///
    /// A cena ainda nao tem EncontroDoChefe (integracao pendente, docs/gameplay/IMPLEMENTATION_PLAN.md
    /// §G1.8). Cada teste monta o encontro em tempo de execucao (AddComponent no Play Mode, nada e salvo) e,
    /// quando precisa, liga aoVencer -> TelaDeMorte.MostrarVitoria por codigo, como o PR de cena fara no
    /// Inspector.
    ///
    /// As voltas passam pelo caminho real corredor -> diretor: o teste chama o handler privado do
    /// HexLoopRunner (o mesmo que o HexPlayer dispara ao entrar numa casa) com a casa do acampamento.
    /// O spawn comum de morcegos e desligado para o resultado nao depender de sorte.
    /// </summary>
    [Category("PlayModeSmoke")]
    public class EncontroDoChefePlayModeTests
    {
        [UnitySetUp]
        public IEnumerator Preparar()
        {
            SaveIsolado.Backup();
            if (!EditorApplication.isPlaying) CenaDoTeste.AbrirCenaPrincipal();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Encerrar()
        {
            Time.timeScale = 1f;
            if (EditorApplication.isPlaying) yield return new ExitPlayMode();
            Time.timeScale = 1f;
            SaveIsolado.Restaurar();
            CenaDoTeste.Restaurar();
            yield return null;
        }

        // --- Apoio ---

        sealed class Partida
        {
            public LoopDirector diretor;
            public HexLoopRunner corredor;
            public LuccaStatus status;
            public LuccaCombate combate;
            public InimigoSpawner inimigos;
            public HexWorld mundo;
            public HexLoopGenerator gerador;
            public TelaDeMorte tela;
            public MapaDePlataformas mapa;
            public EncontroDoChefe encontro;
            public BarraDeVidaDoChefe barra;
            public IndicadorDePerigoDoChefe indicador;
            public int estadosChefe, vitorias, derrotas, voltas, nasceu;
        }

        static T Achar<T>() where T : UnityEngine.Object
        {
            var o = UnityEngine.Object.FindFirstObjectByType<T>();
            Assert.IsNotNull(o, "nao achei " + typeof(T).Name + " em Play Mode");
            return o;
        }

        static IEnumerator Esperar(Func<bool> condicao, float segundosReais, Func<string> descricao)
        {
            float fim = Time.realtimeSinceStartup + segundosReais;
            while (!condicao())
            {
                if (Time.realtimeSinceStartup > fim)
                    Assert.Fail("timeout de " + segundosReais + "s esperando: " + descricao());
                yield return null;
            }
        }

        /// <summary>Acha a cena, desliga o spawn comum e monta o encontro como o PR de cena vai montar.</summary>
        static Partida Montar(bool usarRevoada)
        {
            Application.runInBackground = true;
            Time.timeScale = 1f;

            var p = new Partida
            {
                diretor = Achar<LoopDirector>(),
                corredor = Achar<HexLoopRunner>(),
                status = Achar<LuccaStatus>(),
                combate = Achar<LuccaCombate>(),
                inimigos = Achar<InimigoSpawner>(),
                mundo = Achar<HexWorld>(),
                gerador = Achar<HexLoopGenerator>(),
                tela = Achar<TelaDeMorte>(),
                mapa = Achar<MapaDePlataformas>(),
            };

            p.inimigos.nascerMorcegos = false;
            p.inimigos.LimparTodos();
            p.corredor.andando = false;

            var go = new GameObject("Encontro do chefe (teste)");
            p.encontro = go.AddComponent<EncontroDoChefe>();
            p.encontro.usarRevoada = usarRevoada;
            p.barra = go.AddComponent<BarraDeVidaDoChefe>();
            p.indicador = go.AddComponent<IndicadorDePerigoDoChefe>();

            Partida q = p;
            p.diretor.AoMudarEstado += (_, novo) => { if (novo == EstadoDaPartida.Chefe) q.estadosChefe++; };
            p.diretor.AoVencer += () => q.vitorias++;
            p.diretor.AoPerder += () => q.derrotas++;
            p.diretor.AoCompletarVolta += _ => q.voltas++;
            p.encontro.AoChefeNascer += _ => q.nasceu++;
            return p;
        }

        static PlataformaDef Basica()
        {
            var basica = AssetDatabase.LoadAssetAtPath<PlataformaDef>("Assets/Dados/Plataformas/Basica.asset");
            Assert.IsNotNull(basica);
            return basica;
        }

        /// <summary>Casa do anel do lado oposto ao acampamento (longe do Lucca parado).</summary>
        static HexCoord CasaLonge(Partida p) => p.gerador.Loop[p.gerador.Loop.Count / 2];

        static void PassarPeloAcampamento(Partida p) =>
            Reflexao.Chamar(p.corredor, "AoEntrarNoHexagono", p.mundo.Acampamento);

        static void FecharQuatroVoltas(Partida p)
        {
            for (int v = 1; v <= 4; v++)
            {
                Assert.AreEqual(EstadoDaPartida.Correndo, p.diretor.Estado, "antes da volta " + v);
                PassarPeloAcampamento(p);
            }
        }

        // --- Testes ---

        [UnityTest]
        public IEnumerator QuatroVoltas_IniciamUmConfronto_EAQuintaNaoConta()
        {
            yield return new EnterPlayMode();
            yield return QuatroVoltas_IniciamUmConfronto_EAQuintaNaoContaEmPlayMode();
            yield return new ExitPlayMode();
        }

        static IEnumerator QuatroVoltas_IniciamUmConfronto_EAQuintaNaoContaEmPlayMode()
        {
            yield return null;
            yield return null;
            Partida p = Montar(usarRevoada: false);
            yield return null; // Start da barra e do indicador

            Assert.AreEqual(EstadoDaPartida.Correndo, p.diretor.Estado, "depois do Start a partida corre");
            FecharQuatroVoltas(p);

            Assert.AreEqual(EstadoDaPartida.Chefe, p.diretor.Estado);
            Assert.AreEqual(1, p.estadosChefe, "exatamente um confronto");
            Assert.AreEqual(1, p.nasceu, "o chefe nasce uma vez");
            Assert.IsTrue(p.diretor.BossLiberado);
            Assert.IsFalse(p.corredor.contarVoltas, "a contagem congela no chefe");
            Assert.AreEqual(4, p.corredor.VoltasCompletas);
            Assert.AreEqual(4, p.diretor.VoltasCompletas);

            Inimigo chefe = p.encontro.Chefe;
            Assert.IsNotNull(chefe);
            Assert.AreEqual(p.gerador.CentroDoMiolo, chefe.Casa, "nasce no centro do miolo");
            Assert.AreEqual(1, p.inimigos.QuantosVivos(p.encontro.DefDoChefe));
            Assert.AreEqual(Mathf.RoundToInt(p.encontro.DefDoChefe.pv * Mathf.Pow(1.1f, 4)), chefe.PvMaximo, 1,
                "o chefe nasce com a escala da 4a volta");

            // 5a passagem: nao existe volta nova, nem escala, nem segundo chefe.
            float multiplicador = p.diretor.MultiplicadorDosInimigos;
            p.status.ReceberDano(10);
            int pvAntes = p.status.PvAtual;
            for (int i = 0; i < 5; i++) p.status.GastarFlecha();
            PassarPeloAcampamento(p);

            Assert.AreEqual(4, p.corredor.VoltasCompletas, "o corredor nao fecha a 5a volta");
            Assert.AreEqual(4, p.diretor.VoltasCompletas);
            Assert.AreEqual(4, p.voltas, "AoCompletarVolta nao dispara no chefe");
            Assert.AreEqual(multiplicador, p.diretor.MultiplicadorDosInimigos, 1e-6f, "sem escala nova");
            Assert.AreEqual(EstadoDaPartida.Chefe, p.diretor.Estado);
            Assert.AreEqual(1, p.estadosChefe);
            Assert.AreEqual(1, p.inimigos.QuantosVivos(p.encontro.DefDoChefe), "continua um chefe so");
            Assert.AreEqual(p.status.flechasMaximo, p.status.FlechasAtuais, "o acampamento recarrega flechas no chefe");
            Assert.AreEqual(pvAntes, p.status.PvAtual, "mas nao restaura a vida");

            // No chefe ainda se constroi (preparo durante o confronto).
            Assert.IsTrue(p.mapa.PodeColocar(Basica(), CasaLonge(p), out string motivo), "carta no chefe: " + motivo);

            // Pedir de novo nao faz nascer outro.
            Assert.IsFalse(p.encontro.Iniciar());
            Assert.AreEqual(1, p.nasceu);
            Assert.AreEqual(1, p.inimigos.QuantosVivos(p.encontro.DefDoChefe));

            // O golpe do chefe segue o caminho por UnityEvent ate o LuccaStatus.ReceberDano (fio da cena).
            int pv = p.status.PvAtual;
            Reflexao.Chamar(chefe, "Golpear");
            Assert.AreEqual(Mathf.Max(0, pv - chefe.Ataque), p.status.PvAtual,
                "Inimigo.aoAtacar -> InimigoSpawner.aoInimigoAtacar -> LuccaStatus.ReceberDano");
        }

        [UnityTest]
        public IEnumerator Chefe_EAlvoDoTiroAutomatico_ESofreDanoDeFlechasReais()
        {
            yield return new EnterPlayMode();
            yield return Chefe_EAlvoDoTiroAutomatico_ESofreDanoDeFlechasReaisEmPlayMode();
            yield return new ExitPlayMode();
        }

        static IEnumerator Chefe_EAlvoDoTiroAutomatico_ESofreDanoDeFlechasReaisEmPlayMode()
        {
            yield return null;
            yield return null;
            Partida p = Montar(usarRevoada: false);
            yield return null;
            FecharQuatroVoltas(p);
            Inimigo chefe = p.encontro.Chefe;
            Assert.IsNotNull(chefe);

            // De qualquer casa do anel o chefe esta no alcance, e de qualquer angulo.
            Assert.IsTrue(chefe.AlvoDeQualquerAngulo);
            foreach (HexCoord casa in p.gerador.Loop.Casas)
                Assert.IsTrue(RegrasDoChefe.DentroDoAlcance(HexCoord.Distance(casa, chefe.Casa), p.combate.alcance,
                    chefe.AlcanceExtraComoAlvo), "casa " + casa + " nao alcanca o chefe");

            // A escolha de alvo do LuccaCombate (sem outro inimigo) e o chefe.
            Assert.AreSame(chefe, Reflexao.Chamar(p.combate, "EscolherAlvo"), "o tiro automatico nao mira o chefe");
            Assert.AreEqual(1f, p.barra.FracaoVisivel, 1e-4f, "barra cheia");

            // Flechas reais, sem atalho: o LuccaCombate atira sozinho.
            int flechas = p.status.FlechasAtuais;
            Time.timeScale = 2f;
            yield return Esperar(() => chefe == null || chefe.PvAtual < chefe.PvMaximo || !p.status.EstaVivo, 25f,
                () => "dano no chefe (PV " + (chefe != null ? chefe.PvAtual + "/" + chefe.PvMaximo : "-") +
                      ", flechas " + p.status.FlechasAtuais + ")");
            Time.timeScale = 1f;

            if (!p.status.EstaVivo) Assert.Inconclusive("o Lucca caiu antes da primeira flecha chegar");
            Assert.IsNotNull(chefe);
            Assert.Less(chefe.PvAtual, chefe.PvMaximo, "nenhuma flecha feriu o chefe");
            Assert.Less(p.status.FlechasAtuais, flechas, "o Lucca nao gastou flecha");
            Assert.Less(p.barra.FracaoVisivel, 1f, "a barra acompanha o PV");
            Assert.AreEqual(EstadoDaPartida.Chefe, p.diretor.Estado);
        }

        [UnityTest]
        public IEnumerator MorteDoChefe_VitoriaUmaVez_SalvaEVoltaAoAcampamentoComOsDados()
        {
            yield return new EnterPlayMode();
            yield return MorteDoChefe_VitoriaUmaVez_SalvaEVoltaAoAcampamentoComOsDadosEmPlayMode();
            yield return new ExitPlayMode();
        }

        static IEnumerator MorteDoChefe_VitoriaUmaVez_SalvaEVoltaAoAcampamentoComOsDadosEmPlayMode()
        {
            yield return null;
            yield return null;
            Partida p = Montar(usarRevoada: false);
            p.diretor.aoVencer.AddListener(p.tela.MostrarVitoria); // o que o PR de cena faz no Inspector
            yield return null;

            DadosDoJogador dados = DadosDoJogador.Instancia;
            int vitoriasAntes = dados.Vitorias;
            FecharQuatroVoltas(p);
            Inimigo chefe = p.encontro.Chefe;
            Assert.IsNotNull(chefe);
            Assert.IsFalse(p.tela.painel.activeSelf);

            chefe.ReceberDano(chefe.PvMaximo);
            Assert.AreEqual(EstadoDaPartida.Vitoria, p.diretor.Estado);
            Assert.IsTrue(p.diretor.Venceu);
            Assert.IsFalse(p.diretor.GameOver);
            Assert.AreEqual(1, p.vitorias);
            Assert.AreEqual(0, p.derrotas);
            Assert.IsTrue(p.encontro.Derrotado);
            Assert.IsNull(p.encontro.Chefe);
            Assert.IsFalse(p.corredor.andando, "na vitoria o Lucca para");
            Assert.IsTrue(p.inimigos.pausarSpawnComum);
            Assert.IsTrue(p.tela.painel.activeSelf, "aoVencer mostra a tela de resultado");
            StringAssert.Contains("derrotou o chefe", p.tela.textoResumo.text);
            Assert.AreEqual(vitoriasAntes + 1, dados.Vitorias);
            Assert.GreaterOrEqual(dados.RecordeDeVoltas, 4);
            Assert.AreEqual(-1f, p.barra.FracaoVisivel, "a barra some");
            Assert.IsFalse(p.mapa.PodeColocar(Basica(), CasaLonge(p), out string motivoVitoria), "carta depois da vitoria");
            Assert.AreEqual("a jornada terminou", motivoVitoria);

            // Nada desfaz a vitoria: Lucca morrendo depois, ou o chefe "caindo" de novo.
            p.status.ReceberDano(p.status.PvMaximoTotal + 1000);
            Reflexao.Chamar(p.diretor, "ChefeDerrotado");
            Assert.AreEqual(EstadoDaPartida.Vitoria, p.diretor.Estado);
            Assert.AreEqual(1, p.vitorias);
            Assert.AreEqual(0, p.derrotas, "derrota depois da vitoria");
            Assert.AreEqual(vitoriasAntes + 1, dados.Vitorias);

            // O save em disco tem a vitoria.
            int moedas = dados.MoedasTotais, recorde = dados.RecordeDeVoltas, vitorias = dados.Vitorias;
            Assert.IsTrue(DadosDoJogador.TentarLer(File.ReadAllText(dados.CaminhoDoArquivo),
                out int mDisco, out int rDisco, out int vDisco, out string motivo), motivo);
            Assert.AreEqual(moedas, mDisco);
            Assert.AreEqual(recorde, rDisco);
            Assert.AreEqual(vitorias, vDisco);

            // Voltar ao acampamento pelo botao da tela preserva tudo.
            yield return null;
            Assert.IsTrue(p.combate.AlvoAtual == null, "o Lucca para de mirar depois da vitoria");
            p.tela.VoltarAoAcampamento();
            yield return Esperar(() => SceneManager.GetActiveScene().name == "Acampamento", 20f,
                () => "carregar o Acampamento (cena ativa " + SceneManager.GetActiveScene().name + ")");
            yield return null;

            Assert.AreSame(dados, DadosDoJogador.Instancia, "o singleton atravessa a troca de cena");
            Assert.AreEqual(moedas, DadosDoJogador.Instancia.MoedasTotais);
            Assert.AreEqual(recorde, DadosDoJogador.Instancia.RecordeDeVoltas);
            Assert.AreEqual(vitorias, DadosDoJogador.Instancia.Vitorias);
            DadosDoJogador.Instancia.Carregar();
            Assert.AreEqual(moedas, DadosDoJogador.Instancia.MoedasTotais, "recarregado do disco");
            Assert.AreEqual(recorde, DadosDoJogador.Instancia.RecordeDeVoltas);
            Assert.AreEqual(vitorias, DadosDoJogador.Instancia.Vitorias);
        }

        [UnityTest]
        public IEnumerator MorteDoLuccaNoChefe_EDerrota_EOChefeCaindoDepoisNaoViraVitoria()
        {
            yield return new EnterPlayMode();
            yield return MorteDoLuccaNoChefe_EDerrota_EOChefeCaindoDepoisNaoViraVitoriaEmPlayMode();
            yield return new ExitPlayMode();
        }

        static IEnumerator MorteDoLuccaNoChefe_EDerrota_EOChefeCaindoDepoisNaoViraVitoriaEmPlayMode()
        {
            yield return null;
            yield return null;
            Partida p = Montar(usarRevoada: false);
            p.diretor.aoVencer.AddListener(p.tela.MostrarVitoria);
            yield return null;

            int vitoriasAntes = DadosDoJogador.Instancia.Vitorias;
            FecharQuatroVoltas(p);
            Inimigo chefe = p.encontro.Chefe;
            Assert.IsNotNull(chefe);

            p.status.ReceberDano(p.status.PvMaximoTotal + 1000);
            Assert.AreEqual(EstadoDaPartida.Derrota, p.diretor.Estado);
            Assert.IsTrue(p.diretor.GameOver);
            Assert.IsTrue(p.diretor.DerrotaNoChefe);
            Assert.AreEqual(1, p.derrotas);
            Assert.IsTrue(p.tela.painel.activeSelf, "aoGameOver da cena mostra a tela");
            StringAssert.Contains("diante do chefe", p.tela.textoResumo.text);
            Assert.IsFalse(p.mapa.PodeColocar(Basica(), CasaLonge(p), out string motivoDerrota), "carta depois da derrota");
            Assert.AreEqual("a jornada terminou", motivoDerrota);

            // Uma flecha em voo que mata o chefe depois nao transforma a derrota em vitoria.
            chefe.ReceberDano(chefe.PvMaximo);
            Assert.AreEqual(EstadoDaPartida.Derrota, p.diretor.Estado);
            Assert.AreEqual(0, p.vitorias);
            Assert.AreEqual(1, p.derrotas);
            Assert.AreEqual(vitoriasAntes, DadosDoJogador.Instancia.Vitorias, "derrota nao conta vitoria");
            StringAssert.Contains("diante do chefe", p.tela.textoResumo.text, "a tela nao trocou para vitoria");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Revoada_AvisaComIndicadorEChamaMorcegosComuns()
        {
            yield return new EnterPlayMode();
            yield return Revoada_AvisaComIndicadorEChamaMorcegosComunsEmPlayMode();
            yield return new ExitPlayMode();
        }

        static IEnumerator Revoada_AvisaComIndicadorEChamaMorcegosComunsEmPlayMode()
        {
            yield return null;
            yield return null;
            Partida p = Montar(usarRevoada: true);
            p.encontro.intervaloDaRevoada = 2f;
            p.encontro.avisoDaRevoada = 1f;
            p.encontro.morcegosPorRevoada = 2;
            yield return null;

            float aviso = -1f, uso = -1f;
            int nasceram = -1;
            bool indicadorVisivelNoAviso = false;
            p.encontro.AoAvisarEspecial += _ => aviso = Time.time;
            p.encontro.AoUsarEspecial += n => { uso = Time.time; nasceram = n; };

            FecharQuatroVoltas(p);
            Assert.IsNotNull(p.encontro.Chefe);
            InimigoDef morcego = p.inimigos.morcego;
            Assert.AreEqual(0, p.inimigos.QuantosVivos(morcego), "spawn comum desligado no teste");

            yield return Esperar(() => aviso >= 0f || !p.status.EstaVivo, 10f, () => "aviso da Revoada");
            yield return null;
            indicadorVisivelNoAviso = p.indicador.Visivel;
            Assert.IsTrue(p.encontro.AvisandoEspecial);

            yield return Esperar(() => uso >= 0f || !p.status.EstaVivo, 10f, () => "Revoada");
            if (!p.status.EstaVivo) Assert.Inconclusive("o Lucca caiu durante a espera");
            yield return null;

            Assert.IsTrue(indicadorVisivelNoAviso, "o indicador de perigo aparece durante o aviso");
            Assert.Less(aviso, uso, "o aviso vem antes do ataque");
            Assert.GreaterOrEqual(uso - aviso, 0.9f, "o aviso dura ~1 s");
            Assert.Greater(nasceram, 0, "a Revoada chamou morcegos");
            Assert.LessOrEqual(nasceram, 2);
            Assert.AreEqual(nasceram, p.inimigos.QuantosVivos(morcego));
            Assert.IsFalse(p.indicador.Visivel, "o indicador some depois do ataque");
            Assert.IsTrue(p.inimigos.pausarSpawnComum, "o spawn comum segue pausado no chefe");
        }
    }
}

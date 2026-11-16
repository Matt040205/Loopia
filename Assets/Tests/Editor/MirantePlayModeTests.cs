using System;
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Loopia.Hex.Tests
{
    /// <summary>
    /// G2-C integrado ao LuccaCombate do Codex, na HexPrototipo em Play Mode: Mola + Ilha alta reais, Lucca
    /// teleportado para a ilha levantada e um alvo inofensivo a 4 casas, atras dele. Cobre entrada, saida, alvo
    /// lateral/traseiro, o tiro reservado revalidado na soltura e a regra do chefe intacta. Nada e salvo.
    /// </summary>
    [Category("PlayModeSmoke")]
    public class MirantePlayModeTests
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
            SaveIsolado.Restaurar();
            CenaDoTeste.Restaurar();
            yield return null;
        }

        sealed class Campo
        {
            public HexPlayer player;
            public LuccaCombate combate;
            public LuccaStatus status;
            public InimigoSpawner inimigos;
            public MapaDePlataformas mapa;
            public HexLoopGenerator gerador;
            public HexCoord alta, perto, longe;
            public InimigoDef def;
        }

        static T Achar<T>() where T : Object
        {
            var o = Object.FindFirstObjectByType<T>();
            Assert.IsNotNull(o, typeof(T).Name);
            return o;
        }

        static PlataformaDef Carta(string nome) =>
            AssetDatabase.LoadAssetAtPath<PlataformaDef>("Assets/Dados/Plataformas/" + nome + ".asset");

        static IEnumerator Aguardar(Func<bool> condicao, string contexto, float segundos = 12f)
        {
            float limite = Time.realtimeSinceStartup + segundos;
            while (!condicao())
            {
                if (Time.realtimeSinceStartup > limite) Assert.Fail("timeout: " + contexto);
                yield return null;
            }
        }

        /// <summary>Mapa fixo, Mola e Ilha alta no anel, Lucca parado; acha uma casa do anel a 4 da ilha alta.</summary>
        static IEnumerator Montar(Campo c)
        {
            Application.runInBackground = true;
            for (int i = 0; i < 5; i++) yield return null;
            c.player = Achar<HexPlayer>();
            c.combate = Achar<LuccaCombate>();
            c.status = Achar<LuccaStatus>();
            c.inimigos = Achar<InimigoSpawner>();
            c.mapa = Achar<MapaDePlataformas>();
            c.gerador = Achar<HexLoopGenerator>();

            c.gerador.usarSementeFixa = true;
            c.gerador.semente = 12345;
            Assert.IsTrue(c.gerador.Gerar());
            var corredor = Achar<HexLoopRunner>();
            corredor.andando = false;
            c.inimigos.nascerMorcegos = false;
            c.inimigos.LimparTodos();
            c.player.NascerNoAcampamento();
            yield return null;
            c.mapa.LimparTudo();

            HexLoopPath loop = c.gerador.Loop;
            int n = loop.Count;
            Assert.IsNotNull(c.mapa.Colocar(Carta("Mola"), loop[n / 4]));
            c.alta = loop[n / 4 + 2];
            Assert.IsNotNull(c.mapa.Colocar(Carta("IlhaAlta"), c.alta), "nao colocou a Ilha alta");
            yield return null;
            Assert.IsTrue(c.gerador.mundo.TryGetIlha(c.alta, out HexTile ilha));
            Assert.Greater(ilha.Altura, 1f, "a ilha alta precisa estar levantada");

            // Uma casa do anel a exatamente 4 da ilha alta (fora do alcance base 3) e uma casa sem relevo a 3 dela.
            bool achouLonge = false, achouPerto = false;
            foreach (HexCoord casa in loop.Casas)
            {
                if (casa == c.alta || c.mapa.Tem(casa)) continue;
                if (!achouLonge && HexCoord.Distance(casa, c.alta) == 4) { c.longe = casa; achouLonge = true; }
            }
            Assert.IsTrue(achouLonge, "nao ha casa do anel a 4 da ilha alta");
            foreach (HexCoord casa in loop.Casas)
            {
                if (casa == c.alta || c.mapa.Tem(casa)) continue;
                if (HexCoord.Distance(casa, c.longe) == 4 && HexCoord.Distance(casa, c.alta) >= 2)
                { c.perto = casa; achouPerto = true; break; }
            }
            Assert.IsTrue(achouPerto, "nao ha casa sem relevo a 4 do alvo");

            c.def = ScriptableObject.CreateInstance<InimigoDef>();
            c.def.nome = "Alvo do mirante";
            c.def.voa = true;
            c.def.ataque = 0;
            c.def.xp = 0;
            c.def.pv = 9000;
            c.def.alcance = 0;
            c.def.raioDeDeteccao = 0.1f;
        }

        /// <summary>Teleporta e vira o Lucca de costas para o alvo (o alvo fica atras dele).</summary>
        static void ColocarDeCostas(Campo c, HexCoord casa, Inimigo alvo)
        {
            c.player.ColocarEm(casa);
            Vector3 para = alvo != null ? alvo.transform.position - c.player.transform.position : Vector3.forward;
            para.y = 0f;
            if (para.sqrMagnitude > 0.0001f) c.player.transform.rotation = Quaternion.LookRotation(-para.normalized);
        }

        [UnityTest]
        public IEnumerator Mirante_EntradaESaida_MudaOAlcanceDoCombate()
        {
            yield return new EnterPlayMode();
            yield return EntradaESaida();
            yield return new ExitPlayMode();
        }

        static IEnumerator EntradaESaida()
        {
            var c = new Campo();
            yield return Montar(c);
            Assert.AreEqual(3, c.combate.alcance);

            c.player.ColocarEm(c.alta);
            yield return null;
            yield return null;
            Assert.IsTrue(c.combate.NoMirante, "na ilha alta levantada");
            Assert.AreEqual(4, c.combate.AlcanceAtual);

            c.player.ColocarEm(c.perto);
            yield return null;
            yield return null;
            Assert.IsFalse(c.combate.NoMirante, "saiu da ilha: o efeito acaba");
            Assert.AreEqual(3, c.combate.AlcanceAtual);
            Object.Destroy(c.def);
        }

        [UnityTest]
        public IEnumerator Mirante_AlvoAtrasA4Casas_EEscolhidoEAtingido_ForaDoMiranteNao()
        {
            yield return new EnterPlayMode();
            yield return AlvoLateral();
            yield return new ExitPlayMode();
        }

        static IEnumerator AlvoLateral()
        {
            var c = new Campo();
            yield return Montar(c);
            Inimigo alvo = c.inimigos.Nascer(c.def, c.longe);
            Assert.IsNotNull(alvo);

            // Fora do Mirante, a 4 casas e de costas: nada.
            ColocarDeCostas(c, c.perto, alvo);
            for (int i = 0; i < 10; i++) yield return null;
            Assert.IsNull(c.combate.AlvoAtual, "fora do mirante, 4 casas esta fora do alcance");

            // No Mirante, de costas para o alvo a 4 casas: escolhe e solta uma flecha (com animacao do Codex).
            int tiros = 0;
            c.combate.AoDisparar += (a, _) => { if (a == alvo) tiros++; };
            int flechas = c.status.FlechasAtuais;
            ColocarDeCostas(c, c.alta, alvo);
            yield return Aguardar(() => c.combate.AlvoAtual == alvo, "alvo escolhido no mirante");
            yield return Aguardar(() => tiros >= 1, "flecha solta no alvo atras do Lucca");
            Assert.AreEqual(flechas - tiros, c.status.FlechasAtuais, "uma flecha por tiro");
            Object.Destroy(c.def);
        }

        [UnityTest]
        public IEnumerator Mirante_AlvoAtras_OArcoApontaParaOAlvoNaSoltura()
        {
            yield return new EnterPlayMode();
            yield return MiraVisualAtras();
            yield return new ExitPlayMode();
        }

        /// <summary>
        /// Estabilizacao do MVP (P2-1): de costas para o alvo, o corpo visual vira o que o tronco (65°) nao alcanca. Mede o
        /// angulo horizontal entre o arco e o alvo na soltura com o giro do corpo e, para comparar, sem ele (como antes).
        /// </summary>
        static IEnumerator MiraVisualAtras()
        {
            var c = new Campo();
            yield return Montar(c);
            var ponte = Object.FindFirstObjectByType<LuccaAnimationBridge>();
            if (ponte == null || !ponte.AnimaTiros) Assert.Inconclusive("sem o modelo animado do Lucca na cena");
            Inimigo alvo = c.inimigos.Nascer(c.def, c.longe);
            Assert.IsNotNull(alvo);
            int tiros = 0;
            c.combate.AoDisparar += (a, _) => { if (a == alvo) tiros++; };
            Quaternion raizAntes = c.player.transform.rotation;

            ColocarDeCostas(c, c.alta, alvo);
            raizAntes = c.player.transform.rotation;
            yield return Aguardar(() => tiros >= 1, "flecha solta no alvo atras do Lucca");
            float comCorpo = ponte.ErroDeMiraNaSoltura;
            Assert.Less(Quaternion.Angle(raizAntes, c.player.transform.rotation), 1f, "a raiz de navegacao nao pode girar");

            Reflexao.DefinirCampo(ponte, "giroMaximoDoCorpo", 0f);
            yield return Aguardar(() => tiros >= 2, "segunda flecha, sem o giro do corpo");
            float soTronco = ponte.ErroDeMiraNaSoltura;
            Reflexao.DefinirCampo(ponte, "giroMaximoDoCorpo", 120f);

            Debug.Log("[Mirante] erro horizontal da mira na soltura, alvo atras: com o corpo " + comCorpo.ToString("0.0") +
                      " graus; so o tronco (antes) " + soTronco.ToString("0.0") + " graus");
            Assert.Less(comCorpo, 25f, "o arco tem que apontar para o alvo atras");
            Assert.Greater(soTronco, 45f, "sem o giro do corpo o tronco nao alcanca (caracterizacao do defeito)");
            Object.Destroy(c.def);
        }

        [UnityTest]
        public IEnumerator Mirante_TiroReservado_ECanceladoSeOLuccaSairAntesDaSoltura()
        {
            yield return new EnterPlayMode();
            yield return TiroReservado();
            yield return new ExitPlayMode();
        }

        static IEnumerator TiroReservado()
        {
            var c = new Campo();
            yield return Montar(c);
            Inimigo alvo = c.inimigos.Nascer(c.def, c.longe);
            int tiros = 0;
            c.combate.AoDisparar += (a, _) => tiros++;
            int flechas = c.status.FlechasAtuais;

            ColocarDeCostas(c, c.alta, alvo);
            yield return Aguardar(() => c.combate.AlvoDaPreparacao == alvo, "tiro reservado no mirante");
            Assert.AreEqual(0, tiros, "ainda nao soltou");

            // Sai do Mirante antes da soltura: na revalidacao o alvo esta a 4 casas, fora do alcance base.
            ColocarDeCostas(c, c.perto, alvo);
            yield return Aguardar(() => c.combate.AlvoDaPreparacao == null, "reserva cancelada");
            yield return new WaitForSeconds(1f);
            Assert.AreEqual(0, tiros, "nenhuma flecha depois de sair do mirante");
            Assert.AreEqual(flechas, c.status.FlechasAtuais, "municao intacta");
            Object.Destroy(c.def);
        }

        [UnityTest]
        public IEnumerator Mirante_NaoMudaARegraDoChefe()
        {
            yield return new EnterPlayMode();
            yield return Chefe();
            yield return new ExitPlayMode();
        }

        static IEnumerator Chefe()
        {
            var c = new Campo();
            yield return Montar(c);
            // Alvo grande alem do alcance das duas posicoes (>= 5 da ilha alta e >= 4 da casa comum). Ele voa:
            // pode estar fora do anel. O alcance extra (como o do chefe) o cobre com ou sem Mirante (regra de G1).
            HexCoord distante = c.alta;
            int melhor = -1;
            for (int d = 0; d < 6; d++)
            {
                HexCoord candidato = c.alta + (c.alta.Neighbor(d) - c.alta) * 6;
                int pior = Mathf.Min(HexCoord.Distance(candidato, c.alta) - 1, HexCoord.Distance(candidato, c.perto));
                if (pior > melhor) { melhor = pior; distante = candidato; }
            }
            Assert.GreaterOrEqual(HexCoord.Distance(distante, c.alta), 5);
            Assert.GreaterOrEqual(HexCoord.Distance(distante, c.perto), 4);
            Inimigo grande = c.inimigos.Nascer(c.def, distante);
            Assert.IsNotNull(grande);
            grande.TornarAlvoGrande(10);

            ColocarDeCostas(c, c.perto, grande);
            yield return Aguardar(() => c.combate.AlvoAtual == grande, "alvo grande fora do mirante");
            ColocarDeCostas(c, c.alta, grande);
            yield return Aguardar(() => c.combate.AlvoAtual == grande, "alvo grande no mirante");
            Object.Destroy(c.def);
        }
    }
}

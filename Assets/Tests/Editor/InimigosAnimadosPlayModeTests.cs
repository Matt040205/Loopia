using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace Loopia.Hex.Tests
{
    /// <summary>
    /// Sprint 5B, integracao na HexPrototipo em Play Mode: o visual animado acompanha o gameplay sem mudar nada dele.
    /// Morcego: modelo no lugar da esfera, Attack no mergulho real, um dano por acerto, morte sem recompensa dupla.
    /// Lobo: NavMeshAgent no GameObject de gameplay, locomocao e ataque animados. Chefe: modelo proprio, Revoada
    /// sincronizada com AoAvisarEspecial/AoUsarEspecial e vitoria intacta. Qualquer erro no console reprova o teste.
    /// </summary>
    [Category("PlayModeSmoke")]
    public class InimigosAnimadosPlayModeTests
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

        sealed class Cena
        {
            public InimigoSpawner inimigos;
            public HexLoopGenerator gerador;
            public HexLoopRunner corredor;
            public HexPlayer player;
            public LuccaStatus status;
            public HexWorld mundo;
            public LoopDirector diretor;
            public EncontroDoChefe encontro;
        }

        static T Achar<T>() where T : UnityEngine.Object
        {
            var o = UnityEngine.Object.FindFirstObjectByType<T>();
            Assert.IsNotNull(o, "nao achei " + typeof(T).Name);
            return o;
        }

        static Cena Montar()
        {
            Application.runInBackground = true;
            Time.timeScale = 1f;
            var c = new Cena
            {
                inimigos = Achar<InimigoSpawner>(), gerador = Achar<HexLoopGenerator>(), corredor = Achar<HexLoopRunner>(),
                player = Achar<HexPlayer>(), status = Achar<LuccaStatus>(), mundo = Achar<HexWorld>(),
                diretor = Achar<LoopDirector>(), encontro = Achar<EncontroDoChefe>(),
            };
            c.inimigos.nascerMorcegos = false;
            c.inimigos.LimparTodos();
            c.corredor.andando = false;
            // A Lucca atira sozinha: sem isto o alvo do teste morre antes de mergulhar/correr (so nesta sessao).
            Achar<LuccaCombate>().enabled = false;
            // Parada no acampamento: andando=false nao esvazia o caminho que ja estava na fila.
            c.player.ColocarEm(c.mundo.Acampamento);
            return c;
        }

        static IEnumerator Esperar(Func<bool> condicao, float segundos, Func<string> descricao)
        {
            float fim = Time.realtimeSinceStartup + segundos;
            while (!condicao())
            {
                if (Time.realtimeSinceStartup > fim) Assert.Fail("timeout " + segundos + "s: " + descricao());
                yield return null;
            }
        }

        static HexCoord CasaVizinhaDoLucca(Cena c) =>
            c.gerador.Loop.Casas.First(h => HexCoord.Distance(h, c.player.Coord) == 1);

        static AnimacaoDoInimigo Visual(Inimigo i)
        {
            var a = i.GetComponentInChildren<AnimacaoDoInimigo>();
            Assert.IsNotNull(a, i.name + " sem visual animado");
            return a;
        }

        static bool NoEstado(Animator an, string estado) =>
            an.GetCurrentAnimatorStateInfo(0).IsName(estado) || (an.IsInTransition(0) && an.GetNextAnimatorStateInfo(0).IsName(estado));

        // ------------------------------------------------------------------ morcego

        [UnityTest]
        public IEnumerator Morcego_ModeloAnimado_AtaqueSincronizado_DanoUmaVez_MorteSemRecompensaDupla()
        {
            yield return new EnterPlayMode();
            yield return Morcego_ModeloAnimado_AtaqueSincronizado_DanoUmaVez_MorteSemRecompensaDuplaEmPlayMode();
            yield return new ExitPlayMode();
        }

        // EnterPlayMode recarrega o dominio: o corpo roda num iterador novo, criado ja em Play Mode.
        static IEnumerator Morcego_ModeloAnimado_AtaqueSincronizado_DanoUmaVez_MorteSemRecompensaDuplaEmPlayMode()
        {
            Application.runInBackground = true; // Editor fora de foco congela o Play Mode no frame 2
            yield return null;
            yield return null;
            var c = Montar();
            yield return null;

            Inimigo m = c.inimigos.Nascer(c.inimigos.morcego, CasaVizinhaDoLucca(c));
            Assert.IsNotNull(m);
            var visual = Visual(m);
            Assert.IsNull(m.GetComponentsInChildren<MeshFilter>().FirstOrDefault(f => f.sharedMesh != null && f.sharedMesh.name.Contains("Sphere")),
                "o morcego ainda e uma esfera");
            var smr = m.GetComponentInChildren<SkinnedMeshRenderer>();
            Assert.AreEqual("Bat_Common_Mesh", smr.sharedMesh.name);
            Assert.IsFalse(visual.animator.applyRootMotion);
            Assert.AreEqual(c.inimigos.morcego.escala,
                VisualDeIlha.Medida(VisualDeIlha.LimitesLocais(visual.transform).size, false, EixoDeMedida.MaiorEixo) *
                visual.transform.localScale.x, 0.01f, "envergadura = InimigoDef.escala (uma normalizacao so)");
            Assert.AreEqual(Vector3.zero, visual.transform.localPosition, "pivo preservado na ancora de voo");

            // Um mergulho real: Attack entra com o rasante, a mordida casa com o acerto, o dano chega uma vez.
            int golpes = 0, rasantes = 0;
            c.inimigos.aoInimigoAtacar.AddListener(_ => golpes++);
            m.aoComecarRasante.AddListener(() => rasantes++);
            int pv = c.status.PvAtual;
            float contato = -1f;
            m.aoAtacar.AddListener(_ => contato = Time.time);
            yield return Esperar(() => m.EstaMergulhando, 6f, () => "o morcego nao mergulhou");
            yield return null;
            Assert.IsTrue(visual.animator.GetBool(AnimacaoDoInimigo.PAtacando), "Atacando nao ligou no rasante");
            yield return null;
            Assert.IsTrue(NoEstado(visual.animator, "Attack"), "o Animator nao entrou em Attack no rasante");
            float tMordida = visual.mordida / visual.duracaoDoAtaque, maiorAntesDoAcerto = 0f;
            while (m.EstaMergulhando)
            {
                if (contato < 0f) maiorAntesDoAcerto = Mathf.Max(maiorAntesDoAcerto, visual.TempoDoAtaqueAtual);
                yield return null;
            }
            Assert.AreEqual(1, rasantes);
            Assert.AreEqual(1, golpes, "um mergulho, um golpe (nenhum Animation Event aplica dano)");
            Assert.AreEqual(pv - m.Ataque, c.status.PvAtual, "o dano do golpe chegou uma vez");
            Assert.Greater(contato, 0f, "o mergulho acertou o Lucca parado");
            Assert.LessOrEqual(maiorAntesDoAcerto, tMordida + 1e-4f, "a mordida do clipe veio antes do acerto real");
            Assert.Greater(maiorAntesDoAcerto, tMordida * 0.5f, "a antecipacao/investida nao acompanhou o mergulho");
            yield return Esperar(() => visual.TempoDoAtaqueAtual >= tMordida - 1e-4f || Time.time > contato + 0.1f, 5f,
                () => "relogio do jogo");
            Assert.GreaterOrEqual(visual.TempoDoAtaqueAtual, tMordida - 1e-4f,
                "a mordida do clipe nao chegou em 0,1 s depois do acerto (" + visual.TempoDoAtaqueAtual + ")");
            yield return Esperar(() => !visual.animator.GetBool(AnimacaoDoInimigo.PAtacando), 2f, () => "Attack nao terminou");
            yield return Esperar(() => !m.EstaEmRasante, 4f, () => "nao voltou ao poleiro");
            Assert.AreEqual(1, golpes, "a volta nao gera dano");

            // Hit (aditivo) com dano nao letal
            int hits = 0;
            m.AoSofrerDano += (_, __) => hits++;
            m.ReceberDano(1);
            Assert.AreEqual(1, hits);
            yield return null;

            // Morte: o Inimigo some no mesmo frame, a recompensa vem uma vez, o visual destacado toca a morte e some.
            int derrotas = 0;
            c.inimigos.AoInimigoDerrotado += _ => derrotas++;
            int nivel = c.status.Nivel, xp = c.status.Xp, ganho = m.Xp, limite = c.status.XpParaOProximoNivel;
            int vivos = c.inimigos.Vivos.Count;
            m.ReceberDano(99999);
            m.ReceberDano(99999);
            Assert.AreEqual(1, derrotas, "derrotado uma vez");
            Assert.AreEqual(vivos - 1, c.inimigos.Vivos.Count);
            if (xp + ganho < limite) Assert.AreEqual(xp + ganho, c.status.Xp, "XP dado uma vez");
            else Assert.Greater(c.status.Nivel, nivel);
            yield return null;
            Assert.IsTrue(m == null, "o GameObject de gameplay continua sendo destruido na morte");
            var mortos = UnityEngine.Object.FindObjectsByType<AnimacaoDoInimigo>(FindObjectsSortMode.None).Where(a => a.Destacado).ToList();
            Assert.AreEqual(1, mortos.Count, "um visual de morte");
            var morto = mortos[0];
            Assert.IsNull(morto.GetComponentInParent<Inimigo>(), "o visual da morte nao tem logica");
            Assert.IsTrue(morto.animator.GetBool(AnimacaoDoInimigo.PMorto));
            yield return null;
            Assert.IsTrue(NoEstado(morto.animator, "Death"));
            Assert.IsFalse(UnityEngine.Object.FindObjectsByType<Inimigo>(FindObjectsSortMode.None).Any(), "nenhum inimigo vivo sobrou");
            float vida = morto.TempoDeVidaDaMorte, inicio = Time.time;
            // WaitForSeconds nao vale no runner de EditMode: espera pelo relogio do jogo.
            yield return Esperar(() => morto == null || Time.time > inicio + vida + 0.5f, vida + 10f, () => "relogio do jogo");
            Assert.IsTrue(morto == null, "o visual da morte nao se destruiu em " + (vida + 0.5f) + " s de jogo");
        }

        // ------------------------------------------------------------------ lobo

        [UnityTest]
        public IEnumerator Lobo_MantemNavMeshAgent_AnimaLocomocaoEAtaqueSemDanoExtra()
        {
            yield return new EnterPlayMode();
            yield return Lobo_MantemNavMeshAgent_AnimaLocomocaoEAtaqueSemDanoExtraEmPlayMode();
            yield return new ExitPlayMode();
        }

        // EnterPlayMode recarrega o dominio: o corpo roda num iterador novo, criado ja em Play Mode.
        static IEnumerator Lobo_MantemNavMeshAgent_AnimaLocomocaoEAtaqueSemDanoExtraEmPlayMode()
        {
            Application.runInBackground = true; // Editor fora de foco congela o Play Mode no frame 2
            yield return null;
            yield return null;
            var c = Montar();
            yield return null;

            Inimigo lobo = c.inimigos.Nascer(c.inimigos.lobo, c.player.Coord);
            Assert.IsNotNull(lobo);
            var agente = lobo.GetComponent<NavMeshAgent>();
            Assert.IsNotNull(agente, "o lobo perdeu o NavMeshAgent");
            Assert.IsTrue(agente.enabled && agente.isOnNavMesh, "agente fora do NavMesh");
            var visual = Visual(lobo);
            Assert.IsNull(visual.GetComponentInChildren<NavMeshAgent>(), "o visual nao tem agente");
            Assert.AreEqual(AnimacaoDoInimigo.TipoDeLocomocao.Patas, visual.locomocao);
            Assert.AreEqual("Wolf_Common_Mesh", lobo.GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh.name);

            // afasta o lobo dentro da propria ilha (o mais longe que o NavMesh deixar) para ele correr ate o Lucca
            Vector3 pe = c.player.transform.position;
            float melhor = 0f;
            Vector3 destino = pe;
            for (int i = 0; i < 12; i++)
            {
                Vector3 tentativa = pe + Quaternion.Euler(0f, i * 30f, 0f) * Vector3.forward * 1.8f;
                if (NavMesh.SamplePosition(tentativa, out NavMeshHit h, 0.6f, agente.areaMask) &&
                    c.mundo.MundoParaHex(h.position) == c.player.Coord)
                {
                    float d = Vector3.Distance(h.position, pe);
                    if (d > melhor) { melhor = d; destino = h.position; }
                }
            }
            Assert.Greater(melhor, 1f, "sem lugar no NavMesh da ilha para afastar o lobo");
            agente.Warp(destino);
            int golpes = 0, ataquesVisuais = 0;
            bool viuCorrida = false;
            float maiorVelocidade = 0f;
            c.inimigos.aoInimigoAtacar.AddListener(_ => golpes++);
            bool atacandoAntes = false;
            float fim = Time.time + 6f;
            while (Time.time < fim && c.status.EstaVivo)
            {
                maiorVelocidade = Mathf.Max(maiorVelocidade, agente.velocity.magnitude);
                if (visual.animator.GetFloat(AnimacaoDoInimigo.PVelocidade) > 0.5f && NoEstado(visual.animator, "Locomocao")) viuCorrida = true;
                bool atacando = visual.animator.GetBool(AnimacaoDoInimigo.PAtacando);
                if (atacando && !atacandoAntes) ataquesVisuais++;
                atacandoAntes = atacando;
                yield return null;
            }
            Assert.Greater(maiorVelocidade, 1f, "o agente do lobo nao andou");
            Assert.IsTrue(viuCorrida, "o lobo nao animou a locomocao enquanto o agente andava (maior velocidade " +
                                      maiorVelocidade.ToString("0.00") + " u/s)");
            Assert.Greater(golpes, 0, "o lobo nao atacou");
            Assert.AreEqual(golpes, ataquesVisuais, "cada golpe teve um ataque visual (e nenhum dano extra)");
            Assert.IsFalse(visual.animator.applyRootMotion);
        }

        // ------------------------------------------------------------------ chefe

        [UnityTest]
        public IEnumerator Chefe_ModeloProprio_RevoadaSincronizada_VitoriaIntacta()
        {
            yield return new EnterPlayMode();
            yield return Chefe_ModeloProprio_RevoadaSincronizada_VitoriaIntactaEmPlayMode();
            yield return new ExitPlayMode();
        }

        // EnterPlayMode recarrega o dominio: o corpo roda num iterador novo, criado ja em Play Mode.
        static IEnumerator Chefe_ModeloProprio_RevoadaSincronizada_VitoriaIntactaEmPlayMode()
        {
            Application.runInBackground = true; // Editor fora de foco congela o Play Mode no frame 2
            yield return null;
            yield return null;
            var c = Montar();
            c.encontro.usarRevoada = true;
            c.encontro.intervaloDaRevoada = 4f; // so neste teste (nada e salvo): Revoada logo
            c.encontro.avisoDaRevoada = 2f;
            int vitorias = 0, quedas = 0;
            c.diretor.AoVencer += () => vitorias++;
            c.encontro.AoChefeDerrotado += () => quedas++;
            yield return null;
            for (int v = 0; v < 4; v++) Reflexao.Chamar(c.corredor, "AoEntrarNoHexagono", c.mundo.Acampamento);
            Inimigo chefe = c.encontro.Chefe;
            Assert.IsNotNull(chefe, "o chefe nao nasceu");

            var visual = Visual(chefe);
            Assert.AreEqual("Bat_AncientBoss_Mesh", chefe.GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh.name,
                "o chefe herdou o modelo do morcego comum");
            Assert.IsTrue(visual.temRevoada);
            float envergadura = VisualDeIlha.Medida(VisualDeIlha.LimitesLocais(visual.transform).size, false,
                EixoDeMedida.MaiorEixo) * visual.transform.localScale.x;
            Assert.AreEqual(c.encontro.variante.tamanhoDoVisual, envergadura, 0.02f, "escala do chefe aplicada uma vez");
            Assert.Less(envergadura, 3.2f, "sem multiplicar a escala do morcego de novo");

            float avisou = -1f, usou = -1f, tNoUso = -1f;
            c.encontro.AoAvisarEspecial += _ => avisou = Time.time;
            c.encontro.AoUsarEspecial += _ => { usou = Time.time; tNoUso = visual.TempoDaRevoadaAtual; };
            yield return Esperar(() => avisou > 0f, 8f, () => "sem aviso de Revoada");
            yield return null;
            yield return null;
            Assert.IsTrue(visual.animator.GetBool(AnimacaoDoInimigo.PRevoada), "Revoada nao ligou no aviso");
            yield return Esperar(() => usou > 0f, 4f, () => "sem Revoada");
            float tRaise = visual.raiseDaRevoada / visual.duracaoDaRevoada;
            Assert.AreEqual(tRaise, tNoUso, 0.06f, "o Raise do clipe cai no AoUsarEspecial");
            yield return Esperar(() => !visual.animator.GetBool(AnimacaoDoInimigo.PRevoada), 3f, () => "Revoada nao terminou");

            chefe.ReceberDano(999999);
            Assert.AreEqual(1, quedas, "AoChefeDerrotado uma vez");
            Assert.AreEqual(1, vitorias, "vitoria uma vez");
            yield return null;
            Assert.IsTrue(chefe == null);
            var morto = UnityEngine.Object.FindObjectsByType<AnimacaoDoInimigo>(FindObjectsSortMode.None).FirstOrDefault(a => a.Destacado);
            Assert.IsNotNull(morto, "o Anciao nao tocou a morte");
            Assert.IsTrue(c.diretor.Terminou);
            float depois = Time.time;
            yield return Esperar(() => Time.time > depois + 0.5f, 10f, () => "relogio do jogo");
            Assert.AreEqual(1, vitorias, "a morte animada nao gera segunda vitoria");
        }
    }
}

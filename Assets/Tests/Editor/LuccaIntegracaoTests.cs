using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Loopia.Hex.Tests
{
    public class LuccaIntegracaoTests
    {
        static T Achar<T>() where T : Object
        {
            var componente = Object.FindFirstObjectByType<T>();
            Assert.IsNotNull(componente, typeof(T).Name);
            return componente;
        }

        [UnitySetUp]
        public IEnumerator AbrirCena()
        {
            SaveIsolado.Backup();
            if (!EditorApplication.isPlaying) CenaDoTeste.AbrirCenaPrincipal();
            yield return null;
        }

        [Test, Category("SceneStatic")]
        public void VisualUsaAvatarHumanoidENaoTomaARaiz()
        {
            try
            {
                var player = Achar<HexPlayer>();
                var visual = player.transform.Find("VisualRoot");
                Assert.IsNotNull(visual);
                var animator = visual.GetComponent<Animator>();
                Assert.IsTrue(animator.avatar.isValid && animator.avatar.isHuman);
                Assert.IsFalse(animator.applyRootMotion);
                Assert.AreEqual(2, visual.GetComponentsInChildren<SkinnedMeshRenderer>().Length);
                Assert.IsFalse(player.gameObject.scene.GetRootGameObjects().Any(obj => obj.name == "VisualFlecha"));
                Assert.IsNotNull(Achar<LuccaCombate>().visualDaFlecha);
                Assert.AreEqual("Hand.L", Osso(visual, "Bow_Grip").parent.name);
                Assert.AreEqual("Hand.R", Osso(visual, "Socket_DrawHand").parent.name);
                Assert.AreEqual("Bow_Grip", Osso(visual, "Arrow_Spawn").parent.name);
                Assert.AreEqual(7, animator.runtimeAnimatorController.animationClips.Length);
                var clipes = animator.runtimeAnimatorController.animationClips.ToDictionary(c => c.name);
                Assert.IsTrue(new[] { "Idle", "Walk", "Run" }.All(n => clipes[n].isLooping && clipes[n].isHumanMotion));
                Assert.That(clipes["Walk"].length, Is.EqualTo(52f / 60f).Within(0.001f));
                Assert.That(clipes["Run"].length, Is.EqualTo(0.5f).Within(0.001f));
                float tempo = clipes["Bow Shot"].events.Single(e => e.functionName == "OnBowRelease").time;
                Assert.AreEqual(19f / 60f, tempo, 0.0001f);
            }
            finally { CenaDoTeste.Restaurar(); }
        }

        [UnityTest, Category("PlayModeSmoke")]
        public IEnumerator LocomocaoSegueVelocidadeRealERetomaMarchaAposPulo()
        {
            yield return new EnterPlayMode();
            yield return VerificarLocomocao();
            yield return new ExitPlayMode();
        }

        static IEnumerator VerificarLocomocao()
        {
            Application.runInBackground = true;
            for (int i = 0; i < 5; i++) yield return null;
            var player = Achar<HexPlayer>();
            PrepararCampo(player, Achar<InimigoSpawner>());
            var animator = AnimatorDoPlayer(player);
            var loop = Achar<HexLoopGenerator>().Loop;
            player.Agente.speed = 0.9f;
            Assert.IsTrue(player.MoverPara(loop[4]));
            yield return Aguardar(() => Peso(animator, "Walk") > 0.7f, "Walk a 0,9 u/s");
            yield return Aguardar(() => player.EstaPulando, "pulo a partir do Walk");
            yield return Aguardar(() => !player.EstaPulando && Peso(animator, "Walk") > 0.7f, "retomada do Walk");
            player.Agente.speed = player.velocidade;
            yield return Aguardar(() => Peso(animator, "Run") > 0.8f, "Run a 3,5 u/s");
            yield return Aguardar(() => player.EstaPulando, "pulo a partir do Run");
            yield return Aguardar(() => !player.EstaPulando && Peso(animator, "Run") > 0.8f, "retomada do Run");
            yield return Aguardar(() => player.Coord == loop[4] && Peso(animator, "Idle") > 0.98f, "parada no destino");
            Assert.IsTrue(player.Agente.isOnNavMesh);
            Assert.IsFalse(animator.applyRootMotion);
        }

        static float Peso(Animator animator, string clipe) =>
            animator.GetCurrentAnimatorClipInfo(0).Where(info => info.clip.name == clipe).Sum(info => info.weight);

        [UnityTest, Category("PlayModeSmoke")]
        public IEnumerator Tiro_ConfirmaUmaVez_UsaSocketEToleraEventoAusente()
        {
            yield return new EnterPlayMode();
            yield return VerificarTiro();
            yield return new ExitPlayMode();
        }

        static IEnumerator VerificarTiro()
        {
            Application.runInBackground = true;
            for (int i = 0; i < 5; i++) yield return null;
            var player = Achar<HexPlayer>();
            var combate = Achar<LuccaCombate>();
            var status = Achar<LuccaStatus>();
            var inimigos = Achar<InimigoSpawner>();
            PrepararCampo(player, inimigos);
            var animator = AnimatorDoPlayer(player);
            var socket = Osso(animator.transform, "Arrow_Spawn");
            var def = DefInofensiva();
            var alvo = inimigos.Nascer(def, player.Coord);
            int flechas = status.FlechasAtuais;
            int tiros = 0;
            float ultimo = 0;
            Vector3 origem = default;
            combate.AoDisparar += (acertado, pos) => {
                Assert.AreSame(alvo, acertado);
                tiros++;
                ultimo = Time.time;
                origem = pos;
            };

            yield return Aguardar(() => combate.AlvoDaPreparacao == alvo);
            int id = TiroPendente(combate);
            Assert.AreEqual(flechas, status.FlechasAtuais);
            Assert.Zero(tiros);
            yield return Aguardar(() => tiros == 1, "soltura inicial");
            Assert.AreEqual(flechas - 1, status.FlechasAtuais);
            Assert.Less(Vector3.Distance(origem, socket.position), 0.01f);
            Assert.IsFalse((bool)Reflexao.Chamar(combate, "SoltarTiro", id, socket));
            Assert.AreEqual(flechas - 1, status.FlechasAtuais);

            float primeiro = ultimo;
            animator.fireEvents = false;
            yield return Aguardar(() => tiros == 2);
            Assert.GreaterOrEqual(ultimo - primeiro + 0.0001f, combate.intervaloDeTiro);
            Assert.AreEqual(flechas - 2, status.FlechasAtuais);

            // A reserva perdeu o alvo. O alvo novo recebe sua propria preparacao e soltura.
            yield return Aguardar(() => combate.AlvoDaPreparacao == alvo);
            alvo.ReceberDano(alvo.PvAtual);
            alvo = inimigos.Nascer(def, player.Coord);
            yield return Aguardar(() => combate.AlvoDaPreparacao == alvo);
            Assert.AreEqual(flechas - 2, status.FlechasAtuais);
            Assert.AreEqual(2, tiros);

            // Interromper o estado cancela a reserva atual, sem impedir um novo tiro.
            int interrompido = TiroPendente(combate);
            yield return null;
            animator.Play("Upper Body.Bow Ready", 1, 0f);
            yield return Aguardar(() => TiroPendente(combate) != interrompido);
            Assert.AreEqual(2, tiros);
            yield return Aguardar(() => tiros == 3);

            yield return Aguardar(() => combate.AlvoDaPreparacao == alvo);
            status.ReceberDano(status.PvAtual);
            yield return new WaitForSeconds(0.45f);
            Assert.AreEqual(3, tiros);
            Assert.AreEqual(flechas - 3, status.FlechasAtuais);
            Assert.IsNull(combate.AlvoDaPreparacao);
            Object.Destroy(def);
        }

        [UnityTest, Category("PlayModeSmoke")]
        public IEnumerator SemAnimatorUsaFallback_ESemFlechasSegueCaminhando()
        {
            yield return new EnterPlayMode();
            yield return VerificarFallback();
            yield return new ExitPlayMode();
        }

        static IEnumerator VerificarFallback()
        {
            Application.runInBackground = true;
            for (int i = 0; i < 5; i++) yield return null;
            var player = Achar<HexPlayer>();
            var combate = Achar<LuccaCombate>();
            var status = Achar<LuccaStatus>();
            var inimigos = Achar<InimigoSpawner>();
            PrepararCampo(player, inimigos);
            var animator = AnimatorDoPlayer(player);
            animator.enabled = false;
            var def = DefInofensiva();
            inimigos.Nascer(def, player.Coord);
            int tiros = 0;
            combate.AoDisparar += (_, pos) => {
                Assert.Less(Vector3.Distance(pos, player.transform.position + Vector3.up * combate.alturaDoTiro), 0.001f);
                tiros++;
            };
            yield return Aguardar(() => tiros == 1, "disparo sem Animator");
            while (status.FlechasAtuais > 0) status.GastarFlecha();
            animator.enabled = true;
            var inicio = player.Coord;
            Assert.IsTrue(player.MoverPara(Achar<HexLoopGenerator>().Loop[1]));
            yield return Aguardar(() => player.Coord != inicio, "travessia sem flechas");
            Assert.AreEqual(1, tiros);
            Object.Destroy(def);
        }

        [UnityTest, Category("PlayModeSmoke")]
        public IEnumerator MolaRealUsaSpringJumpEDepoisRetomaONavMesh()
        {
            yield return new EnterPlayMode();
            yield return VerificarMola();
            yield return new ExitPlayMode();
        }

        static IEnumerator VerificarMola()
        {
            Application.runInBackground = true;
            for (int i = 0; i < 5; i++) yield return null;
            var p = Achar<HexPlayer>();
            var gerador = Achar<HexLoopGenerator>();
            gerador.usarSementeFixa = true;
            gerador.semente = 12345;
            Assert.IsTrue(gerador.Gerar());
            PrepararCampo(p, Achar<InimigoSpawner>());
            yield return null;
            var mapa = Achar<MapaDePlataformas>();
            var a = AnimatorDoPlayer(p);
            int indice = gerador.Loop.Count / 4;
            var entrada = gerador.Loop[indice];
            var destino = gerador.Loop[indice + 2];
            p.ColocarEm(gerador.Loop[indice - 1]);
            Assert.IsNotNull(mapa.Colocar(Carta("Mola"), entrada));
            Assert.IsNotNull(mapa.Colocar(Carta("IlhaAlta"), destino));
            yield return null;
            p.ColocarEm(gerador.Loop[indice - 1]);
            Assert.IsTrue(p.Agente.isOnNavMesh, "NavMesh da ilha antes da Mola");
            float duracao = 0;
            bool jump = false;
            p.AoIniciarPulo += (mola, segundos) => {
                if (mola) duracao = segundos;
                else jump = true;
            };
            Assert.IsTrue(p.EnfileirarPasso(entrada));
            Assert.IsTrue(p.EnfileirarPasso(gerador.Loop[indice + 1]));
            Assert.IsTrue(p.EnfileirarPasso(destino));
            yield return Aguardar(() => duracao > 0, "impulso da Mola no link real",
                () => $"casa {p.Coord}, impulso {p.AlturaDoImpulso}, caminho {p.CaminhoAtual.Count}, " +
                    $"NavMesh {p.Agente.isOnNavMesh}, status {p.Agente.pathStatus}, posicao {p.transform.position}");
            int spring = Animator.StringToHash("Base Layer.Spring Jump");
            yield return Aguardar(() => a.GetCurrentAnimatorStateInfo(0).fullPathHash == spring, "estado Spring Jump");
            Assert.IsTrue(p.EstaPulando);
            Assert.AreEqual(0.70f, duracao, 0.001f);
            Assert.IsTrue(jump);
            yield return Aguardar(() => p.Coord == destino && !p.EstaPulando, "chegada a Ilha Alta");
            Assert.IsTrue(p.Agente.isOnNavMesh);
            yield return Aguardar(() => a.GetCurrentAnimatorStateInfo(0).fullPathHash != spring, "fim do Spring Jump");
        }

        [UnityTearDown]
        public IEnumerator Restaurar()
        {
            Time.timeScale = 1f;
            if (EditorApplication.isPlaying) yield return new ExitPlayMode();
            Time.timeScale = 1f;
            SaveIsolado.Restaurar();
            CenaDoTeste.Restaurar();
            yield return null;
        }

        static void PrepararCampo(HexPlayer p, InimigoSpawner inimigos)
        {
            p.GetComponent<HexLoopRunner>().andando = false;
            inimigos.nascerMorcegos = false;
            inimigos.LimparTodos();
            p.NascerNoAcampamento();
        }

        static InimigoDef DefInofensiva()
        {
            var def = ScriptableObject.CreateInstance<InimigoDef>();
            def.nome = "Alvo de teste";
            def.voa = true;
            def.ataque = 0;
            def.xp = 0;
            def.pv = 9000;
            return def;
        }

        static PlataformaDef Carta(string nome) =>
            AssetDatabase.LoadAssetAtPath<PlataformaDef>("Assets/Dados/Plataformas/" + nome + ".asset");

        static Transform Osso(Transform t, string nome) => t.GetComponentsInChildren<Transform>().Single(x => x.name == nome);

        static Animator AnimatorDoPlayer(HexPlayer player)
        {
            var animator = player.GetComponentInChildren<Animator>();
            Assert.IsNotNull(animator, "Animator da Lucca em " + player.gameObject.scene.path);
            return animator;
        }

        static int TiroPendente(LuccaCombate combate) => (int)typeof(LuccaCombate)
            .GetField("_tiroPendente", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            .GetValue(combate);

        static IEnumerator Aguardar(Func<bool> condicao, string contexto = "animacao ou navegacao",
            Func<string> diagnostico = null)
        {
            float limite = Time.realtimeSinceStartup + 12f;
            while (!condicao() && Time.realtimeSinceStartup < limite) yield return null;
            if (!condicao()) Assert.Fail("tempo limite aguardando " + contexto + ". " + diagnostico?.Invoke());
        }
    }
}

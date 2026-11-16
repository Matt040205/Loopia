using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Loopia.Hex.Tests
{
    /// <summary>
    /// Estabilizacao do MVP (P2-1/P2-2): onde o pe de apoio desliza. Mede a velocidade horizontal do pe que esta no chao
    /// (o mais baixo, a menos de 3,5 cm da altura de sola calibrada parado) com o corredor real andando, separada por
    /// contexto: reta, curva antes do pulo / depois do pouso / longe de pulo, pouso, aceleracao e tiro (alvo a frente ou a
    /// mais de 52 graus), mais o erro da mira na soltura.
    ///
    /// O criterio de contato pega tambem quadros do pe em balanco rente ao chao: o P95 fica perto de 5 u/s ate na reta, entao
    /// compare MEDIANAS entre configuracoes, nao o P95 (nem com o P95 de 2,39 u/s da Sprint 2C.2, que media so o meio do
    /// apoio de cada clipe).
    ///
    /// Medicao, nao regra: fica fora da suite padrao (PlayModeMedicao). O resumo vai para o console e para
    /// %TEMP%/loopia_apoio_dos_pes.txt e %TEMP%/loopia_apoio_locomocao.txt.
    /// </summary>
    public class ApoioDosPesMedicaoTests
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

        static T Achar<T>() where T : Object
        {
            var o = Object.FindFirstObjectByType<T>();
            Assert.IsNotNull(o, typeof(T).Name);
            return o;
        }

        [UnityTest, Category("PlayModeMedicao")]
        public IEnumerator Medicao_ApoioDosPes_PorContexto()
        {
            yield return new EnterPlayMode();
            yield return Medir();
            yield return new ExitPlayMode();
        }

        /// <summary>
        /// Captura antes/depois para o relatorio (%TEMP%/loopia_mira_atras_*.png): o tiro no alvo atras no Mirante com e sem
        /// o giro do corpo, no mesmo cenario do PlayModeSmoke. Uma renderizacao por imagem.
        /// </summary>
        [UnityTest, Category("PlayModeMedicao")]
        public IEnumerator Captura_MiraAtras_AntesEDepois()
        {
            try
            {
                yield return new EnterPlayMode();
                // Entrar em Play Mode recarrega o dominio: a chave estatica so vale se for ligada depois.
                CapturaDeEvidencia.Ativa = true;
                yield return MirantePlayModeTests.MiraVisualAtras();
                yield return new ExitPlayMode();
            }
            finally { CapturaDeEvidencia.Ativa = false; }
        }

        sealed class Amostras
        {
            public readonly Dictionary<string, List<float>> porContexto = new Dictionary<string, List<float>>();
            public int tiros;
            public float giroMaximoVisto;
            public readonly List<float> errosLado = new List<float>();
            public readonly List<float> errosAtras = new List<float>();

            public void Somar(string contexto, float v)
            {
                if (!porContexto.TryGetValue(contexto, out var lista)) porContexto[contexto] = lista = new List<float>();
                lista.Add(v);
            }
        }

        static IEnumerator Medir()
        {
            Application.runInBackground = true;
            Time.timeScale = 1f;
            for (int i = 0; i < 5; i++) yield return null;

            var player = Achar<HexPlayer>();
            var corredor = Achar<HexLoopRunner>();
            var inimigos = Achar<InimigoSpawner>();
            var gerador = Achar<HexLoopGenerator>();
            var combate = Achar<LuccaCombate>();
            var status = Achar<LuccaStatus>();
            var ponte = player.GetComponentInChildren<LuccaAnimationBridge>();
            if (ponte == null) Assert.Inconclusive("sem LuccaAnimationBridge na cena");
            var animator = ponte.GetComponent<Animator>();
            Transform peE = Pe(animator, HumanBodyBones.LeftToes, HumanBodyBones.LeftFoot);
            Transform peD = Pe(animator, HumanBodyBones.RightToes, HumanBodyBones.RightFoot);

            // Mapa da cena como esta (regerar ou teleportar dessincroniza o corredor). Para e espera a fila acabar.
            corredor.andando = false;
            corredor.contarVoltas = false; // seis janelas passam de 4 voltas: sem chefe no meio da medicao
            inimigos.nascerMorcegos = false;
            inimigos.LimparTodos();
            float espera = Time.realtimeSinceStartup + 10f;
            while ((player.EstaAndando || player.EstaPulando) && Time.realtimeSinceStartup < espera) yield return null;

            // Sola parada: altura do pe mais baixo acima da raiz, em Idle.
            float t0 = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - t0 < 0.6f) yield return null;
            float sola = Mathf.Min(peE.position.y, peD.position.y) - player.transform.position.y;

            // Alvos parados e inofensivos sobre o anel: o Lucca passa por eles e atira para frente, para o lado e para tras.
            var def = ScriptableObject.CreateInstance<InimigoDef>();
            def.nome = "Alvo da medicao";
            def.voa = true;
            def.ataque = 0;
            def.xp = 0;
            def.pv = 90000;
            def.alcance = 0;
            def.raioDeDeteccao = 0.1f;
            int n = gerador.Loop.Count;
            var noAnel = new List<Inimigo>();
            foreach (int k in new[] { n / 5, 2 * n / 5, 3 * n / 5, 4 * n / 5 })
            {
                Inimigo alvo = inimigos.Nascer(def, gerador.Loop[k]);
                Assert.IsNotNull(alvo, "alvo " + k);
                noAnel.Add(alvo);
            }

            // Como o chefe: alvo grande no miolo do anel, de lado o tempo todo enquanto o Lucca contorna.
            int somaQ = 0, somaR = 0;
            foreach (HexCoord casa in gerador.Loop.Casas) { somaQ += casa.q; somaR += casa.r; }
            HexCoord miolo = new HexCoord(Mathf.RoundToInt((float)somaQ / n), Mathf.RoundToInt((float)somaR / n));

            combate.AoDisparar += (_, __) =>
            {
                if (_amostras == null) return;
                _amostras.tiros++;
                // O erro da soltura e gravado logo antes do disparo; so vale para alvos fora da folga do tronco.
                if (_anguloNaPreparacao <= 52f || float.IsNaN(ponte.ErroDeMiraNaSoltura)) return;
                (_anguloNaPreparacao <= 100f ? _amostras.errosLado : _amostras.errosAtras).Add(ponte.ErroDeMiraNaSoltura);
            };
            var relatorio = new StringBuilder();
            relatorio.AppendLine("[ApoioDosPes] sola parada " + sola.ToString("0.000") + " u; limiar de contato +0,035 u");

            // (parado, correndo): o padrao atual, sem giro do corpo (antes do P2-1) e giro inteiro mesmo correndo.
            var configuracoes = new[] { (120f, 30f), (0f, 0f), (120f, 120f) };
            for (int cenario = 0; cenario < 2; cenario++)
            {
                Inimigo grande = null;
                if (cenario == 1)
                {
                    foreach (Inimigo alvo in noAnel) if (alvo != null) Object.Destroy(alvo.gameObject);
                    grande = inimigos.Nascer(def, miolo);
                    Assert.IsNotNull(grande, "alvo grande no miolo " + miolo);
                    grande.TornarAlvoGrande(10);
                }
                string nome = cenario == 0 ? "alvos no anel" : "alvo grande no miolo (como o chefe)";
                foreach (var (corpo, correndo) in configuracoes)
                {
                    Reflexao.DefinirCampo(ponte, "giroMaximoDoCorpo", corpo);
                    Reflexao.DefinirCampo(ponte, "giroDoCorpoCorrendo", correndo);
                    _nomeDaJanela = nome + ", corpo " + corpo + "/" + correndo;
                    _amostras = new Amostras();
                    status.RestaurarVidaEMunicao(); // sem contar voltas o acampamento nao recarrega
                    corredor.andando = true;
                    yield return Janela(JanelaPorConfiguracao, player, combate, ponte, animator, peE, peD, sola);
                    corredor.andando = false;
                    relatorio.Append(Resumo(nome + ", corpo parado " + corpo + " / correndo " + correndo, _amostras));
                }
            }
            Reflexao.DefinirCampo(ponte, "giroMaximoDoCorpo", 120f);
            Reflexao.DefinirCampo(ponte, "giroDoCorpoCorrendo", 30f);

            string texto = relatorio.ToString();
            Debug.Log(texto);
            try { File.WriteAllText(Path.Combine(Path.GetTempPath(), "loopia_apoio_dos_pes.txt"), texto); }
            catch (Exception) { /* so registro */ }
        }

        /// <summary>
        /// So locomocao (sem alvos). A maior fonte de deslize e a "curva" logo antes de cada pulo e depois de cada pouso: a
        /// raiz do agente ainda olha para a direcao antiga. Compara o giro da marcha do corpo visual desligado e ligado e
        /// registra os quadros antes de dois pulos de cada janela (angulo da raiz e do visual contra a velocidade).
        /// </summary>
        [UnityTest, Category("PlayModeMedicao")]
        public IEnumerator Medicao_ApoioDosPes_SoLocomocao_MarchaLigadaEDesligada()
        {
            yield return new EnterPlayMode();
            yield return Medicao_ApoioDosPes_SoLocomocao();
            yield return new ExitPlayMode();
        }

        static IEnumerator Medicao_ApoioDosPes_SoLocomocao()
        {
            Application.runInBackground = true;
            Time.timeScale = 1f;
            for (int i = 0; i < 5; i++) yield return null;

            var player = Achar<HexPlayer>();
            var corredor = Achar<HexLoopRunner>();
            var inimigos = Achar<InimigoSpawner>();
            var combate = Achar<LuccaCombate>();
            var ponte = player.GetComponentInChildren<LuccaAnimationBridge>();
            if (ponte == null) Assert.Inconclusive("sem LuccaAnimationBridge na cena");
            var animator = ponte.GetComponent<Animator>();
            Transform peE = Pe(animator, HumanBodyBones.LeftToes, HumanBodyBones.LeftFoot);
            Transform peD = Pe(animator, HumanBodyBones.RightToes, HumanBodyBones.RightFoot);

            corredor.andando = false;
            corredor.contarVoltas = false;
            inimigos.nascerMorcegos = false;
            inimigos.LimparTodos();
            float espera = Time.realtimeSinceStartup + 10f;
            while ((player.EstaAndando || player.EstaPulando) && Time.realtimeSinceStartup < espera) yield return null;
            float t0 = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - t0 < 0.6f) yield return null;
            float sola = Mathf.Min(peE.position.y, peD.position.y) - player.transform.position.y;

            var relatorio = new StringBuilder();
            relatorio.AppendLine("[ApoioDosPes] so locomocao, sem alvos; sola parada " + sola.ToString("0.000") + " u");
            _diagnostico.Clear();
            // A velocidade angular do agente (360, 720, 1440) nao mudou nada nesta medicao: a raiz converge sozinha em ~0,3 s.
            foreach (float marcha in new[] { 0f, 75f })
            {
                Reflexao.DefinirCampo(ponte, "giroDaMarchaMaximo", marcha);
                _nomeDaJanela = "giroDaMarchaMaximo " + marcha;
                _amostras = new Amostras();
                corredor.andando = true;
                yield return Janela(JanelaPorConfiguracao, player, combate, ponte, animator, peE, peD, sola);
                corredor.andando = false;
                relatorio.Append(Resumo(_nomeDaJanela, _amostras));
            }
            Reflexao.DefinirCampo(ponte, "giroDaMarchaMaximo", 75f);

            string texto = relatorio.ToString() + _diagnostico;
            Debug.Log(texto);
            try { File.WriteAllText(Path.Combine(Path.GetTempPath(), "loopia_apoio_locomocao.txt"), texto); }
            catch (Exception) { /* so registro */ }
        }

        static Amostras _amostras;
        static readonly Queue<(float, string)> _quadros = new Queue<(float, string)>();
        static readonly StringBuilder _diagnostico = new StringBuilder();
        static int _pulosRegistrados;
        static float _inicioDaJanela;
        static string _nomeDaJanela = "";
        static float _anguloNaPreparacao;

        static Transform Pe(Animator a, HumanBodyBones dedos, HumanBodyBones pe)
        {
            Transform t = a.GetBoneTransform(dedos);
            if (t == null) t = a.GetBoneTransform(pe);
            Assert.IsNotNull(t, "osso " + pe);
            return t;
        }

        const float JanelaPorConfiguracao = 25f;

        /// <summary>Uma janela de tempo com o corredor andando (~uma volta), amostrando a cada frame.</summary>
        static IEnumerator Janela(float segundos, HexPlayer player, LuccaCombate combate, LuccaAnimationBridge ponte, Animator animator,
            Transform peE, Transform peD, float sola)
        {
            float fim = Time.time + segundos;
            _inicioDaJanela = Time.time;
            _pulosRegistrados = 0;
            _quadros.Clear();
            Vector3 antE = peE.position, antD = peD.position;
            bool apoioE = false, apoioD = false;
            float yawAnt = player.transform.eulerAngles.y;
            Vector3 posAnt = player.transform.position;
            float fimDoPulo = -1f;
            bool pulandoAntes = false;
            // Amostras da janela com o instante, para reclassificar as curvas que antecedem um pulo.
            var janela = new List<(float tempo, string contexto, float v)>();
            yield return null;

            while (Time.time < fim)
            {
                yield return null;
                float dt = Time.deltaTime;
                if (dt <= 0f) continue;

                Vector3 raiz = player.transform.position;
                float velRaiz = new Vector2(raiz.x - posAnt.x, raiz.z - posAnt.z).magnitude / dt;
                float yawRate = Mathf.Abs(Mathf.DeltaAngle(yawAnt, player.transform.eulerAngles.y)) / dt;
                posAnt = raiz;
                yawAnt = player.transform.eulerAngles.y;

                var ag = player.Agente;
                Vector3 velPlano = Vector3.ProjectOnPlane(ag.velocity, Vector3.up);
                bool andandoRapido = velPlano.magnitude > 0.05f;
                string quadro = Time.time.ToString("0.000") + " vel " + velPlano.magnitude.ToString("0.00") +
                    " MoveSpeed " + animator.GetFloat("MoveSpeed").ToString("0.00") + " raiz-vs-vel " +
                    (andandoRapido ? Vector3.SignedAngle(player.transform.forward, velPlano, Vector3.up).ToString("0") : "-") +
                    " visual-vs-vel " + (andandoRapido ? Vector3.SignedAngle(ponte.transform.forward, velPlano, Vector3.up).ToString("0") : "-") +
                    " link " + (ag.nextOffMeshLinkData.valid ? Horizontal(ag.nextOffMeshLinkData.startPos - raiz).ToString("0.00") : "-");
                _quadros.Enqueue((Time.time, quadro));
                while (_quadros.Count > 0 && Time.time - _quadros.Peek().Item1 > 0.3f) _quadros.Dequeue();
                if (player.EstaPulando && !pulandoAntes && _pulosRegistrados < 2 && Time.time - _inicioDaJanela > 3f)
                {
                    _pulosRegistrados++;
                    _diagnostico.AppendLine("--- " + _nomeDaJanela + ", quadros antes do pulo " + _pulosRegistrados);
                    foreach (var q in _quadros) _diagnostico.AppendLine(q.Item2);
                }
                if (player.EstaPulando && !pulandoAntes)
                    for (int k = janela.Count - 1; k >= 0 && Time.time - janela[k].tempo < 0.3f; k--)
                        if (janela[k].contexto.StartsWith("curva"))
                            janela[k] = (janela[k].tempo, "curva ate 0,3 s antes do pulo", janela[k].v);
                pulandoAntes = player.EstaPulando;
                if (player.EstaPulando) fimDoPulo = Time.time;
                float alturaE = peE.position.y - raiz.y, alturaD = peD.position.y - raiz.y;
                bool contatoE = alturaE <= alturaD && alturaE < sola + 0.035f;
                bool contatoD = alturaD < alturaE && alturaD < sola + 0.035f;

                string contexto;
                if (player.EstaPulando) contexto = null;
                else if (Time.time - fimDoPulo < 0.15f) contexto = "pouso (0,15 s)";
                else if (animator.GetLayerWeight(1) > 0.1f && TocandoTiro(animator))
                    contexto = AnguloDoAlvo(player, combate) > 52f ? "tiro, alvo a mais de 52 graus" : "tiro, alvo a frente";
                else if (yawRate > 90f) contexto = Time.time - fimDoPulo < 0.3f ? "curva ate 0,3 s depois do pouso" : "curva longe de pulo";
                else if (velRaiz < 0.06f) contexto = "parado";
                else if (Mathf.Abs(velRaiz - player.velocidade) > 0.3f) contexto = "aceleracao";
                else contexto = "reta";

                _amostras.giroMaximoVisto = Mathf.Max(_amostras.giroMaximoVisto, Mathf.Abs(ponte.GiroDoCorpoAtual));
                if (combate.AlvoDaPreparacao != null) _anguloNaPreparacao = AnguloDoAlvo(player, combate);
                if (contexto != null)
                {
                    if (contatoE && apoioE) janela.Add((Time.time, contexto, Horizontal(peE.position - antE) / dt));
                    if (contatoD && apoioD) janela.Add((Time.time, contexto, Horizontal(peD.position - antD) / dt));
                }
                apoioE = contatoE;
                apoioD = contatoD;
                antE = peE.position;
                antD = peD.position;
            }
            foreach (var amostra in janela) _amostras.Somar(amostra.contexto, amostra.v);
        }

        static bool TocandoTiro(Animator a)
        {
            AnimatorStateInfo s = a.GetCurrentAnimatorStateInfo(1);
            return s.IsName("Bow Shot") || a.IsInTransition(1);
        }

        static float AnguloDoAlvo(HexPlayer player, LuccaCombate combate)
        {
            Inimigo alvo = combate.AlvoDaPreparacao != null ? combate.AlvoDaPreparacao : combate.AlvoAtual;
            if (alvo == null) return 0f;
            Vector3 para = alvo.transform.position - player.transform.position;
            para.y = 0f;
            return para.sqrMagnitude < 0.0001f ? 0f : Vector3.Angle(player.transform.forward, para);
        }

        static float Horizontal(Vector3 v) => new Vector2(v.x, v.z).magnitude;

        static string Resumo(string titulo, Amostras a)
        {
            var sb = new StringBuilder();
            var todas = a.porContexto.Values.SelectMany(x => x).ToList();
            sb.AppendLine("[ApoioDosPes] " + titulo + ": " + a.tiros + " tiros, giro do corpo maximo visto " +
                          a.giroMaximoVisto.ToString("0") + " graus; todas " + Linha(todas));
            sb.AppendLine("  erro da mira na soltura: alvo de lado (52 a 100 graus) " + Erros(a.errosLado) +
                          "; alvo atras (mais de 100 graus) " + Erros(a.errosAtras));
            foreach (var par in a.porContexto.OrderByDescending(p => p.Value.Count))
                sb.AppendLine("  " + par.Key + ": " + Linha(par.Value));
            return sb.ToString();
        }

        static string Erros(List<float> v)
        {
            if (v.Count == 0) return "sem tiros";
            var o = v.OrderBy(x => x).ToList();
            return "n " + o.Count + ", mediana " + Pct(o, 0.5f).ToString("0") + ", maximo " + o[o.Count - 1].ToString("0") + " graus";
        }

        static string Linha(List<float> v)
        {
            if (v.Count == 0) return "sem amostras";
            var o = v.OrderBy(x => x).ToList();
            return "n " + o.Count + ", mediana " + Pct(o, 0.5f).ToString("0.00") + ", P95 " + Pct(o, 0.95f).ToString("0.00") +
                   " u/s";
        }

        static float Pct(List<float> o, float p) => o[Mathf.Clamp(Mathf.RoundToInt(p * (o.Count - 1)), 0, o.Count - 1)];
    }
}

using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Loopia.Hex.Tests
{
    /// <summary>
    /// Isola o arquivo de save do jogador (loopia_save.json). O backup fica em disco (pasta temporaria)
    /// e NAO em campo estatico, porque entrar em Play Mode recarrega o dominio e zera a memoria.
    /// Backup() e idempotente: se ja existe um backup, ele e o original e nao e sobrescrito.
    ///
    /// Varios Editors (uma worktree cada) compartilham o MESMO save e a mesma pasta temporaria. Por isso o
    /// backup leva um id do projeto no nome, e uma trava comum impede duas suites ao mesmo tempo: a segunda
    /// falha logo no inicio, antes de tocar no save (incidente da G2, docs/gameplay/g2/G2_TEST_REPORT.md).
    /// </summary>
    public static class SaveIsolado
    {
        static string Save => Path.Combine(Application.persistentDataPath, "loopia_save.json");
        static string Temp => Path.GetTempPath();
        static string ArquivoDeBackup => Path.Combine(Temp, "loopia_tests_save_" + IdDoProjeto + ".bak");
        static string MarcaDeAusente => Path.Combine(Temp, "loopia_tests_save_" + IdDoProjeto + ".absent");
        static string Trava => Path.Combine(Temp, "loopia_tests_save.lock");

        // Formato antigo (branches sem esta correcao): se existir, ha uma suite de outra worktree rodando agora.
        static string BackupAntigo => Path.Combine(Temp, "loopia_tests_save.bak");
        static string AusenteAntigo => Path.Combine(Temp, "loopia_tests_save.absent");

        /// <summary>Trava mais velha que isto e considerada sobra de uma suite interrompida.</summary>
        const double MinutosDeTravaValida = 30;

        static string _id;
        static string IdDoProjeto
        {
            get
            {
                if (_id != null) return _id;
                using (var sha = SHA256.Create())
                {
                    byte[] h = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(Application.dataPath.ToLowerInvariant()));
                    _id = BitConverter.ToString(h, 0, 4).Replace("-", "").ToLowerInvariant();
                }
                return _id;
            }
        }

        public static void Backup()
        {
            GarantirExclusividade();
            if (File.Exists(ArquivoDeBackup) || File.Exists(MarcaDeAusente)) return;

            if (File.Exists(Save)) File.Copy(Save, ArquivoDeBackup, true);
            else File.WriteAllText(MarcaDeAusente, "o save nao existia antes dos testes");
        }

        /// <summary>Falha antes de tocar no save se outra worktree estiver no meio de uma suite.</summary>
        static void GarantirExclusividade()
        {
            if (File.Exists(BackupAntigo) || File.Exists(AusenteAntigo))
                Assert.Fail("Outra worktree (backup no formato antigo em " + Temp + ") esta rodando testes que mexem no " +
                            "save. Espere ela terminar; se for sobra de uma execucao interrompida, confira o save e apague o arquivo.");

            if (File.Exists(Trava))
            {
                string[] linhas = File.ReadAllLines(Trava);
                bool minha = linhas.Length > 0 && linhas[0] == Application.dataPath;
                bool recente = (DateTime.Now - File.GetLastWriteTime(Trava)).TotalMinutes < MinutosDeTravaValida;
                if (!minha && recente && ProcessoVivo(linhas.Length > 1 ? linhas[1] : null))
                    Assert.Fail("Outra worktree (" + linhas[0] + ") esta rodando testes que mexem no save. " +
                                "Rode uma suite por vez (trava: " + Trava + ").");
            }

            File.WriteAllLines(Trava, new[] { Application.dataPath,
                System.Diagnostics.Process.GetCurrentProcess().Id.ToString() });
        }

        static bool ProcessoVivo(string pid)
        {
            if (!int.TryParse(pid, out int id)) return false;
            try { return !System.Diagnostics.Process.GetProcessById(id).HasExited; }
            catch (ArgumentException) { return false; }
        }

        /// <summary>Devolve o save ao estado original. Mantem o backup, para poder ser chamado de novo.</summary>
        public static void Restaurar()
        {
            if (File.Exists(ArquivoDeBackup)) File.Copy(ArquivoDeBackup, Save, true);
            else if (File.Exists(MarcaDeAusente) && File.Exists(Save)) File.Delete(Save);
        }

        /// <summary>Apaga o backup. So no fim da suite, depois de Restaurar().</summary>
        public static void Descartar()
        {
            if (File.Exists(ArquivoDeBackup)) File.Delete(ArquivoDeBackup);
            if (File.Exists(MarcaDeAusente)) File.Delete(MarcaDeAusente);
            if (File.Exists(Trava))
            {
                string[] linhas = File.ReadAllLines(Trava);
                if (linhas.Length > 0 && linhas[0] == Application.dataPath) File.Delete(Trava);
            }
        }

        static string Sha(string caminho)
        {
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(caminho))).Replace("-", "").ToLowerInvariant();
        }

        public static string HashAtual() => File.Exists(Save) ? Sha(Save) : "(arquivo ausente)";

        public static string HashDoBackup()
        {
            if (File.Exists(MarcaDeAusente)) return "(arquivo ausente)";
            return File.Exists(ArquivoDeBackup) ? Sha(ArquivoDeBackup) : null;
        }
    }

    /// <summary>
    /// Guarda qual cena estava aberta antes dos testes (em SessionState, que sobrevive ao reload de
    /// dominio) e a reabre no fim. Nunca salva cena alguma.
    /// </summary>
    public static class CenaDoTeste
    {
        const string Chave = "Loopia.Tests.CenaOriginal";
        public const string CenaPrincipal = "Assets/Scenes/HexPrototipo.unity";

        public static void Registrar()
        {
            if (!string.IsNullOrEmpty(SessionState.GetString(Chave, ""))) return;
            string atual = EditorSceneManager.GetActiveScene().path;
            SessionState.SetString(Chave, string.IsNullOrEmpty(atual) ? "<vazia>" : atual);
        }

        public static void NovaCenaVazia()
        {
            Registrar();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        public static void AbrirCenaPrincipal()
        {
            Registrar();
            EditorSceneManager.OpenScene(CenaPrincipal, OpenSceneMode.Single);
        }

        public static void Restaurar()
        {
            string original = SessionState.GetString(Chave, "");
            if (string.IsNullOrEmpty(original)) return;
            SessionState.EraseString(Chave);

            if (original == "<vazia>") EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            else EditorSceneManager.OpenScene(original, OpenSceneMode.Single);
        }
    }

    /// <summary>Guarda de toda a suite: backup do save no inicio, restauracao e cena original no fim.</summary>
    [SetUpFixture]
    public class GuardaDaSuite
    {
        [OneTimeSetUp]
        public void Inicio()
        {
            SaveIsolado.Backup();
        }

        [OneTimeTearDown]
        public void Fim()
        {
            SaveIsolado.Restaurar();
            string antes = SaveIsolado.HashDoBackup();
            string depois = SaveIsolado.HashAtual();
            SaveIsolado.Descartar();
            CenaDoTeste.Restaurar();
            Time.timeScale = 1f;
            Debug.Log("[Testes] SHA-256 do save: backup=" + antes + " depois=" + depois);
            Assert.AreEqual(antes, depois, "o save do jogador nao foi restaurado exatamente");
        }
    }

    /// <summary>Monta um HexWorld + HexLoopGenerator com os parametros da cena HexPrototipo.</summary>
    public sealed class MundoDeTeste
    {
        public GameObject raiz;
        public HexWorld mundo;
        public HexLoopGenerator gerador;
        Mesh _malha;
        Material _material;

        public static MundoDeTeste Criar(int semente, bool usarSementeFixa = true)
        {
            var m = new MundoDeTeste { raiz = new GameObject("MundoDeTeste") };
            m.mundo = m.raiz.AddComponent<HexWorld>();
            m.mundo.tamanhoDoHexagono = 3f;
            m.mundo.folgaEntreHexagonos = 0.3f;
            m.mundo.espessura = 0.6f;

            m.gerador = m.raiz.AddComponent<HexLoopGenerator>();
            m.gerador.mundo = m.mundo;
            m.gerador.raioMinimo = 4;
            m.gerador.raioMaximo = 6;
            m.gerador.deformacoes = 400;
            m.gerador.mioloMinimo = 9;
            m.gerador.usarSementeFixa = usarSementeFixa;
            m.gerador.semente = semente;
            return m;
        }

        /// <summary>Gera o mapa e guarda malha/material compartilhados para destruir no fim.</summary>
        public bool Gerar()
        {
            bool ok = gerador.Gerar();
            foreach (HexTile ilha in mundo.Ilhas)
            {
                var mf = ilha.GetComponent<MeshFilter>();
                var mr = ilha.GetComponent<MeshRenderer>();
                if (mf != null) _malha = mf.sharedMesh;
                if (mr != null) _material = mr.sharedMaterial;
                break;
            }
            return ok;
        }

        public void Destruir()
        {
            if (raiz != null) UnityEngine.Object.DestroyImmediate(raiz);
            if (_malha != null) UnityEngine.Object.DestroyImmediate(_malha);
            if (_material != null) UnityEngine.Object.DestroyImmediate(_material);
            raiz = null;
        }
    }

    public static class Reflexao
    {
        const BindingFlags Todos = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        public static object Chamar(object alvo, string metodo, params object[] args)
        {
            MethodInfo mi = alvo.GetType().GetMethod(metodo, Todos);
            Assert.IsNotNull(mi, "metodo '" + metodo + "' nao encontrado em " + alvo.GetType().Name +
                                 " (renomeado? atualize o teste)");
            return mi.Invoke(alvo, args);
        }

        public static void DefinirCampo(object alvo, string campo, object valor)
        {
            FieldInfo fi = alvo.GetType().GetField(campo, Todos);
            Assert.IsNotNull(fi, "campo '" + campo + "' nao encontrado em " + alvo.GetType().Name);
            fi.SetValue(alvo, valor);
        }
    }
    /// <summary>
    /// Captura de evidencia (antes/depois) para os relatorios: UMA renderizacao por imagem, de uma camera temporaria com a
    /// mesma orientacao da camera do jogo, fechada no Lucca. Desligada por padrao; so os testes PlayModeMedicao ligam.
    /// Nunca chamar em lote: dezenas de Camera.Render num quadro ja derrubaram a Unity (D3D12 sem memoria).
    /// </summary>
    public static class CapturaDeEvidencia
    {
        public static bool Ativa;

        public static string Salvar(Transform foco, string nome, float meiaAltura = 1.1f, int lado = 720)
        {
            if (!Ativa || foco == null) return null;
            Camera principal = Camera.main;
            if (principal == null) return null;
            var objeto = new GameObject("Camera da captura (teste)");
            var camera = objeto.AddComponent<Camera>();
            camera.CopyFrom(principal);
            camera.orthographic = true;
            camera.orthographicSize = meiaAltura;
            camera.aspect = 1f;
            camera.transform.rotation = principal.transform.rotation;
            camera.transform.position = foco.position + Vector3.up * 0.45f - camera.transform.forward * 12f;
            RenderTexture alvo = RenderTexture.GetTemporary(lado, lado, 24);
            RenderTexture anterior = RenderTexture.active;
            var imagem = new Texture2D(lado, lado, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = alvo;
                camera.Render();
                RenderTexture.active = alvo;
                imagem.ReadPixels(new Rect(0, 0, lado, lado), 0, 0);
                imagem.Apply();
                string caminho = Path.Combine(Path.GetTempPath(), "loopia_" + nome + ".png");
                File.WriteAllBytes(caminho, imagem.EncodeToPNG());
                Debug.Log("[Captura] " + caminho);
                return caminho;
            }
            finally
            {
                RenderTexture.active = anterior;
                camera.targetTexture = null;
                RenderTexture.ReleaseTemporary(alvo);
                UnityEngine.Object.Destroy(imagem);
                UnityEngine.Object.Destroy(objeto);
            }
        }
    }
}

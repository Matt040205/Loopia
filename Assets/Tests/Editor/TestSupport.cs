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
    /// </summary>
    public static class SaveIsolado
    {
        static string Save => Path.Combine(Application.persistentDataPath, "loopia_save.json");
        static string ArquivoDeBackup => Path.Combine(Path.GetTempPath(), "loopia_tests_save.bak");
        static string MarcaDeAusente => Path.Combine(Path.GetTempPath(), "loopia_tests_save.absent");

        public static void Backup()
        {
            if (File.Exists(ArquivoDeBackup) || File.Exists(MarcaDeAusente)) return;

            if (File.Exists(Save)) File.Copy(Save, ArquivoDeBackup, true);
            else File.WriteAllText(MarcaDeAusente, "o save nao existia antes dos testes");
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
}

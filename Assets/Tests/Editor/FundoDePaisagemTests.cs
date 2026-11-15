using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Loopia.Hex.Tests
{
    /// <summary>
    /// Pass B: o FundoDePaisagem gera o vale de forma deterministica, dentro do orcamento, sem collider/sombra, sem nada
    /// salvo na cena, e cobre a projecao da camera do jogo nos zooms e aspectos da matriz.
    /// Monta o componente numa cena vazia (sem Play, sem salvar).
    /// </summary>
    [Category("Logic")]
    public class FundoDePaisagemTests
    {
        /// <summary>Maquete antiga ("Cenario background" + "Rio de fundo"), medida no Editor em 2026-11-15.</summary>
        const long TriangulosDoFundoAntigo = 490588;

        GameObject _go;
        FundoDePaisagem _fundo;

        [SetUp]
        public void Montar()
        {
            CenaDoTeste.NovaCenaVazia();
            _go = new GameObject("Fundo de paisagem (teste)");
            _go.SetActive(false); // evita gerar no OnEnable antes de configurar
            _fundo = _go.AddComponent<FundoDePaisagem>();
            MateriaisDaPaisagem.Configurar(_fundo);
            _go.SetActive(true);
        }

        [TearDown]
        public void Desmontar()
        {
            if (_go != null) Object.DestroyImmediate(_go);
        }

        [OneTimeTearDown]
        public void Restaurar() => CenaDoTeste.Restaurar();

        static long Vertices(GameObject raiz)
        {
            long n = 0;
            foreach (var mf in raiz.GetComponentsInChildren<MeshFilter>()) n += mf.sharedMesh.vertexCount;
            return n;
        }

        [Test]
        public void GeraNoOnEnable_DentroDoOrcamento()
        {
            Assert.IsNotNull(_fundo.Gerado, "o OnEnable deveria gerar (gerarNoEditor = true)");
            Assert.Greater(_fundo.triangulosGerados, 200000, "fundo vazio demais: algo deixou de ser gerado");
            Assert.Less(_fundo.triangulosGerados, TriangulosDoFundoAntigo, "o fundo novo tem que custar menos triangulos que a maquete antiga");
            Assert.LessOrEqual(_fundo.Gerado.GetComponentsInChildren<MeshRenderer>().Length, 120, "malhas demais: a combinacao por material/bloco falhou");
        }

        [Test]
        public void MesmaSemente_MesmoResultado_SementeDiferente_Muda()
        {
            long tris = _fundo.triangulosGerados, verts = Vertices(_fundo.Gerado);
            _fundo.Gerar();
            Assert.AreEqual(tris, _fundo.triangulosGerados);
            Assert.AreEqual(verts, Vertices(_fundo.Gerado));
            _fundo.semente += 1;
            _fundo.Gerar();
            Assert.AreNotEqual(verts, Vertices(_fundo.Gerado), "semente diferente deveria mudar a paisagem");
        }

        [Test]
        public void Renderers_SemColliderSemSombra_NaoSalvos()
        {
            Assert.AreEqual(HideFlags.DontSave, _fundo.Gerado.hideFlags & HideFlags.DontSave, "o gerado nao pode ir para o arquivo da cena");
            Assert.IsEmpty(_go.GetComponentsInChildren<Collider>(true), "fundo nao pode ter collider (raios do mouse, NavMesh)");
            foreach (var r in _fundo.Gerado.GetComponentsInChildren<MeshRenderer>())
            {
                Assert.AreEqual(UnityEngine.Rendering.ShadowCastingMode.Off, r.shadowCastingMode, r.name);
                Assert.IsFalse(r.receiveShadows, r.name);
                Assert.AreEqual(HideFlags.DontSave, r.gameObject.hideFlags & HideFlags.DontSave, r.name);
                Assert.AreEqual(HideFlags.DontSave, r.GetComponent<MeshFilter>().sharedMesh.hideFlags & HideFlags.DontSave, r.name);
            }
        }

        [Test]
        public void Desativar_LimpaTudo()
        {
            var malha = _fundo.Gerado.GetComponentInChildren<MeshFilter>().sharedMesh;
            _go.SetActive(false);
            Assert.AreEqual(0, _go.transform.childCount);
            Assert.IsTrue(malha == null, "as malhas geradas tem que ser destruidas (vazamento no Editor)");
        }

        [Test]
        public void SemConfiguracao_AvisaENaoGera()
        {
            var go = new GameObject("vazio");
            go.SetActive(false);
            var f = go.AddComponent<FundoDePaisagem>();
            LogAssert.Expect(LogType.Warning, "[FundoDePaisagem] Modelos ou materiais nao atribuidos; fundo nao gerado.");
            go.SetActive(true);
            Assert.IsNull(f.Gerado);
            Object.DestroyImmediate(go);
        }

        /// <summary>A camera do jogo (30/45, distancia 70, alvo no centro do anel) nunca ve borda do terreno, em 16:9 e
        /// 21:9, do zoom mais proximo ao mais aberto medido nas seeds (orthographicSize 16-18,5) com folga.</summary>
        [Test]
        public void Terreno_CobreAProjecaoDaCamera()
        {
            var terreno = _fundo.Gerado.transform.Find("Terreno").GetComponent<MeshFilter>().sharedMesh;
            var dir = new Vector3(0.70710678f, 0f, -0.70710678f);
            var fundo = new Vector3(0.70710678f, 0f, 0.70710678f);
            var centro = new Vector3(0f, _fundo.alturaDoChao, 0f) + fundo * (-_fundo.alturaDoChao * 1.7320508f);
            float minU = float.MaxValue, maxU = float.MinValue, minV = float.MaxValue, maxV = float.MinValue;
            foreach (var p in terreno.vertices)
            {
                var d = p - centro;
                minU = Mathf.Min(minU, Vector3.Dot(d, dir)); maxU = Mathf.Max(maxU, Vector3.Dot(d, dir));
                minV = Mathf.Min(minV, Vector3.Dot(d, fundo)); maxV = Mathf.Max(maxV, Vector3.Dot(d, fundo));
            }
            var camGo = new GameObject("cam");
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.transform.rotation = Quaternion.Euler(30f, 45f, 0f);
            foreach (var alvo in new[] { Vector3.zero, new Vector3(3f, 0f, -3f), new Vector3(-3f, 0f, 3f) })
            {
                cam.transform.position = alvo - cam.transform.forward * 70f;
                foreach (float tamanho in new[] { 14f, 16.8f, 18.5f, 21f })
                foreach (float aspecto in new[] { 16f / 9f, 21f / 9f })
                {
                    cam.orthographicSize = tamanho; cam.aspect = aspecto;
                    var plano = new Plane(Vector3.up, new Vector3(0f, _fundo.alturaDoChao, 0f));
                    for (int i = 0; i < 4; i++)
                    {
                        var ray = cam.ViewportPointToRay(new Vector3(i % 2, i / 2, 0f));
                        Assert.IsTrue(plano.Raycast(ray, out float t));
                        var d = ray.GetPoint(t) - centro;
                        float u = Vector3.Dot(d, dir), v = Vector3.Dot(d, fundo);
                        string caso = "alvo " + alvo + " tamanho " + tamanho + " aspecto " + aspecto + " canto " + i;
                        Assert.That(u, Is.InRange(minU + 1f, maxU - 1f), caso);
                        Assert.That(v, Is.InRange(minV + 1f, maxV - 1f), caso);
                    }
                }
            }
            Object.DestroyImmediate(camGo);
        }
    }
}

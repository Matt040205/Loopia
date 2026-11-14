using NUnit.Framework;
using Unity.AI.Navigation;
using UnityEngine;

namespace Loopia.Hex.Tests
{
    /// <summary>
    /// VisualDeIlha: bounds de MeshFilter (comportamento antigo, inalterado) e de SkinnedMeshRenderer (novo),
    /// e a normalizacao por eixo (MaiorEixo / Altura / Comprimento). A caixa de teste tem 1 x 2 x 3 (x, y, z).
    /// </summary>
    [Category("Logic")]
    public class VisualDeIlhaTests
    {
        const float Tol = 1e-3f;
        Mesh _malha;
        GameObject _pai;

        [OneTimeSetUp]
        public void CenaVazia() => CenaDoTeste.NovaCenaVazia();

        [OneTimeTearDown]
        public void Restaurar() => CenaDoTeste.Restaurar();

        [SetUp]
        public void Montar()
        {
            _pai = new GameObject("PaiDeTeste");
            _malha = CaixaDe(new Vector3(1f, 2f, 3f), new Vector3(0.5f, 0.7f, -0.2f));
        }

        [TearDown]
        public void Limpar()
        {
            if (_pai != null) Object.DestroyImmediate(_pai);
            if (_malha != null) Object.DestroyImmediate(_malha);
        }

        /// <summary>Caixa de tamanho dado, com o centro deslocado (pivo fora do centro, como nos modelos reais).</summary>
        static Mesh CaixaDe(Vector3 tamanho, Vector3 centro)
        {
            var m = new Mesh { name = "CaixaDeTeste" };
            var v = new Vector3[8];
            for (int i = 0; i < 8; i++)
                v[i] = centro + Vector3.Scale(tamanho * 0.5f,
                    new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
            m.vertices = v;
            m.triangles = new[] { 0, 1, 2, 1, 3, 2 };
            m.RecalculateBounds();
            return m;
        }

        GameObject PrefabComFiltro()
        {
            var go = new GameObject("PrefabFiltro");
            go.transform.SetParent(_pai.transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = _malha;
            go.AddComponent<MeshRenderer>();
            return go;
        }

        GameObject PrefabSoSkinned(Vector3 deslocamentoDoFilho)
        {
            var go = new GameObject("PrefabSkinned");
            go.transform.SetParent(_pai.transform, false);
            var filho = new GameObject("Malha");
            filho.transform.SetParent(go.transform, false);
            filho.transform.localPosition = deslocamentoDoFilho;
            filho.AddComponent<SkinnedMeshRenderer>().sharedMesh = _malha;
            Assert.IsNull(go.GetComponentInChildren<MeshFilter>(), "o prefab de teste nao pode ter MeshFilter");
            return go;
        }

        [Test]
        public void LimitesLocais_SoSkinnedMeshRenderer_NaoEhVazio()
        {
            var prefab = PrefabSoSkinned(Vector3.zero);
            Bounds b = VisualDeIlha.LimitesLocais(prefab.transform);
            Assert.AreEqual(1f, b.size.x, Tol);
            Assert.AreEqual(2f, b.size.y, Tol);
            Assert.AreEqual(3f, b.size.z, Tol);
            Assert.AreEqual(0.5f, b.center.x, Tol);
            Assert.AreEqual(0.7f, b.center.y, Tol);
            Assert.AreEqual(-0.2f, b.center.z, Tol);
        }

        [Test]
        public void LimitesLocais_SkinnedNoFilho_AplicaATransformacaoDoFilho()
        {
            var prefab = PrefabSoSkinned(new Vector3(10f, 0f, 0f));
            Bounds b = VisualDeIlha.LimitesLocais(prefab.transform);
            Assert.AreEqual(10.5f, b.center.x, Tol);
            Assert.AreEqual(3f, b.size.z, Tol);
        }

        [Test]
        public void LimitesLocais_SemNenhumMesh_EhVazio()
        {
            var go = new GameObject("Vazio");
            go.transform.SetParent(_pai.transform, false);
            Assert.AreEqual(Vector3.zero, VisualDeIlha.LimitesLocais(go.transform).size);
        }

        [Test]
        public void LimitesLocais_MeshFilterContinuaIgual()
        {
            var prefab = PrefabComFiltro();
            Bounds b = VisualDeIlha.LimitesLocais(prefab.transform);
            Assert.AreEqual(new Vector3(1f, 2f, 3f).x, b.size.x, Tol);
            Assert.AreEqual(2f, b.size.y, Tol);
            Assert.AreEqual(3f, b.size.z, Tol);
        }

        [Test]
        public void LimitesLocais_FiltroESkinnedJuntos_SomamAsDuasCaixas()
        {
            var prefab = PrefabComFiltro();
            var filho = new GameObject("Skinned");
            filho.transform.SetParent(prefab.transform, false);
            filho.transform.localPosition = new Vector3(0f, 5f, 0f);
            filho.AddComponent<SkinnedMeshRenderer>().sharedMesh = _malha;
            Bounds b = VisualDeIlha.LimitesLocais(prefab.transform);
            Assert.AreEqual(7f, b.size.y, Tol); // de -0.3 (caixa de baixo) ate 6.7 (caixa deslocada em 5)
        }

        static Bounds Medido(GameObject criado) => VisualDeIlha.LimitesLocais(criado.transform);

        [Test]
        public void Criar_Skinned_MaiorEixo_NormalizaOMaiorLado()
        {
            var go = VisualDeIlha.Criar(PrefabSoSkinned(Vector3.zero), _pai.transform, 1.5f);
            Bounds b = Medido(go);
            Assert.AreEqual(1.5f, b.size.z, Tol);     // z (3) era o maior eixo
            Assert.AreEqual(1.0f, b.size.y, Tol);
            Assert.AreEqual(0.5f, b.size.x, Tol);
        }

        [Test]
        public void Criar_Skinned_Altura_NormalizaPeloEixoY()
        {
            var go = VisualDeIlha.Criar(PrefabSoSkinned(Vector3.zero), _pai.transform, 1.5f, false, EixoDeMedida.Altura);
            Bounds b = Medido(go);
            Assert.AreEqual(1.5f, b.size.y, Tol);
            Assert.AreEqual(2.25f, b.size.z, Tol);
            Assert.AreEqual(0.75f, b.size.x, Tol);
        }

        [Test]
        public void Criar_Skinned_Comprimento_NormalizaOMaiorDeXZ()
        {
            var go = VisualDeIlha.Criar(PrefabSoSkinned(Vector3.zero), _pai.transform, 1.5f, false, EixoDeMedida.Comprimento);
            Bounds b = Medido(go);
            Assert.AreEqual(1.5f, Mathf.Max(b.size.x, b.size.z), Tol);
            Assert.AreEqual(1.0f, b.size.y, Tol);
        }

        [Test]
        public void Criar_Skinned_BaseEmYZero_ECentradoEmXZ()
        {
            var go = VisualDeIlha.Criar(PrefabSoSkinned(Vector3.zero), _pai.transform, 1.5f);
            Bounds b = Medido(go);
            Assert.AreEqual(0f, b.min.y, Tol);
            Assert.AreEqual(0f, b.center.x, Tol);
            Assert.AreEqual(0f, b.center.z, Tol);
        }

        [Test]
        public void Criar_Topo_UsaOMaiorDeXZ_ETopoEmYZero()
        {
            // topo=true (modelo da ilha): so x e z entram na medida e o topo (max.y) fica em y = 0.
            var go = VisualDeIlha.Criar(PrefabComFiltro(), _pai.transform, 1.5f, true);
            Bounds b = Medido(go);
            Assert.AreEqual(1.5f, Mathf.Max(b.size.x, b.size.z), Tol);   // z = 3 vira 1.5: fator 0.5
            Assert.AreEqual(1.0f, b.size.y, Tol);                          // y = 2 * 0.5
            Assert.AreEqual(0f, b.max.y, Tol);
        }

        [Test]
        public void Criar_MeshFilter_AssinaturaAntiga_ContinuaMedindoOMaiorEixo()
        {
            var go = VisualDeIlha.Criar(PrefabComFiltro(), _pai.transform, 1.5f);
            Bounds b = Medido(go);
            Assert.AreEqual(1.5f, b.size.z, Tol);
            Assert.AreEqual(1.0f, b.size.y, Tol);
            Assert.AreEqual(0.5f, b.size.x, Tol);
            Assert.AreEqual(0f, b.min.y, Tol);
        }

        [Test]
        public void Criar_MeshFilter_NovaSobrecargaMaiorEixo_IgualAAntiga()
        {
            var a = VisualDeIlha.Criar(PrefabComFiltro(), _pai.transform, 1.5f, false);
            var b = VisualDeIlha.Criar(PrefabComFiltro(), _pai.transform, 1.5f, false, EixoDeMedida.MaiorEixo);
            Assert.AreEqual(Medido(a).size.x, Medido(b).size.x, 1e-5f);
            Assert.AreEqual(Medido(a).size.y, Medido(b).size.y, 1e-5f);
            Assert.AreEqual(Medido(a).size.z, Medido(b).size.z, 1e-5f);
            Assert.AreEqual(Medido(a).min.y, Medido(b).min.y, 1e-5f);
        }

        [Test]
        public void Criar_MantemArteForaDoBake_ComColisoresDesligados()
        {
            var prefab = PrefabComFiltro();
            prefab.AddComponent<BoxCollider>();
            var go = VisualDeIlha.Criar(prefab, _pai.transform, 1f);
            Assert.IsTrue(go.GetComponent<NavMeshModifier>().ignoreFromBuild);
            foreach (var c in go.GetComponentsInChildren<Collider>()) Assert.IsFalse(c.enabled);
        }

        [Test]
        public void Criar_PrefabNulo_DevolveNulo()
        {
            Assert.IsNull(VisualDeIlha.Criar(null, _pai.transform, 1f));
            Assert.IsNull(VisualDeIlha.Criar(null, _pai.transform, 1f, false, EixoDeMedida.Altura));
        }

        [Test]
        public void Medida_PorEixo()
        {
            var t = new Vector3(1f, 5f, 3f);
            Assert.AreEqual(5f, VisualDeIlha.Medida(t, false, EixoDeMedida.MaiorEixo), 1e-6f);
            Assert.AreEqual(3f, VisualDeIlha.Medida(t, true, EixoDeMedida.MaiorEixo), 1e-6f);
            Assert.AreEqual(5f, VisualDeIlha.Medida(t, false, EixoDeMedida.Altura), 1e-6f);
            Assert.AreEqual(3f, VisualDeIlha.Medida(t, false, EixoDeMedida.Comprimento), 1e-6f);
        }
    }
}

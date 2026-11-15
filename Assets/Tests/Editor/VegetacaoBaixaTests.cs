using System.Collections.Generic;
using NUnit.Framework;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;

namespace Loopia.Hex.Tests
{
    /// <summary>
    /// Pass C (C2/C3): a camada "VegetacaoBaixa" do DecoracaoDeIlha com o perfil real. Fica num filho proprio (a decoracao
    /// principal e os valores congelados do Pass A nao mudam), respeita a faixa da categoria, as regras de corredor/centro,
    /// nao sobrepoe a primeira camada, varia por ilha e nunca passa de 12 objetos por ilha. Sem Play, cena vazia.
    /// </summary>
    [Category("Logic")]
    public class VegetacaoBaixaTests
    {
        const string CaminhoDoPerfil = "Assets/Dados/Arte/EscalaDeArte.asset";
        const string PastaDosModelos = "Assets/Modelos/HexPrototype/";
        static readonly string[] ModelosDaCena =
            { "Arvore", "Pedra", "CogumeloMarrom", "CogumeloVermelho", "CepoDeMadeira", "TroncoDeMadeira" };
        static readonly int[] Sementes = { 1, 4, 7, 12345 };

        MundoDeTeste _m;

        [OneTimeSetUp] public void CenaVazia() => CenaDoTeste.NovaCenaVazia();
        [OneTimeTearDown] public void Restaurar() => CenaDoTeste.Restaurar();

        [TearDown]
        public void Limpar()
        {
            if (_m != null) _m.Destruir();
            _m = null;
        }

        static ArtScaleProfile Perfil() => AssetDatabase.LoadAssetAtPath<ArtScaleProfile>(CaminhoDoPerfil);

        MundoDeTeste Gerar(int semente)
        {
            _m = MundoDeTeste.Criar(semente);
            var d = _m.raiz.AddComponent<DecoracaoDeIlha>();
            d.quantidade = 4; d.larguraDoCorredor = 0.4f; d.semente = 173; d.perfil = Perfil();
            d.modelos = new GameObject[ModelosDaCena.Length];
            for (int i = 0; i < ModelosDaCena.Length; i++) d.modelos[i] = AssetDatabase.LoadAssetAtPath<GameObject>(PastaDosModelos + ModelosDaCena[i] + ".prefab");
            _m.mundo.decoracao = d;
            Assert.IsTrue(_m.Gerar());
            return _m;
        }

        static List<Transform> Filhos(HexTile ilha, string raiz)
        {
            var l = new List<Transform>();
            var r = ilha.transform.Find(raiz);
            if (r != null) foreach (Transform t in r) if (t.gameObject.activeSelf) l.Add(t);
            return l;
        }

        [Test]
        public void PerfilReal_TemCamadaConfigurada()
        {
            var p = Perfil();
            Assert.IsNotEmpty(p.vegetacaoBaixa);
            Assert.That(p.vegetacaoMaxima, Is.InRange(1, 8), "4 da decoracao principal + vegetacao <= 12 por ilha");
            Assert.LessOrEqual(p.vegetacaoMinima, p.vegetacaoMaxima);
            foreach (var m in p.vegetacaoBaixa)
            {
                Assert.IsNotNull(m.prefab);
                Assert.IsTrue(m.categoria == CategoriaDeArte.Arbusto || m.categoria == CategoriaDeArte.Capim, m.prefab.name + " fora das categorias de vegetacao baixa");
                Assert.IsEmpty(m.prefab.GetComponentsInChildren<Collider>(true), m.prefab.name + " tem collider");
            }
        }

        [TestCaseSource(nameof(Sementes))]
        public void Camada_RespeitaFaixasRegrasELimite(int semente)
        {
            var m = Gerar(semente);
            var perfil = Perfil();
            float raio = m.mundo.tamanhoDoHexagono * (1f - m.mundo.folgaEntreHexagonos);
            int total = 0;
            var contagens = new HashSet<int>();
            foreach (HexTile ilha in m.mundo.Ilhas)
            {
                var principais = Filhos(ilha, "Decoracoes");
                var veg = Filhos(ilha, "VegetacaoBaixa");
                Assert.LessOrEqual(principais.Count + veg.Count, 12, ilha.Coord + ": mais de 12 objetos de dressing");
                Assert.LessOrEqual(veg.Count, perfil.vegetacaoMaxima);
                contagens.Add(veg.Count);
                total += veg.Count;
                foreach (Transform t in veg)
                {
                    Vector3 p = t.localPosition;
                    Bounds b = VisualDeIlha.LimitesLocais(t);
                    Assert.LessOrEqual(b.size.y, 0.6f * perfil.alturaDeReferencia + 1e-3f, t.name + " alto demais para vegetacao baixa");
                    // mesma pegada do DecoracaoDeIlha: extensao no plano x fracao da regra da categoria
                    ModeloDeArte modelo = perfil.vegetacaoBaixa.Find(x => x.prefab != null && x.prefab.name == t.name);
                    Assert.IsNotNull(modelo, t.name + " nao e da camada de vegetacao");
                    Assert.IsTrue(perfil.TryRegra(modelo.categoria, out RegraDeCategoria regra));
                    float pegada = new Vector2(b.extents.x, b.extents.z).magnitude * regra.fracaoDaPegada;
                    Assert.IsTrue(DecoracaoDeIlha.PosicaoLivre(p, pegada, raio, 0.4f), ilha.Coord + "/" + t.name + " no corredor/centro/borda");
                    foreach (Transform o in principais)
                    {
                        Bounds bo = VisualDeIlha.LimitesLocais(o);
                        float po = new Vector2(bo.extents.x, bo.extents.z).magnitude;
                        Assert.Greater(Vector3.Distance(o.localPosition, p), 0.05f + Mathf.Min(pegada, po) * 0.5f, ilha.Coord + ": " + t.name + " sobre " + o.name);
                    }
                    Assert.IsTrue(t.GetComponent<NavMeshModifier>() != null && t.GetComponent<NavMeshModifier>().ignoreFromBuild, t.name + " entraria no bake");
                    foreach (var c in t.GetComponentsInChildren<Collider>(true)) Assert.IsFalse(c.enabled);
                }
            }
            Assert.Greater(total, m.mundo.TotalDeIlhas, "camada quase vazia (semente " + semente + ")");
            Assert.Greater(contagens.Count, 1, "a quantidade deveria variar por ilha (C3)");
        }

        [Test]
        public void MesmaSemente_MesmaVegetacao()
        {
            string Assinatura()
            {
                var m = Gerar(7);
                var sb = new System.Text.StringBuilder();
                foreach (HexTile ilha in m.mundo.Ilhas)
                    foreach (Transform t in Filhos(ilha, "VegetacaoBaixa"))
                        sb.Append(ilha.Coord).Append(t.name).Append(t.localPosition.ToString("F3")).Append(t.localScale.ToString("F3"));
                Limpar();
                return sb.ToString();
            }
            Assert.AreEqual(Assinatura(), Assinatura());
        }

        [Test]
        public void SortearDe_MultiplicadorZeroExcluiItem_EOriginalSemMultiplicador()
        {
            var a = new ModeloDeArte { prefab = new GameObject("a"), peso = 1f };
            var b = new ModeloDeArte { prefab = new GameObject("b"), peso = 1f };
            var lista = new List<ModeloDeArte> { a, b };
            var r = new System.Random(3);
            for (int i = 0; i < 200; i++) Assert.AreSame(b, ArtScaleProfile.SortearDe(lista, r, new[] { 0f, 1f }));
            var r1 = new System.Random(9); var r2 = new System.Random(9);
            var perfil = ScriptableObject.CreateInstance<ArtScaleProfile>();
            perfil.modelos = lista;
            for (int i = 0; i < 50; i++) Assert.AreSame(perfil.Sortear(r1), ArtScaleProfile.SortearDe(lista, r2, null));
            Object.DestroyImmediate(a.prefab); Object.DestroyImmediate(b.prefab); Object.DestroyImmediate(perfil);
        }
    }
}

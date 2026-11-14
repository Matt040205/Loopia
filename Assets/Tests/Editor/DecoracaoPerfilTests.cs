using System.Collections.Generic;
using NUnit.Framework;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;

namespace Loopia.Hex.Tests
{
    /// <summary>
    /// DecoracaoDeIlha em Edit Mode, com os valores da cena (tamanho 3, folga 0.3 => raio 2.1; corredor 0.4):
    /// (1) sem perfil o resultado e IDENTICO ao algoritmo anterior ao Pass A (valores congelados, capturados do
    /// codigo antigo); (2) com o perfil real EscalaDeArte.asset, nas 11 sementes dos testes do gerador.
    /// </summary>
    [Category("Logic")]
    public class DecoracaoPerfilTests
    {
        const string CaminhoDoPerfil = "Assets/Dados/Arte/EscalaDeArte.asset";
        const string PastaDosModelos = "Assets/Modelos/HexPrototype/";
        static readonly string[] ModelosDaCena =
            { "Arvore", "Pedra", "CogumeloMarrom", "CogumeloVermelho", "CepoDeMadeira", "TroncoDeMadeira" };
        static readonly int[] Sementes = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 12345 };

        MundoDeTeste _m;
        readonly List<Object> _extras = new List<Object>();

        [OneTimeSetUp]
        public void CenaVazia() => CenaDoTeste.NovaCenaVazia();

        [OneTimeTearDown]
        public void Restaurar() => CenaDoTeste.Restaurar();

        [TearDown]
        public void Limpar()
        {
            foreach (var o in _extras) if (o != null) Object.DestroyImmediate(o);
            _extras.Clear();
            if (_m != null) _m.Destruir();
            _m = null;
        }

        static ArtScaleProfile PerfilReal()
        {
            var p = AssetDatabase.LoadAssetAtPath<ArtScaleProfile>(CaminhoDoPerfil);
            Assert.IsNotNull(p, "perfil ausente: " + CaminhoDoPerfil);
            return p;
        }

        static GameObject Prefab(string nome)
        {
            var g = AssetDatabase.LoadAssetAtPath<GameObject>(PastaDosModelos + nome + ".prefab");
            Assert.IsNotNull(g, "prefab ausente: " + nome);
            return g;
        }

        DecoracaoDeIlha AnexarDecoracao(MundoDeTeste m, ArtScaleProfile perfil)
        {
            var d = m.raiz.AddComponent<DecoracaoDeIlha>();
            d.quantidade = 4;
            d.larguraDoCorredor = 0.4f;
            d.semente = 173;
            d.perfil = perfil;
            d.modelos = new GameObject[ModelosDaCena.Length];
            for (int i = 0; i < ModelosDaCena.Length; i++) d.modelos[i] = Prefab(ModelosDaCena[i]);
            m.mundo.decoracao = d;
            return d;
        }

        static List<Transform> Ativas(HexTile ilha)
        {
            var lista = new List<Transform>();
            Transform raiz = ilha.transform.Find("Decoracoes");
            if (raiz == null) return lista;
            foreach (Transform t in raiz) if (t.gameObject.activeSelf) lista.Add(t);
            return lista;
        }

        // --- (1) fallback: sem perfil == algoritmo antigo ---

        struct Esperado
        {
            public HexCoord coord; public string nome; public float x, z, rotY; public Vector3 tamanho;
        }

        static Esperado G(int q, int r, string nome, float x, float z, float rotY, float sx, float sy, float sz)
            => new Esperado { coord = new HexCoord(q, r), nome = nome, x = x, z = z, rotY = rotY, tamanho = new Vector3(sx, sy, sz) };

        // Capturado do DecoracaoDeIlha ANTES do Pass A (semente 173, 4 tentativas, corredor 0.4, raio 2.1, os 6 modelos da cena
        // na ordem da cena). Posicoes/rotacoes/tamanhos em 4 casas; so as decoracoes que foram colocadas.
        static readonly Esperado[] Congelado =
        {
            G(0, 0, "Arvore", 1.4761f, -0.7769f, 249.00f, 0.2437f, 0.4941f, 0.2472f),
            G(0, 0, "CogumeloMarrom", 0.0837f, 1.6213f, 136.00f, 0.3484f, 0.2886f, 0.3664f),
            G(0, 0, "CogumeloVermelho", 1.3470f, 0.8829f, 27.00f, 0.4330f, 0.2972f, 0.4553f),
            G(1, 0, "CogumeloVermelho", 1.4423f, 0.8115f, 68.00f, 0.2895f, 0.1987f, 0.3044f),
            G(1, 0, "Pedra", 1.4393f, -0.7251f, 178.00f, 0.3716f, 0.2699f, 0.4288f),
            G(1, 0, "CogumeloMarrom", -0.0110f, 1.5949f, 347.00f, 0.2715f, 0.2249f, 0.2856f),
            G(1, 0, "Arvore", -1.3092f, -0.8214f, 199.00f, 0.1420f, 0.2879f, 0.1440f),
            G(2, -3, "CogumeloVermelho", 1.3787f, 0.8018f, 50.00f, 0.5256f, 0.3608f, 0.5526f),
            G(2, -3, "CogumeloMarrom", -1.3738f, 0.9024f, 70.00f, 0.4005f, 0.3317f, 0.4212f),
            G(2, -3, "CogumeloMarrom", 0.0912f, 1.6384f, 355.00f, 0.3717f, 0.3079f, 0.3909f),
            G(2, -3, "CogumeloVermelho", -0.1391f, -1.6151f, 227.00f, 0.3133f, 0.2150f, 0.3294f),
            G(-3, 2, "CogumeloVermelho", -0.0749f, 1.5877f, 38.00f, 0.3449f, 0.2368f, 0.3627f),
            G(-3, 2, "CepoDeMadeira", -0.0821f, -1.6844f, 89.00f, 0.2768f, 0.0807f, 0.2911f),
            G(-3, 2, "CepoDeMadeira", 1.4624f, -0.6843f, 202.00f, 0.3376f, 0.0984f, 0.3550f),
            G(4, 1, "Pedra", 1.3825f, -0.8118f, 138.00f, 0.5058f, 0.3673f, 0.5837f),
            G(4, 1, "CogumeloMarrom", -0.0332f, 1.5817f, 336.00f, 0.4666f, 0.3865f, 0.4908f),
            G(4, 1, "Arvore", -1.2799f, -0.8995f, 17.00f, 0.1811f, 0.3672f, 0.1837f),
            G(4, 1, "CepoDeMadeira", 1.3436f, 0.8388f, 78.00f, 0.4450f, 0.1297f, 0.4679f),
            G(1, -4, "CepoDeMadeira", -1.2772f, -0.8325f, 266.00f, 0.3120f, 0.0909f, 0.3281f),
            G(1, -4, "Pedra", 0.0058f, -1.6071f, 337.00f, 0.5064f, 0.3678f, 0.5845f),
            G(1, -4, "CepoDeMadeira", 1.3749f, 0.7908f, 200.00f, 0.4289f, 0.1250f, 0.4510f),
            G(1, -4, "CepoDeMadeira", -1.4957f, 0.8302f, 226.00f, 0.3034f, 0.0884f, 0.3190f)
        };

        [Test]
        public void SemPerfil_AsDecoracoesSaoIdenticasAoAlgoritmoAntigo()
        {
            _m = MundoDeTeste.Criar(1);
            _m.mundo.GarantirPreparado();
            AnexarDecoracao(_m, null);

            var coords = new List<HexCoord>();
            foreach (var e in Congelado) if (!coords.Contains(e.coord)) coords.Add(e.coord);
            // ilhas em que o algoritmo antigo nao colocou nada nao aparecem na tabela; todas as 6 aparecem.
            Assert.AreEqual(6, coords.Count);

            Mesh malha = null;
            Material material = null;
            foreach (HexCoord c in coords)
            {
                HexTile ilha = _m.mundo.CriarIlha(c, HexTileKind.Caminho);
                malha = ilha.GetComponent<MeshFilter>().sharedMesh;
                material = ilha.GetComponent<MeshRenderer>().sharedMaterial;
                var ativas = Ativas(ilha);
                var esperadas = new List<Esperado>();
                foreach (var e in Congelado) if (e.coord == c) esperadas.Add(e);
                Assert.AreEqual(esperadas.Count, ativas.Count, "quantidade de decoracoes em " + c);
                for (int i = 0; i < ativas.Count; i++)
                {
                    Esperado e = esperadas[i];
                    Transform t = ativas[i];
                    string onde = c + " #" + i + " " + e.nome;
                    Assert.AreEqual(e.nome, t.name, onde + " modelo");
                    Assert.AreEqual(e.x, t.localPosition.x, 6e-4f, onde + " x");
                    Assert.AreEqual(0f, t.localPosition.y, 1e-6f, onde + " y");
                    Assert.AreEqual(e.z, t.localPosition.z, 6e-4f, onde + " z");
                    Assert.AreEqual(e.rotY, t.localEulerAngles.y, 0.02f, onde + " rotacao");
                    Vector3 s = VisualDeIlha.LimitesLocais(t).size;
                    Assert.AreEqual(e.tamanho.x, s.x, 6e-4f, onde + " tamanho x");
                    Assert.AreEqual(e.tamanho.y, s.y, 6e-4f, onde + " tamanho y");
                    Assert.AreEqual(e.tamanho.z, s.z, 6e-4f, onde + " tamanho z");
                }
            }
            if (malha != null) Object.DestroyImmediate(malha);
            if (material != null) Object.DestroyImmediate(material);
            _m.mundo.LimparMundo();
        }

        // --- (2) perfil real ---

        /// <summary>Acha a entrada do perfil (e a regra) de uma decoracao: pelo nome do prefab e pela faixa de tamanho.</summary>
        static bool Classificar(ArtScaleProfile perfil, Transform deco, out ModeloDeArte modelo, out RegraDeCategoria regra, out float medida)
        {
            Vector3 tamanho = VisualDeIlha.LimitesLocais(deco).size;
            modelo = null; regra = null; medida = 0f;
            foreach (var candidato in perfil.modelos)
            {
                if (candidato.prefab == null || candidato.prefab.name != deco.name) continue;
                Assert.IsTrue(perfil.TryRegra(candidato.categoria, out RegraDeCategoria r), "sem regra para " + candidato.categoria);
                float m = VisualDeIlha.Medida(tamanho, false, r.eixo);
                float baixo = r.tamanhoMinimo * perfil.alturaDeReferencia * 0.95f;
                float alto = r.tamanhoMaximo * perfil.alturaDeReferencia * 1.05f;
                if (m >= baixo && m <= alto) { modelo = candidato; regra = r; medida = m; return true; }
            }
            return false;
        }

        MundoDeTeste GerarComPerfil(int semente)
        {
            _m = MundoDeTeste.Criar(semente);
            AnexarDecoracao(_m, PerfilReal());
            Assert.IsTrue(_m.Gerar(), "Gerar() falhou para a semente " + semente);
            return _m;
        }

        [TestCaseSource(nameof(Sementes))]
        public void ComPerfil_CadaDecoracaoRespeitaAsRegrasDeColocacao(int semente)
        {
            var m = GerarComPerfil(semente);
            var perfil = PerfilReal();
            float raio = m.mundo.tamanhoDoHexagono * (1f - m.mundo.folgaEntreHexagonos);
            Vector3 frente = perfil.FrenteDaCameraXZ;
            int total = 0, grandes = 0;
            foreach (HexTile ilha in m.mundo.Ilhas)
            {
                var ativas = Ativas(ilha);
                var pos = new List<Vector3>();
                var pegadas = new List<float>();
                foreach (Transform t in ativas)
                {
                    Assert.IsTrue(Classificar(perfil, t, out ModeloDeArte modelo, out RegraDeCategoria regra, out float medida),
                        ilha.Coord + "/" + t.name + ": medida " + (VisualDeIlha.LimitesLocais(t).size) + " fora de toda faixa do perfil (+-5%)");
                    Bounds b = VisualDeIlha.LimitesLocais(t);
                    float pegada = new Vector2(b.extents.x, b.extents.z).magnitude * regra.fracaoDaPegada;
                    Vector3 p = t.localPosition;

                    Assert.IsTrue(DecoracaoDeIlha.PosicaoLivre(p, pegada, raio, 0.4f),
                        ilha.Coord + "/" + t.name + " invade o centro, um corredor ou a borda: " + p);
                    if (modelo.categoria == CategoriaDeArte.ArvoreGrande)
                    {
                        grandes++;
                        Assert.Greater(Vector3.Dot(p.normalized, frente), regra.limiarDoLadoLonge,
                            ilha.Coord + ": ArvoreGrande fora do lado longe da camera: " + p);
                    }

                    for (int j = 0; j < pos.Count; j++)
                        Assert.GreaterOrEqual(Vector3.Distance(pos[j], p), pegada + pegadas[j] + 0.12f - 1e-4f,
                            ilha.Coord + ": decoracoes sobrepostas");
                    pos.Add(p); pegadas.Add(pegada);

                    Assert.IsTrue(t.GetComponent<NavMeshModifier>() != null && t.GetComponent<NavMeshModifier>().ignoreFromBuild,
                        t.name + " sem NavMeshModifier.ignoreFromBuild");
                    foreach (var c in t.GetComponentsInChildren<Collider>(true))
                        Assert.IsFalse(c.enabled, t.name + " tem collider ligado");
                    total++;
                }
            }
            Assert.Greater(total, 0);
            Debug.Log("[Testes] semente " + semente + ": " + total + " decoracoes em " + m.mundo.TotalDeIlhas + " ilhas, " + grandes + " arvores grandes");
        }

        [TestCaseSource(nameof(Sementes))]
        public void ComPerfil_TodaIlhaTemPeloMenosUmaDecoracao(int semente)
        {
            var m = GerarComPerfil(semente);
            var vazias = new List<string>();
            int total = 0;
            foreach (HexTile ilha in m.mundo.Ilhas)
            {
                int n = Ativas(ilha).Count;
                total += n;
                if (n == 0) vazias.Add(ilha.Coord.ToString());
            }
            Debug.Log("[Testes] semente " + semente + ": " + total + " decoracoes, ilhas sem decoracao: " + vazias.Count + "/" + m.mundo.TotalDeIlhas);
            CollectionAssert.IsEmpty(vazias, "ilhas sem nenhuma decoracao (semente " + semente + "): " + string.Join(", ", vazias));
        }

        static readonly int[] SementesDaOclusao = { 12345, 7, 2024, 1, 2, 3, 4, 5, 6, 8, 9, 10 };

        /// <summary>
        /// Raio da camera (orto, Euler(inclinacao, giro, 0)) ate pontos do caminho a 0.5 u de altura (altura do corpo do Lucca),
        /// 6 por trecho entre ilhas consecutivas do anel, contra as caixas dos renderers (AABB = conservador) das arvores grandes.
        /// </summary>
        [TestCaseSource(nameof(SementesDaOclusao))]
        public void ComPerfil_ArvoresGrandesNaoOcultamNenhumPontoDoCaminho(int semente)
        {
            var m = GerarComPerfil(semente);
            var perfil = PerfilReal();
            Vector3 direcao = perfil.DirecaoDaCamera;
            var caixas = new List<Bounds>();
            foreach (HexTile ilha in m.mundo.Ilhas)
                foreach (Transform t in Ativas(ilha))
                {
                    if (!Classificar(perfil, t, out ModeloDeArte modelo, out _, out _) || modelo.categoria != CategoriaDeArte.ArvoreGrande) continue;
                    foreach (var r in t.GetComponentsInChildren<Renderer>()) caixas.Add(r.bounds);
                }
            Assert.Greater(caixas.Count, 0, "semente " + semente + ": nenhuma arvore grande (o teste nao provaria nada)");

            var loop = m.gerador.Loop;
            int pontos = 0, ocultos = 0;
            for (int i = 0; i < loop.Count; i++)
            {
                Vector3 a = m.mundo.PosicaoDe(loop[i]);
                Vector3 b = m.mundo.PosicaoDe(loop[(i + 1) % loop.Count]);
                for (int k = 0; k < 6; k++)
                {
                    Vector3 q = Vector3.Lerp(a, b, k / 6f) + Vector3.up * 0.5f;
                    var raio = new Ray(q - direcao * 100f, direcao);
                    pontos++;
                    foreach (Bounds c in caixas)
                        if (c.IntersectRay(raio, out float d) && d < 99.5f) { ocultos++; break; }
                }
            }
            Debug.Log("[Testes] oclusao, semente " + semente + ": " + caixas.Count + " renderers de arvore grande, " + ocultos + "/" + pontos + " pontos ocultos");
            Assert.AreEqual(0, ocultos, "arvores grandes ocultam pontos do caminho (semente " + semente + ")");
        }

        [Test]
        public void ComPerfil_EDeterministico()
        {
            string Assinatura()
            {
                var m = GerarComPerfil(12345);
                var linhas = new List<string>();
                foreach (HexTile ilha in m.mundo.Ilhas)
                    foreach (Transform t in Ativas(ilha))
                        linhas.Add(ilha.Coord + t.name + t.localPosition.ToString("F4") + t.localEulerAngles.y.ToString("F1")
                                   + VisualDeIlha.LimitesLocais(t).size.ToString("F4"));
                linhas.Sort();
                _m.Destruir(); _m = null;
                return string.Join("|", linhas);
            }
            Assert.AreEqual(Assinatura(), Assinatura());
        }

        [Test]
        public void ComPerfil_AlturaDasDecoracoesCabeNasFaixas_EHaArvoresGrandes()
        {
            int grandes = 0, pequenas = 0;
            foreach (int semente in Sementes)
            {
                var m = GerarComPerfil(semente);
                var perfil = PerfilReal();
                foreach (HexTile ilha in m.mundo.Ilhas)
                    foreach (Transform t in Ativas(ilha))
                    {
                        Assert.IsTrue(Classificar(perfil, t, out ModeloDeArte modelo, out _, out _), ilha.Coord + "/" + t.name + " fora da faixa");
                        if (modelo.categoria == CategoriaDeArte.ArvoreGrande) grandes++;
                        if (modelo.categoria == CategoriaDeArte.ArvorePequena) pequenas++;
                    }
                _m.Destruir(); _m = null;
            }
            Debug.Log("[Testes] 11 sementes: " + grandes + " arvores grandes, " + pequenas + " pequenas");
            Assert.Greater(grandes, 0, "nenhuma ArvoreGrande foi colocada: as regras de posicao a impedem?");
            Assert.Greater(pequenas, 0);
        }
    }
}

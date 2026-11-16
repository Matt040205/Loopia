using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Loopia.Hex.Tests
{
    /// <summary>ArtScaleProfile: tabela da Scene Bible, sorteio de tamanho e sorteio ponderado (sem cena).</summary>
    [Category("Logic")]
    public class ArtScaleProfileTests
    {
        readonly List<UnityEngine.Object> _criados = new List<UnityEngine.Object>();

        [TearDown]
        public void Limpar()
        {
            foreach (var o in _criados) if (o != null) UnityEngine.Object.DestroyImmediate(o);
            _criados.Clear();
        }

        ArtScaleProfile NovoPerfil()
        {
            var p = ScriptableObject.CreateInstance<ArtScaleProfile>();
            _criados.Add(p);
            return p;
        }

        GameObject NovoPrefab(string nome)
        {
            var g = new GameObject(nome);
            _criados.Add(g);
            return g;
        }

        [Test]
        public void RegrasPadrao_SaoATabelaDaSceneBible()
        {
            var p = NovoPerfil();
            void Confere(CategoriaDeArte c, float min, float max, EixoDeMedida eixo, bool longe)
            {
                Assert.IsTrue(p.TryRegra(c, out RegraDeCategoria r), "sem regra para " + c);
                Assert.AreEqual(min, r.tamanhoMinimo, 1e-6f, c + " minimo");
                Assert.AreEqual(max, r.tamanhoMaximo, 1e-6f, c + " maximo");
                Assert.AreEqual(eixo, r.eixo, c + " eixo");
                Assert.AreEqual(longe, r.soNoLadoLongeDaCamera, c + " lado longe");
            }
            Confere(CategoriaDeArte.ArvoreGrande, 1.8f, 2.4f, EixoDeMedida.Altura, true);
            Confere(CategoriaDeArte.ArvorePequena, 1.1f, 1.5f, EixoDeMedida.Altura, false);
            Confere(CategoriaDeArte.Arbusto, 0.3f, 0.6f, EixoDeMedida.Altura, false);
            Confere(CategoriaDeArte.Capim, 0.1f, 0.25f, EixoDeMedida.Altura, false);
            Confere(CategoriaDeArte.Pedra, 0.3f, 0.8f, EixoDeMedida.MaiorEixo, false);
            Confere(CategoriaDeArte.Cogumelo, 0.2f, 0.45f, EixoDeMedida.Altura, false);
            Confere(CategoriaDeArte.Tronco, 0.5f, 1.0f, EixoDeMedida.Comprimento, false);
            Confere(CategoriaDeArte.Outro, 0.28f, 0.65f, EixoDeMedida.MaiorEixo, false);
            Assert.AreEqual(1f, p.alturaDeReferencia, 1e-6f);
            Assert.AreEqual(45f, p.giroDaCamera, 1e-6f);
            Assert.AreEqual(30f, p.inclinacaoDaCamera, 1e-6f);
            // limiar do lado longe: 0.2 em todas (padrao original); so a arvore grande evita cobrir o caminho.
            foreach (var r in p.regras)
            {
                Assert.AreEqual(0.2f, r.limiarDoLadoLonge, 1e-6f, r.categoria + " limiar");
                Assert.AreEqual(r.categoria == CategoriaDeArte.ArvoreGrande, r.evitarCobrirCaminho, r.categoria + " evitarCobrirCaminho");
            }
        }

        [Test]
        public void TodaCategoriaDoEnumTemRegraPadrao()
        {
            var p = NovoPerfil();
            foreach (CategoriaDeArte c in Enum.GetValues(typeof(CategoriaDeArte)))
                Assert.IsTrue(p.TryRegra(c, out _), "falta regra padrao para " + c);
        }

        [Test]
        public void TryRegra_CategoriaSemRegra_DevolveFalso()
        {
            var p = NovoPerfil();
            p.regras.RemoveAll(r => r.categoria == CategoriaDeArte.Capim);
            Assert.IsFalse(p.TryRegra(CategoriaDeArte.Capim, out RegraDeCategoria r));
            Assert.IsNull(r);
        }

        [Test]
        public void SortearTamanho_FicaDentroDaFaixaDeCadaRegra_EmUnidadesDeMundo()
        {
            foreach (float referencia in new[] { 1f, 1.7f })
            {
                var p = NovoPerfil();
                p.alturaDeReferencia = referencia;
                var random = new System.Random(99);
                foreach (var regra in p.regras)
                {
                    float menor = float.MaxValue, maior = float.MinValue;
                    for (int i = 0; i < 300; i++)
                    {
                        float t = p.SortearTamanho(regra, random);
                        Assert.GreaterOrEqual(t, regra.tamanhoMinimo * referencia - 1e-5f, regra.categoria + " abaixo do minimo");
                        Assert.LessOrEqual(t, regra.tamanhoMaximo * referencia + 1e-5f, regra.categoria + " acima do maximo");
                        menor = Mathf.Min(menor, t); maior = Mathf.Max(maior, t);
                    }
                    Assert.Greater(maior - menor, 0.5f * (regra.tamanhoMaximo - regra.tamanhoMinimo) * referencia,
                        regra.categoria + ": o sorteio nao cobre a faixa");
                }
            }
        }

        [Test]
        public void SortearTamanho_ConsomeExatamenteUmSorteio()
        {
            var p = NovoPerfil();
            var a = new System.Random(5);
            var b = new System.Random(5);
            p.SortearTamanho(p.regras[0], a);
            b.NextDouble();
            Assert.AreEqual(b.Next(), a.Next(), "SortearTamanho tem que usar um NextDouble, para a sequencia ser previsivel");
        }

        [Test]
        public void Sortear_EhDeterministicoPorSemente()
        {
            var p = NovoPerfil();
            p.modelos.Add(new ModeloDeArte { prefab = NovoPrefab("A"), categoria = CategoriaDeArte.Pedra, peso = 1f });
            p.modelos.Add(new ModeloDeArte { prefab = NovoPrefab("B"), categoria = CategoriaDeArte.Cogumelo, peso = 2f });
            p.modelos.Add(new ModeloDeArte { prefab = NovoPrefab("C"), categoria = CategoriaDeArte.Tronco, peso = 3f });

            string Sequencia(int semente)
            {
                var r = new System.Random(semente);
                var nomes = new List<string>();
                for (int i = 0; i < 60; i++) nomes.Add(p.Sortear(r).prefab.name);
                return string.Join(",", nomes);
            }

            Assert.AreEqual(Sequencia(1234), Sequencia(1234));
            Assert.AreNotEqual(Sequencia(1234), Sequencia(4321));
        }

        [Test]
        public void Sortear_RespeitaOsPesos()
        {
            var p = NovoPerfil();
            p.modelos.Add(new ModeloDeArte { prefab = NovoPrefab("Leve"), categoria = CategoriaDeArte.Pedra, peso = 1f });
            p.modelos.Add(new ModeloDeArte { prefab = NovoPrefab("Pesado"), categoria = CategoriaDeArte.Pedra, peso = 3f });
            var r = new System.Random(7);
            int pesado = 0;
            const int n = 4000;
            for (int i = 0; i < n; i++) if (p.Sortear(r).prefab.name == "Pesado") pesado++;
            Assert.AreEqual(0.75f, pesado / (float)n, 0.03f, "peso 3 contra 1 deveria dar ~75%");
        }

        [Test]
        public void Sortear_MesmoPrefabEmDuasCategorias_RespeitaAProporcaoDoPeso()
        {
            // Como o asset real: a arvore entra como ArvorePequena (peso 2) e ArvoreGrande (peso 1).
            var p = NovoPerfil();
            var arvore = NovoPrefab("Arvore");
            p.modelos.Add(new ModeloDeArte { prefab = arvore, categoria = CategoriaDeArte.ArvorePequena, peso = 2f });
            p.modelos.Add(new ModeloDeArte { prefab = arvore, categoria = CategoriaDeArte.ArvoreGrande, peso = 1f });
            var r = new System.Random(3);
            int grandes = 0;
            const int n = 3000;
            for (int i = 0; i < n; i++) if (p.Sortear(r).categoria == CategoriaDeArte.ArvoreGrande) grandes++;
            Assert.AreEqual(1f / 3f, grandes / (float)n, 0.03f);
        }

        [Test]
        public void Sortear_IgnoraPrefabNulo_EPesoZero()
        {
            var p = NovoPerfil();
            p.modelos.Add(new ModeloDeArte { prefab = null, categoria = CategoriaDeArte.Pedra, peso = 100f });
            p.modelos.Add(new ModeloDeArte { prefab = NovoPrefab("SemPeso"), categoria = CategoriaDeArte.Pedra, peso = 0f });
            p.modelos.Add(null);
            p.modelos.Add(new ModeloDeArte { prefab = NovoPrefab("Valido"), categoria = CategoriaDeArte.Pedra, peso = 1f });
            var r = new System.Random(11);
            for (int i = 0; i < 200; i++) Assert.AreEqual("Valido", p.Sortear(r).prefab.name);
        }

        [Test]
        public void Sortear_SemNenhumModeloValido_DevolveNulo()
        {
            var p = NovoPerfil();
            Assert.IsNull(p.Sortear(new System.Random(1)));
            p.modelos.Add(new ModeloDeArte { prefab = null, peso = 1f });
            Assert.IsNull(p.Sortear(new System.Random(1)));
            p.modelos = null;
            Assert.IsNull(p.Sortear(new System.Random(1)));
        }

        [Test]
        public void FrenteDaCamera_ApontaParaOLadoLongeQueACameraOlha()
        {
            // HexIsoCamera.giro = 45: a camera fica em -frente e olha para +frente = (sen 45, 0, cos 45).
            var p = NovoPerfil();
            Vector3 f = p.FrenteDaCameraXZ;
            Assert.AreEqual(Mathf.Sin(45f * Mathf.Deg2Rad), f.x, 1e-5f);
            Assert.AreEqual(0f, f.y, 1e-6f);
            Assert.AreEqual(Mathf.Cos(45f * Mathf.Deg2Rad), f.z, 1e-5f);

            Assert.IsTrue(DecoracaoDeIlha.EstaLongeDaCamera(f * 1.6f, f), "o ponto na direcao em que a camera olha esta longe dela");
            Assert.IsFalse(DecoracaoDeIlha.EstaLongeDaCamera(-f * 1.6f, f), "o ponto atras (perto da camera) nao vale");
            Assert.IsFalse(DecoracaoDeIlha.EstaLongeDaCamera(new Vector3(f.z, 0f, -f.x) * 1.6f, f), "lateral (dot 0) nao vale");
        }
    }
}

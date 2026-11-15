using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Loopia.Hex.Tests
{
    /// <summary>
    /// Pass C (C1): toda carta sem modelo tem marcador; o marcador e arte pura (sem collider), cabe na ilha (raio 2,1) e
    /// respeita a escala da Bible. Cartas do CAMINHO: nada acima de 0,12 u no centro (onde o Lucca anda) e tudo que
    /// passa de 0,6 u fica no lado longe da camera. Instancia os prefabs numa cena vazia (sem Play).
    /// </summary>
    [Category("Logic")]
    public class MarcadoresDeCartaTests
    {
        static readonly string[] Cartas = { "Basica", "Floresta", "IlhaAlta", "Torre", "Cabana", "CampoPacifico", "AreaDeTreino" };
        static readonly Vector3 Fundo = new Vector3(0.70710678f, 0f, 0.70710678f);
        readonly List<GameObject> _criados = new List<GameObject>();

        [OneTimeSetUp] public void CenaVazia() => CenaDoTeste.NovaCenaVazia();
        [OneTimeTearDown] public void Restaurar() => CenaDoTeste.Restaurar();

        [TearDown]
        public void Limpar()
        {
            foreach (var g in _criados) if (g != null) Object.DestroyImmediate(g);
            _criados.Clear();
        }

        static PlataformaDef Carta(string nome)
        {
            var c = AssetDatabase.LoadAssetAtPath<PlataformaDef>("Assets/Dados/Plataformas/" + nome + ".asset");
            Assert.IsNotNull(c, nome);
            return c;
        }

        GameObject Instanciar(PlataformaDef c)
        {
            var g = (GameObject)PrefabUtility.InstantiatePrefab(c.marcador);
            g.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            _criados.Add(g);
            return g;
        }

        [TestCaseSource(nameof(Cartas))]
        public void Carta_TemMarcadorArtePuraQueCabeNaIlha(string nome)
        {
            var c = Carta(nome);
            Assert.IsNull(c.modelo, nome + ": o marcador substitui o vazio, nao o item flutuante");
            Assert.IsNotNull(c.marcador, nome + " sem marcador");
            var g = Instanciar(c);
            Assert.IsEmpty(g.GetComponentsInChildren<Collider>(true), nome + ": marcador com collider");
            var rs = g.GetComponentsInChildren<Renderer>();
            Assert.IsNotEmpty(rs);
            float alturaMax = 0f;
            foreach (var r in rs)
            {
                Bounds b = r.bounds;
                // a peca fica na ilha (centro dentro de 1,8); copas podem sobrar um pouco da borda (caixa ate 2,7)
                Assert.LessOrEqual(new Vector2(b.center.x, b.center.z).magnitude, 1.8f, nome + "/" + r.name + " fora da ilha");
                foreach (var canto in new[] { new Vector2(b.min.x, b.min.z), new Vector2(b.max.x, b.min.z), new Vector2(b.min.x, b.max.z), new Vector2(b.max.x, b.max.z) })
                    Assert.LessOrEqual(canto.magnitude, 2.7f, nome + "/" + r.name + " sobra demais da ilha");
                Assert.GreaterOrEqual(b.min.y, -0.05f, nome + "/" + r.name + " abaixo do topo da ilha");
                alturaMax = Mathf.Max(alturaMax, b.max.y);
            }
            Assert.LessOrEqual(alturaMax, 2.5f, nome + ": mais alto que a maior construcao da Bible (2,5 L)");
            Assert.Greater(alturaMax, 0.03f);
        }

        [TestCaseSource(nameof(Cartas))]
        public void CartaDoCaminho_NaoCobreOCentroNemOLucca(string nome)
        {
            var c = Carta(nome);
            if (c.onde != ColocacaoPermitida.NoCaminho) Assert.Pass("carta fora do caminho: construcao no centro e permitida");
            var g = Instanciar(c);
            foreach (var r in g.GetComponentsInChildren<Renderer>())
            {
                Bounds b = r.bounds;
                var centro = new Vector3(b.center.x, 0f, b.center.z);
                // ponto da caixa mais perto do centro da ilha (no plano)
                var maisPerto = new Vector2(Mathf.Clamp(0f, b.min.x, b.max.x), Mathf.Clamp(0f, b.min.z, b.max.z));
                bool longe = Vector3.Dot(centro.normalized, Fundo) > 0.2f;
                if (maisPerto.magnitude < 0.9f && b.max.y > 0.12f)
                    Assert.IsTrue(longe, nome + "/" + r.name + ": peca alta no centro, do lado da camera (onde o Lucca anda)");
                if (b.max.y > 0.6f)
                    Assert.Greater(Vector3.Dot(centro.normalized, Fundo), 0.2f, nome + "/" + r.name + ": peca alta do lado da camera cobriria o Lucca");
            }
        }

        [Test]
        public void FlorestaECampo_EscondemADecoracao_DemaisNao()
        {
            Assert.IsTrue(Carta("Floresta").ocultarDecoracao);
            Assert.IsTrue(Carta("CampoPacifico").ocultarDecoracao);
            foreach (var n in new[] { "Basica", "IlhaAlta", "Torre", "Cabana", "AreaDeTreino" }) Assert.IsFalse(Carta(n).ocultarDecoracao, n);
        }
    }
}

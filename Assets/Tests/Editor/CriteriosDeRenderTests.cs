using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.VFX;

namespace Loopia.Hex.Tests
{
    /// <summary>
    /// Criterios de render do trabalho (Sprint 6): luz dinamica com cookie, Decal Projector instanciado e
    /// tres efeitos de VFX Graph. Confere o que o avaliador ve no Inspector, sem Play. Nao salva a cena.
    /// </summary>
    [Category("SceneStatic")]
    public class CriteriosDeRenderSceneTests
    {
        const string CaminhoDoCookie = "Assets/Arte/Luz/CookieSombraDasNuvens.png";

        [OneTimeSetUp]
        public void Abrir() => CenaDoTeste.AbrirCenaPrincipal();

        [OneTimeTearDown]
        public void Restaurar() => CenaDoTeste.Restaurar();

        static T Unico<T>() where T : Object
        {
            T[] todos = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.AreEqual(1, todos.Length, "esperava exatamente um " + typeof(T).Name + " na HexPrototipo");
            return todos[0];
        }

        [Test]
        public void Sol_LuzRealtimeComCookieDasNuvens()
        {
            var sombra = Unico<SombraDasNuvens>();
            var luz = sombra.GetComponent<Light>();
            var cookie = AssetDatabase.LoadAssetAtPath<Texture>(CaminhoDoCookie);
            Assert.IsNotNull(cookie, CaminhoDoCookie);
            Assert.AreEqual(LightType.Directional, luz.type);
            Assert.AreEqual(LightmapBakeType.Realtime, luz.lightmapBakeType, "luz dinamica (Realtime)");
            Assert.AreSame(cookie, luz.cookie, "cookie no campo Cookie do componente Light");
            Assert.AreSame(cookie, sombra.cookie);
            Assert.AreEqual(TextureWrapMode.Repeat, cookie.wrapMode, "cookie direcional precisa repetir");
            Assert.Greater(sombra.vento.sqrMagnitude, 0f, "sem vento a sombra nao anda");
            Vector2 tamanho = luz.GetUniversalAdditionalLightData().lightCookieSize;
            Assert.AreEqual(sombra.tamanho, tamanho, "tamanho do cookie no dado URP igual ao do componente");
        }

        [Test]
        public void Cookie_ImportadoComAlfaDosTonsDeCinza()
        {
            var imp = (TextureImporter)AssetImporter.GetAtPath(CaminhoDoCookie);
            Assert.AreEqual(TextureImporterType.Cookie, imp.textureType);
            Assert.AreEqual(TextureImporterAlphaSource.FromGrayScale, imp.alphaSource,
                "sem alfa dos cinzas o cookie Alpha8 fica todo branco e nao aparece");
        }

        [Test]
        public void Renderer_TemDecalAtivo()
        {
            var dados = AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/PC_Renderer.asset");
            Assert.IsNotNull(dados);
            var decal = dados.rendererFeatures.Find(f => f is DecalRendererFeature);
            Assert.IsNotNull(decal, "PC_Renderer sem Decal Renderer Feature");
            Assert.IsTrue(decal.isActive);
        }

        [Test]
        public void Inimigos_DeixamMarcaComDecalProjector()
        {
            foreach (string caminho in new[] { "Assets/Dados/Lobo.asset", "Assets/Dados/Morcego.asset" })
            {
                var def = AssetDatabase.LoadAssetAtPath<InimigoDef>(caminho);
                Assert.IsNotNull(def.marcaNoChao, caminho + ": marcaNoChao");
                var projetor = def.marcaNoChao.GetComponent<DecalProjector>();
                Assert.IsNotNull(projetor, "prefab da marca sem DecalProjector");
                Assert.IsNotNull(def.marcaNoChao.GetComponent<MarcaNoChao>());
                Assert.IsNotNull(projetor.material, "DecalProjector sem material");
                Assert.AreEqual("Shader Graphs/Decal", projetor.material.shader.name);
                Assert.IsNotNull(projetor.material.GetTexture("Base_Map"), "material do decal sem textura");
            }
        }

        [Test]
        public void TresEfeitosDiferentesDeVfxGraph_LigadosNoJogo()
        {
            var lobo = AssetDatabase.LoadAssetAtPath<InimigoDef>("Assets/Dados/Lobo.asset");
            var efeitos = new Dictionary<string, GameObject>
            {
                { "morte de inimigo", lobo.efeitoDeMorte },
                { "coleta de moeda", Unico<GeradorDeMoedas>().efeitoDeColeta },
                { "carta colocada", Unico<MapaDePlataformas>().efeitoAoColocar },
            };
            var assets = new HashSet<VisualEffectAsset>();
            foreach (var par in efeitos)
            {
                Assert.IsNotNull(par.Value, par.Key + ": prefab do efeito");
                var ve = par.Value.GetComponent<VisualEffect>();
                Assert.IsNotNull(ve, par.Key + ": sem VisualEffect");
                Assert.IsNotNull(ve.visualEffectAsset, par.Key + ": VisualEffect sem grafo");
                assets.Add(ve.visualEffectAsset);
            }
            Assert.AreEqual(3, assets.Count, "os tres efeitos precisam ser grafos VFX diferentes");
        }

        [Test]
        public void Nuvens_ToonFofasNaCamada()
        {
            var camada = Unico<CamadaDeNuvens>();
            Assert.IsNotNull(camada.material, "camada sem material");
            Assert.AreEqual("Loopia/Nuvem Toon", camada.material.shader.name);
            Assert.IsFalse(ShaderUtil.ShaderHasError(camada.material.shader), "shader das nuvens com erro");
            Assert.GreaterOrEqual(camada.formatos, 3, "precisa de formatos dos tres portes");
            Assert.Less(camada.tamanho.x, camada.tamanho.y, "nuvens pequenas e grandes");
            Assert.LessOrEqual(camada.material.GetFloat("_Teto"), 0.95f, "teto de brilho abaixo do limiar do bloom");
            Assert.IsNotNull(camada.materialDaSombra, "sem material da sombra no chao");
            Assert.AreEqual("Shader Graphs/Decal", camada.materialDaSombra.shader.name);
            Assert.IsNotNull(camada.materialDaSombra.GetTexture("Base_Map"));
        }
    }

    [Category("Logic")]
    public class CriteriosDeRenderLogicTests
    {
        readonly List<Object> _criados = new List<Object>();

        [TearDown]
        public void Limpar()
        {
            foreach (var o in _criados) if (o != null) Object.DestroyImmediate(o);
            _criados.Clear();
        }

        [Test]
        public void SombraDasNuvens_AndaComOVentoEDaAVoltaSemSalto()
        {
            var go = new GameObject("sol de teste");
            _criados.Add(go);
            go.AddComponent<Light>().type = LightType.Directional;
            var sombra = go.AddComponent<SombraDasNuvens>();
            sombra.tamanho = new Vector2(10f, 10f);
            sombra.vento = new Vector2(1f, 0.5f);

            sombra.Avancar(4f);
            Assert.AreEqual(4f, sombra.Deslocamento.x, 1e-4f);
            Assert.AreEqual(2f, sombra.Deslocamento.y, 1e-4f);
            sombra.Avancar(8f); // 12 -> 2 (a textura repete a cada 10)
            Assert.AreEqual(2f, sombra.Deslocamento.x, 1e-4f);
            Assert.AreEqual(6f, sombra.Deslocamento.y, 1e-4f);
            Assert.AreEqual(sombra.Deslocamento, go.GetComponent<Light>().GetUniversalAdditionalLightData().lightCookieOffset);
        }

        [Test]
        public void MarcaNoChao_SomePeloFadeEDepoisESeDestroi()
        {
            var go = new GameObject("marca de teste");
            _criados.Add(go);
            go.AddComponent<DecalProjector>();
            var marca = go.AddComponent<MarcaNoChao>();
            marca.tempoVisivel = 2f;
            marca.tempoSumindo = 2f;

            Assert.IsTrue(marca.Envelhecer(1f));
            Assert.AreEqual(1f, marca.Projetor.fadeFactor, 1e-4f, "inteira antes de comecar a sumir");
            Assert.IsTrue(marca.Envelhecer(2f));
            Assert.AreEqual(0.5f, marca.Projetor.fadeFactor, 1e-4f, "metade do caminho");
            Assert.IsFalse(marca.Envelhecer(2f), "passou do tempo: some");
            Assert.IsTrue(marca == null, "destruida ao sumir");
        }

        [Test]
        public void MalhaDaNuvem_FundoAchatadoEMesmaSementeMesmoFormato()
        {
            Mesh a = MalhaDeNuvem.Criar(new System.Random(42), 2, 1);
            Mesh b = MalhaDeNuvem.Criar(new System.Random(42), 2, 1);
            _criados.Add(a);
            _criados.Add(b);
            Assert.Greater(a.vertexCount, 0);
            Assert.AreEqual(a.vertexCount, b.vertexCount, "mesma semente, mesmo formato");
            Assert.AreEqual(a.bounds.center, b.bounds.center);
            Assert.Greater(a.bounds.min.y, -0.1f, "o fundo da nuvem e achatado");
            Assert.Greater(a.bounds.max.y, 0.3f, "o topo fica fofo, bem acima do fundo");
            Assert.AreEqual(1f, a.bounds.size.x, 0.08f, "largura normalizada em uma unidade; a camada escala pela vista");

            // Abertura (cor do vertice R) ponderada pela area: os vertices se concentram nas costuras, entao contar
            // vertices exagera as dobras. Medido em 23/11: 29-47% da area em dobra, abertura media 0,54-0,70.
            Color32[] cores = a.colors32;
            Assert.AreEqual(a.vertexCount, cores.Length, "a abertura (dobras) vai na cor do vertice");
            Vector3[] v = a.vertices;
            int[] t = a.triangles;
            double area = 0, aberturaPorArea = 0, areaEmDobra = 0;
            for (int i = 0; i < t.Length; i += 3)
            {
                float face = Vector3.Cross(v[t[i + 1]] - v[t[i]], v[t[i + 2]] - v[t[i]]).magnitude * 0.5f;
                float abertura = (cores[t[i]].r + cores[t[i + 1]].r + cores[t[i + 2]].r) / (3f * 255f);
                area += face;
                aberturaPorArea += face * abertura;
                if (abertura < 0.5f) areaEmDobra += face;
            }
            Assert.Greater(areaEmDobra / area, 0.05, "sem dobra entre gomos: a nuvem vira uma bola so");
            Assert.Less(areaEmDobra / area, 0.6, "dobra demais: a nuvem inteira ficaria na sombra");
            Assert.Greater(aberturaPorArea / area, 0.4);
        }

        [Test]
        public void MalhaDaNuvem_PortesCrescemEmGomosEFacesEscondidasSaem()
        {
            Mesh pequena = MalhaDeNuvem.Criar(new System.Random(7), 2, 0);
            Mesh grande = MalhaDeNuvem.Criar(new System.Random(7), 2, 2);
            _criados.Add(pequena);
            _criados.Add(grande);
            Assert.Greater(grande.triangles.Length, pequena.triangles.Length, "a grande tem mais gomos");
            // Icosfera com 2 subdivisoes = 320 faces por gomo; a pequena tem 4 gomos (3 na base + 1 em cima).
            Assert.Less(pequena.triangles.Length / 3, 4 * 320, "faces escondidas dentro de outros gomos deviam sair");
        }

        [Test]
        public void MalhaDaNuvem_CadaGomoLevaCentroERitmoParaOShader()
        {
            Mesh m = MalhaDeNuvem.Criar(new System.Random(11), 2, 2);
            _criados.Add(m);
            var centros = new List<Vector4>();
            var dados = new List<Vector4>();
            m.GetUVs(2, centros);
            m.GetUVs(3, dados);
            Assert.AreEqual(m.vertexCount, centros.Count, "UV2 = centro do gomo");
            Assert.AreEqual(m.vertexCount, dados.Count, "UV3 = fase, tipo, amplitude, frequencia");
            bool temEstavel = false, temBroto = false;
            foreach (Vector4 d in dados)
            {
                if (d.y < 0.5f) { temEstavel = true; Assert.That(d.z, Is.InRange(0.04f, 0.16f), "gomo estavel so incha um pouco"); }
                else { temBroto = true; Assert.That(d.z, Is.InRange(0.4f, 0.75f), "broto recolhe ate essa escala"); }
                Assert.Greater(d.w, 0f, "frequencia");
            }
            Assert.IsTrue(temEstavel && temBroto, "a nuvem grande tem gomos de base e brotos");
        }

        static List<Vector2> Quadrado() => new List<Vector2> { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };

        [Test]
        public void Zona_EnvoltoriaConvexaAntiHorariaSemPontosDeDentro()
        {
            var pontos = new List<Vector2>(Quadrado()) { new Vector2(0.5f, 0.5f), new Vector2(0.2f, 0.7f), new Vector2(0.5f, 0f) };
            List<Vector2> casco = ComposicaoDasNuvens.Envoltoria(pontos);
            Assert.AreEqual(4, casco.Count, "so os cantos ficam");
            Assert.AreEqual(1f, ComposicaoDasNuvens.Area(casco), 1e-5f);
            float soma = 0f;
            for (int i = 0; i < casco.Count; i++)
            {
                Vector2 a = casco[i], b = casco[(i + 1) % casco.Count];
                soma += a.x * b.y - b.x * a.y;
            }
            Assert.Greater(soma, 0f, "anti-horaria");
        }

        [Test]
        public void Zona_DistanciaComSinalENormalParaFora()
        {
            var zona = Quadrado();
            Assert.AreEqual(-0.25f, ComposicaoDasNuvens.Distancia(new Vector2(0.25f, 0.5f), zona, out Vector2 n1), 1e-5f, "dentro e negativa");
            Assert.AreEqual(-1f, n1.x, 1e-4f, "normal para fora pela aresta mais proxima (esquerda)");
            Assert.AreEqual(0.5f, ComposicaoDasNuvens.Distancia(new Vector2(1.5f, 0.5f), zona, out Vector2 n2), 1e-5f);
            Assert.AreEqual(1f, n2.x, 1e-4f);
            float canto = ComposicaoDasNuvens.Distancia(new Vector2(2f, 2f), zona, out Vector2 n3);
            Assert.AreEqual(Mathf.Sqrt(2f), canto, 1e-4f, "fora pelo canto");
            Assert.AreEqual(1f, n3.magnitude, 1e-4f);
            Assert.IsTrue(float.IsPositiveInfinity(ComposicaoDasNuvens.Distancia(Vector2.zero, new List<Vector2>(), out _)), "sem zona, nada bloqueia");
        }

        [Test]
        public void Zona_DesvioNuncaEntraEContornaQuemVemDeFrente()
        {
            var normal = Vector2.left; // a zona fica a direita; a nuvem vem da esquerda, de frente
            var vento = new Vector2(1f, 0f);
            Vector2 longe = ComposicaoDasNuvens.Desviar(vento, 0.5f, normal, 0.12f, 1f);
            Assert.AreEqual(vento, longe, "longe da zona nada muda");
            Vector2 naBorda = ComposicaoDasNuvens.Desviar(vento, 0f, normal, 0.12f, 1f);
            Assert.GreaterOrEqual(Vector2.Dot(naBorda, normal), -1e-5f, "na borda, nenhuma parte da velocidade entra");
            Assert.Greater(Mathf.Abs(naBorda.y), 0.5f, "de frente, ela escorrega pela borda em vez de parar");
            Vector2 dentro = ComposicaoDasNuvens.Desviar(vento, -0.05f, normal, 0.12f, 1f);
            Assert.Greater(Vector2.Dot(dentro, normal), 0f, "ja por cima, e empurrada para fora");
            for (float folga = 0.12f; folga >= 0f; folga -= 0.01f)
            {
                Vector2 v = ComposicaoDasNuvens.Desviar(vento, folga, normal, 0.12f, -1f);
                Assert.LessOrEqual(-Vector2.Dot(v, normal), 1f * (folga / 0.12f) + 1e-4f, "a entrada diminui ate zero na borda");
            }
        }

        [Test]
        public void Vida_CurvaSuaveSemSaltos()
        {
            Assert.AreEqual(0f, ComposicaoDasNuvens.Suave(0f));
            Assert.AreEqual(1f, ComposicaoDasNuvens.Suave(1f));
            float anterior = 0f;
            for (int i = 1; i <= 100; i++)
            {
                float v = ComposicaoDasNuvens.Suave(i / 100f);
                Assert.GreaterOrEqual(v, anterior);
                Assert.Less(v - anterior, 0.02f, "sem tranco ao nascer ou sumir");
                anterior = v;
            }
        }

        [Test]
        public void EfeitoPassageiro_ForaDoPlayNaoCriaNada()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Modelos/HexPrototype/BrilhoDaMoeda.prefab");
            Assert.IsNotNull(prefab);
            Assert.IsNull(EfeitoPassageiro.Tocar(prefab, Vector3.zero), "teste de EditMode nao pode sujar a cena");
            Assert.IsNull(EfeitoPassageiro.Tocar(null, Vector3.zero));
        }
    }
}

using NUnit.Framework;
using UnityEngine;

namespace Loopia.Hex.Tests
{
    /// <summary>
    /// Pass D1 (Logic): a matematica do DayNightController (global) sem abrir a cena do jogo. Cria o componente num GameObject
    /// temporario, com uma Light e uma Camera tambem temporarias, numa cena vazia; varre as 24 h em passos de 1 min de jogo.
    /// Nao salva nada. As RenderSettings escritas por Apply() ficam na cena vazia temporaria.
    /// </summary>
    [Category("Logic")]
    public class DayNightControllerTests
    {
        const float PassoEmHoras = 1f / 60f; // 1 min de jogo

        GameObject _raiz;
        DayNightController _dn;
        Light _sol;
        Camera _camera;

        [OneTimeSetUp]
        public void AbrirCenaVazia() => CenaDoTeste.NovaCenaVazia();

        [OneTimeTearDown]
        public void Restaurar() => CenaDoTeste.Restaurar();

        [SetUp]
        public void Criar()
        {
            _raiz = new GameObject("DayNightDeTeste");
            _sol = new GameObject("SolDeTeste").AddComponent<Light>();
            _sol.type = LightType.Directional;
            _camera = new GameObject("CameraDeTeste").AddComponent<Camera>();
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _dn = _raiz.AddComponent<DayNightController>();
            _dn.sunLight = _sol;
            _dn.targetCamera = _camera;
            _dn.autoAdvanceTime = false;
            // garante a paleta padrao (o Reset() do componente so roda pelo menu do Editor)
            Reflexao.Chamar(_dn, "SetDefaultPalette");
        }

        [TearDown]
        public void Destruir()
        {
            if (_raiz != null) Object.DestroyImmediate(_raiz);
            if (_sol != null) Object.DestroyImmediate(_sol.gameObject);
            if (_camera != null) Object.DestroyImmediate(_camera.gameObject);
        }

        [Test]
        public void Sol_NuncaIluminaPorBaixoDoHorizonte_Em24hDeMinutoEmMinuto()
        {
            for (float h = 0f; h < 24f; h += PassoEmHoras)
            {
                _dn.timeOfDay = h;
                _dn.Apply();
                // A luz aponta para onde o forward aponta: forward.y > 0 = luz vindo de baixo.
                float paraCima = _sol.transform.forward.y;
                if (_sol.intensity > 0.05f)
                    Assert.LessOrEqual(paraCima, 1e-4f, "hora " + h.ToString("F3") + ": luz vem de baixo (forward.y=" + paraCima + ", intensidade=" + _sol.intensity + ")");
            }
        }

        [Test]
        public void Sol_EmNenhumaHoraAlemDaElevacaoMinima_ENuncaAbaixoDeZero()
        {
            for (float h = 0f; h < 24f; h += PassoEmHoras)
            {
                _dn.AvaliarLuz(h, out float x, out float intensidade, out Color cor);
                Assert.GreaterOrEqual(x, 0f, "hora " + h);
                Assert.LessOrEqual(x, 180f, "hora " + h);
            }
        }

        [Test]
        public void Intensidade_NaoDaSaltoMaiorQue002PorMinutoDeJogo_Incluindo24para0()
        {
            const float limite = 0.02f;
            float maximo = 0f, onde = 0f;
            _dn.AvaliarLuz(0f, out _, out float anterior, out _);
            for (int minuto = 1; minuto <= 24 * 60; minuto++)
            {
                float h = minuto / 60f; // 24h == 0h (Repeat)
                _dn.AvaliarLuz(h, out _, out float atual, out _);
                float passo = Mathf.Abs(atual - anterior);
                if (passo > maximo) { maximo = passo; onde = h; }
                anterior = atual;
            }
            Debug.Log("[Testes] maior variacao de intensidade por minuto de jogo: " + maximo.ToString("F5") + " em " + onde.ToString("F2") + "h");
            Assert.LessOrEqual(maximo, limite, "salto de intensidade de " + maximo + " em " + onde + "h");
        }

        [Test]
        public void Intensidade_AmanhecerNaoEstoura_AntesDoNascerDoSolEhMenorQueMeio()
        {
            // Antes: 0,2 (5h17) -> 1,0 (6h) com o sol ainda abaixo do horizonte. Agora: a intensidade antes do nascer so chega a
            // 1,0 no fim do crepusculo, e nunca passa de 0,5 antes de metade do crepusculo.
            _dn.AvaliarLuz(_dn.sunriseHour - _dn.duracaoDoCrepusculo * 0.5f, out _, out float meio, out _);
            Assert.Less(meio, 0.6f);
            _dn.AvaliarLuz(_dn.sunriseHour - _dn.duracaoDoCrepusculo, out _, out float inicio, out _);
            Assert.AreEqual(0f, inicio, 1e-4f);
        }

        [Test]
        public void Noite_TemLuaAcimaDoHorizonteEIntensidadeDaLua()
        {
            _dn.AvaliarLuz(22f, out float x, out float intensidade, out Color cor);
            Assert.Greater(intensidade, 0.2f, "a lua tem que iluminar as 22h");
            Assert.Greater(x, 20f); Assert.Less(x, 160f);
            Assert.AreEqual(_dn.luaCor.b, cor.b, 1e-4f);
        }

        [Test]
        public void MeioDia_SolAltoEIntensidadeDaCurva()
        {
            _dn.AvaliarLuz(12f, out float x, out float intensidade, out _);
            Assert.AreEqual(90f, x, 1.5f);
            Assert.AreEqual(_dn.sunIntensityCurve.Evaluate(0.5f), intensidade, 1e-3f);
        }

        [Test]
        public void Apply_Linear_EscreveModoInicioFimECorDaNevoa()
        {
            _dn.modoDaNevoa = FogMode.Linear;
            _dn.fogInicio = 105f; _dn.fogFim = 230f;
            _dn.timeOfDay = 12f;
            _dn.Apply();
            Assert.IsTrue(RenderSettings.fog);
            Assert.AreEqual(FogMode.Linear, RenderSettings.fogMode);
            Assert.AreEqual(105f, RenderSettings.fogStartDistance, 1e-3f);
            Assert.AreEqual(230f, RenderSettings.fogEndDistance, 1e-3f);

            Color ceu = _dn.skyColorGradient.Evaluate(0.5f);
            Color esperado = DayNightController.CorDaNevoa(ceu, _dn.misturaDaNevoaComHorizonte, _dn.nevoaDessaturacao, _dn.nevoaClareamento, _dn.FatorDeDia(12f));
            Color real = RenderSettings.fogColor;
            Assert.AreEqual(esperado.r, real.r, 1e-3f); Assert.AreEqual(esperado.g, real.g, 1e-3f); Assert.AreEqual(esperado.b, real.b, 1e-3f);
            Assert.AreEqual(ceu.r, _camera.backgroundColor.r, 1e-3f, "fundo da camera continua sendo a cor do ceu");
        }

        [Test]
        public void Apply_Exponencial_EscreveDensidade_ModoAntigoContinuaDisponivel()
        {
            _dn.modoDaNevoa = FogMode.Exponential;
            _dn.fogDensity = 0.012f;
            _dn.misturaDaNevoaComHorizonte = 0f; // sem horizonte: cor da nevoa == cor do ceu (comportamento antigo)
            _dn.timeOfDay = 14.8f;
            _dn.Apply();
            Assert.AreEqual(FogMode.Exponential, RenderSettings.fogMode);
            Assert.AreEqual(0.012f, RenderSettings.fogDensity, 1e-6f);
            Color ceu = _dn.skyColorGradient.Evaluate(14.8f / 24f);
            Assert.AreEqual(ceu.r, RenderSettings.fogColor.r, 1e-3f);
            Assert.AreEqual(ceu.g, RenderSettings.fogColor.g, 1e-3f);
            Assert.AreEqual(ceu.b, RenderSettings.fogColor.b, 1e-3f);
        }

        [Test]
        public void Apply_FogFimNuncaFicaAntesDoInicio()
        {
            _dn.modoDaNevoa = FogMode.Linear;
            _dn.fogInicio = 120f; _dn.fogFim = 50f;
            _dn.Apply();
            Assert.Greater(RenderSettings.fogEndDistance, RenderSettings.fogStartDistance);
        }

        [Test]
        public void Apply_SyncFogDesligado_NaoMexeNoFog()
        {
            RenderSettings.fog = false; RenderSettings.fogMode = FogMode.Exponential; RenderSettings.fogStartDistance = 3f;
            _dn.syncFog = false;
            _dn.modoDaNevoa = FogMode.Linear; _dn.fogInicio = 105f;
            _dn.Apply();
            Assert.IsFalse(RenderSettings.fog);
            Assert.AreEqual(FogMode.Exponential, RenderSettings.fogMode);
            Assert.AreEqual(3f, RenderSettings.fogStartDistance, 1e-4f);
        }

        [Test]
        public void CorDaNevoa_MisturaZero_DevolveOCeu_MisturaUm_EhMaisClaraEMenosSaturada()
        {
            Color ceu = new Color(0.4f, 0.8f, 0.9f, 1f);
            Color zero = DayNightController.CorDaNevoa(ceu, 0f, 0.5f, 0.5f, 1f);
            Assert.AreEqual(ceu.r, zero.r, 1e-5f); Assert.AreEqual(ceu.g, zero.g, 1e-5f); Assert.AreEqual(ceu.b, zero.b, 1e-5f);

            Color um = DayNightController.CorDaNevoa(ceu, 1f, 0.5f, 0.5f, 1f);
            Color.RGBToHSV(ceu, out _, out float sCeu, out float vCeu);
            Color.RGBToHSV(um, out _, out float sUm, out float vUm);
            Assert.Less(sUm, sCeu, "o horizonte e menos saturado que o ceu");
            Assert.GreaterOrEqual(vUm, vCeu - 1e-4f, "o horizonte nao e mais escuro que o ceu de dia");

            // de noite (fator de dia 0) o clareamento some
            Color noite = DayNightController.CorDaNevoa(ceu, 1f, 0f, 1f, 0f);
            Assert.AreEqual(ceu.r, noite.r, 1e-5f);
        }

        [Test]
        public void Ambiente_NoturnoGanhaPreenchimento_DiurnoNao()
        {
            _dn.syncAmbientLight = true;
            _dn.timeOfDay = 12f; _dn.Apply();
            Color diurno = RenderSettings.ambientSkyColor;
            Color esperadoDiurno = _dn.skyColorGradient.Evaluate(0.5f);
            float lumEsp = esperadoDiurno.r * 0.2126f + esperadoDiurno.g * 0.7152f + esperadoDiurno.b * 0.0722f;
            // de dia so ha a dessaturacao: a luminancia do ambiente e a do ceu
            float lumDia = diurno.r * 0.2126f + diurno.g * 0.7152f + diurno.b * 0.0722f;
            Assert.AreEqual(lumEsp, lumDia, 0.01f);

            _dn.timeOfDay = 22f; _dn.Apply();
            Color noturno = RenderSettings.ambientSkyColor;
            Color base22 = _dn.skyColorGradient.Evaluate(22f / 24f);
            Assert.Greater(noturno.b, base22.b + 0.1f, "o preenchimento noturno tem que somar luz fria ao ambiente");
            Assert.Greater(noturno.r, base22.r + 0.1f);
        }

        [Test]
        public void FatorDeDia_ZeroNoMeioDaNoite_UmDeDia()
        {
            Assert.AreEqual(0f, _dn.FatorDeDia(0f), 1e-5f);
            Assert.AreEqual(0f, _dn.FatorDeDia(23.5f), 1e-5f);
            Assert.AreEqual(1f, _dn.FatorDeDia(12f), 1e-5f);
            Assert.AreEqual(1f, _dn.FatorDeDia(6f), 1e-5f);
            Assert.AreEqual(1f, _dn.FatorDeDia(18f), 1e-5f);
        }
    }
}

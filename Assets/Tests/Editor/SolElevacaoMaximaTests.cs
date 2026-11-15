using NUnit.Framework;
using UnityEngine;

namespace Loopia.Hex.Tests
{
    /// <summary>
    /// Pass D2 (Logic): com elevacaoMaximaDoSol &lt; 90 o sol nao passa pelo zenite (luz de lado ao meio-dia, o relevo le),
    /// nasce e se poe exatamente na mesma direcao do arco antigo, nunca vem de baixo e muda de direcao de forma continua.
    /// Mesmo esquema do DayNightControllerTests (componente temporario numa cena vazia).
    /// </summary>
    [Category("Logic")]
    public class SolElevacaoMaximaTests
    {
        const float PassoEmHoras = 1f / 60f;
        const float Maxima = 58f;

        GameObject _raiz;
        DayNightController _dn;
        Light _sol;
        Camera _camera;

        [OneTimeSetUp] public void AbrirCenaVazia() => CenaDoTeste.NovaCenaVazia();
        [OneTimeTearDown] public void Restaurar() => CenaDoTeste.Restaurar();

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
            Reflexao.Chamar(_dn, "SetDefaultPalette");
            _dn.elevacaoMaximaDoSol = Maxima;
        }

        [TearDown]
        public void Destruir()
        {
            if (_raiz != null) Object.DestroyImmediate(_raiz);
            if (_sol != null) Object.DestroyImmediate(_sol.gameObject);
            if (_camera != null) Object.DestroyImmediate(_camera.gameObject);
        }

        Vector3 Direcao(float hora)
        {
            _dn.timeOfDay = hora;
            _dn.Apply();
            return _sol.transform.forward;
        }

        static float Elevacao(Vector3 forward) => Mathf.Asin(Mathf.Clamp(-forward.y, -1f, 1f)) * Mathf.Rad2Deg;

        [Test]
        public void PadraoContinua90_ComportamentoDoD1()
        {
            var novo = new GameObject("padrao").AddComponent<DayNightController>();
            Assert.AreEqual(90f, novo.elevacaoMaximaDoSol);
            Object.DestroyImmediate(novo.gameObject);
        }

        [Test]
        public void DeDia_ElevacaoNuncaPassaDaMaxima_ENuncaVemDeBaixo()
        {
            for (float h = _dn.sunriseHour; h <= _dn.sunsetHour; h += PassoEmHoras)
            {
                float e = Elevacao(Direcao(h));
                Assert.LessOrEqual(e, Maxima + 0.05f, "hora " + h.ToString("F3"));
                Assert.GreaterOrEqual(e, _dn.elevacaoMinimaDoSol - 0.05f, "hora " + h.ToString("F3"));
            }
            Assert.AreEqual(Maxima, Elevacao(Direcao(12f)), 0.05f, "ao meio-dia o sol fica na elevacao maxima");
        }

        [Test]
        public void NascerEPor_MesmaDirecaoDoArcoAntigo()
        {
            foreach (float h in new[] { _dn.sunriseHour, _dn.sunsetHour, _dn.sunriseHour - 0.5f, _dn.sunsetHour + 0.5f })
            {
                Vector3 novo = Direcao(h);
                _dn.elevacaoMaximaDoSol = 90f;
                Vector3 antigo = Direcao(h);
                _dn.elevacaoMaximaDoSol = Maxima;
                Assert.Less(Vector3.Angle(novo, antigo), 0.05f, "hora " + h + ": direcao mudou em relacao ao D1");
            }
        }

        [Test]
        public void Direcao_MudaDeFormaContinua_NoMaximo1GrauPorMinutoDeJogo()
        {
            Vector3 anterior = Direcao(0f);
            float pior = 0f, onde = 0f;
            for (int minuto = 1; minuto <= 24 * 60; minuto++)
            {
                float h = minuto / 60f;
                Vector3 atual = Direcao(h);
                float passo = Vector3.Angle(anterior, atual);
                // a troca sol -> lua (intensidade 0 nas duas pontas) e a unica descontinuidade permitida
                bool troca = Mathf.Abs(h - (_dn.sunsetHour + _dn.duracaoDoCrepusculo)) < 0.02f ||
                             Mathf.Abs(h - (_dn.sunriseHour - _dn.duracaoDoCrepusculo)) < 0.02f;
                if (!troca && passo > pior) { pior = passo; onde = h; }
                anterior = atual;
            }
            Debug.Log("[Testes] maior giro do sol por minuto de jogo (elevacao maxima " + Maxima + "): " + pior.ToString("F3") + " graus em " + onde.ToString("F2") + "h");
            Assert.LessOrEqual(pior, 1f);
        }
    }
}

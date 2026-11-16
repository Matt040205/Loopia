using NUnit.Framework;
using UnityEngine;

namespace Loopia.Hex.Tests
{
    /// <summary>
    /// Sprint 4, legibilidade em telas grandes (docs/sprints/SPRINT-4.md, item 1): contas do zoom do jogador,
    /// regras de resolucao do build e escala da HUD IMGUI. Puro, sem cena.
    /// </summary>
    [Category("Logic")]
    public class LegibilidadeDaTelaTests
    {
        const float Aspecto = 16f / 9f;

        // --- Zoom: contas ---

        [Test]
        public void ZoomMinimo_EAFracaoDoMenorTamanho_ESemZoomQuandoOAnelJaCabe()
        {
            Assert.AreEqual(1f, HexIsoCamera.ZoomMinimo(-1f, 7f), "sem enquadramento ainda: sem zoom");
            Assert.AreEqual(1f, HexIsoCamera.ZoomMinimo(6f, 7f), "anel menor que o limite: sem zoom");
            Assert.AreEqual(7f / 16.829f, HexIsoCamera.ZoomMinimo(16.829f, 7f), 1e-5f);
        }

        [Test]
        public void LimitarDeslocamento_SemZoomEZero_ComZoomParaNaBordaDoAnelInteiro()
        {
            Assert.AreEqual(Vector2.zero, HexIsoCamera.LimitarDeslocamento(new Vector2(5f, -3f), 16f, Aspecto, 1f));

            Vector2 d = HexIsoCamera.LimitarDeslocamento(new Vector2(100f, -100f), 16f, 2f, 0.5f);
            Assert.AreEqual(16f, d.x, 1e-4f, "metade da largura do anel inteiro (16 x 2 x 0.5)");
            Assert.AreEqual(-8f, d.y, 1e-4f, "metade da altura do anel inteiro (16 x 0.5)");

            Vector2 dentro = HexIsoCamera.LimitarDeslocamento(new Vector2(3f, -2f), 16f, 2f, 0.5f);
            Assert.AreEqual(new Vector2(3f, -2f), dentro, "dentro do limite nao muda");
        }

        [Test]
        public void DeslocamentoAoAproximar_MantemParadoOPontoSobOCursor()
        {
            const float baseH = 16f;
            var cursor = new Vector2(0.3f, -0.4f);
            Vector2 antes = new Vector2(1f, 0.5f);
            Vector2 depois = HexIsoCamera.DeslocamentoAoAproximar(antes, cursor, baseH, Aspecto, 0.9f, 0.6f);

            // Ponto do mundo sob o cursor, no plano da camera: centro + cursor x meia-altura atual (x aspecto na horizontal).
            Vector2 p0 = antes + new Vector2(cursor.x * Aspecto * baseH * 0.9f, cursor.y * baseH * 0.9f);
            Vector2 p1 = depois + new Vector2(cursor.x * Aspecto * baseH * 0.6f, cursor.y * baseH * 0.6f);
            Assert.AreEqual(p0.x, p1.x, 1e-4f);
            Assert.AreEqual(p0.y, p1.y, 1e-4f);
        }

        [Test]
        public void DeslocamentoAoAproximar_AfastarTudo_VoltaAoCentro()
        {
            Vector2 d = HexIsoCamera.DeslocamentoAoAproximar(new Vector2(4f, -3f), new Vector2(0.8f, 0.8f), 16f, Aspecto, 0.5f, 1f);
            Assert.AreEqual(Vector2.zero, d);
        }

        // --- Resolucao do build ---

        [Test]
        public void Resolucao_JanelaNaPrimeiraAbertura_VaiParaTelaCheiaNoTamanhoDoMonitor()
        {
            // O caso do playtest: registro com janela 1280x720 num monitor 1920x1080.
            var d = ResolucaoDaTela.Decidir(1280, 720, FullScreenMode.Windowed, 1920, 1080, false, false);
            Assert.IsTrue(d.mudar);
            Assert.AreEqual(1920, d.largura);
            Assert.AreEqual(1080, d.altura);
            Assert.AreEqual(FullScreenMode.FullScreenWindow, d.modo);
        }

        [Test]
        public void Resolucao_JanelaDepoisDoPrimeiroAjuste_RespeitaOJogador()
        {
            Assert.IsFalse(ResolucaoDaTela.Decidir(1280, 720, FullScreenMode.Windowed, 1920, 1080, true, false).mudar);
        }

        [Test]
        public void Resolucao_TelaCheiaMenorQueOMonitor_SempreVaiParaODoMonitor_NoMesmoModo()
        {
            var sem = ResolucaoDaTela.Decidir(1280, 720, FullScreenMode.FullScreenWindow, 2560, 1440, true, false);
            Assert.IsTrue(sem.mudar);
            Assert.AreEqual(2560, sem.largura);
            Assert.AreEqual(1440, sem.altura);
            Assert.AreEqual(FullScreenMode.FullScreenWindow, sem.modo);

            var exclusiva = ResolucaoDaTela.Decidir(1024, 768, FullScreenMode.ExclusiveFullScreen, 1920, 1080, true, false);
            Assert.IsTrue(exclusiva.mudar);
            Assert.AreEqual(FullScreenMode.ExclusiveFullScreen, exclusiva.modo);
        }

        [Test]
        public void Resolucao_TelaCheiaNativa_Mantem()
        {
            Assert.IsFalse(ResolucaoDaTela.Decidir(1920, 1080, FullScreenMode.FullScreenWindow, 1920, 1080, false, false).mudar);
            Assert.IsFalse(ResolucaoDaTela.Decidir(1920, 1080, FullScreenMode.FullScreenWindow, 1920, 1080, true, false).mudar);
        }

        [Test]
        public void Resolucao_ArgumentoDeTelaOuMonitorDesconhecido_NuncaMexe()
        {
            Assert.IsFalse(ResolucaoDaTela.Decidir(1280, 720, FullScreenMode.Windowed, 1920, 1080, false, true).mudar);
            Assert.IsFalse(ResolucaoDaTela.Decidir(1280, 720, FullScreenMode.FullScreenWindow, 1920, 1080, true, true).mudar);
            Assert.IsFalse(ResolucaoDaTela.Decidir(1280, 720, FullScreenMode.Windowed, 0, 0, false, false).mudar);
        }

        [Test]
        public void Resolucao_ReconheceOsArgumentosDeTelaDaUnity()
        {
            Assert.IsTrue(ResolucaoDaTela.TemArgumentoDeTela(new[] { "Loopia.exe", "-screen-width", "1920" }));
            Assert.IsTrue(ResolucaoDaTela.TemArgumentoDeTela(new[] { "Loopia.exe", "-Window-Mode", "borderless" }));
            Assert.IsTrue(ResolucaoDaTela.TemArgumentoDeTela(new[] { "Loopia.exe", "-screen-fullscreen", "0" }));
            Assert.IsFalse(ResolucaoDaTela.TemArgumentoDeTela(new[] { "Loopia.exe", "-logFile", "saida.log" }));
            Assert.IsFalse(ResolucaoDaTela.TemArgumentoDeTela(new string[0]));
            Assert.IsFalse(ResolucaoDaTela.TemArgumentoDeTela(null));
        }

        // --- Escala da HUD ---

        [TestCase(720f, 1f)]
        [TestCase(1080f, 1f)]
        [TestCase(1440f, 4f / 3f)]
        [TestCase(2160f, 2f)]
        public void EscalaDaInterface_SoAumentaAcimaDe1080p(float alturaDaTela, float esperado)
        {
            Assert.AreEqual(esperado, EscalaDaInterface.Fator(alturaDaTela), 1e-5f);
        }
    }

    /// <summary>
    /// Zoom do jogador numa camera de verdade sobre um anel gerado (MundoDeTeste, semente do Pass A), sem Play:
    /// o ponto sob o cursor fica parado, a vista nunca sai do anel inteiro, afastar tudo volta exatamente ao
    /// enquadramento-base e a vista-base (usada pelas nuvens) nao muda com o zoom.
    /// </summary>
    [Category("Logic")]
    public class ZoomDaCameraNoMundoTests
    {
        MundoDeTeste _m;
        GameObject _objeto;
        Camera _cam;
        HexIsoCamera _iso;
        RenderTexture _alvo;

        [OneTimeSetUp] public void CenaVazia() => CenaDoTeste.NovaCenaVazia();
        [OneTimeTearDown] public void Restaurar() => CenaDoTeste.Restaurar();

        [SetUp]
        public void Montar()
        {
            _m = MundoDeTeste.Criar(12345);
            Assert.IsTrue(_m.Gerar());

            // Alvo fixo de 1920x1080: pixelRect e aspecto nao dependem do tamanho da Game view.
            _alvo = new RenderTexture(1920, 1080, 16);
            _objeto = new GameObject("CameraDeTeste");
            _cam = _objeto.AddComponent<Camera>();
            _cam.targetTexture = _alvo;
            _iso = _objeto.AddComponent<HexIsoCamera>();
            _iso.mundo = _m.mundo;
            _iso.margem = EnquadramentoDaCameraTests.MargemDaCena;
            _iso.menorMeiaAltura = 7f;
            _iso.Enquadrar();
            Assert.Greater(_iso.MeiaAlturaBase, 7f, "o anel de teste precisa ser maior que o limite de zoom");
        }

        [TearDown]
        public void Limpar()
        {
            if (_objeto != null) Object.DestroyImmediate(_objeto);
            if (_alvo != null) Object.DestroyImmediate(_alvo);
            if (_m != null) _m.Destruir();
            _m = null;
        }

        [Test]
        public void SemZoom_ACameraMostraOAnelInteiro()
        {
            Assert.AreEqual(1f, _iso.Zoom);
            Assert.AreEqual(_iso.MeiaAlturaBase, _cam.orthographicSize, 1e-5f);
            Assert.AreEqual(Vector2.zero, _iso.Deslocamento);
        }

        [Test]
        public void Aproximar_MantemParadoOPontoDoMundoSobOCursor()
        {
            float baseOrtho = _cam.orthographicSize;
            var cursor = new Vector2(1400f, 300f);
            Vector3 antes = _cam.ScreenToWorldPoint(new Vector3(cursor.x, cursor.y, 50f));

            _iso.Aproximar(3, cursor, imediato: true);

            Assert.AreEqual(baseOrtho * Mathf.Pow(_iso.passoDoZoom, 3), _cam.orthographicSize, 1e-3f);
            Vector3 depois = _cam.ScreenToWorldPoint(new Vector3(cursor.x, cursor.y, 50f));
            Assert.Less(Vector3.Distance(antes, depois), 1e-3f, "o ponto sob o cursor andou: antes " + antes + " depois " + depois);
        }

        [Test]
        public void AproximarTudoEArrastarLonge_AVistaNuncaSaiDoAnelInteiro()
        {
            _iso.Aproximar(20, new Vector2(960f, 540f), imediato: true);
            Assert.AreEqual(HexIsoCamera.ZoomMinimo(_iso.MeiaAlturaBase, 7f), _iso.Zoom, 1e-4f, "para no menor tamanho");
            Assert.AreEqual(7f, _cam.orthographicSize, 1e-3f);

            _iso.Arrastar(new Vector2(-1e6f, 1e6f));
            Vector2 d = _iso.Deslocamento;
            float a = _cam.aspect, s = _cam.orthographicSize, meia = _iso.MeiaAlturaBase;
            Assert.Greater(Mathf.Abs(d.x), 0.1f, "o arraste tem que mover a vista");
            Assert.LessOrEqual(Mathf.Abs(d.x) + s * a, meia * a + 1e-3f, "saiu pela lateral do anel inteiro");
            Assert.LessOrEqual(Mathf.Abs(d.y) + s, meia + 1e-3f, "saiu por cima/baixo do anel inteiro");
        }

        [Test]
        public void AfastarAlemDoAnelInteiro_NaoPassaDoEnquadramentoBase()
        {
            float baseOrtho = _cam.orthographicSize;
            _iso.Aproximar(-5, new Vector2(100f, 100f), imediato: true);
            Assert.AreEqual(1f, _iso.Zoom);
            Assert.AreEqual(baseOrtho, _cam.orthographicSize, 1e-5f);
        }

        [Test]
        public void VoltarAoAnelInteiro_RestauraExatamenteAPosicaoEOTamanho()
        {
            Vector3 posicao = _objeto.transform.position;
            float baseOrtho = _cam.orthographicSize;

            _iso.Aproximar(4, new Vector2(200f, 900f), imediato: true);
            _iso.Arrastar(new Vector2(50f, -80f));
            Assert.Greater(Vector3.Distance(posicao, _objeto.transform.position), 0.01f);

            _iso.VoltarAoAnelInteiro(imediato: true);
            Assert.AreEqual(baseOrtho, _cam.orthographicSize, 1e-5f);
            Assert.Less(Vector3.Distance(posicao, _objeto.transform.position), 1e-4f);
        }

        [Test]
        public void ReEnquadrar_GuardaOZoomEABase()
        {
            float baseOrtho = _iso.MeiaAlturaBase;
            _iso.Aproximar(2, new Vector2(960f, 540f), imediato: true);
            float zoom = _iso.Zoom;

            _iso.Enquadrar(); // o LateUpdate faz isto a cada 0,25 s e o MapaDePlataformas ao mudar o relevo

            Assert.AreEqual(baseOrtho, _iso.MeiaAlturaBase, 1e-5f);
            Assert.AreEqual(zoom, _iso.Zoom, 1e-6f);
            Assert.AreEqual(baseOrtho * zoom, _cam.orthographicSize, 1e-4f);
        }

        [Test]
        public void VistaBase_NaoMudaComOZoom_EBateComACameraSemZoom()
        {
            Assert.IsTrue(_iso.TryVistaBase(out Vector3 pos0, out Quaternion rot0, out float meia0));

            // Sem zoom, o raio da vista-base acerta o mesmo ponto que o da camera (nuvens iguais ao que eram).
            var plano = new Plane(Vector3.up, new Vector3(0f, -6f, 0f));
            foreach (var vp in new[] { new Vector3(0f, 0f), new Vector3(1f, 0f), new Vector3(0f, 1f), new Vector3(1f, 1f) })
            {
                Ray daCamera = _cam.ViewportPointToRay(vp);
                Ray daBase = CamadaDeNuvens.RaioDaVistaBase(vp, pos0, rot0, meia0, _cam.aspect);
                Assert.IsTrue(plano.Raycast(daCamera, out float d1));
                Assert.IsTrue(plano.Raycast(daBase, out float d2));
                Assert.Less(Vector3.Distance(daCamera.GetPoint(d1), daBase.GetPoint(d2)), 1e-3f, "canto " + vp);
            }

            _iso.Aproximar(3, new Vector2(300f, 700f), imediato: true);
            _iso.Arrastar(new Vector2(-40f, 25f));

            Assert.IsTrue(_iso.TryVistaBase(out Vector3 pos1, out Quaternion rot1, out float meia1));
            Assert.Less(Vector3.Distance(pos0, pos1), 1e-5f);
            Assert.AreEqual(meia0, meia1, 1e-6f);
            Assert.Less(Quaternion.Angle(rot0, rot1), 1e-3f);
        }

        [Test]
        public void Perspectiva_NaoTemZoom()
        {
            _iso.ortografica = false;
            _iso.Enquadrar();
            _iso.Aproximar(3, new Vector2(960f, 540f), imediato: true);
            Assert.AreEqual(1f, _iso.Zoom);
            Assert.IsFalse(_iso.TryVistaBase(out _, out _, out _), "nuvens usam a camera direto em perspectiva");
        }
    }
}

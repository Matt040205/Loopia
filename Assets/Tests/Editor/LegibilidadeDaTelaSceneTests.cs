using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Loopia.Hex.Tests
{
    /// <summary>
    /// Sprint 4, legibilidade em telas grandes: configuracao do projeto e da HexPrototipo (sem Play, sem salvar).
    /// MSAA do PC fica em RenderDoPCTests; o raio maximo do anel em HexPrototipoSceneTests.
    /// </summary>
    [Category("SceneStatic")]
    public class LegibilidadeDaTelaSceneTests
    {
        [OneTimeSetUp] public void AbrirCena() => CenaDoTeste.AbrirCenaPrincipal();
        [OneTimeTearDown] public void Restaurar() => CenaDoTeste.Restaurar();

        [Test]
        public void Player_TelaCheiaNaResolucaoDoMonitor_EJanelaRedimensionavel()
        {
            Assert.IsTrue(PlayerSettings.defaultIsNativeResolution, "o build tem que abrir na resolucao do monitor");
            Assert.AreEqual(FullScreenMode.FullScreenWindow, PlayerSettings.fullScreenMode);
            Assert.IsTrue(PlayerSettings.resizableWindow,
                "em janela o jogador precisa poder maximizar (no playtest ficou preso em 1280x720 num monitor 1920x1080)");
        }

        [Test]
        public void Camera_ZoomDoJogadorLigado_EAproximaAlemDoMenorAnel()
        {
            var iso = Object.FindFirstObjectByType<HexIsoCamera>();
            Assert.IsNotNull(iso);
            Assert.IsTrue(iso.ortografica);
            Assert.IsTrue(iso.enquadrarAutomaticamente, "o anel inteiro continua sendo a vista padrao");
            Assert.IsTrue(iso.permitirZoom);
            // Menor anel medido no Pass A (semente 2024, 24 ilhas, 16:9): orthographicSize 15.967. O limite do zoom
            // tem que ficar bem abaixo disso, senao a roda do mouse quase nao aproxima.
            Assert.Less(iso.menorMeiaAltura, 15.967f * 0.6f);
            Assert.Greater(iso.menorMeiaAltura, 3f, "mais perto que isso o Lucca sai da tela a cada passo");
            Assert.IsTrue(iso.GetComponent<Camera>().allowMSAA, "a camera do jogo nao pode desligar o MSAA do pipeline");
        }
    }
}

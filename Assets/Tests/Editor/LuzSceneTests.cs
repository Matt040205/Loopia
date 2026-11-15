using NUnit.Framework;
using UnityEngine;

namespace Loopia.Hex.Tests
{
    /// <summary>
    /// Pass D2 na cena HexPrototipo (sem Play, sem salvar): o sol da cena tem teto de elevacao (luz de lado ao meio-dia) e as
    /// decisoes medidas do D2 continuam valendo: sem tonemapping no Volume da cena e distancia de sombra que nao alcanca as
    /// ilhas (sombras custavam +57 % de batches sem ganho visivel; docs/art/pass-d2/RELATORIO_PASS_D2.md).
    /// </summary>
    [Category("SceneStatic")]
    public class LuzSceneTests
    {
        [OneTimeSetUp] public void AbrirCena() => CenaDoTeste.AbrirCenaPrincipal();
        [OneTimeTearDown] public void Restaurar() => CenaDoTeste.Restaurar();

        [Test]
        public void Sol_TemTetoDeElevacao_EntreNascerEZenite()
        {
            var dn = Object.FindFirstObjectByType<DayNightController>();
            Assert.IsNotNull(dn);
            Assert.That(dn.elevacaoMaximaDoSol, Is.InRange(45f, 70f), "sem teto o meio-dia volta a ter luz de cima (relevo some)");
            Assert.Greater(dn.elevacaoMaximaDoSol, dn.elevacaoMinimaDoSol + 10f);
        }

        [Test]
        public void Volume_SemTonemapping_SombraNaoAlcancaAsIlhas()
        {
            foreach (var v in Object.FindObjectsByType<UnityEngine.Rendering.Volume>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (v.sharedProfile == null) continue;
                if (v.sharedProfile.TryGet(out UnityEngine.Rendering.Universal.Tonemapping tm))
                    Assert.IsFalse(tm.active && tm.mode.overrideState && tm.mode.value != UnityEngine.Rendering.Universal.TonemappingMode.None,
                        v.name + ": tonemapping ligado (Neutral lavava, ACES escurecia a noite; ver relatorio do D2)");
            }
            var urp = (UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset)UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
            Assert.LessOrEqual(urp.shadowDistance, 70f, "a camera fica a 70 u: sombra alem disso liga sombras nas ilhas (+57 % batches medidos)");
        }
    }
}

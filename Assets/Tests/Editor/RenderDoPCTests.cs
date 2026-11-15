using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Loopia.Hex.Tests
{
    /// <summary>
    /// Pass D2 (render), decisoes medidas em docs/art/pass-d2/RELATORIO_PASS_D2_RENDER.md:
    /// - Opaque Texture desligada: nenhum shader usado na cena le _CameraOpaqueTexture (so URP Lit/Unlit em runtime).
    /// - SSAO desligado: efeito imperceptivel na camera do jogo e o prepass de DepthNormals redesenhava toda a geometria
    ///   (batches 531 -> 290, triangulos 912k -> 455k no Editor).
    /// Abre a HexPrototipo (sem Play, sem salvar) para conferir os shaders das ilhas/fundo/decoracao.
    /// </summary>
    [Category("SceneStatic")]
    public class RenderDoPCTests
    {
        [OneTimeSetUp] public void AbrirCena() => CenaDoTeste.AbrirCenaPrincipal();
        [OneTimeTearDown] public void Restaurar() => CenaDoTeste.Restaurar();

        [Test]
        public void PipelineDoPC_SemOpaqueTexture_SemSSAO()
        {
            var urp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");
            Assert.IsNotNull(urp);
            Assert.IsFalse(urp.supportsCameraOpaqueTexture, "Opaque Texture religada: so faz sentido se algum shader passar a ler a cor da cena");
            var dados = AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/PC_Renderer.asset");
            Assert.IsNotNull(dados);
            foreach (var f in dados.rendererFeatures)
                if (f is ScreenSpaceAmbientOcclusion)
                    Assert.IsFalse(f.isActive, "SSAO religado: o prepass de DepthNormals dobra a geometria desenhada");
        }

        [Test]
        public void ShadersDaCena_NaoDependemDaCorDaCena()
        {
            int vistos = 0;
            foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                foreach (var m in r.sharedMaterials)
                {
                    if (m == null || m.shader == null) continue;
                    vistos++;
                    string caminho = AssetDatabase.GetAssetPath(m.shader);
                    if (!caminho.EndsWith(".shadergraph")) continue;
                    StringAssert.DoesNotContain("SceneColorNode", System.IO.File.ReadAllText(caminho), r.name + ": shader le a cor da cena (precisa da Opaque Texture)");
                }
            Assert.Greater(vistos, 0);
        }
    }
}

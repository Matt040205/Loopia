using System.Linq;
using NUnit.Framework;
using UnityEditor;

namespace Loopia.Hex.Tests
{
    /// <summary>
    /// Estabilizacao do MVP (P0-3/P0-4): nome do produto e cenas do build padrao. companyName fica fixo porque ele e o
    /// productName formam a pasta do save (Application.persistentDataPath); trocar exige migracao (DadosDoJogador).
    /// </summary>
    [Category("SceneStatic")]
    public class ConfiguracaoDeBuildTests
    {
        static readonly string[] CenasDoMvp = { "Assets/Scenes/Acampamento.unity", "Assets/Scenes/HexPrototipo.unity" };
        static readonly string[] CenasLegadas =
            { "Assets/Scenes/Menu.unity", "Assets/Scenes/Testes.unity", "Assets/Scenes/GameOver.unity", "Assets/Scenes/Win.unity" };

        [Test]
        public void Produto_ChamaLoopia_EmpresaNaoMuda()
        {
            Assert.AreEqual("Loopia", PlayerSettings.productName);
            Assert.AreEqual("DefaultCompany", PlayerSettings.companyName,
                "mudar a empresa muda a pasta do save; a migracao so conhece .../DefaultCompany/GameJam13-06");
            Assert.AreEqual(DadosDoJogador.PastaDoProdutoAntigo, "GameJam13-06");
        }

        [Test]
        public void BuildPadrao_SoAcampamentoEHexPrototipo_NessaOrdem()
        {
            string[] habilitadas = EditorBuildSettings.scenes.Where(c => c.enabled).Select(c => c.path).ToArray();
            CollectionAssert.AreEqual(CenasDoMvp, habilitadas, "Acampamento (0) abre o jogo; HexPrototipo (1) e a jornada");
        }

        [Test]
        public void CenasLegadas_ContinuamNoProjeto_MasForaDoBuild()
        {
            foreach (string cena in CenasLegadas)
            {
                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<SceneAsset>(cena), cena + " foi apagada; so devia sair do build");
                Assert.IsFalse(EditorBuildSettings.scenes.Any(c => c.path == cena && c.enabled), cena + " voltou ao build");
            }
        }
    }
}

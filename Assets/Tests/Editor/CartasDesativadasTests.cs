using NUnit.Framework;
using UnityEditor;

namespace Loopia.Hex.Tests
{
    /// <summary>
    /// Mola fora do jogo (decisao de design depois da Sprint 5B): a carta nao entra na mao, no sorteio nem pode ser
    /// colocada, e a Ilha Alta (que exige Mola no mapa) deixa de aparecer. A mecanica da Mola continua no codigo.
    /// </summary>
    [Category("Logic")]
    public class CartasDesativadasTests
    {
        const string Dados = "Assets/Dados/Plataformas/";

        static PlataformaDef D(string nome)
        {
            var d = AssetDatabase.LoadAssetAtPath<PlataformaDef>(Dados + nome + ".asset");
            Assert.IsNotNull(d, nome);
            return d;
        }

        [Test]
        public void Mola_EstaForaDoJogo_EAIlhaAltaNaoAparece()
        {
            var mola = D("Mola");
            Assert.IsTrue(mola.desativada, "Mola desativada");
            Assert.IsFalse(mola.PodeAparecer(null));
            Assert.IsFalse(D("IlhaAlta").PodeAparecer(null), "sem Mola no mapa nao ha Ilha Alta");
        }

        [Test]
        public void DemaisCartas_ContinuamAtivas()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:PlataformaDef", new[] { "Assets/Dados/Plataformas" }))
            {
                var d = AssetDatabase.LoadAssetAtPath<PlataformaDef>(AssetDatabase.GUIDToAssetPath(guid));
                if (d.name == "Mola") continue;
                Assert.IsFalse(d.desativada, d.name + " foi desativada sem querer");
            }
        }
    }
}

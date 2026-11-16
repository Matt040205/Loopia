using NUnit.Framework;
using UnityEngine;

namespace Loopia.Hex.Tests
{
    /// <summary>
    /// Estabilizacao do MVP (P2-1): no Mirante (e no chefe) o tiro vai em qualquer direcao, mas o tronco so gira 65°.
    /// O que passa da folga do tronco vira o corpo visual inteiro; a raiz de navegacao nao gira.
    /// </summary>
    [Category("Logic")]
    public class LuccaMiraTests
    {
        const float Folga = 52f;   // 0,8 x 65
        const float Corpo = 120f;

        [Test]
        public void DentroDaFolga_SoOTroncoMira()
        {
            Assert.AreEqual(0f, LuccaAnimationBridge.GiroDoCorpo(0f, Folga, Corpo));
            Assert.AreEqual(0f, LuccaAnimationBridge.GiroDoCorpo(40f, Folga, Corpo));
            Assert.AreEqual(0f, LuccaAnimationBridge.GiroDoCorpo(-52f, Folga, Corpo));
        }

        [Test]
        public void AlemDaFolga_OCorpoViraOExcesso_ComOMesmoSinal()
        {
            Assert.AreEqual(38f, LuccaAnimationBridge.GiroDoCorpo(90f, Folga, Corpo), 1e-4f);
            Assert.AreEqual(-38f, LuccaAnimationBridge.GiroDoCorpo(-90f, Folga, Corpo), 1e-4f);
        }

        [Test]
        public void AlvoAtras_CorpoNoLimite_ETroncoDentroDos65()
        {
            float corpo = LuccaAnimationBridge.GiroDoCorpo(178f, Folga, Corpo);
            Assert.AreEqual(120f, corpo, 1e-4f, "o corpo para no limite");
            Assert.LessOrEqual(Mathf.Abs(178f - corpo), 65f, "o resto cabe no tronco: sem torcao de 180°");
            Assert.AreEqual(-120f, LuccaAnimationBridge.GiroDoCorpo(-178f, Folga, Corpo), 1e-4f);
            Assert.AreEqual(-120f, LuccaAnimationBridge.GiroDoCorpo(182f, Folga, Corpo), 1e-4f, "182 = -178");
        }

        [Test]
        public void ErroHorizontal_IgnoraAAltura()
        {
            Assert.AreEqual(0f, LuccaAnimationBridge.ErroHorizontal(new Vector3(0, 0, 1), new Vector3(0, 5, 3), Vector3.up), 1e-3f);
            Assert.AreEqual(180f, LuccaAnimationBridge.ErroHorizontal(Vector3.forward, Vector3.back, Vector3.up), 1e-3f);
            Assert.AreEqual(90f, LuccaAnimationBridge.ErroHorizontal(Vector3.forward, Vector3.right + Vector3.up, Vector3.up), 1e-3f);
        }
    }
}

using NUnit.Framework;
using UnityEngine;

namespace Loopia.Hex.Tests
{
    /// <summary>
    /// Pass D2: inimigos sem prefab e flechas reaproveitam um material por cor em vez de criar (e vazar) um por spawn.
    /// </summary>
    [Category("Logic")]
    public class MateriaisCompartilhadosTests
    {
        [Test]
        public void MesmaCor_MesmoMaterial_CorDiferente_OutroMaterial()
        {
            var a = MateriaisCompartilhados.Lit("Teste", new Color(0.1f, 0.2f, 0.3f));
            int depoisDoPrimeiro = MateriaisCompartilhados.Quantidade;
            for (int i = 0; i < 50; i++)
                Assert.AreSame(a, MateriaisCompartilhados.Lit("Teste", new Color(0.1f, 0.2f, 0.3f)), "spawn " + i + " criou material novo");
            Assert.AreEqual(depoisDoPrimeiro, MateriaisCompartilhados.Quantidade, "50 spawns da mesma cor nao podem criar materiais");
            var b = MateriaisCompartilhados.Lit("Teste", new Color(0.9f, 0.2f, 0.3f));
            Assert.AreNotSame(a, b);
            Assert.AreNotSame(a, MateriaisCompartilhados.Lit("Teste", new Color(0.1f, 0.2f, 0.3f), 0.1f), "suavidade diferente e outro material");
        }

        [Test]
        public void Material_TemACorEAsPropriedadesPedidas()
        {
            var cor = new Color(0.4f, 0.6f, 0.8f, 1f);
            var m = MateriaisCompartilhados.Lit("Morcego", cor, 0.1f);
            Assert.AreEqual((Color32)cor, (Color32)m.GetColor("_BaseColor"));
            Assert.AreEqual(0.1f, m.GetFloat("_Smoothness"), 1e-4f);
            Assert.AreEqual(0f, m.GetFloat("_Metallic"), 1e-4f);
        }
    }
}

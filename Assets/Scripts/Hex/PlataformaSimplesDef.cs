using UnityEngine;

namespace Loopia.Hex
{
    /// <summary>
    /// Plataforma de bonus fixo. Cobre a basica (0 de tudo), o campo pacifico (+2 PV)
    /// e a torre (+8% de dano) do GDD.
    /// </summary>
    [CreateAssetMenu(fileName = "Plataforma", menuName = "Loopia/Plataforma/Bonus simples")]
    public class PlataformaSimplesDef : PlataformaDef
    {
        [Header("Bonus")]
        [Tooltip("Quanto de PV maximo esta plataforma soma ao Lucca.")]
        public int bonusDePv = 0;

        [Tooltip("Quanto de dano soma as flechas, em fracao. 0.08 = +8%.")]
        public float bonusDeDano = 0f;

        public override int BonusDePv(Plataforma plataforma) => bonusDePv;

        public override float BonusDeDano(Plataforma plataforma) => bonusDeDano;
    }
}

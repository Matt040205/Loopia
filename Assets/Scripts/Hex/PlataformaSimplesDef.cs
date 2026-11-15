using UnityEngine;

namespace Loopia.Hex
{
    /// <summary>
    /// Plataforma de bonus fixo. Cobre a basica (0 de tudo), o campo pacifico (+2 PV)
    /// e a torre (+8% de dano) do GDD, e as cartas de sobrevivencia de G2: Santuario (+3 PV),
    /// Fonte Restauradora (+2 por coracao) e Oficina de Arqueiro (+2 flechas maximas).
    /// </summary>
    [CreateAssetMenu(fileName = "Plataforma", menuName = "Loopia/Plataforma/Bonus simples")]
    public class PlataformaSimplesDef : PlataformaDef
    {
        [Header("Bonus")]
        [Tooltip("Quanto de PV maximo esta plataforma soma ao Lucca.")]
        public int bonusDePv = 0;

        [Tooltip("Quanto de dano soma as flechas, em fracao. 0.08 = +8%.")]
        public float bonusDeDano = 0f;

        [Tooltip("Flechas maximas extras (Oficina de Arqueiro).")]
        [Min(0)] public int bonusDeFlechas = 0;

        [Tooltip("PV extra a cada coracao coletado (Fonte Restauradora). Nao afeta outras curas.")]
        [Min(0)] public int bonusDeCuraPorCoracao = 0;

        public override int BonusDePv(Plataforma plataforma) => bonusDePv;

        public override float BonusDeDano(Plataforma plataforma) => bonusDeDano;

        public override int BonusDeFlechas(Plataforma plataforma) => bonusDeFlechas;

        public override int BonusDeCuraPorCoracao(Plataforma plataforma) => bonusDeCuraPorCoracao;
    }
}

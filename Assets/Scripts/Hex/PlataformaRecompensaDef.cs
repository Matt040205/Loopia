using UnityEngine;

namespace Loopia.Hex
{
    /// <summary>
    /// Plataforma de recompensa: o Lucca pega ao passar por cima. Cobre coracao (+5 PV),
    /// flechas (+10) e moedas (+3) do GDD.
    /// </summary>
    [CreateAssetMenu(fileName = "Plataforma", menuName = "Loopia/Plataforma/Recompensa")]
    public class PlataformaRecompensaDef : PlataformaDef
    {
        [Header("O que o Lucca pega ao passar")]
        [Tooltip("GDD: o coracao restaura +5 de PV.")]
        public int cura = 0;

        [Tooltip("GDD: o item flecha adiciona 10 flechas.")]
        public int flechas = 0;

        [Tooltip("GDD: a plataforma com moedas adiciona 3 moedas.")]
        public int moedas = 0;

        [Tooltip("Ligado: volta a valer a cada volta. Desligado: so da a recompensa uma vez.")]
        public bool recarregaACadaVolta = true;

        public override void AoColocar(Plataforma plataforma) => plataforma.Carregada = true;

        public override void AoFecharVolta(Plataforma plataforma)
        {
            if (recarregaACadaVolta) plataforma.Carregada = true;
        }

        public override void AoPlayerPassar(Plataforma plataforma, LuccaStatus status)
        {
            if (!plataforma.Carregada || status == null) return;

            plataforma.Carregada = false;

            status.Curar(cura);
            status.GanharFlechas(flechas);
            status.GanharMoedas(moedas);
        }
    }
}

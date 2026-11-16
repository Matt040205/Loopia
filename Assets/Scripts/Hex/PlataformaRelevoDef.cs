using UnityEngine;

namespace Loopia.Hex
{
    [CreateAssetMenu(fileName = "IlhaAlta", menuName = "Loopia/Plataforma/Ilha alta")]
    public class PlataformaRelevoDef : PlataformaDef
    {
        [Min(1f)] public float altura = 2.2f;

        [Header("Mirante (G2-C, prototipo)")]
        [Tooltip("Lucca nesta ilha (levantada) ganha alcance extra e atira em qualquer direcao. " +
                 "So vale quando o LuccaCombate consultar o MiranteDoLucca (integracao pendente).")]
        public bool ehMirante = true;
        [Min(0)] public int bonusDeAlcance = 1;
        public bool tiroEmQualquerDirecao = true;

        public override bool DisponivelParaCompra(MapaDePlataformas mapa) => mapa != null && mapa.TemMola;
    }
}

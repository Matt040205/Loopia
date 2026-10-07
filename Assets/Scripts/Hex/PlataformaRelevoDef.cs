using UnityEngine;

namespace Loopia.Hex
{
    [CreateAssetMenu(fileName = "IlhaAlta", menuName = "Loopia/Plataforma/Ilha alta")]
    public class PlataformaRelevoDef : PlataformaDef
    {
        [Min(1f)] public float altura = 2.2f;
        public override bool DisponivelParaCompra(MapaDePlataformas mapa) => mapa != null && mapa.TemMola;
    }
}

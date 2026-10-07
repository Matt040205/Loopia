using UnityEngine;

namespace Loopia.Hex
{
    [CreateAssetMenu(fileName = "Mola", menuName = "Loopia/Plataforma/Mola")]
    public class PlataformaMolaDef : PlataformaDef
    {
        [Min(1f)] public float alturaDoImpulso = 3f;
        public override void AoPlayerPassar(Plataforma plataforma, LuccaStatus status)
        {
            if (plataforma.Mapa.player != null) plataforma.Mapa.player.CarregarMola(alturaDoImpulso);
        }
    }
}

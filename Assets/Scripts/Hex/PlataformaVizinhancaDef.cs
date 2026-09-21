using UnityEngine;

namespace Loopia.Hex
{
    /// <summary>
    /// Plataforma cujo bonus cresce conforme as vizinhas. Cobre a cabana ("+2 de PV; se tiver
    /// campo pacifico em area adjacente 3x3, +2 por campo pacifico") e a area de treino
    /// ("+8% de dano; se tiver torre em area adjacente 3x3, +15% por torre").
    /// </summary>
    [CreateAssetMenu(fileName = "Plataforma", menuName = "Loopia/Plataforma/Bonus por vizinhanca")]
    public class PlataformaVizinhancaDef : PlataformaSimplesDef
    {
        [Header("Vizinhanca")]
        [Tooltip("Que tipo de plataforma conta como vizinha boa.")]
        public PlataformaDef tipoQueConta;

        [Tooltip("Alcance em hexagonos. O GDD fala em 3x3 e 5x5, pensando em grade quadrada; " +
                 "no hexagono isso vira 1 (as 6 vizinhas) e 2 (as 18 em volta).")]
        [Min(1)] public int raio = 1;

        public int pvPorVizinha = 0;
        public float danoPorVizinha = 0f;

        public override int BonusDePv(Plataforma plataforma)
        {
            return bonusDePv + pvPorVizinha * ContarVizinhas(plataforma);
        }

        public override float BonusDeDano(Plataforma plataforma)
        {
            return bonusDeDano + danoPorVizinha * ContarVizinhas(plataforma);
        }

        int ContarVizinhas(Plataforma plataforma)
        {
            if (tipoQueConta == null || plataforma == null || plataforma.Mapa == null) return 0;
            return plataforma.Mapa.ContarPorTipo(tipoQueConta, plataforma.Casa, raio);
        }
    }
}

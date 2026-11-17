using UnityEngine;

namespace Loopia.Hex
{
    /// <summary>
    /// Floresta: "essa plataforma faz que todo loop tenha lobos nessa area".
    /// A cada volta fechada ela repovoa a propria casa ate lobosNaCasa lobos (outros inimigos na casa nao contam).
    /// </summary>
    [CreateAssetMenu(fileName = "Floresta", menuName = "Loopia/Plataforma/Floresta")]
    public class PlataformaFlorestaDef : PlataformaDef
    {
        [Header("Lobos")]
        public InimigoDef loboQueNasce;

        [Tooltip("Quantos lobos esta casa mantem. O GDD diz que 'todo loop tenha lobos nessa " +
                 "area', e nao que se acumule mais um por volta: a floresta repoe ate este " +
                 "numero, em vez de empilhar para sempre.")]
        [Min(1)] public int lobosNaCasa = 1;

        [Tooltip("Ligado: ja nasce um lobo no instante em que a carta e colocada.")]
        public bool nascerAoColocar = true;

        public override void AoColocar(Plataforma plataforma)
        {
            if (nascerAoColocar) Povoar(plataforma);
        }

        public override void AoFecharVolta(Plataforma plataforma) => Povoar(plataforma);

        void Povoar(Plataforma plataforma)
        {
            if (loboQueNasce == null || plataforma == null || plataforma.Mapa == null) return;

            InimigoSpawner spawner = plataforma.Mapa.inimigos;
            if (spawner == null) return;

            // Repoe so o que falta: sem isto, cada volta empilharia mais um lobo eternamente. Conta so os lobos
            // desta floresta: um morcego pousado na casa nao ocupa a vaga do lobo (antes ocupava, e o lobo nao nascia).
            int faltam = lobosNaCasa - spawner.QuantosEm(plataforma.Casa, loboQueNasce);
            for (int i = 0; i < faltam; i++)
            {
                spawner.Nascer(loboQueNasce, plataforma.Casa);
            }
        }
    }
}

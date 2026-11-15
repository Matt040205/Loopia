using System.Collections.Generic;

namespace Loopia.Hex
{
    /// <summary>
    /// Regras puras do confronto final, separadas para poderem ser testadas sem cena.
    /// </summary>
    public static class RegrasDoChefe
    {
        /// <summary>
        /// Quantas casas a mais de alcance o chefe precisa declarar para o tiro do Lucca o alcancar de
        /// QUALQUER casa do anel. O chefe fica no centro do miolo; num anel de raio R toda casa esta a ~R
        /// do centro, e R vai de 4 a 6 — maior que o alcance do tiro (3). Sem isto ele seria inalcancavel.
        /// </summary>
        public static int AlcanceExtraParaCobrirOAnel(IReadOnlyList<HexCoord> anel, HexCoord centro, int alcanceDoTiro)
        {
            if (anel == null || anel.Count == 0) return 0;

            int maior = 0;
            for (int i = 0; i < anel.Count; i++)
            {
                int distancia = HexCoord.Distance(anel[i], centro);
                if (distancia > maior) maior = distancia;
            }

            return maior > alcanceDoTiro ? maior - alcanceDoTiro : 0;
        }

        /// <summary>A regra de alcance do tiro, com o alcance extra de alvos grandes (0 para inimigos comuns).</summary>
        public static bool DentroDoAlcance(int distanciaEmCasas, int alcanceDoTiro, int alcanceExtraDoAlvo)
        {
            if (alcanceExtraDoAlvo < 0) alcanceExtraDoAlvo = 0;
            return distanciaEmCasas - alcanceExtraDoAlvo <= alcanceDoTiro;
        }
    }
}

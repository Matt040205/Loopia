namespace Loopia.Hex
{
    /// <summary>
    /// G2-C (prototipo aprovado): Ilha alta como Mirante. Enquanto o Lucca esta numa ilha alta de verdade
    /// (relevo levantado, o que exige Mola), ganha alcance extra e pode atirar em qualquer direcao. Regras
    /// puras, sem Unity, para o LuccaCombate consultar quando a integracao for feita
    /// (docs/gameplay/g2/G2_HANDOFF.md, secao Mirante).
    /// </summary>
    public static class RegrasDoMirante
    {
        /// <summary>Abaixo disto a ilha conta como chao (a Ilha alta sem Mola fica com altura 0).</summary>
        public const float AlturaMinima = 0.01f;

        /// <summary>A casa e um mirante ativo: carta de relevo com bonus de mirante e ilha levantada.</summary>
        public static bool EhMirante(PlataformaDef def, float alturaDaIlha)
        {
            return def is PlataformaRelevoDef relevo && relevo.ehMirante && alturaDaIlha > AlturaMinima;
        }

        /// <summary>Alcance do tiro em casas: o base, mais o bonus so enquanto esta no mirante.</summary>
        public static int AlcanceDoTiro(int alcanceBase, bool noMirante, int bonusDeAlcance)
        {
            if (!noMirante || bonusDeAlcance <= 0) return alcanceBase;
            return alcanceBase + bonusDeAlcance;
        }

        /// <summary>
        /// Pode atirar em quem nao esta a frente? Vale no mirante (se a carta liberar) ou para alvos grandes
        /// (regra do chefe, que continua independente do mirante).
        /// </summary>
        public static bool IgnoraDirecao(bool noMirante, bool tiroEmQualquerDirecao, bool alvoDeQualquerAngulo)
        {
            return alvoDeQualquerAngulo || (noMirante && tiroEmQualquerDirecao);
        }
    }
}

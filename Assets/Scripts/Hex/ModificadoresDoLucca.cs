using System;

namespace Loopia.Hex
{
    /// <summary>
    /// O que as plataformas do mapa somam ao Lucca agora (G2). Valor imutavel com soma explicita: o
    /// MapaDePlataformas soma um por plataforma e entrega o total ao LuccaStatus. Nao e um framework de
    /// atributos; o mesmo resumo pode receber equipamentos/status depois (docs/gameplay/g2/MODIFIER_SYSTEM.md).
    /// </summary>
    public readonly struct ModificadoresDoLucca : IEquatable<ModificadoresDoLucca>
    {
        /// <summary>PV maximo extra.</summary>
        public readonly int PvMaximo;

        /// <summary>Dano extra das flechas, em fracao (0.08 = +8%).</summary>
        public readonly float Dano;

        /// <summary>Flechas maximas extras.</summary>
        public readonly int FlechasMaximo;

        /// <summary>PV extra a cada coracao coletado (so coracao, nao toda cura).</summary>
        public readonly int CuraPorCoracao;

        public static readonly ModificadoresDoLucca Zero = default;

        public ModificadoresDoLucca(int pvMaximo, float dano, int flechasMaximo, int curaPorCoracao)
        {
            PvMaximo = pvMaximo;
            Dano = dano;
            FlechasMaximo = flechasMaximo;
            CuraPorCoracao = curaPorCoracao;
        }

        public ModificadoresDoLucca Somar(ModificadoresDoLucca outro)
        {
            return new ModificadoresDoLucca(PvMaximo + outro.PvMaximo, Dano + outro.Dano,
                FlechasMaximo + outro.FlechasMaximo, CuraPorCoracao + outro.CuraPorCoracao);
        }

        public static ModificadoresDoLucca operator +(ModificadoresDoLucca a, ModificadoresDoLucca b) => a.Somar(b);

        public ModificadoresDoLucca ComPvMaximo(int pvMaximo) =>
            new ModificadoresDoLucca(pvMaximo, Dano, FlechasMaximo, CuraPorCoracao);

        public bool Equals(ModificadoresDoLucca outro) =>
            PvMaximo == outro.PvMaximo && Dano.Equals(outro.Dano) &&
            FlechasMaximo == outro.FlechasMaximo && CuraPorCoracao == outro.CuraPorCoracao;

        public override bool Equals(object obj) => obj is ModificadoresDoLucca m && Equals(m);

        public override int GetHashCode()
        {
            unchecked
            {
                int h = PvMaximo;
                h = h * 397 ^ Dano.GetHashCode();
                h = h * 397 ^ FlechasMaximo;
                return h * 397 ^ CuraPorCoracao;
            }
        }

        public static bool operator ==(ModificadoresDoLucca a, ModificadoresDoLucca b) => a.Equals(b);
        public static bool operator !=(ModificadoresDoLucca a, ModificadoresDoLucca b) => !a.Equals(b);

        public override string ToString() =>
            "PV+" + PvMaximo + " dano+" + Dano.ToString("P0") + " flechas+" + FlechasMaximo + " coracao+" + CuraPorCoracao;
    }
}

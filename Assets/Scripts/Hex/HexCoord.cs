using System;
using UnityEngine;

namespace Loopia.Hex
{
    /// <summary>
    /// Coordenada axial (q, r) de um hexagono "pointy-top", igual a imagem de referencia:
    /// q cresce para o leste (+X) e r cresce para o norte (+Z).
    /// O terceiro eixo do sistema cubico e derivado: s = -q - r.
    /// </summary>
    [Serializable]
    public struct HexCoord : IEquatable<HexCoord>
    {
        public int q;
        public int r;

        public HexCoord(int q, int r)
        {
            this.q = q;
            this.r = r;
        }

        public int S => -q - r;

        public static readonly HexCoord Zero = new HexCoord(0, 0);

        /// <summary>As 6 direcoes vizinhas, no sentido anti-horario a partir do leste.</summary>
        public static readonly HexCoord[] Directions =
        {
            new HexCoord(1, 0),   // leste
            new HexCoord(0, 1),   // nordeste
            new HexCoord(-1, 1),  // noroeste
            new HexCoord(-1, 0),  // oeste
            new HexCoord(0, -1),  // sudoeste
            new HexCoord(1, -1),  // sudeste
        };

        public HexCoord Neighbor(int direction)
        {
            int i = direction % 6;
            if (i < 0) i += 6;
            return this + Directions[i];
        }

        /// <summary>Quantos passos de hexagono separam a e b.</summary>
        public static int Distance(HexCoord a, HexCoord b)
        {
            int dq = a.q - b.q;
            int dr = a.r - b.r;
            int ds = a.S - b.S;
            return (Mathf.Abs(dq) + Mathf.Abs(dr) + Mathf.Abs(ds)) / 2;
        }

        public int DistanceTo(HexCoord other) => Distance(this, other);

        /// <summary>
        /// Arredonda uma coordenada axial fracionaria para o hexagono mais proximo.
        /// Converte para cubo, arredonda os tres eixos e corrige o que estiver mais errado.
        /// </summary>
        public static HexCoord Round(float qf, float rf)
        {
            float sf = -qf - rf;

            int rq = Mathf.RoundToInt(qf);
            int rr = Mathf.RoundToInt(rf);
            int rs = Mathf.RoundToInt(sf);

            float dq = Mathf.Abs(rq - qf);
            float dr = Mathf.Abs(rr - rf);
            float ds = Mathf.Abs(rs - sf);

            if (dq > dr && dq > ds) rq = -rr - rs;
            else if (dr > ds) rr = -rq - rs;

            return new HexCoord(rq, rr);
        }

        public static HexCoord operator +(HexCoord a, HexCoord b) => new HexCoord(a.q + b.q, a.r + b.r);
        public static HexCoord operator -(HexCoord a, HexCoord b) => new HexCoord(a.q - b.q, a.r - b.r);
        public static HexCoord operator *(HexCoord a, int k) => new HexCoord(a.q * k, a.r * k);
        public static bool operator ==(HexCoord a, HexCoord b) => a.q == b.q && a.r == b.r;
        public static bool operator !=(HexCoord a, HexCoord b) => !(a == b);

        public bool Equals(HexCoord other) => this == other;
        public override bool Equals(object obj) => obj is HexCoord other && this == other;
        public override int GetHashCode() => (q * 397) ^ r;
        public override string ToString() => $"({q}, {r})";
    }
}

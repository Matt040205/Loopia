using UnityEngine;

namespace Loopia.Hex
{
    /// <summary>
    /// Converte entre coordenada axial e posicao no plano XZ para hexagonos "pointy-top"
    /// (vertice apontando para o norte, lados retos a leste e a oeste).
    /// Todas as contas sao feitas no espaco local do mundo.
    /// </summary>
    [System.Serializable]
    public struct HexLayout
    {
        public static readonly float Sqrt3 = Mathf.Sqrt(3f);

        [Tooltip("Distancia do centro do hexagono ate um de seus vertices.")]
        public float size;

        public HexLayout(float size)
        {
            this.size = size;
        }

        /// <summary>Largura do hexagono: distancia entre os vizinhos leste e oeste.</summary>
        public float Width => Sqrt3 * size;

        /// <summary>Altura total do hexagono, de vertice a vertice.</summary>
        public float Height => 2f * size;

        /// <summary>Distancia vertical entre duas linhas de hexagonos.</summary>
        public float RowSpacing => 1.5f * size;

        public Vector3 ToWorld(HexCoord hex)
        {
            float x = size * Sqrt3 * (hex.q + hex.r * 0.5f);
            float z = size * 1.5f * hex.r;
            return new Vector3(x, 0f, z);
        }

        public HexCoord ToHex(Vector3 local)
        {
            float qf = (local.x * Sqrt3 / 3f - local.z / 3f) / size;
            float rf = (local.z * 2f / 3f) / size;
            return HexCoord.Round(qf, rf);
        }

        /// <summary>Posicao do vertice "index" (0 a 5) em relacao ao centro do hexagono.</summary>
        public Vector3 Corner(int index)
        {
            float angle = Mathf.Deg2Rad * (60f * index - 30f);
            return new Vector3(size * Mathf.Cos(angle), 0f, size * Mathf.Sin(angle));
        }
    }
}

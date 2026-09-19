using System.Collections.Generic;

namespace Loopia.Hex
{
    /// <summary>
    /// O caminho em loop do GDD: uma lista ordenada de hexagonos em que a ultima casa
    /// e vizinha da primeira. Andar sempre para o proximo indice fecha a volta e recomeca,
    /// que e exatamente o "o fim conecta-se ao inicio".
    /// </summary>
    public class HexLoopPath
    {
        readonly List<HexCoord> _casas = new List<HexCoord>();
        readonly Dictionary<HexCoord, int> _indices = new Dictionary<HexCoord, int>();

        public IReadOnlyList<HexCoord> Casas => _casas;
        public int Count => _casas.Count;

        /// <summary>Indexador circular: pode passar de Count ou ficar negativo sem problema.</summary>
        public HexCoord this[int indice] => _casas[Normalizar(indice)];

        public void Definir(IList<HexCoord> casas)
        {
            _casas.Clear();
            _indices.Clear();

            for (int i = 0; i < casas.Count; i++)
            {
                _casas.Add(casas[i]);
                _indices[casas[i]] = i;
            }
        }

        public int Normalizar(int indice)
        {
            if (_casas.Count == 0) return 0;
            int i = indice % _casas.Count;
            return i < 0 ? i + _casas.Count : i;
        }

        public bool Contem(HexCoord casa) => _indices.ContainsKey(casa);

        public bool TryIndiceDe(HexCoord casa, out int indice) => _indices.TryGetValue(casa, out indice);

        /// <summary>Proxima casa a partir de uma casa do loop. Sentido +1 ou -1.</summary>
        public bool TryProxima(HexCoord atual, int sentido, out HexCoord proxima)
        {
            proxima = default(HexCoord);

            int indice;
            if (!_indices.TryGetValue(atual, out indice)) return false;

            proxima = this[indice + sentido];
            return true;
        }
    }
}

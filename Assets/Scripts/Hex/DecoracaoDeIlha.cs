using System.Collections.Generic;
using UnityEngine;

namespace Loopia.Hex
{
    [DisallowMultipleComponent]
    public class DecoracaoDeIlha : MonoBehaviour
    {
        public GameObject[] modelos;
        [Range(1, 8)] public int quantidade = 4;
        [Min(0.1f)] public float larguraDoCorredor = 0.4f;
        public int semente = 173;

        public void Decorar(HexTile ilha, HexWorld mundo)
        {
            if (modelos == null || modelos.Length == 0) return;
            var raiz = new GameObject("Decoracoes").transform;
            raiz.SetParent(ilha.transform, false);
            var random = new System.Random(unchecked(semente + ilha.Coord.GetHashCode()));
            float raio = mundo.tamanhoDoHexagono * (1f - mundo.folgaEntreHexagonos);
            var ocupadas = new List<Vector3>();
            var pegadas = new List<float>();
            for (int i = 0; i < quantidade; i++)
            {
                var prefab = modelos[random.Next(modelos.Length)];
                if (prefab == null) continue;
                float tamanho = Mathf.Lerp(0.28f, 0.65f, (float)random.NextDouble());
                var go = VisualDeIlha.Criar(prefab, raiz, tamanho);
                go.transform.localRotation = Quaternion.Euler(0f, random.Next(360), 0f);
                Bounds b = VisualDeIlha.LimitesLocais(go.transform);
                float pegada = new Vector2(b.extents.x, b.extents.z).magnitude;
                bool colocou = false;
                for (int tentativa = 0; tentativa < 60; tentativa++)
                {
                    float angulo = random.Next(6) * 60f + (float)random.NextDouble() * 12f - 6f;
                    float distancia = raio * Mathf.Lerp(0.72f, 0.82f, (float)random.NextDouble());
                    Vector3 p = Quaternion.Euler(0f, angulo, 0f) * Vector3.forward * distancia;
                    if (!PosicaoLivre(p, pegada, raio, larguraDoCorredor)) continue;
                    bool sobrepoe = false;
                    for (int j = 0; j < ocupadas.Count; j++)
                        if (Vector3.Distance(ocupadas[j], p) < pegada + pegadas[j] + 0.12f) sobrepoe = true;
                    if (sobrepoe) continue;
                    go.transform.localPosition = p;
                    ocupadas.Add(p);
                    pegadas.Add(pegada);
                    colocou = true;
                    break;
                }
                if (!colocou) { go.SetActive(false); Destroy(go); }
            }
        }

        /// <summary>Reserva o centro e os seis acessos, inclusive para ilhas colocadas depois.</summary>
        public static bool PosicaoLivre(Vector3 p, float pegada, float raio, float corredor)
        {
            if (p.magnitude - pegada < raio * 0.48f) return false;
            float apotema = raio * HexLayout.Sqrt3 * 0.5f;
            for (int i = 0; i < 6; i++)
            {
                Vector3 direcao = new HexLayout(1f).ToWorld(new HexCoord(0, 0).Neighbor(i)).normalized;
                if (Vector3.Dot(p, direcao) + pegada > apotema * 0.99f) return false;
                float t = Mathf.Clamp(Vector3.Dot(p, direcao), 0f, raio);
                if (Vector3.Distance(p, direcao * t) < corredor + pegada) return false;
            }
            return true;
        }
    }
}

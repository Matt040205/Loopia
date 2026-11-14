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

        [Tooltip("Opcional. Com perfil, o modelo, o tamanho (em L) e o lado da ilha vem das regras da Scene Bible. " +
                 "Sem perfil, vale o comportamento antigo (lista 'modelos', tamanho 0.28 a 0.65).")]
        public ArtScaleProfile perfil;

        public void Decorar(HexTile ilha, HexWorld mundo)
        {
            if (perfil != null) { DecorarComPerfil(ilha, mundo); return; }
            DecorarSemPerfil(ilha, mundo);
        }

        /// <summary>Comportamento original, com a mesma sequencia de sorteios.</summary>
        void DecorarSemPerfil(HexTile ilha, HexWorld mundo)
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
                if (!colocou) Descartar(go);
            }
        }

        /// <summary>
        /// Teste conservador de oclusao: a caixa do modelo em p (alinhada ao mundo, folgada para qualquer rotacao em Y)
        /// esta entre a camera e algum ponto (a 0.5 u de altura) dos seis corredores da ilha, ate duas ilhas de distancia?
        /// Tudo no espaco local da ilha; direcaoDaCamera e o sentido em que a camera olha (ja local).
        /// </summary>
        public static bool CobreOCaminho(Vector3 p, Bounds modelo, Vector3 direcaoDaCamera, float tamanhoDoHexagono)
        {
            float meia = new Vector2(modelo.extents.x, modelo.extents.z).magnitude;
            float altura = modelo.size.y;
            var caixa = new Bounds(new Vector3(p.x, altura * 0.5f, p.z), new Vector3(meia * 2f, altura, meia * 2f));
            float passo = tamanhoDoHexagono * HexLayout.Sqrt3 / 6f;
            float alcance = tamanhoDoHexagono * HexLayout.Sqrt3 * 2f;
            for (int k = 0; k < 6; k++)
            {
                Vector3 direcao = new HexLayout(1f).ToWorld(new HexCoord(0, 0).Neighbor(k)).normalized;
                for (float t = 0f; t <= alcance + 0.001f; t += passo)
                {
                    Vector3 q = direcao * t + Vector3.up * 0.5f;
                    var raio = new Ray(q - direcaoDaCamera * 100f, direcaoDaCamera);
                    if (caixa.IntersectRay(raio, out float d) && d < 99.5f) return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Decoracao guiada pelo perfil de escala: modelo ponderado, tamanho pela regra da categoria (em L),
        /// medido no eixo da regra. Mesmas regras de posicao (centro/corredores/borda), 60 tentativas e semente
        /// por coordenada; categorias "so no lado longe da camera" so aceitam posicoes com Dot(p, frente) > 0.2.
        /// </summary>
        void DecorarComPerfil(HexTile ilha, HexWorld mundo)
        {
            var raiz = new GameObject("Decoracoes").transform;
            raiz.SetParent(ilha.transform, false);
            var random = new System.Random(unchecked(semente + ilha.Coord.GetHashCode()));
            float raio = mundo.tamanhoDoHexagono * (1f - mundo.folgaEntreHexagonos);
            // A camera olha para +frente; "longe da camera" = lado positivo. Convertido para o espaco local da ilha.
            Vector3 frente = ilha.transform.InverseTransformDirection(perfil.FrenteDaCameraXZ);
            frente.y = 0f;
            frente = frente.sqrMagnitude > 0.0001f ? frente.normalized : perfil.FrenteDaCameraXZ;
            Vector3 direcaoDaCamera = ilha.transform.InverseTransformDirection(perfil.DirecaoDaCamera);
            var ocupadas = new List<Vector3>();
            var pegadas = new List<float>();
            for (int i = 0; i < quantidade; i++)
            {
                ModeloDeArte modelo = perfil.Sortear(random);
                if (modelo == null) continue;
                if (!perfil.TryRegra(modelo.categoria, out RegraDeCategoria regra)) continue;
                float tamanho = perfil.SortearTamanho(regra, random);
                var go = VisualDeIlha.Criar(modelo.prefab, raiz, tamanho, false, regra.eixo);
                go.transform.localRotation = Quaternion.Euler(0f, random.Next(360), 0f);
                Bounds b = VisualDeIlha.LimitesLocais(go.transform);
                float pegada = new Vector2(b.extents.x, b.extents.z).magnitude * regra.fracaoDaPegada;
                bool colocou = false;
                for (int tentativa = 0; tentativa < 60; tentativa++)
                {
                    float angulo = random.Next(6) * 60f + (float)random.NextDouble() * 12f - 6f;
                    float distancia = raio * Mathf.Lerp(0.72f, 0.82f, (float)random.NextDouble());
                    Vector3 p = Quaternion.Euler(0f, angulo, 0f) * Vector3.forward * distancia;
                    if (regra.soNoLadoLongeDaCamera && !EstaLongeDaCamera(p, frente, regra.limiarDoLadoLonge)) continue;
                    if (!PosicaoLivre(p, pegada, raio, larguraDoCorredor)) continue;
                    bool sobrepoe = false;
                    for (int j = 0; j < ocupadas.Count; j++)
                        if (Vector3.Distance(ocupadas[j], p) < pegada + pegadas[j] + 0.12f) sobrepoe = true;
                    if (sobrepoe) continue;
                    if (regra.evitarCobrirCaminho && CobreOCaminho(p, b, direcaoDaCamera, mundo.tamanhoDoHexagono)) continue;
                    go.transform.localPosition = p;
                    ocupadas.Add(p);
                    pegadas.Add(pegada);
                    colocou = true;
                    break;
                }
                if (!colocou) Descartar(go);
            }
        }

        /// <summary>Lado oposto a camera: a posicao aponta (em mais de 0.2 de cosseno) na mesma direcao em que a camera olha.</summary>
        public static bool EstaLongeDaCamera(Vector3 p, Vector3 frenteDaCameraXZ, float limiar = 0.2f)
            => Vector3.Dot(p.normalized, frenteDaCameraXZ) > limiar;

        // Destroy so vale em Play Mode; no Editor (testes) ele registra erro, entao usa DestroyImmediate.
        static void Descartar(GameObject go)
        {
            go.SetActive(false);
            if (Application.isPlaying) Destroy(go);
            else DestroyImmediate(go);
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

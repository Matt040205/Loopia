using System.Collections.Generic;
using UnityEngine;

public class LoopGenerator : MonoBehaviour
{
    [Header("Prefabs & Configurações")]
    [Tooltip("O prefab da plataforma comum que preencherá a maior parte do caminho.")]
    public GameObject plataformaSimplesPrefab;
    [Tooltip("O prefab da sua plataforma especial de spawn/loop.")]
    public GameObject plataformaLarPrefab;
    [Tooltip("Prefab usado nos slots internos do grid (área de construção fora do caminho).")]
    public GameObject slotInternoPrefab;
    [Tooltip("Distância base entre os hexágonos do grid.")]
    public float tileSpacing = 2f;
    [Tooltip("Valor fixo de Y para todas as plataformas")]
    public float fixedY = -65.01516f;

    [Header("Configurações do Grid")]
    [Tooltip("Raio do anel hexagonal (quantos hexágonos por lado).")]
    public int gridSize = 3;

    [Header("Configurações de Gap/Pulo")]
    [Tooltip("Quantos passos do caminho o trampolim pula. 2 = pula uma plataforma (gap pequeno).")]
    public int gapSize = 2;

    // Posições mundiais do loop e instâncias de plataforma
    private List<Vector3> pathPositions = new List<Vector3>();
    private Dictionary<Vector3, GameObject> plataformasAtuais = new Dictionary<Vector3, GameObject>();
    private Dictionary<Vector3, GameObject> slotsInternos = new Dictionary<Vector3, GameObject>();

    // As 6 direções vizinhas de um hexágono (coordenadas axiais)
    public static readonly Vector2Int[] DirecoesHex =
    {
        new Vector2Int(1, 0),  new Vector2Int(1, -1), new Vector2Int(0, -1),
        new Vector2Int(-1, 0), new Vector2Int(-1, 1), new Vector2Int(0, 1)
    };

    void Awake()
    {
        // Validação para garantir que os prefabs foram configurados
        if (plataformaSimplesPrefab == null || plataformaLarPrefab == null)
        {
            Debug.LogError("ERRO: Um ou mais prefabs de plataforma não foram definidos no Inspector do LoopGenerator!", this.gameObject);
            return;
        }

        GerarCaminhoDoLoop();
        GerarPlataformasSimples();
        GerarSlotsInternos();
    }

    // 1) Gera as coordenadas do anel hexagonal (loop fechado sem cruzamentos)
    void GerarCaminhoDoLoop()
    {
        pathPositions.Clear();
        int size = Mathf.Max(1, gridSize);
        var hexPath = new List<Vector2Int>();
        Vector2Int current = new Vector2Int(0, -size); // topo do anel

        // As 6 direções axiais percorridas em ordem formam um anel de raio "size"
        Vector2Int[] direcoes =
        {
            new Vector2Int(1, 0),  new Vector2Int(1, -1), new Vector2Int(0, -1),
            new Vector2Int(-1, 0), new Vector2Int(-1, 1), new Vector2Int(0, 1)
        };

        for (int side = 0; side < 6; side++)
        {
            for (int i = 0; i < size; i++)
            {
                hexPath.Add(current);
                current += direcoes[side];
            }
        }

        // Fecha o loop repetindo o ponto inicial (o LoopMover usa índice % PathCount)
        hexPath.Add(hexPath[0]);

        foreach (var h in hexPath)
        {
            pathPositions.Add(HexToWorld(h.x, h.y));
        }
    }

    // Conversão de coordenada axial (q, r) para posição mundial (hexágono pointy-top)
    public Vector3 HexToWorld(int q, int r)
    {
        float x = tileSpacing * Mathf.Sqrt(3f) * (q + r * 0.5f);
        float z = tileSpacing * 1.5f * r;
        return new Vector3(x, fixedY, z);
    }

    // Conversão inversa: posição mundial -> coordenada axial (q, r) arredondada
    public Vector2Int WorldToHex(Vector3 worldPos)
    {
        float qf = worldPos.x / (tileSpacing * Mathf.Sqrt(3f)) - worldPos.z / (3f * tileSpacing);
        float rf = worldPos.z / (1.5f * tileSpacing);
        float sf = -qf - rf;

        float rq = Mathf.Round(qf);
        float rr = Mathf.Round(rf);
        float rs = Mathf.Round(sf);

        float dq = Mathf.Abs(rq - qf);
        float dr = Mathf.Abs(rr - rf);
        float ds = Mathf.Abs(rs - sf);

        if (dq > dr && dq > ds) rq = -rr - rs;
        else if (dr > ds) rr = -rq - rs;

        return new Vector2Int((int)rq, (int)rr);
    }

    // 2) Instancia a plataforma correta em cada ponto (mundo), salva no dicionário
    void GerarPlataformasSimples()
    {
        plataformasAtuais.Clear();
        for (int i = 0; i < pathPositions.Count; i++)
        {
            // Se for o primeiro ponto do caminho (o spawn), usa a Plataforma Lar.
            GameObject prefabParaInstanciar = (i == 0) ? plataformaLarPrefab : plataformaSimplesPrefab;

            Vector3 mundo = pathPositions[i];

            // Evita instanciar duas plataformas no mesmo lugar (ponto final == inicial)
            if (plataformasAtuais.ContainsKey(mundo)) continue;

            var tile = Instantiate(prefabParaInstanciar, mundo, Quaternion.identity, transform);
            plataformasAtuais[mundo] = tile;
        }
    }

    // 3) Gera os slots internos do disco hexagonal (hexágonos com distância < gridSize do centro)
    void GerarSlotsInternos()
    {
        slotsInternos.Clear();
        if (slotInternoPrefab == null) return;

        int size = Mathf.Max(1, gridSize);
        for (int q = -size + 1; q <= size - 1; q++)
        {
            int r1 = Mathf.Max(-size + 1, -q - size + 1);
            int r2 = Mathf.Min(size - 1, -q + size - 1);
            for (int r = r1; r <= r2; r++)
            {
                Vector3 mundo = HexToWorld(q, r);
                if (plataformasAtuais.ContainsKey(mundo)) continue;

                var slot = Instantiate(slotInternoPrefab, mundo, Quaternion.identity, transform);
                slotsInternos[mundo] = slot;
            }
        }
    }

    // --- Métodos expostos para uso externo ---

    public int PathCount => pathPositions.Count;

    // Retorna o índice do ponto do caminho mais próximo da posição mundial informada
    public int GetIndexPorPosicao(Vector3 worldPos)
    {
        if (pathPositions.Count == 0) return -1;

        int melhor = 0;
        float menorDist = float.MaxValue;
        for (int i = 0; i < pathPositions.Count; i++)
        {
            float dx = pathPositions[i].x - worldPos.x;
            float dz = pathPositions[i].z - worldPos.z;
            float d = dx * dx + dz * dz;
            if (d < menorDist)
            {
                menorDist = d;
                melhor = i;
            }
        }
        return melhor;
    }

    public Vector3 GetWorldPositionByIndex(int idx)
    {
        if (PathCount == 0) return Vector3.zero;

        idx = idx % PathCount;
        if (idx < 0) idx += PathCount;
        return pathPositions[idx];
    }

    public void UpdatePlatformReference(Vector3 worldPos, GameObject novoTile)
    {
        Vector2Int hex = WorldToHex(worldPos);
        Vector3 key = HexToWorld(hex.x, hex.y);

        if (plataformasAtuais.ContainsKey(key))
        {
            plataformasAtuais[key] = novoTile;
        }
        else if (slotsInternos.ContainsKey(key))
        {
            slotsInternos[key] = novoTile;
        }
    }

    // Retorna a plataforma/slot que ocupa o hexágono mais próximo da posição informada
    public GameObject GetPlataformaEm(Vector3 worldPos)
    {
        Vector2Int hex = WorldToHex(worldPos);
        return GetPlataformaEmHex(hex.x, hex.y);
    }

    public GameObject GetPlataformaEmHex(int q, int r)
    {
        Vector3 key = HexToWorld(q, r);
        if (plataformasAtuais.TryGetValue(key, out var plataforma)) return plataforma;
        if (slotsInternos.TryGetValue(key, out var slot)) return slot;
        return null;
    }

    // True se o hexágono informado faz parte do caminho do loop
    public bool EhHexDoCaminho(int q, int r)
    {
        Vector3 key = HexToWorld(q, r);
        return plataformasAtuais.ContainsKey(key);
    }

    public List<Vector3> GetPath()
    {
        return new List<Vector3>(pathPositions);
    }
}

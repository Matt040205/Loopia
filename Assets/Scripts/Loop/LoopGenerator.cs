using System.Collections.Generic;
using UnityEngine;
using System.Linq; // Adicionado para facilitar a vida

public class LoopGenerator : MonoBehaviour
{
    [Header("Prefabs & Configurações")]
    [Tooltip("O prefab da plataforma comum que preencherá a maior parte do caminho.")]
    public GameObject plataformaSimplesPrefab;
    [Tooltip("O prefab da sua plataforma especial de spawn/loop.")]
    public GameObject plataformaLarPrefab; // <-- NOVA VARIÁVEL
    public float tileSpacing = 2f;
    [Tooltip("Valor fixo de Y para todas as plataformas")]
    public float fixedY = -65.01516f;

    // Guarda posições lógicas do loop (x,z) e instâncias
    private List<Vector3> pathPositions = new List<Vector3>();
    private Dictionary<Vector3, GameObject> plataformasAtuais = new Dictionary<Vector3, GameObject>();

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
    }

    // 1) Gera as coordenadas lógicas do loop (bordas de 3x3; centro vazio)
    void GerarCaminhoDoLoop()
    {
        pathPositions.Clear();
        pathPositions.Add(new Vector3(1, 0, 0)); // Posição 0 - SPAWN
        pathPositions.Add(new Vector3(2, 0, 0));
        pathPositions.Add(new Vector3(2, 0, 1));
        pathPositions.Add(new Vector3(2, 0, 2));
        pathPositions.Add(new Vector3(1, 0, 2));
        pathPositions.Add(new Vector3(0, 0, 2));
        pathPositions.Add(new Vector3(0, 0, 1));
        pathPositions.Add(new Vector3(0, 0, 0));
        pathPositions.Add(new Vector3(1, 0, 0)); // fecha o loop
    }

    // 2) Instancia a plataforma correta em cada ponto (mundo), salva no dicionário
    void GerarPlataformasSimples()
    {
        plataformasAtuais.Clear();
        for (int i = 0; i < pathPositions.Count; i++)
        {
            // --- LÓGICA ATUALIZADA AQUI ---
            GameObject prefabParaInstanciar;

            // Se for o primeiro ponto do caminho (o spawn), usa a Plataforma Lar.
            if (i == 0)
            {
                prefabParaInstanciar = plataformaLarPrefab;
            }
            // Para todos os outros pontos, usa a plataforma simples.
            else
            {
                prefabParaInstanciar = plataformaSimplesPrefab;
            }

            Vector3 posXZ = pathPositions[i];
            Vector3 mundo = new Vector3(
                posXZ.x * tileSpacing,
                fixedY,
                posXZ.z * tileSpacing
            );

            // Evita instanciar duas plataformas no mesmo lugar se o ponto final do caminho for o mesmo que o inicial
            if (plataformasAtuais.ContainsKey(mundo)) continue;

            var tile = Instantiate(prefabParaInstanciar, mundo, Quaternion.identity, transform);
            plataformasAtuais[mundo] = tile;
        }
    }

    // --- Métodos expostos para uso externo (sem alterações) ---

    public int PathCount => pathPositions.Count;

    public int GetIndexPorPosicao(Vector3 worldPos)
    {
        Vector3 lógica = new Vector3(Mathf.Round(worldPos.x / tileSpacing), 0, Mathf.Round(worldPos.z / tileSpacing));
        return pathPositions.IndexOf(lógica);
    }

    public Vector3 GetWorldPositionByIndex(int idx)
    {
        // Pequena correção para evitar erro se o PathCount for 0
        if (PathCount == 0) return Vector3.zero;

        idx = idx % PathCount;
        if (idx < 0) idx += PathCount;

        Vector3 lógica = pathPositions[idx];
        return new Vector3(lógica.x * tileSpacing, fixedY, lógica.z * tileSpacing);
    }

    public void UpdatePlatformReference(Vector3 worldPos, GameObject novoTile)
    {
        Vector3 key = new Vector3(Mathf.Round(worldPos.x / tileSpacing) * tileSpacing, fixedY, Mathf.Round(worldPos.z / tileSpacing) * tileSpacing);
        if (plataformasAtuais.ContainsKey(key))
            plataformasAtuais[key] = novoTile;
    }

    public List<Vector3> GetPath()
    {
        // Usando LINQ para uma conversão mais limpa e direta.
        return pathPositions.Select(posXZ => new Vector3(posXZ.x * tileSpacing, fixedY, posXZ.z * tileSpacing)).ToList();
    }
}
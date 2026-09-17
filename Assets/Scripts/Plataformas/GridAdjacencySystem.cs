using System.Collections.Generic;
using UnityEngine;

// Sistema de adjacência do grid hexagonal (efeitos "3x3" do PDF)
public static class GridAdjacencySystem
{
    // Área "3x3" no grid hexagonal: o próprio hexágono + seus 6 vizinhos (distância <= 1)
    public static List<Vector2Int> Area3x3(Vector2Int centro)
    {
        var area = new List<Vector2Int> { centro };
        foreach (var dir in LoopGenerator.DirecoesHex)
        {
            area.Add(centro + dir);
        }
        return area;
    }

    public static List<GameObject> PlataformasNaArea(LoopGenerator gerador, Vector2Int centro)
    {
        var lista = new List<GameObject>();
        if (gerador == null) return lista;

        foreach (var hex in Area3x3(centro))
        {
            GameObject obj = gerador.GetPlataformaEmHex(hex.x, hex.y);
            if (obj != null) lista.Add(obj);
        }
        return lista;
    }

    public static int ContarTipoNaArea<T>(LoopGenerator gerador, Vector2Int centro) where T : Component
    {
        int contagem = 0;
        foreach (var obj in PlataformasNaArea(gerador, centro))
        {
            if (obj.GetComponent<T>() != null) contagem++;
        }
        return contagem;
    }
}

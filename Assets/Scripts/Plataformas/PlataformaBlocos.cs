using System.Collections.Generic;
using UnityEngine;

public class PlataformaBlocos : MonoBehaviour
{
    [Header("Configuração dos Blocos")]
    [Tooltip("Prefab do bloco destrutível (com componente BlocoDestrutivel).")]
    public GameObject blocoPrefab;
    [Tooltip("Posições locais onde os blocos nascem sobre a plataforma.")]
    public Vector3[] posicoesDosBlocos = new Vector3[]
    {
        new Vector3(-0.7f, 0.6f, 0f),
        new Vector3(0f, 0.6f, 0f),
        new Vector3(0.7f, 0.6f, 0f)
    };
    [Tooltip("Vida de cada bloco.")]
    public float vidaDoBloco = 30f;

    private Dictionary<int, GameObject> blocosPorIndice = new Dictionary<int, GameObject>();

    void OnEnable()
    {
        GameManager.OnLoopAvancado += RegenerarBlocos;
    }

    void OnDisable()
    {
        GameManager.OnLoopAvancado -= RegenerarBlocos;
    }

    void Start()
    {
        CriarTodosOsBlocos();
    }

    private void CriarTodosOsBlocos()
    {
        if (blocoPrefab == null)
        {
            Debug.LogWarning("[PlataformaBlocos] blocoPrefab não definido!", this.gameObject);
            return;
        }

        for (int i = 0; i < posicoesDosBlocos.Length; i++)
        {
            CriarBloco(i);
        }
    }

    private void CriarBloco(int indice)
    {
        GameObject bloco = Instantiate(blocoPrefab, transform.position + posicoesDosBlocos[indice], Quaternion.identity, transform);
        BlocoDestrutivel destrutivel = bloco.GetComponent<BlocoDestrutivel>();
        if (destrutivel != null)
        {
            destrutivel.plataformaDona = this;
            destrutivel.vida = vidaDoBloco;
            destrutivel.indice = indice;
        }
        blocosPorIndice[indice] = bloco;
    }

    public void RegistrarBlocoDestruido(BlocoDestrutivel bloco)
    {
        if (bloco == null) return;
        blocosPorIndice[bloco.indice] = null;
    }

    // Ao fim de cada loop, recria os blocos que foram destruídos
    private void RegenerarBlocos()
    {
        foreach (int indice in new List<int>(blocosPorIndice.Keys))
        {
            if (blocosPorIndice[indice] == null)
            {
                CriarBloco(indice);
            }
        }
        Debug.Log("<color=cyan>[PlataformaBlocos] Blocos regenerados para o novo loop.</color>");
    }
}

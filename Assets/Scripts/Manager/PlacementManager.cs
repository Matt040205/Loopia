using UnityEngine;
using System.Collections;

public class PlacementManager : MonoBehaviour
{
    public static PlacementManager Instance;
    private GameObject plataformaSelecionada;
    private UIPlatformCard cartaSelecionada;

    void Awake() { Instance = this; }

    public void SelecionarPlataforma(GameObject prefab, UIPlatformCard cartaVisual)
    {
        plataformaSelecionada = prefab;
        cartaSelecionada = cartaVisual;
    }

    public void TentarConstruirNaPlataforma(PlataformaAlvo alvo)
    {
        // --- NOVA VERIFICAÇÃO DE SEGURANÇA ---
        // Se o alvo tem o componente PlataformaLar, exibe uma mensagem e não faz nada.
        if (alvo.GetComponent<PlataformaLar>() != null)
        {
            Debug.Log("A Plataforma Lar é insubstituível!");
            return;
        }

        if (plataformaSelecionada != null && alvo != null)
        {
            StartCoroutine(SubstituirPlataforma(alvo, plataformaSelecionada));
            if (cartaSelecionada != null)
            {
                cartaSelecionada.UsarCarta();
                cartaSelecionada = null;
            }
            plataformaSelecionada = null;
        }
    }

    private IEnumerator SubstituirPlataforma(PlataformaAlvo plataformaAntiga, GameObject prefabNovaPlataforma)
    {
        Vector3 pos = plataformaAntiga.transform.position;
        Quaternion rot = plataformaAntiga.transform.rotation;
        Transform parent = plataformaAntiga.transform.parent;
        Destroy(plataformaAntiga.gameObject);
        yield return new WaitForEndOfFrame();
        Instantiate(prefabNovaPlataforma, pos, rot, parent);
    }
}
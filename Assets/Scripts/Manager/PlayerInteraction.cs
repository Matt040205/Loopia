// PlayerInteraction.cs (Atualizado)

using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                // MUDANÇA AQUI: Procuramos por PlataformaAlvo em vez de PlataformaBase
                PlataformaAlvo alvo = hit.collider.GetComponent<PlataformaAlvo>();

                if (alvo != null)
                {
                    Debug.Log("Clique detectado no alvo: " + alvo.name);
                    PlacementManager.Instance.TentarConstruirNaPlataforma(alvo);
                }
            }
        }
    }
}